using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Accesso in sola lettura ai vigneti. Vedi <see cref="IAziendeRepository"/> per il motivo
/// dell'astrazione rispetto a 4Grapes.
/// </summary>
public interface IVignetiRepository
{
    Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default);

    Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Tutti i vigneti delle aziende indicate (null = di tutte), senza geometria: una lettura sola
    /// per i riepiloghi della pagina iniziale. Stessa superficie e stesse esclusioni dell'elenco
    /// per azienda.
    /// </summary>
    Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default);
}
