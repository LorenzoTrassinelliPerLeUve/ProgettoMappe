using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono rilievi della nutrizione.</summary>
public class InMemoryLettureNutrizionePortale : ILettureNutrizionePortale
{
    public Task<IReadOnlyList<RilievoDto>> RilieviAsync(string? vista, int tipo, IReadOnlyList<string> colonne,
        IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RilievoDto>>([]);

    public Task<(int IdTessitura, int IdTessituraSuolo)?> TessituraAsync(int sabbia, int limo, int argilla,
        CancellationToken ct = default) =>
        Task.FromResult<(int, int)?>(null);

    public Task<IReadOnlyList<SottovignetoMenuDto>> SottovignetiAsync(int idVigneto, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SottovignetoMenuDto>>([]);

    public Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoMenuDto>>([]);

    public Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default) =>
        Task.FromResult<VignetoRilievo?>(null);

    public Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default) =>
        Task.FromResult<RilievoEsistente?>(null);

    public Task<IReadOnlyList<ObiettivoRigaDto>> ObiettiviAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ObiettivoRigaDto>>([]);

    public Task<ObiettivoEsistente?> ObiettivoAsync(int idVigneto, int anno, CancellationToken ct = default) =>
        Task.FromResult<ObiettivoEsistente?>(null);
}
