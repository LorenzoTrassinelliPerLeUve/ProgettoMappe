namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono utenti (vedi <see cref="InMemoryUtentiRepository"/>).</summary>
public class InMemoryAccessoUnicoRepository : IAccessoUnicoRepository
{
    public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<AnagraficaUtente?>(null);

    public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AziendaUtente>>([]);

    public Task<IReadOnlyList<AbilitazionePersonale>> GetAbilitazioniPersonaliAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AbilitazionePersonale>>([]);

    public Task<AbilitazionePersonale?> GetAbilitazionePersonaleAsync(int id, CancellationToken ct = default)
        => Task.FromResult<AbilitazionePersonale?>(null);

    public Task<IReadOnlySet<string>> GetCodiciAbilitazioneAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

    public Task<bool> EsisteLoginAsync(string loginName, CancellationToken ct = default) => Task.FromResult(false);

    public Task<bool> AbbinatoAsync(int idUtente, int idEnte, CancellationToken ct = default) => Task.FromResult(false);
}
