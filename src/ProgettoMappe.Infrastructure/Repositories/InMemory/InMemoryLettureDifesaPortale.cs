using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes le tabelle della difesa sono vuote.</summary>
public class InMemoryLettureDifesaPortale : ILettureDifesaPortale
{
    public Task<IReadOnlyList<AvversitaDaTrattareDto>> AvversitaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AvversitaDaTrattareDto>>([]);

    public Task<IReadOnlyList<IrroratriceDto>> IrroratriciAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceDto>>([]);

    public Task<IReadOnlyList<VocePortaleDto>> TipiIrroratriceAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([]);

    public Task<IReadOnlyList<IrroratriceAziendaDto>> IrroratriciAziendaAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IrroratriceAziendaDto>>([]);

    public Task<IReadOnlyList<int>> AziendeModificabiliAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    public Task<int?> AziendaDellIrroratriceAsync(int id, CancellationToken ct = default) => Task.FromResult<int?>(null);
}
