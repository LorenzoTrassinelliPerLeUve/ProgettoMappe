using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Una serie di valori per vigneto (barre del report): chiave del nome in <see cref="TestiReport"/> o nome dai dati.</summary>
public sealed record SerieVigneti(string Nome, IReadOnlyDictionary<int, double> PerVigneto);

/// <summary>Una serie per settimana (linee del report).</summary>
public sealed record SerieSettimane(string Nome, IReadOnlyDictionary<int, double> PerSettimana);

/// <summary>Il cruscotto del foglio 26.</summary>
public sealed record CruscottoRilievi(IReadOnlyList<(string Tipo, int Rilievi)> PerTipo, int Rilievi, double Organi, int Monitorati,
    int Totale);

/// <summary>Il foglio 29 «Mal dell'esca»: KPI e diffusione per vigneto di ogni valore della collezione.</summary>
public sealed record EscaAnno(int Rilievi, double PianteControllate, IReadOnlyDictionary<int, double> OrganiPerValore,
    IReadOnlyList<SerieVigneti> Diffusione);

/// <summary>Una generazione delle tignole del foglio 31: contachilometri e quota di superficie controllata.</summary>
public sealed record GenerazioneTignole(int Numero, double? Valore, double? QuotaControllata, ScalaQuadrante Scala);

/// <summary>Una fase fenologica del foglio 32: serie per vitigno e anno, KPI dell'annata, annata −1, 5 e 10 anni.</summary>
public sealed record FaseAnnate(int Fase, IReadOnlyList<(string Vitigno, IReadOnlyDictionary<int, double> PerAnno)> Vitigni,
    double? Annata, double? AnnataPrima, double? Media5, double? Media10);

/// <summary>
/// Sezione X «Analisi sanità delle uve e difesa» (fogli 26–36 e 71 di Qlik, task #203), dagli schemi del principale:
/// <list type="bullet">
/// <item>danno su foglie (organi 1, 3, 4, 10) e grappoli (2, 5) per vigneto = massimo di <c>PercIntensita</c> dell'anno
///   (lo S03 di Qlik pesa per la superficie, ma con la dimensione vigneto il peso si semplifica), in frazione, negativo → 0;
///   acinellatura (168) senza filtro sull'organo;</item>
/// <item>mal dell'esca: organi con quel valore / organi controllati dei rilievi di tipo 9 (collezione già sommata dal dba);</item>
/// <item>tignole: catture medie per settimana e tipo (148, 149, 153, 155); nidi per generazione = media dei vigneti del
///   massimo di <c>PercDiffusione</c>, generazioni 1310/410, 1320/420, 1330/430;</item>
/// <item>fenologia: giorno medio per vigneto e anno (dallo snapshot), media dei vigneti per vitigno; KPI 5 e 10 anni sulle
///   coppie vigneto-anno; confronto BBCH dei rilievi per settimana (foglie 10–25 nelle settimane 5–25, grappoli nelle 15–40).</item>
/// </list>
/// La settimana è quella di Qlik (<see cref="CalcoloStatoIdricoReport.Settimana"/>).
/// </summary>
public static class CalcoloSanitaReport
{
    public static readonly IReadOnlyList<int> OrganiFoglia = [1, 3, 4, 10];
    public static readonly IReadOnlyList<int> OrganiGrappolo = [2, 5];
    public static readonly IReadOnlyList<int> TipiFoglia = [12, 11, 73, 86, 84, 32, 30, 29];
    public static readonly IReadOnlyList<int> TipiGrappolo = [12, 11, 73, 86, 84, 32, 30, 29, 13, 168];
    public static readonly IReadOnlyList<int> ValoriEsca = [100, 50, 10, 60, 40, 30];
    public static readonly IReadOnlyList<int> ValoriEscaKpi = [0, 10, 30, 40, 50, 60, 100];
    public static readonly IReadOnlyList<int> TipiVoli = [148, 149, 153, 155];
    public const int Acinellatura = 168;
    public const int Tignole = 13;
    public const int Esca = 9;

    private static readonly HashSet<int> BbchGrappoli =
        [53, 55, 57, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 73, 75, 77, 79, 80, 81, 82, 83, 84, 85];

    /// <summary>Le tre generazioni: codici di <c>IdEtaEpocaGenerazione</c> e fasce del contachilometri (0–100).</summary>
    public static readonly IReadOnlyList<(int Numero, int[] Codici, ScalaQuadrante Scala)> Generazioni =
    [
        (1, [1310, 410], ScalaTignole(20, 45.4, 75.4)),
        (2, [1320, 420], ScalaTignole(5, 15, 20)),
        (3, [1330, 430], ScalaTignole(1, 4, 10)),
    ];

    private static ScalaQuadrante ScalaTignole(double a, double b, double c) => new(0, 100,
        [new(0, a, "#46c646"), new(a, b, "#ffcf02"), new(b, c, "#f93f17"), new(c, 100, "#8e477d")]);

