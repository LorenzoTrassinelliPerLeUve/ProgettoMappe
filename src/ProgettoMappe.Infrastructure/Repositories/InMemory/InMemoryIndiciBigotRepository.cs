using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Indici BIGOT di esempio (non reali) per i vigneti in-memory.</summary>
public sealed class InMemoryIndiciBigotRepository : IIndiciBigotRepository
{
    private static readonly IReadOnlyList<IndiceBigotVigneto> Dati =
    [
        new(2024, 1, 9, 78, 223, "ALTA", 3.1m, 2.2m, 1.4m, 20, "COMPATTO", 99.5m, "LIEVE", 0m),
        new(2025, 1, 9, 84, 223, "MOLTO ALTA", 3.3m, 1.9m, 1.7m, 21, "MEDIO COMPATTO", 99.9m, "ASSENTE", 1m),
        new(2024, 2, 1, 2, 10, null, null, null, null, 8, null, null, null, null),
        new(2025, 2, 9, 61, 223, "MEDIA", 2.2m, 2.8m, 0.8m, 9, "SPARGOLO", 96.0m, "SEVERO", 0m),
    ];

    public Task<IReadOnlyList<IndiceBigotVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IndiceBigotVigneto>?>(Dati.Where(d => idVigneti.Contains(d.IdVigneto)).ToList());
}
