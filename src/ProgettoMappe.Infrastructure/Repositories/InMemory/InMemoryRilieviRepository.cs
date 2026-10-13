using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Ultimi rilievi di esempio (non reali), con date relative a oggi così che si vedano tutte le classi:
/// vigneto 1 rilevato 3 giorni fa con foto, vigneto 2 20 giorni fa, vigneto 3 45 giorni fa; gli
/// altri mai rilevati.
/// </summary>
public sealed class InMemoryRilieviRepository : IRilieviRepository
{
    public Task<IReadOnlyList<UltimoRilievo>> GetUltimiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var oggi = DateTime.Today.AddHours(9);
        IReadOnlyList<UltimoRilievo> esempi =
        [
            new(1, oggi.AddDays(-3), "Fenologia", "M. Rossi", true),
            new(2, oggi.AddDays(-20), "Sanitario", "L. Bianchi", false),
            new(3, oggi.AddDays(-45), "Maturazione", null, false),
        ];
        var vigneti = InMemoryFourGrapesData.Vigneti.Where(v => v.AziendaId == aziendaId).Select(v => v.Id).ToHashSet();
        return Task.FromResult<IReadOnlyList<UltimoRilievo>>(esempi.Where(r => vigneti.Contains(r.IdVigneto)).ToList());
    }
}
