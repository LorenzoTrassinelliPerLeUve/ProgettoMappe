using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.BilancioIdrico;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

/// <summary>
/// Un calcolo salvato (task #335, punto A): azienda, stagione, quando, con quale motore e quali parametri, il riepilogo con
/// le anomalie dei dati. <see cref="Errore"/> valorizzato = calcolo fallito (i risultati restano quelli di prima).
/// </summary>
public sealed record CalcoloSalvato(long Id, int IdEnte, int Anno, DateTime CalcolatoIlUtc, DateOnly Da, DateOnly A, string VersioneMotore,
    string VersioneParametri, int DurataMs, RiepilogoCalcolo? Riepilogo, string? Errore);

/// <summary>
/// I risultati del DSS Bilancio Idrico (task #335): lo stesso file SQLite della configurazione (<c>BilancioIdrico:Database</c>),
/// tabelle <c>calcoli</c> (uno per giro e azienda, anche quelli falliti: è lo storico), <c>vigneti_giorno</c>,
/// <c>settori_giorno</c> e <c>shift_giorno</c>. Dei risultati si tiene solo l'ultimo calcolo riuscito di ogni azienda: ogni
/// notte si rifà tutto (niente code né ricalcoli a pezzi).
/// </summary>
public sealed class ArchivioRisultatiBilancio
{
    private readonly string _percorso;
    private readonly SemaphoreSlim _scrittura = new(1, 1);

    public ArchivioRisultatiBilancio(IOptions<BilancioIdricoOptions> opzioni) : this(opzioni.Value.PercorsoScelto())
    {
    }

    public ArchivioRisultatiBilancio(string percorso)
    {
        _percorso = LettoreNdvi.Espandi(percorso);
    }

    public string Percorso => _percorso;

