using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un destinatario del bollettino: <c>Utente.LoginName</c> (l'email) e <c>Utente.Lingua</c>.</summary>
public sealed record DestinatarioBollettino(string Email, string Lingua);

/// <summary>
/// Quello che i bollettini leggono da 4Grapes per un ente e un giorno (tutto concesso, dba 16/11, sotto-task #282): trattamenti di
/// gruppo del giorno, nomi, meteo dei comuni, probabilità d'infezione massima e protezione media per avversità.
/// </summary>
public sealed record LettiBollettino(
    string Azienda,
    string? Linea,
    IReadOnlyList<TrattamentoBollettino> TrattamentiIt,
    IReadOnlyList<TrattamentoBollettino> TrattamentiEn,
    IReadOnlyList<MeteoComuneBollettino> Meteo,
    IReadOnlyDictionary<int, decimal> Infezione,
    IReadOnlyDictionary<int, decimal> Protezione,
    IReadOnlyList<(int Id, string Nome)> Avversita);

public interface ILettureBollettini
{
    /// <returns>null se l'ente non c'è.</returns>
    Task<LettiBollettino?> LeggiAsync(int ente, int tipo, int? linea, DateTime giorno, CancellationToken ct = default);

    /// <summary>Il numero più alto per ente, tipo e anno di <c>GiornoRif</c> (0 se nessuno).</summary>
    Task<int> UltimoNumeroAsync(int ente, int tipo, int anno, CancellationToken ct = default);

    /// <summary><c>Utente_Ente.AbilitaInvioBollettini = 1</c> e utente attivo (<c>StatoEntita = 1</c>), come il portale.</summary>
    Task<IReadOnlyList<DestinatarioBollettino>> DestinatariAsync(int ente, CancellationToken ct = default);

    /// <summary>Gli ultimi 300 bollettini (di quelle aziende, null = tutte), dal più recente.</summary>
    Task<IReadOnlyList<BollettinoArchivioDto>> ArchivioAsync(IReadOnlyCollection<int>? aziende, CancellationToken ct = default);
}

/// <summary>Senza 4Grapes: niente.</summary>
public class InMemoryLettureBollettini : ILettureBollettini
{
    public Task<LettiBollettino?> LeggiAsync(int ente, int tipo, int? linea, DateTime giorno, CancellationToken ct = default) =>
        Task.FromResult<LettiBollettino?>(null);

    public Task<int> UltimoNumeroAsync(int ente, int tipo, int anno, CancellationToken ct = default) => Task.FromResult(0);

    public Task<IReadOnlyList<DestinatarioBollettino>> DestinatariAsync(int ente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DestinatarioBollettino>>([]);

    public Task<IReadOnlyList<BollettinoArchivioDto>> ArchivioAsync(IReadOnlyCollection<int>? aziende, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BollettinoArchivioDto>>([]);
}

/// <summary>Le query di <c>BollettiniTrattamentiController</c> del portale, con la connessione di sola lettura.</summary>
public class FourGrapesLettureBollettini : ILettureBollettini
{
    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureBollettini(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<LettiBollettino?> LeggiAsync(int ente, int tipo, int? linea, DateTime giorno, CancellationToken ct = default)
    {
        giorno = giorno.Date;
        var nomi = await LeggiAsync("""
            SELECT e.RagioneSociale, (SELECT tl.Descrizione FROM dbo.TipologiaLinea tl WHERE tl.Id = @Linea) AS Linea
            FROM dbo.Ente e WHERE e.IdEnte = @Ente
            """, [Intero("@Ente", ente), IntOpz("@Linea", linea)], r => (r.Testo("RagioneSociale") ?? $"Azienda {ente}", r.Testo("Linea")), ct);
        if (nomi.Count == 0) return null;

        // Trattamenti di gruppo del giorno: per il 100 della linea (o senza linea di gruppo) sui vigneti di quella linea.
        var filtroLinea = tipo == GeneratoreBollettini.TipoPeronosporaOidio
            ? "AND (t.Gruppo_IdTipologiaLinea IS NULL OR t.Gruppo_IdTipologiaLinea = @Linea) AND v.IdTipologiaLinea = @Linea"
            : "";
        var testate = await LeggiAsync($"""
            SELECT t.Id, t.IdVigneto, vg.Vigna, v.NomeVigneto, v.Superficie, t.Gruppo_IdRischio, t.Gruppo_Esclusi, t.IdBBCH, t.Note,
                   tr.Descrizione AS Transito, tr.Descrizione_EN AS TransitoEn
            FROM dbo.Trattamento t
            JOIN dbo.Vigneto v ON v.IdVigneto = t.IdVigneto
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            LEFT JOIN dbo.TransitoNelVigneto tr ON tr.Id = t.IdTransitoNelVigneto
            WHERE t.Data >= @Giorno AND t.Data < @Dopo AND t.Gruppo_IdEnte = @Ente AND (t.Suggerito = 1 OR t.Effettivo = 1)
              AND t.IdTipoTrattamento = @Tipo {filtroLinea}
            ORDER BY t.Id
            """, [Data("@Giorno", giorno), Data("@Dopo", giorno.AddDays(1)), Intero("@Ente", ente), Intero("@Tipo", tipo), IntOpz("@Linea", linea)],
            r => r.Intero("Id") is { } id
                ? new
                {
                    T = new TrattamentoBollettino(id, r.Intero("IdVigneto") ?? 0, r.Testo("Vigna"), r.Testo("NomeVigneto"), r.Decimale("Superficie"),
                        r.Intero("Gruppo_IdRischio"), r.Valore("Gruppo_Esclusi") as bool?, r.Intero("IdBBCH") ?? 0, r.Valore("Note") as string,
                        r.Testo("Transito"), []),
                    En = r.Testo("TransitoEn"),
                }
                : null, ct);

        var prodotti = new Dictionary<int, List<ProdottoBollettino>>();
        if (testate.Count > 0)
        {
            var parametri = testate.Select((t, i) => Intero($"@T{i}", t.T.Id)).ToList();
            foreach (var p in await LeggiAsync($"""
                SELECT ta.IdTrattamento, ta.IdAvversitaDaTrattare, ta.NumRegProdottoVite, ag.NOME_COMMERCIALE, ta.Dosaggio
                FROM dbo.TrattamentoAvversita ta
                LEFT JOIN dbo.AgrofarmaciUnivoco ag ON ag.NUM_REG = ta.NumRegProdottoVite
                WHERE ta.IdTrattamento IN ({string.Join(", ", parametri.Select(x => x.ParameterName))})
                ORDER BY ta.Id
                """, parametri, r => r.Intero("IdTrattamento") is { } t
                    ? (t, new ProdottoBollettino(r.Intero("IdAvversitaDaTrattare") ?? 0, r.Testo("NOME_COMMERCIALE") ?? r.Testo("NumRegProdottoVite") ?? "",
                        r.Decimale("Dosaggio") ?? 0))
                    : ((int, ProdottoBollettino)?)null, ct))
            {
                var (t, prodotto) = p!.Value;
                (prodotti.TryGetValue(t, out var l) ? l : prodotti[t] = []).Add(prodotto);
            }
        }

        var it = testate.Select(x => x.T with { Prodotti = prodotti.GetValueOrDefault(x.T.Id) ?? [] }).ToList();
        var en = testate.Select((x, i) => it[i] with { Transito = x.En }).ToList();

        var infezione = (await LeggiAsync("""
            SELECT mi.IdAvversitaDaTrattare, MAX(mi.PercInfezione) AS Massimo
            FROM dbo.Vigneto v
            JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
            JOIN dbo.ModelloInfezione mi ON mi.IdVigneto = v.IdVigneto
            WHERE vg.Ente_IdEnte = @Ente AND mi.Data >= @Giorno AND mi.Data < @Dopo
            GROUP BY mi.IdAvversitaDaTrattare
            """, [Intero("@Ente", ente), Data("@Giorno", giorno), Data("@Dopo", giorno.AddDays(1))],
            r => r.Intero("IdAvversitaDaTrattare") is { } a && r.Decimale("Massimo") is { } m ? (a, m) : ((int, decimal)?)null, ct))
            .ToDictionary(x => x!.Value.Item1, x => x!.Value.Item2);

        // Protezione: media per vigneto e avversità, poi media per avversità, dei suggeriti creati prima delle 23 del giorno prima.
        var protezione = (await LeggiAsync("""
            WITH pv AS (
                SELECT t.IdVigneto, ta.IdAvversitaDaTrattare, AVG(mp.PercProtezione) AS Media
                FROM dbo.Vigneto v
                JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
                JOIN dbo.Trattamento t ON t.IdVigneto = v.IdVigneto
                JOIN dbo.TrattamentoAvversita ta ON ta.IdTrattamento = t.Id
                JOIN dbo.ModelloProtezione mp ON mp.IdTrattamentoAvversita = ta.Id
                WHERE vg.Ente_IdEnte = @Ente AND mp.Data >= @Giorno AND mp.Data < @Dopo AND t.DataCreazione < @Prima AND t.Suggerito = 1
                GROUP BY t.IdVigneto, ta.IdAvversitaDaTrattare)
            SELECT IdAvversitaDaTrattare, AVG(Media) AS Media FROM pv GROUP BY IdAvversitaDaTrattare
            """, [Intero("@Ente", ente), Data("@Giorno", giorno), Data("@Dopo", giorno.AddDays(1)), Data("@Prima", giorno.AddHours(-1))],
            r => r.Intero("IdAvversitaDaTrattare") is { } a && r.Decimale("Media") is { } m ? (a, m) : ((int, decimal)?)null, ct))
            .ToDictionary(x => x!.Value.Item1, x => x!.Value.Item2);

        var meteo = await MeteoAsync(ente, giorno, ct);
        var avversita = await LeggiAsync("SELECT Id, Descrizione FROM dbo.AvversitaDaTrattare ORDER BY Id", [],
            r => r.Intero("Id") is { } id ? (id, r.Testo("Descrizione") ?? "") : ((int, string)?)null, ct);

        return new LettiBollettino(nomi[0].Item1, nomi[0].Item2, it, en, meteo, infezione, protezione,
            avversita.Select(a => a!.Value).ToList());
    }

    /// <summary>
    /// <c>GetDatiMeteoComuni</c>: comuni dei vigneti in carico dell'ente; meteo di tutti i vigneti di quei comuni, ultimi 7 giorni e
    /// prossimi 6; per vigneto giorni con pioggia (<c>Precipitazione1 &gt; 0</c>), mm sommati, temperature medie; per comune la media.
    /// </summary>
    private async Task<IReadOnlyList<MeteoComuneBollettino>> MeteoAsync(int ente, DateTime giorno, CancellationToken ct)
    {
        var righe = await LeggiAsync("""
            WITH comuni AS (
                SELECT DISTINCT m.Area3, m.AdministrativeAreaLevel3 AS Nome
                FROM dbo.VignetoMaps m
                JOIN dbo.Vigneto v ON v.IdVigneto = m.IdVigneto
                JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
                WHERE vg.Ente_IdEnte = @Ente AND v.Carico = 1 AND v.Estirpo = 0 AND m.Area3 IS NOT NULL),
            pv AS (
                SELECT mg.IdVigneto, m.Area3, CASE WHEN mg.Data < @Giorno THEN 0 ELSE 1 END AS Prossimi,
                       SUM(CASE WHEN mg.Precipitazione1 > 0 THEN 1 ELSE 0 END) AS Gg, SUM(CAST(mg.Precipitazione AS decimal(18, 4))) AS Mm,
                       AVG(CAST(mg.TemperaturaMin AS decimal(18, 4))) AS TMin, AVG(CAST(mg.TemperaturaMed AS decimal(18, 4))) AS TMed,
                       AVG(CAST(mg.TemperaturaMax AS decimal(18, 4))) AS TMax
                FROM dbo.MeteoGiornaliero mg
                JOIN dbo.VignetoMaps m ON m.IdVigneto = mg.IdVigneto
                WHERE mg.Data >= @Da AND mg.Data < @A AND m.Area3 IN (SELECT Area3 FROM comuni)
                GROUP BY mg.IdVigneto, m.Area3, CASE WHEN mg.Data < @Giorno THEN 0 ELSE 1 END)
            SELECT c.Area3, c.Nome, pv.Prossimi, AVG(CAST(pv.Gg AS decimal(18, 4))) AS Gg, AVG(pv.Mm) AS Mm, AVG(pv.TMin) AS TMin,
                   AVG(pv.TMed) AS TMed, AVG(pv.TMax) AS TMax
            FROM comuni c JOIN pv ON pv.Area3 = c.Area3
            GROUP BY c.Area3, c.Nome, pv.Prossimi
            """, [Intero("@Ente", ente), Data("@Giorno", giorno), Data("@Da", giorno.AddDays(-7)), Data("@A", giorno.AddDays(6))],
            r => r.Testo("Area3") is { } area
                ? new { Area = area, Nome = r.Testo("Nome") ?? "", Prossimi = r.Intero("Prossimi") == 1, Gg = r.Decimale("Gg") ?? 0,
                    Mm = r.Decimale("Mm") ?? 0, TMin = r.Decimale("TMin") ?? 0, TMed = r.Decimale("TMed") ?? 0, TMax = r.Decimale("TMax") ?? 0 }
                : null, ct);
        return CalcoloBollettini.MeteoComuni(righe.Select(x => (x!.Area, x.Nome, x.Prossimi, x.Gg, x.Mm, x.TMin, x.TMed, x.TMax)));
    }

    public async Task<int> UltimoNumeroAsync(int ente, int tipo, int anno, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT MAX(NumeroBollettino) AS Numero FROM dbo.Bollettino
            WHERE IdEnte = @Ente AND IdTipoBollettino = @Tipo AND GiornoRif >= @Inizio AND GiornoRif < @Fine
            """, [Intero("@Ente", ente), Intero("@Tipo", tipo), Data("@Inizio", new DateTime(anno, 1, 1)), Data("@Fine", new DateTime(anno + 1, 1, 1))],
            r => (int?)(r.Intero("Numero") ?? 0), ct)).FirstOrDefault() ?? 0;

    public Task<IReadOnlyList<DestinatarioBollettino>> DestinatariAsync(int ente, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT u.LoginName, u.Lingua
            FROM dbo.Utente_Ente ue JOIN dbo.Utente u ON u.idUtente = ue.Utente_idUtente
            WHERE ue.Ente_IdEnte = @Ente AND ue.AbilitaInvioBollettini = 1 AND u.StatoEntita = 1
            ORDER BY u.LoginName
            """, [Intero("@Ente", ente)],
            r => r.Testo("LoginName")?.Trim() is { Length: > 0 } e ? new DestinatarioBollettino(e, r.Testo("Lingua")?.Trim() ?? "IT") : null, ct);

    public async Task<IReadOnlyList<BollettinoArchivioDto>> ArchivioAsync(IReadOnlyCollection<int>? aziende, CancellationToken ct = default)
    {
        if (aziende is { Count: 0 }) return [];
        var parametri = (aziende ?? []).Select((a, i) => Intero($"@A{i}", a)).ToList();
        var filtro = aziende is null ? "" : $"WHERE b.IdEnte IN ({string.Join(", ", parametri.Select(p => p.ParameterName))})";
        return await LeggiAsync($"""
            SELECT TOP 300 b.IdBollettino, b.IdEnte, e.RagioneSociale, b.IdTipoBollettino, b.NumeroBollettino, b.GiornoRif, tl.Descrizione AS Linea,
                   b.DataInvio, u.Nome, u.Cognome
            FROM dbo.Bollettino b
            JOIN dbo.Ente e ON e.IdEnte = b.IdEnte
            LEFT JOIN dbo.TipologiaLinea tl ON tl.Id = b.IdTipologiaLinea
            LEFT JOIN dbo.Utente u ON u.idUtente = b.IdUtente
            {filtro}
            ORDER BY b.IdBollettino DESC
            """, parametri,
            r => r.Intero("IdBollettino") is { } id && r.Valore("GiornoRif") is DateTime g
                ? new BollettinoArchivioDto(id, r.Intero("IdEnte") ?? 0, r.Testo("RagioneSociale") ?? "", r.Intero("IdTipoBollettino") ?? 0,
                    r.Intero("NumeroBollettino") ?? 0, g, r.Testo("Linea"), r.Valore("DataInvio") as DateTime?,
                    $"{r.Testo("Nome")} {r.Testo("Cognome")}".Trim(), false)
                : null, ct);
    }

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private static SqlParameter IntOpz(string nome, int? valore) => new(nome, SqlDbType.Int) { Value = (object?)valore ?? DBNull.Value };

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

/// <summary>Le parti dei bollettini che non leggono il database: calendario, meteo per comune, livelli, archivio su file.</summary>
public static class CalcoloBollettini
{
    public const int Colonne = 7;

    /// <summary>
    /// <c>BollettiniAziendaliGiornalieri</c>: gli enti dei vigneti filtrati (stessi filtri dei trattamenti), 7 giorni dal − 3 al
    /// + 3; per giorno un pulsante per ogni linea dei vigneti con trattamenti peronospora/oidio e «ALTRE AVV.» se ci sono trattamenti
    /// delle altre avversità; suggeriti già trasformati in effettivi nascosti. <paramref name="tipo"/> 100/200 = solo quel tipo.
    /// </summary>
    public static BollettiniGiornalieriDto Calendario(FiltriTrattamenti f, int tipo, DatiTrattamenti dati, IReadOnlySet<int>? vignetiUtente)
    {
        var vigneti = CalcoloTrattamenti.Filtra(f, dati, vignetiUtente);
        var perId = vigneti.ToDictionary(v => v.IdVigneto);
        var dal = CalcoloTrattamenti.Dal(f.Data);
        var trattamenti = dati.Trattamenti
            .Where(t => t.Data >= dal && perId.ContainsKey(t.IdVigneto) && (t.Suggerito || t.Effettivo))
            .Where(t => tipo == 0 || t.IdTipoTrattamento == tipo).ToList();
        var trasformati = trattamenti.Where(t => t.GeneratoDa is not null).Select(t => t.GeneratoDa!.Value).ToHashSet();
        trattamenti = trattamenti.Where(t => !trasformati.Contains(t.Id)).ToList();
        var giorni = Enumerable.Range(0, Colonne).Select(i => DateOnly.FromDateTime(dal).AddDays(i)).ToList();

        var righe = vigneti.GroupBy(v => v.IdAzienda).Select(a =>
        {
            var ids = a.Select(v => v.IdVigneto).ToHashSet();
            var suoi = trattamenti.Where(t => ids.Contains(t.IdVigneto)).ToList();
            var celle = giorni.Select(g =>
            {
                var delGiorno = suoi.Where(t => DateOnly.FromDateTime(t.Data) == g).ToList();
                var pulsanti = delGiorno.Where(t => t.IdTipoTrattamento == CalcoloTrattamenti.PeronosporaOidio)
                    .Select(t => perId[t.IdVigneto]).DistinctBy(v => v.IdLinea)
                    .Select(v => new PulsanteBollettinoDto(CalcoloTrattamenti.PeronosporaOidio, v.IdLinea, v.Linea ?? $"Linea {v.IdLinea}"))
                    .ToList();
                if (delGiorno.Any(t => t.IdTipoTrattamento == CalcoloTrattamenti.AltreAvversita))
                    pulsanti.Add(new PulsanteBollettinoDto(CalcoloTrattamenti.AltreAvversita, null, "ALTRE AVV."));
                return (IReadOnlyList<PulsanteBollettinoDto>)pulsanti;
            }).ToList();
            return new RigaBollettiniDto(a.Key, a.First().Azienda, celle);
        }).OrderBy(r => r.Nome, StringComparer.Create(GeneratoreBollettini.Cultura, false)).ToList();

        var filtri = CalcoloTrattamenti.Calcola(f with { TipoTrattamento = 0 }, dati, vignetiUtente).Filtri;
        return new BollettiniGiornalieriDto(f.Data, DateOnly.FromDateTime(dal), f.Data.AddDays(CalcoloTrattamenti.GiorniPrima), giorni,
            righe, filtri);
    }

    /// <summary>Le righe per comune (ultimi = 0, prossimi = 1) diventano un comune; nessun dato = comune saltato; ordine per nome.</summary>
    public static IReadOnlyList<MeteoComuneBollettino> MeteoComuni(
        IEnumerable<(string Area, string Nome, bool Prossimi, decimal Gg, decimal Mm, decimal TMin, decimal TMed, decimal TMax)> righe) =>
        righe.GroupBy(r => (r.Area, r.Nome)).Select(c =>
        {
            var u = c.FirstOrDefault(r => !r.Prossimi);
            var p = c.FirstOrDefault(r => r.Prossimi);
            // (int) del portale: la media dei giorni di pioggia si tronca.
            return new MeteoComuneBollettino(c.Key.Nome, (int)u.Gg, u.Mm, u.TMin, u.TMed, u.TMax, (int)p.Gg, p.Mm, p.TMin, p.TMed, p.TMax);
        }).OrderBy(m => m.Nome, StringComparer.Ordinal).ToList();

    /// <summary><c>Ente.GetTestoPressioneDaValore</c> (testi in maiuscolo, scritti da noi: le traduzioni del portale non le abbiamo).</summary>
    public static string TestoPressione(decimal? valore, string lingua) => (valore, lingua == "IT") switch
    {
        (null or <= 0, var it) => it ? "NULLA" : "NONE",
        (< 5, var it) => it ? "MOLTO BASSA" : "VERY LOW",
        (< 10, var it) => it ? "BASSA" : "LOW",
        (< 20, var it) => it ? "MEDIA" : "MEDIUM",
        (< 40, var it) => it ? "ALTA" : "HIGH",
        (< 70, var it) => it ? "MOLTO ALTA" : "VERY HIGH",
        (_, var it) => it ? "ELEVATA" : "EXTREME",
    };

    /// <summary><c>ModelloInfezione.GetTestoDaValore</c>.</summary>
    public static string TestoProbabilita(decimal? valore, string lingua) => (valore, lingua == "IT") switch
    {
        (null, _) => "-",
        (< 2.5m, var it) => it ? "MOLTO BASSO" : "VERY LOW",
        (< 10m, var it) => it ? "BASSO" : "LOW",
        (< 50m, var it) => it ? "MEDIO" : "MEDIUM",
        (< 90m, var it) => it ? "ALTO" : "HIGH",
        (_, var it) => it ? "MOLTO ALTO" : "VERY HIGH",
    };

    /// <summary><c>ModelloProtezione.GetTestoDaValore</c> (nessuna / scarsa &lt; 70 / media ≤ 80 / buona).</summary>
    public static string TestoProtezione(decimal? valore, string lingua) => (valore, lingua == "IT") switch
    {
        (null or <= 0, var it) => it ? "NESSUNA" : "NONE",
        (< 70m, var it) => it ? "SCARSA" : "POOR",
        (<= 80m, var it) => it ? "MEDIA" : "MEDIUM",
        (_, var it) => it ? "BUONA" : "GOOD",
    };

    /// <summary>Il file della copia in archivio: <c>{cartella}/{ente}/{anno}/{tipo}-{numero}-{giorno}-{id}.{lingua}.html</c>.</summary>
    public static string FileArchivio(string cartella, int ente, int tipo, int numero, DateTime giorno, int id, string lingua) =>
        Path.Combine(cartella, ente.ToString(CultureInfo.InvariantCulture), giorno.Year.ToString(CultureInfo.InvariantCulture),
            $"{tipo}-{numero:D3}-{giorno:yyyyMMdd}-{id}.{lingua}.html");
}
