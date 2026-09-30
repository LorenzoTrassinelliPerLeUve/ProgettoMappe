using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Indici BIGOT reali, serviti dalla copia in memoria della vista (vedi <see cref="CacheIndiciBigot"/>).</summary>
public sealed class FourGrapesIndiciBigotRepository : IIndiciBigotRepository
{
    private readonly CacheIndiciBigot _cache;

    public FourGrapesIndiciBigotRepository(CacheIndiciBigot cache)
    {
        _cache = cache;
    }

    public Task<IReadOnlyList<IndiceBigotVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult(_cache.Per(idVigneti));
}
