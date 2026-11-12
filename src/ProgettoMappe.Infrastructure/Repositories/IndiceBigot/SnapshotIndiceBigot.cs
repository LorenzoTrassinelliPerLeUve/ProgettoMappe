using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.Repositories.IndiceBigot;

/// <summary>Sezione <c>IndiceBigot</c> dell'Api: snapshot notturno del report «Indice Bigot» (task #189).</summary>
public sealed class IndiceBigotOptions
{
    public const string Sezione = "IndiceBigot";

    /// <summary>
    /// File SQLite dello snapshot (<c>~</c> = cartella dell'utente dell'Api). Se la cartella non è
    /// scrivibile (anteprima) si usa la cartella temporanea, con un Warning.
    /// </summary>
    public string Database { get; set; } = "~/progetti/mappe/dati/indice-bigot.db";

    /// <summary>Ora italiana della lettura notturna da 4Grapes (0–23).</summary>
    public int OraNotturna { get; set; } = 3;

    /// <summary>
    /// Snapshot preparato da fuori (stesso schema), che l'Api legge soltanto: si usa se è più recente del
    /// suo. Serve quando l'Api non può leggere le viste (permesso mancante) o in anteprima. Vuoto = nessuno.
    /// </summary>
    public string Esterno { get; set; } = "";
}

public sealed record EnteIndice(int Id, string Nome);

public sealed record VignaIndice(int Id, int IdEnte, string Nome);

public sealed record VitignoIndice(int Id, string Nome);

public sealed record VignetoIndice(int Id, string Nome, int? IdVigna, int? IdVitigno, double? Superficie, int? AnnoImpianto);

public sealed record SottovignetoIndice(int Id, string Nome, int IdVigneto);

/// <summary>Una riga di VistaIndicePerParametro (IdSubVigneto null) o di VistaIndicePerParametro_Sub.</summary>
public sealed record RigaIndice(int Anno, int IdVigneto, int? IdSubVigneto, string IdParametro,
    double? Min, double? Max, double? Avg, double? Valore, double? Score, double? Punteggio);

/// <summary>Tutto lo snapshot, come letto da 4Grapes.</summary>
public sealed record DatiIndiceBigot(
    IReadOnlyList<EnteIndice> Enti,
    IReadOnlyList<VignaIndice> Vigne,
    IReadOnlyList<VitignoIndice> Vitigni,
    IReadOnlyList<VignetoIndice> Vigneti,
    IReadOnlyList<SottovignetoIndice> Sottovigneti,
    IReadOnlyList<RigaIndice> Righe);

/// <summary>La parte dello snapshot di un'azienda: anagrafica e righe dei suoi vigneti e sottovigneti.</summary>
public sealed record IndiceAzienda(
    DateTimeOffset Snapshot,
    EnteIndice? Ente,
    IReadOnlyList<VignaIndice> Vigne,
    IReadOnlyList<VitignoIndice> Vitigni,
    IReadOnlyList<VignetoIndice> Vigneti,
    IReadOnlyList<SottovignetoIndice> Sottovigneti,
    IReadOnlyList<RigaIndice> Righe);

/// <summary>
/// Snapshot del report «Indice Bigot» in un file SQLite di Mappe (task #189): lo scrive di notte
/// <c>AggiornamentoSnapshotIndice</c> dell'Api, la pagina lo legge per azienda senza mai toccare
/// 4Grapes. Il file nuovo si scrive accanto e poi sostituisce il vecchio (chi legge vede sempre uno
/// snapshot intero). Se la cartella configurata non è scrivibile si usa la cartella temporanea.
/// </summary>
public sealed class SnapshotIndiceBigot
{
    private readonly ILogger<SnapshotIndiceBigot> _logger;
    private readonly string? _esterno;
    private string _percorso;

    public SnapshotIndiceBigot(IOptions<IndiceBigotOptions> opzioni, ILogger<SnapshotIndiceBigot> logger)
    {
        _logger = logger;
        _percorso = LettoreNdvi.Espandi(opzioni.Value.Database);
        _esterno = opzioni.Value.Esterno is { Length: > 0 } e ? LettoreNdvi.Espandi(e) : null;
        if (!File.Exists(_percorso) && File.Exists(PercorsoTemporaneo))
        {
            _percorso = PercorsoTemporaneo;
        }
    }

    private static string PercorsoTemporaneo => Path.Combine(Path.GetTempPath(), "mappe-indice-bigot.db");

    /// <summary>Dove l'Api scrive lo snapshot.</summary>
    public string Percorso => _percorso;

