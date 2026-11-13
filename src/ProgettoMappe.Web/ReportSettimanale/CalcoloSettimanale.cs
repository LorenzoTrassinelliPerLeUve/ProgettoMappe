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

    /// <summary>Slide 2: rilievi distinti (vigneto, ora, tipo), organi controllati, vigneti con rilievi su quelli dell'azienda.</summary>
    public static CruscottoSettimanale Cruscotto(IReadOnlyList<RilievoSettimanaDto> sel, int vignetiTotali)
    {
        var distinti = sel.Select(r => (r.IdVigneto, r.Data, r.TipoRilievo)).Distinct().ToList();
        return new CruscottoSettimanale(
            distinti.GroupBy(r => r.TipoRilievo).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ToList(),
            distinti.Count, sel.Sum(r => r.NrOrgani ?? 0), sel.Select(r => r.IdVigneto).Distinct().Count(), vignetiTotali);
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

    /// <summary>S22: rilievi senza avversità (presenza = 0, sugli organi) / rilievi del tipo; null finché lo snapshot non ha il campo.</summary>
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
            .Select(g => new SerieSettimane(g.Key, settimane.Select(s => Media(g.Where(r => SettimanaQlik.Di(r.Data) == s),
                r => r.StatoIdrico) * -1).ToList())).ToList();

    /// <summary>Catture per trappola: per tipo di monitoraggio e settimana, media sui vigneti della media.</summary>
    public static IReadOnlyList<(int Tipo, IReadOnlyList<double?> Valori)> Catture(IReadOnlyList<RilievoSettimanaDto> sel,
        IReadOnlyList<int> settimane) =>
        sel.Where(r => TipiTrappole.Contains(r.TipoRilievo) && r.CatturePerTrappola is not null).GroupBy(r => r.TipoRilievo).OrderBy(g => g.Key)
            .Select(g => (g.Key, (IReadOnlyList<double?>)settimane.Select(s => Media(g.Where(r => SettimanaQlik.Di(r.Data) == s),
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
            var perVigneto = righe.Where(r => SettimanaQlik.Di(r.Data) == s).GroupBy(r => (r.IdVigneto, r.Data.Year))
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

    /// <summary>Esca (slide 17): rilievi del tipo 9, piante controllate e organi contati per valore (collezione dell'anno).</summary>
    public static (int Rilievi, double Piante, IReadOnlyDictionary<int, double> PerValore) Esca(IReadOnlyList<RilievoSettimanaDto> sel,
        IEnumerable<CollezioneEscaDto> collezione, int anno)
    {
        var righe = sel.Where(r => r.TipoRilievo == 9).ToList();
        return (righe.Select(r => (r.IdVigneto, r.Data)).Distinct().Count(), righe.Sum(r => r.NrOrgani ?? 0),
            collezione.Where(c => c.Anno == anno).GroupBy(c => c.Valore).ToDictionary(g => g.Key, g => g.Sum(c => c.NumOrgani)));
    }

    /// <summary>
    /// Slide 7: giorno medio di una fase (1 germogliamento, 2 fioritura, 3 invaiatura, 4 vendemmia; modello BBCH e
    /// vendemmia già ridotti dallo snapshot per vigneto e anno) negli anni dati: media per anno sui vigneti, poi sugli anni.
    /// </summary>
    public static double? GiornoFase(IEnumerable<FaseFenologicaDto> fasi, int fase, Func<int, bool> anni)
    {
        var perAnno = fasi.Where(f => f.Fase == fase && anni(f.Anno)).GroupBy(f => f.Anno).Select(g => g.Average(f => f.Giorno)).ToList();
        return perAnno.Count == 0 ? null : perAnno.Average();
    }

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
