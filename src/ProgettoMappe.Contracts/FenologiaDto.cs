namespace ProgettoMappe.Contracts;

/// <summary>Una fase BBCH vista nell'anno, con il primo giorno in cui è stata rilevata.</summary>
public record FaseRilevataDto(int Bbch, string? Descrizione, DateOnly Data);

/// <summary>
/// Fenologia di un vigneto in un anno (rilievi BBCH di 4Grapes): la fase dell'ultimo rilievo, il
/// giorno di quel rilievo e le altre fasi viste prima, dalla più vecchia. <see cref="Descrizione"/>:
/// nome della fase senza il numero (null se 4Grapes non lo ha).
/// </summary>
public record FenologiaVignetoDto(int Anno, int IdVigneto, int Bbch, string? Descrizione, DateOnly Data,
    IReadOnlyList<FaseRilevataDto> Precedenti) : IRigaAnnuale;

/// <summary>
/// Fenologia dei vigneti di un'azienda, tutti gli anni. <see cref="Pronto"/> false: dati ancora in
/// caricamento lato server, lista vuota. <see cref="Aggiornamento"/>: ultima lettura da 4Grapes (UTC).
/// </summary>
public record FenologiaAziendaDto(bool Pronto, IReadOnlyList<FenologiaVignetoDto> Fasi, DateTimeOffset? Aggiornamento = null)
{
    public static readonly FenologiaAziendaDto NonPronta = new(false, []);
}
