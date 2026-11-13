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
    bool StoricoDisponibile,
    DateTimeOffset? Snapshot = null);

/// <summary>Suolo e riserva idrica di un vigneto (<c>VistaStatistichePotenzialeIdricoVigneti</c>, foglio 42 di Qlik).</summary>
public sealed record SuoloVigneto(int IdVigneto, string Nome, string? Vigna, double? CapacitaDiCampo, double? AcquaDisponibile,
    double? PuntoDiAppassimento, double? Profondita, double? Pietrosita, double? AwcRealeVigneto, double? AwcRealeVigna,
    double? Conducibilita);

/// <summary>Una riga di scottature: da <c>VistaScottature</c> (fonte «vista») o da un rilievo di tipo 32 (organo nel campo).</summary>
public sealed record ScottaturaVigneto(int IdVigneto, int Anno, int? Organo, double? Intensita, double? Diffusione);

/// <summary>Una riga di <c>VistaVigoria</c> (fogli 45–46): punteggi 0–10 per vigneto e anno.</summary>
public sealed record VigoriaReport(int IdVigneto, int Anno, double? LegnoPotatura, double? SfePerMetro, double? StatoVegetativo,
    double? GestioneChioma, double? Punteggio);

/// <summary>Anagrafica di un vigneto del report: vigna, vitigno, anno d'impianto e superficie grezza (m²).</summary>
public sealed record VignetoReport(int IdVigneto, string Nome, string? Vigna, string? Vitigno, int? AnnoImpianto, double? Superficie,
    double? NumeroPianteContate = null, double? SullaFila = null, double? TraFila = null, double? Esposizione = null,
    double? Orientamento = null);

/// <summary>
/// Un rilievo di campo del report (tabella <c>Rilievo</c> dello snapshot, tappa 4): solo i tipi che i fogli usano
/// (stato idrico, chioma, esca, sanità, tignole) e quelli con un BBCH. Colonne grezze, i calcoli li fa il Web.
/// </summary>
public sealed record RilievoReport(int IdVigneto, int? IdSubVigneto, DateTime Data, int Anno, int TipoRilievo, int? Organo,
    int? EtaEpoca, double? NrOrgani, double? PercDiffusione, double? PercIntensita, double? NrFormeMobiliNidi,
    double? CatturePerTrappola, double? SfePerVite, int? StatoIdrico, int? Bbch, double? AltezzaChioma, double? LarghezzaChioma,
    double? DistanzaPiante, double? SpaziVuoti, double? FormaAllevamento, int? GestioneChioma, int? IdUtente, string? Note,
    double? PotenzialeIdrico = null);

/// <summary>Organi contati del mal dell'esca (tipo 9) per vigneto, anno e valore, già sommati dal dba.</summary>
public sealed record CollezioneReport(int IdVigneto, int Anno, int Valore, double NumOrgani);

/// <summary>Il valore <c>Avg</c> di un parametro dell'Indice Bigot (<c>VistaIndicePerParametro</c>) per vigneto e anno.</summary>
public sealed record ParametroIndiceReport(int IdVigneto, int Anno, string Parametro, double? Valore);

/// <summary>
/// Produzione per vigneto e anno (kg): da <c>VistaProduzioneVendemmia</c> (reale, <see cref="Stima"/> falso) o da
/// <c>VistaProduzioneGrappoli</c> (stima dalla conta dei grappoli).
/// </summary>
public sealed record ProduzioneReport(int IdVigneto, int Anno, bool Stima, double? RealeVigneto, double? RealeEttaro,
    double? PercFallanze, double? PercInAllevamento);

/// <summary>Dati di campo del report fine stagione dallo snapshot (tappa 3): suolo, scottature, vigoria e produzione.</summary>
public sealed record CampoReportLetto(
    IReadOnlyDictionary<int, double?> Superfici,
    IReadOnlyList<SuoloVigneto> Suolo,
    IReadOnlyList<ScottaturaVigneto> ScottatureVista,
    IReadOnlyList<ScottaturaVigneto> ScottatureRilievi,
    IReadOnlyList<VigoriaReport> Vigoria,
    IReadOnlyList<VignetoReport> Vigneti,
    IReadOnlyList<ProduzioneReport> Produzione,
    IReadOnlyList<RilievoReport>? Rilievi = null,
    IReadOnlyList<CollezioneReport>? Collezione = null,
    IReadOnlyList<ParametroIndiceReport>? Parametri = null,
    IReadOnlyDictionary<int, string>? GestioniChioma = null,
    IReadOnlyDictionary<int, string>? SottoVigneti = null,
    SanitaReportLetta? Sanita = null);

/// <summary>Rilievi di un tipo in un anno (cruscotto, foglio 26): rilievi distinti e organi controllati.</summary>
public sealed record ConteggioRilievi(int Anno, int TipoRilievo, int Rilievi, double Organi, int? IdVigneto = null);

/// <summary>Giorno dell'anno medio di una fase fenologica per vigneto e anno (1 germogliamento, 2 fioritura, 3 invaiatura, 4 vendemmia).</summary>
public sealed record FaseFenologica(int IdVigneto, int Anno, int Fase, double Giorno);

/// <summary>Il resto dei dati della sezione sanità (fogli 26–36): conteggi, nomi dei tipi, vigneti monitorati, fasi.</summary>
public sealed record SanitaReportLetta(
    IReadOnlyList<ConteggioRilievi> Conteggi,
    IReadOnlyDictionary<int, string> NomiTipi,
    IReadOnlyDictionary<int, int> VignetiMonitorati,
    IReadOnlyList<FaseFenologica> Fasi);

/// <summary>
/// Un controllo sui dati del dba (tabella <c>_controlli</c> dello snapshot, <c>controlli.md</c>): solo le righe
/// del dettaglio dell'azienda (colonna «Azienda» = ragione sociale), oppure tutte se il controllo non ha quella colonna.
/// </summary>
public sealed record ControlloReport(
    string Codice,
    string Titolo,
    string? Perche,
    string Gravita,
    int? Annata,
    IReadOnlyList<string> Colonne,
    IReadOnlyList<IReadOnlyList<string>> Righe);
