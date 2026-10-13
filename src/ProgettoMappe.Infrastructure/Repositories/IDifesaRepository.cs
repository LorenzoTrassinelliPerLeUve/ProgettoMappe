using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Ultimo trattamento e linea di difesa dei vigneti (task #68). <c>dbo.Trattamento</c> (70.000 righe) e
/// <c>TrattamentoAvversita</c> (200.000) non hanno indici per vigneto né per trattamento: si leggono
/// insieme, una volta per tutte le aziende del perimetro (dba, 2026-10-13), mai riga per riga.
/// </summary>
public interface IDifesaRepository
{
    /// <summary>
    /// Per i vigneti delle <paramref name="aziende"/> (null = tutte): l'ultimo giorno di trattamento
    /// fatto (i vigneti mai trattati non ci sono) e la linea di difesa (solo i vigneti che hanno linea
    /// o classe di rischio).
    /// </summary>
    Task<DifesaLetta> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default);
}
