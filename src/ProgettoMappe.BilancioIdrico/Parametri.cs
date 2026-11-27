namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// Tutte le scelte del modello, con i valori di partenza = quello che fa oggi il codice SQL di 4g (REGOLE.md §§ 4 e 7).
/// Le formule leggono solo da qui: per cambiare una scelta (per esempio una risposta dell'agronomo, REGOLE.md § 8) si crea
/// un'altra istanza con <c>with</c>, senza toccare il motore. Accanto a ogni scelta aperta c'è il numero della domanda di § 8.
/// </summary>
public sealed record Parametri
{
    /// <summary>Nome del preset da cui viene (<c>ComeOggi</c>, <c>ComeMatteo</c>): finisce nella versione dei parametri.</summary>
    public string Nome { get; init; } = NomeComeOggi;

    public const string NomeComeOggi = "ComeOggi";
    public const string NomeComeMatteo = "ComeMatteo";

    // ---- Costanti del modello (§ 4.1; domanda 13) ----

    /// <summary>Frazione dell'AWC facilmente utilizzabile (RAW): Ks &lt; 1 sotto AWC × (1 − RAW). BI_ParametroModello.</summary>
    public double FrazioneRawAwc { get; init; } = 0.45;

    /// <summary>Pdlwp = ln(FAW / numeratore) / divisore.</summary>
    public double PdlwpNumeratore { get; init; } = 1.057;

    public double PdlwpDivisore { get; init; } = 5.3452;

    /// <summary>Criticità Pdlwp: Pdlwp ≤ soglia + questo valore (MPa).</summary>
    public double PdlwpDeltaAvvisoMpa { get; init; } = 0.04;

    /// <summary>
    /// Domanda 15: frazione dell'AWC rimasta sotto la quale comincia lo stress (Ks &lt; 1) e la criticità deficit.
    /// <c>null</c> (codice) = 1 − <see cref="FrazioneRawAwc"/> = 0,55; l'Excel di Matteo usa 0,40.
    /// </summary>
    public double? FrazioneAwcInizioStress { get; init; }

    /// <summary>La frazione dell'AWC sotto la quale Ks &lt; 1.</summary>
    public double InizioStress => FrazioneAwcInizioStress ?? 1 - FrazioneRawAwc;

    /// <summary>
    /// Domanda 17: <c>true</c> (Excel) = l'avviso anticipato di <see cref="PdlwpDeltaAvvisoMpa"/> cambia anche suggerimento e
    /// deficit (si irriga a soglia + delta); <c>false</c> (codice) = segnala solo la criticità.
    /// </summary>
    public bool AvvisoAnticipatoNelSuggerimento { get; init; }

    /// <summary>Domanda 18: <c>true</c> (Excel) = il suggerimento guarda il Pdlwp di ieri; <c>false</c> (codice) = quello di oggi.</summary>
    public bool SuggerimentoConPdlwpDiIeri { get; init; }

    // ---- Inizio stagione e acqua iniziale (domanda 1; § 6) ----

    /// <summary>
    /// <c>true</c> = come il vecchio motore: il bilancio parte da <see cref="IngressiAzienda.Da"/> a suolo pieno (il «bug AWPrev»).
    /// Serve solo per la parità con i run salvati. <c>false</c> = bilancio continuo da <see cref="InizioStagioneMese"/>/
    /// <see cref="InizioStagioneGiorno"/>, qualunque sia il primo giorno chiesto.
    /// </summary>
    public bool PartenzaComeIlVecchio { get; init; }

    /// <summary>
    /// Solo per la parità: AW ed ETa di ogni giorno arrotondate a 4 decimali prima di passare al giorno dopo, come le colonne
    /// decimal del vecchio motore. Senza, su 5 mesi lo scarto sull'AW arriva a 0,0008 mm.
    /// </summary>
    public bool DecimaliComeSql { get; init; }

    public int InizioStagioneMese { get; init; } = 1;
    public int InizioStagioneGiorno { get; init; } = 1;

    /// <summary>Acqua del primo giorno come frazione dell'AWC (1 = suolo pieno).</summary>
    public double FrazioneAcquaIniziale { get; init; } = 1;

    /// <summary>Se valorizzato, il suolo resta pieno finché il vigneto non raggiunge questo BBCH (per esempio 10 = germogliamento).</summary>
    public int? SuoloPienoFinoAlBbch { get; init; }

    // ---- Bilancio del giorno ----

