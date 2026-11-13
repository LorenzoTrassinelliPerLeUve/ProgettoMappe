using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>Un'avversità delle slide sanità: tipo/i di rilievo, organi (null = tutti), chiave del nome.</summary>
public sealed record AvversitaSettimanale(string Chiave, IReadOnlyList<int> Tipi, IReadOnlyList<int>? Organi);

/// <summary>I KPI di un'avversità: intensità e diffusione (S03, frazioni), vigneti senza danno (S22), copertura (S21).</summary>
public sealed record KpiAvversita(AvversitaSettimanale Avversita, double Intensita, double Diffusione, double? SenzaDanno, double Copertura);

/// <summary>Cruscotto (slide 2): rilievi per tipo, totali e vigneti monitorati.</summary>
public sealed record CruscottoSettimanale(IReadOnlyList<(int Tipo, int Rilievi)> PerTipo, int Rilievi, double Organi, int Monitorati,
    int VignetiTotali);

/// <summary>Una linea per settimana: nome della serie e valore per settimana (null = nessun dato).</summary>
public sealed record SerieSettimane(string Nome, IReadOnlyList<double?> Valori);

/// <summary>
/// Le formule delle slide del report settimanale (task #213, schemi in docs/specifiche/report-settimanale/schemi.md),
/// sui rilievi già filtrati con <see cref="SettimanaReport.Contiene"/>. Superficie = <c>Vigneto.Superficie</c> grezza;
/// il denominatore <c>sum(Aggr(avg(Superficie),IdVigneto))</c> è su tutti i vigneti dell'azienda.
/// </summary>
public static class CalcoloSettimanale
{
    public static readonly IReadOnlyList<int> OrganiGrappolo = [2, 5];
    public static readonly IReadOnlyList<int> OrganiFoglia = [1, 3, 4, 10];

    /// <summary>Slide 3 «Sanità Grappoli KPI», nell'ordine dei riquadri di Qlik.</summary>
    public static readonly IReadOnlyList<AvversitaSettimanale> Grappolo =
    [
        new("avv.peronospora", [12], OrganiGrappolo), new("avv.oidio", [11], OrganiGrappolo),
        new("avv.botrite", [73], OrganiGrappolo), new("avv.marciumi", [86, 131], OrganiGrappolo),
        new("avv.blackrot", [84], OrganiGrappolo), new("avv.scottature", [32], OrganiGrappolo),
        new("avv.fitotossicita", [30], OrganiGrappolo), new("avv.grandine", [29], OrganiGrappolo),
        new("avv.gelata", [28], null), new("avv.tignole", [13], OrganiGrappolo),
        new("avv.filatura", [164], null), new("avv.acinellatura", [168], null),
    ];

    /// <summary>Slide 5 «Sanità Foglia KPI».</summary>
    public static readonly IReadOnlyList<AvversitaSettimanale> Foglia =
    [
        new("avv.peronospora", [12], OrganiFoglia), new("avv.oidio", [11], OrganiFoglia),
        new("avv.botrite", [73], OrganiFoglia), new("avv.blackrot", [84], OrganiFoglia),
        new("avv.scottature", [32], OrganiFoglia), new("avv.fitotossicita", [30], OrganiFoglia),
        new("avv.grandine", [29], OrganiFoglia),
    ];

    /// <summary>Fasce dei contachilometri di Qlik.</summary>
    public static readonly ScalaQuadrante ScalaVigoria = Scala(0, 10, [0.86, 2.24, 3.325, 5.229, 6.7],
        ["#f8981d", "#ffcf02", "#46c646", "#276e27", "#ffcf02", "#f8981d"]);

    public static readonly ScalaQuadrante ScalaStatoIdrico = Scala(0, 4, [0.5, 1.04, 2.18, 3.175, 3.875],
        ["#ffcf02", "#46c646", "#276e27", "#276e27", "#276e27", "#ffcf02"]);

