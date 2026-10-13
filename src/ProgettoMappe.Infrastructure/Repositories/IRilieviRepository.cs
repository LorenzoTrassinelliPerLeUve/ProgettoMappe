using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Ultimi rilievi in campo dei vigneti (task #45). <c>dbo.Rilievo</c> ha 1,19 milioni di righe: si
/// legge sempre per azienda, mai tutta la tabella (niente copia in memoria come per le viste).
/// </summary>
public interface IRilieviRepository
{
    /// <summary>L'ultimo rilievo di ciascun vigneto dell'azienda; i vigneti mai rilevati non ci sono.</summary>
    Task<IReadOnlyList<UltimoRilievo>> GetUltimiPerAziendaAsync(int aziendaId, CancellationToken ct = default);
}
