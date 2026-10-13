using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Fenologia dei vigneti (rilievi BBCH di 4Grapes): per vigneto e anno l'ultima fase rilevata, con
/// la sua data e le fasi precedenti. Stesso schema degli indici BIGOT: una copia in memoria, con ora
/// di aggiornamento e ricarico a comando.
/// </summary>
public interface IFenologiaRepository
{
    /// <summary>Fenologia dei vigneti indicati, tutti gli anni; null se i dati non sono ancora pronti.</summary>
    Task<IReadOnlyList<FenologiaVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default);

    StatoIndiciBigot Stato { get; }

    /// <summary>Rilegge i dati in background e torna subito; false se una lettura è già in corso.</summary>
    bool AvviaRicarico(CancellationToken stop);
}
