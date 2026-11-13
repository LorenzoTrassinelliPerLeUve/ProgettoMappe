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
    IReadOnlyList<ProduzioneDto> Produzione,
    IReadOnlyList<RilievoFineStagioneDto>? Rilievi = null,
    IReadOnlyList<CollezioneEscaDto>? Collezione = null,
    IReadOnlyList<ParametroIndiceFineStagioneDto>? Parametri = null,
    IReadOnlyDictionary<int, string>? GestioniChioma = null,
    IReadOnlyDictionary<int, string>? SottoVigneti = null);

public sealed record VignetoFineStagioneDto(int IdVigneto, string Nome, string? Vigna, string? Vitigno, int? AnnoImpianto, double? Superficie,
    double? NumeroPianteContate = null, double? SullaFila = null, double? TraFila = null);

/// <summary>Un rilievo di campo (tipi usati dai fogli, tappa 4): colonne grezze di <c>Rilievo</c>.</summary>
public sealed record RilievoFineStagioneDto(int IdVigneto, int? IdSubVigneto, DateTime Data, int Anno, int TipoRilievo, int? Organo,
    int? EtaEpoca, double? NrOrgani, double? PercDiffusione, double? PercIntensita, double? NrFormeMobiliNidi,
    double? CatturePerTrappola, double? SfePerVite, int? StatoIdrico, int? Bbch, double? AltezzaChioma, double? LarghezzaChioma,
    double? DistanzaPiante, double? SpaziVuoti, double? FormaAllevamento, int? GestioneChioma, int? IdUtente = null, string? Note = null);

/// <summary>Organi contati del mal dell'esca per vigneto, anno e valore (100 colpo apoplettico … 0 sintomi assenti).</summary>
public sealed record CollezioneEscaDto(int IdVigneto, int Anno, int Valore, double NumOrgani);

/// <summary><c>Avg</c> di un parametro dell'Indice Bigot (SFE, PRODUZIONE) per vigneto e anno.</summary>
public sealed record ParametroIndiceFineStagioneDto(int IdVigneto, int Anno, string Parametro, double? Valore);

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
