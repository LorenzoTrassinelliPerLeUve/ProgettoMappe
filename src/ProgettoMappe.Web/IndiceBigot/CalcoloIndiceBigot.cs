using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Un parametro dell'Indice Bigot: Id come in 4Grapes, nome leggibile, peso di <c>ParametroIndice</c>.</summary>
public sealed record ParametroIndice(string Id, string Nome, string Breve, int Peso)
{
    /// <summary>Punteggio massimo del parametro (score al massimo): Peso × 100 / 223.</summary>
    public double Massimo => Peso * 100.0 / CalcoloIndiceBigot.PesoTotale;
}

/// <summary>Indice di un vigneto (o sottovigneto) in un anno: somma dei punteggi dei parametri presenti.</summary>
public sealed record IndiceVignetoAnno(int Id, int Anno, double Punteggio, int NumParametri, int Peso)
{
    public bool Completo => NumParametri == CalcoloIndiceBigot.Parametri.Count;
}

/// <summary>KPI dell'anno sui vigneti scelti (fogli «IB ISTOGRAMMA + KPI» di Qlik: solo vigneti con 9 parametri).</summary>
public sealed record KpiIndice(int Anno, int VignetiCompleti, int VignetiConIndice, double? Minimo, double? Massimo, double? MediaPesata);

/// <summary>Barra dell'istogramma: punteggio del parametro (o media dei vigneti) e suo massimo.</summary>
public sealed record BarraParametro(ParametroIndice Parametro, double? Punteggio, string? Valore);

/// <summary>
/// Le formule dell'app Qlik «INDICE BIGOT - REPORT» (task #189), riscritte su righe di
/// <c>VistaIndicePerParametro</c>: la logica dell'indice è nel database, qui si somma, si filtra e si
/// fa la media come Qlik. Regole:
/// <list type="bullet">
/// <item>Indice del vigneto nell'anno = somma dei <c>Punteggio</c> delle sue righe (non arrotondato).</item>
/// <item><c>NumParametri</c> = numero di parametri presenti (in Qlik veniva dalla join con
/// <c>VistaIndicePerVigneto</c>, troppo lenta: qui si conta).</item>
/// <item>«Anno scelto» (<c>Max(Anno_Indice)</c> di Qlik) = l'anno indicato o, se manca, l'ultimo anno con
/// righe fra i vigneti scelti.</item>
/// <item>KPI e media per vigna/vitigno: solo vigneti con tutti i 9 parametri; tabelle per vigneto: tutti.</item>
/// <item>Media dell'azienda: media dei punteggi pesata con <c>Vigneto.Superficie</c> (grezza, come Qlik);
/// vigneti senza superficie fuori dalla media.</item>
/// </list>
/// </summary>
public static class CalcoloIndiceBigot
{
    /// <summary>Somma dei pesi di <c>dbo.ParametroIndice</c> (non concessa: pesi copiati dall'analisi del 21/10).</summary>
    public const int PesoTotale = 223;

    /// <summary>I nove parametri, nell'ordine dei fogli di Qlik.</summary>
    public static readonly IReadOnlyList<ParametroIndice> Parametri =
    [
        new("VIGORIA", "Vigoria", "Vigoria", 8),
        new("MORFOLOGIA", "Morfologia del grappolo", "Morfologia", 4),
        new("SFE", "Superficie fogliare esposta (m²/vite)", "SFE", 22),
        new("PRODUZIONE", "Produzione (kg/vite)", "Produzione", 17),
        new("SFEKg", "SFE/kg (m² di foglia per kg d'uva)", "SFE/kg", 50),
        new("REGIME IDRICO", "Stress idrico", "Stress idrico", 23),
        new("AMPELOPATIE", "Sanità dei grappoli (ampelopatie)", "Sanità", 77),
        new("BIODIVERSITA", "Biodiversità", "Biodiversità", 12),
        new("ETA VIGNETO", "Età del vigneto", "Età", 10),
    ];

    private static readonly Dictionary<string, ParametroIndice> PerId =
        Parametri.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public static ParametroIndice? Parametro(string id) => PerId.GetValueOrDefault(id);

    private static int Chiave(RigaIndiceParametroDto r) => r.IdSubVigneto ?? r.IdVigneto;

    /// <summary>Anni con righe, dal più recente.</summary>
    public static IReadOnlyList<int> Anni(IEnumerable<RigaIndiceParametroDto> righe) =>
        righe.Select(r => r.Anno).Distinct().OrderDescending().ToList();

