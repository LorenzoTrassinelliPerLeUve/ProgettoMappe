using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Sottovigneti dei vigneti (task #66), letti per azienda come i vigneti.</summary>
public interface ISottovignetiRepository
{
    /// <summary>I sottovigneti di tutti i vigneti dell'azienda, ordinati per vigneto e nome.</summary>
    Task<IReadOnlyList<Sottovigneto>> GetPerAziendaAsync(int aziendaId, CancellationToken ct = default);
}