    /// <summary>Tignole per generazione (slide 16): codici di <c>idEtaEpocaGenerazione</c> e fasce del contachilometri.</summary>
    public static readonly IReadOnlyList<(string Chiave, int[] Codici, ScalaQuadrante Scala)> Generazioni =
    [
        ("tignole.prima", [1310, 410], Scala(0, 100, [20, 45.4, 75.4], Colori4)),
        ("tignole.seconda", [1320, 420], Scala(0, 100, [5, 15, 20], Colori4)),
        ("tignole.terza", [1330, 430], Scala(0, 100, [1, 4, 10], Colori4)),
    ];

    public static readonly IReadOnlyList<int> TipiTrappole = [148, 149, 153, 155];

    /// <summary>BBCH delle linee «Foglie» e «Grappoli» (slide 8–11).</summary>
    public static readonly IReadOnlySet<int> BbchFoglie = Enumerable.Range(10, 16).ToHashSet();

    public static readonly IReadOnlySet<int> BbchGrappoli =
        new HashSet<int> { 53, 55, 57, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 72, 73, 75, 77, 79, 80, 81, 82, 83, 84, 85 };

    private static string[] Colori4 => ["#46c646", "#ffcf02", "#f93f17", "#8e477d"];

    private static ScalaQuadrante Scala(double min, double max, double[] limiti, string[] colori)
    {
        var bordi = new[] { min }.Concat(limiti).Append(max).ToArray();
        return new ScalaQuadrante(min, max, Enumerable.Range(0, colori.Length).Select(i => new FasciaQuadrante(bordi[i], bordi[i + 1], colori[i])).ToList());
    }

    private static double Totale(IReadOnlyDictionary<int, double?> superfici) => superfici.Values.Sum(s => s is > 0 ? s.Value : 0);

    private static double Sup(IReadOnlyDictionary<int, double?> superfici, int vigneto) =>
        superfici.TryGetValue(vigneto, out var s) && s is > 0 ? s.Value : 0;

    private static bool Organo(RilievoSettimanaDto r, IReadOnlyList<int>? organi) => organi is null || r.Organo is { } o && organi.Contains(o);

    /// <summary>S02 vigoria: la classe di Qlik (soglie 10 / 8,8 / 7,5 / 6,2 / 4,9 / 3,6 / 2,3 / 1).</summary>
    public static string? ClasseVigoria(double? v) => v switch
    {
        null => null,
        >= 10 => "vigoria.eccessiva",
        >= 8.8 => "vigoria.molto-alta",
        >= 7.5 => "vigoria.alta",
        >= 6.2 => "vigoria.medio-alta",
        >= 4.9 => "vigoria.media-classe",
        >= 3.6 => "vigoria.medio-bassa",
        >= 2.3 => "vigoria.bassa",
        >= 1 => "vigoria.molto-bassa",
        _ => null,
    };

    /// <summary>
    /// La chiave del rilievo come <c>Count(distinct IdRilievo)</c> di Qlik, che legge IdRilievo come timestamp al
    /// millesimo («2024-09-16 00:00:00.000001» e «…000002» sono lo stesso rilievo); senza IdRilievo vigneto, ora e tipo.
    /// </summary>
    private static string Chiave(RilievoSettimanaDto r) =>
        r.IdRilievo is { } id ? id[..Math.Min(id.Length, 23)] : $"{r.IdVigneto}|{r.Data:O}|{r.TipoRilievo}";

    /// <summary>Slide 2: rilievi distinti (IdRilievo), organi controllati, vigneti con rilievi su quelli dell'azienda.</summary>
    public static CruscottoSettimanale Cruscotto(IReadOnlyList<RilievoSettimanaDto> sel, int vignetiTotali)
    {
        var distinti = sel.GroupBy(Chiave).Select(g => g.First()).ToList();
        return new CruscottoSettimanale(
            distinti.GroupBy(r => r.TipoRilievo).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ThenBy(t => t.Key).ToList(),
            distinti.Count, sel.Sum(r => r.NrOrgani ?? 0), sel.Select(r => r.IdVigneto).Distinct().Count(), vignetiTotali);
    }

    /// <summary>Le avversità che il KPI «Sanità Grappolo» toglie da 1: botrite, marciumi, oidio (intensità sul grappolo).</summary>
    public static readonly IReadOnlyList<int> TipiSanitaGrappolo = [73, 86, 11];

