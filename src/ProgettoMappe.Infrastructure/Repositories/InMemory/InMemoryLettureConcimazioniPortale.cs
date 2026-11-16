using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes non ci sono concimazioni.</summary>
public class InMemoryLettureConcimazioniPortale : ILettureConcimazioniPortale
{
    public Task<IReadOnlyList<TargetEnologicoDto>> TargetAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TargetEnologicoDto>>([]);

    public Task<IReadOnlyList<VocePortaleDto>> EpocheAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([]);

    public Task<IReadOnlyList<VocePortaleDto>> ModalitaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([]);

    public Task<IReadOnlyList<VignetoConcimazioneDto>> VignetiAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoConcimazioneDto>>([]);

    public Task<IReadOnlyList<UnitaBaseDto>> UnitaBaseAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UnitaBaseDto>>([]);

    public Task<UnitaConcimazioneDto> UnitaConcimazioneAsync(int anno, int azienda, CancellationToken ct = default) =>
        Task.FromResult(new UnitaConcimazioneDto([], [], []));

    public Task<IReadOnlyList<FattoreConcimazioneDto>> FattoriAsync(int anno, int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FattoreConcimazioneDto>>([]);

    public Task<ResocontoConcimazioniDto> ResocontoAsync(int anno, int azienda, int? target, int? epoca, CancellationToken ct = default) =>
        Task.FromResult(new ResocontoConcimazioniDto([], [], [], false));

    public Task<ConcimazioneRealeDto?> ConcimazioneRealeAsync(int id, CancellationToken ct = default) =>
        Task.FromResult<ConcimazioneRealeDto?>(null);

    public Task<int> ProssimoInserimentoAsync(CancellationToken ct = default) => Task.FromResult(1);

    public Task<IReadOnlyList<(string Foglio, TabellaPortaleDto Tabella)>> EsportaUnitaAsync(string lingua, int anno, int azienda,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<(string, TabellaPortaleDto)>>([]);
}
