namespace ProgettoMappe.Contracts;

/// <summary>
/// Ultimo rilievo in campo di un vigneto, di qualunque tipo (4Grapes). <see cref="Data"/> null: il
/// vigneto non è mai stato rilevato (tipo, autore e foto allora non ci sono). <see cref="Autore"/>:
/// «M. Broggi».
/// </summary>
public record UltimoRilievoDto(int IdVigneto, DateTime? Data, string? Tipo, string? Autore, bool ConFoto) : IRigaVigneto;

/// <summary>
/// Ultimo rilievo di ogni vigneto di un'azienda (una riga per vigneto, anche se mai rilevato).
/// <see cref="Pronto"/> false: dati non disponibili, lista vuota. <see cref="Aggiornamento"/>:
/// quando l'Api li ha letti da 4Grapes (UTC; restano in memoria pochi minuti).
/// </summary>
public record UltimiRilieviAziendaDto(bool Pronto, IReadOnlyList<UltimoRilievoDto> Rilievi, DateTimeOffset? Aggiornamento = null)
{
    public static readonly UltimiRilieviAziendaDto NonPronti = new(false, []);
}