    /// <summary>Slide 3 «Sanità Grappolo»: 1 − Σ intensità pesate (S03) di botrite, marciumi e oidio.</summary>
    public static double SanitaGrappolo(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici) =>
        1 - TipiSanitaGrappolo.Sum(t => Pesata(sel, superfici, [t], OrganiGrappolo, r => r.PercIntensita));

    /// <summary>
    /// Slide 3 «Riduzione ponderata della produzione» (S23): Σ max(PercDanno) × superficie / superficie dei vigneti con un
    /// danno registrato / 100, sugli anni dati (Qlik non filtra l'anno: tutti gli anni dello snapshot). Null senza danni.
    /// </summary>
    public static double? Danno(IEnumerable<DannoSettimanaDto> danni, IReadOnlyDictionary<int, double?> superfici, Func<int, bool> anni)
    {
        var perVigneto = danni.Where(d => anni(d.Anno) && superfici.ContainsKey(d.IdVigneto)).GroupBy(d => d.IdVigneto)
            .Select(g => (Sup: Sup(superfici, g.Key), Max: g.Max(d => d.PercDanno))).ToList();
        var totale = perVigneto.Sum(v => v.Sup);
        return totale <= 0 ? null : perVigneto.Sum(v => v.Max * v.Sup) / totale / 100;
    }

    /// <summary>
    /// I tre KPI in testa alla slide 3 in una riga: sanità del grappolo, riduzione ponderata della produzione (danni
    /// dell'anno del report: Qlik li prende di tutti gli anni, differenza voluta) e nidi ogni 100 grappoli.
    /// </summary>
    public static string RiassuntoGrappolo(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici,
        IEnumerable<DannoSettimanaDto> danni, int anno, System.Globalization.CultureInfo c, Func<string, object[], string> t) =>
        string.Join(" · ",
            t("kpi.sanita-grappolo", [SanitaGrappolo(sel, superfici).ToString("P1", c)]),
            t("kpi.riduzione", [Danno(danni, superfici, a => a == anno)?.ToString("P1", c) ?? "—"]),
            t("kpi.nidi", [Nidi(sel, superfici).ToString("N1", c)]));

    /// <summary>I controlli del dba per la settimana scelta (<c>RS_RiepilogoSettimana</c>); null se non ci sono.</summary>
    public static RiepilogoSettimanaDto? Controlli(IEnumerable<RiepilogoSettimanaDto> riepilogo, SettimanaReport settimana) =>
        riepilogo.FirstOrDefault(r => r.Anno == settimana.Anno && r.Settimana == settimana.Numero);

    /// <summary>
    /// Le righe dei controlli in cima al report (come il fine stagione): settimana senza rilievi, vigneti rilevati sugli
    /// attivi, punti meteo e giorni mancanti o in previsione (<c>RS_RiepilogoSettimana</c> del dba).
    /// </summary>
    public static IReadOnlyList<string> TestiControlli(IEnumerable<RiepilogoSettimanaDto> riepilogo, SettimanaReport settimana,
        int rilieviSettimana, Func<string, object[], string> t)
    {
        var righe = new List<string>();
        if (rilieviSettimana == 0)
        {
            righe.Add(t("controlli.vuota", []));
        }
        if (Controlli(riepilogo, settimana) is { } c)
        {
            righe.Add(t("controlli.rilievi", [c.VignetiRilevati, c.VignetiAttivi, c.Rilievi]));
            righe.Add(c.PuntiMeteo == 0 ? t("controlli.nessuno", [])
                : t("controlli.meteo", [c.PuntiMeteo, c.PuntiMeteoCompleti, c.GiorniMeteoMancanti, c.GiorniMeteoPrevisione]));
        }
        return righe;
    }

    /// <summary>Slide 3 «Numero di nidi / 100 grappoli» (S23): Σ max(NrFormeMobiliNidi) × superficie / superficie totale × 100.</summary>
    public static double Nidi(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici) =>
        Pesata(sel, superfici, [13], OrganiGrappolo, r => r.NrFormeMobiliNidi) * 10000;

