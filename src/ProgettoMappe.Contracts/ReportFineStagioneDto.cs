namespace ProgettoMappe.Contracts;

/// <summary>
/// Meteo giornaliero per vigneto di un'azienda per il report fine stagione (task #201): dati grezzi, i calcoli
/// (annata agraria, fasi, medie di Qlik) li fa il Web in <c>ReportFineStagione/CalcoloMeteoReport</c>.
/// <c>StoricoDisponibile</c> = false: solo dal 2026, i confronti con gli anni passati restano vuoti.
/// </summary>
public sealed record MeteoReportFineStagioneDto(
    IReadOnlyList<VignetoMeteoReportDto> Vigneti,
    IReadOnlyList<GiornoMeteoReportDto> Giorni,
    bool StoricoDisponibile,
    DateTimeOffset? Snapshot = null);

/// <summary>Suolo e scottature dei vigneti dell'azienda (snapshot); <c>Superfici</c> = Vigneto.Superficie grezza.</summary>
public sealed record CampoReportFineStagioneDto(
    IReadOnlyDictionary<int, double?> Superfici,
    IReadOnlyList<SuoloVignetoDto> Suolo,
    IReadOnlyList<ScottaturaDto> ScottatureVista,
    IReadOnlyList<ScottaturaDto> ScottatureRilievi,
    IReadOnlyList<VigoriaDto> Vigoria,
    IReadOnlyList<VignetoFineStagioneDto> Vigneti,
    IReadOnlyList<ProduzioneDto> Produzione);

public sealed record VignetoFineStagioneDto(int IdVigneto, string Nome, string? Vigna, string? Vitigno, int? AnnoImpianto, double? Superficie);

/// <summary>Produzione per vigneto e anno in kg; <c>Stima</c> = da conta grappoli, altrimenti da vendemmia.</summary>
public sealed record ProduzioneDto(int IdVigneto, int Anno, bool Stima, double? RealeVigneto, double? RealeEttaro,
    double? PercFallanze, double? PercInAllevamento);

public sealed record VigoriaDto(int IdVigneto, int Anno, double? LegnoPotatura, double? SfePerMetro, double? StatoVegetativo,
    double? GestioneChioma, double? Punteggio);

public sealed record SuoloVignetoDto(int IdVigneto, string Nome, string? Vigna, double? CapacitaDiCampo, double? AcquaDisponibile,
    double? PuntoDiAppassimento, double? Profondita, double? Pietrosita, double? AwcRealeVigneto, double? AwcRealeVigna,
    double? Conducibilita);

public sealed record ScottaturaDto(int IdVigneto, int Anno, int? Organo, double? Intensita, double? Diffusione);

/// <summary>Un controllo del dba sui dati del report, con le sole righe dell'azienda (vuote = tutto a posto).</summary>
public sealed record ControlloReportDto(
    string Codice,
    string Titolo,
    string? Perche,
    string Gravita,
    int? Annata,
    IReadOnlyList<string> Colonne,
    IReadOnlyList<IReadOnlyList<string>> Righe);

public sealed record VignetoMeteoReportDto(int Id, string Nome, int? IdVigna, string? Vigna, bool DatiMeteoStorico);

public sealed record GiornoMeteoReportDto(
    int IdVigneto,
    DateOnly Data,
    bool Previsione,
    decimal? Precipitazione,
    decimal? TemperaturaMin,
    decimal? TemperaturaMed,
    decimal? TemperaturaMax);