    /// <summary>
    /// Domanda 2. <c>true</c> (codice): l'AW di un giorno è l'acqua a inizio giornata, con i flussi di ieri. <c>false</c>
    /// (documenti): l'AW è a fine giornata, con i flussi di oggi (Ks resta calcolato sull'acqua a inizio giornata).
    /// </summary>
    public bool AwConFlussiDiIeri { get; init; } = true;

    /// <summary>Pioggia sotto questa soglia (mm) non conta (presentazione: «&lt; 5 mm non utile»). 0 = conta tutta.</summary>
    public double PioggiaMinimaUtileMm { get; init; }

    /// <summary>
    /// Matteo, risposta 7: <c>true</c> = <see cref="PioggiaMinimaUtileMm"/> vale per l'<b>evento</b> piovoso, cioè i giorni
    /// consecutivi con pioggia sommati (4 mm la sera + 10 mm la mattina dopo = 14 mm, tutti utili); un evento sotto soglia
    /// non entra nel bilancio. <c>false</c> = la soglia vale giorno per giorno.
    /// </summary>
    public bool PioggiaPerEvento { get; init; }

    /// <summary>
    /// Domanda 22, punto d'estensione per il ruscellamento: mm di pioggia del giorno che scorrono via e non entrano nel
    /// suolo (argomenti: pioggia, ET0). <c>null</c> = nessun ruscellamento (codice ed Excel). Le perdite oltre la capacità di
    /// campo (D) sono sempre calcolate: <see cref="RisultatoVignetoGiorno.PerditeMm"/>.
    /// </summary>
    public Func<double, double?, double>? Ruscellamento { get; init; }

    /// <summary>Domanda 3: giorni dopo oggi sommati nell'ETa futura (codice: 2 fissi).</summary>
    public int GiorniEtaFutura { get; init; } = 2;

    /// <summary>Domanda 3: <c>true</c> = l'ETa futura usa il turno irriguo del suolo invece di <see cref="GiorniEtaFutura"/>.</summary>
    public bool EtaFuturaDalTurnoIrriguo { get; init; }

    /// <summary>Turno irriguo quando nessuna regola lo trova (§ 1.13).</summary>
    public int TurnoIrriguoPredefinito { get; init; } = 3;

    /// <summary>
    /// Regole del turno per classe di tessitura e scheletro (BI_RegolaSuoloIrrigazione). Il codice SQL non le trova mai
    /// (lettere contro numeri, § 1.13): per riprodurlo l'elenco <see cref="ClasseTessitura"/> è vuoto.
    /// </summary>
    public IReadOnlyList<RegolaTurno> RegoleTurno { get; init; } = RegoleTurnoDiPartenza;

    /// <summary>
    /// IdTessitura → classe delle regole (A, F, S). Vuota come oggi: nessuna regola si applica. Con una regola trovata il
    /// motore dà anche il turno e il controllo del tempo massimo (<see cref="ConsiglioSettore.OltreTempoMassimo"/>).
    /// </summary>
    public IReadOnlyDictionary<int, string> ClasseTessitura { get; init; } = new Dictionary<int, string>();

    /// <summary>
    /// Domanda 21: <c>true</c> (codice) = irrigazione suggerita = deficit + ETa di oggi + ETa futura; <c>false</c> (Excel) =
    /// deficit + ETa futura (i giorni del turno).
    /// </summary>
    public bool VolumeConEtaDiOggi { get; init; } = true;

    /// <summary>
    /// <c>false</c> (codice e presentazione: «reintegro alla soglia PLWP») = il deficit riporta la FAW alla soglia;
    /// <c>true</c> = la riporta a capacità di campo (FAW 1).
    /// </summary>
    public bool ReintegroACapacitaDiCampo { get; init; }

    // ---- Suolo (domande 4, 5, 12; § 1.1) ----

    /// <summary>Domanda 5: <c>true</c> (codice) = l'ultima analisi in assoluto vale per tutti i giorni; <c>false</c> = quella fatta entro il giorno.</summary>
    public bool AnalisiSuoloPiuRecente { get; init; } = true;

    /// <summary>Come domanda 5, per la gestione del suolo (rilievo 140) e le radici (rilievo 145).</summary>
    public bool RilieviVignetoPiuRecenti { get; init; } = true;

    /// <summary>Domanda 4: metri per un punto di «ProfonditàSuolo» (codice: il punteggio è già metri).</summary>
    public double ProfonditaSuoloScala { get; init; } = 1;

    /// <summary>Profondità in metri quando manca o è 0.</summary>
    public double ProfonditaSuoloRipiegoM { get; init; } = 1;

