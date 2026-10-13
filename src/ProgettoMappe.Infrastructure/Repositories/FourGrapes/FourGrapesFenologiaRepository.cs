using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Fenologia reale dei vigneti, servita dalla copia in memoria (vedi <see cref="CacheFenologia"/>).</summary>
public sealed class FourGrapesFenologiaRepository : IFenologiaRepository
{
    private readonly CacheFenologia _cache;

    public FourGrapesFenologiaRepository(CacheFenologia cache)
    {
        _cache = cache;
    }

    public Task<IReadOnlyList<FenologiaVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult(_cache.Per(idVigneti));

    public StatoIndiciBigot Stato => _cache.Stato;

    public bool AvviaRicarico(CancellationToken stop) => _cache.AvviaRicarico(stop);
}
