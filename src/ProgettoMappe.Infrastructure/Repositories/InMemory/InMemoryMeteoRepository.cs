using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Meteo di esempio (non reale), con date relative a oggi: l'azienda 1 ha sette giorni di pioggia e
/// temperature e la peronospora sopra soglia in un vigneto; l'azienda 2 non ha meteo («nessun dato meteo»).
/// </summary>
public sealed class InMemoryMeteoRepository : IMeteoRepository
{
    private static readonly decimal[] Pioggia = [0m, 12.4m, 3m, 0m, 0m, 21.6m, 5.2m];

    public Task<MeteoLetto> GetAsync(IReadOnlySet<int>? aziende, DateOnly oggi, int giorni, CancellationToken ct = default)
    {
        var giorniMeteo = Enumerable.Range(1, Math.Min(giorni, Pioggia.Length))
            .Select(i => new MeteoGiornoAzienda(1, oggi.AddDays(-i), Pioggia[i - 1], 8m + i % 3, 19m + i % 4))
            .ToList();
        List<InfezioneVigneto> infezioni = [new(1, 1, IMeteoRepository.Peronospora, 34m), new(1, 1, IMeteoRepository.Oidio, 12m)];
        List<AvversitaInfezione> avversita = [new(IMeteoRepository.Oidio, "Oidio", 30m), new(IMeteoRepository.Peronospora, "Peronospora", 30m)];

        return Task.FromResult(new MeteoLetto(oggi,
            giorniMeteo.Where(g => aziende is null || aziende.Contains(g.AziendaId)).ToList(),
            infezioni.Where(i => aziende is null || aziende.Contains(i.AziendaId)).ToList(),
            avversita));
    }
}