    /// <summary>Domanda 4: percento per un punto di «Pietrosità».</summary>
    public double PietrositaScala { get; init; } = 1;

    /// <summary>§ 7.6: metri per un'unità di Vigneto.TraFila (codice: metri; se fossero cm: 0,01).</summary>
    public double DistanzaFileScala { get; init; } = 1;

    /// <summary>Domanda 20: <c>true</c> (codice) = AWC per il volume radicale (radici / interfila × profondità radici); <c>false</c> (Excel) = senza.</summary>
    public bool AwcConVolumeRadicale { get; init; } = true;

    /// <summary>
    /// <c>false</c> (codice): «suolo mancante» (priorità 3) solo senza nessuna analisi; un'analisi senza acqua disponibile
    /// (per esempio senza tessitura) dà AWC vuota ma priorità 0. <c>true</c> = anche l'AWC vuota è «suolo mancante».
    /// </summary>
    public bool AwcVuotaComeSuoloMancante { get; init; }

    /// <summary>
    /// Matteo, risposta 4: <c>true</c> = acqua disponibile della tessitura e scheletro sono la <b>media del settore</b> (dei
    /// vigneti che stanno in un settore con lui; senza settore, del vigneto), con tutte le analisi qualunque sia la data
    /// (sono stabili nel tempo). La profondità del suolo resta quella del vigneto (media delle sue analisi), poi del settore.
    /// <c>false</c> (codice) = l'analisi del vigneto scelta con <see cref="AnalisiSuoloPiuRecente"/>.
    /// </summary>
    public bool SuoloMedioDelSettore { get; init; }

    /// <summary>Domanda 12: gestione del suolo senza rilievo 140 (2 = Lavorato). <c>null</c> = Kc mancante.</summary>
    public int? GestioneSuoloRipiego { get; init; } = 2;

    // ---- Meteo (domanda 6) ----

    /// <summary><c>true</c> (codice): riga presente con ET0 o pioggia vuoti = 0. <c>false</c> = meteo mancante.</summary>
    public bool MeteoVuotoComeZero { get; init; } = true;

    /// <summary>
    /// Task #383 (proprietario, 27/11/2026): nei giorni senza meteo dell'azienda il vigneto prende il meteo del vigneto attivo
    /// più vicino che ce l'ha, entro questo raggio dal centro (oltre resta «meteo mancante»). <c>null</c> o ≤ 0 = spento.
    /// </summary>
    public double? RaggioMeteoVicinoKm { get; init; } = 15;

    // ---- Soglia e suggerimento (domande 7, 8, 9, 11, 14) ----

    /// <summary>Domanda 7: suolo a zero (Pdlwp non calcolabile). <c>true</c> = IRRIGARE; <c>false</c> = DATO_NON_VALIDO.</summary>
    public bool SuoloVuotoIrrigare { get; init; } = true;

    /// <summary>
    /// Domanda 8: <c>true</c> (codice, che lo faceva col settore «Regime in Asciutta») = anche un vigneto senza impianto
    /// (nessun settore, o settori con impianto 0) ha il suggerimento; <c>false</c> = N_A, solo il monitoraggio dello stress.
    /// </summary>
    public bool SuggerimentoSenzaImpianto { get; init; } = true;

    /// <summary>
    /// Matteo, risposta 10: <c>false</c> = un vigneto in <b>asciutta</b> (tutti i suoi settori hanno impianto 0) ha bilancio e
    /// stress ma non il suggerimento (N_A). Un vigneto senza settore non è «in asciutta»: è solo non ancora configurato, e
    /// per lui vale <see cref="SuggerimentoSenzaImpianto"/>.
    /// </summary>
    public bool SuggerimentoInAsciutta { get; init; } = true;

    /// <summary>Domanda 9: fasi in cui il suggerimento è sempre NON_IRRIGARE (codice: solo la 0).</summary>
    public IReadOnlySet<int> FasiNonIrrigare { get; init; } = new HashSet<int> { 0 };

    /// <summary>Domanda 11: <c>true</c> = mm e ore suggeriti a zero quando il suggerimento non è IRRIGARE.</summary>
    public bool AzzeraVolumeSeNonIrrigare { get; init; }

    /// <summary>Domanda 14: stile del vino quando il vigneto non ne ha uno. <c>null</c> = niente soglia (N_A).</summary>
    public int? StileVinoRipiego { get; init; }