    /// <summary>L'anno indicato se ha righe, altrimenti l'ultimo anno con righe; null se non ci sono righe.</summary>
    public static int? AnnoScelto(IReadOnlyCollection<RigaIndiceParametroDto> righe, int? anno)
    {
        if (righe.Count == 0)
        {
            return null;
        }
        return anno is { } a && righe.Any(r => r.Anno == a) ? a : righe.Max(r => r.Anno);
    }

    /// <summary>
    /// Indice per vigneto (o sottovigneto, se le righe sono della vista _Sub) e anno. Il peso è la somma
    /// dei pesi dei parametri presenti (223 con tutti).
    /// </summary>
    public static IReadOnlyList<IndiceVignetoAnno> Indici(IEnumerable<RigaIndiceParametroDto> righe) =>
        righe.GroupBy(r => (Id: Chiave(r), r.Anno))
            .Select(g =>
            {
                // NumParametri = numero di righe, come COUNT(IdParametro) di VistaIndicePerVigneto[_Sub] usato da
                // Qlik: nella vista _Sub la vigoria ha righe ripetute che contano due volte (anche nel punteggio).
                // Decisione del proprietario (12/11): copiare Qlik, doppioni compresi. Per vigneto le righe sono uniche.
                var parametri = g.Select(r => r.IdParametro).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return new IndiceVignetoAnno(g.Key.Id, g.Key.Anno,
                    g.Sum(r => r.Punteggio ?? 0),
                    g.Count(),
                    parametri.Sum(p => Parametro(p)?.Peso ?? 0));
            })
            .ToList();

    /// <summary>
    /// KPI dell'anno sui vigneti delle righe (fogli 2 di Qlik): minimo, massimo e media pesata con la
    /// superficie dei soli vigneti con 9 parametri.
    /// </summary>
    public static KpiIndice Kpi(IReadOnlyCollection<RigaIndiceParametroDto> righe, IReadOnlyDictionary<int, double?> superfici, int anno)
    {
        var indici = Indici(righe.Where(r => r.Anno == anno)).ToList();
        var completi = indici.Where(i => i.Completo).ToList();

        double? media = null;
        var pesati = completi.Where(i => superfici.GetValueOrDefault(i.Id) is not null).ToList();
        var sommaSuperfici = pesati.Sum(i => superfici[i.Id]!.Value);
        if (pesati.Count > 0 && sommaSuperfici != 0)
        {
            media = pesati.Sum(i => i.Punteggio * superfici[i.Id]!.Value) / sommaSuperfici;
        }

        return new KpiIndice(anno, completi.Count, indici.Count,
            completi.Count > 0 ? completi.Min(i => i.Punteggio) : null,
            completi.Count > 0 ? completi.Max(i => i.Punteggio) : null,
            media);
    }

