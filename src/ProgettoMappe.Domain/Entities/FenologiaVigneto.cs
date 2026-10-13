namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un rilievo di fenologia (vista 4Grapes <c>VistaRilievoBBCH</c>, rilievi di tipo 119): la fase
/// BBCH vista in un vigneto in un giorno. <see cref="Data"/> è il giorno (4Grapes ha anche l'ora).
/// </summary>
public sealed record RilievoBbch(int Anno, int IdVigneto, DateOnly Data, int Bbch);

/// <summary>Una fase BBCH vista in un vigneto, con il primo giorno dell'anno in cui è stata rilevata.</summary>
public sealed record FaseRilevata(int Bbch, string? Descrizione, DateOnly Data);

/// <summary>
/// Fenologia di un vigneto in un anno: la fase dell'ultimo rilievo (<see cref="Bbch"/>, rilevata il
/// giorno <see cref="Data"/>) e le altre fasi viste prima, dalla più vecchia
/// (vedi <c>FenologiaVigneti.Riassumi</c>). <see cref="Descrizione"/>: il nome da <c>dbo.BBCH</c>,
/// senza il numero davanti; null se il codice non c'è.
/// </summary>
public sealed record FenologiaVigneto(int Anno, int IdVigneto, int Bbch, string? Descrizione, DateOnly Data,
    IReadOnlyList<FaseRilevata> Precedenti);