    /// <summary>
    /// Matteo, risposta 9: target di concimazione (TargetEnologico di 4Grapes) → stile del vino, per i vigneti senza stile.
    /// Vuota = nessun ripiego dal target (la tabella si compila con lui nella configurazione «modello»).
    /// </summary>
    public IReadOnlyDictionary<int, int> StileDaTarget { get; init; } = new Dictionary<int, int>();

    /// <summary>Soglie Pdlwp (MPa) per (stile, fase), BI_SogliaPdlwp. Domanda 9: la fase 7 oggi è −0,2 per tutti.</summary>
    public IReadOnlyDictionary<(int Stile, int Fase), double> Soglie { get; init; } = SoglieDiPartenza;

    // ---- Irrigazione (domanda 10; § 3.4, § 1.11) ----

    /// <summary>Domanda 10: <c>false</c> (codice) = l'evento su uno shift dà ore × tasso senza efficienza.</summary>
    public bool EfficienzaSuShift { get; init; }

    /// <summary>
    /// Matteo («l'efficienza considerala sempre»): <c>true</c> = anche le irrigazioni registrate su un settore in ore, o in
    /// mm senza un'efficienza propria, passano dall'efficienza dell'impianto (come gli shift con <see cref="EfficienzaSuShift"/>).
    /// </summary>
    public bool EfficienzaSulleIrrigazioni { get; init; }

    /// <summary>
    /// Solo per la parità con i run vecchi (REGOLE.md § 9): <c>true</c> = <see cref="SettoreVigneto.Percentuale"/> è la parte
    /// del settore occupata dal vigneto (somma 100 per settore, l'errore del vecchio DSS) e i totali del settore sono
    /// Σ valore × percentuale / 100. <c>false</c> = parte del vigneto nel settore; totali del settore come media pesata
    /// con superficie × quota.
    /// </summary>
    public bool QuoteDelSettore { get; init; }

    /// <summary>Efficienza tipica per impianto (BI_TipologiaImpianto), quando il settore non ha quella reale.</summary>
    public IReadOnlyDictionary<int, double> EfficienzaImpianto { get; init; } = EfficienzaDiPartenza;

    /// <summary>Efficienza quando mancano sia la reale sia la tipica.</summary>
    public double EfficienzaRipiego { get; init; } = 1;

    // ---- Tabelle del modello ----

    /// <summary>Curve del Kc per gestione del suolo (BI_KcCurva), punti ordinati per BBCH.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<PuntoKc>> CurveKc { get; init; } = CurveKcDiPartenza;

    /// <summary>
    /// Domanda 16: Kc a gradini per (gestione, fase), come l'Excel. <c>null</c> (codice) = interpolato sulle
    /// <see cref="CurveKc"/>.
    /// </summary>
    public IReadOnlyDictionary<(int Gestione, int Fase), double>? KcPerFase { get; init; }

    /// <summary>Domanda 16: <c>true</c> (Excel) = il Kc di oggi usa il BBCH del giorno dopo.</summary>
    public bool KcConBbchDelGiornoDopo { get; init; }

    /// <summary>
    /// Fase fenologica da BBCH: la fase è quella dell'ultimo intervallo con <see cref="IntervalloFase.BbchDa"/> ≤ BBCH
    /// (valori scritti nel codice, § 1.3; BI_FaseFenologica non è letta).
    /// </summary>
    public IReadOnlyList<IntervalloFase> Fasi { get; init; } = FasiDiPartenza;

    public static IReadOnlyList<IntervalloFase> FasiDiPartenza { get; } =
    [
        new(int.MinValue, 0), new(10, 1), new(55, 2), new(61, 3), new(72, 4), new(79, 5), new(85, 6), new(91, 7),
    ];

    public static IReadOnlyDictionary<int, IReadOnlyList<PuntoKc>> CurveKcDiPartenza { get; } = new Dictionary<int, IReadOnlyList<PuntoKc>>
    {
        [1] = [new(0, 0.30), new(10, 0.45), new(65, 0.70), new(89, 0.70), new(97, 0.30)],
        [2] = [new(0, 0.20), new(10, 0.30), new(65, 0.65), new(89, 0.65), new(97, 0.20)],
        [3] = [new(0, 0.25), new(10, 0.40), new(65, 0.70), new(89, 0.70), new(97, 0.25)],
    };

