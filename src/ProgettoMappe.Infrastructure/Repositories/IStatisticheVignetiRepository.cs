using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Accesso in sola lettura alle statistiche dei vigneti di 4Grapes: rischio oidio/peronospora,
/// vigoria (per anno) e potenziale idrico (senza anno). Stesso schema degli indici BIGOT: una copia
/// in memoria, con ora di aggiornamento e ricarico a comando (lo stato ha la stessa forma).
/// </summary>
public interface IStatisticheVignetiRepository
{
    /// <summary>Statistiche dei vigneti indicati, tutti gli anni; null se i dati non sono ancora pronti.</summary>
    Task<StatisticheVigneti?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default);

    StatoIndiciBigot Stato { get; }

    /// <summary>Rilegge i dati in background e torna subito; false se una lettura è già in corso.</summary>
    bool AvviaRicarico(CancellationToken stop);
}
