using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

/// <summary>Sezione <c>BilancioIdrico</c> dell'Api: dove sta il database SQLite della configurazione del DSS.</summary>
public sealed class BilancioIdricoOptions
{
    public const string Sezione = "BilancioIdrico";

    /// <summary>Percorso del file; <c>~</c> all'inizio è la cartella dell'utente che fa girare l'Api.</summary>
    public string Database { get; set; } = "~/progetti/mappe/dati/bilancio-idrico.db";
}

/// <summary>
/// Una voce della configurazione: <see cref="Tipo"/> (vedi <see cref="TipiVoce"/>), <see cref="Chiave"/> unica nel tipo,
/// azienda (0 = di tutte, come le tabelle del modello) e il record in <see cref="Dati"/>; <c>null</c> = voce tolta.
/// </summary>
public sealed record VoceConfigurazione(string Tipo, string Chiave, int IdEnte, object? Dati)
{
    public static VoceConfigurazione Di(SettoreIrriguo s) => new(TipiVoce.Settore, Inv(s.IdSettore), s.IdEnte, s);
    public static VoceConfigurazione Di(int idEnte, QuotaVignetoSettore q) => new(TipiVoce.Quota, $"{Inv(q.IdSettore)}:{Inv(q.IdVigneto)}", idEnte, q);
    public static VoceConfigurazione Di(int idEnte, ImpiantoSettore i) => new(TipiVoce.Impianto, $"{Inv(i.IdSettore)}:{i.ValidoDal:yyyy-MM-dd}", idEnte, i);
    public static VoceConfigurazione Di(ShiftIrriguo s) => new(TipiVoce.Shift, Inv(s.IdShift), s.IdEnte, s);
    public static VoceConfigurazione Di(int idEnte, SettoreNelloShift s) =>
        new(TipiVoce.SettoreShift, $"{Inv(s.IdShift)}:{Inv(s.IdSettore)}:{s.ValidoDal:yyyy-MM-dd}", idEnte, s);
    public static VoceConfigurazione Di(int idEnte, StileVinoVigneto s) => new(TipiVoce.Stile, $"{Inv(s.IdVigneto)}:{s.ValidoDal:yyyy-MM-dd}", idEnte, s);
    public static VoceConfigurazione Di(IrrigazioneRegistrata i) => new(TipiVoce.Irrigazione, Inv(i.IdEvento), i.IdEnte, i);
    public static VoceConfigurazione Di(ModelloBilancio m) => new(TipiVoce.Modello, "modello", 0, m);

    /// <summary>
    /// La voce di un record arrivato in JSON (Api); null se il tipo non esiste o il record non si legge. L'azienda dei record
    /// che la portano (settore, shift, irrigazione) deve essere <paramref name="idEnte"/>.
    /// </summary>
    public static VoceConfigurazione? DaJson(string tipo, int idEnte, JsonElement json)
    {
        if (!TipiVoce.Record.TryGetValue(tipo, out var tipoRecord))
            return null;
        object? dati;
        try
        {
            dati = json.Deserialize(tipoRecord, ArchivioConfigurazioneBilancio.Json);
        }
        catch (JsonException)
        {
            return null;
        }
        return dati switch
        {
            SettoreIrriguo s when s.IdEnte == idEnte => Di(s),
            QuotaVignetoSettore q when q.Percentuale is >= 0 and <= 100 => Di(idEnte, q),
            ImpiantoSettore i => Di(idEnte, i),
            ShiftIrriguo s when s.IdEnte == idEnte => Di(s),
            SettoreNelloShift s => Di(idEnte, s),
            StileVinoVigneto s when s.IdStileVino > 0 => Di(idEnte, s),
            IrrigazioneRegistrata i when i.IdEnte == idEnte && (i.IdSettore is null) != (i.IdShift is null) => Di(i),
            ModelloBilancio m => Di(m),
            _ => null,
        };
    }

    private static string Inv(int n) => n.ToString(CultureInfo.InvariantCulture);
}

public static class TipiVoce
{
    public const string Settore = "settore";
    public const string Quota = "quota";
    public const string Impianto = "impianto";
    public const string Shift = "shift";
    public const string SettoreShift = "settore-shift";
    public const string Stile = "stile";
    public const string Irrigazione = "irrigazione";
    public const string Modello = "modello";

    /// <summary>Il tipo .NET del record di ogni tipo di voce.</summary>
    public static readonly IReadOnlyDictionary<string, Type> Record = new Dictionary<string, Type>
    {
        [Settore] = typeof(SettoreIrriguo),
        [Quota] = typeof(QuotaVignetoSettore),
        [Impianto] = typeof(ImpiantoSettore),
        [Shift] = typeof(ShiftIrriguo),
        [SettoreShift] = typeof(SettoreNelloShift),
        [Stile] = typeof(StileVinoVigneto),
        [Irrigazione] = typeof(IrrigazioneRegistrata),
        [Modello] = typeof(ModelloBilancio),
    };
}

