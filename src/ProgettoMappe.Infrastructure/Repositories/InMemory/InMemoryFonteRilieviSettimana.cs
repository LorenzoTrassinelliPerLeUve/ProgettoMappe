using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Rilievi di esempio (non reali) per il report settimanale: l'azienda 1 ha, in ogni periodo chiesto, un BBCH il secondo
/// giorno, una tignola il terzo e uno stato idrico il quinto; le altre aziende nessuno.
/// </summary>
public sealed class InMemoryFonteRilieviSettimana : IFonteRilieviSettimana
{
    private static readonly IReadOnlyDictionary<int, string> Nomi = new Dictionary<int, string>
    {
        [13] = "TIGNOLE",
        [146] = "VALUTAZIONE STATO IDRICO",
    };

    public Task<RilieviSettimanaLetti> LeggiAsync(int aziendaId, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        if (aziendaId != 1)
        {
            return Task.FromResult(new RilieviSettimanaLetti([], Nomi, "4grapes", null));
        }

        var inizio = da.ToDateTime(new TimeOnly(9, 0));
        List<RilievoSettimana> rilievi = [new(1, inizio.AddDays(1), 0, 65), new(1, inizio.AddDays(2), 13, null), new(2, inizio.AddDays(4), 146, null)];
        return Task.FromResult(new RilieviSettimanaLetti(
            rilievi.Where(r => DateOnly.FromDateTime(r.Data) <= a).ToList(), Nomi, "4grapes", null));
    }
}
