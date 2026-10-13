using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Statistiche reali dei vigneti, servite dalla copia in memoria (vedi <see cref="CacheStatisticheVigneti"/>).</summary>
public sealed class FourGrapesStatisticheVignetiRepository : IStatisticheVignetiRepository
{
    private readonly CacheStatisticheVigneti _cache;

    public FourGrapesStatisticheVignetiRepository(CacheStatisticheVigneti cache)
    {
        _cache = cache;
    }

    public Task<StatisticheVigneti?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult(_cache.Per(idVigneti));

    public StatoIndiciBigot Stato => _cache.Stato;

    public bool AvviaRicarico(CancellationToken stop) => _cache.AvviaRicarico(stop);
}