    public static CruscottoRilievi Cruscotto(CampoReportFineStagioneDto campo, int anno)
    {
        var sanita = campo.Sanita;
        var conteggi = sanita?.Conteggi.Where(c => c.Anno == anno).ToList() ?? [];
        // I conteggi arrivano per vigneto (un rilievo è di un vigneto solo): si sommano per tipo.
        var perTipo = conteggi.GroupBy(c => c.TipoRilievo).Select(g => (Tipo: g.Key, Rilievi: g.Sum(c => c.Rilievi)))
            .OrderByDescending(c => c.Rilievi)
            .Select(c => (sanita!.NomiTipi.TryGetValue(c.Tipo, out var n) && n.Length > 0 ? n : $"{c.Tipo}", c.Rilievi))
            .ToList();
        return new CruscottoRilievi(perTipo, conteggi.Sum(c => c.Rilievi), conteggi.Sum(c => c.Organi),
            sanita?.VignetiMonitorati.GetValueOrDefault(anno) ?? 0, campo.Vigneti.Count);
    }

    /// <summary>Fogli 27–28: una serie per tipo (Id del tipo come nome), valore per vigneto in frazione 0–1.</summary>
    public static IReadOnlyList<(int Tipo, SerieVigneti Serie)> Danno(CampoReportFineStagioneDto campo, int anno, bool grappolo)
    {
        var organi = grappolo ? OrganiGrappolo : OrganiFoglia;
        var rilievi = (campo.Rilievi ?? []).Where(r => r.Anno == anno && r.PercIntensita is not null).ToList();
        return (grappolo ? TipiGrappolo : TipiFoglia).Select(tipo =>
        {
            var valori = rilievi.Where(r => r.TipoRilievo == tipo && (tipo == Acinellatura || r.Organo is { } o && organi.Contains(o)))
                .GroupBy(r => r.IdVigneto)
                .ToDictionary(g => g.Key, g => Math.Max(0, g.Max(r => r.PercIntensita!.Value)) / 100);
            return (tipo, new SerieVigneti($"{tipo}", valori));
        }).ToList();
    }

