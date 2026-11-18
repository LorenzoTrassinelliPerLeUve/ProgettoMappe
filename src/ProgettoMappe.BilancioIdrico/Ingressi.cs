namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// Tutto quello che serve per calcolare il bilancio di un'azienda fra <see cref="Da"/> e <see cref="A"/> (giorni compresi).
/// Sono i dati grezzi (rilievi, meteo, configurazione): la scelta del valore di ogni giorno la fa il motore
/// (<see cref="ScelteDelGiorno"/>, REGOLE.md § 3). Chi li legge (4Grapes, database di Mappe, csv) non sta qui.
/// </summary>
public sealed record IngressiAzienda
{
    public required int IdEnte { get; init; }

    /// <summary>Primo giorno dei risultati.</summary>
    public required DateOnly Da { get; init; }

    /// <summary>Ultimo giorno dei risultati (anche ultimo giorno dell'ETa futura).</summary>
    public required DateOnly A { get; init; }

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
    public IReadOnlyList<VignetoImpianto> Vigneti { get; init; } = [];
    public IReadOnlyList<EventoIrrigazione> Irrigazioni { get; init; } = [];

    /// <summary>
    /// Irrigazioni già ripartite per settore, vigneto e giorno (come BI_InputIrrigazioneSettoreVignetoGiornaliera):
    /// si sommano a quelle che vengono da <see cref="Irrigazioni"/>. Servono alla parità con i run salvati.
    /// </summary>
    public IReadOnlyList<IrrigazioneVigneto> IrrigazioniRipartite { get; init; } = [];
}

/// <summary>Un settore irriguo; <c>Asciutta</c> = il settore «Regime in Asciutta» (impianto 0, REGOLE.md § 5.4).</summary>
public sealed record Settore(int IdSettore, bool Asciutta = false);

/// <summary>Un vigneto in un settore, con la sua quota (<c>Percentuale</c>, 0–100).</summary>
public sealed record SettoreVigneto(int IdSettore, int IdVigneto, double Percentuale);

/// <summary>Configurazione dell'impianto di un settore; vale quella con <see cref="ValidoDal"/> più recente (§ 3.5).</summary>
public sealed record ConfigurazioneSettore(int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl, int? IdTipologiaImpianto,
    double? TassoMmOra, double? EfficienzaReale);

public sealed record ShiftSettore(int IdShift, int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl);

/// <summary>Stile del vino del vigneto (BI_VignetoStileConfig): vale quello con <see cref="ValidoDal"/> più recente.</summary>
public sealed record StileVigneto(int IdVigneto, int IdStileVino, DateOnly ValidoDal, DateOnly? ValidoAl);

/// <summary>Una riga di meteo aziendale. ET0 o pioggia vuoti con la riga presente: vedi <see cref="Parametri.MeteoVuotoComeZero"/>.</summary>
public sealed record MeteoGiorno(DateOnly Data, double? Et0Mm, double? PioggiaMm);

public enum FonteFenologia
{
    /// <summary>Rilievo (ModelloBBCH): fra il primo e l'ultimo si riporta l'ultimo valore.</summary>
    Storico,

    /// <summary>Previsione IA: vale solo nel suo giorno, dopo l'ultimo storico.</summary>
    Previsione,
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

/// <summary><c>DistanzaFile</c> = Vigneto.TraFila così com'è; l'unità la dice <see cref="Parametri.DistanzaFileScala"/>.</summary>
public sealed record VignetoImpianto(int IdVigneto, double? DistanzaFile);

/// <summary>Un evento di irrigazione, su un settore oppure su uno shift (§ 3.4).</summary>
public sealed record EventoIrrigazione(DateOnly Data, int? IdSettore, int? IdShift, double? DurataOre = null, double? VolumeMm = null,
    double? Efficienza = null, double? VolumeUtileMm = null);

public sealed record IrrigazioneVigneto(int IdSettore, int IdVigneto, DateOnly Data, double Mm);
