using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>Rilievi BBCH di esempio (non reali) per i vigneti in-memory, riassunti come quelli veri.</summary>
public sealed class InMemoryFenologiaRepository : IFenologiaRepository
{
    private static readonly Dictionary<int, string> Fasi = new()
    {
        [57] = "infiorescenze pienamente sviluppate; fiori tra di loro separati",
        [65] = "Piena fioritura: 50% delle caliptre è caduto",
        [75] = "Acini delle dimensioni di un pisello",
        [81] = "25% dell'invaiatura",
        [89] = "Vendemmia",
    };

    private static readonly IReadOnlyList<FenologiaVigneto> Dati = CacheFenologia.Riassumi(
    [
        new(2024, 1, new DateOnly(2024, 6, 10), 65),
        new(2024, 1, new DateOnly(2024, 9, 20), 89),
        new(2025, 1, new DateOnly(2025, 5, 28), 57),
        new(2025, 1, new DateOnly(2025, 6, 12), 65),
        new(2025, 2, new DateOnly(2025, 7, 2), 75),
        new(2025, 2, new DateOnly(2025, 8, 5), 81),
    ], Fasi);

    private static readonly DateTimeOffset Avvio = DateTimeOffset.UtcNow;

    public Task<IReadOnlyList<FenologiaVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FenologiaVigneto>?>(Dati.Where(f => idVigneti.Contains(f.IdVigneto)).ToList());

    /// <summary>Dati fissi: "caricati" all'avvio del processo.</summary>
    public StatoIndiciBigot Stato => new(true, false, Avvio, null);

    /// <summary>Niente da rileggere: i dati di esempio sono fissi.</summary>
    public bool AvviaRicarico(CancellationToken stop) => true;
}
