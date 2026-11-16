using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono vigneti da modificare.</summary>
public class InMemoryLettureVignetiPortale : ILettureVignetiPortale
{
    public Task<VignetoSchedaDto?> VignetoAsync(int idVigneto, CancellationToken ct = default) => Task.FromResult<VignetoSchedaDto?>(null);

    public Task<MenuVignetoDto> MenuAsync(CancellationToken ct = default) =>
        Task.FromResult(new MenuVignetoDto([], [], [], [], [], [], [], [], [], []));
}
