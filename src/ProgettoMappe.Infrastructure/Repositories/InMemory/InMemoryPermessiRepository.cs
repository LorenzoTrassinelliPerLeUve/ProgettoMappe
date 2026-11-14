namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono utenti (vedi <see cref="InMemoryUtentiRepository"/>): nessun permesso.</summary>
public class InMemoryPermessiRepository : IPermessiRepository
{
    public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<PermessiUtente?>(null);
}