    /// <summary>
    /// Il file da leggere: il più recente (per <c>info.letto_il</c>) fra quello dell'Api e quello esterno;
    /// null se non ce n'è nessuno leggibile.
    /// </summary>
    private async Task<(string Percorso, DateTimeOffset Snapshot, int Righe)?> InUsoAsync(CancellationToken ct)
    {
        (string, DateTimeOffset, int)? migliore = null;
        foreach (var percorso in new[] { _percorso, _esterno })
        {
            if (percorso is null || !File.Exists(percorso))
            {
                continue;
            }
            try
            {
                await using var connessione = await ApriAsync(percorso, SqliteOpenMode.ReadOnly, ct);
                await using var comando = connessione.CreateCommand();
                comando.CommandText = "SELECT letto_il, (SELECT COUNT(*) FROM riga) FROM info";
                await using var reader = await comando.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct)
                    && DateTimeOffset.TryParse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var letto)
                    && (migliore is null || letto > migliore.Value.Item2))
                {
                    migliore = (percorso, letto, reader.GetInt32(1));
                }
            }
            catch (SqliteException ex)
            {
                _logger.LogWarning("Snapshot Indice Bigot {Percorso} non leggibile: {Messaggio}", percorso, ex.Message);
            }
        }
        return migliore;
    }

    /// <summary>Ora dello snapshot in uso e numero di righe; null se non c'è ancora.</summary>
    public async Task<(DateTimeOffset Snapshot, int Righe)?> InfoAsync(CancellationToken ct) =>
        await InUsoAsync(ct) is { } inUso ? (inUso.Snapshot, inUso.Righe) : null;

    /// <summary>Scrive uno snapshot nuovo al posto di quello in uso.</summary>
    public async Task ScriviAsync(DatiIndiceBigot dati, DateTimeOffset lettoIl, CancellationToken ct)
    {
        try
        {
            await ScriviInAsync(_percorso, dati, lettoIl, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException
                                   && _percorso != PercorsoTemporaneo)
        {
            _logger.LogWarning("Snapshot Indice Bigot: {Percorso} non scrivibile ({Messaggio}), uso {Temporaneo}",
                _percorso, ex.Message, PercorsoTemporaneo);
            _percorso = PercorsoTemporaneo;
            await ScriviInAsync(_percorso, dati, lettoIl, ct);
        }
    }

    private static async Task ScriviInAsync(string percorso, DatiIndiceBigot dati, DateTimeOffset lettoIl, CancellationToken ct)
    {
        var cartella = Path.GetDirectoryName(percorso);
        if (!string.IsNullOrEmpty(cartella))
        {
            Directory.CreateDirectory(cartella);
        }

        var nuovo = percorso + ".nuovo";
        File.Delete(nuovo);
        await using (var connessione = await ApriAsync(nuovo, SqliteOpenMode.ReadWriteCreate, ct))
        {
            await using var transazione = (SqliteTransaction)await connessione.BeginTransactionAsync(ct);
            await EseguiAsync(connessione, """
                CREATE TABLE info (letto_il TEXT NOT NULL);
                CREATE TABLE ente (id INTEGER PRIMARY KEY, nome TEXT NOT NULL);
                CREATE TABLE vigna (id INTEGER PRIMARY KEY, id_ente INTEGER NOT NULL, nome TEXT NOT NULL);
                CREATE TABLE vitigno (id INTEGER PRIMARY KEY, nome TEXT NOT NULL);
                CREATE TABLE vigneto (id INTEGER PRIMARY KEY, nome TEXT NOT NULL, id_vigna INTEGER, id_vitigno INTEGER,
                    superficie REAL, anno_impianto INTEGER);
                CREATE TABLE sottovigneto (id INTEGER PRIMARY KEY, nome TEXT NOT NULL, id_vigneto INTEGER NOT NULL);
                CREATE TABLE riga (anno INTEGER NOT NULL, id_vigneto INTEGER NOT NULL, id_sottovigneto INTEGER,
                    id_parametro TEXT NOT NULL, min REAL, max REAL, avg REAL, valore REAL, score REAL, punteggio REAL);
                CREATE INDEX vigna_ente ON vigna (id_ente);
                CREATE INDEX vigneto_vigna ON vigneto (id_vigna);
                CREATE INDEX riga_vigneto ON riga (id_vigneto);
                """, ct);

            await using (var info = connessione.CreateCommand())
            {
                info.CommandText = "INSERT INTO info VALUES ($t)";
                info.Parameters.AddWithValue("$t", lettoIl.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                await info.ExecuteNonQueryAsync(ct);
            }

            await InserisciAsync(connessione, "ente", dati.Enti, e => [e.Id, e.Nome], ct);
            await InserisciAsync(connessione, "vigna", dati.Vigne, v => [v.Id, v.IdEnte, v.Nome], ct);
            await InserisciAsync(connessione, "vitigno", dati.Vitigni, v => [v.Id, v.Nome], ct);
            await InserisciAsync(connessione, "vigneto", dati.Vigneti,
                v => [v.Id, v.Nome, v.IdVigna, v.IdVitigno, v.Superficie, v.AnnoImpianto], ct);
            await InserisciAsync(connessione, "sottovigneto", dati.Sottovigneti, s => [s.Id, s.Nome, s.IdVigneto], ct);
            await InserisciAsync(connessione, "riga", dati.Righe,
                r => [r.Anno, r.IdVigneto, r.IdSubVigneto, r.IdParametro, r.Min, r.Max, r.Avg, r.Valore, r.Score, r.Punteggio], ct);
            await transazione.CommitAsync(ct);
        }

        File.Move(nuovo, percorso, overwrite: true);
    }

    /// <summary>Anagrafica e righe dell'azienda; null se non c'è ancora uno snapshot.</summary>
    public async Task<IndiceAzienda?> AziendaAsync(int idEnte, CancellationToken ct)
    {
        if (await InUsoAsync(ct) is not { } inUso)
        {
            return null;
        }

        await using var connessione = await ApriAsync(inUso.Percorso, SqliteOpenMode.ReadOnly, ct);
        var info = await LeggiAsync(connessione, "SELECT letto_il FROM info", null,
            r => DateTimeOffset.Parse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), ct);
        if (info.Count == 0)
        {
            return null;
        }

        var ente = (await LeggiAsync(connessione, "SELECT id, nome FROM ente WHERE id = $e", idEnte,
            r => new EnteIndice(r.GetInt32(0), r.GetString(1)), ct)).FirstOrDefault();
        var vigne = await LeggiAsync(connessione, "SELECT id, id_ente, nome FROM vigna WHERE id_ente = $e ORDER BY nome", idEnte,
            r => new VignaIndice(r.GetInt32(0), r.GetInt32(1), r.GetString(2)), ct);
        var vigneti = await LeggiAsync(connessione, """
            SELECT v.id, v.nome, v.id_vigna, v.id_vitigno, v.superficie, v.anno_impianto
            FROM vigneto v JOIN vigna g ON g.id = v.id_vigna WHERE g.id_ente = $e ORDER BY v.nome
            """, idEnte,
            r => new VignetoIndice(r.GetInt32(0), r.GetString(1), Intero(r, 2), Intero(r, 3), Reale(r, 4), Intero(r, 5)), ct);
        var vitigni = await LeggiAsync(connessione, """
            SELECT DISTINCT t.id, t.nome FROM vitigno t JOIN vigneto v ON v.id_vitigno = t.id
            JOIN vigna g ON g.id = v.id_vigna WHERE g.id_ente = $e ORDER BY t.nome
            """, idEnte, r => new VitignoIndice(r.GetInt32(0), r.GetString(1)), ct);
        var sottovigneti = await LeggiAsync(connessione, """
            SELECT s.id, s.nome, s.id_vigneto FROM sottovigneto s JOIN vigneto v ON v.id = s.id_vigneto
            JOIN vigna g ON g.id = v.id_vigna WHERE g.id_ente = $e ORDER BY s.nome
            """, idEnte, r => new SottovignetoIndice(r.GetInt32(0), r.GetString(1), r.GetInt32(2)), ct);
        var righe = await LeggiAsync(connessione, """
            SELECT r.anno, r.id_vigneto, r.id_sottovigneto, r.id_parametro, r.min, r.max, r.avg, r.valore, r.score, r.punteggio
            FROM riga r JOIN vigneto v ON v.id = r.id_vigneto JOIN vigna g ON g.id = v.id_vigna WHERE g.id_ente = $e
            """, idEnte,
            r => new RigaIndice(r.GetInt32(0), r.GetInt32(1), Intero(r, 2), r.GetString(3),
                Reale(r, 4), Reale(r, 5), Reale(r, 6), Reale(r, 7), Reale(r, 8), Reale(r, 9)), ct);

        return new IndiceAzienda(info[0], ente, vigne, vitigni, vigneti, sottovigneti, righe);
    }

    private static int? Intero(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt32(i);

    private static double? Reale(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);

    private static async Task<SqliteConnection> ApriAsync(string percorso, SqliteOpenMode modo, CancellationToken ct)
    {
        var stringa = new SqliteConnectionStringBuilder { DataSource = percorso, Mode = modo, Pooling = false };
        var connessione = new SqliteConnection(stringa.ToString());
        await connessione.OpenAsync(ct);
        return connessione;
    }

    private static async Task EseguiAsync(SqliteConnection connessione, string sql, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync(ct);
    }

    private static async Task InserisciAsync<T>(SqliteConnection connessione, string tabella, IEnumerable<T> righe,
        Func<T, object?[]> valori, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        SqliteParameter[]? parametri = null;
        foreach (var riga in righe)
        {
            var v = valori(riga);
            if (parametri is null)
            {
                parametri = v.Select((_, i) =>
                {
                    var p = comando.CreateParameter();
                    p.ParameterName = "$p" + i.ToString(CultureInfo.InvariantCulture);
                    comando.Parameters.Add(p);
                    return p;
                }).ToArray();
                comando.CommandText = $"INSERT INTO {tabella} VALUES ({string.Join(", ", parametri.Select(p => p.ParameterName))})";
            }
            for (var i = 0; i < v.Length; i++)
            {
                parametri[i].Value = v[i] ?? DBNull.Value;
            }
            await comando.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task<List<T>> LeggiAsync<T>(SqliteConnection connessione, string sql, int? idEnte,
        Func<SqliteDataReader, T> leggi, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        if (idEnte is { } e)
        {
            comando.Parameters.AddWithValue("$e", e);
        }
        var risultato = new List<T>();
        await using var reader = await comando.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            risultato.Add(leggi(reader));
        }
        return risultato;
    }
}
