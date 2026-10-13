namespace ProgettoMappe.Contracts;

/// <summary>Che cosa è un segnaposto dei dispositivi: una stazione meteo, un dispositivo con sensore, le trappole di un vigneto.</summary>
public enum TipoDispositivo
{
    Stazione,
    Sensore,
    Trappole,
}

/// <summary>
/// Colore del segnaposto (decisione del proprietario, 2026-10-13): ultimo dato di oggi o di ieri =
/// <see cref="Attivo"/> (verde), fino a 10 giorni = <see cref="InRitardo"/> (giallo), oltre =
/// <see cref="Fermo"/> (rosso, «ferma»), nessun dato = <see cref="Mai"/> (grigio).
/// <see cref="NonDisponibile"/> (grigio): 4Grapes non registra quando il dispositivo manda dati.
/// </summary>
public enum StatoDispositivo
{
    Attivo,
    InRitardo,
    Fermo,
    Mai,
    NonDisponibile,
}

/// <summary>
/// Un segnaposto dei dispositivi di un'azienda (4Grapes, task #69). <see cref="Longitudine"/> e
/// <see cref="Latitudine"/> (WGS84): la posizione della stazione, altrimenti il centro del vigneto; null =
/// nessuna posizione (non va sulla mappa). <see cref="UltimoDato"/>: per le stazioni l'ultimo giorno di
/// meteo misurato, per le trappole l'ultimo rilievo di monitoraggio; <see cref="Giorni"/>: quanti giorni
/// fa (ora italiana). <see cref="Trappole"/>: quante sono (solo trappole); <see cref="Sensore"/>,
/// <see cref="StatoFourGrapes"/> e <see cref="Registrato"/>: solo dispositivi.
/// </summary>
public record DispositivoDto(
    string Id,
    TipoDispositivo Tipo,
    string Nome,
    int IdVigneto,
    double? Longitudine,
    double? Latitudine,
    DateTime? UltimoDato,
    int? Giorni,
    StatoDispositivo Stato,
    int? Trappole = null,
    string? Sensore = null,
    string? StatoFourGrapes = null,
    DateTime? Registrato = null);

/// <summary>
/// Stazioni, dispositivi e trappole di un'azienda. <see cref="Pronti"/> false: non disponibili, lista
/// vuota. <see cref="Aggiornamento"/>: quando l'Api li ha letti da 4Grapes (UTC; in memoria 10 minuti).
/// </summary>
public record DispositiviAziendaDto(bool Pronti, IReadOnlyList<DispositivoDto> Dispositivi, DateTimeOffset? Aggiornamento = null)
{
    public static readonly DispositiviAziendaDto NonPronti = new(false, []);
}

/// <summary>Per la pagina iniziale: quanti segnaposti ha ogni azienda e quanti sono fermi (solo le aziende che ne hanno).</summary>
public record DispositiviAziendeDto(IReadOnlyList<RiepilogoDispositiviDto> Aziende, DateTimeOffset? Aggiornamento = null);

public record RiepilogoDispositiviDto(int AziendaId, int Dispositivi, int Fermi);
