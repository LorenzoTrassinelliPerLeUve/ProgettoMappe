namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono rilievi delle app.</summary>
public class InMemoryLettureRilieviApp : ILettureRilieviApp
{
    public Task<IReadOnlyList<RilievoApp>> RilieviAsync(TipoRilievoApp tipo, int idEnte, int? idVigneto, DateOnly? da, DateOnly? a,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RilievoApp>>([]);

    public Task<RilievoApp?> PerCodiceAsync(TipoRilievoApp tipo, string codice, CancellationToken ct = default) =>
        Task.FromResult<RilievoApp?>(null);
}
