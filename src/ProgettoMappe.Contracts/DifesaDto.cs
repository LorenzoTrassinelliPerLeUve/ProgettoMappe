namespace ProgettoMappe.Contracts;

/// <summary>
/// Difesa di un vigneto (4Grapes, task #68): l'ultimo giorno di trattamento fatto (<see cref="Data"/>
/// null = nessun trattamento registrato; principi attivi, prodotti e avversità scritti come in 4Grapes)
/// e la linea di difesa assegnata (<see cref="IdLinea"/> null = nessuna) con la classe di rischio
/// («ALTO / BASSO» = peronospora alto, oidio basso).
/// </summary>
public record DifesaVignetoDto(int IdVigneto, DateTime? Data, IReadOnlyList<string> PrincipiAttivi, IReadOnlyList<string> Prodotti,
    IReadOnlyList<string> Avversita, int? IdLinea, string? Linea, string? Rischio) : IRigaVigneto;

/// <summary>
/// Difesa di ogni vigneto di un'azienda (una riga per vigneto, anche senza trattamenti né linea).
/// <see cref="Pronto"/> false: dati non disponibili, lista vuota. <see cref="Aggiornamento"/>: quando
/// l'Api li ha letti da 4Grapes (UTC; restano in memoria 10 minuti).
/// </summary>
public record DifesaAziendaDto(bool Pronto, IReadOnlyList<DifesaVignetoDto> Vigneti, DateTimeOffset? Aggiornamento = null)
{
    public static readonly DifesaAziendaDto NonPronta = new(false, []);
}