    /// <summary>
    /// Versione dei parametri: «ComeOggi» senza tabelle del modello nella configurazione, altrimenti la versione della voce
    /// <c>modello</c> e un'impronta delle tabelle usate.
    /// </summary>
    public static string VersioneParametri(Parametri p, int? versioneModello)
    {
        if (versioneModello is null)
            return p.Nome;
        var json = JsonSerializer.Serialize(ModelloBilancio.Da(p), ArchivioConfigurazioneBilancio.Json);
        var impronta = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))[..8].ToLowerInvariant();
        return $"{p.Nome} + modello v{versioneModello} ({impronta})";
    }

    /// <summary>Salva un calcolo riuscito: i risultati dell'azienda prendono il posto di quelli di prima.</summary>
    public async Task<long> SalvaAsync(int idEnte, int anno, DateOnly da, DateOnly a, string versioneParametri, int durataMs,
        RisultatoBilancio risultato, RiepilogoCalcolo riepilogo, CancellationToken ct = default)
    {
        await _scrittura.WaitAsync(ct);
        try
        {
            await using var conn = await ApriPerScrivereAsync(ct);
            await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
            foreach (var tabella in new[] { "vigneti_giorno", "settori_giorno", "shift_giorno" })
            {
                await using var del = conn.CreateCommand();
                del.Transaction = tx;
                del.CommandText = $"DELETE FROM {tabella} WHERE id_ente = $e";
                del.Parameters.AddWithValue("$e", idEnte);
                await del.ExecuteNonQueryAsync(ct);
            }
            var id = await InserisciCalcoloAsync(conn, tx, idEnte, anno, da, a, versioneParametri, durataMs,
                JsonSerializer.Serialize(riepilogo, ArchivioConfigurazioneBilancio.Json), null, ct);

            await using (var ins = conn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO vigneti_giorno VALUES ($e, $v, $d, $et0, $pioggia, $irr, $bbch, $fase, $gest, $stile, $awc, $kc, $soglia,
                        $turno, $ks, $eta, $aw, $faw, $pdlwp, $deficit, $etaf, $sugg_mm, $perdite, $senza_imp, $sugg, $prio, $segnali, $settori)
                    """;
                var nomi = new[] { "$e", "$v", "$d", "$et0", "$pioggia", "$irr", "$bbch", "$fase", "$gest", "$stile", "$awc", "$kc", "$soglia",
                    "$turno", "$ks", "$eta", "$aw", "$faw", "$pdlwp", "$deficit", "$etaf", "$sugg_mm", "$perdite", "$senza_imp", "$sugg",
                    "$prio", "$segnali", "$settori" };
                var par = nomi.ToDictionary(n => n, n => ins.Parameters.Add(n, SqliteType.Text));
                foreach (var r in risultato.Vigneti)
                {
                    object V(object? x) => x ?? DBNull.Value;
                    par["$e"].Value = idEnte;
                    par["$v"].Value = r.IdVigneto;
                    par["$d"].Value = Data(r.Data);
                    par["$et0"].Value = V(r.Et0Mm);
                    par["$pioggia"].Value = V(r.PioggiaMm);
                    par["$irr"].Value = r.IrrigazioneMm;
                    par["$bbch"].Value = V(r.Bbch);
                    par["$fase"].Value = V(r.Fase);
                    par["$gest"].Value = V(r.IdGestioneSuolo);
                    par["$stile"].Value = V(r.IdStileVino);
                    par["$awc"].Value = V(r.AwcMm);
                    par["$kc"].Value = V(r.Kc);
                    par["$soglia"].Value = V(r.SogliaMpa);
                    par["$turno"].Value = r.TurnoIrriguoGiorni;
                    par["$ks"].Value = V(r.Ks);
                    par["$eta"].Value = V(r.EtaMm);
                    par["$aw"].Value = r.AwMm;
                    par["$faw"].Value = V(r.Faw);
                    par["$pdlwp"].Value = V(r.PdlwpMpa);
                    par["$deficit"].Value = V(r.DeficitMm);
                    par["$etaf"].Value = r.EtaFuturaMm;
                    par["$sugg_mm"].Value = r.IrrigazioneSuggeritaMm;
                    par["$perdite"].Value = r.PerditeMm;
                    par["$senza_imp"].Value = r.SenzaImpianto ? 1 : 0;
                    par["$sugg"].Value = r.Suggerimento.Codice();
                    par["$prio"].Value = r.Priorita;
                    par["$segnali"].Value = Segnali(r);
                    par["$settori"].Value = r.Settori.Count == 0 ? DBNull.Value
                        : JsonSerializer.Serialize(r.Settori, ArchivioConfigurazioneBilancio.Json);
                    // Tipi veri (Text li convertirebbe in stringa).
                    foreach (var p in par.Values)
                        p.SqliteType = p.Value switch { int or long or bool => SqliteType.Integer, double => SqliteType.Real, _ => SqliteType.Text };
                    await ins.ExecuteNonQueryAsync(ct);
                }
            }

            await using (var ins = conn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = "INSERT INTO settori_giorno VALUES ($e, $s, $d, $mm, $ore, $prio)";
                var e = ins.Parameters.AddWithValue("$e", idEnte);
                var s = ins.Parameters.Add("$s", SqliteType.Integer);
                var d = ins.Parameters.Add("$d", SqliteType.Text);
                var mm = ins.Parameters.Add("$mm", SqliteType.Real);
                var ore = ins.Parameters.Add("$ore", SqliteType.Real);
                var prio = ins.Parameters.Add("$prio", SqliteType.Integer);
                foreach (var r in risultato.Settori)
                {
                    s.Value = r.IdSettore;
                    d.Value = Data(r.Data);
                    mm.Value = r.IrrigazioneSuggeritaMm;
                    ore.Value = (object?)r.IrrigazioneSuggeritaOre ?? DBNull.Value;
                    prio.Value = r.Priorita;
                    await ins.ExecuteNonQueryAsync(ct);
                }
            }

            await using (var ins = conn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = "INSERT INTO shift_giorno VALUES ($e, $s, $d, $ore, $n)";
                ins.Parameters.AddWithValue("$e", idEnte);
                var s = ins.Parameters.Add("$s", SqliteType.Integer);
                var d = ins.Parameters.Add("$d", SqliteType.Text);
                var ore = ins.Parameters.Add("$ore", SqliteType.Real);
                var n = ins.Parameters.Add("$n", SqliteType.Integer);
                foreach (var r in risultato.Shift)
                {
                    s.Value = r.IdShift;
                    d.Value = Data(r.Data);
                    ore.Value = (object?)r.OreComando ?? DBNull.Value;
                    n.Value = r.SettoriCoinvolti;
                    await ins.ExecuteNonQueryAsync(ct);
                }
            }

            await tx.CommitAsync(ct);
            return id;
        }
        finally
        {
            _scrittura.Release();
        }
    }

    /// <summary>Annota un calcolo fallito: i risultati dell'azienda restano quelli dell'ultimo calcolo riuscito.</summary>
    public async Task SalvaErroreAsync(int idEnte, int anno, DateOnly da, DateOnly a, string versioneParametri, int durataMs, string errore,
        CancellationToken ct = default)
    {
        await _scrittura.WaitAsync(ct);
        try
        {
            await using var conn = await ApriPerScrivereAsync(ct);
            await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
            await InserisciCalcoloAsync(conn, tx, idEnte, anno, da, a, versioneParametri, durataMs, null,
                errore.Length > 1000 ? errore[..1000] : errore, ct);
            await tx.CommitAsync(ct);
        }
        finally
        {
            _scrittura.Release();
        }
    }

    /// <summary>L'ultimo calcolo riuscito dell'azienda e, se è più recente, l'ultimo fallito.</summary>
    public async Task<(CalcoloSalvato? Riuscito, CalcoloSalvato? ErroreDopo)> UltimoCalcoloAsync(int idEnte, CancellationToken ct = default)
    {
        var calcoli = await CalcoliAsync("WHERE id_ente = $e ORDER BY id DESC LIMIT 50", c => c.Parameters.AddWithValue("$e", idEnte), ct);
        var riuscito = calcoli.FirstOrDefault(c => c.Errore is null);
        var errore = calcoli.FirstOrDefault(c => c.Errore is not null && (riuscito is null || c.Id > riuscito.Id));
        return (riuscito, errore);
    }

    /// <summary>L'ultimo calcolo di ogni azienda (riuscito o no): lo stato del giro notturno.</summary>
    public Task<IReadOnlyList<CalcoloSalvato>> UltimiCalcoliAsync(CancellationToken ct = default) =>
        CalcoliAsync("WHERE id IN (SELECT MAX(id) FROM calcoli GROUP BY id_ente) ORDER BY id_ente", _ => { }, ct);

    /// <summary>
    /// Le righe dei vigneti dell'azienda dell'ultimo calcolo, fra <paramref name="da"/> e <paramref name="a"/>; solo
    /// <paramref name="idVigneto"/> se indicato.
    /// </summary>
    public async Task<IReadOnlyList<RisultatoVignetoGiorno>> VignetiAsync(int idEnte, DateOnly da, DateOnly a, int? idVigneto = null,
        CancellationToken ct = default)
    {
        var righe = new List<RisultatoVignetoGiorno>();
        await LeggiAsync(c =>
        {
            c.CommandText = """
                SELECT id_vigneto, data, et0, pioggia, irrigazione, bbch, fase, gestione, stile, awc, kc, soglia, turno, ks, eta, aw, faw,
                       pdlwp, deficit, eta_futura, irr_sugg_mm, perdite, senza_impianto, suggerimento, priorita, segnali, settori
                FROM vigneti_giorno WHERE id_ente = $e AND data BETWEEN $da AND $a AND ($v IS NULL OR id_vigneto = $v)
                ORDER BY id_vigneto, data
                """;
            c.Parameters.AddWithValue("$e", idEnte);
            c.Parameters.AddWithValue("$da", Data(da));
            c.Parameters.AddWithValue("$a", Data(a));
            c.Parameters.AddWithValue("$v", (object?)idVigneto ?? DBNull.Value);
        }, r =>
        {
            double? D(int i) => r.IsDBNull(i) ? null : r.GetDouble(i);
            int? I(int i) => r.IsDBNull(i) ? null : r.GetInt32(i);
            var segnali = r.GetInt32(25);
            righe.Add(new RisultatoVignetoGiorno
            {
                IdVigneto = r.GetInt32(0),
                Data = DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Et0Mm = D(2),
                PioggiaMm = D(3),
                IrrigazioneMm = r.GetDouble(4),
                Bbch = I(5),
                Fase = I(6),
                IdGestioneSuolo = I(7),
                IdStileVino = I(8),
                AwcMm = D(9),
                Kc = D(10),
                SogliaMpa = D(11),
                TurnoIrriguoGiorni = r.GetInt32(12),
                Ks = D(13),
                EtaMm = D(14),
                AwMm = r.GetDouble(15),
                Faw = D(16),
                PdlwpMpa = D(17),
                DeficitMm = D(18),
                EtaFuturaMm = r.GetDouble(19),
                IrrigazioneSuggeritaMm = r.GetDouble(20),
                PerditeMm = r.GetDouble(21),
                SenzaImpianto = r.GetInt32(22) != 0,
                Suggerimento = DaCodice(r.GetString(23)),
                Priorita = r.GetInt32(24),
                MeteoMancante = (segnali & 1) != 0,
                FenologiaMancante = (segnali & 2) != 0,
                SuoloMancante = (segnali & 4) != 0,
                ConfigMancante = (segnali & 8) != 0,
                KcMancante = (segnali & 16) != 0,
                CriticitaPdlwp = (segnali & 32) != 0,
                CriticitaDeficit = (segnali & 64) != 0,
                Settori = r.IsDBNull(26) ? []
                    : JsonSerializer.Deserialize<List<ConsiglioSettore>>(r.GetString(26), ArchivioConfigurazioneBilancio.Json) ?? [],
            });
        }, ct);
        return righe;
    }

    /// <summary>I totali dei settori dell'azienda fra due giorni.</summary>
    public async Task<IReadOnlyList<RisultatoSettoreGiorno>> SettoriAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        var righe = new List<RisultatoSettoreGiorno>();
        await LeggiAsync(c =>
        {
            c.CommandText = "SELECT id_settore, data, mm, ore, priorita FROM settori_giorno WHERE id_ente = $e AND data BETWEEN $da AND $a ORDER BY id_settore, data";
            c.Parameters.AddWithValue("$e", idEnte);
            c.Parameters.AddWithValue("$da", Data(da));
            c.Parameters.AddWithValue("$a", Data(a));
        }, r => righe.Add(new RisultatoSettoreGiorno(r.GetInt32(0), DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.GetDouble(2), r.IsDBNull(3) ? null : r.GetDouble(3), r.GetInt32(4))), ct);
        return righe;
    }

    /// <summary>Gli shift dell'azienda fra due giorni.</summary>
    public async Task<IReadOnlyList<RisultatoShiftGiorno>> ShiftAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        var righe = new List<RisultatoShiftGiorno>();
        await LeggiAsync(c =>
        {
            c.CommandText = "SELECT id_shift, data, ore, settori FROM shift_giorno WHERE id_ente = $e AND data BETWEEN $da AND $a ORDER BY id_shift, data";
            c.Parameters.AddWithValue("$e", idEnte);
            c.Parameters.AddWithValue("$da", Data(da));
            c.Parameters.AddWithValue("$a", Data(a));
        }, r => righe.Add(new RisultatoShiftGiorno(r.GetInt32(0), DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            r.IsDBNull(2) ? null : r.GetDouble(2), r.GetInt32(3))), ct);
        return righe;
    }

    public static Suggerimento DaCodice(string codice) => codice switch
    {
        "IRRIGARE" => Suggerimento.Irrigare,
        "NON_IRRIGARE" => Suggerimento.NonIrrigare,
        "N_A" => Suggerimento.NonApplicabile,
        _ => Suggerimento.DatoNonValido,
    };

    private static int Segnali(RisultatoVignetoGiorno r) =>
        (r.MeteoMancante ? 1 : 0) | (r.FenologiaMancante ? 2 : 0) | (r.SuoloMancante ? 4 : 0) | (r.ConfigMancante ? 8 : 0)
        | (r.KcMancante ? 16 : 0) | (r.CriticitaPdlwp ? 32 : 0) | (r.CriticitaDeficit ? 64 : 0);

    private static string Data(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<long> InserisciCalcoloAsync(SqliteConnection conn, SqliteTransaction tx, int idEnte, int anno, DateOnly da,
        DateOnly a, string versioneParametri, int durataMs, string? riepilogo, string? errore, CancellationToken ct)
    {
        await using var ins = conn.CreateCommand();
        ins.Transaction = tx;
        ins.CommandText = """
            INSERT INTO calcoli (id_ente, anno, calcolato_il, da, a, versione_motore, versione_parametri, durata_ms, riepilogo, errore)
            VALUES ($e, $anno, $il, $da, $a, $vm, $vp, $ms, $r, $err);
            SELECT last_insert_rowid();
            """;
        ins.Parameters.AddWithValue("$e", idEnte);
        ins.Parameters.AddWithValue("$anno", anno);
        ins.Parameters.AddWithValue("$il", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        ins.Parameters.AddWithValue("$da", Data(da));
        ins.Parameters.AddWithValue("$a", Data(a));
        ins.Parameters.AddWithValue("$vm", MotoreBilancio.Versione);
        ins.Parameters.AddWithValue("$vp", versioneParametri);
        ins.Parameters.AddWithValue("$ms", durataMs);
        ins.Parameters.AddWithValue("$r", (object?)riepilogo ?? DBNull.Value);
        ins.Parameters.AddWithValue("$err", (object?)errore ?? DBNull.Value);
        return Convert.ToInt64(await ins.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<CalcoloSalvato>> CalcoliAsync(string filtro, Action<SqliteCommand> parametri, CancellationToken ct)
    {
        var calcoli = new List<CalcoloSalvato>();
        await LeggiAsync(c =>
        {
            c.CommandText = $"SELECT id, id_ente, anno, calcolato_il, da, a, versione_motore, versione_parametri, durata_ms, riepilogo, errore FROM calcoli {filtro}";
            parametri(c);
        }, r => calcoli.Add(new CalcoloSalvato(r.GetInt64(0), r.GetInt32(1), r.GetInt32(2),
            DateTime.Parse(r.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
            DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateOnly.ParseExact(r.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture), r.GetString(6), r.GetString(7), r.GetInt32(8),
            r.IsDBNull(9) ? null : JsonSerializer.Deserialize<RiepilogoCalcolo>(r.GetString(9), ArchivioConfigurazioneBilancio.Json),
            r.IsDBNull(10) ? null : r.GetString(10))), ct);
        return calcoli;
    }

    private async Task LeggiAsync(Action<SqliteCommand> prepara, Action<SqliteDataReader> riga, CancellationToken ct)
    {
        if (!File.Exists(_percorso))
            return;
        await using var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = _percorso, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        await conn.OpenAsync(ct);
        await using var c = conn.CreateCommand();
        prepara(c);
        try
        {
            await using var r = await c.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                riga(r);
        }
        catch (SqliteException e) when (e.SqliteErrorCode == 1 && e.Message.Contains("no such table", StringComparison.Ordinal))
        {
        }
    }

    private async Task<SqliteConnection> ApriPerScrivereAsync(CancellationToken ct)
    {
        var cartella = Path.GetDirectoryName(_percorso);
        if (!string.IsNullOrEmpty(cartella))
            Directory.CreateDirectory(cartella);
        var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = _percorso, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 120 }.ToString());
        await conn.OpenAsync(ct);
        await using var crea = conn.CreateCommand();
        // WAL: le pagine leggono mentre il giro notturno scrive.
        crea.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS calcoli (
                id INTEGER PRIMARY KEY AUTOINCREMENT, id_ente INTEGER NOT NULL, anno INTEGER NOT NULL, calcolato_il TEXT NOT NULL,
                da TEXT NOT NULL, a TEXT NOT NULL, versione_motore TEXT NOT NULL, versione_parametri TEXT NOT NULL,
                durata_ms INTEGER NOT NULL, riepilogo TEXT, errore TEXT);
            CREATE INDEX IF NOT EXISTS calcoli_ente ON calcoli (id_ente, id);
            CREATE TABLE IF NOT EXISTS vigneti_giorno (
                id_ente INTEGER NOT NULL, id_vigneto INTEGER NOT NULL, data TEXT NOT NULL, et0 REAL, pioggia REAL, irrigazione REAL NOT NULL,
                bbch INTEGER, fase INTEGER, gestione INTEGER, stile INTEGER, awc REAL, kc REAL, soglia REAL, turno INTEGER NOT NULL,
                ks REAL, eta REAL, aw REAL NOT NULL, faw REAL, pdlwp REAL, deficit REAL, eta_futura REAL NOT NULL, irr_sugg_mm REAL NOT NULL,
                perdite REAL NOT NULL, senza_impianto INTEGER NOT NULL, suggerimento TEXT NOT NULL, priorita INTEGER NOT NULL,
                segnali INTEGER NOT NULL, settori TEXT,
                PRIMARY KEY (id_ente, id_vigneto, data)) WITHOUT ROWID;
            CREATE TABLE IF NOT EXISTS settori_giorno (
                id_ente INTEGER NOT NULL, id_settore INTEGER NOT NULL, data TEXT NOT NULL, mm REAL NOT NULL, ore REAL, priorita INTEGER NOT NULL,
                PRIMARY KEY (id_ente, id_settore, data)) WITHOUT ROWID;
            CREATE TABLE IF NOT EXISTS shift_giorno (
                id_ente INTEGER NOT NULL, id_shift INTEGER NOT NULL, data TEXT NOT NULL, ore REAL, settori INTEGER NOT NULL,
                PRIMARY KEY (id_ente, id_shift, data)) WITHOUT ROWID;
            """;
        await crea.ExecuteNonQueryAsync(ct);
        return conn;
    }
}
