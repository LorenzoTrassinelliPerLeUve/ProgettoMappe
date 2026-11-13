namespace ProgettoMappe.Contracts;

/// <summary>
/// I rilievi di un periodo per il report settimanale (task #213/#214): dati grezzi, settimane e calcoli li fa il Web in
/// <c>ReportSettimanale/</c>. <c>Fonte</c> = «snapshot» o «4grapes» (solo tipo, data e BBCH, tipo 0 = fase da
/// <c>VistaRilievoBBCH</c>). Danni, esca, meteo e riepilogo vengono dalle tabelle RS_ del dba (vuoti senza).
/// </summary>
public sealed record RilieviSettimanaDto(
    IReadOnlyList<RilievoSettimanaDto> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot)
{
    public IReadOnlyList<DannoSettimanaDto> Danni { get; init; } = [];
    public IReadOnlyList<EscaSettimanaDto> Esca { get; init; } = [];
    public IReadOnlyList<MeteoSettimanaDto> Meteo { get; init; } = [];
    public IReadOnlyList<RiepilogoSettimanaDto> Riepilogo { get; init; } = [];

    /// <summary>Giorno medio (dell'anno di Qlik) per vigneto, anno e codice BBCH del modello; 89 = vendemmia.</summary>
    public IReadOnlyList<GiornoBbchDto> GiorniBbch { get; init; } = [];
}

public sealed record GiornoBbchDto(int IdVigneto, int Anno, int Bbch, double Giorno);

/// <summary>
/// Un rilievo con le misure delle slide. <c>IdRilievo</c> = chiave di 4Grapes (testo), <c>Settimana</c> = <c>Week</c> di
/// Qlik già calcolata dal dba (null = da calcolare).
/// </summary>
public sealed record RilievoSettimanaDto(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch,
    int? Organo = null, int? EtaEpoca = null, double? NrOrgani = null, double? PercDiffusione = null, double? PercIntensita = null,
    double? NrFormeMobiliNidi = null, double? CatturePerTrappola = null, int? StatoIdrico = null,
    int? PresenzaAvversita = null, double? LunghezzaGermogli = null, string? IdRilievo = null, int? Settimana = null);

/// <summary>Danno da ampelopatie per vigneto e anno (0–100).</summary>
public sealed record DannoSettimanaDto(int IdVigneto, int Anno, double PercDanno);

/// <summary>Una riga della collezione del mal dell'esca: rilievo e valore contato (0, 10, 30, 40, 50, 60, 100).</summary>
public sealed record EscaSettimanaDto(string IdRilievo, int Valore);

/// <summary>Meteo di un vigneto per settimana di Qlik (pioggia mm, temperature °C).</summary>
public sealed record MeteoSettimanaDto(int IdVigneto, int Anno, int Settimana, int GiorniConDati, int GiorniPrevisione,
    double? Pioggia, double? TMedia, double? TMin, double? TMax);

/// <summary>Controlli del dba per azienda e settimana.</summary>
public sealed record RiepilogoSettimanaDto(int Anno, int Settimana, int VignetiAttivi, int VignetiRilevati, int Rilievi,
    int PuntiMeteo, int PuntiMeteoCompleti, int GiorniMeteoMancanti, int GiorniMeteoPrevisione);
