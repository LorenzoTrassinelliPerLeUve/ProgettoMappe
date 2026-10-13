using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Stato della copia degli indici: <paramref name="Pronta"/> se c'è una copia in uso,
/// <paramref name="InCaricamento"/> mentre si rilegge, ore UTC dell'ultima lettura riuscita e
/// dell'ultima fallita (null se l'ultima è riuscita).
/// </summary>
public sealed record StatoIndiciBigot(bool Pronta, bool InCaricamento, DateTimeOffset? UltimoAggiornamento, DateTimeOffset? UltimoErrore);

/// <summary>Accesso in sola lettura agli indici BIGOT dei vigneti (tutti gli anni disponibili).</summary>
public interface IIndiciBigotRepository
{
    /// <summary>
    /// Indici dei vigneti indicati, tutti gli anni. Null se i dati non sono ancora pronti (es. la
    /// cache della vista 4Grapes è in caricamento): il chiamante non deve restare in attesa.
    /// </summary>
    Task<IReadOnlyList<IndiceBigotVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default);

    /// <summary>Quando i dati sono stati letti l'ultima volta da 4Grapes (null se non ancora).</summary>
    DateTimeOffset? UltimoAggiornamento { get; }

    StatoIndiciBigot Stato { get; }

    /// <summary>
    /// Rilegge i dati da 4Grapes in background e torna subito. False se una lettura è già in
    /// corso. <paramref name="stop"/>: arresto dell'applicazione (non la richiesta HTTP).
    /// </summary>
    bool AvviaRicarico(CancellationToken stop);
}
