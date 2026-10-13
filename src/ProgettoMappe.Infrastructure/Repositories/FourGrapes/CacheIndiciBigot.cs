using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Copia in memoria dell'intera vista 4Grapes <c>VistaIndiceBIGOT</c> (~112.000 righe, pochi MB).
/// La vista impiega 30–40 s per qualsiasi query, anche filtrata per un solo vigneto: interrogarla
/// ad ogni apertura della mappa non è praticabile. La si legge quindi tutta con UNA query
/// (in background, dal servizio AggiornamentoCopia dell'Api), indicizzata per IdVigneto, e la
/// si rilegge periodicamente (i dati sono annuali). Meccanismo comune in <see cref="CacheVista"/>.
/// </summary>
public sealed class CacheIndiciBigot : CacheVista
{
    private const string Query = """
        SELECT Anno, IdVigneto, NumParametri, Punteggio, Peso, Vigoria, SFE, Produzione, SFEKg,
               EtaVigneto, Morfologia, Ampelopatie, RegimeIdrico, Biodiversita
        FROM dbo.VistaIndiceBIGOT
        WHERE Anno IS NOT NULL AND IdVigneto IS NOT NULL
        """;

    /// <summary>La vista impiega ~40 s: il timeout predefinito di 30 s non basta.</summary>
    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromMinutes(5);

    private volatile IReadOnlyDictionary<int, IReadOnlyList<IndiceBigotVigneto>>? _perVigneto;

    public CacheIndiciBigot(IServiceScopeFactory scopeFactory, ILogger<CacheIndiciBigot> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string Nome => "indici BIGOT";

    public override bool Pronta => _perVigneto is not null;

    /// <summary>Righe dei vigneti richiesti, o null se la cache non è ancora stata caricata.</summary>
    public IReadOnlyList<IndiceBigotVigneto>? Per(IEnumerable<int> idVigneti)
    {
        var perVigneto = _perVigneto;
        if (perVigneto is null)
        {
            return null;
        }

        return idVigneti.Distinct()
            .SelectMany(id => perVigneto.TryGetValue(id, out var righe) ? righe : [])
            .ToList();
    }

    protected override async Task<string> LeggiAsync(SqlConnection connessione, CancellationToken ct)
    {
        await using var comando = connessione.CreateCommand();
        comando.CommandText = Query;
        comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;

        var righe = new List<IndiceBigotVigneto>(capacity: 120_000);
        var scartate = 0;
        await using (var reader = await comando.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                if (Leggi(reader) is { } riga)
                {
                    righe.Add(riga);
                }
                else
                {
                    scartate++;
                }
            }
        }

        if (scartate > 0)
        {
            Logger.LogWarning("Indici BIGOT: {Scartate} righe saltate perché senza Anno o IdVigneto leggibile", scartate);
        }

        var perVigneto = righe
            .GroupBy(r => r.IdVigneto)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IndiceBigotVigneto>)g.OrderBy(r => r.Anno).ToList());
        _perVigneto = perVigneto;
        return $"{righe.Count} righe, {perVigneto.Count} vigneti";
    }

    /// <summary>
    /// Una riga della vista; null se Anno o IdVigneto non sono leggibili (la riga va saltata).
    /// Gli altri valori NULL o di tipo inatteso diventano "dato mancante" (<see cref="LetturaSicura"/>).
    /// </summary>
    public static IndiceBigotVigneto? Leggi(IDataRecord r)
    {
        if (r.Intero("Anno") is not { } anno || r.Intero("IdVigneto") is not { } idVigneto)
        {
            return null;
        }

        return new IndiceBigotVigneto(
            anno,
            idVigneto,
            r.Intero("NumParametri"),
            r.Intero("Punteggio"),
            r.Intero("Peso"),
            r.Testo("Vigoria"),
            r.Decimale("SFE"),
            r.Decimale("Produzione"),
            r.Decimale("SFEKg"),
            r.Intero("EtaVigneto"),
            r.Testo("Morfologia"),
            r.Decimale("Ampelopatie"),
            r.Testo("RegimeIdrico"),
            r.Decimale("Biodiversita"));
    }
}
