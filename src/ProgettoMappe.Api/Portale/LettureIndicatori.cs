using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Portale;

/// <summary>Le letture degli indicatori aziendali (sezione 3), separate dal calcolo per provarlo con dati finti.</summary>
public interface ILettureIndicatori
{
    /// <summary>Vigneti, infezioni del giorno, rilievi dell'anno fino al giorno, fasi BBCH e protezione dei 7 giorni seguenti.</summary>
    Task<DatiIndicatori> LeggiAsync(DateTime giorno, CancellationToken ct = default);

    /// <summary>I vigneti abbinati all'utente (<c>VistaUtentiVigneti</c>), per chi non è MasterUser.</summary>
    Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default);
}

/// <summary>Senza 4Grapes: nessun dato.</summary>
public class InMemoryLettureIndicatori : ILettureIndicatori
{
    public Task<DatiIndicatori> LeggiAsync(DateTime giorno, CancellationToken ct = default) =>
        Task.FromResult(new DatiIndicatori([], new Dictionary<(int, int), decimal>(), [], new Dictionary<int, int>(), [], []));

    public Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());
}

/// <summary>
/// Le query del controller del portale, riscritte in SQL (dba, 14/11: Organo FOGLIA = 1, GRAPPOLO = 2; TipoEnte
/// AZIENDA_VITIVINICOLA = 1; colonna <c>Rilievo.PercIntensità</c> con l'accento; <c>ModelloProtezione.Data</c> con ore
/// diverse: sempre per intervallo). Ogni lettura parte dai vigneti degli indicatori (mai una query solo per data su
/// <c>ModelloInfezione</c>, che ha 42 milioni di righe).
/// </summary>
public class FourGrapesLettureIndicatori : ILettureIndicatori
{
    public const int OrganoFoglia = 1;
    public const int OrganoGrappolo = 2;
    public const int TipoEnteAziendaVitivinicola = 1;

