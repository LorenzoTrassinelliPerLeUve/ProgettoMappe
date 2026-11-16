namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono utenti (vedi <see cref="InMemoryUtentiRepository"/>).</summary>
public class InMemoryAccessoUnicoRepository : IAccessoUnicoRepository
{
    public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<AnagraficaUtente?>(null);

    public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AziendaUtente>>([]);
}
