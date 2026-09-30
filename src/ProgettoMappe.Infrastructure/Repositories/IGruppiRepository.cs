using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Gruppi di aziende, in sola lettura: solo quelli con almeno un'azienda valida.</summary>
public interface IGruppiRepository
{
    Task<IReadOnlyList<Gruppo>> GetGruppiAsync(CancellationToken ct = default);
}
