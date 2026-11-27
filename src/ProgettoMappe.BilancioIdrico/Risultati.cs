namespace ProgettoMappe.BilancioIdrico;

public enum Suggerimento
{
    Irrigare,
    NonIrrigare,

    /// <summary>Nessuna soglia (manca lo stile del vino o la fase): «N_A».</summary>
    NonApplicabile,

    /// <summary>Suolo a zero con <see cref="Parametri.SuoloVuotoIrrigare"/> spento.</summary>
    DatoNonValido,
}

public static class CodiciSuggerimento
{
    /// <summary>I codici del vecchio motore (BI_ResultSettoreVignetoGiornaliero.SuggerimentoIrrigazione).</summary>
    public static string Codice(this Suggerimento s) => s switch
    {
        Suggerimento.Irrigare => "IRRIGARE",
        Suggerimento.NonIrrigare => "NON_IRRIGARE",
        Suggerimento.NonApplicabile => "N_A",
        _ => "DATO_NON_VALIDO",
    };
}

/// <summary>
/// Un vigneto in un giorno: gli input scelti e le uscite (come BI_ResultSettoreVignetoGiornaliero, che però aveva una riga
/// per settore). Il consiglio in ore, che dipende dall'impianto, sta in <see cref="Settori"/>.
/// </summary>
public sealed record RisultatoVignetoGiorno
{
    public required int IdVigneto { get; init; }
    public required DateOnly Data { get; init; }

    // Input scelti per il giorno
    public double? Et0Mm { get; init; }
    public double? PioggiaMm { get; init; }
    public double IrrigazioneMm { get; init; }
    public int? Bbch { get; init; }
    public int? Fase { get; init; }
    public int? IdGestioneSuolo { get; init; }
    public int? IdStileVino { get; init; }
    public double? AwcMm { get; init; }
    public double? Kc { get; init; }
    public double? SogliaMpa { get; init; }
    public int TurnoIrriguoGiorni { get; init; }

    // Uscite
    public double? Ks { get; init; }
    public double? EtaMm { get; init; }

    /// <summary>Acqua disponibile: a inizio giornata con <see cref="Parametri.AwConFlussiDiIeri"/>, altrimenti a fine giornata.</summary>
    public double AwMm { get; init; }

    public double? Faw { get; init; }
    public double? PdlwpMpa { get; init; }
    public double? DeficitMm { get; init; }
    public double EtaFuturaMm { get; init; }
    public double IrrigazioneSuggeritaMm { get; init; }
    /// <summary>Acqua persa oltre la capacità di campo (percolazione, D) nei flussi del giorno.</summary>
    public double PerditeMm { get; init; }

    /// <summary>Nessun settore con un impianto (impianto ≠ 0) nel giorno.</summary>
    public bool SenzaImpianto { get; init; }

    /// <summary>Il consiglio per ogni settore in cui sta il vigneto (vuoto senza settore).</summary>
    public IReadOnlyList<ConsiglioSettore> Settori { get; init; } = [];
    public Suggerimento Suggerimento { get; init; }

    public bool MeteoMancante { get; init; }

    /// <summary>Task #383: il vigneto da cui viene il meteo del giorno (ripiego); null = meteo della propria azienda.</summary>
    public int? MeteoDaVigneto { get; init; }
    public double? MeteoDistanzaKm { get; init; }
    public bool FenologiaMancante { get; init; }
    public bool SuoloMancante { get; init; }
    /// <summary>Uno dei settori del vigneto non ha la configurazione nel giorno (senza settore: mai).</summary>
    public bool ConfigMancante { get; init; }
    public bool KcMancante { get; init; }
    public bool CriticitaPdlwp { get; init; }
    public bool CriticitaDeficit { get; init; }

    /// <summary>3 manca un dato, 2 criticità Pdlwp, 1 criticità deficit, 0 niente.</summary>
    public int Priorita { get; init; }
}

/// <summary>
/// Il consiglio irriguo del vigneto per l'impianto di un settore: ore = mm suggeriti / (tasso × efficienza), vuote senza
/// tasso o senza configurazione. <c>OltreTempoMassimo</c>: le ore superano il tempo massimo della regola del suolo.
/// </summary>
public sealed record ConsiglioSettore(int IdSettore, double Percentuale, double? IrrigazioneSuggeritaOre, bool ConfigMancante,
    bool OltreTempoMassimo = false);

/// <summary>§ 2: un settore in un giorno.</summary>
public sealed record RisultatoSettoreGiorno(int IdSettore, DateOnly Data, double IrrigazioneSuggeritaMm, double? IrrigazioneSuggeritaOre,
    int Priorita);

/// <summary>§ 2: uno shift in un giorno.</summary>
public sealed record RisultatoShiftGiorno(int IdShift, DateOnly Data, double? OreComando, int SettoriCoinvolti);

public sealed record RisultatoBilancio(IReadOnlyList<RisultatoVignetoGiorno> Vigneti, IReadOnlyList<RisultatoSettoreGiorno> Settori,
    IReadOnlyList<RisultatoShiftGiorno> Shift)
{
    /// <summary>I valori del suolo e dello stile usati per ogni vigneto nell'ultimo giorno, con da dove vengono.</summary>
    public IReadOnlyList<SuoloVigneto> Suoli { get; init; } = [];
}

/// <summary>Da dove viene un valore usato per l'AWC o lo stile (la scheda del vigneto lo mostra).</summary>
public static class FontiValore
{
    public const string Analisi = "analisi";
    public const string Settore = "settore";
    public const string Calibrazione = "calibrazione";
    public const string Rilievo = "rilievo";
    public const string Ripiego = "ripiego";
    public const string Configurazione = "configurazione";
    public const string Target = "target";
}

/// <summary>
/// Matteo, s8: i valori del vigneto che danno l'AWC (acqua della tessitura, profondità del suolo, scheletro, radici, interfila)
/// e lo stile, ognuno con la sua fonte (<see cref="FontiValore"/>; null = manca). <c>VignetiNelSettore</c> = quanti vigneti
/// con analisi fanno la media del settore.
/// </summary>
public sealed record SuoloVigneto(
    int IdVigneto,
    double? AcquaDisponibileMmM,
    string? FonteAcqua,
    double? ProfonditaSuoloM,
    string? FonteProfondita,
    double? ScheletroPerc,
    string? FonteScheletro,
    double? ProfonditaRadiciM,
    double? LarghezzaRadiciM,
    string? FonteRadici,
    double? DistanzaFileM,
    double FattoreRadici,
    double? AwcMm,
    string? FonteAwc,
    int VignetiNelSettore,
    int? IdStileVino,
    string? FonteStile,
    int? IdTargetConcimazione);
