namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Meteo di un'azienda in un giorno (<c>dbo.VistaMeteoGiornalieroAzienda</c> di 4Grapes): media dei
/// vigneti dell'azienda che hanno il meteo (i «punti meteo»). Pioggia in mm, temperature in °C;
/// null se 4Grapes non ha il valore.
/// </summary>
public sealed record MeteoGiornoAzienda(int AziendaId, DateOnly Data, decimal? Pioggia, decimal? TemperaturaMin, decimal? TemperaturaMax);

/// <summary>
/// Probabilità di infezione di un vigneto in un giorno (<c>dbo.ModelloInfezione</c>, modello classico,
/// colonna PercInfezione: percentuale 0–100) per un'avversità (11 = oidio, 12 = peronospora).
/// </summary>
public sealed record InfezioneVigneto(int AziendaId, int IdVigneto, int IdAvversita, decimal Percentuale);

/// <summary>
/// Un'avversità del modello di infezione (<c>dbo.AvversitaDaTrattare</c>): nome e soglia di
/// probabilità oltre la quale 4Grapes la considera da trattare (null se non c'è).
/// </summary>
public sealed record AvversitaInfezione(int Id, string Nome, decimal? Soglia);

/// <summary>
/// Quello che serve per il meteo delle aziende, letto da 4Grapes in una volta: giorni passati
/// (<see cref="Giorni"/>), probabilità di infezione di <see cref="Oggi"/> per vigneto e le avversità.
/// </summary>
public sealed record MeteoLetto(
    DateOnly Oggi,
    IReadOnlyList<MeteoGiornoAzienda> Giorni,
    IReadOnlyList<InfezioneVigneto> Infezioni,
    IReadOnlyList<AvversitaInfezione> Avversita);
