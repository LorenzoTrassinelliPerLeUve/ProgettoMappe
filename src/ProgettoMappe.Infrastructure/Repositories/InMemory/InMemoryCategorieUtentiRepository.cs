namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Senza 4Grapes non ci sono utenti; l'utente di prova delle foto (<c>Portale:UtenteDiProva</c>, vedi
/// <see cref="InMemoryPermessiRepository"/>) è un interno Perleuve, così può provare le altre esperienze con «Vedi come…».
/// </summary>
public class InMemoryCategorieUtentiRepository : ICategorieUtentiRepository
{
    private readonly int? _utenteDiProva;

    public InMemoryCategorieUtentiRepository(int? utenteDiProva = null) => _utenteDiProva = utenteDiProva;

    public Task<SegnaliCategoria?> GetSegnaliAsync(int idUtente, IReadOnlyCollection<int> gruppiConsulenza, CancellationToken ct = default)
        => Task.FromResult(_utenteDiProva == idUtente
            ? new SegnaliCategoria(idUtente, "prova@perleuve.it", true, false, false, null, true)
            : null);
}