    /// <summary>Slide 13: lunghezza media dei germogli (tipo 113) per vigneto nei rilievi dati (media per vigneto e settimana, poi media).</summary>
    public static IReadOnlyDictionary<int, double> GermogliPerVigneto(IEnumerable<RilievoSettimanaDto> rilievi) =>
        rilievi.Where(r => r.LunghezzaGermogli is not null).GroupBy(r => r.IdVigneto)
            .ToDictionary(g => g.Key, g => g.GroupBy(r => Settimana(r)).Average(s => s.Average(r => r.LunghezzaGermogli!.Value)));

    /// <summary>Slide 13 «Confronto storico»: per anno la media sui vigneti e settimane (aggr IdVigneto, Week_germoglio, Anno).</summary>
    public static IReadOnlyDictionary<int, double> GermogliPerAnno(IEnumerable<RilievoSettimanaDto> rilievi) =>
        rilievi.Where(r => r.LunghezzaGermogli is not null).GroupBy(r => r.Data.Year)
            .ToDictionary(g => g.Key, g => g.GroupBy(r => (r.IdVigneto, Settimana(r))).Average(s => s.Average(r => r.LunghezzaGermogli!.Value)));

    /// <summary>La <c>Week</c> di Qlik del rilievo: quella del dba se c'è, altrimenti dalla tabella DOY_WEEK.</summary>
    public static int Settimana(RilievoSettimanaDto r) => r.Settimana ?? SettimanaQlik.Di(r.Data);

    /// <summary>Meteo per settimana (RS_MeteoSettimana): pioggia e temperature medie sui punti meteo dell'azienda.</summary>
    public static (IReadOnlyList<double?> Pioggia, IReadOnlyList<double?> TMin, IReadOnlyList<double?> TMedia, IReadOnlyList<double?> TMax)
        Meteo(IEnumerable<MeteoSettimanaDto> meteo, int anno, IReadOnlyList<int> settimane)
    {
        var perSettimana = meteo.Where(m => m.Anno == anno && m.GiorniConDati > 0).ToLookup(m => m.Settimana);
        double? MediaDi(int s, Func<MeteoSettimanaDto, double?> v) =>
            perSettimana[s].Select(v).Where(x => x is not null).Select(x => x!.Value).DefaultIfEmpty(double.NaN).Average() is var x && double.IsNaN(x) ? null : x;
        return (settimane.Select(s => MediaDi(s, m => m.Pioggia)).ToList(), settimane.Select(s => MediaDi(s, m => m.TMin)).ToList(),
            settimane.Select(s => MediaDi(s, m => m.TMedia)).ToList(), settimane.Select(s => MediaDi(s, m => m.TMax)).ToList());
    }

    /// <summary>S03: Σ sui tipi di Σ_vigneti max(misura) × superficie / superficie totale / 100 (solo addendi &gt; 0).</summary>
    public static double Pesata(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici,
        IReadOnlyList<int> tipi, IReadOnlyList<int>? organi, Func<RilievoSettimanaDto, double?> misura)
    {
        var totale = Totale(superfici);
        if (totale <= 0)
        {
            return 0;
        }
        return tipi.Sum(tipo =>
        {
            var somma = sel.Where(r => r.TipoRilievo == tipo && Organo(r, organi) && misura(r) is not null)
                .GroupBy(r => r.IdVigneto).Sum(g => g.Max(misura)!.Value * Sup(superfici, g.Key));
            var valore = somma / totale / 100;
            return valore > 0 ? valore : 0;
        });
    }

    /// <summary>S21: superficie dei vigneti con almeno un rilievo del tipo / superficie totale.</summary>
    public static double Copertura(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici,
        Func<RilievoSettimanaDto, bool> filtro)
    {
        var totale = Totale(superfici);
        return totale <= 0 ? 0 : sel.Where(filtro).Select(r => r.IdVigneto).Distinct().Sum(v => Sup(superfici, v)) / totale;
    }

    /// <summary>S22: rilievi senza avversità (presenza = 0, sugli organi) / rilievi del tipo; null senza rilievi del tipo o senza il campo.</summary>
    public static double? SenzaDanno(IReadOnlyList<RilievoSettimanaDto> sel, int tipo, IReadOnlyList<int>? organi)
    {
        var righe = sel.Where(r => r.TipoRilievo == tipo).ToList();
        if (righe.Count == 0 || righe.All(r => r.PresenzaAvversita is null))
        {
            return null;
        }
        return (double)righe.Count(r => Organo(r, organi) && r.PresenzaAvversita == 0) / righe.Count;
    }

