using ProgettoMappe.BilancioIdrico;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

// La configurazione del DSS Bilancio Idrico che sta in Mappe (task #333, PIANO.md principio 5): settori, quote, impianti,
// shift, stili, irrigazioni registrate e tabelle del modello. Ogni voce è un record serializzato in JSON nell'archivio
// (ArchivioConfigurazioneBilancio), con versione, autore e data di ogni modifica.

/// <summary>Un settore irriguo dell'azienda. <c>SuperficieM2</c> serve solo a ripartire le quote nell'importazione.</summary>
public sealed record SettoreIrriguo(int IdSettore, int IdEnte, string Nome, double? SuperficieM2 = null, bool Attivo = true);

/// <summary>
/// La parte del <b>vigneto</b> che sta nel settore, 0–100 (PIANO.md «Correzioni» 1, REGOLE.md § 9): somma ≤ 100 per vigneto.
/// </summary>
public sealed record QuotaVignetoSettore(int IdSettore, int IdVigneto, double Percentuale);

/// <summary>Impianto di un settore da <c>ValidoDal</c> (BI_SettoreConfig): tipologia, gocciolatori, tasso, efficienza.</summary>
public sealed record ImpiantoSettore(int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl, int? IdTipologiaImpianto,
    double? DistanzaGocciolatoriM = null, double? DistanzaAliM = null, double? PortataLh = null, double? TassoMmOra = null,
    double? EfficienzaReale = null);

public sealed record ShiftIrriguo(int IdShift, int IdEnte, string Nome, bool Attivo = true);

public sealed record SettoreNelloShift(int IdShift, int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl);

public sealed record StileVinoVigneto(int IdVigneto, int IdStileVino, DateOnly ValidoDal, DateOnly? ValidoAl);

/// <summary>Un'irrigazione registrata dal consulente, su un settore o su uno shift (BI_InputIrrigazioneEvento).</summary>
public sealed record IrrigazioneRegistrata(int IdEvento, int IdEnte, DateOnly Data, int? IdSettore, int? IdShift,
    double? DurataOre = null, double? VolumeMm = null, double? Efficienza = null, double? VolumeUtileMm = null);

/// <summary>
/// Le tabelle del modello che il consulente può cambiare (BI_ParametroModello, BI_KcCurva, BI_SogliaPdlwp,
/// BI_TipologiaImpianto, BI_RegolaSuoloIrrigazione). Le altre scelte restano in <see cref="Parametri"/> (codice, con test).
/// </summary>
public sealed record ModelloBilancio
{
    public double FrazioneRawAwc { get; init; }
    public IReadOnlyList<KcGestione> CurveKc { get; init; } = [];
    public IReadOnlyList<SogliaStileFase> Soglie { get; init; } = [];
    public IReadOnlyList<EfficienzaTipologia> Efficienze { get; init; } = [];
    public IReadOnlyList<RegolaTurno> RegoleTurno { get; init; } = [];

    /// <summary>IdTessitura → classe delle regole del turno (A, F, S). Vuota = come il vecchio, nessuna regola trovata.</summary>
    public IReadOnlyList<ClasseTessituraTurno> ClassiTessitura { get; init; } = [];

    /// <summary>Le tabelle con i valori di <see cref="Parametri.ComeOggi"/>.</summary>
    public static ModelloBilancio Da(Parametri p) => new()
    {
        FrazioneRawAwc = p.FrazioneRawAwc,
        CurveKc = p.CurveKc.SelectMany(c => c.Value.Select(k => new KcGestione(c.Key, k.Bbch, k.Kc))).ToList(),
        Soglie = p.Soglie.Select(s => new SogliaStileFase(s.Key.Stile, s.Key.Fase, s.Value)).OrderBy(s => s.IdStileVino).ThenBy(s => s.IdFase).ToList(),
        Efficienze = p.EfficienzaImpianto.Select(e => new EfficienzaTipologia(e.Key, e.Value)).OrderBy(e => e.IdTipologiaImpianto).ToList(),
        RegoleTurno = p.RegoleTurno,
        ClassiTessitura = p.ClasseTessitura.Select(c => new ClasseTessituraTurno(c.Key, c.Value)).ToList(),
    };

    /// <summary><paramref name="p"/> con queste tabelle (le tabelle vuote lasciano i valori di <paramref name="p"/>).</summary>
    public Parametri Applica(Parametri p) => p with
    {
        FrazioneRawAwc = FrazioneRawAwc > 0 ? FrazioneRawAwc : p.FrazioneRawAwc,
        CurveKc = CurveKc.Count == 0 ? p.CurveKc : CurveKc.GroupBy(k => k.IdGestioneSuolo).ToDictionary(g => g.Key,
            g => (IReadOnlyList<PuntoKc>)g.OrderBy(k => k.Bbch).Select(k => new PuntoKc(k.Bbch, k.Kc)).ToList()),
        Soglie = Soglie.Count == 0 ? p.Soglie : Soglie.ToDictionary(s => (s.IdStileVino, s.IdFase), s => s.SogliaMpa),
        EfficienzaImpianto = Efficienze.Count == 0 ? p.EfficienzaImpianto
            : Efficienze.ToDictionary(e => e.IdTipologiaImpianto, e => e.Efficienza),
        RegoleTurno = RegoleTurno.Count == 0 ? p.RegoleTurno : RegoleTurno,
        ClasseTessitura = ClassiTessitura.Count == 0 ? p.ClasseTessitura : ClassiTessitura.ToDictionary(c => c.IdTessitura, c => c.Classe),
    };
}

public sealed record KcGestione(int IdGestioneSuolo, int Bbch, double Kc);

public sealed record SogliaStileFase(int IdStileVino, int IdFase, double SogliaMpa);

public sealed record EfficienzaTipologia(int IdTipologiaImpianto, double Efficienza);

public sealed record ClasseTessituraTurno(int IdTessitura, string Classe);

/// <summary>La configurazione valida oggi per un'azienda (l'ultima versione di ogni voce).</summary>
public sealed record ConfigurazioneAzienda
{
    public required int IdEnte { get; init; }
    public IReadOnlyList<SettoreIrriguo> Settori { get; init; } = [];
    public IReadOnlyList<QuotaVignetoSettore> Quote { get; init; } = [];
    public IReadOnlyList<ImpiantoSettore> Impianti { get; init; } = [];
    public IReadOnlyList<ShiftIrriguo> Shift { get; init; } = [];
    public IReadOnlyList<SettoreNelloShift> SettoriShift { get; init; } = [];
    public IReadOnlyList<StileVinoVigneto> Stili { get; init; } = [];
    public IReadOnlyList<IrrigazioneRegistrata> Irrigazioni { get; init; } = [];

    /// <summary>Le tabelle del modello (uguali per tutte le aziende); null = quelle di <see cref="Parametri.ComeOggi"/>.</summary>
    public ModelloBilancio? Modello { get; init; }
}
