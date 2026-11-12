namespace ProgettoMappe.Contracts;

/// <summary>
/// Meteo giornaliero per vigneto di un'azienda per il report fine stagione (task #201): dati grezzi, i calcoli
/// (annata agraria, fasi, medie di Qlik) li fa il Web in <c>ReportFineStagione/CalcoloMeteoReport</c>.
/// <c>StoricoDisponibile</c> = false: solo dal 2026, i confronti con gli anni passati restano vuoti.
/// </summary>
public sealed record MeteoReportFineStagioneDto(
    IReadOnlyList<VignetoMeteoReportDto> Vigneti,
    IReadOnlyList<GiornoMeteoReportDto> Giorni,
    bool StoricoDisponibile);

public sealed record VignetoMeteoReportDto(int Id, string Nome, int? IdVigna, string? Vigna, bool DatiMeteoStorico);

public sealed record GiornoMeteoReportDto(
    int IdVigneto,
    DateOnly Data,
    bool Previsione,
    decimal? Precipitazione,
    decimal? TemperaturaMin,
    decimal? TemperaturaMed,
    decimal? TemperaturaMax);
