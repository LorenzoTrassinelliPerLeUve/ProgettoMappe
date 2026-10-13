using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Trattamenti e linee di esempio (non reali), con date relative a oggi così che si vedano le classi:
/// vigneto 1 trattato 3 giorni fa con rame contro la peronospora (linea BIO 4 KG), vigneto 2 20 giorni
/// fa (linea SOFT, senza rischio); gli altri mai trattati e senza linea.
/// </summary>
public sealed class InMemoryDifesaRepository : IDifesaRepository
{
    public Task<DifesaLetta> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
    {
        var oggi = DateTime.Today.AddHours(9);
        var vigneti = InMemoryFourGrapesData.Vigneti
            .Where(v => aziende is null || aziende.Contains(v.AziendaId))
            .ToDictionary(v => v.Id, v => v.AziendaId);

        List<UltimoTrattamento> trattamenti = [];
        List<LineaDifesaVigneto> linee = [];
        if (vigneti.TryGetValue(1, out var azienda1))
        {
            trattamenti.Add(new(azienda1, 1, oggi.AddDays(-3), ["RAME (IDROSSIDO)"], ["CYPRUS 25 WG BLU"], ["Peronospora"]));
            linee.Add(new(azienda1, 1, 4, "BIO 4 KG", "ALTO / BASSO"));
        }
        if (vigneti.TryGetValue(2, out var azienda2))
        {
            trattamenti.Add(new(azienda2, 2, oggi.AddDays(-20), ["ZOLFO", "CIFLUFENAMID"], ["THIOVIT JET", "CYMBAL"], ["Oidio"]));
            linee.Add(new(azienda2, 2, 3, "SOFT", null));
        }

        return Task.FromResult(new DifesaLetta(trattamenti, linee));
    }
}
