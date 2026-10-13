using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Stazioni meteo, dispositivi e trappole da 4Grapes (task #69, GRANT per colonna del 2026-10-13,
/// schema letto dal dba). 4Grapes non registra l'ultima lettura di stazioni e dispositivi; scelte del
/// proprietario (2026-10-13):
/// <list type="bullet">
/// <item><c>StazioniMeteo</c> (16 righe; Latitudine/Longitudine float): ultimo dato = MAX(Data) di
/// <c>MeteoGiornaliero</c> con IdVigneto della stazione e Previsione = 0 (indice su IdVigneto + Data).
/// L'azienda è quella del vigneto oppure <c>StazioniMeteo.IdEnte</c>.</item>
/// <item><c>Dispositivo</c> (11 righe) + <c>Sensore</c>: solo stato del collegamento e registrazione.</item>
/// <item><c>Trappola</c> (7.324 righe, solo codice e vigneto): contate per vigneto; ultimo rilievo =
/// MAX(InizioRilievo) dei tipi di monitoraggio 147–155 di <c>Rilievo</c>, per vigneto (indice
/// IX_Rilievo_Vigneto_Data). Le catture non si leggono (servirebbero altri GRANT).</item>
/// </list>
/// </summary>
public sealed class FourGrapesDispositiviRepository : IDispositiviRepository
{
    private const string FiltroSegnaposto = "/*aziende*/";

    /// <summary>Tipi di rilievo del monitoraggio con trappole (Cicaline … Argyrotenia, dba 2026-10-13).</summary>
    public const int PrimoTipoTrappole = 147;
    public const int UltimoTipoTrappole = 155;

    private const string SelectStazioni = """
        SELECT vg.Ente_IdEnte AS EnteVigneto, s.IdEnte AS EnteStazione, s.IdVigneto, s.IdStazione, s.Descrizione,
               s.Latitudine, s.Longitudine, u.Ultimo
        FROM dbo.StazioniMeteo s
        LEFT JOIN dbo.Vigneto v ON v.IdVigneto = s.IdVigneto
        LEFT JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        OUTER APPLY (SELECT MAX(m.Data) AS Ultimo FROM dbo.MeteoGiornaliero m
                     WHERE m.IdVigneto = s.IdVigneto AND m.Previsione = 0 AND m.Data <= @Limite) u
        WHERE 1 = 1 /*aziende*/
        """;

    private const string SelectSensori = """
        SELECT vg.Ente_IdEnte AS IdEnte, d.IdVigneto, d.Codice, se.Descrizione AS Sensore, d.Status, d.DataRegistrazione
        FROM dbo.Dispositivo d
        JOIN dbo.Vigneto v ON v.IdVigneto = d.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        LEFT JOIN dbo.Sensore se ON se.Id = d.IdSensore
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte > 0 /*aziende*/
        """;

    private const string SelectTrappole = """
        WITH trappole AS (
            SELECT t.Vigneto_IdVigneto AS IdVigneto, COUNT(*) AS Trappole
            FROM dbo.Trappola t
            GROUP BY t.Vigneto_IdVigneto
        )
        SELECT vg.Ente_IdEnte AS IdEnte, tr.IdVigneto, tr.Trappole, u.Ultimo
        FROM trappole tr
        JOIN dbo.Vigneto v ON v.IdVigneto = tr.IdVigneto
        JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
        OUTER APPLY (SELECT MAX(r.InizioRilievo) AS Ultimo FROM dbo.Rilievo r
                     WHERE r.Vigneto_IdVigneto = tr.IdVigneto AND r.InizioRilievo <= @Limite
                       AND r.TipoRilievo_idTipoRilievo BETWEEN @PrimoTipo AND @UltimoTipo) u
        WHERE v.IdVigneto > 0 AND vg.Ente_IdEnte > 0 /*aziende*/
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromSeconds(60);

    private readonly FourGrapesDbContext _db;
    private readonly ILogger<FourGrapesDispositiviRepository> _logger;
    private readonly TimeProvider _orologio;

    public FourGrapesDispositiviRepository(FourGrapesDbContext db, ILogger<FourGrapesDispositiviRepository> logger, TimeProvider? orologio = null)
    {
        _db = db;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    public async Task<DispositiviLetti> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
    {
        if (aziende is { Count: 0 })
        {
            return new DispositiviLetti([], [], []);
        }

        // Date in ora italiana; un giorno di margine copre il fuso del server, le date oltre sono errori.
        var limite = _orologio.GetUtcNow().UtcDateTime.AddDays(1);
        var scartate = 0;

        var filtroStazioni = aziende is null ? "" : $"AND ({InAziende("vg.Ente_IdEnte", aziende)} OR {InAziende("s.IdEnte", aziende)})";
        var stazioni = new List<StazioneMeteo>();
        using (var comando = await CreaComandoAsync(SelectStazioni.Replace(FiltroSegnaposto, filtroStazioni), aziende, ct))
        {
            comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = limite });
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiStazione(reader, aziende) is { } stazione) stazioni.Add(stazione); else scartate++;
            }
        }

        var sensori = new List<SensoreVigneto>();
        using (var comando = await CreaComandoAsync(SelectSensori.Replace(FiltroSegnaposto, Filtro(aziende)), aziende, ct))
        {
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiSensore(reader) is { } sensore) sensori.Add(sensore); else scartate++;
            }
        }

        var trappole = new List<TrappoleVigneto>();
        using (var comando = await CreaComandoAsync(SelectTrappole.Replace(FiltroSegnaposto, Filtro(aziende)), aziende, ct))
        {
            comando.Parameters.Add(new SqlParameter("@Limite", SqlDbType.DateTime2) { Value = limite });
            comando.Parameters.Add(new SqlParameter("@PrimoTipo", PrimoTipoTrappole));
            comando.Parameters.Add(new SqlParameter("@UltimoTipo", UltimoTipoTrappole));
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (LeggiTrappole(reader) is { } riga) trappole.Add(riga); else scartate++;
            }
        }

        if (scartate > 0)
        {
            _logger.LogWarning("Dispositivi: {Scartate} righe illeggibili o fuori dalle aziende saltate", scartate);
        }

        return new DispositiviLetti(stazioni, sensori, trappole);
    }

    /// <summary>
    /// Una stazione; null senza vigneto o senza un'azienda valida fra quelle chieste (prima quella del
    /// vigneto, poi quella della stazione). Coordinate fuori intervallo o 0/0 → senza posizione propria.
    /// </summary>
    public static StazioneMeteo? LeggiStazione(IDataRecord riga, IReadOnlySet<int>? aziende)
    {
        if (riga.Intero("IdVigneto") is not { } vigneto)
        {
            return null;
        }

        bool Valida(int? id) => id is > 0 && (aziende is null || aziende.Contains(id.Value));
        var enteVigneto = riga.Intero("EnteVigneto");
        var enteStazione = riga.Intero("EnteStazione");
        int? azienda = Valida(enteVigneto) ? enteVigneto : Valida(enteStazione) ? enteStazione : null;
        if (azienda is null)
        {
            return null;
        }

        var lat = riga.Reale("Latitudine");
        var lon = riga.Reale("Longitudine");
        if (lat is not { } la || lon is not { } lo || la is < -90 or > 90 || lo is < -180 or > 180 || (la == 0 && lo == 0))
        {
            lat = lon = null;
        }

        return new StazioneMeteo(azienda.Value, vigneto, riga.Intero("IdStazione"), riga.Testo("Descrizione"), lat, lon,
            riga.Valore("Ultimo") as DateTime?);
    }

    /// <summary>Un dispositivo; null senza azienda, vigneto o codice.</summary>
    public static SensoreVigneto? LeggiSensore(IDataRecord riga) =>
        riga.Intero("IdEnte") is { } azienda && riga.Intero("IdVigneto") is { } vigneto && riga.Testo("Codice") is { } codice
            ? new SensoreVigneto(azienda, vigneto, codice, riga.Testo("Sensore"), riga.Testo("Status"), riga.Valore("DataRegistrazione") as DateTime?)
            : null;

    /// <summary>Le trappole di un vigneto; null senza azienda o vigneto o con un conteggio non positivo.</summary>
    public static TrappoleVigneto? LeggiTrappole(IDataRecord riga) =>
        riga.Intero("IdEnte") is { } azienda && riga.Intero("IdVigneto") is { } vigneto && riga.Intero("Trappole") is { } quante and > 0
            ? new TrappoleVigneto(azienda, vigneto, quante,riga.Valore("Ultimo") as DateTime?)
            : null;

    /// <summary>Le aziende (poche decine) come parametri, mai concatenate nel testo.</summary>
    private static string InAziende(string colonna, IReadOnlySet<int> aziende) =>
        $"{colonna} IN ({string.Join(", ", aziende.Select((_, i) => $"@a{i}"))})";

    private static string Filtro(IReadOnlySet<int>? aziende) =>
        aziende is null ? "" : $"AND {InAziende("vg.Ente_IdEnte", aziende)}";

    private async Task<SqlCommand> CreaComandoAsync(string sql, IReadOnlySet<int>? aziende, CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;
        var i = 0;
        foreach (var id in aziende ?? Enumerable.Empty<int>())
        {
            comando.Parameters.Add(new SqlParameter($"@a{i++}", id));
        }

        return comando;
    }
}
