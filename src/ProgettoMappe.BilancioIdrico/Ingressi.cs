namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// Tutto quello che serve per calcolare il bilancio di un'azienda fra <see cref="Da"/> e <see cref="A"/> (giorni compresi).
/// Sono i dati grezzi (rilievi, meteo, configurazione): la scelta del valore di ogni giorno la fa il motore
/// (<see cref="ScelteDelGiorno"/>, REGOLE.md § 3). Chi li legge (4Grapes, database di Mappe, csv) non sta qui.
/// <para>
/// L'unità di calcolo è il <b>vigneto</b> (PIANO.md, principio 6b): si calcolano i vigneti di <see cref="Vigneti"/> più quelli
/// che stanno in un settore. I settori sono facoltativi: portano le irrigazioni registrate, l'impianto e il consiglio in ore.
/// </para>
/// </summary>
public sealed record IngressiAzienda
{
    public required int IdEnte { get; init; }

    /// <summary>Primo giorno dei risultati.</summary>
    public required DateOnly Da { get; init; }

    /// <summary>Ultimo giorno dei risultati (anche ultimo giorno dell'ETa futura).</summary>
    public required DateOnly A { get; init; }

    /// <summary>I vigneti da calcolare, con o senza settore.</summary>
    public IReadOnlyList<Vigneto> Vigneti { get; init; } = [];

    public IReadOnlyList<Settore> Settori { get; init; } = [];
    public IReadOnlyList<SettoreVigneto> SettoriVigneti { get; init; } = [];
    public IReadOnlyList<ConfigurazioneSettore> Configurazioni { get; init; } = [];
    public IReadOnlyList<ShiftSettore> ShiftSettori { get; init; } = [];
    public IReadOnlyList<StileVigneto> Stili { get; init; } = [];

    /// <summary>Meteo dell'azienda (uno per giorno): lo stesso per tutti i vigneti.</summary>
    public IReadOnlyList<MeteoGiorno> Meteo { get; init; } = [];

    public IReadOnlyList<FenologiaGiorno> Fenologia { get; init; } = [];
    public IReadOnlyList<AnalisiSuolo> AnalisiSuolo { get; init; } = [];
    public IReadOnlyList<RilievoRadici> Radici { get; init; } = [];
    public IReadOnlyList<RilievoGestioneSuolo> GestioniSuolo { get; init; } = [];
    public IReadOnlyList<EventoIrrigazione> Irrigazioni { get; init; } = [];

    /// <summary>
    /// Irrigazioni già in mm sul vigneto (registrate sul vigneto, oppure già ripartite come
    /// BI_InputIrrigazioneSettoreVignetoGiornaliera): si sommano a quelle che vengono da <see cref="Irrigazioni"/>.
    /// </summary>
    public IReadOnlyList<IrrigazioneVigneto> IrrigazioniVigneto { get; init; } = [];
}

/// <summary>
/// Un vigneto. <c>DistanzaFile</c> = Vigneto.TraFila così com'è (unità in <see cref="Parametri.DistanzaFileScala"/>);
/// <c>SuperficieM2</c> pesa il vigneto nei totali del settore; <c>AwcManualeMm</c> = correzione a mano dell'AWC
/// (calibrazione, domanda 23), al posto di quella dell'analisi.
/// <para>
/// Calibrazione (Matteo, s8): <c>ScheletroManualePerc</c> al posto dello scheletro delle analisi, <c>ProfonditaRadiciManualeM</c>
/// e <c>LarghezzaRadiciManualeM</c> al posto di quelle del rilievo 145 (ciascuna da sola). <c>IdTargetConcimazione</c> = target
/// di concimazione del vigneto in 4Grapes (ripiego dello stile, <see cref="Parametri.StileDaTarget"/>).
/// </para>
/// </summary>
public sealed record Vigneto(int IdVigneto, double? DistanzaFile = null, double? SuperficieM2 = null, double? AwcManualeMm = null,
    double? ScheletroManualePerc = null, double? ProfonditaRadiciManualeM = null, double? LarghezzaRadiciManualeM = null,
    int? IdTargetConcimazione = null);