    public static IReadOnlyDictionary<(int Stile, int Fase), double> SoglieDiPartenza { get; } = CreaSoglie(new double[,]
    {
        { -0.2, -0.2, -0.2, -0.2, -0.2, -0.2, -0.2 },
        { -0.2, -0.2, -0.2, -0.2, -0.3, -0.4, -0.2 },
        { -0.2, -0.2, -0.2, -0.2, -0.4, -0.45, -0.2 },
        { -0.2, -0.2, -0.2, -0.3, -0.5, -0.6, -0.2 },
        { -0.2, -0.2, -0.3, -0.3, -0.6, -0.7, -0.2 },
        { -0.2, -0.2, -0.3, -0.3, -0.6, -0.8, -0.2 },
    });

    public static IReadOnlyDictionary<int, double> EfficienzaDiPartenza { get; } = new Dictionary<int, double>
    {
        [0] = 1, [1] = 0.90, [2] = 0.93, [3] = 0.88, [4] = 0.80, [5] = 0.70,
    };

    public static IReadOnlyList<RegolaTurno> RegoleTurnoDiPartenza { get; } =
    [
        new("A", 0, 5, 9, 6), new("A", 5, 15, 8, 6), new("A", 15, 100, 7, 6),
        new("F", 0, 5, 6, 3), new("F", 5, 15, 5, 3), new("F", 15, 100, 4, 3),
        new("S", 0, 5, 3, 1), new("S", 5, 15, 2.5, 1), new("S", 15, 100, 2, 1),
    ];

    /// <summary>I valori del motore SQL di maggio 2026 (dopo le tabelle statiche: l'ordine di inizializzazione conta).</summary>
    public static Parametri ComeOggi { get; } = new();

    /// <summary>Soglia stile 3, fase 6 (maturazione) del file di aprile di Matteo; nel DB del vecchio DSS (BI_SogliaPdlwp) −0,45.</summary>
    public const double SogliaStile3Fase6Matteo = -0.5;

    /// <summary>
    /// Le risposte di Matteo Frandoli del 18/11/2026 (RISPOSTE-MATTEO-2026-11-18.md, REGOLE.md § 10): RAW 0,5; gestione di
    /// ripiego inerbito; meteo mancante = anomalia del giorno; pioggia utile per evento ≥ 5 mm; ETa futura sui 7 giorni di
    /// previsione; suggerimento sul Pdlwp di ieri, senza l'avviso 0,04; suolo a zero = dato non valido; soglie del file di aprile;
    /// suolo medio del settore; efficienza sempre; asciutta senza suggerimento. Kc interpolato con la fase del giorno e AW a
    /// inizio giornata restano come <see cref="ComeOggi"/>.
    /// </summary>
    public static Parametri ComeMatteo { get; } = new()
    {
        Nome = NomeComeMatteo,
        FrazioneRawAwc = 0.50,
        GestioneSuoloRipiego = 1,
        MeteoVuotoComeZero = false,
        PioggiaMinimaUtileMm = 5,
        PioggiaPerEvento = true,
        GiorniEtaFutura = 7,
        EtaFuturaDalTurnoIrriguo = false,
        AvvisoAnticipatoNelSuggerimento = false,
        SuggerimentoConPdlwpDiIeri = true,
        SuoloVuotoIrrigare = false,
        SuggerimentoInAsciutta = false,
        Soglie = SoglieDiPartenza.ToDictionary(s => s.Key, s => s.Key == (3, 6) ? SogliaStile3Fase6Matteo : s.Value),
        SuoloMedioDelSettore = true,
        EfficienzaSuShift = true,
        EfficienzaSulleIrrigazioni = true,
    };

    /// <summary>Il preset con quel nome (<see cref="NomeComeOggi"/> se il nome non si conosce).</summary>
    public static Parametri Preset(string? nome) =>
        string.Equals(nome?.Trim(), NomeComeMatteo, StringComparison.OrdinalIgnoreCase) ? ComeMatteo : ComeOggi;

    /// <summary>Righe stile 1..6 × fasi 1..7.</summary>
    private static Dictionary<(int, int), double> CreaSoglie(double[,] valori)
    {
        var soglie = new Dictionary<(int, int), double>();
        for (var s = 0; s < valori.GetLength(0); s++)
            for (var f = 0; f < valori.GetLength(1); f++)
                soglie[(s + 1, f + 1)] = valori[s, f];
        return soglie;
    }
}

public sealed record PuntoKc(int Bbch, double Kc);

public sealed record IntervalloFase(int BbchDa, int Fase);

/// <summary>Una riga di BI_RegolaSuoloIrrigazione: scheletro in [Da, A) percento.</summary>
public sealed record RegolaTurno(string ClasseTessitura, double ScheletroDaPerc, double ScheletroAPerc, double TempoMassimoOre,
    int TurnoIrriguoGiorni);
