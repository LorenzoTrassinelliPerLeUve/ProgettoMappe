using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono utenti né impostazioni.</summary>
public class InMemoryLettureAmministrazionePortale : ILettureAmministrazionePortale
{
    public Task<IReadOnlyList<UtenteListaDto>> UtentiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UtenteListaDto>>([]);

    public Task<UtenteSchedaDto?> UtenteAsync(int id, CancellationToken ct = default) => Task.FromResult<UtenteSchedaDto?>(null);

    public Task<ImpostazioniDto?> ImpostazioniAsync(CancellationToken ct = default) => Task.FromResult<ImpostazioniDto?>(null);
}
