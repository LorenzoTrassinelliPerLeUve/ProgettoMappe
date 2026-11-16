using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono rilievi della nutrizione.</summary>
public class InMemoryLettureNutrizionePortale : ILettureNutrizionePortale
{
    public Task<IReadOnlyList<RilievoDto>> RilieviAsync(string vista, IReadOnlyList<string> colonne, IReadOnlyList<int>? soloEnti,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RilievoDto>>([]);

    public Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoMenuDto>>([]);

    public Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default) =>
        Task.FromResult<VignetoRilievo?>(null);

    public Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default) =>
        Task.FromResult<RilievoEsistente?>(null);
}
