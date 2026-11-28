using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.Reportistica;

/// <summary>Sezione <c>Reportistica</c> dell'Api: dove sta il database SQLite delle scelte, dei commenti e delle pubblicazioni.</summary>
public sealed class ReportisticaOptions
{
    public const string Sezione = "Reportistica";

    /// <summary>Percorso del file; <c>~</c> all'inizio è la cartella dell'utente che fa girare l'Api.</summary>
    public string Database { get; set; } = "~/progetti/mappe/dati/reportistica.db";

    /// <summary>Dove scrivere se la cartella di <see cref="Database"/> non è scrivibile (l'anteprima con l'appsettings della produzione).</summary>
    public string DatabaseRipiego { get; set; } = "~/.local/share/mappe/reportistica.db";

    public string PercorsoScelto() =>
        new BilancioIdricoOptions { Database = Database, DatabaseRipiego = DatabaseRipiego }.PercorsoScelto();
}

/// <summary>
/// L'archivio della reportistica (task #416, tappa R2; docs/specifiche/reportistica/PROGETTO.md §3.5): un SQLite di Mappe, non
/// 4Grapes. Come filari e bilancio idrico, ogni modifica è una riga nuova con la sua versione (mai UPDATE sul passato), così
/// resta lo storico di chi ha cambiato cosa; l'unico UPDATE è il ritiro di una pubblicazione. Edizione <c>""</c> nelle scelte =
/// predefinita dell'azienda. Il file e le tabelle nascono alla prima scrittura; senza file le letture sono vuote.
/// </summary>
public sealed partial class ArchivioReportistica(IOptions<ReportisticaOptions> opzioni)
{
    private readonly string _percorso = opzioni.Value.PercorsoScelto();
    private readonly SemaphoreSlim _scrittura = new(1, 1);

