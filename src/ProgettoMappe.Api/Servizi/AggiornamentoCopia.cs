using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Servizi;

/// <summary>
/// Carica una copia in memoria di viste 4Grapes (<see cref="CacheIndiciBigot"/>,
/// <see cref="CacheStatisticheVigneti"/>) all'avvio, in background: l'Api risponde subito, i dati
/// diventano disponibili dopo la lettura. Poi la rilegge periodicamente. Registrato solo quando è
/// configurata la connessione a 4Grapes.
/// </summary>
public sealed class AggiornamentoCopia<TCache> : BackgroundService where TCache : CacheVista
{
    public static readonly TimeSpan Intervallo = TimeSpan.FromHours(12);

    private readonly TCache _cache;

    public AggiornamentoCopia(TCache cache)
    {
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Primo caricamento subito; se non riesce si riprova dopo pochi minuti, poi ogni Intervallo.
        while (!stoppingToken.IsCancellationRequested)
        {
            await _cache.AggiornaAsync(stoppingToken);
            try
            {
                await Task.Delay(_cache.Pronta ? Intervallo : TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
