using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

/// <summary>Sezione <c>ReportFineStagione</c> dell'Api.</summary>
public sealed class ReportFineStagioneOptions
{
    public const string Sezione = "ReportFineStagione";

    /// <summary>
    /// Snapshot SQLite del dba (task #200, rifatto ogni notte alle 3:10), che l'Api legge soltanto. Più percorsi
    /// separati da <c>;</c>: si usa il primo che esiste. Vuoto o nessun file = meteo da 4Grapes senza storico.
    /// </summary>
    public string Snapshot { get; set; } = "";
}

/// <summary>
/// Lettore dello snapshot del report fine stagione (task #201): <c>rfs.sqlite</c> del dba con le fonti del report
/// già filtrate sui vigneti di Qlik (Estirpo = 0, Carico = 1, vigna visibile, gruppi 1, 3, 4, 24, 34) e la vista
/// <c>Meteo</c> = storico di <c>4g_Storico</c> + anno in corso. Date in testo ISO, bit in 0/1. Sola lettura.
/// </summary>
public sealed class SnapshotReportFineStagione
{
    private readonly string[] _percorsi;
    private readonly ILogger<SnapshotReportFineStagione> _logger;

    public SnapshotReportFineStagione(IOptions<ReportFineStagioneOptions> opzioni, ILogger<SnapshotReportFineStagione> logger)
    {
        _logger = logger;
        _percorsi = opzioni.Value.Snapshot.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(LettoreNdvi.Espandi).ToArray();
    }

    /// <summary>Il file in uso: il primo che esiste; null = nessuno snapshot.</summary>
    public string? Percorso => _percorsi.FirstOrDefault(File.Exists);

