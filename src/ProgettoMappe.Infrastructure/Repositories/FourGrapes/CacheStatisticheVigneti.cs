using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Copia in memoria delle tre viste statistiche di 4Grapes (dba, 2026-10-13): rischi
/// (<c>VistaStatisticheMappaVigneti</c>, ~85.000 righe), vigoria
/// (<c>VistaStatisticheVigoriaMappaVigneti</c>, ~17.000 righe con anno) e potenziale idrico
/// (<c>VistaStatistichePotenzialeIdricoMappaVigneti</c>, una riga per vigneto). Lette tutte in
/// pochi secondi, senza il poligono (la mappa usa quello che ha già). Uniche per (IdVigneto, Anno);
/// le righe senza anno (vigneti senza dati, in LEFT JOIN nelle viste) si saltano. Le viste coprono
/// tutti i vigneti di 4Grapes: il filtro per azienda lo fa chi chiede (<see cref="Per"/>).
/// </summary>
public sealed class CacheStatisticheVigneti : CacheVista
{
    private const string QueryRischi = """
        SELECT Anno, IdVigneto, Rischio_OIDIO, Rischio_PERONOSPORA
        FROM dbo.VistaStatisticheMappaVigneti
        WHERE Anno IS NOT NULL AND IdVigneto IS NOT NULL
        """;

    private const string QueryVigoria = """
        SELECT Anno, IdVigneto, PunteggioVigoria
        FROM dbo.VistaStatisticheVigoriaMappaVigneti
        WHERE Anno IS NOT NULL AND IdVigneto IS NOT NULL
        """;

    private const string QueryPotenzialeIdrico = """
        SELECT IdVigneto, PotenzialeIdrico
        FROM dbo.VistaStatistichePotenzialeIdricoMappaVigneti
        WHERE IdVigneto IS NOT NULL AND PotenzialeIdrico IS NOT NULL
        """;

    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromMinutes(2);

    private volatile Copia? _copia;

    public CacheStatisticheVigneti(IServiceScopeFactory scopeFactory, ILogger<CacheStatisticheVigneti> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string Nome => "statistiche dei vigneti (rischi, vigoria, potenziale idrico)";

    public override bool Pronta => _copia is not null;

    /// <summary>Statistiche dei vigneti richiesti, o null se la cache non è ancora stata caricata.</summary>
    public StatisticheVigneti? Per(IEnumerable<int> idVigneti)
    {
        var copia = _copia;
        if (copia is null)
        {
            return null;
        }

        var ids = idVigneti.Distinct().ToList();
        return new StatisticheVigneti(
            ids.SelectMany(id => copia.Rischi.TryGetValue(id, out var r) ? r : []).ToList(),
            ids.SelectMany(id => copia.Vigoria.TryGetValue(id, out var v) ? v : []).ToList(),
            ids.Select(id => copia.PotenzialeIdrico.GetValueOrDefault(id)).OfType<PotenzialeIdricoVigneto>().ToList());
    }

    protected override async Task<string> LeggiAsync(SqlConnection connessione, CancellationToken ct)
    {
        // Prima si leggono tutte e tre, poi si sostituisce la copia: mai metà vecchia e metà nuova.
        // Il nome della vista nel log dice quale manca, se una lettura fallisce (es. GRANT mancante).
        var rischi = await LeggiVistaAsync(connessione, QueryRischi, "VistaStatisticheMappaVigneti", LeggiRischio, ct);
        var vigoria = await LeggiVistaAsync(connessione, QueryVigoria, "VistaStatisticheVigoriaMappaVigneti", LeggiVigoria, ct);
        var potenziale = await LeggiVistaAsync(connessione, QueryPotenzialeIdrico, "VistaStatistichePotenzialeIdricoMappaVigneti",
            LeggiPotenzialeIdrico, ct);

        _copia = new Copia(
            rischi.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => (IReadOnlyList<RischioVigneto>)g.OrderBy(r => r.Anno).ToList()),
            vigoria.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => (IReadOnlyList<VigoriaVigneto>)g.OrderBy(r => r.Anno).ToList()),
            potenziale.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.First()));
        return $"{rischi.Count} righe di rischio, {vigoria.Count} di vigoria, {potenziale.Count} di potenziale idrico";
    }

    private async Task<List<T>> LeggiVistaAsync<T>(SqlConnection connessione, string query, string vista,
        Func<IDataRecord, T?> leggi, CancellationToken ct) where T : class
    {
        try
        {
            await using var comando = connessione.CreateCommand();
            comando.CommandText = query;
            comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;

            var righe = new List<T>();
            var scartate = 0;
            await using var reader = await comando.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (leggi(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }

            if (scartate > 0)
            {
                Logger.LogWarning("{Vista}: {Scartate} righe saltate perché senza Anno o IdVigneto leggibile", vista, scartate);
            }
            return righe;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"lettura di {vista}: {ex.Message}", ex);
        }
    }

    /// <summary>Una riga dei rischi; null se Anno o IdVigneto non sono leggibili.</summary>
    public static RischioVigneto? LeggiRischio(IDataRecord r) =>
        r.Intero("Anno") is { } anno && r.Intero("IdVigneto") is { } id
            ? new RischioVigneto(anno, id, r.Decimale("Rischio_OIDIO"), r.Decimale("Rischio_PERONOSPORA"))
            : null;

    /// <summary>Una riga della vigoria; null se Anno o IdVigneto non sono leggibili.</summary>
    public static VigoriaVigneto? LeggiVigoria(IDataRecord r) =>
        r.Intero("Anno") is { } anno && r.Intero("IdVigneto") is { } id
            ? new VigoriaVigneto(anno, id, r.Decimale("PunteggioVigoria"))
            : null;

    /// <summary>Una riga del potenziale idrico; null se IdVigneto non è leggibile.</summary>
    public static PotenzialeIdricoVigneto? LeggiPotenzialeIdrico(IDataRecord r) =>
        r.Intero("IdVigneto") is { } id ? new PotenzialeIdricoVigneto(id, r.Decimale("PotenzialeIdrico")) : null;

    private sealed record Copia(
        IReadOnlyDictionary<int, IReadOnlyList<RischioVigneto>> Rischi,
        IReadOnlyDictionary<int, IReadOnlyList<VigoriaVigneto>> Vigoria,
        IReadOnlyDictionary<int, PotenzialeIdricoVigneto> PotenzialeIdrico);
}
