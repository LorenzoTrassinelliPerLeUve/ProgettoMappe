using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Accesso in sola lettura agli indici BIGOT dei vigneti (tutti gli anni disponibili).</summary>
public interface IIndiciBigotRepository
{
    /// <summary>
    /// Indici dei vigneti indicati, tutti gli anni. Null se i dati non sono ancora pronti (es. la
    /// cache della vista 4Grapes è in caricamento): il chiamante non deve restare in attesa.
    /// </summary>
    Task<IReadOnlyList<IndiceBigotVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default);
}
