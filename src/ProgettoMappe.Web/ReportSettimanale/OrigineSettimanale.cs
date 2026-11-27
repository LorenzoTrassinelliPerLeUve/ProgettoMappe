using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>Una riga per vigneto (o per avversità) nel conto di un numero: valore parziale, peso e quanto pesa sul totale.</summary>
public sealed record RigaOrigine(int? IdVigneto, string Nome, string? Nota, double? Valore, double? Peso, double? Contributo);

/// <summary>Un rilievo entrato nel conto, con la misura che ha dato.</summary>
public sealed record RilievoOrigine(string? IdRilievo, int IdVigneto, string Vigneto, DateTime Data, int Tipo, string Misura);

/// <summary>
/// «Da dove viene questo numero» (Reportistica R3, task #417, PROGETTO.md §3.4): il numero com'è nel report, la regola in
/// parole, il conto con i numeri veri, le righe per vigneto, i rilievi usati e quello che è rimasto fuori e perché.
/// <c>Id</c> = id stabile dell'elemento (<c>rs.{sezione}.{elemento}</c>).
/// </summary>
public sealed record TracciaNumero(string Id, string Titolo, string Valore, string Regola, string Conto,
    IReadOnlyList<RigaOrigine> Righe, IReadOnlyList<RilievoOrigine> Rilievi, IReadOnlyList<string> Esclusi,
    string NomeValore, string? NomePeso);

/// <summary>I dati che servono alla traccia: gli stessi che la pagina passa a <see cref="CalcoloSettimanale"/>.</summary>
public sealed record ContestoOrigine(IReadOnlyList<RilievoSettimanaDto> Settimana, IReadOnlyDictionary<int, double?> Superfici,
    IReadOnlyDictionary<int, string> NomiVigneti, IReadOnlyDictionary<int, string> NomiTipi, CultureInfo Cultura,
    Func<string, object[], string> T);

/// <summary>
/// Le tracce dei KPI del report settimanale: rifanno passo per passo le formule di <see cref="CalcoloSettimanale"/> sugli
/// stessi rilievi (la settimana scelta), così il totale delle righe è il numero del report (lo controllano i test
/// <c>OrigineSettimanaleTests</c>). Si parte dai KPI (§3.4): le linee per settimana non hanno ancora la traccia.
/// </summary>
public static class OrigineSettimanale
{
    /// <summary>Quanti rilievi elencare al massimo (il conto li usa tutti).</summary>
    public const int MassimoRilievi = 300;

    private static double Sup(ContestoOrigine c, int vigneto) =>
        c.Superfici.TryGetValue(vigneto, out var s) && s is > 0 ? s.Value : 0;

    private static double Totale(ContestoOrigine c) => c.Superfici.Values.Sum(s => s is > 0 ? s.Value : 0);

    private static string Nome(ContestoOrigine c, int vigneto) =>
        c.NomiVigneti.TryGetValue(vigneto, out var n) && n.Length > 0 ? n : vigneto.ToString(CultureInfo.InvariantCulture);

    private static string Tipo(ContestoOrigine c, int tipo) =>
        c.NomiTipi.TryGetValue(tipo, out var n) && n.Length > 0 ? n : tipo.ToString(CultureInfo.InvariantCulture);

    private static string N(ContestoOrigine c, double v, string formato = "N2") => v.ToString(formato, c.Cultura);

    private static bool Organo(RilievoSettimanaDto r, IReadOnlyList<int>? organi) => organi is null || r.Organo is { } o && organi.Contains(o);

    private static RilievoOrigine Rilievo(ContestoOrigine c, RilievoSettimanaDto r, string misura) =>
        new(r.IdRilievo, r.IdVigneto, Nome(c, r.IdVigneto), r.Data, r.TipoRilievo, misura);

    private static IReadOnlyList<RilievoOrigine> Elenco(IEnumerable<RilievoOrigine> rilievi) =>
        rilievi.OrderByDescending(r => r.Data).ThenBy(r => r.Vigneto).Take(MassimoRilievi).ToList();

    private static string Organi(ContestoOrigine c, IReadOnlyList<int>? organi) =>
        organi is null ? c.T("origine.organi.tutti", []) : c.T("origine.organi", [string.Join(", ", organi)]);

    private static string Misura(ContestoOrigine c, RilievoSettimanaDto r, double? valore, string nomeMisura) =>
        $"{nomeMisura} {(valore is { } v ? N(c, v) : "—")} · {c.T("origine.organo", [r.Organo?.ToString(CultureInfo.InvariantCulture) ?? "—"])}";