    public static IReadOnlyList<KpiAvversita> Sanita(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, double?> superfici,
        IReadOnlyList<AvversitaSettimanale> elenco) =>
        elenco.Select(a => new KpiAvversita(a,
            Pesata(sel, superfici, a.Tipi, a.Organi, r => r.PercIntensita),
            Pesata(sel, superfici, a.Tipi, a.Organi, r => r.PercDiffusione),
            SenzaDanno(sel, a.Tipi[0], a.Organi),
            Copertura(sel, superfici, r => r.TipoRilievo == a.Tipi[0]))).ToList();

    /// <summary>S10 vigoria: media sui vigneti della media del punteggio nell'anno; null senza dati.</summary>
    public static double? Vigoria(IEnumerable<VigoriaDto> vigoria, int anno) =>
        vigoria.Where(v => v.Anno == anno && v.Punteggio is not null).GroupBy(v => v.IdVigneto)
            .Select(g => g.Average(v => v.Punteggio!.Value)).DefaultIfEmpty(double.NaN).Average() is var m && double.IsNaN(m) ? null : m;

    /// <summary>S12: media sui vigneti del massimo dello stato idrico (tipo 146, maggio–settembre) dell'anno.</summary>
    public static double? StatoIdrico(IEnumerable<RilievoSettimanaDto> rilievi, int anno)
    {
        var valori = rilievi.Where(r => r.TipoRilievo == 146 && r.Data.Year == anno && r.Data.Month is >= 5 and <= 9 && r.StatoIdrico is not null)
            .GroupBy(r => r.IdVigneto).Select(g => (double)g.Max(r => r.StatoIdrico!.Value)).ToList();
        return valori.Count == 0 ? null : valori.Average();
    }

    /// <summary>Stato idrico per vigna e settimana (maggio–settembre), × −1 come la linea di Qlik.</summary>
    public static IReadOnlyList<SerieSettimane> StatoIdricoPerVigna(IReadOnlyList<RilievoSettimanaDto> sel,
        IReadOnlyDictionary<int, string?> vignaDi, IReadOnlyList<int> settimane) =>
        sel.Where(r => r.TipoRilievo == 146 && r.Data.Month is >= 5 and <= 9 && r.StatoIdrico is not null)
            .GroupBy(r => vignaDi.GetValueOrDefault(r.IdVigneto) ?? "—").OrderBy(g => g.Key)
            .Select(g => new SerieSettimane(g.Key, settimane.Select(s => Media(g.Where(r => Settimana(r) == s),
                r => r.StatoIdrico) * -1).ToList())).ToList();

    /// <summary>Catture per trappola: per tipo di monitoraggio e settimana, media sui vigneti della media.</summary>
    public static IReadOnlyList<(int Tipo, IReadOnlyList<double?> Valori)> Catture(IReadOnlyList<RilievoSettimanaDto> sel,
        IReadOnlyList<int> settimane) =>
        sel.Where(r => TipiTrappole.Contains(r.TipoRilievo) && r.CatturePerTrappola is not null).GroupBy(r => r.TipoRilievo).OrderBy(g => g.Key)
            .Select(g => (g.Key, (IReadOnlyList<double?>)settimane.Select(s => Media(g.Where(r => Settimana(r) == s),
                r => r.CatturePerTrappola)).ToList())).ToList();

    /// <summary>S18b: media sui vigneti del massimo della diffusione della generazione (0 senza dati) e copertura.</summary>
    public static (double Valore, double Copertura) Generazione(IReadOnlyList<RilievoSettimanaDto> sel,
        IReadOnlyDictionary<int, double?> superfici, int[] codici)
    {
        var righe = sel.Where(r => r.EtaEpoca is { } e && codici.Contains(e)).ToList();
        var massimi = righe.Where(r => r.PercDiffusione is not null).GroupBy(r => r.IdVigneto).Select(g => g.Max(r => r.PercDiffusione!.Value)).ToList();
        var valore = massimi.Count == 0 ? 0 : massimi.Average();
        return (valore > 0 ? valore : 0, Copertura(righe, superfici, _ => true));
    }

