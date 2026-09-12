using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Accesso in sola lettura ai vigneti. Vedi <see cref="IAziendeRepository"/> per il motivo
/// dell'astrazione rispetto a 4Grapes.
/// </summary>
public interface IVignetiRepository
{
    Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default);

    Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default);
}