    /// <summary>Meteo dei vigneti dell'azienda; null se lo snapshot non c'è o non si legge.</summary>
    public async Task<MeteoReportLetto?> LeggiMeteoAsync(int aziendaId, CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            var vigneti = new List<VignetoMeteoReport>();
            await using (var comando = Comando(connessione, """
                SELECT v.IdVigneto, v.NomeVigneto, v.IdVigna, g.Vigna, v.DatiMeteoStorico
                FROM Vigneto v LEFT JOIN Vigna g ON g.IdVigna = v.IdVigna
                WHERE v.IdEnte = $azienda
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    var id = r.GetInt32(0);
                    var nome = r.IsDBNull(1) ? "" : r.GetString(1).Trim();
                    vigneti.Add(new VignetoMeteoReport(id, nome.Length > 0 ? nome : $"Vigneto {id}",
                        r.IsDBNull(2) ? null : r.GetInt32(2), r.IsDBNull(3) ? null : r.GetString(3).Trim(),
                        !r.IsDBNull(4) && r.GetInt64(4) == 1));
                }
            }

            var giorni = new List<GiornoMeteoReport>();
            await using (var comando = Comando(connessione, """
                SELECT m.IdVigneto, m.Data, m.Previsione, m.Precipitazione, m.TemperaturaMin, m.TemperaturaMed, m.TemperaturaMax
                FROM Meteo m JOIN Vigneto v ON v.IdVigneto = m.IdVigneto
                WHERE v.IdEnte = $azienda
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    if (r.IsDBNull(1) || !DateOnly.TryParse(r.GetString(1)[..Math.Min(10, r.GetString(1).Length)],
                            CultureInfo.InvariantCulture, out var data))
                    {
                        continue;
                    }
                    giorni.Add(new GiornoMeteoReport(r.GetInt32(0), data, !r.IsDBNull(2) && r.GetInt64(2) == 1,
                        Decimale(r, 3), Decimale(r, 4), Decimale(r, 5), Decimale(r, 6)));
                }
            }

            return new MeteoReportLetto(vigneti, giorni, StoricoDisponibile: true, await CostruitoIlAsync(connessione, ct));
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Snapshot del report fine stagione {Percorso} non leggibile: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Suolo (foglio 42) e scottature (fogli 43–44: <c>VistaScottature</c> e rilievi di tipo 32 sui grappoli, organi 2 e 5)
    /// dei vigneti dell'azienda; null se lo snapshot non c'è o non si legge.
    /// </summary>
    public async Task<CampoReportLetto?> LeggiCampoAsync(int aziendaId, CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            var superfici = new Dictionary<int, double?>();
            await using (var comando = Comando(connessione, "SELECT IdVigneto, Superficie FROM Vigneto WHERE IdEnte = $azienda", aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    superfici[r.GetInt32(0)] = Reale(r, 1);
                }
            }

            var suolo = new List<SuoloVigneto>();
            await using (var comando = Comando(connessione, """
                SELECT v.IdVigneto, v.NomeVigneto, g.Vigna, s.CapacitaDiCampo, s.AcquaDisponibile_mm_m, s.PuntoDiAppassimentoPermanente,
                       s.PunteggioProfonditaSuolo, s.PunteggioPietrositaSuolo, s.PotenzialeIdricoVigneto, s.PotenzialeIdricoVigna,
                       s.ConducibilitaIdraulicaSaturazione
                FROM VistaStatistichePotenzialeIdricoVigneti s
                JOIN Vigneto v ON v.IdVigneto = s.IdVigneto LEFT JOIN Vigna g ON g.IdVigna = v.IdVigna
                WHERE v.IdEnte = $azienda
                ORDER BY g.Vigna, v.NomeVigneto
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    suolo.Add(new SuoloVigneto(r.GetInt32(0), r.IsDBNull(1) ? $"Vigneto {r.GetInt32(0)}" : r.GetString(1).Trim(),
                        r.IsDBNull(2) ? null : r.GetString(2).Trim(), Reale(r, 3), Reale(r, 4), Reale(r, 5), Reale(r, 6), Reale(r, 7),
                        Reale(r, 8), Reale(r, 9), Reale(r, 10)));
                }
            }

            var vista = new List<ScottaturaVigneto>();
            await using (var comando = Comando(connessione, """
                SELECT s.IdVigneto, s.Anno, s.PercIntensita, s.PercDiffusione
                FROM VistaScottature s JOIN Vigneto v ON v.IdVigneto = s.IdVigneto
                WHERE v.IdEnte = $azienda AND s.Anno IS NOT NULL
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    vista.Add(new ScottaturaVigneto(r.GetInt32(0), r.GetInt32(1), null, Reale(r, 2), Reale(r, 3)));
                }
            }

            var rilievi = new List<ScottaturaVigneto>();
            await using (var comando = Comando(connessione, """
                SELECT r.IdVigneto, r.Anno, r.IdOrgano, r.PercIntensita, r.PercDiffusione
                FROM Rilievo r JOIN Vigneto v ON v.IdVigneto = r.IdVigneto
                WHERE v.IdEnte = $azienda AND r.IdTipoRilievo = 32 AND r.IdOrgano IN (2, 5) AND r.Anno IS NOT NULL
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    rilievi.Add(new ScottaturaVigneto(r.GetInt32(0), r.GetInt32(1), r.IsDBNull(2) ? null : r.GetInt32(2), Reale(r, 3), Reale(r, 4)));
                }
            }

            var vigoria = new List<VigoriaReport>();
            await using (var comando = Comando(connessione, """
                SELECT g.IdVigneto, g.Anno, g.PunteggioLegnoPotatura, g.PunteggioSFEPerMetro, g.PunteggioStatoVegetativo,
                       g.PunteggioGestioneChioma, g.Punteggio
                FROM VistaVigoria g JOIN Vigneto v ON v.IdVigneto = g.IdVigneto
                WHERE v.IdEnte = $azienda AND g.Anno IS NOT NULL
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    vigoria.Add(new VigoriaReport(r.GetInt32(0), r.GetInt32(1), Reale(r, 2), Reale(r, 3), Reale(r, 4), Reale(r, 5), Reale(r, 6)));
                }
            }

            var anagrafica = new List<VignetoReport>();
            await using (var comando = Comando(connessione, """
                SELECT v.IdVigneto, v.NomeVigneto, g.Vigna, t.Vitigno, v.AnnoImpianto, v.Superficie,
                       v.NumeroPianteContate, v.SullaFila, v.TraFila, v.Esposizione, v.Orientamento
                FROM Vigneto v LEFT JOIN Vigna g ON g.IdVigna = v.IdVigna LEFT JOIN Vitigno t ON t.IdVitigno = v.IdVitigno
                WHERE v.IdEnte = $azienda
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    var id = r.GetInt32(0);
                    anagrafica.Add(new VignetoReport(id, r.IsDBNull(1) ? $"Vigneto {id}" : r.GetString(1).Trim(),
                        r.IsDBNull(2) ? null : r.GetString(2).Trim(), r.IsDBNull(3) ? null : r.GetString(3).Trim(),
                        r.IsDBNull(4) ? null : r.GetInt32(4), Reale(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8),
                        NumeroDaTesto(r, 9), NumeroDaTesto(r, 10)));
                }
            }

            var produzione = new List<ProduzioneReport>();
            await using (var comando = Comando(connessione, """
                SELECT p.IdVigneto, p.Anno, 0, p.ProduzioneRealeVigneto, p.ProduzioneRealeEttaro, p.PercFallanze, p.PercInAllevamento
                FROM VistaProduzioneVendemmia p JOIN Vigneto v ON v.IdVigneto = p.IdVigneto
                WHERE v.IdEnte = $azienda AND p.Anno IS NOT NULL
                UNION ALL
                SELECT p.IdVigneto, p.Anno, 1, p.ProduzioneRealeVigneto, p.ProduzioneRealeEttaro, p.PercFallanze, p.PercInAllevamento
                FROM VistaProduzioneGrappoli p JOIN Vigneto v ON v.IdVigneto = p.IdVigneto
                WHERE v.IdEnte = $azienda AND p.Anno IS NOT NULL
                """, aziendaId))
            await using (var r = await comando.ExecuteReaderAsync(ct))
            {
                while (await r.ReadAsync(ct))
                {
                    produzione.Add(new ProduzioneReport(r.GetInt32(0), r.GetInt32(1), r.GetInt64(2) == 1, Reale(r, 3), Reale(r, 4),
                        Reale(r, 5), Reale(r, 6)));
                }
            }

            return new CampoReportLetto(superfici, suolo, vista, rilievi, vigoria, anagrafica, produzione,
                await RilieviAsync(connessione, aziendaId, ct), await CollezioneAsync(connessione, aziendaId, ct),
                await ParametriAsync(connessione, aziendaId, ct), await NomiAsync(connessione, "SELECT Id, Descrizione FROM GestioneChioma", 0, ct),
                await NomiAsync(connessione, """
                    SELECT s.IdSubVigneto, s.Nome FROM SubVigneto s JOIN Vigneto v ON v.IdVigneto = s.IdVigneto WHERE v.IdEnte = $azienda
                    """, aziendaId, ct),
                await SanitaAsync(connessione, aziendaId, ct));
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Dati di campo del report fine stagione {Percorso} non leggibili: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// I tipi di rilievo dei fogli (esca 9, sanità, tignole, chioma 117, stato idrico 146, potenziale pre-alba 165 del
    /// report inglese, acinellatura 168).
    /// </summary>
    public static readonly IReadOnlyList<int> TipiRilievo = [9, 11, 12, 13, 29, 30, 32, 73, 84, 86, 117, 146, 148, 149, 153, 155, 165, 168];

    /// <summary>I rilievi dei tipi dei fogli, più quelli con un BBCH o una generazione delle tignole (fenologia, nidi).</summary>
    private static async Task<IReadOnlyList<RilievoReport>> RilieviAsync(SqliteConnection connessione, int aziendaId, CancellationToken ct)
    {
        var lista = new List<RilievoReport>();
        await using var comando = Comando(connessione, $"""
            SELECT r.IdVigneto, r.IdSubVigneto, r.InizioRilievo, r.Anno, r.IdTipoRilievo, r.IdOrgano, r.IdEtaEpocaGenerazione,
                   r.NrOrganiControllati, r.PercDiffusione, r.PercIntensita, r.NrFormeMobiliNidi, r.CatturePerTrappola, r.SFEPerVite,
                   r.IdValutazioneStatoIdrico, r.BBCH, r.AltezzaChioma, r.LarghezzaChioma, r.DistanzaPianteSullaFila, r.SpaziVuoti,
                   r.FormaAllevamento, r.IdGestioneChioma, r.IdUtente, r.NoteRilievo, r.PotenzialeIdricoPerVite
            FROM Rilievo r JOIN Vigneto v ON v.IdVigneto = r.IdVigneto
            WHERE v.IdEnte = $azienda AND r.Anno IS NOT NULL AND r.InizioRilievo IS NOT NULL AND r.IdTipoRilievo IS NOT NULL
              AND (r.IdTipoRilievo IN ({string.Join(", ", TipiRilievo)}) OR r.BBCH IS NOT NULL OR r.IdEtaEpocaGenerazione IS NOT NULL)
            """, aziendaId);
        await using var r = await comando.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            if (!DateTime.TryParse(r.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
            {
                continue;
            }
            lista.Add(new RilievoReport(r.GetInt32(0), Intero(r, 1), data, r.GetInt32(3), r.GetInt32(4), Intero(r, 5), Intero(r, 6),
                Reale(r, 7), Reale(r, 8), Reale(r, 9), Reale(r, 10), Reale(r, 11), Reale(r, 12), Intero(r, 13), Intero(r, 14),
                Reale(r, 15), Reale(r, 16), Reale(r, 17), Reale(r, 18), Reale(r, 19), Intero(r, 20), Intero(r, 21),
                r.IsDBNull(22) ? null : Convert.ToString(r.GetValue(22), CultureInfo.InvariantCulture), Reale(r, 23)));
        }
        return lista;
    }

    private static async Task<IReadOnlyList<CollezioneReport>> CollezioneAsync(SqliteConnection connessione, int aziendaId, CancellationToken ct)
    {
        var lista = new List<CollezioneReport>();
        await using var comando = Comando(connessione, """
            SELECT c.IdVigneto, c.Anno, c.ValoreConteggiato, SUM(c.NumOrgani)
            FROM Collezione c JOIN Vigneto v ON v.IdVigneto = c.IdVigneto
            WHERE v.IdEnte = $azienda AND c.Anno IS NOT NULL AND c.ValoreConteggiato IS NOT NULL
            GROUP BY c.IdVigneto, c.Anno, c.ValoreConteggiato
            """, aziendaId);
        await using var r = await comando.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            lista.Add(new CollezioneReport(r.GetInt32(0), r.GetInt32(1), (int)r.GetDouble(2), Reale(r, 3) ?? 0));
        }
        return lista;
    }

    private static async Task<IReadOnlyList<ParametroIndiceReport>> ParametriAsync(SqliteConnection connessione, int aziendaId, CancellationToken ct)
    {
        var lista = new List<ParametroIndiceReport>();
        await using var comando = Comando(connessione, """
            SELECT p.IdVigneto, p.Anno, p.IdParametro, p.Avg
            FROM VistaIndicePerParametro p JOIN Vigneto v ON v.IdVigneto = p.IdVigneto
            WHERE v.IdEnte = $azienda AND p.Anno IS NOT NULL AND p.IdParametro IN ('SFE', 'PRODUZIONE')
            """, aziendaId);
        await using var r = await comando.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            lista.Add(new ParametroIndiceReport(r.GetInt32(0), r.GetInt32(1), r.GetString(2), Reale(r, 3)));
        }
        return lista;
    }

    /// <summary>
    /// Sanità (fogli 26–36): rilievi e organi per anno e tipo (tutti i tipi), vigneti monitorati per anno
    /// (<c>Count(distinct IdVignetoDaApp)</c>), nomi dei tipi e fasi fenologiche già ridotte a un giorno medio per
    /// vigneto e anno: <c>ModelloBBCH</c> (germogliamento 7–9, fioritura 63–68, invaiatura 81–83) e
    /// <c>Rilievo_vendemmia</c>. Il giorno è il numero del giorno dell'anno, come <c>Day_Ref</c> di Qlik.
    /// </summary>
    private static async Task<SanitaReportLetta> SanitaAsync(SqliteConnection connessione, int aziendaId, CancellationToken ct)
    {
        var conteggi = new List<ConteggioRilievi>();
        await using (var comando = Comando(connessione, """
            SELECT r.Anno, r.IdTipoRilievo, COUNT(DISTINCT r.IdRilievo), TOTAL(r.NrOrganiControllati), r.IdVigneto
            FROM Rilievo r JOIN Vigneto v ON v.IdVigneto = r.IdVigneto
            WHERE v.IdEnte = $azienda AND r.Anno IS NOT NULL AND r.IdTipoRilievo IS NOT NULL
            GROUP BY r.Anno, r.IdTipoRilievo, r.IdVigneto
            """, aziendaId))
        await using (var r = await comando.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                conteggi.Add(new ConteggioRilievi(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetDouble(3), r.GetInt32(4)));
            }
        }

        var monitorati = new Dictionary<int, int>();
        await using (var comando = Comando(connessione, """
            SELECT r.Anno, COUNT(DISTINCT r.IdVignetoDaApp)
            FROM Rilievo r JOIN Vigneto v ON v.IdVigneto = r.IdVigneto
            WHERE v.IdEnte = $azienda AND r.Anno IS NOT NULL
            GROUP BY r.Anno
            """, aziendaId))
        await using (var r = await comando.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                monitorati[r.GetInt32(0)] = r.GetInt32(1);
            }
        }

        var fasi = new List<FaseFenologica>();
        await using (var comando = Comando(connessione, $"""
            SELECT m.IdVigneto, CAST(substr(m.Data, 1, 4) AS INTEGER) AS Anno,
                   CASE WHEN m.IdBBCH_UltimaSettimana IN (7, 8, 9) THEN 1
                        WHEN m.IdBBCH_UltimaSettimana BETWEEN 63 AND 68 THEN 2 ELSE 3 END AS Fase,
                   AVG({GiornoQlik("m.Data")})
            FROM ModelloBBCH m JOIN Vigneto v ON v.IdVigneto = m.IdVigneto
            WHERE v.IdEnte = $azienda AND m.Data IS NOT NULL
              AND (m.IdBBCH_UltimaSettimana IN (7, 8, 9, 81, 82, 83) OR m.IdBBCH_UltimaSettimana BETWEEN 63 AND 68)
            GROUP BY m.IdVigneto, Anno, Fase
            UNION ALL
            SELECT d.IdVigneto, CAST(substr(d.DataDiVendemmia, 1, 4) AS INTEGER), 4,
                   AVG({GiornoQlik("d.DataDiVendemmia")})
            FROM Rilievo_vendemmia d JOIN Vigneto v ON v.IdVigneto = d.IdVigneto
            WHERE v.IdEnte = $azienda AND d.DataDiVendemmia IS NOT NULL
            GROUP BY d.IdVigneto, 2
            """, aziendaId))
        await using (var r = await comando.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                if (!r.IsDBNull(3))
                {
                    fasi.Add(new FaseFenologica(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetDouble(3)));
                }
            }
        }

        return new SanitaReportLetta(conteggi,
            await NomiAsync(connessione, "SELECT idTipoRilievo, TipoRilevo FROM TipoRilievo", 0, ct), monitorati, fasi);
    }

    /// <summary>
    /// <c>DayNumberOfYear</c> di Qlik in SQL: conta sempre su un anno di 366 giorni, quindi negli anni non bisestili dal
    /// 1° marzo dà un giorno in più del calendario (1/3 = 61 in ogni anno). Così le medie fra anni diversi tornano con Qlik
    /// (task #205: gli storici 5/10 anni della fenologia).
    /// </summary>
    private static string GiornoQlik(string colonna) => $"""
        (CAST(strftime('%j', substr({colonna}, 1, 10)) AS REAL)
         + CASE WHEN CAST(substr({colonna}, 6, 2) AS INTEGER) > 2
                 AND NOT (CAST(substr({colonna}, 1, 4) AS INTEGER) % 4 = 0
                          AND (CAST(substr({colonna}, 1, 4) AS INTEGER) % 100 <> 0 OR CAST(substr({colonna}, 1, 4) AS INTEGER) % 400 = 0))
                THEN 1 ELSE 0 END)
        """;

    /// <summary>Id → nome (gestioni della chioma, sottovigneti): righe senza Id saltate, nome assente = vuoto.</summary>
    /// <summary>
    /// Report settimanale (task #213): tutti i rilievi dell'azienda fra due date comprese, con i nomi dei tipi; null se lo
    /// snapshot non c'è o non si legge (allora si va su 4Grapes). Il filtro sull'anno è in SQL, quello sul giorno qui
    /// (<c>InizioRilievo</c> è testo).
    /// </summary>
    public async Task<RilieviSettimanaLetti?> LeggiRilieviAsync(int aziendaId, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            if (await TabellaAsync(connessione, "RS_Rilievo", ct))
            {
                return await LeggiRilieviRsAsync(connessione, aziendaId, da, a, ct);
            }
            var rilievi = new List<RilievoSettimana>();
            await using (var comando = Comando(connessione, """
                SELECT r.IdVigneto, r.InizioRilievo, r.IdTipoRilievo, r.BBCH, r.IdOrgano, r.IdEtaEpocaGenerazione,
                       r.NrOrganiControllati, r.PercDiffusione, r.PercIntensita, r.NrFormeMobiliNidi, r.CatturePerTrappola,
                       r.IdValutazioneStatoIdrico
                FROM Rilievo r JOIN Vigneto v ON v.IdVigneto = r.IdVigneto
                WHERE v.IdEnte = $azienda AND r.Anno BETWEEN $annoDa AND $annoA
                  AND r.InizioRilievo IS NOT NULL AND r.IdTipoRilievo IS NOT NULL
                """, aziendaId))
            {
                comando.Parameters.AddWithValue("$annoDa", da.Year);
                comando.Parameters.AddWithValue("$annoA", a.Year);
                await using var r = await comando.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    if (DateTime.TryParse(r.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
                        && DateOnly.FromDateTime(data) is var giorno && giorno >= da && giorno <= a)
                    {
                        rilievi.Add(new RilievoSettimana(r.GetInt32(0), data, r.GetInt32(2), Intero(r, 3), Intero(r, 4),
                            Intero(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8), Reale(r, 9), Reale(r, 10), Intero(r, 11)));
                    }
                }
            }
            return new RilieviSettimanaLetti(rilievi,
                await NomiAsync(connessione, "SELECT idTipoRilievo, TipoRilevo FROM TipoRilievo", 0, ct), "snapshot",
                await CostruitoIlAsync(connessione, ct));
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Rilievi della settimana {Percorso} non leggibili: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Le tabelle del report settimanale (task #212, <c>dati-dba.md</c> in docs/specifiche/report-settimanale): rilievi
    /// legati all'azienda dell'app (<c>IdAziendaDaApp</c>) con la <c>Week</c> di Qlik, danni da ampelopatie, collezione
    /// dell'esca, meteo per settimana dei vigneti dell'azienda e riepilogo dei controlli.
    /// </summary>
    private static async Task<RilieviSettimanaLetti> LeggiRilieviRsAsync(SqliteConnection connessione, int aziendaId, DateOnly da,
        DateOnly a, CancellationToken ct)
    {
        // Dal 14/11 (dba #219) le tabelle RS_ hanno anche le righe del perimetro LAORE: il settimanale tiene le VTS.
        var soloVts = await ColonnaAsync(connessione, "RS_Rilievo", "VTS", ct) ? " AND VTS = 1" : "";
        var (rilievi, esca) = await RilieviEscaRsAsync(connessione, "IdAziendaDaApp = $azienda" + soloVts,
            c => c.Parameters.AddWithValue("$azienda", aziendaId), da, a, ct);

        var danni = new List<DannoSettimana>();
        await using (var comando = Comando(connessione, """
            SELECT d.IdVigneto, d.Anno, d.PercDanno FROM RS_VistaDanniDaAmpelopatie d
            JOIN RS_Vigneto v ON v.IdVigneto = d.IdVigneto
            WHERE v.IdEnte = $azienda AND d.PercDanno IS NOT NULL AND d.Anno IS NOT NULL
            """, aziendaId))
        await using (var r = await comando.ExecuteReaderAsync(ct))
        {
            while (await r.ReadAsync(ct))
            {
                danni.Add(new DannoSettimana((int)r.GetDouble(0), (int)r.GetDouble(1), r.GetDouble(2)));
            }
        }

        var meteo = new List<MeteoSettimana>();
        if (await TabellaAsync(connessione, "RS_MeteoSettimana", ct))
        {
            await using var comando = Comando(connessione, """
                SELECT m.IdVigneto, m.Anno, m.Week, m.GiorniConDati, m.GiorniPrevisione, m.Pioggia, m.TMed, m.TMin, m.TMax
                FROM RS_MeteoSettimana m JOIN RS_Vigneto v ON v.IdVigneto = m.IdVigneto
                WHERE v.IdEnte = $azienda AND m.Anno BETWEEN $annoDa AND $annoA
                """, aziendaId);
            comando.Parameters.AddWithValue("$annoDa", da.Year);
            comando.Parameters.AddWithValue("$annoA", a.Year);
            await using var r = await comando.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                meteo.Add(new MeteoSettimana((int)r.GetDouble(0), (int)r.GetDouble(1), (int)r.GetDouble(2), Intero(r, 3) ?? 0,
                    Intero(r, 4) ?? 0, Reale(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8)));
            }
        }

        var riepilogo = await RiepilogoRsAsync(connessione, "VTS", "IdEnte = $azienda", c => c.Parameters.AddWithValue("$azienda", aziendaId), ct);

        // Fenologia della slide 7 come Qlik: Day_Ref = giorno dell'anno su 366 giorni (dal 1/3 un giorno in più negli anni non
        // bisestili), media per vigneto, anno e codice del modello giornaliero senza le righe recuperate dalla REGIONE (dba,
        // 13/11: Qlik le scarta, anche le NULL); la vendemmia dalle date dei rilievi con BBCH 89.
        var giorni = new List<GiornoBbchSettimana>();
        const string GiornoQlik = """
            (CAST(strftime('%j', {0}) AS INTEGER) + CASE WHEN CAST(strftime('%m', {0}) AS INTEGER) > 2
              AND NOT ((CAST(strftime('%Y', {0}) AS INTEGER) % 4 = 0 AND CAST(strftime('%Y', {0}) AS INTEGER) % 100 <> 0)
                OR CAST(strftime('%Y', {0}) AS INTEGER) % 400 = 0) THEN 1 ELSE 0 END)
            """;
        foreach (var sql in new[]
        {
            await TabellaAsync(connessione, "ModelloBBCH", ct) ? $"""
                SELECT m.IdVigneto, CAST(strftime('%Y', m.Data) AS INTEGER), m.IdBBCH_UltimaSettimana, AVG({string.Format(CultureInfo.InvariantCulture, GiornoQlik, "m.Data")})
                FROM ModelloBBCH m JOIN RS_Vigneto v ON v.IdVigneto = m.IdVigneto
                WHERE v.IdEnte = $azienda AND m.IdBBCH_UltimaSettimana IN (7, 8, 9, 63, 64, 65, 66, 67, 68, 81, 82, 83)
                  AND m.IdBBCH_RecuperatoDa <> 'REGIONE'
                GROUP BY m.IdVigneto, CAST(strftime('%Y', m.Data) AS INTEGER), m.IdBBCH_UltimaSettimana
                """ : null,
            $"""
                SELECT g.IdVigneto, CAST(strftime('%Y', g.DataDiVendemmia) AS INTEGER), 89, AVG({string.Format(CultureInfo.InvariantCulture, GiornoQlik, "g.DataDiVendemmia")})
                FROM RS_Vendemmia g JOIN RS_Vigneto v ON v.IdVigneto = g.IdVigneto
                WHERE v.IdEnte = $azienda AND g.DataDiVendemmia IS NOT NULL
                GROUP BY g.IdVigneto, CAST(strftime('%Y', g.DataDiVendemmia) AS INTEGER)
                """,
        })
        {
            if (sql is null)
            {
                continue;
            }
            await using var comando = Comando(connessione, sql, aziendaId);
            await using var r = await comando.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                if (!r.IsDBNull(1) && !r.IsDBNull(2) && !r.IsDBNull(3))
                {
                    giorni.Add(new GiornoBbchSettimana((int)r.GetDouble(0), r.GetInt32(1), (int)r.GetDouble(2), r.GetDouble(3)));
                }
            }
        }

        return new RilieviSettimanaLetti(rilievi,
            await NomiAsync(connessione, "SELECT idTipoRilievo, TipoRilevo FROM TipoRilievo", 0, ct), "snapshot",
            await CostruitoIlAsync(connessione, ct))
        {
            GiorniBbch = giorni,
            Danni = danni,
            Esca = esca,
            Meteo = meteo,
            Riepilogo = riepilogo,
        };
    }

    /// <summary>
    /// Rilievi di <c>RS_Rilievo</c> che passano il <paramref name="filtro"/> (SQL sulle colonne di RS_Rilievo) fra due date
    /// comprese e le righe della collezione dell'esca (tipo 9) di quei rilievi: comune al settimanale e al LAORE.
    /// </summary>
    private static async Task<(List<RilievoSettimana> Rilievi, List<EscaSettimana> Esca)> RilieviEscaRsAsync(SqliteConnection connessione,
        string filtro, Action<SqliteCommand> parametri, DateOnly da, DateOnly a, CancellationToken ct)
    {
        SqliteCommand Nuovo(string sql)
        {
            var comando = connessione.CreateCommand();
            comando.CommandText = sql;
            parametri(comando);
            comando.Parameters.AddWithValue("$annoDa", da.Year);
            comando.Parameters.AddWithValue("$annoA", a.Year);
            return comando;
        }

        var rilievi = new List<RilievoSettimana>();
        await using (var comando = Nuovo($"""
            SELECT IdVigneto, InizioRilievo, IdTipoRilievo, BBCH, IdOrgano, IdEtaEpocaGenerazione, NrOrganiControllati,
                   PercDiffusione, PercIntensita, NrFormeMobiliNidi, CatturePerTrappola, IdValutazioneStatoIdrico,
                   PresenzaAvversitaCarenza, LungMediaGermogliPerVite, IdRilievo, Week
            FROM RS_Rilievo
            WHERE ({filtro}) AND Anno BETWEEN $annoDa AND $annoA
              AND InizioRilievo IS NOT NULL AND IdTipoRilievo IS NOT NULL AND IdVigneto IS NOT NULL
            """))
        {
            await using var r = await comando.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                if (DateTime.TryParse(r.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
                    && DateOnly.FromDateTime(data) is var giorno && giorno >= da && giorno <= a)
                {
                    rilievi.Add(new RilievoSettimana((int)r.GetDouble(0), data, (int)r.GetDouble(2), Intero(r, 3), Intero(r, 4),
                        Intero(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8), Reale(r, 9), Reale(r, 10), Intero(r, 11),
                        Intero(r, 12), Reale(r, 13), r.IsDBNull(14) ? null : r.GetString(14), Intero(r, 15)));
                }
            }
        }

        var esca = new List<EscaSettimana>();
        await using (var comando = Nuovo($"""
            SELECT c.IdRilievo, c.ValoreConteggiato FROM RS_Collezione c
            WHERE c.ValoreConteggiato IS NOT NULL AND c.IdRilievo IN (
                SELECT IdRilievo FROM RS_Rilievo WHERE ({filtro}) AND IdTipoRilievo = 9 AND Anno BETWEEN $annoDa AND $annoA)
            """))
        {
            await using var r = await comando.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                esca.Add(new EscaSettimana(r.GetString(0), (int)r.GetDouble(1)));
            }
        }
        return (rilievi, esca);
    }

    /// <summary>
    /// I controlli del dba per settimana (<c>RS_RiepilogoSettimana</c>) delle aziende che passano il filtro, sommati per
    /// settimana; con la colonna <c>Perimetro</c> (dba #219) solo le righe del perimetro dato. Vuoto senza tabella.
    /// </summary>
    private static async Task<List<RiepilogoSettimana>> RiepilogoRsAsync(SqliteConnection connessione, string perimetro, string filtro,
        Action<SqliteCommand> parametri, CancellationToken ct)
    {
        var riepilogo = new List<RiepilogoSettimana>();
        if (!await TabellaAsync(connessione, "RS_RiepilogoSettimana", ct))
        {
            return riepilogo;
        }
        var colonna = await ColonnaAsync(connessione, "RS_RiepilogoSettimana", "Perimetro", ct) ? " AND Perimetro = $perimetro" : "";
        await using var comando = connessione.CreateCommand();
        comando.CommandText = $"""
            SELECT Anno, Week, SUM(VignetiAttivi), SUM(VignetiRilevati), SUM(Rilievi), SUM(PuntiMeteo), SUM(PuntiMeteoCompleti),
                   SUM(GiorniMeteoMancanti), SUM(GiorniMeteoPrevisione)
            FROM RS_RiepilogoSettimana WHERE ({filtro}){colonna} AND Anno IS NOT NULL AND Week IS NOT NULL
            GROUP BY Anno, Week
            """;
        comando.Parameters.AddWithValue("$perimetro", perimetro);
        parametri(comando);
        await using var r = await comando.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            riepilogo.Add(new RiepilogoSettimana((int)r.GetDouble(0), (int)r.GetDouble(1), Intero(r, 2) ?? 0, Intero(r, 3) ?? 0,
                Intero(r, 4) ?? 0, Intero(r, 5) ?? 0, Intero(r, 6) ?? 0, Intero(r, 7) ?? 0, Intero(r, 8) ?? 0));
        }
        return riepilogo;
    }

    private static async Task<bool> ColonnaAsync(SqliteConnection connessione, string tabella, string colonna, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = "SELECT 1 FROM pragma_table_info($tabella) WHERE name = $colonna";
        comando.Parameters.AddWithValue("$tabella", tabella);
        comando.Parameters.AddWithValue("$colonna", colonna);
        return await comando.ExecuteScalarAsync(ct) is not null;
    }

    /// <summary>
    /// Il perimetro del report settimanale LAORE come lo script di Qlik (dba, task #218): aziende vitivinicole
    /// (<c>IdTipoEnte = 1</c>) con vigneti in Sardegna (<c>VignetoMaps.AdministrativeAreaLevel1</c>), senza filtri su
    /// gruppo, estirpo o carico (estirpati inclusi come Qlik). Dal 14/11 il dba lo segna con la colonna <c>LAORE</c> di ogni
    /// tabella RS_ (task #219); con uno snapshot di prima si ricostruisce. Condizione sui vigneti (alias <c>m</c>).
    /// </summary>
    private static string VignetiLaore(bool colonna) => colonna ? "m.LAORE = 1" : """
        m.AdministrativeAreaLevel1 = 'Sardegna'
          AND m.IdVigneto IN (SELECT v.IdVigneto FROM RS_Vigneto v JOIN RS_Ente e ON e.IdEnte = v.IdEnte WHERE e.IdTipoEnte = 1)
        """;

    /// <summary>
    /// Report settimanale LAORE (task #217): i vigneti del perimetro con provincia (<c>AdministrativeAreaLevel2</c>) e
    /// comune (<c>AdministrativeAreaLevel3</c>); null se lo snapshot non c'è o non ha le tabelle.
    /// </summary>
    public async Task<IReadOnlyList<LuogoVigneto>?> LeggiLuoghiLaoreAsync(CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            if (!await TabellaAsync(connessione, "RS_VignetoMaps", ct))
            {
                return null;
            }
            var luoghi = new List<LuogoVigneto>();
            await using var comando = connessione.CreateCommand();
            comando.CommandText = $"""
                SELECT m.IdVigneto, m.AdministrativeAreaLevel2, m.AdministrativeAreaLevel3 FROM RS_VignetoMaps m
                WHERE {VignetiLaore(await ColonnaAsync(connessione, "RS_VignetoMaps", "LAORE", ct))} AND m.IdVigneto IS NOT NULL
                """;
            await using var r = await comando.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var provincia = r.IsDBNull(1) ? null : r.GetString(1).Trim();
                luoghi.Add(new LuogoVigneto((int)r.GetDouble(0), string.IsNullOrEmpty(provincia) ? "—" : provincia,
                    r.IsDBNull(2) ? null : r.GetString(2).Trim()));
            }
            return luoghi;
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Luoghi LAORE {Percorso} non leggibili: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Report settimanale LAORE: i rilievi del perimetro (rilievo dell'azienda di tipo 1 su un vigneto in Sardegna, come lo
    /// script) fra due date, ristretti a provincia e comune se dati; null senza snapshot o senza le tabelle RS_.
    /// </summary>
    public async Task<RilieviSettimanaLetti?> LeggiRilieviLaoreAsync(string? provincia, string? comune, DateOnly da, DateOnly a,
        CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            if (!await TabellaAsync(connessione, "RS_Rilievo", ct) || !await TabellaAsync(connessione, "RS_VignetoMaps", ct))
            {
                return null;
            }
            var colonna = await ColonnaAsync(connessione, "RS_Rilievo", "LAORE", ct);
            // I vigneti del territorio scelto: filtro comune a rilievi, meteo e controlli.
            var territorio = $"""
                SELECT m.IdVigneto FROM RS_VignetoMaps m WHERE {VignetiLaore(colonna)}
                    AND ($provincia IS NULL OR m.AdministrativeAreaLevel2 = $provincia)
                    AND ($comune IS NULL OR m.AdministrativeAreaLevel3 = $comune)
                """;
            void Parametri(SqliteCommand c)
            {
                c.Parameters.AddWithValue("$provincia", (object?)provincia ?? DBNull.Value);
                c.Parameters.AddWithValue("$comune", (object?)comune ?? DBNull.Value);
            }

            var (rilievi, esca) = await RilieviEscaRsAsync(connessione, colonna
                ? $"LAORE = 1 AND IdVigneto IN ({territorio})"
                : $"IdAziendaDaApp IN (SELECT IdEnte FROM RS_Ente WHERE IdTipoEnte = 1) AND IdVigneto IN ({territorio})", Parametri, da, a, ct);

            // Meteo per settimana dei punti meteo del territorio (quasi nessuno: 8 aziende su 173 ne hanno, dba #219).
            var meteo = new List<MeteoSettimana>();
            if (colonna && await TabellaAsync(connessione, "RS_MeteoSettimana", ct))
            {
                await using var comando = connessione.CreateCommand();
                comando.CommandText = $"""
                    SELECT IdVigneto, Anno, Week, GiorniConDati, GiorniPrevisione, Pioggia, TMed, TMin, TMax
                    FROM RS_MeteoSettimana WHERE LAORE = 1 AND IdVigneto IN ({territorio}) AND Anno BETWEEN $annoDa AND $annoA
                    """;
                Parametri(comando);
                comando.Parameters.AddWithValue("$annoDa", da.Year);
                comando.Parameters.AddWithValue("$annoA", a.Year);
                await using var r = await comando.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    meteo.Add(new MeteoSettimana((int)r.GetDouble(0), (int)r.GetDouble(1), (int)r.GetDouble(2), Intero(r, 3) ?? 0,
                        Intero(r, 4) ?? 0, Reale(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8)));
                }
            }

            // Controlli: le aziende con almeno un vigneto nel territorio, sommate per settimana.
            var riepilogo = colonna
                ? await RiepilogoRsAsync(connessione, "LAORE", $"IdEnte IN (SELECT v.IdEnte FROM RS_Vigneto v WHERE v.IdVigneto IN ({territorio}))",
                    Parametri, ct)
                : [];

            return new RilieviSettimanaLetti(rilievi,
                await NomiAsync(connessione, "SELECT idTipoRilievo, TipoRilevo FROM TipoRilievo", 0, ct), "snapshot",
                await CostruitoIlAsync(connessione, ct))
            {
                Esca = esca,
                Meteo = meteo,
                Riepilogo = riepilogo,
            };
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Rilievi LAORE {Percorso} non leggibili: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    private static async Task<bool> TabellaAsync(SqliteConnection connessione, string nome, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = "SELECT 1 FROM sqlite_master WHERE type IN ('table', 'view') AND name = $nome";
        comando.Parameters.AddWithValue("$nome", nome);
        return await comando.ExecuteScalarAsync(ct) is not null;
    }

    private static async Task<IReadOnlyDictionary<int, string>> NomiAsync(SqliteConnection connessione, string sql, int aziendaId,
        CancellationToken ct)
    {
        var mappa = new Dictionary<int, string>();
        await using var comando = Comando(connessione, sql, aziendaId);
        await using var r = await comando.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            if (!r.IsDBNull(0))
            {
                mappa[(int)r.GetDouble(0)] = r.IsDBNull(1) ? "" : Convert.ToString(r.GetValue(1), CultureInfo.InvariantCulture)!.Trim();
            }
        }
        return mappa;
    }

    private static int? Intero(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : (int)r.GetDouble(i);

    private static double? Reale(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);

    /// <summary>Un numero che può arrivare come testo («290», ma anche «nord sud»): il testo non numerico è null.</summary>
    private static double? NumeroDaTesto(SqliteDataReader r, int i) =>
        r.IsDBNull(i) ? null
        : r.GetFieldType(i) == typeof(string)
            ? double.TryParse(r.GetString(i).Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null
            : r.GetDouble(i);

    /// <summary>I controlli del dba sui dati, con le sole righe dell'azienda; null se lo snapshot non c'è.</summary>
    public async Task<IReadOnlyList<ControlloReport>?> LeggiControlliAsync(int aziendaId, CancellationToken ct = default)
    {
        if (Percorso is not { } percorso)
        {
            return null;
        }
        try
        {
            await using var connessione = await ApriAsync(percorso, ct);
            string? azienda;
            await using (var comando = Comando(connessione, "SELECT RagioneSociale FROM Ente WHERE IdEnte = $azienda", aziendaId))
            {
                azienda = (await comando.ExecuteScalarAsync(ct)) as string;
            }

            var controlli = new List<ControlloReport>();
            await using var lettura = Comando(connessione,
                "SELECT codice, titolo, perche, gravita, annata, dettaglio_json FROM _controlli ORDER BY rowid", aziendaId);
            await using var r = await lettura.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var (colonne, righe) = Dettaglio(r.IsDBNull(5) ? null : r.GetString(5), azienda);
                controlli.Add(new ControlloReport(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2),
                    r.IsDBNull(3) ? "" : r.GetString(3), r.IsDBNull(4) ? null : r.GetInt32(4), colonne, righe));
            }
            return controlli;
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning("Controlli del report fine stagione {Percorso} non leggibili: {Messaggio}", percorso, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Il dettaglio JSON di un controllo (<c>{"colonne": [...], "righe": [[...]]}</c>): con una colonna «Azienda» si
    /// tengono solo le righe dell'azienda (confronto senza maiuscole e spazi ai bordi), senza si tengono tutte.
    /// </summary>
    public static (IReadOnlyList<string> Colonne, IReadOnlyList<IReadOnlyList<string>> Righe) Dettaglio(string? json, string? azienda)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], []);
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            var colonne = doc.RootElement.TryGetProperty("colonne", out var c)
                ? c.EnumerateArray().Select(Testo).ToList() : [];
            var righe = doc.RootElement.TryGetProperty("righe", out var rr)
                ? rr.EnumerateArray().Select(riga => (IReadOnlyList<string>)riga.EnumerateArray().Select(Testo).ToList()).ToList() : [];
            var indice = colonne.FindIndex(x => string.Equals(x, "Azienda", StringComparison.OrdinalIgnoreCase));
            if (indice >= 0)
            {
                righe = righe.Where(riga => indice < riga.Count && azienda is not null
                    && string.Equals(riga[indice].Trim(), azienda.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return (colonne, righe);
        }
        catch (JsonException)
        {
            return ([], []);
        }
    }

    private static string Testo(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : e.ToString();

    private static decimal? Decimale(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : (decimal)r.GetDouble(i);

    private static async Task<DateTimeOffset?> CostruitoIlAsync(SqliteConnection connessione, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = "SELECT MAX(costruito_il) FROM _snapshot";
        return await comando.ExecuteScalarAsync(ct) is string s
            && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d) ? d : null;
    }

    private static async Task<SqliteConnection> ApriAsync(string percorso, CancellationToken ct)
    {
        var connessione = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = percorso,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        await connessione.OpenAsync(ct);
        return connessione;
    }

    private static SqliteCommand Comando(SqliteConnection connessione, string sql, int aziendaId)
    {
        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.Parameters.AddWithValue("$azienda", aziendaId);
        return comando;
    }
}
