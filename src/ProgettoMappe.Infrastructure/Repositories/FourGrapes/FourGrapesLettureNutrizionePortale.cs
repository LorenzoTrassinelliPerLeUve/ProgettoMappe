using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Letture dei rilievi della nutrizione del Portale (vedi <see cref="ILettureNutrizionePortale"/>).</summary>
public class FourGrapesLettureNutrizionePortale : ILettureNutrizionePortale
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureNutrizionePortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    /// <summary>Le colonne delle analisi dei terreni che stanno in <c>Tessitura</c>, non in <c>Rilievo</c>.</summary>
    private static readonly string[] ColonneTessitura = ["Sabbia", "Limo", "Argilla"];

    public Task<IReadOnlyList<RilievoDto>> RilieviAsync(string? vista, int tipo, IReadOnlyList<string> colonne,
        IReadOnlyList<int>? soloEnti, CancellationToken ct = default)
    {
        if (vista is not null && !Scrittura.TabellaScrivibile.NomeValido(vista) || !colonne.All(Scrittura.TabellaScrivibile.NomeValido))
        {
            throw new ArgumentException("Nome di vista o di colonna non valido");
        }

        var (filtro, parametri) = FiltroEnti(vista is null ? "g.Ente_IdEnte" : "IdEnte", soloEnti);
        if (filtro is null)
        {
            return Task.FromResult<IReadOnlyList<RilievoDto>>([]);
        }

        string sql;
        if (vista is null)
        {
            // Come ListaRilieviAjax di TessituraController: Rilievo del tipo con vigneto, vigna, azienda.
            var altre = string.Concat(colonne.Select(c => ColonneTessitura.Contains(c) ? $", t.[{c}]" : $", r.[{c}]"));
            parametri.Add(Intero("@Tipo", tipo));
            var dove = filtro.Length == 0 ? " WHERE r.TipoRilievo_idTipoRilievo = @Tipo" : $"{filtro} AND r.TipoRilievo_idTipoRilievo = @Tipo";
            sql = $"""
                SELECT r.IdRilievo, r.InizioRilievo, YEAR(r.InizioRilievo) AS Anno, g.Ente_IdEnte AS IdEnte, e.RagioneSociale,
                       g.IdVigna, g.Vigna, v.IdVigneto, v.NomeVigneto, v.Vitigno_idVitigno AS IdVitigno, w.Vitigno{altre}
                FROM dbo.Rilievo r
                JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
                JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
                LEFT JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte
                LEFT JOIN dbo.Vitigno w ON w.idVitigno = v.Vitigno_idVitigno
                LEFT JOIN dbo.Tessitura t ON t.IdTessitura = r.IdTessitura{dove}
                ORDER BY r.InizioRilievo
                """;
        }
        else
        {
            var altre = string.Concat(colonne.Select(c => $", [{c}]"));
            sql = $"""
                SELECT IdRilievo, InizioRilievo, Anno, IdEnte, RagioneSociale, IdVigna, Vigna, IdVigneto, NomeVigneto, IdVitigno,
                       Vitigno{altre}
                FROM dbo.[{vista}]{filtro}
                ORDER BY InizioRilievo
                """;
        }

        return LeggiAsync(sql, parametri,
            r => r.Valore("IdRilievo") is DateTime id && r.Valore("InizioRilievo") is DateTime inizio
                ? new RilievoDto(id.Ticks, inizio, r.Intero("Anno"), r.Intero("IdEnte"), r.Testo("RagioneSociale"), r.Intero("IdVigna"),
                    r.Testo("Vigna"), r.Intero("IdVigneto"), r.Testo("NomeVigneto"), r.Intero("IdVitigno"), r.Testo("Vitigno"),
                    colonne.ToDictionary(c => c, c => Testo(r.Valore(c))), Modificabile: false)
                : null, ct);
    }

    /// <summary>Testo invariante del valore (date «yyyy-MM-dd»), come lo legge il Web.</summary>
    private static string? Testo(object? valore) => valore switch
    {
        null => null,
        DateTime d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => valore.ToString(),
    };

    public Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT g.IdVigna, g.Vigna, v.IdVigneto, v.NomeVigneto, v.Vitigno_idVitigno, t.Vitigno
            FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            LEFT JOIN dbo.Vitigno t ON t.idVitigno = v.Vitigno_idVitigno
            WHERE g.Ente_IdEnte = @IdEnte AND g.IdVigna > 0 AND v.IdVigneto > 0
            ORDER BY g.Vigna, v.NomeVigneto
            """, [Intero("@IdEnte", azienda)],
            r => r.Intero("IdVigna") is { } vigna && r.Intero("IdVigneto") is { } vigneto
                ? new VignetoMenuDto(vigna, r.Testo("Vigna") ?? $"Vigna {vigna}", vigneto, r.Testo("NomeVigneto") ?? $"Vigneto {vigneto}",
                    r.Intero("Vitigno_idVitigno"), r.Testo("Vitigno"))
                : null, ct);

    public async Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT v.IdVigneto, v.Vigna_IdVigna, g.Ente_IdEnte, v.Vitigno_idVitigno, v.Latitudine, v.Longitudine, v.Conduzione_idConduzione
            FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            WHERE v.IdVigneto > 0 AND v.IdVigneto = @IdVigneto
            """, [Intero("@IdVigneto", idVigneto)],
            r => r.Intero("IdVigneto") is { } id && r.Intero("Vigna_IdVigna") is { } vigna && r.Intero("Ente_IdEnte") is { } ente
                ? new VignetoRilievo(id, vigna, ente, r.Intero("Vitigno_idVitigno"), r.Decimale("Latitudine"), r.Decimale("Longitudine"),
                    r.Intero("Conduzione_idConduzione"))
                : null, ct)).FirstOrDefault();

    public async Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT r.IdRilievo, g.Ente_IdEnte, r.InizioRilievo
            FROM dbo.Rilievo r LEFT JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
            LEFT JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            WHERE r.IdRilievo = @IdRilievo AND r.TipoRilievo_idTipoRilievo = @Tipo
            """, [new SqlParameter("@IdRilievo", SqlDbType.DateTime2) { Value = idRilievo }, Intero("@Tipo", tipo)],
            r => r.Valore("IdRilievo") is DateTime id && r.Valore("InizioRilievo") is DateTime inizio
                ? new RilievoEsistente(id, r.Intero("Ente_IdEnte"), inizio)
                : null, ct)).FirstOrDefault();

    public async Task<(int IdTessitura, int IdTessituraSuolo)?> TessituraAsync(int sabbia, int limo, int argilla,
        CancellationToken ct = default)
    {
        var righe = await LeggiAsync("""
            SELECT TOP 1 IdTessitura, IdTessituraSuolo FROM dbo.Tessitura
            WHERE Sabbia = @Sabbia AND Limo = @Limo AND Argilla = @Argilla
            ORDER BY IdTessitura
            """, [Intero("@Sabbia", sabbia), Intero("@Limo", limo), Intero("@Argilla", argilla)],
            r => r.Intero("IdTessitura") is { } t && r.Intero("IdTessituraSuolo") is { } s ? new[] { t, s } : null, ct);
        return righe.Count == 0 ? null : (righe[0][0], righe[0][1]);
    }

    public Task<IReadOnlyList<SottovignetoMenuDto>> SottovignetiAsync(int idVigneto, CancellationToken ct = default) =>
        LeggiAsync("SELECT IdSubVigneto, Nome FROM dbo.SubVigneto WHERE IdSubVigneto > 0 AND IdVigneto = @IdVigneto ORDER BY Nome",
            [Intero("@IdVigneto", idVigneto)],
            r => r.Intero("IdSubVigneto") is { } id ? new SottovignetoMenuDto(id, r.Testo("Nome") ?? $"Sottovigneto {id}") : null, ct);

    public Task<IReadOnlyList<ObiettivoRigaDto>> ObiettiviAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default)
    {
        var (filtro, parametri) = FiltroEnti("IdEnte", soloEnti);
        if (filtro is null)
        {
            return Task.FromResult<IReadOnlyList<ObiettivoRigaDto>>([]);
        }

        return LeggiAsync($"""
            SELECT IdVigneto, NomeVigneto, IdVigna, Vigna, IdEnte, RagioneSociale, idVitigno, Vitigno, IdTarget, TargetEnologico,
                   AnnoUltimo, ObiettivoProduttivoVigneto, ObiettivoProduttivoPerPianta, ObiettivoProduttivoPerEttaro,
                   ProduzioneUltimoAnnoVigneto, ProduzionePerPiantaUltimoAnno, ProduzioneUltimoAnnoPerEttaro,
                   ProduzioneMediaAnnuaVigneto, ProduzioneMediaAnnuaPerPianta, ProduzioneMediaAnnuaPerEttaro, PercFallanze,
                   PercInAllevamento
            FROM dbo.VistaVignetiConProduzioni{filtro}
            ORDER BY Vigna, NomeVigneto
            """, parametri,
            r => r.Intero("IdVigneto") is { } id
                ? new ObiettivoRigaDto(id, r.Testo("NomeVigneto"), r.Intero("IdVigna"), r.Testo("Vigna"), r.Intero("IdEnte"),
                    r.Testo("RagioneSociale"), r.Intero("idVitigno"), r.Testo("Vitigno"), r.Intero("IdTarget"), r.Testo("TargetEnologico"),
                    r.Intero("AnnoUltimo"), r.Decimale("ObiettivoProduttivoVigneto"), r.Decimale("ObiettivoProduttivoPerPianta"),
                    r.Decimale("ObiettivoProduttivoPerEttaro"), r.Decimale("ProduzioneUltimoAnnoVigneto"),
                    r.Decimale("ProduzionePerPiantaUltimoAnno"), r.Decimale("ProduzioneUltimoAnnoPerEttaro"),
                    r.Decimale("ProduzioneMediaAnnuaVigneto"), r.Decimale("ProduzioneMediaAnnuaPerPianta"),
                    r.Decimale("ProduzioneMediaAnnuaPerEttaro"), r.Decimale("PercFallanze"), r.Decimale("PercInAllevamento"), false)
                : null, ct);
    }

    public async Task<ObiettivoEsistente?> ObiettivoAsync(int idVigneto, int anno, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT TOP 1 v.IdVigneto, g.Ente_IdEnte, v.NumeroPiante, v.Superficie, t.IdTargetProduttivo,
                   t.ObiettivoProduttivoVigneto, t.ObiettivoProduttivoPerPianta, t.ObiettivoProduttivoPerEttaro
            FROM dbo.Vigneto v JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            LEFT JOIN dbo.TargetProduttivo t ON t.IdVigneto = v.IdVigneto AND t.Anno = @Anno
            WHERE v.IdVigneto = @IdVigneto
            ORDER BY t.IdTargetProduttivo
            """, [Intero("@IdVigneto", idVigneto), Intero("@Anno", anno)],
            r => r.Intero("IdVigneto") is { } id && r.Intero("Ente_IdEnte") is { } ente
                ? new ObiettivoEsistente(r.Intero("IdTargetProduttivo"), ente, new ObiettivoDto(id, anno,
                    r.Decimale("ObiettivoProduttivoVigneto"), r.Decimale("ObiettivoProduttivoPerPianta"),
                    r.Decimale("ObiettivoProduttivoPerEttaro"), r.Decimale("NumeroPiante"), r.Decimale("Superficie")))
                : null, ct)).FirstOrDefault();

    /// <summary>« WHERE colonna IN (…)» con i parametri; null = nessuna azienda (lista vuota), "" = tutte.</summary>
    private static (string? Filtro, List<SqlParameter> Parametri) FiltroEnti(string colonna, IReadOnlyList<int>? soloEnti)
    {
        if (soloEnti is null) return ("", []);
        if (soloEnti.Count == 0) return (null, []);
        return ($" WHERE {colonna} IN ({string.Join(", ", soloEnti.Select((_, i) => $"@E{i}"))})",
            soloEnti.Select((id, i) => Intero($"@E{i}", id)).ToList());
    }

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct) where T : class
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
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
