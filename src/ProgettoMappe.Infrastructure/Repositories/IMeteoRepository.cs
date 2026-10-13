using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Meteo e probabilità di infezione per azienda (task #67). Si legge sempre per tutte le aziende del
/// perimetro in una volta (la pagina iniziale le mostra tutte), poi l'Api lo tiene in memoria.
/// </summary>
public interface IMeteoRepository
{
    /// <summary>Id delle avversità del modello classico: oidio e peronospora (dba, 2026-10-13).</summary>
    public const int Oidio = 11;
    public const int Peronospora = 12;

    /// <summary>
    /// Meteo dei <paramref name="giorni"/> giorni prima di <paramref name="oggi"/> (oggi escluso: è
    /// ancora previsione) e probabilità di infezione di oggi, per le aziende indicate (null = tutte).
    /// </summary>
    Task<MeteoLetto> GetAsync(IReadOnlySet<int>? aziende, DateOnly oggi, int giorni, CancellationToken ct = default);
}
