using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Senza 4Grapes il Portale non ha dati: elenchi vuoti.</summary>
public class InMemoryLetturePortale : ILetturePortale
{
    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeAbbinateAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>([]);

    public Task<IReadOnlyList<AziendaPortaleDto>> TutteLeAziendeAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>([]);

    public Task<IReadOnlyList<int>> UtentiCheCondividonoAsync(int idUtente, int idAzienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<int>>([]);

    public Task<TabellaPortaleDto> RilieviExportAsync(string lingua, IReadOnlyList<int>? idUtenti, int idAzienda, int anno,
        CancellationToken ct = default) => Task.FromResult(new TabellaPortaleDto([], [], []));

    public Task<IReadOnlyList<AsportazioneDto>> AsportazioniAsync(int idAzienda, int? anno, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AsportazioneDto>>([]);

    public Task<IReadOnlyList<FotoGalleriaDto>> GalleriaAsync(int idAzienda, int anno, int? idUtenteVigneti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FotoGalleriaDto>>([]);
}