    public string Percorso => _percorso;

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS scelte (
            report TEXT NOT NULL, id_ente INTEGER NOT NULL, edizione TEXT NOT NULL, id_elemento TEXT NOT NULL,
            versione INTEGER NOT NULL, visibile INTEGER NOT NULL, autore TEXT NOT NULL, creato_il TEXT NOT NULL,
            PRIMARY KEY (report, id_ente, edizione, id_elemento, versione));
        CREATE TABLE IF NOT EXISTS commenti (
            report TEXT NOT NULL, id_ente INTEGER NOT NULL, edizione TEXT NOT NULL, id_sezione TEXT NOT NULL, lingua TEXT NOT NULL,
            versione INTEGER NOT NULL, testo TEXT NOT NULL, autore TEXT NOT NULL, creato_il TEXT NOT NULL,
            PRIMARY KEY (report, id_ente, edizione, id_sezione, lingua, versione));
        CREATE TABLE IF NOT EXISTS stati (
            report TEXT NOT NULL, id_ente INTEGER NOT NULL, edizione TEXT NOT NULL, versione INTEGER NOT NULL,
            stato TEXT NOT NULL, autore TEXT NOT NULL, creato_il TEXT NOT NULL,
            PRIMARY KEY (report, id_ente, edizione, versione));
        CREATE TABLE IF NOT EXISTS pubblicazioni (
            report TEXT NOT NULL, id_ente INTEGER NOT NULL, edizione TEXT NOT NULL, lingua TEXT NOT NULL, versione INTEGER NOT NULL,
            pubblicato_il TEXT NOT NULL, autore TEXT NOT NULL, dati_al TEXT, nota TEXT, nome_file TEXT NOT NULL,
            contenuto TEXT NOT NULL, pptx BLOB NOT NULL, impronta_sha256 TEXT NOT NULL, ritirata_il TEXT, ritirata_da TEXT,
            PRIMARY KEY (report, id_ente, edizione, lingua, versione));
        CREATE TABLE IF NOT EXISTS obiettivi_bigot (
            id_ente INTEGER NOT NULL, id_vigneto INTEGER NOT NULL, anno INTEGER NOT NULL, parametro TEXT NOT NULL, versione INTEGER NOT NULL,
            minimo REAL, obiettivo REAL, massimo REAL, autore TEXT NOT NULL, creato_il TEXT NOT NULL,
            PRIMARY KEY (id_ente, id_vigneto, anno, parametro, versione));
        """;

    /// <summary>L'edizione con stato, ultime scelte (anche le predefinite), ultimi commenti e pubblicazioni (senza i file).</summary>
    public async Task<EdizioneReportDto> EdizioneAsync(string report, int idEnte, string edizione, CancellationToken ct)
    {
        var vuota = new EdizioneReportDto(report, idEnte, edizione, StatiReport.Bozza, null, null, [], [], [], false);
        await using var c = await ApriAsync(sola: true, ct);
        if (c is null)
            return vuota;

        var scelte = new List<SceltaReportDto>();
        await using (var q = Comando(c, """
            SELECT id_elemento, visibile, edizione, versione, autore, creato_il FROM scelte s
            WHERE report = $r AND id_ente = $e AND edizione IN ($ed, '')
              AND versione = (SELECT MAX(versione) FROM scelte u WHERE u.report = s.report AND u.id_ente = s.id_ente
                              AND u.edizione = s.edizione AND u.id_elemento = s.id_elemento)
            ORDER BY edizione, id_elemento
            """, report, idEnte, edizione))
        await using (var l = await q.ExecuteReaderAsync(ct))
            while (await l.ReadAsync(ct))
                scelte.Add(new SceltaReportDto(l.GetString(0), l.GetInt64(1) != 0, l.GetString(2).Length == 0, l.GetInt32(3), l.GetString(4), Data(l.GetString(5))));

        var commenti = new List<CommentoReportDto>();
        await using (var q = Comando(c, """
            SELECT id_sezione, lingua, testo, versione, autore, creato_il FROM commenti s
            WHERE report = $r AND id_ente = $e AND edizione = $ed
              AND versione = (SELECT MAX(versione) FROM commenti u WHERE u.report = s.report AND u.id_ente = s.id_ente
                              AND u.edizione = s.edizione AND u.id_sezione = s.id_sezione AND u.lingua = s.lingua)
            ORDER BY id_sezione, lingua
            """, report, idEnte, edizione))
        await using (var l = await q.ExecuteReaderAsync(ct))
            while (await l.ReadAsync(ct))
                if (l.GetString(2).Length > 0)
                    commenti.Add(new CommentoReportDto(l.GetString(0), l.GetString(1), l.GetString(2), l.GetInt32(3), l.GetString(4), Data(l.GetString(5))));

        string stato = StatiReport.Bozza;
        string? statoAutore = null;
        DateTimeOffset? statoIl = null;
        await using (var q = Comando(c, "SELECT stato, autore, creato_il FROM stati WHERE report = $r AND id_ente = $e AND edizione = $ed ORDER BY versione DESC LIMIT 1",
                         report, idEnte, edizione))
        await using (var l = await q.ExecuteReaderAsync(ct))
            if (await l.ReadAsync(ct))
                (stato, statoAutore, statoIl) = (l.GetString(0), l.GetString(1), Data(l.GetString(2)));

        var pubblicazioni = await PubblicazioniAsync(c, report, idEnte, edizione, ct);
        var ultima = pubblicazioni.Where(p => p.RitirataIl is null).Select(p => (DateTimeOffset?)p.PubblicatoIl).Max();
        var modifiche = ultima is { } u && (scelte.Any(s => !s.Predefinita && s.CreatoIl > u) || commenti.Any(m => m.CreatoIl > u));
        return new EdizioneReportDto(report, idEnte, edizione, stato, statoAutore, statoIl, scelte, commenti, pubblicazioni, modifiche);
    }

    /// <summary>L'ultima pubblicazione non ritirata nella lingua (quella del cliente), o una versione precisa; null se non c'è.</summary>
    public async Task<ContenutoPubblicatoDto?> ContenutoAsync(string report, int idEnte, string edizione, string lingua, int? versione,
        CancellationToken ct)
    {
        await using var c = await ApriAsync(sola: true, ct);
        if (c is null)
            return null;
        await using var q = Comando(c, $"""
            SELECT {ColonnePubblicazione}, contenuto FROM pubblicazioni
            WHERE report = $r AND id_ente = $e AND edizione = $ed AND lingua = $l
              AND {(versione is null ? "ritirata_il IS NULL" : "versione = $v")}
            ORDER BY versione DESC LIMIT 1
            """, report, idEnte, edizione);
        q.Parameters.AddWithValue("$l", lingua);
        q.Parameters.AddWithValue("$v", versione ?? 0);
        await using var l = await q.ExecuteReaderAsync(ct);
        return await l.ReadAsync(ct) ? new ContenutoPubblicatoDto(Pubblicazione(l), l.GetString(10)) : null;
    }

    /// <summary>Il PowerPoint salvato di una versione; null se non c'è.</summary>
    public async Task<(PubblicazioneReportDto Pubblicazione, byte[] Pptx)?> PptxAsync(string report, int idEnte, string edizione, string lingua,
        int versione, CancellationToken ct)
    {
        await using var c = await ApriAsync(sola: true, ct);
        if (c is null)
            return null;
        await using var q = Comando(c, $"""
            SELECT {ColonnePubblicazione}, pptx FROM pubblicazioni
            WHERE report = $r AND id_ente = $e AND edizione = $ed AND lingua = $l AND versione = $v
            """, report, idEnte, edizione);
        q.Parameters.AddWithValue("$l", lingua);
        q.Parameters.AddWithValue("$v", versione);
        await using var l = await q.ExecuteReaderAsync(ct);
        return await l.ReadAsync(ct) ? (Pubblicazione(l), (byte[])l.GetValue(10)) : null;
    }

    public Task SalvaSceltaAsync(string report, int idEnte, string edizione, string idElemento, bool visibile, string autore,
        DateTimeOffset adesso, CancellationToken ct) =>
        ScriviAsync(async (c, t) =>
        {
            await using var q = Comando(c, """
                INSERT INTO scelte (report, id_ente, edizione, id_elemento, versione, visibile, autore, creato_il)
                SELECT $r, $e, $ed, $id, COALESCE(MAX(versione), 0) + 1, $vis, $a, $il FROM scelte
                WHERE report = $r AND id_ente = $e AND edizione = $ed AND id_elemento = $id
                """, report, idEnte, edizione, t);
            q.Parameters.AddWithValue("$id", idElemento);
            q.Parameters.AddWithValue("$vis", visibile ? 1 : 0);
            Autore(q, autore, adesso);
            await q.ExecuteNonQueryAsync(ct);
        }, ct);

    public Task SalvaCommentoAsync(string report, int idEnte, string edizione, string idSezione, string lingua, string testo, string autore,
        DateTimeOffset adesso, CancellationToken ct) =>
        ScriviAsync(async (c, t) =>
        {
            await using var q = Comando(c, """
                INSERT INTO commenti (report, id_ente, edizione, id_sezione, lingua, versione, testo, autore, creato_il)
                SELECT $r, $e, $ed, $id, $l, COALESCE(MAX(versione), 0) + 1, $testo, $a, $il FROM commenti
                WHERE report = $r AND id_ente = $e AND edizione = $ed AND id_sezione = $id AND lingua = $l
                """, report, idEnte, edizione, t);
            q.Parameters.AddWithValue("$id", idSezione);
            q.Parameters.AddWithValue("$l", lingua);
            q.Parameters.AddWithValue("$testo", testo);
            Autore(q, autore, adesso);
            await q.ExecuteNonQueryAsync(ct);
        }, ct);

    public Task SalvaStatoAsync(string report, int idEnte, string edizione, string stato, string autore, DateTimeOffset adesso,
        CancellationToken ct) =>
        ScriviAsync((c, t) => InserisciStatoAsync(c, t, report, idEnte, edizione, stato, autore, adesso, ct), ct);

    /// <summary>Congela una versione nuova (ultima + 1 per edizione e lingua) e mette l'edizione in «pubblicato».</summary>
    public async Task<PubblicazioneReportDto> PubblicaAsync(string report, int idEnte, string edizione, string lingua, DateTimeOffset? datiAl,
        string? nota, string nomeFile, string contenuto, byte[] pptx, string autore, DateTimeOffset adesso, CancellationToken ct)
    {
        PubblicazioneReportDto? fatta = null;
        var impronta = Convert.ToHexStringLower(SHA256.HashData(pptx));
        await ScriviAsync(async (c, t) =>
        {
            int versione;
            await using (var m = Comando(c, "SELECT COALESCE(MAX(versione), 0) + 1 FROM pubblicazioni WHERE report = $r AND id_ente = $e AND edizione = $ed AND lingua = $l",
                             report, idEnte, edizione, t))
            {
                m.Parameters.AddWithValue("$l", lingua);
                versione = Convert.ToInt32(await m.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
            }

            await using (var q = Comando(c, """
                INSERT INTO pubblicazioni (report, id_ente, edizione, lingua, versione, pubblicato_il, autore, dati_al, nota, nome_file,
                                           contenuto, pptx, impronta_sha256)
                VALUES ($r, $e, $ed, $l, $v, $il, $a, $dati, $nota, $nome, $contenuto, $pptx, $impronta)
                """, report, idEnte, edizione, t))
            {
                q.Parameters.AddWithValue("$l", lingua);
                q.Parameters.AddWithValue("$v", versione);
                Autore(q, autore, adesso);
                q.Parameters.AddWithValue("$dati", datiAl is { } d ? Testo(d) : DBNull.Value);
                q.Parameters.AddWithValue("$nota", (object?)nota ?? DBNull.Value);
                q.Parameters.AddWithValue("$nome", nomeFile);
                q.Parameters.AddWithValue("$contenuto", contenuto);
                q.Parameters.AddWithValue("$pptx", pptx);
                q.Parameters.AddWithValue("$impronta", impronta);
                await q.ExecuteNonQueryAsync(ct);
            }

            await InserisciStatoAsync(c, t, report, idEnte, edizione, StatiReport.Pubblicato, autore, adesso, ct);
            fatta = new PubblicazioneReportDto(versione, lingua, Data(Testo(adesso)), autore, datiAl is { } x ? Data(Testo(x)) : null, nota, nomeFile,
                impronta, null, null);
        }, ct);
        return fatta!;
    }

    /// <summary>
    /// Ritira una versione (il cliente non la vede più, resta in archivio); se non resta nessuna versione in vista l'edizione torna
    /// in bozza. False se la versione non c'è o era già ritirata.
    /// </summary>
    public async Task<bool> RitiraAsync(string report, int idEnte, string edizione, string lingua, int versione, string autore,
        DateTimeOffset adesso, CancellationToken ct)
    {
        var fatto = false;
        await ScriviAsync(async (c, t) =>
        {
            await using (var q = Comando(c, """
                UPDATE pubblicazioni SET ritirata_il = $il, ritirata_da = $a
                WHERE report = $r AND id_ente = $e AND edizione = $ed AND lingua = $l AND versione = $v AND ritirata_il IS NULL
                """, report, idEnte, edizione, t))
            {
                q.Parameters.AddWithValue("$l", lingua);
                q.Parameters.AddWithValue("$v", versione);
                Autore(q, autore, adesso);
                fatto = await q.ExecuteNonQueryAsync(ct) > 0;
            }

            if (!fatto)
                return;
            await using var restano = Comando(c, "SELECT COUNT(*) FROM pubblicazioni WHERE report = $r AND id_ente = $e AND edizione = $ed AND ritirata_il IS NULL",
                report, idEnte, edizione, t);
            if (Convert.ToInt32(await restano.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) == 0)
                await InserisciStatoAsync(c, t, report, idEnte, edizione, StatiReport.Bozza, autore, adesso, ct);
        }, ct);
        return fatto;
    }

    private const string ColonnePubblicazione =
        "versione, lingua, pubblicato_il, autore, dati_al, nota, nome_file, impronta_sha256, ritirata_il, ritirata_da";

    private static PubblicazioneReportDto Pubblicazione(SqliteDataReader l) =>
        new(l.GetInt32(0), l.GetString(1), Data(l.GetString(2)), l.GetString(3), l.IsDBNull(4) ? null : Data(l.GetString(4)),
            l.IsDBNull(5) ? null : l.GetString(5), l.GetString(6), l.GetString(7), l.IsDBNull(8) ? null : Data(l.GetString(8)),
            l.IsDBNull(9) ? null : l.GetString(9));

    private static async Task<IReadOnlyList<PubblicazioneReportDto>> PubblicazioniAsync(SqliteConnection c, string report, int idEnte,
        string edizione, CancellationToken ct)
    {
        var risultato = new List<PubblicazioneReportDto>();
        await using var q = Comando(c, $"SELECT {ColonnePubblicazione} FROM pubblicazioni WHERE report = $r AND id_ente = $e AND edizione = $ed ORDER BY versione DESC, lingua",
            report, idEnte, edizione);
        await using var l = await q.ExecuteReaderAsync(ct);
        while (await l.ReadAsync(ct))
            risultato.Add(Pubblicazione(l));
        return risultato;
    }

    private static async Task InserisciStatoAsync(SqliteConnection c, SqliteTransaction t, string report, int idEnte, string edizione,
        string stato, string autore, DateTimeOffset adesso, CancellationToken ct)
    {
        await using var q = Comando(c, """
            INSERT INTO stati (report, id_ente, edizione, versione, stato, autore, creato_il)
            SELECT $r, $e, $ed, COALESCE(MAX(versione), 0) + 1, $stato, $a, $il FROM stati WHERE report = $r AND id_ente = $e AND edizione = $ed
            """, report, idEnte, edizione, t);
        q.Parameters.AddWithValue("$stato", stato);
        Autore(q, autore, adesso);
        await q.ExecuteNonQueryAsync(ct);
    }

    private static SqliteCommand Comando(SqliteConnection c, string sql, string report, int idEnte, string edizione, SqliteTransaction? t = null)
    {
        var q = c.CreateCommand();
        q.Transaction = t;
        q.CommandText = sql;
        q.Parameters.AddWithValue("$r", report);
        q.Parameters.AddWithValue("$e", idEnte);
        q.Parameters.AddWithValue("$ed", edizione);
        return q;
    }

    private static void Autore(SqliteCommand q, string autore, DateTimeOffset adesso)
    {
        q.Parameters.AddWithValue("$a", autore);
        q.Parameters.AddWithValue("$il", Testo(adesso));
    }

    // Millisecondi nel testo: due scritture nello stesso secondo restano in ordine rispetto a una pubblicazione.
    private static string Testo(DateTimeOffset d) => d.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static DateTimeOffset Data(string testo) =>
        DateTimeOffset.Parse(testo, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    /// <summary>Apre il file; in sola lettura null se il file o la tabella non ci sono ancora.</summary>
    private async Task<SqliteConnection?> ApriAsync(bool sola, CancellationToken ct, string tabella = "pubblicazioni")
    {
        if (sola && !File.Exists(_percorso))
            return null;
        if (!sola && Path.GetDirectoryName(_percorso) is { Length: > 0 } cartella)
            Directory.CreateDirectory(cartella);

        var stringa = new SqliteConnectionStringBuilder
        {
            DataSource = _percorso,
            Mode = sola ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        };
        var c = new SqliteConnection(stringa.ToString());
        await c.OpenAsync(ct);
        if (sola)
        {
            await using var esiste = c.CreateCommand();
            esiste.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $t";
            esiste.Parameters.AddWithValue("$t", tabella);
            if (await esiste.ExecuteScalarAsync(ct) is null)
            {
                await c.DisposeAsync();
                return null;
            }
        }
        else
        {
            await using var schema = c.CreateCommand();
            schema.CommandText = Schema;
            await schema.ExecuteNonQueryAsync(ct);
        }

        return c;
    }

    private async Task ScriviAsync(Func<SqliteConnection, SqliteTransaction, Task> lavoro, CancellationToken ct)
    {
        await _scrittura.WaitAsync(ct);
        try
        {
            await using var c = (await ApriAsync(sola: false, ct))!;
            await using var t = (SqliteTransaction)await c.BeginTransactionAsync(ct);
            await lavoro(c, t);
            await t.CommitAsync(ct);
        }
        finally
        {
            _scrittura.Release();
        }
    }
}
