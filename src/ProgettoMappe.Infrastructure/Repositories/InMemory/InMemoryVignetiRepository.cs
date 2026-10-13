using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Implementazione con dati di esempio, da sostituire con l'accesso reale a 4Grapes una
/// volta nota la struttura del database (vedi <see cref="IVignetiRepository"/>).
/// </summary>
public class InMemoryVignetiRepository : IVignetiRepository
{
    public Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Vigneto>>(
            InMemoryFourGrapesData.Vigneti.Where(v => v.AziendaId == aziendaId).ToList());

    public Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(InMemoryFourGrapesData.Vigneti.FirstOrDefault(v => v.Id == id));

    public Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SuperficieVigneto>>(InMemoryFourGrapesData.Vigneti
            .Where(v => aziende is null || aziende.Contains(v.AziendaId))
            .Select(v => new SuperficieVigneto(v.Id, v.AziendaId, v.SuperficieEttari))
            .ToList());
}