/// <summary>Una versione di una voce, con chi l'ha scritta e quando (UTC).</summary>
public sealed record VersioneVoce(string Tipo, string Chiave, int IdEnte, int Versione, string? DatiJson, string Autore,
    DateTime CreatoIlUtc, string? Nota);

/// <summary>
/// Configurazione del DSS Bilancio Idrico (task #333): un database SQLite di Mappe come i filari (<c>Disegni</c>), mai
/// 4Grapes. Una tabella sola, <c>voci</c>: ogni modifica è una riga nuova con versione = ultima + 1, autore e data; la
/// voce valida è la versione più alta, se non è una cancellazione (<c>dati</c> NULL). Lo storico resta tutto.
/// File e tabella nascono alla prima scrittura; senza file la configurazione è vuota.
/// </summary>
public sealed class ArchivioConfigurazioneBilancio
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly string _percorso;
    private readonly SemaphoreSlim _scrittura = new(1, 1);

    public ArchivioConfigurazioneBilancio(IOptions<BilancioIdricoOptions> opzioni) : this(opzioni.Value.Database)
    {
    }

    public ArchivioConfigurazioneBilancio(string percorso)
    {
        _percorso = LettoreNdvi.Espandi(percorso);
    }

    public string Percorso => _percorso;

    /// <summary>La configurazione valida di un'azienda, con le tabelle del modello (voci di tutte le aziende).</summary>
    public async Task<ConfigurazioneAzienda> LeggiAsync(int idEnte, CancellationToken ct = default)
    {
        var voci = await UltimeAsync(idEnte, ct);
        List<T> Di<T>(string tipo) => voci.Where(v => v.Tipo == tipo && v.DatiJson is not null)
            .Select(v => JsonSerializer.Deserialize<T>(v.DatiJson!, Json)!).ToList();
        return new ConfigurazioneAzienda
        {
            IdEnte = idEnte,
            Settori = Di<SettoreIrriguo>(TipiVoce.Settore).OrderBy(s => s.IdSettore).ToList(),
            Quote = Di<QuotaVignetoSettore>(TipiVoce.Quota),
            Impianti = Di<ImpiantoSettore>(TipiVoce.Impianto),
            Shift = Di<ShiftIrriguo>(TipiVoce.Shift),
            SettoriShift = Di<SettoreNelloShift>(TipiVoce.SettoreShift),
            Stili = Di<StileVinoVigneto>(TipiVoce.Stile),
            Irrigazioni = Di<IrrigazioneRegistrata>(TipiVoce.Irrigazione).OrderBy(i => i.Data).ToList(),
            Modello = Di<ModelloBilancio>(TipiVoce.Modello).FirstOrDefault(),
        };
    }

    /// <summary>L'ultima versione di ogni voce dell'azienda e delle voci comuni (azienda 0), anche le cancellate.</summary>
    public Task<IReadOnlyList<VersioneVoce>> UltimeAsync(int idEnte, CancellationToken ct = default) =>
        LeggiAsync(c =>
        {
            c.CommandText = """
                SELECT tipo, chiave, id_ente, versione, dati, autore, creato_il, nota FROM voci v
                WHERE v.id_ente IN ($ente, 0)
                  AND v.versione = (SELECT MAX(versione) FROM voci u WHERE u.tipo = v.tipo AND u.chiave = v.chiave)
                ORDER BY tipo, chiave
                """;
            c.Parameters.AddWithValue("$ente", idEnte);
        }, ct);

    /// <summary>Tutte le versioni di una voce, dalla più recente.</summary>
    public Task<IReadOnlyList<VersioneVoce>> StoricoAsync(string tipo, string chiave, CancellationToken ct = default) =>
        LeggiAsync(c =>
        {
            c.CommandText = "SELECT tipo, chiave, id_ente, versione, dati, autore, creato_il, nota FROM voci WHERE tipo = $t AND chiave = $k ORDER BY versione DESC";
            c.Parameters.AddWithValue("$t", tipo);
            c.Parameters.AddWithValue("$k", chiave);
        }, ct);

    /// <summary>Il numero più alto usato per settori, shift o irrigazioni (per darne uno nuovo).</summary>
    public async Task<int> UltimoIdAsync(string tipo, CancellationToken ct = default)
    {
        if (!File.Exists(_percorso))
            return 0;
        await using var conn = await ApriAsync(SqliteOpenMode.ReadOnly, ct);
        await using var c = conn.CreateCommand();
        c.CommandText = "SELECT COALESCE(MAX(CAST(chiave AS INTEGER)), 0) FROM voci WHERE tipo = $t";
        c.Parameters.AddWithValue("$t", tipo);
        return Convert.ToInt32(await c.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Scrive le voci in una transazione: ciascuna diventa la versione successiva della sua chiave. Una voce uguale
    /// all'ultima versione non si riscrive. Restituisce quante versioni nuove ha scritto.
    /// </summary>
    public async Task<int> SalvaAsync(IEnumerable<VoceConfigurazione> voci, string autore, string? nota, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(autore))
            throw new ArgumentException("Serve l'autore della modifica.", nameof(autore));
        var elenco = voci.ToList();
        foreach (var v in elenco)
            if (!TipiVoce.Record.TryGetValue(v.Tipo, out var tipo) || (v.Dati is not null && !tipo.IsInstanceOfType(v.Dati)))
                throw new ArgumentException($"Voce non valida: {v.Tipo} {v.Chiave}.", nameof(voci));

        await _scrittura.WaitAsync(ct);
        try
        {
            var cartella = Path.GetDirectoryName(_percorso);
            if (!string.IsNullOrEmpty(cartella))
                Directory.CreateDirectory(cartella);
            await using var conn = await ApriAsync(SqliteOpenMode.ReadWriteCreate, ct);
            await using (var crea = conn.CreateCommand())
            {
                crea.CommandText = """
                    CREATE TABLE IF NOT EXISTS voci (
                        tipo TEXT NOT NULL, chiave TEXT NOT NULL, id_ente INTEGER NOT NULL, versione INTEGER NOT NULL,
                        dati TEXT, autore TEXT NOT NULL, creato_il TEXT NOT NULL, nota TEXT,
                        PRIMARY KEY (tipo, chiave, versione));
                    CREATE INDEX IF NOT EXISTS voci_ente ON voci (id_ente, tipo);
                    """;
                await crea.ExecuteNonQueryAsync(ct);
            }

            await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
            var adesso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var scritte = 0;
            foreach (var v in elenco)
            {
                var json = v.Dati is null ? null : JsonSerializer.Serialize(v.Dati, TipiVoce.Record[v.Tipo], Json);
                await using var ultima = conn.CreateCommand();
                ultima.Transaction = tx;
                ultima.CommandText = "SELECT versione, dati FROM voci WHERE tipo = $t AND chiave = $k ORDER BY versione DESC LIMIT 1";
                ultima.Parameters.AddWithValue("$t", v.Tipo);
                ultima.Parameters.AddWithValue("$k", v.Chiave);
                int versione = 0;
                string? datiUltima = null;
                await using (var r = await ultima.ExecuteReaderAsync(ct))
                    if (await r.ReadAsync(ct))
                    {
                        versione = r.GetInt32(0);
                        datiUltima = r.IsDBNull(1) ? null : r.GetString(1);
                    }
                if ((versione > 0 && datiUltima == json) || (versione == 0 && json is null))
                    continue;

                await using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO voci (tipo, chiave, id_ente, versione, dati, autore, creato_il, nota)
                    VALUES ($t, $k, $e, $v, $d, $a, $c, $n)
                    """;
                ins.Parameters.AddWithValue("$t", v.Tipo);
                ins.Parameters.AddWithValue("$k", v.Chiave);
                ins.Parameters.AddWithValue("$e", v.IdEnte);
                ins.Parameters.AddWithValue("$v", versione + 1);
                ins.Parameters.AddWithValue("$d", (object?)json ?? DBNull.Value);
                ins.Parameters.AddWithValue("$a", autore);
                ins.Parameters.AddWithValue("$c", adesso);
                ins.Parameters.AddWithValue("$n", (object?)nota ?? DBNull.Value);
                await ins.ExecuteNonQueryAsync(ct);
                scritte++;
            }

            await tx.CommitAsync(ct);
            return scritte;
        }
        finally
        {
            _scrittura.Release();
        }
    }

    private async Task<IReadOnlyList<VersioneVoce>> LeggiAsync(Action<SqliteCommand> prepara, CancellationToken ct)
    {
        if (!File.Exists(_percorso))
            return [];
        await using var conn = await ApriAsync(SqliteOpenMode.ReadOnly, ct);
        await using var c = conn.CreateCommand();
        prepara(c);
        var risultato = new List<VersioneVoce>();
        try
        {
            await using var r = await c.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                risultato.Add(new VersioneVoce(r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3),
                    r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5),
                    DateTime.Parse(r.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                    r.IsDBNull(7) ? null : r.GetString(7)));
        }
        catch (SqliteException e) when (e.SqliteErrorCode == 1 && e.Message.Contains("no such table", StringComparison.Ordinal))
        {
            return [];
        }
        return risultato;
    }

    private async Task<SqliteConnection> ApriAsync(SqliteOpenMode modo, CancellationToken ct)
    {
        var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _percorso, Mode = modo, Pooling = false }.ToString());
        await conn.OpenAsync(ct);
        return conn;
    }
}
