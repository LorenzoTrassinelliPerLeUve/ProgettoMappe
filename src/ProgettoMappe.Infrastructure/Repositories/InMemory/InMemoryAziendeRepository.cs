using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Implementazione con dati di esempio, da sostituire con l'accesso reale a 4Grapes una
/// volta nota la struttura del database (vedi <see cref="IAziendeRepository"/>).
/// </summary>
public class InMemoryAziendeRepository : IAziendeRepository
{
    public Task<IReadOnlyList<Azienda>> GetAziendeAsync(CancellationToken ct = default) =>
        Task.FromResult(InMemoryFourGrapesData.Aziende);

    public Task<Azienda?> GetAziendaAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(InMemoryFourGrapesData.Aziende.FirstOrDefault(a => a.Id == id));
}
