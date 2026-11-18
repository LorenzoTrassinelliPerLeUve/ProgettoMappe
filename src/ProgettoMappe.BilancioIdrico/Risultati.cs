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
    IReadOnlyList<RisultatoShiftGiorno> Shift);
