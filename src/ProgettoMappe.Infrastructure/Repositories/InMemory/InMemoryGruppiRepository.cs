using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Gruppi di esempio (non reali) per le aziende in-memory.</summary>
public sealed class InMemoryGruppiRepository : IGruppiRepository
{
    public Task<IReadOnlyList<Gruppo>> GetGruppiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Gruppo>>(
        [
            new Gruppo(1, "Gruppo di prova", new DateOnly(2020, 1, 1), new DateOnly(2100, 12, 31), [1, 2]),
            new Gruppo(2, "Gruppo concluso di prova", new DateOnly(2020, 1, 1), new DateOnly(2023, 1, 1), [2]),
        ]);
}
