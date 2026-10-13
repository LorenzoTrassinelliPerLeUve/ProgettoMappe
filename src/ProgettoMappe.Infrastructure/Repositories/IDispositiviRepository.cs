using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Stazioni meteo, dispositivi e trappole delle aziende (task #69). Poche righe nel perimetro: tutte le
/// aziende si leggono insieme (la pagina iniziale deve sapere quali hanno dispositivi fermi).
/// </summary>
public interface IDispositiviRepository
{
    /// <summary>Per le <paramref name="aziende"/> (null = tutte): stazioni, dispositivi e vigneti con trappole.</summary>
    Task<DispositiviLetti> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default);
}
