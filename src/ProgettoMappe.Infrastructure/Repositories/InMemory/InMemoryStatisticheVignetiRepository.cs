using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Statistiche di esempio (non reali) per i vigneti in-memory.</summary>
public sealed class InMemoryStatisticheVignetiRepository : IStatisticheVignetiRepository
{
    private static readonly StatisticheVigneti Dati = new(
        [
            new(2024, 1, 3m, 4m),
            new(2025, 1, 6m, 5m),
            new(2024, 2, null, null),
            new(2025, 2, 4m, 8m),
        ],
        [
            new(2024, 1, 5m),
            new(2025, 1, 7m),
            new(2025, 2, 2m),
        ],
        [
            new(1, 182.4m),
        ]);

    private static readonly DateTimeOffset Avvio = DateTimeOffset.UtcNow;

    public Task<StatisticheVigneti?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult<StatisticheVigneti?>(new StatisticheVigneti(
            Dati.Rischi.Where(r => idVigneti.Contains(r.IdVigneto)).ToList(),
            Dati.Vigoria.Where(r => idVigneti.Contains(r.IdVigneto)).ToList(),
            Dati.PotenzialeIdrico.Where(r => idVigneti.Contains(r.IdVigneto)).ToList()));

    /// <summary>Dati fissi: "caricati" all'avvio del processo.</summary>
    public StatoIndiciBigot Stato => new(true, false, Avvio, null);

    /// <summary>Niente da rileggere: i dati di esempio sono fissi.</summary>
    public bool AvviaRicarico(CancellationToken stop) => true;
}
