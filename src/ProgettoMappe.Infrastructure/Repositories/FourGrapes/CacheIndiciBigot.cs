using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Copia in memoria dell'intera vista 4Grapes <c>VistaIndiceBIGOT</c> (~112.000 righe, pochi MB).
/// La vista impiega 30–40 s per qualsiasi query, anche filtrata per un solo vigneto: interrogarla
/// ad ogni apertura della mappa non è praticabile. La si legge quindi tutta con UNA query
/// (in background, dal servizio AggiornamentoIndiciBigot dell'Api), indicizzata per IdVigneto, e la
/// si rilegge periodicamente (i dati sono annuali). Sola lettura: solo SELECT.
/// Singleton: usa uno scope dedicato per il <see cref="FourGrapesDbContext"/> (scoped).
/// </summary>
public sealed class CacheIndiciBigot
{
    private const string Query = """
        SELECT Anno, IdVigneto, NumParametri, Punteggio, Peso, Vigoria, SFE, Produzione, SFEKg,
               EtaVigneto, Morfologia, Ampelopatie, RegimeIdrico, Biodiversita
        FROM dbo.VistaIndiceBIGOT
        WHERE Anno IS NOT NULL AND IdVigneto IS NOT NULL
        """;

    /// <summary>La vista impiega ~40 s: il timeout predefinito di 30 s non basta.</summary>
    private static readonly TimeSpan TimeoutQuery = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheIndiciBigot> _logger;
    private readonly SemaphoreSlim _caricamento = new(1, 1);
    private volatile IReadOnlyDictionary<int, IReadOnlyList<IndiceBigotVigneto>>? _perVigneto;

    public CacheIndiciBigot(IServiceScopeFactory scopeFactory, ILogger<CacheIndiciBigot> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool Pronta => _perVigneto is not null;

    public DateTimeOffset? UltimoAggiornamento { get; private set; }

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

    /// <summary>
    /// Rilegge la vista. Un solo caricamento alla volta; in caso di errore si tiene la copia
    /// precedente (se c'è) e si registra solo il messaggio, mai la connection string.
    /// </summary>
    public async Task AggiornaAsync(CancellationToken ct)
    {
        await _caricamento.WaitAsync(ct);
        try
        {
            var inizio = DateTimeOffset.UtcNow;
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FourGrapesDbContext>();
            var connessione = (SqlConnection)db.Database.GetDbConnection();
            if (connessione.State != ConnectionState.Open)
            {
                await connessione.OpenAsync(ct);
            }

            await using var comando = connessione.CreateCommand();
            comando.CommandText = Query;
            comando.CommandTimeout = (int)TimeoutQuery.TotalSeconds;

            var righe = new List<IndiceBigotVigneto>(capacity: 120_000);
            await using (var reader = await comando.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    righe.Add(Leggi(reader));
                }
            }

            _perVigneto = righe
                .GroupBy(r => r.IdVigneto)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<IndiceBigotVigneto>)g.OrderBy(r => r.Anno).ToList());
            UltimoAggiornamento = DateTimeOffset.UtcNow;

            _logger.LogInformation("Indici BIGOT caricati: {Righe} righe, {Vigneti} vigneti in {Secondi:0.0} s",
                righe.Count, _perVigneto.Count, (UltimoAggiornamento.Value - inizio).TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Caricamento indici BIGOT non riuscito ({Tipo}: {Messaggio}); {Stato}",
                ex.GetType().Name, ex.Message, Pronta ? "resta in uso la copia precedente" : "dati non disponibili");
        }
        finally
        {
            _caricamento.Release();
        }
    }

    private static IndiceBigotVigneto Leggi(SqlDataReader r)
    {
        int? Int(string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetInt32(i); }
        decimal? Dec(string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetDecimal(i); }
        string? Str(string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetString(i).Trim(); }

        return new IndiceBigotVigneto(
            r.GetInt32(r.GetOrdinal("Anno")),
            r.GetInt32(r.GetOrdinal("IdVigneto")),
            Int("NumParametri"),
            Int("Punteggio"),
            Int("Peso"),
            Str("Vigoria"),
            Dec("SFE"),
            Dec("Produzione"),
            Dec("SFEKg"),
            Int("EtaVigneto"),
            Str("Morfologia"),
            Dec("Ampelopatie"),
            Str("RegimeIdrico"),
            Dec("Biodiversita"));
    }
}
