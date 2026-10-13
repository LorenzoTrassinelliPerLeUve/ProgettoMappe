namespace ProgettoMappe.Contracts;

/// <summary>Riga dell'indice BIGOT di un vigneto in un anno (valori così come in 4Grapes).</summary>
public record IndiceBigotDto(
    int Anno,
    int IdVigneto,
    int? NumParametri,
    int? Punteggio,
    int? Peso,
    string? Vigoria,
    decimal? Sfe,
    decimal? Produzione,
    decimal? SfeKg,
    int? EtaVigneto,
    string? Morfologia,
    decimal? Ampelopatie,
    string? RegimeIdrico,
    decimal? Biodiversita);

/// <summary>
/// Indici BIGOT dei vigneti di un'azienda, tutti gli anni. <see cref="Pronto"/> false: i dati
/// sono ancora in caricamento lato server (riprovare più tardi), <see cref="Indici"/> è vuoto.
/// <see cref="Aggiornamento"/>: quando l'Api ha letto gli indici da 4Grapes l'ultima volta (UTC).
/// </summary>
public record IndiciBigotAziendaDto(bool Pronto, IReadOnlyList<IndiceBigotDto> Indici, DateTimeOffset? Aggiornamento = null);