    /// <summary>Diffusione massima delle tignole (tipo 13) per vigneto nella stagione.</summary>
    public static IReadOnlyDictionary<int, double> DiffusioneTignole(IReadOnlyList<RilievoSettimanaDto> sel) =>
        sel.Where(r => r.TipoRilievo == 13 && r.PercDiffusione is not null).GroupBy(r => r.IdVigneto)
            .ToDictionary(g => g.Key, g => g.Max(r => r.PercDiffusione!.Value));

    /// <summary>BBCH medio per settimana (media per vigneto e settimana, poi sui vigneti) negli anni dati.</summary>
    public static IReadOnlyList<double?> BbchPerSettimana(IEnumerable<RilievoSettimanaDto> rilievi, IReadOnlySet<int> codici,
        Func<int, bool> anni, IReadOnlyList<int> settimane)
    {
        var righe = rilievi.Where(r => r.Bbch is { } b && codici.Contains(b) && anni(r.Data.Year)).ToList();
        return settimane.Select(s =>
        {
            var perVigneto = righe.Where(r => Settimana(r) == s).GroupBy(r => (r.IdVigneto, r.Data.Year))
                .Select(g => g.Average(r => (double)r.Bbch!.Value)).ToList();
            return perVigneto.Count == 0 ? (double?)null : perVigneto.Average();
        }).ToList();
    }

    /// <summary>Media per vigneto, poi sui vigneti; null senza valori.</summary>
    private static double? Media(IEnumerable<RilievoSettimanaDto> righe, Func<RilievoSettimanaDto, double?> misura)
    {
        var perVigneto = righe.Where(r => misura(r) is not null).GroupBy(r => r.IdVigneto).Select(g => g.Average(r => misura(r)!.Value)).ToList();
        return perVigneto.Count == 0 ? null : perVigneto.Average();
    }

    private static double? Media(IEnumerable<RilievoSettimanaDto> righe, Func<RilievoSettimanaDto, int?> misura) =>
        Media(righe, r => (double?)misura(r));

    /// <summary>
    /// Esca (slide 17, S15): rilievi del tipo 9 (<c>Count(IdRilievo)</c>), piante controllate e righe della collezione per
    /// valore, solo dei rilievi dati (la collezione è legata al rilievo, quindi alla settimana scelta).
    /// </summary>
    public static (int Rilievi, double Piante, IReadOnlyDictionary<int, double> PerValore) Esca(IReadOnlyList<RilievoSettimanaDto> sel,
        IEnumerable<EscaSettimanaDto> collezione)
    {
        var righe = sel.Where(r => r.TipoRilievo == 9).ToList();
        var rilievi = righe.Where(r => r.IdRilievo is not null).Select(r => r.IdRilievo!).ToHashSet();
        return (righe.Count, righe.Sum(r => r.NrOrgani ?? 0),
            collezione.Where(c => rilievi.Contains(c.IdRilievo)).GroupBy(c => c.Valore).ToDictionary(g => g.Key, g => (double)g.Count()));
    }

