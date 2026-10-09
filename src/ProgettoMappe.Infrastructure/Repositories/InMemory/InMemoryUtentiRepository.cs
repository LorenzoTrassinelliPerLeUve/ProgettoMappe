namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Senza 4Grapes non ci sono utenti: il login non riesce mai. Di proposito nessun utente di
/// esempio, così una credenziale nota non può finire attiva per errore.
/// </summary>
public class InMemoryUtentiRepository : IUtentiRepository
{
    public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CredenzialiUtente>>([]);
}
