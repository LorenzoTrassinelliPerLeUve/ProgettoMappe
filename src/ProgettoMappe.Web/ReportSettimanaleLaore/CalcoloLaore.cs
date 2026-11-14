using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportSettimanale;

namespace ProgettoMappe.Web.ReportSettimanaleLaore;

/// <summary>
/// I dati del report LAORE: territorio scelto (provincia/comune, testo per titoli e nome file), settimana, rilievi dello
/// snapshot già ristretti al territorio e luogo di ogni vigneto del perimetro.
/// </summary>
public sealed record DatiLaore(string Territorio, SettimanaReport Settimana, RilieviSettimanaDto Rilievi,
    IReadOnlyDictionary<int, LuogoVignetoDto> Luoghi)
{
    /// <summary>KPI e barre: solo la settimana scelta (come il filtro <c>Week</c> di Qlik, scelta del settimanale #214).</summary>
    public IReadOnlyList<RilievoSettimanaDto> Settimanali => Rilievi.Rilievi.Where(Settimana.Nella).ToList();

    /// <summary>Linee per settimana: l'anno fino alla settimana scelta (in Qlik un punto solo, differenza voluta come nel settimanale).</summary>
    public IReadOnlyList<RilievoSettimanaDto> Stagione => Rilievi.Rilievi.Where(Settimana.Contiene).ToList();

    /// <summary>Vigneti del perimetro nel territorio: il denominatore del cruscotto.</summary>
    public int VignetiTotali => Luoghi.Count;
}

/// <summary>Una malattia delle barre per provincia (fogli 7–14): tipo di rilievo, organi, intensità o diffusione.</summary>
public sealed record MalattiaLaore(string Chiave, int Tipo, IReadOnlyList<int> Organi, bool Intensita);

/// <summary>
/// Le formule dell'app Qlik «REPORT SETTIMANALE - LAORE» (task #217, schemi in <c>laore/schemi.md</c> del lavoro): la
/// dimensione è la provincia (<c>AdministrativeAreaLevel2</c>), non il vigneto. Dove la formula è la stessa del settimanale
/// si usa <see cref="CalcoloSettimanale"/>.
/// </summary>
public static class CalcoloLaore
{
    /// <summary>BBCH della linea «Grappoli» di LAORE: quelli del settimanale più l'89 (vendemmia), come nell'app.</summary>
    public static readonly IReadOnlySet<int> BbchGrappoli = CalcoloSettimanale.BbchGrappoli.Append(89).ToHashSet();

    public static IReadOnlySet<int> BbchFoglie => CalcoloSettimanale.BbchFoglie;

    /// <summary>Tipi di rilievo delle trappole dei fogli «Tignole»: Cryptoblabes e Lobesia botrana.</summary>
    public const int Cryptoblabes = 153;
    public const int Lobesia = 148;

    public static readonly IReadOnlyList<MalattiaLaore> Malattie =
    [
        new("sezione.peronospora-int-foglia", 12, CalcoloSettimanale.OrganiFoglia, true),
        new("sezione.peronospora-diff-foglia", 12, CalcoloSettimanale.OrganiFoglia, false),
        new("sezione.oidio-int-foglia", 11, CalcoloSettimanale.OrganiFoglia, true),
        new("sezione.oidio-diff-foglia", 11, CalcoloSettimanale.OrganiFoglia, false),
        new("sezione.peronospora-int-grappolo", 12, CalcoloSettimanale.OrganiGrappolo, true),
        new("sezione.peronospora-diff-grappolo", 12, CalcoloSettimanale.OrganiGrappolo, false),
        new("sezione.oidio-int-grappolo", 11, CalcoloSettimanale.OrganiGrappolo, true),
        new("sezione.oidio-diff-grappolo", 11, CalcoloSettimanale.OrganiGrappolo, false),
    ];

    /// <summary>I vigneti del perimetro nella provincia e nel comune scelti (vuoti = tutti), per IdVigneto.</summary>
    public static IReadOnlyDictionary<int, LuogoVignetoDto> Filtra(IEnumerable<LuogoVignetoDto> luoghi, string? provincia, string? comune) =>
        luoghi.Where(l => (string.IsNullOrEmpty(provincia) || l.Provincia == provincia) && (string.IsNullOrEmpty(comune) || l.Comune == comune))
            .GroupBy(l => l.IdVigneto).ToDictionary(g => g.Key, g => g.First());

    /// <summary>Il territorio scelto per titoli e nome del file: «Sardegna», «SS» o «Alghero (SS)».</summary>
    public static string Territorio(string? provincia, string? comune) =>
        !string.IsNullOrEmpty(comune) ? string.IsNullOrEmpty(provincia) ? comune : $"{comune} ({provincia})"
        : !string.IsNullOrEmpty(provincia) ? provincia : TestiLaore.Testo("territorio.sardegna");

    /// <summary>Le province dei vigneti del territorio, in ordine alfabetico (una barra o una linea ciascuna).</summary>
    public static IReadOnlyList<string> Province(IReadOnlyDictionary<int, LuogoVignetoDto> luoghi) =>
        luoghi.Values.Select(l => l.Provincia).Distinct().Order(StringComparer.Ordinal).ToList();

    private static string ProvinciaDi(IReadOnlyDictionary<int, LuogoVignetoDto> luoghi, int vigneto) =>
        luoghi.TryGetValue(vigneto, out var l) ? l.Provincia : "—";