    /// <summary>
    /// S03 (<see cref="CalcoloSettimanale.Pesata"/>): per tipo Σ_vigneti max(misura) × superficie / superficie totale / 100,
    /// solo addendi &gt; 0, poi × <paramref name="fattore"/> (nidi: 10.000 per avere «ogni 100 grappoli» in percentuale).
    /// </summary>
    public static TracciaNumero Pesata(ContestoOrigine c, string id, string titolo, string valore, IReadOnlyList<int> tipi,
        IReadOnlyList<int>? organi, Func<RilievoSettimanaDto, double?> misura, string nomeMisura, double fattore = 1)
    {
        var totale = Totale(c);
        var righe = new List<RigaOrigine>();
        var rilievi = new List<RilievoOrigine>();
        var esclusi = new List<string>();
        foreach (var tipo in tipi)
        {
            var delTipo = c.Settimana.Where(r => r.TipoRilievo == tipo).ToList();
            var validi = delTipo.Where(r => Organo(r, organi) && misura(r) is not null).ToList();
            foreach (var g in validi.GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key)))
            {
                var massimo = g.Max(misura)!.Value;
                var sup = Sup(c, g.Key);
                righe.Add(new RigaOrigine(g.Key, Nome(c, g.Key), tipi.Count > 1 ? Tipo(c, tipo) : null, massimo, sup,
                    totale > 0 ? massimo * sup / totale / 100 * fattore : 0));
                if (sup <= 0)
                {
                    esclusi.Add(c.T("origine.senza-superficie", [Nome(c, g.Key)]));
                }
            }
            rilievi.AddRange(validi.Select(r => Rilievo(c, r, Misura(c, r, misura(r), nomeMisura))));
            if (delTipo.Count(r => !Organo(r, organi)) is var altriOrgani and > 0)
            {
                esclusi.Add(c.T("origine.altri-organi", [altriOrgani, Tipo(c, tipo)]));
            }
            if (delTipo.Count(r => Organo(r, organi) && misura(r) is null) is var vuoti and > 0)
            {
                esclusi.Add(c.T("origine.misura-vuota", [vuoti, Tipo(c, tipo), nomeMisura]));
            }
        }
        var senza = c.Superfici.Keys.Except(righe.Select(r => r.IdVigneto ?? 0)).Count(v => Sup(c, v) > 0);
        if (senza > 0)
        {
            esclusi.Add(c.T("origine.vigneti-zero", [senza]));
        }
        var numeratore = righe.Sum(r => (r.Valore ?? 0) * (r.Peso ?? 0));
        return new TracciaNumero(id, titolo, valore,
            c.T("origine.regola.pesata", [string.Join(", ", tipi.Select(t => Tipo(c, t))), Organi(c, organi), nomeMisura]),
            c.T(fattore == 1 ? "origine.conto.pesata" : "origine.conto.pesata-fattore",
                [N(c, numeratore), N(c, totale, "N0"), N(c, fattore, "N0")]),
            righe, Elenco(rilievi), esclusi, c.T("origine.colonna.massimo", [nomeMisura]), c.T("origine.colonna.superficie", []));
    }

    /// <summary>S22 (<see cref="CalcoloSettimanale.SenzaDanno"/>): rilievi senza avversità sugli organi / rilievi del tipo.</summary>
    public static TracciaNumero SenzaDanno(ContestoOrigine c, string id, string titolo, string valore, int tipo, IReadOnlyList<int>? organi)
    {
        var righe = c.Settimana.Where(r => r.TipoRilievo == tipo).ToList();
        var buoni = righe.Where(r => Organo(r, organi) && r.PresenzaAvversita == 0).ToList();
        var perVigneto = righe.GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key))
            .Select(g => new RigaOrigine(g.Key, Nome(c, g.Key), null, g.Count(r => Organo(r, organi) && r.PresenzaAvversita == 0), g.Count(), null))
            .ToList();
        var esclusi = new List<string>();
        if (righe.Count(r => !Organo(r, organi)) is var altri and > 0)
        {
            esclusi.Add(c.T("origine.altri-organi-denominatore", [altri]));
        }
        return new TracciaNumero(id, titolo, valore, c.T("origine.regola.senza-danno", [Tipo(c, tipo), Organi(c, organi)]),
            c.T("origine.conto.frazione", [buoni.Count, righe.Count]), perVigneto,
            Elenco(righe.Select(r => Rilievo(c, r, c.T("origine.presenza", [r.PresenzaAvversita?.ToString(CultureInfo.InvariantCulture) ?? "—",
                r.Organo?.ToString(CultureInfo.InvariantCulture) ?? "—"])))),
            esclusi, c.T("origine.colonna.senza-danno", []), c.T("origine.colonna.rilievi", []));
    }

    /// <summary>S21 (<see cref="CalcoloSettimanale.Copertura"/>): superficie dei vigneti con un rilievo del tipo / superficie totale.</summary>
    public static TracciaNumero Copertura(ContestoOrigine c, string id, string titolo, string valore, int tipo)
    {
        var delTipo = c.Settimana.Where(r => r.TipoRilievo == tipo).ToList();
        var righe = delTipo.GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key))
            .Select(g => new RigaOrigine(g.Key, Nome(c, g.Key), null, g.Count(), Sup(c, g.Key), null)).ToList();
        return new TracciaNumero(id, titolo, valore, c.T("origine.regola.copertura", [Tipo(c, tipo)]),
            c.T("origine.conto.frazione", [N(c, righe.Sum(r => r.Peso ?? 0), "N0"), N(c, Totale(c), "N0")]), righe,
            Elenco(delTipo.Select(r => Rilievo(c, r, Tipo(c, r.TipoRilievo)))), [], c.T("origine.colonna.rilievi", []),
            c.T("origine.colonna.superficie", []));
    }

    /// <summary>Slide 3 «Sanità Grappolo»: 1 − intensità pesate di botrite, marciumi e oidio sul grappolo.</summary>
    public static TracciaNumero SanitaGrappolo(ContestoOrigine c, string titolo, string valore)
    {
        var righe = CalcoloSettimanale.TipiSanitaGrappolo.Select(t => new RigaOrigine(null, Tipo(c, t), null,
            CalcoloSettimanale.Pesata(c.Settimana, c.Superfici, [t], CalcoloSettimanale.OrganiGrappolo, r => r.PercIntensita), null,
            -CalcoloSettimanale.Pesata(c.Settimana, c.Superfici, [t], CalcoloSettimanale.OrganiGrappolo, r => r.PercIntensita))).ToList();
        var rilievi = c.Settimana.Where(r => CalcoloSettimanale.TipiSanitaGrappolo.Contains(r.TipoRilievo)
                && Organo(r, CalcoloSettimanale.OrganiGrappolo) && r.PercIntensita is not null)
            .Select(r => Rilievo(c, r, Misura(c, r, r.PercIntensita, c.T("kpi.intensita", []))));
        return new TracciaNumero("rs.sanita-grappoli.sanita", titolo, valore, c.T("origine.regola.sanita-grappolo", []),
            c.T("origine.conto.sanita-grappolo", [string.Join(" − ", righe.Select(r => (r.Valore ?? 0).ToString("P1", c.Cultura)))]),
            righe, Elenco(rilievi), [], c.T("origine.colonna.intensita-pesata", []), null);
    }

    /// <summary>Slide 3 «Riduzione ponderata della produzione»: danni dell'anno per vigneto (vista del dba, non rilievi).</summary>
    public static TracciaNumero Danno(ContestoOrigine c, string titolo, string valore, IEnumerable<DannoSettimanaDto> danni, int anno)
    {
        var perVigneto = danni.Where(d => d.Anno == anno && c.Superfici.ContainsKey(d.IdVigneto)).GroupBy(d => d.IdVigneto)
            .Select(g => (Id: g.Key, Max: g.Max(d => d.PercDanno), Sup: Sup(c, g.Key))).OrderBy(v => Nome(c, v.Id)).ToList();
        var totale = perVigneto.Sum(v => v.Sup);
        var righe = perVigneto.Select(v => new RigaOrigine(v.Id, Nome(c, v.Id), null, v.Max, v.Sup,
            totale > 0 ? v.Max * v.Sup / totale / 100 : 0)).ToList();
        return new TracciaNumero("rs.sanita-grappoli.riduzione", titolo, valore, c.T("origine.regola.danno", [anno]),
            c.T("origine.conto.pesata", [N(c, perVigneto.Sum(v => v.Max * v.Sup)), N(c, totale, "N0")]), righe, [],
            [c.T("origine.danno.fonte", [])], c.T("origine.colonna.danno", []), c.T("origine.colonna.superficie", []));
    }

    /// <summary>Slide 2: rilievi distinti, organi controllati, vigneti monitorati (<see cref="CalcoloSettimanale.Cruscotto"/>).</summary>
    public static TracciaNumero Cruscotto(ContestoOrigine c, string elemento, string titolo, string valore, int vignetiTotali)
    {
        var righe = c.Settimana.GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key))
            .Select(g => new RigaOrigine(g.Key, Nome(c, g.Key), null,
                g.Select(r => r.IdRilievo is { } i ? i[..Math.Min(i.Length, 23)] : $"{r.Data:O}|{r.TipoRilievo}").Distinct().Count(),
                g.Sum(r => r.NrOrgani ?? 0), null)).ToList();
        var conto = elemento switch
        {
            "organi" => c.T("origine.conto.somma", [N(c, righe.Sum(r => r.Peso ?? 0), "N0")]),
            "monitorati" or "percentuale" => c.T("origine.conto.frazione", [righe.Count, vignetiTotali]),
            _ => c.T("origine.conto.somma", [N(c, righe.Sum(r => r.Valore ?? 0), "N0")]),
        };
        return new TracciaNumero($"rs.dashboard.{elemento}", titolo, valore, c.T($"origine.regola.cruscotto.{elemento}", []), conto, righe,
            Elenco(c.Settimana.Select(r => Rilievo(c, r, Tipo(c, r.TipoRilievo)
                + (r.NrOrgani is { } o ? " · " + c.T("origine.organi-controllati", [N(c, o, "N0")]) : "")))),
            [], c.T("origine.colonna.rilievi", []), c.T("origine.colonna.organi", []));
    }

    /// <summary>
    /// Slide 17 (<see cref="CalcoloSettimanale.Esca"/>): rilievi del tipo 9, piante controllate o piante con un valore della
    /// collezione; per ogni rilievo quante piante ha contato (il dettaglio del rilievo le mostra una per una).
    /// </summary>
    public static TracciaNumero Esca(ContestoOrigine c, string elemento, string titolo, string valore, IEnumerable<EscaSettimanaDto> collezione,
        int? valoreCollezione)
    {
        var rilievi = c.Settimana.Where(r => r.TipoRilievo == 9).ToList();
        var perRilievo = collezione.Where(e => valoreCollezione is null || e.Valore == valoreCollezione)
            .GroupBy(e => e.IdRilievo).ToDictionary(g => g.Key, g => g.Count());
        double Conte(RilievoSettimanaDto r) => r.IdRilievo is { } i ? perRilievo.GetValueOrDefault(i) : 0;
        var righe = rilievi.GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key))
            .Select(g => new RigaOrigine(g.Key, Nome(c, g.Key), null, valoreCollezione is null ? g.Count() : g.Sum(Conte),
                g.Sum(r => r.NrOrgani ?? 0), null)).ToList();
        var conto = valoreCollezione is null
            ? c.T("origine.conto.somma", [N(c, elemento == "piante" ? righe.Sum(r => r.Peso ?? 0) : rilievi.Count, "N0")])
            : c.T("origine.conto.somma", [N(c, righe.Sum(r => r.Valore ?? 0), "N0")]);
        return new TracciaNumero($"rs.esca.{elemento}", titolo, valore,
            valoreCollezione is { } v ? c.T("origine.regola.esca.valore", [v]) : c.T($"origine.regola.esca.{elemento}", []), conto, righe,
            Elenco(rilievi.Select(r => Rilievo(c, r, c.T("origine.esca.rilievo", [N(c, r.NrOrgani ?? 0, "N0"), Conte(r)])))), [],
            valoreCollezione is null ? c.T("origine.colonna.rilievi", []) : c.T("origine.colonna.piante-valore", [valoreCollezione]),
            c.T("origine.colonna.piante", []));
    }

    /// <summary>Slide 16 (<see cref="CalcoloSettimanale.Generazione"/>): media sui vigneti della diffusione massima della generazione.</summary>
    public static TracciaNumero Generazione(ContestoOrigine c, string chiave, string titolo, string valore, int[] codici)
    {
        var righe = c.Settimana.Where(r => r.EtaEpoca is { } e && codici.Contains(e)).ToList();
        var perVigneto = righe.Where(r => r.PercDiffusione is not null).GroupBy(r => r.IdVigneto).OrderBy(g => Nome(c, g.Key))
            .Select(g => new RigaOrigine(g.Key, Nome(c, g.Key), null, g.Max(r => r.PercDiffusione!.Value), Sup(c, g.Key), null)).ToList();
        return new TracciaNumero($"rs.tignole-larve.{chiave}", titolo, valore,
            c.T("origine.regola.generazione", [string.Join(", ", codici)]),
            c.T("origine.conto.media", [N(c, perVigneto.Sum(r => r.Valore ?? 0)), perVigneto.Count]), perVigneto,
            Elenco(righe.Select(r => Rilievo(c, r, $"{c.T("kpi.diffusione", [])} {(r.PercDiffusione is { } d ? N(c, d) : "—")}"))), [],
            c.T("origine.colonna.massimo", [c.T("kpi.diffusione", [])]), c.T("origine.colonna.superficie", []));
    }
}
