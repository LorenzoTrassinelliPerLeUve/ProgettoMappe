using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Letture della tappa 1 del Portale, con le stesse condizioni delle query del portale (RilieviController,
/// AsportazioniController, GalleriaFotograficaController, metodi di Utente.cs). Il portale scrive gli Id dentro il
/// testo SQL; qui sono sempre parametri.
/// </summary>
public class FourGrapesLetturePortale : ILetturePortale
{
    /// <summary>Le viste <c>VistaExportRilievi_{lingua}</c> che esistono (dba, 14/11: IT, EN, ES, FR, RO, SL).</summary>
    public static readonly IReadOnlyList<string> Lingue = ["IT", "EN", "ES", "FR", "RO", "SL"];

    private readonly FourGrapesDbContext _db;

    public FourGrapesLetturePortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeAbbinateAsync(int idUtente, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT e.IdEnte, e.RagioneSociale FROM dbo.Ente e
            WHERE e.IdEnte IN (SELECT ue.Ente_IdEnte FROM dbo.Utente_Ente ue WHERE ue.Utente_idUtente = @IdUtente AND ue.StatoEntita = 1)
            ORDER BY e.RagioneSociale
            """, [Intero("@IdUtente", idUtente)], Azienda, ct);

    public Task<IReadOnlyList<AziendaPortaleDto>> TutteLeAziendeAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT IdEnte, RagioneSociale FROM dbo.Ente WHERE IdEnte > 0 ORDER BY RagioneSociale", [], Azienda, ct);

    public async Task<IReadOnlyList<int>> UtentiCheCondividonoAsync(int idUtente, int idAzienda, CancellationToken ct = default) =>
        (await LeggiAsync(
            "SELECT IdUtenteSorgente FROM dbo.Condivisione WHERE IdEnte = @IdEnte AND IdUtenteDestinatario = @IdUtente",
            [Intero("@IdEnte", idAzienda), Intero("@IdUtente", idUtente)],
            r => r.Intero("IdUtenteSorgente"), ct))
        .OfType<int>().ToList();

    public async Task<TabellaPortaleDto> RilieviExportAsync(string lingua, IReadOnlyList<int>? idUtenti, int idAzienda, int anno,
        CancellationToken ct = default)
    {
        if (!Lingue.Contains(lingua))
        {
            throw new ArgumentException($"Lingua non prevista: {lingua}");
        }

        var parametri = new List<SqlParameter> { Intero("@IdAzienda", idAzienda), Intero("@Anno", anno) };
        var filtroUtenti = "";
        var ordine = "_Utente, _Azienda, _Vigneto, _InizioRilievo";
        if (idUtenti is not null)
        {
            var nomi = idUtenti.Select((id, i) => $"@U{i}").ToList();
            parametri.AddRange(idUtenti.Select((id, i) => Intero($"@U{i}", id)));
            filtroUtenti = $" AND _IdUtente IN ({string.Join(", ", nomi)})";
            // «Miei» (un utente solo) senza _Utente nell'ordine, come DownloadRilieviUtente.
            if (idUtenti.Count == 1)
            {
                ordine = "_Azienda, _Vigneto, _InizioRilievo";
            }
        }

        await using var comando = await ComandoAsync(
            $"SELECT * FROM dbo.VistaExportRilievi_{lingua} WHERE _IdAzienda = @IdAzienda AND _Anno = @Anno{filtroUtenti} ORDER BY {ordine}",
            parametri, ct);
        await using var reader = await comando.ExecuteReaderAsync(ct);
        var colonne = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var tipi = Enumerable.Range(0, reader.FieldCount).Select(i => TipoColonna(reader.GetFieldType(i))).ToList();
        var righe = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync(ct))
        {
            var riga = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                riga[i] = reader.IsDBNull(i) ? null : Valore(reader.GetValue(i));
            }

            righe.Add(riga);
        }

        return new TabellaPortaleDto(colonne, tipi, righe);
    }

    public Task<IReadOnlyList<AsportazioneDto>> AsportazioniAsync(int idAzienda, int? anno, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT Anno, IdVigneto, NomeVigneto, IdVigna, Vigna, IdVitigno, Vitigno, Produzione_t_ha, Zona,
                   Asport_N_kg_ha_anno, Asport_P2O5_kg_ha_anno, Asport_K2O_kg_ha_anno
            FROM dbo.VistaAsportazioni
            WHERE IdEnte = @IdEnte AND (@Anno IS NULL OR Anno = @Anno)
            ORDER BY Anno
            """,
            [Intero("@IdEnte", idAzienda), new SqlParameter("@Anno", SqlDbType.Int) { Value = (object?)anno ?? DBNull.Value }],
            r => r.Intero("Anno") is { } a && r.Intero("IdVigneto") is { } v
                ? new AsportazioneDto(a, v, r.Testo("NomeVigneto") ?? $"Vigneto {v}", r.Intero("IdVigna"), r.Testo("Vigna"),
                    r.Intero("IdVitigno"), r.Testo("Vitigno"), r.Decimale("Produzione_t_ha"), r.Testo("Zona"),
                    r.Decimale("Asport_N_kg_ha_anno"), r.Decimale("Asport_P2O5_kg_ha_anno"), r.Decimale("Asport_K2O_kg_ha_anno"))
                : null,
            ct);

    public Task<IReadOnlyList<FotoGalleriaDto>> GalleriaAsync(int idAzienda, int anno, int? idUtenteVigneti, CancellationToken ct = default) =>
        LeggiAsync($"""
            SELECT DataOraInserimento, Azienda, Vigna, Vigneto, Vitigno, IdTipoRilievo, TipoRilievo, Presenza, UrlThumbnail, UrlMedium
            FROM dbo.VistaGalleriaFotografica
            WHERE IdAzienda = @IdAzienda AND Anno = @Anno
            {(idUtenteVigneti is null ? "" : "AND IdVigneto IN (SELECT IdVigneto FROM dbo.VistaUtentiVigneti WHERE idUtente = @IdUtente)")}
            ORDER BY DataOraInserimento DESC
            """,
            [Intero("@IdAzienda", idAzienda), Intero("@Anno", anno), Intero("@IdUtente", idUtenteVigneti ?? 0)],
            r => r.Valore("DataOraInserimento") is DateTime data
                ? new FotoGalleriaDto(data, r.Testo("Azienda"), r.Testo("Vigna"), r.Testo("Vigneto"), r.Testo("Vitigno"),
                    r.Intero("IdTipoRilievo"), r.Testo("TipoRilievo"), r.Valore("Presenza") as bool?, r.Testo("UrlThumbnail"),
                    r.Testo("UrlMedium"))
                : null,
            ct);

    private static AziendaPortaleDto? Azienda(IDataRecord r) =>
        r.Intero("IdEnte") is { } id ? new AziendaPortaleDto(id, r.Testo("RagioneSociale") ?? $"Azienda {id}") : null;

    /// <summary>Tipo della colonna per il Web: «data», «numero», «si-no» o «testo».</summary>
    public static string TipoColonna(Type tipo) =>
        tipo == typeof(DateTime) || tipo == typeof(DateTimeOffset) ? "data"
        : tipo == typeof(bool) ? "si-no"
        : tipo == typeof(int) || tipo == typeof(long) || tipo == typeof(short) || tipo == typeof(byte)
          || tipo == typeof(decimal) || tipo == typeof(double) || tipo == typeof(float) ? "numero"
        : "testo";

    private static object? Valore(object valore) => valore switch
    {
        DateTime d => d.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        byte[] => null,
        string or bool or int or long or short or byte or decimal or double or float => valore,
        _ => Convert.ToString(valore, CultureInfo.InvariantCulture),
    };

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct)
    {
        await using var comando = await ComandoAsync(sql, parametri, ct);
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

    private async Task<SqlCommand> ComandoAsync(string sql, IEnumerable<SqlParameter> parametri, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        return comando;
    }
}
