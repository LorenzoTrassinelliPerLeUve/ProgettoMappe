using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Accesso in sola lettura alle aziende. L'implementazione reale interrogherà 4Grapes una
/// volta nota la struttura delle tabelle (vedi <c>FourGrapesDbContext</c>); nel frattempo
/// un'implementazione con dati di esempio permette di sviluppare e testare API e webapp
/// end-to-end.
/// </summary>
public interface IAziendeRepository
{
    Task<IReadOnlyList<Azienda>> GetAziendeAsync(CancellationToken ct = default);

    Task<Azienda?> GetAziendaAsync(int id, CancellationToken ct = default);
}
