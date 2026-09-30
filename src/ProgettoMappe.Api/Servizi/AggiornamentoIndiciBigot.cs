using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Api.Servizi;

/// <summary>
/// Carica <see cref="CacheIndiciBigot"/> all'avvio (in background: l'Api risponde subito, gli
/// indici diventano disponibili dopo ~40 s) e la rilegge periodicamente. Registrato solo quando
/// è configurata la connessione a 4Grapes.
/// </summary>
public sealed class AggiornamentoIndiciBigot : BackgroundService
{
    public static readonly TimeSpan Intervallo = TimeSpan.FromHours(12);

    private readonly CacheIndiciBigot _cache;

    public AggiornamentoIndiciBigot(CacheIndiciBigot cache)
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