    public static EscaAnno MalDellEsca(CampoReportFineStagioneDto campo, int anno)
    {
        var rilievi = (campo.Rilievi ?? []).Where(r => r.TipoRilievo == Esca && r.Anno == anno).ToList();
        var organiPerVigneto = rilievi.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.Sum(r => r.NrOrgani ?? 0));
        var collezione = (campo.Collezione ?? []).Where(c => c.Anno == anno).ToList();
        var diffusione = ValoriEsca.Select(v => new SerieVigneti($"{v}", collezione.Where(c => c.Valore == v)
                .GroupBy(c => c.IdVigneto)
                .Where(g => organiPerVigneto.GetValueOrDefault(g.Key) > 0)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.NumOrgani) / organiPerVigneto[g.Key])))
            .ToList();
        return new EscaAnno(rilievi.Count, organiPerVigneto.Values.Sum(),
            ValoriEscaKpi.ToDictionary(v => v, v => collezione.Where(c => c.Valore == v).Sum(c => c.NumOrgani)), diffusione);
    }

    /// <summary>Foglio 30: catture per trappola, una linea per tipo di monitoraggio.</summary>
    public static IReadOnlyList<(int Tipo, SerieSettimane Serie)> VoliTignole(CampoReportFineStagioneDto campo, int anno) =>
        (campo.Rilievi ?? []).Where(r => r.Anno == anno && TipiVoli.Contains(r.TipoRilievo) && r.CatturePerTrappola is not null)
            .GroupBy(r => r.TipoRilievo).OrderBy(g => TipiVoli.ToList().IndexOf(g.Key))
            .Select(g => (g.Key, new SerieSettimane($"{g.Key}", MediaVignetiPerSettimana(g, r => r.CatturePerTrappola!.Value))))
            .ToList();

    /// <summary>Foglio 31: le tre generazioni (contachilometri e quota di superficie dei vigneti controllati).</summary>
    public static IReadOnlyList<GenerazioneTignole> NidiTignole(CampoReportFineStagioneDto campo, int anno)
    {
        var superficieTotale = campo.Vigneti.Sum(v => v.Superficie ?? 0);
        var superfici = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Superficie ?? 0);
        return Generazioni.Select(g =>
        {
            var righe = (campo.Rilievi ?? []).Where(r => r.Anno == anno && r.EtaEpoca is { } e && g.Codici.Contains(e)).ToList();
            var massimi = righe.Where(r => r.PercDiffusione is not null).GroupBy(r => r.IdVigneto)
                .Select(v => v.Max(r => r.PercDiffusione!.Value)).ToList();
            double? valore = massimi.Count == 0 ? null : Math.Max(0, massimi.Average());
            var controllata = righe.Select(r => r.IdVigneto).Distinct().Sum(id => superfici.GetValueOrDefault(id));
            return new GenerazioneTignole(g.Numero, valore, superficieTotale > 0 ? controllata / superficieTotale : null, g.Scala);
        }).ToList();
    }

    /// <summary>Foglio 31: diffusione massima delle tignole (tipo 13) per vigneto nella stagione.</summary>
    public static IReadOnlyDictionary<int, double> TignoleMassime(CampoReportFineStagioneDto campo, int anno) =>
        (campo.Rilievi ?? []).Where(r => r.Anno == anno && r.TipoRilievo == Tignole && r.PercDiffusione is not null)
            .GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.Max(r => r.PercDiffusione!.Value));

    /// <summary>Foglio 71: i rilievi dei nidi larvali dell'annata (con una generazione), dal più recente.</summary>
    public static IReadOnlyList<RilievoFineStagioneDto> RilieviNidi(CampoReportFineStagioneDto campo, int anno) =>
        (campo.Rilievi ?? []).Where(r => r.Anno == anno && r.EtaEpoca is { } e && Generazioni.Any(g => g.Codici.Contains(e)))
            .OrderByDescending(r => r.Data).ToList();

    /// <summary>Foglio 32: le quattro fasi, serie per vitigno e anno fino all'annata.</summary>
    public static IReadOnlyList<FaseAnnate> Fenologia(CampoReportFineStagioneDto campo, int annata)
    {
        var vitigni = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Vitigno ?? "—");
        var fasi = (campo.Sanita?.Fasi ?? []).Where(f => f.Anno <= annata).ToList();
        return Enumerable.Range(1, 4).Select(fase =>
        {
            var diFase = fasi.Where(f => f.Fase == fase).ToList();
            var perVitigno = diFase.GroupBy(f => vitigni.GetValueOrDefault(f.IdVigneto, "—"))
                .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(g => (g.Key, (IReadOnlyDictionary<int, double>)g.GroupBy(f => f.Anno).ToDictionary(a => a.Key, a => a.Average(f => f.Giorno))))
                .ToList();
            double? Media(int da, int a) => diFase.Where(f => f.Anno >= da && f.Anno <= a).Select(f => f.Giorno).ToList()
                is { Count: > 0 } l ? l.Average() : null;
            return new FaseAnnate(fase, perVitigno, Media(annata, annata), Media(annata - 1, annata - 1),
                Media(annata - 5, annata - 1), Media(annata - 10, annata - 1));
        }).ToList();
    }

    /// <summary>Fogli 33–34: BBCH medio per settimana e vitigno nell'annata.</summary>
    public static IReadOnlyList<SerieSettimane> BbchPerVitigno(CampoReportFineStagioneDto campo, int annata, bool grappoli)
    {
        var vitigni = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Vitigno ?? "—");
        return RilieviBbch(campo, grappoli).Where(r => r.Anno == annata)
            .GroupBy(r => vitigni.GetValueOrDefault(r.IdVigneto, "—"))
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new SerieSettimane(g.Key, MediaVignetiPerSettimana(g, r => r.Bbch!.Value)))
            .ToList();
    }

    /// <summary>Fogli 35–36: annata, annata prima e media dei 5 anni prima (coppie vigneto-anno per settimana).</summary>
    public static IReadOnlyList<SerieSettimane> BbchStorico(CampoReportFineStagioneDto campo, int annata, bool grappoli)
    {
        var righe = RilieviBbch(campo, grappoli).ToList();
        SerieSettimane Serie(string nome, int da, int a) => new(nome, righe.Where(r => r.Anno >= da && r.Anno <= a)
            .GroupBy(r => CalcoloStatoIdricoReport.Settimana(r.Data))
            .ToDictionary(s => s.Key, s => s.GroupBy(r => (r.IdVigneto, r.Anno)).Average(v => v.Average(r => (double)r.Bbch!.Value))));
        return [Serie("annata", annata, annata), Serie("annata-prima", annata - 1, annata - 1), Serie("media-5", annata - 5, annata - 1)];
    }

    private static IEnumerable<RilievoFineStagioneDto> RilieviBbch(CampoReportFineStagioneDto campo, bool grappoli) =>
        (campo.Rilievi ?? []).Where(r => r.Bbch is { } b && (grappoli ? BbchGrappoli.Contains(b) : b is >= 10 and <= 25))
            .Where(r => CalcoloStatoIdricoReport.Settimana(r.Data) is var s && (grappoli ? s is >= 15 and <= 40 : s is >= 5 and <= 25));

    private static IReadOnlyDictionary<int, double> MediaVignetiPerSettimana(IEnumerable<RilievoFineStagioneDto> righe,
        Func<RilievoFineStagioneDto, double> valore) =>
        righe.GroupBy(r => CalcoloStatoIdricoReport.Settimana(r.Data))
            .ToDictionary(s => s.Key, s => s.GroupBy(r => r.IdVigneto).Average(v => v.Average(valore)));

    /// <summary>
    /// Il giorno dell'anno come «GG/MM», come <c>Day_Ref = DOY + Yearend(Today())</c> di Qlik: Yearend è il 31/12 alle
    /// 23:59:59, quindi un giorno medio 89,2 diventa il 31/03 e un 89 esatto il 30/03 (si mostra il giorno della data).
    /// </summary>
    public static string GiornoMese(double giorno, int anno) =>
        new DateTime(anno, 12, 31).AddDays(Math.Floor(giorno + 0.99999)).ToString("dd/MM");
}
