using Microsoft.Data.Sqlite;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Reportistica;

/// <summary>
/// Obiettivi dei parametri dell'Indice Bigot (task #419, tappa R6, PROGETTO.md §5.1), nello stesso SQLite delle scelte dei report.
/// Come il resto dell'archivio ogni salvataggio è una versione nuova: l'ultima vale, le altre sono lo storico. Una versione con
/// minimo, obiettivo e massimo vuoti toglie l'obiettivo (vale il livello sopra).
/// </summary>
public sealed partial class ArchivioReportistica
{
    private const string TabellaObiettivi = "obiettivi_bigot";

    private const string ColonneObiettivo = "id_ente, id_vigneto, anno, parametro, minimo, obiettivo, massimo, versione, autore, creato_il";

    /// <summary>L'ultima versione di ogni obiettivo dell'annata: predefiniti (ente 0) e, con <paramref name="idEnte"/> &gt; 0, quelli dell'azienda e dei suoi vigneti.</summary>
    public async Task<ObiettiviBigotDto> ObiettiviAsync(int anno, int idEnte, CancellationToken ct)
    {
        await using var c = await ApriAsync(sola: true, ct, TabellaObiettivi);
        if (c is null)
            return new ObiettiviBigotDto(anno, idEnte, [], []);

        var obiettivi = new List<ObiettivoBigotDto>();
        await using (var q = c.CreateCommand())
        {
            q.CommandText = $"""
                SELECT {ColonneObiettivo} FROM obiettivi_bigot o
                WHERE anno = $anno AND id_ente IN (0, $e)
                  AND versione = (SELECT MAX(versione) FROM obiettivi_bigot u WHERE u.id_ente = o.id_ente AND u.id_vigneto = o.id_vigneto
                                  AND u.anno = o.anno AND u.parametro = o.parametro)
                ORDER BY id_ente, id_vigneto, parametro
                """;
            q.Parameters.AddWithValue("$anno", anno);
            q.Parameters.AddWithValue("$e", idEnte);
            await using var l = await q.ExecuteReaderAsync(ct);
            while (await l.ReadAsync(ct))
                obiettivi.Add(Obiettivo(l));
        }

        var anni = new List<int>();
        await using (var q = c.CreateCommand())
        {
            q.CommandText = "SELECT DISTINCT anno FROM obiettivi_bigot WHERE id_ente IN (0, $e) ORDER BY anno DESC";
            q.Parameters.AddWithValue("$e", idEnte);
            await using var l = await q.ExecuteReaderAsync(ct);
            while (await l.ReadAsync(ct))
                anni.Add(l.GetInt32(0));
        }

        return new ObiettiviBigotDto(anno, idEnte, obiettivi, anni);
    }

    /// <summary>Tutte le versioni di un obiettivo, dalla più recente.</summary>
    public async Task<IReadOnlyList<ObiettivoBigotDto>> StoricoObiettivoAsync(int idEnte, int idVigneto, int anno, string parametro, CancellationToken ct)
    {
        await using var c = await ApriAsync(sola: true, ct, TabellaObiettivi);
        if (c is null)
            return [];
        var risultato = new List<ObiettivoBigotDto>();
        await using var q = c.CreateCommand();
        q.CommandText = $"""
            SELECT {ColonneObiettivo} FROM obiettivi_bigot
            WHERE id_ente = $e AND id_vigneto = $v AND anno = $anno AND parametro = $p ORDER BY versione DESC
            """;
        q.Parameters.AddWithValue("$e", idEnte);
        q.Parameters.AddWithValue("$v", idVigneto);
        q.Parameters.AddWithValue("$anno", anno);
        q.Parameters.AddWithValue("$p", parametro);
        await using var l = await q.ExecuteReaderAsync(ct);
        while (await l.ReadAsync(ct))
            risultato.Add(Obiettivo(l));
        return risultato;
    }

    /// <summary>Una versione nuova dell'obiettivo (già controllato e con il parametro normalizzato).</summary>
    public Task SalvaObiettivoAsync(SalvaObiettivoBigotDto o, string autore, DateTimeOffset adesso, CancellationToken ct) =>
        ScriviAsync(async (c, t) => await InserisciObiettivoAsync(c, t, o.IdEnte, o.IdVigneto, o.Anno, o.Parametro, o.Minimo, o.Obiettivo, o.Massimo,
            autore, adesso, ct), ct);

    /// <summary>
    /// Copia nell'annata <paramref name="adAnno"/> gli obiettivi (non vuoti) del livello che lì mancano: con ente 0 i predefiniti,
    /// altrimenti quelli dell'azienda e dei suoi vigneti. Quelli già presenti nell'annata nuova non si toccano. Restituisce quanti.
    /// </summary>
    public async Task<int> CopiaObiettiviAsync(int idEnte, int daAnno, int adAnno, string autore, DateTimeOffset adesso, CancellationToken ct)
    {
        var origine = (await ObiettiviAsync(daAnno, idEnte, ct)).Obiettivi.Where(o => o.IdEnte == idEnte && !o.Vuoto).ToList();
        var giaPresenti = (await ObiettiviAsync(adAnno, idEnte, ct)).Obiettivi.Where(o => o.IdEnte == idEnte)
            .Select(o => (o.IdVigneto, o.Parametro)).ToHashSet();
        var daCopiare = origine.Where(o => !giaPresenti.Contains((o.IdVigneto, o.Parametro))).ToList();
        if (daCopiare.Count == 0)
            return 0;
        await ScriviAsync(async (c, t) =>
        {
            foreach (var o in daCopiare)
                await InserisciObiettivoAsync(c, t, idEnte, o.IdVigneto, adAnno, o.Parametro, o.Minimo, o.Obiettivo, o.Massimo, autore, adesso, ct);
        }, ct);
        return daCopiare.Count;
    }

    private static async Task InserisciObiettivoAsync(SqliteConnection c, SqliteTransaction t, int idEnte, int idVigneto, int anno, string parametro,
        double? minimo, double? obiettivo, double? massimo, string autore, DateTimeOffset adesso, CancellationToken ct)
    {
        await using var q = c.CreateCommand();
        q.Transaction = t;
        q.CommandText = """
            INSERT INTO obiettivi_bigot (id_ente, id_vigneto, anno, parametro, versione, minimo, obiettivo, massimo, autore, creato_il)
            SELECT $e, $v, $anno, $p, COALESCE(MAX(versione), 0) + 1, $min, $obj, $max, $a, $il FROM obiettivi_bigot
            WHERE id_ente = $e AND id_vigneto = $v AND anno = $anno AND parametro = $p
            """;
        q.Parameters.AddWithValue("$e", idEnte);
        q.Parameters.AddWithValue("$v", idVigneto);
        q.Parameters.AddWithValue("$anno", anno);
        q.Parameters.AddWithValue("$p", parametro);
        q.Parameters.AddWithValue("$min", (object?)minimo ?? DBNull.Value);
        q.Parameters.AddWithValue("$obj", (object?)obiettivo ?? DBNull.Value);
        q.Parameters.AddWithValue("$max", (object?)massimo ?? DBNull.Value);
        Autore(q, autore, adesso);
        await q.ExecuteNonQueryAsync(ct);
    }

    private static ObiettivoBigotDto Obiettivo(SqliteDataReader l) =>
        new(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.GetString(3),
            l.IsDBNull(4) ? null : l.GetDouble(4), l.IsDBNull(5) ? null : l.GetDouble(5), l.IsDBNull(6) ? null : l.GetDouble(6),
            l.GetInt32(7), l.GetString(8), Data(l.GetString(9)));
}