/// <summary>Un settore irriguo.</summary>
public sealed record Settore(int IdSettore);

/// <summary>
/// Un vigneto in un settore. <c>Percentuale</c> = parte del <b>vigneto</b> che sta nel settore, 0–100 (somma ≤ 100 per
/// vigneto, REGOLE.md § 9). Nel vecchio DSS era la parte del settore occupata dal vigneto: vedi
/// <see cref="Parametri.QuoteDelSettore"/>.
/// </summary>
public sealed record SettoreVigneto(int IdSettore, int IdVigneto, double Percentuale);

/// <summary>
/// Configurazione dell'impianto di un settore; vale quella con <see cref="ValidoDal"/> più recente (§ 3.5).
/// Impianto 0 = nessun impianto (asciutta).
/// </summary>
public sealed record ConfigurazioneSettore(int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl, int? IdTipologiaImpianto,
    double? TassoMmOra, double? EfficienzaReale);

public sealed record ShiftSettore(int IdShift, int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl);

/// <summary>Stile del vino del vigneto (BI_VignetoStileConfig): vale quello con <see cref="ValidoDal"/> più recente.</summary>
public sealed record StileVigneto(int IdVigneto, int IdStileVino, DateOnly ValidoDal, DateOnly? ValidoAl);

/// <summary>
/// Una riga di meteo aziendale. ET0 o pioggia vuoti con la riga presente: vedi <see cref="Parametri.MeteoVuotoComeZero"/>.
/// <c>Previsione</c>: giorno previsto, non osservato; entra nell'ETa futura come gli altri giorni.
/// </summary>
public sealed record MeteoGiorno(DateOnly Data, double? Et0Mm, double? PioggiaMm, bool Previsione = false);

public enum FonteFenologia
{
    /// <summary>Rilievo (ModelloBBCH): fra il primo e l'ultimo si riporta l'ultimo valore.</summary>
    Storico,

    /// <summary>Previsione IA: vale solo nel suo giorno, dopo l'ultimo storico.</summary>
    Previsione,

    /// <summary>
    /// BBCH già scelto per il giorno (come BI_InputFenologiaGiornaliera o le righe dei run vecchi): vale solo nel suo giorno,
    /// senza riporto, e prima di tutto il resto.
    /// </summary>
    Risolta,
}

public sealed record FenologiaGiorno(int IdVigneto, DateOnly Data, int Bbch, FonteFenologia Fonte = FonteFenologia.Storico);

/// <summary>
/// Un'analisi del suolo (rilievo 144). <see cref="ProfonditaSuolo"/> e <see cref="Pietrosita"/> sono i «Punteggi» delle
/// tabelle di decodifica: come si leggono lo dicono <see cref="Parametri.ProfonditaSuoloScala"/> e
/// <see cref="Parametri.PietrositaScala"/>.
/// </summary>
public sealed record AnalisiSuolo(int IdVigneto, DateOnly Data, double? AcquaDisponibileMmM, double? ProfonditaSuolo = null,
    double? Pietrosita = null, int? IdTessitura = null, double? ScheletroPerc = null);

/// <summary>Rilievo 145: larghezza e profondità della zona delle radici, in metri.</summary>
public sealed record RilievoRadici(int IdVigneto, DateOnly Data, double? LarghezzaM, double? ProfonditaM);

/// <summary>Rilievo 140: gestione del suolo sotto le file (1 inerbito, 2 lavorato, 3 file alterne).</summary>
public sealed record RilievoGestioneSuolo(int IdVigneto, DateOnly Data, int IdGestioneSuolo);

/// <summary>Un evento di irrigazione, su un settore oppure su uno shift (§ 3.4).</summary>
public sealed record EventoIrrigazione(DateOnly Data, int? IdSettore, int? IdShift, double? DurataOre = null, double? VolumeMm = null,
    double? Efficienza = null, double? VolumeUtileMm = null);

/// <summary>mm d'irrigazione sul vigneto in un giorno; <c>IdSettore</c> = da quale settore arriva (null = registrata sul vigneto).</summary>
public sealed record IrrigazioneVigneto(int? IdSettore, int IdVigneto, DateOnly Data, double Mm);
