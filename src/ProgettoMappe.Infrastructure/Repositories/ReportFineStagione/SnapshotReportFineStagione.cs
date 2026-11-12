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