    /// <summary>
    /// Istogramma dei 9 parametri nell'anno. Con un solo vigneto: i suoi punteggi e valori. Con più
    /// vigneti: la media dei punteggi fra i vigneti con 9 parametri (come la tabella per vigna/vitigno di
    /// Qlik) e il valore medio del parametro.
    /// </summary>
    public static IReadOnlyList<BarraParametro> Istogramma(IReadOnlyCollection<RigaIndiceParametroDto> righe, int anno)
    {
        var dellAnno = righe.Where(r => r.Anno == anno).ToList();
        var vigneti = dellAnno.Select(Chiave).Distinct().Count();
        if (vigneti > 1)
        {
            var completi = Indici(dellAnno).Where(i => i.Completo).Select(i => i.Id).ToHashSet();
            dellAnno = dellAnno.Where(r => completi.Contains(Chiave(r))).ToList();
        }

        return Parametri.Select(p =>
        {
            var delParametro = dellAnno.Where(r => string.Equals(r.IdParametro, p.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            // Punteggio del vigneto = somma delle sue righe del parametro; poi media fra i vigneti.
            var perVigneto = delParametro.GroupBy(Chiave).Select(g => g.Sum(r => r.Punteggio ?? 0)).ToList();
            return new BarraParametro(p, perVigneto.Count > 0 ? perVigneto.Average() : null, Valore(p, delParametro));
        }).ToList();
    }

    /// <summary>
    /// Valore leggibile di un parametro sulle righe date (già filtrate per parametro), con le formule di
    /// Qlik: classi di vigoria e morfologia da <c>Avg</c>, stress idrico da <c>Max</c>, sanità =
    /// (100 − <c>Max</c>) %, biodiversità ed età da <c>Valore</c>, SFE/produzione/SFE-kg da <c>Avg</c>.
    /// </summary>
    public static string? Valore(ParametroIndice parametro, IReadOnlyCollection<RigaIndiceParametroDto> righe)
    {
        var numero = ValoreNumerico(parametro, righe);
        if (numero is not { } v)
        {
            return null;
        }

        return parametro.Id.ToUpperInvariant() switch
        {
            "VIGORIA" => ClasseVigoria(v),
            "MORFOLOGIA" => ClasseMorfologia(v),
            "REGIME IDRICO" => ClasseStressIdrico(v),
            "AMPELOPATIE" => v.ToString("0 '%'", Cultura),
            "ETA VIGNETO" or "BIODIVERSITA" => v.ToString("0.#", Cultura),
            _ => v.ToString("0.00", Cultura),
        };
    }

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Il numero dietro <see cref="Valore"/> (sanità in percentuale 0–100); null senza dati.</summary>
    public static double? ValoreNumerico(ParametroIndice parametro, IReadOnlyCollection<RigaIndiceParametroDto> righe)
    {
        static double? Media(IEnumerable<double?> valori)
        {
            var presenti = valori.Where(v => v is not null).Select(v => v!.Value).ToList();
            return presenti.Count > 0 ? presenti.Average() : null;
        }

        return parametro.Id.ToUpperInvariant() switch
        {
            "REGIME IDRICO" => Media(righe.Select(r => r.Max)),
            "AMPELOPATIE" => Media(righe.Select(r => r.Max)) is { } danno ? 100 - danno : null,
            "BIODIVERSITA" or "ETA VIGNETO" => Media(righe.Select(r => r.Valore)),
            _ => Media(righe.Select(r => r.Avg)),
        };
    }

    /// <summary>Classi di vigoria del foglio «storico - vigoria» di Qlik (scala 1–10).</summary>
    public static string ClasseVigoria(double v) => v switch
    {
        10 => "Eccessiva",
        >= 8.2 => "Molto alta",
        >= 7 => "Alta",
        >= 5.5 => "Medio/alta",
        >= 4 => "Media",
        >= 2.5 => "Medio/bassa",
        >= 1 => "Bassa",
        _ => "Molto bassa",
    };

    /// <summary>Compattezza del grappolo, 1 molto compatto … 6 molto spargolo (sotto 1: nessuna classe, come Qlik).</summary>
    public static string? ClasseMorfologia(double v) => v switch
    {
        6 => "Molto spargolo",
        >= 5 => "Spargolo",
        >= 4 => "Medio spargolo",
        >= 3 => "Medio compatto",
        >= 2 => "Compatto",
        >= 1 => "Molto compatto",
        _ => null,
    };

    /// <summary>Stress idrico massimo dell'anno, 0 assente … 4 molto severo.</summary>
    public static string? ClasseStressIdrico(double v) => v switch
    {
        4 => "Molto severo",
        >= 3 => "Severo",
        >= 2 => "Medio",
        >= 1 => "Lieve",
        >= 0 => "Assente",
        _ => null,
    };

    /// <summary>
    /// Storico di un parametro (o dell'indice, con <paramref name="parametro"/> null) per vigneto: una cella
    /// per anno, come le pivot di Qlik. L'indice dello storico conta solo i vigneti con 9 parametri.
    /// </summary>
    public static IReadOnlyDictionary<(int Id, int Anno), (double? Numero, string? Testo)> Storico(
        IReadOnlyCollection<RigaIndiceParametroDto> righe, ParametroIndice? parametro)
    {
        if (parametro is null)
        {
            return Indici(righe).Where(i => i.Completo)
                .ToDictionary(i => (i.Id, i.Anno), i => ((double?)i.Punteggio, (string?)i.Punteggio.ToString("0.0", Cultura)));
        }

        return righe.Where(r => string.Equals(r.IdParametro, parametro.Id, StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => (Chiave(r), r.Anno))
            .ToDictionary(g => g.Key, g =>
            {
                var lista = g.ToList();
                return (ValoreNumerico(parametro, lista), Valore(parametro, lista));
            });
    }
}
