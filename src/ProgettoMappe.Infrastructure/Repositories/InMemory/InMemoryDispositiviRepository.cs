using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Dispositivi di esempio (non reali), con date relative a oggi così che si vedano i colori: una
/// stazione con il dato di ieri vicino al vigneto 1, una ferma da 15 giorni sul vigneto 2 (senza
/// posizione propria), un dispositivo sul vigneto 1, trappole controllate 5 giorni fa sul vigneto 2.
/// </summary>
public sealed class InMemoryDispositiviRepository : IDispositiviRepository
{
    public Task<DispositiviLetti> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
    {
        var oggi = DateTime.Today;
        var vigneti = InMemoryFourGrapesData.Vigneti
            .Where(v => aziende is null || aziende.Contains(v.AziendaId))
            .ToDictionary(v => v.Id, v => v.AziendaId);

        List<StazioneMeteo> stazioni = [];
        List<SensoreVigneto> sensori = [];
        List<TrappoleVigneto> trappole = [];
        if (vigneti.TryGetValue(1, out var azienda1))
        {
            stazioni.Add(new(azienda1, 1, 7, "Stazione Poggio (dato di prova)", 43.4710, 11.0425, oggi.AddDays(-1)));
            sensori.Add(new(azienda1, 1, "PROVA-001", "Umidità suolo", "Correctly updated", oggi.AddYears(-1)));
        }
        if (vigneti.TryGetValue(2, out var azienda2))
        {
            stazioni.Add(new(azienda2, 2, 8, "Stazione Colle (dato di prova)", null, null, oggi.AddDays(-15)));
            trappole.Add(new(azienda2, 2, 3, oggi.AddDays(-5).AddHours(8)));
        }

        return Task.FromResult(new DispositiviLetti(stazioni, sensori, trappole));
    }
}
