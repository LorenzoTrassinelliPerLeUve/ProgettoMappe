namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un vigneto attivo dell'azienda per il report fine stagione (task #201): come la tabella Vigneto dello
/// script Qlik (Estirpo = 0, Carico = 1). <see cref="DatiMeteoStorico"/> è il flag di 4Grapes che tutte le
/// misure meteo di Qlik usano come filtro (dba, 2026-11-12: significato non documentato e poco affidabile,
/// ma per la parità si usa com'è).
/// </summary>
public sealed record VignetoMeteoReport(int IdVigneto, string Nome, int? IdVigna, string? Vigna, bool DatiMeteoStorico);

/// <summary>Un giorno di <c>MeteoGiornaliero</c> (o dello storico) per un vigneto, valori grezzi di 4Grapes.</summary>
public sealed record GiornoMeteoReport(
    int IdVigneto,
    DateOnly Data,
    bool Previsione,
    decimal? Precipitazione,
    decimal? TemperaturaMin,
    decimal? TemperaturaMed,
    decimal? TemperaturaMax);

/// <summary>
/// Il meteo letto per un'azienda. <see cref="StoricoDisponibile"/> è falso finché Mappe non legge
/// <c>4g_Storico.dbo.MeteoGiornaliero_Storico</c> (1984–2025): senza, i confronti con gli ultimi 10 e 30 anni
/// restano vuoti.
/// </summary>
public sealed record MeteoReportLetto(
    IReadOnlyList<VignetoMeteoReport> Vigneti,
    IReadOnlyList<GiornoMeteoReport> Giorni,
    bool StoricoDisponibile);