    /// <summary>
    /// S24 per provincia: <c>AVG(aggr(max(misura), IdVigneto, Anno)) / 100</c> sui rilievi del tipo e degli organi; le
    /// province senza rilievi restano fuori (Qlik non mostra la barra).
    /// </summary>
    public static IReadOnlyList<(string Provincia, double Valore)> MalattiaPerProvincia(IReadOnlyList<RilievoSettimanaDto> sel,
        IReadOnlyDictionary<int, LuogoVignetoDto> luoghi, MalattiaLaore m)
    {
        Func<RilievoSettimanaDto, double?> misura = m.Intensita ? r => r.PercIntensita : r => r.PercDiffusione;
        return sel.Where(r => r.TipoRilievo == m.Tipo && r.Organo is { } o && m.Organi.Contains(o) && misura(r) is not null)
            .GroupBy(r => (r.IdVigneto, r.Data.Year)).Select(g => (Provincia: ProvinciaDi(luoghi, g.Key.IdVigneto), Max: g.Max(misura)!.Value))
            .GroupBy(v => v.Provincia).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Average(v => v.Max) / 100)).ToList();
    }

    /// <summary>Fogli 3–4: BBCH medio per settimana, una linea per provincia (S14, anno della settimana).</summary>
    public static IReadOnlyList<SerieSettimane> BbchPerProvincia(IReadOnlyList<RilievoSettimanaDto> stagione,
        IReadOnlyDictionary<int, LuogoVignetoDto> luoghi, IReadOnlySet<int> codici, int anno, IReadOnlyList<int> settimane) =>
        stagione.GroupBy(r => ProvinciaDi(luoghi, r.IdVigneto)).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new SerieSettimane(g.Key, CalcoloSettimanale.BbchPerSettimana(g, codici, a => a == anno, settimane)))
            .Where(s => s.Valori.Any(v => v is not null)).ToList();

    /// <summary>
    /// Fogli 5–6: anno in corso, anno precedente e media storica (i cinque anni prima) su tutto il territorio. Anno in corso
    /// fino alla settimana scelta, gli altri sull'anno intero fino alla stessa settimana.
    /// </summary>
    public static (IReadOnlyList<double?> Anno, IReadOnlyList<double?> Precedente, IReadOnlyList<double?> Storico) BbchStorico(
        IReadOnlyList<RilievoSettimanaDto> rilievi, IReadOnlySet<int> codici, int anno, IReadOnlyList<int> settimane) =>
        (CalcoloSettimanale.BbchPerSettimana(rilievi, codici, a => a == anno, settimane),
         CalcoloSettimanale.BbchPerSettimana(rilievi, codici, a => a == anno - 1, settimane),
         CalcoloSettimanale.BbchPerSettimana(rilievi, codici, a => a >= anno - 5 && a <= anno - 1, settimane));

    /// <summary>
    /// Fogli «Tignole» (S25): catture per trappola per settimana, una linea per provincia:
    /// <c>avg(aggr(avg(CatturePerTrappola), WEEK_REF, IdVigneto))</c>.
    /// </summary>
    public static IReadOnlyList<SerieSettimane> CatturePerProvincia(IReadOnlyList<RilievoSettimanaDto> stagione,
        IReadOnlyDictionary<int, LuogoVignetoDto> luoghi, int tipo, IReadOnlyList<int> settimane) =>
        stagione.Where(r => r.TipoRilievo == tipo && r.CatturePerTrappola is not null)
            .GroupBy(r => ProvinciaDi(luoghi, r.IdVigneto)).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new SerieSettimane(g.Key, settimane.Select(s =>
            {
                var perVigneto = g.Where(r => CalcoloSettimanale.Settimana(r) == s).GroupBy(r => r.IdVigneto)
                    .Select(v => v.Average(r => r.CatturePerTrappola!.Value)).ToList();
                return perVigneto.Count == 0 ? (double?)null : perVigneto.Average();
            }).ToList())).ToList();

    /// <summary>
    /// Foglio «Mal dell'Esca», grafico per provincia: righe della collezione con quel valore / piante controllate
    /// (<c>NrOrganiControllati</c> dei rilievi del tipo 9) della provincia, per ogni classe tranne «sintomi assenti».
    /// </summary>
    public static IReadOnlyList<(string Provincia, IReadOnlyDictionary<int, double> PerValore)> EscaPerProvincia(
        IReadOnlyList<RilievoSettimanaDto> sel, IReadOnlyDictionary<int, LuogoVignetoDto> luoghi, IEnumerable<EscaSettimanaDto> collezione)
    {
        var righe = collezione.ToList();
        return sel.Where(r => r.TipoRilievo == 9).GroupBy(r => ProvinciaDi(luoghi, r.IdVigneto)).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (g.Key, Esca: CalcoloSettimanale.Esca(g.ToList(), righe)))
            .Where(p => p.Esca.Piante > 0)
            .Select(p => (p.Key, (IReadOnlyDictionary<int, double>)p.Esca.PerValore.ToDictionary(v => v.Key, v => v.Value / p.Esca.Piante)))
            .ToList();
    }

    /// <summary>Foglio «Filtri»: c'è almeno un rilievo del mal dell'esca (S26) e la diffusione media delle tignole è &gt; 0 (S24).</summary>
    public static (bool Esca, bool Tignole) Presenze(IReadOnlyList<RilievoSettimanaDto> sel)
    {
        var tignole = CalcoloSettimanale.DiffusioneTignole(sel);
        return (sel.Any(r => r.TipoRilievo == 9), tignole.Count > 0 && tignole.Values.Average() > 0);
    }
}