    /// <summary>I vigneti di <c>sql_filtri_vigneti</c>: Carico, non estirpati, di aziende vitivinicole, con il comune.</summary>
    private const string Vigneti = """
        SELECT v.IdVigneto, vg.Ente_IdEnte AS IdAzienda
        FROM dbo.Vigneto v
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
        JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
        WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = @TipoEnte AND m.Area3 IS NOT NULL
        """;

    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureIndicatori(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<DatiIndicatori> LeggiAsync(DateTime giorno, CancellationToken ct = default)
    {
        var inizio = giorno.Date;
        var fineGiorno = inizio.AddDays(1);
        var inizioAnno = new DateTime(inizio.Year, 1, 1);

        var vigneti = await LeggiAsync($"""
            SELECT v.IdVigneto, vg.Ente_IdEnte AS IdAzienda, e.RagioneSociale, m.Area1, m.AdministrativeAreaLevel1,
                   m.Area2, m.AdministrativeAreaLevel2, m.Area3, m.AdministrativeAreaLevel3
            FROM dbo.Vigneto v
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
            JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = @TipoEnte AND m.Area3 IS NOT NULL
            """, [], r => r.Intero("IdVigneto") is { } id && r.Intero("IdAzienda") is { } az && r.Testo("Area3") is { } comune
                ? new VignetoIndicatori(id, az, r.Testo("RagioneSociale") ?? $"Azienda {az}", r.Testo("Area1"),
                    r.Testo("AdministrativeAreaLevel1"), r.Testo("Area2"), r.Testo("AdministrativeAreaLevel2"), comune,
                    r.Testo("AdministrativeAreaLevel3"))
                : null, ct);

        var infezioni = (await LeggiAsync($"""
            WITH vv AS ({Vigneti})
            SELECT mi.IdVigneto, mi.IdAvversitaDaTrattare, MAX(mi.PercInfezione) AS Massimo
            FROM vv JOIN dbo.ModelloInfezione mi ON mi.IdVigneto = vv.IdVigneto
            WHERE mi.Data >= @Inizio AND mi.Data < @Fine AND mi.IdAvversitaDaTrattare IN (@Oidio, @Peronospora)
            GROUP BY mi.IdVigneto, mi.IdAvversitaDaTrattare
            """, [Data("@Inizio", inizio), Data("@Fine", fineGiorno)],
            r => r.Intero("IdVigneto") is { } v && r.Intero("IdAvversitaDaTrattare") is { } a && r.Decimale("Massimo") is { } m
                ? new { Chiave = (v, a), m }
                : null, ct)).ToDictionary(x => x.Chiave, x => x.m);

        // Rilievi di peronospora/oidio dell'anno fino al giorno, su foglia o grappolo (vigneti_R_* del portale).
        var controlli = await LeggiAsync($"""
            WITH vv AS ({Vigneti})
            SELECT r.Vigneto_IdVigneto AS IdVigneto, r.TipoRilievo_idTipoRilievo AS Tipo,
                   SUM(CASE WHEN r.PercDiffusione > 0 THEN 1 ELSE 0 END) AS ConInfezione,
                   MAX(r.PercDiffusione) AS MaxDiffusione, MAX(r.[PercIntensità]) AS MaxIntensita
            FROM vv JOIN dbo.Rilievo r ON r.Vigneto_IdVigneto = vv.IdVigneto
            WHERE r.TipoRilievo_idTipoRilievo IN (@Oidio, @Peronospora) AND r.PercDiffusione IS NOT NULL
              AND r.[PercIntensità] IS NOT NULL AND r.InizioRilievo >= @InizioAnno AND r.InizioRilievo < @Fine
              AND r.Organo_idOrgano IN (@Foglia, @Grappolo)
            GROUP BY r.Vigneto_IdVigneto, r.TipoRilievo_idTipoRilievo
            """, [Data("@InizioAnno", inizioAnno), Data("@Fine", fineGiorno), Intero("@Foglia", OrganoFoglia),
                Intero("@Grappolo", OrganoGrappolo)],
            r => r.Intero("IdVigneto") is { } v && r.Intero("Tipo") is { } t
                ? new ControlloVigneto(v, t, r.Intero("ConInfezione") ?? 0, r.Decimale("MaxDiffusione") ?? 0, r.Decimale("MaxIntensita") ?? 0)
                : null, ct);

        var bbchVigneti = (await LeggiAsync($"""
            WITH vv AS ({Vigneti})
            SELECT mb.IdVigneto, MAX(mb.IdBBCH_UltimaSettimana) AS Bbch
            FROM vv JOIN dbo.ModelloBBCH mb ON mb.IdVigneto = vv.IdVigneto
            WHERE mb.Data >= @InizioAnno AND mb.Data < @Fine
            GROUP BY mb.IdVigneto
            """, [Data("@InizioAnno", inizioAnno), Data("@Fine", fineGiorno)],
            r => r.Intero("IdVigneto") is { } v && r.Intero("Bbch") is { } b ? new { v, b } : null, ct))
            .ToDictionary(x => x.v, x => x.b);

        var bbch = await LeggiAsync("SELECT BBCH, Pressione_PERONOSPORA, Pressione_OIDIO FROM dbo.BBCH", [],
            r => r.Intero("BBCH") is { } b ? new PressioneBbch(b, r.Intero("Pressione_PERONOSPORA"), r.Intero("Pressione_OIDIO")) : null, ct);

        // Protezione dal giorno a 7 giorni dopo: media per avversità, giorno e vigneto (modelli_protezione).
        var protezione = await LeggiAsync($"""
            WITH vv AS ({Vigneti})
            SELECT ta.IdAvversitaDaTrattare, CAST(mp.Data AS date) AS Giorno, t.IdVigneto, AVG(mp.PercProtezione) AS Media
            FROM vv
            JOIN dbo.Trattamento t ON t.IdVigneto = vv.IdVigneto
            JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
            JOIN dbo.ModelloProtezione mp ON mp.IdTrattamentoAvversita = ta.Id
            WHERE mp.Data >= @Inizio AND mp.Data < @FineProtezione
            GROUP BY ta.IdAvversitaDaTrattare, CAST(mp.Data AS date), t.IdVigneto
            """, [Data("@Inizio", inizio), Data("@FineProtezione", inizio.AddDays(8))],
            r => r.Intero("IdAvversitaDaTrattare") is { } a && r.Valore("Giorno") is DateTime g && r.Intero("IdVigneto") is { } v
                ? new ProtezioneVigneto(g, v, a, r.Decimale("Media") ?? 0)
                : null, ct);

        return new DatiIndicatori(vigneti, infezioni, controlli, bbchVigneti, bbch, protezione);
    }

    public async Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default) =>
        (await LeggiAsync("SELECT IdVigneto FROM dbo.VistaUtentiVigneti WHERE idUtente = @IdUtente",
            [Intero("@IdUtente", idUtente)], r => r.Intero("IdVigneto"), ct)).OfType<int>().ToHashSet();

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private static SqlParameter Data(string nome, DateTime valore) => new(nome, SqlDbType.DateTime2) { Value = valore };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        comando.Parameters.Add(Intero("@TipoEnte", TipoEnteAziendaVitivinicola));
        comando.Parameters.Add(Intero("@Oidio", CalcoloIndicatori.Oidio));
        comando.Parameters.Add(Intero("@Peronospora", CalcoloIndicatori.Peronospora));
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        await using var reader = await comando.ExecuteReaderAsync(ct);
        var risultati = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            if (riga(reader) is { } valore)
            {
                risultati.Add(valore);
            }
        }

        return risultati;
    }
}
