using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Sottovigneti di esempio (non reali): il vigneto 1 diviso in due.</summary>
public sealed class InMemorySottovignetiRepository : ISottovignetiRepository
{
    public Task<IReadOnlyList<Sottovigneto>> GetPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var vigneti = InMemoryFourGrapesData.Vigneti.Where(v => v.AziendaId == aziendaId).Select(v => v.Id).ToHashSet();
        return Task.FromResult<IReadOnlyList<Sottovigneto>>(
            InMemoryFourGrapesData.Sottovigneti.Where(s => vigneti.Contains(s.IdVigneto)).ToList());
    }
}
