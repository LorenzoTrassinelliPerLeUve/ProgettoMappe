using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono utenti né impostazioni.</summary>
public class InMemoryLettureAmministrazionePortale : ILettureAmministrazionePortale
{
    public Task<IReadOnlyList<UtenteListaDto>> UtentiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UtenteListaDto>>([]);

    public Task<UtenteSchedaDto?> UtenteAsync(int id, CancellationToken ct = default) => Task.FromResult<UtenteSchedaDto?>(null);

    public Task<ImpostazioniDto?> ImpostazioniAsync(CancellationToken ct = default) => Task.FromResult<ImpostazioniDto?>(null);

    public Task<IReadOnlyList<AbilitazioneCategoriaDto>> AbilitazioniCategoriaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AbilitazioneCategoriaDto>>([]);

    public Task<IReadOnlyList<AbilitazionePersonaDto>> AbilitazioniPersonaAsync(int idUtente, DateOnly oggi, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AbilitazionePersonaDto>>([]);

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeUtenteAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>([]);

    public Task<IReadOnlyList<GruppoUtentiDto>> GruppiUtentiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GruppoUtentiDto>>([]);

    public Task<GruppoUtentiSchedaDto?> GruppoUtentiAsync(int id, CancellationToken ct = default) => Task.FromResult<GruppoUtentiSchedaDto?>(null);

    public Task<IReadOnlyList<PartecipanteGruppoDto>> CercaUtentiAsync(string testo, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PartecipanteGruppoDto>>([]);
}