    /// <summary>Slide 17, grafico per vigneto: righe della collezione per valore / piante controllate del vigneto.</summary>
    public static IReadOnlyDictionary<int, IReadOnlyDictionary<int, double>> EscaPerVigneto(IReadOnlyList<RilievoSettimanaDto> sel,
        IEnumerable<EscaSettimanaDto> collezione)
    {
        var righe = sel.Where(r => r.TipoRilievo == 9 && r.IdRilievo is not null).ToList();
        var vignetoDi = righe.GroupBy(r => r.IdRilievo!).ToDictionary(g => g.Key, g => g.First().IdVigneto);
        var piante = righe.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.Sum(r => r.NrOrgani ?? 0));
        return collezione.Where(c => vignetoDi.ContainsKey(c.IdRilievo)).GroupBy(c => vignetoDi[c.IdRilievo])
            .Where(g => piante.GetValueOrDefault(g.Key) > 0)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<int, double>)g.GroupBy(c => c.Valore)
                .ToDictionary(v => v.Key, v => v.Count() / piante[g.Key]));
    }

    /// <summary>
    /// Slide 7: giorno medio di una fase (1 germogliamento, 2 fioritura, 3 invaiatura, 4 vendemmia; modello BBCH e
    /// vendemmia già ridotti dallo snapshot per vigneto e anno) negli anni dati: media per anno sui vigneti, poi sugli anni.
    /// </summary>
    public static double? GiornoFase(IEnumerable<FaseFenologicaDto> fasi, int fase, Func<int, bool> anni)
    {
        // avg(aggr(…, Anno_BBCH, IdVigneto)) di Qlik: media su tutte le coppie vigneto–anno del periodo.
        var valori = fasi.Where(f => f.Fase == fase && anni(f.Anno)).Select(f => f.Giorno).ToList();
        return valori.Count == 0 ? null : valori.Average();
    }

    /// <summary>Le fasi della slide 7: dal modello giornaliero come Qlik se lo snapshot le ha, altrimenti quelle del fine stagione.</summary>
    public static IReadOnlyList<FaseFenologicaDto> Fasi(RilieviSettimanaDto rilievi, CampoReportFineStagioneDto? campo) =>
        rilievi.GiorniBbch.Count > 0 ? FasiQlik(rilievi.GiorniBbch) : campo?.Sanita?.Fasi ?? [];

    /// <summary>I codici del modello per fase, nell'ordine dell'IF di Qlik: il primo presente nell'anno decide il giorno.</summary>
    public static readonly IReadOnlyDictionary<int, int[]> CascataFasi = new Dictionary<int, int[]>
    {
        [1] = [9, 8, 7], [2] = [68, 67, 66, 65, 64, 63], [3] = [83, 82, 81], [4] = [89],
    };

    /// <summary>
    /// Le fasi della slide 7 come le formule di Qlik: per vigneto e anno il giorno medio del codice più alto della cascata
    /// presente nel modello giornaliero (germogliamento 9, se no 8, se no 7…), la vendemmia dai rilievi con BBCH 89.
    /// </summary>
    public static IReadOnlyList<FaseFenologicaDto> FasiQlik(IEnumerable<GiornoBbchDto> giorni) =>
        giorni.GroupBy(g => (g.IdVigneto, g.Anno)).SelectMany(g => CascataFasi.Select(c =>
                c.Value.Select(codice => g.FirstOrDefault(x => x.Bbch == codice)).FirstOrDefault(x => x is not null) is { } trovato
                    ? new FaseFenologicaDto(g.Key.IdVigneto, g.Key.Anno, c.Key, trovato.Giorno) : null))
            .OfType<FaseFenologicaDto>().ToList();

    /// <summary>Le colonne della slide 7: annata, anno prima, storico 5 e 10 anni (fino all'anno prima).</summary>
    public static IReadOnlyList<(string Chiave, Func<int, bool> Anni)> PeriodiFase(int anno) =>
    [
        ("fase.annata", a => a == anno), ("fase.precedente", a => a == anno - 1),
        ("fase.storico5", a => a >= anno - 5 && a <= anno - 1), ("fase.storico10", a => a >= anno - 10 && a <= anno - 1),
    ];

    /// <summary>Slide 8–9: BBCH per settimana di ogni vitigno nell'anno (media per vigneto e settimana, poi sui vigneti del vitigno).</summary>
    public static IReadOnlyList<SerieSettimane> BbchPerVitigno(IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlySet<int> codici,
        IReadOnlyDictionary<int, string?> vitignoDi, int anno, IReadOnlyList<int> settimane) =>
        sel.Where(r => r.Bbch is { } b && codici.Contains(b) && r.Data.Year == anno)
            .GroupBy(r => vitignoDi.GetValueOrDefault(r.IdVigneto) ?? "—").OrderBy(g => g.Key)
            .Select(g => new SerieSettimane(g.Key, BbchPerSettimana(g, codici, _ => true, settimane))).ToList();

    /// <summary>I valori della collezione dell'esca nell'ordine dei KPI di Qlik.</summary>
    public static readonly IReadOnlyList<int> ValoriEsca = [0, 10, 30, 40, 50, 60, 100];
}
