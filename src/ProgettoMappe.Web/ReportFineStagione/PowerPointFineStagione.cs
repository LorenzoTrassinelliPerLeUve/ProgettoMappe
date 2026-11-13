using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.Report;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>I dati di un report fine stagione: tutto quello che la pagina legge dall'Api (meteo, campo, indice).</summary>
public sealed record DatiFineStagione(string Azienda, int Annata, MeteoReportFineStagioneDto? Meteo, CampoReportFineStagioneDto? Campo,
    IndiceBigotReportDto? Indice);

/// <summary>
/// PowerPoint del report fine stagione (task #203), al posto delle automazioni Qlik «PPT_REPORT_FINE_STAGIONE» (71 slide)
/// e «REPORT_FINE_STAGIONE_ENG» (75): una slide per foglio, nello stesso ordine e con gli stessi titoli
/// (<see cref="SlideIt"/>, <see cref="SlideEn"/>, da <c>automazione.md</c>). L'ordine inglese è quello di Qlik, con le sue
/// ripetizioni (fattori SFE due volte, i quattro fogli dell'invaiatura anche in fondo). Le tabelle lunghe continuano su
/// altre slide («(2/3)» nel titolo). Grafici veri e quadranti a forme come il pilota Indice Bigot
/// (<see cref="PresentazionePptx"/>), con i numeri delle stesse classi di calcolo della pagina.
/// </summary>
public static class PowerPointFineStagione
{
    private static readonly string[] Storici = ["ib-storico", "ib-storico-vigoria", "ib-storico-morfologia", "ib-storico-sfe",
        "ib-storico-produzione", "ib-storico-sfekg", "ib-storico-stress", "ib-storico-sanita", "ib-storico-biodiversita"];

    private static readonly string[] Indice = ["ib-istogramma", "ib-vigneti", "ib-vigna", "ib-indicatori-1", "ib-indicatori-2",
        "ib-indicatori-3", .. Storici];

    private static readonly string[] Meteo =
    [
        "termo-1", "termico-1", "freddi-1", "pioggia-1", "termo-2", "termico-2", "pioggia-2", "termo-3", "termico-3", "caldi-3",
        "pioggia-3", "termo-4", "termico-4", "caldi-4", "pioggia-4", "giorno-1", "giorno-2", "giorno-3", "giorno-4",
        "pioggia-storico", "pioggia-agraria", "pioggia-periodo",
    ];

    private static readonly string[] Sanita =
        ["cruscotto", "foglie", "grappoli", "esca", "voli", "nidi", "fenologia", "bbch-foglie", "bbch-grappoli", "bbch-storico-foglie", "bbch-storico-grappoli"];

    private static readonly string[] Produzione = ["prod-vigneto", "prod-vigneto-anno", "prod-confronto", "prod-stime", "fallanze"];

    /// <summary>Le 71 slide del report italiano di Qlik, in ordine.</summary>
    public static readonly IReadOnlyList<string> SlideIt =
    [
        "copertina", "sommario", "caratterizzazione", .. Meteo, .. Sanita,
        "gestione-idrica", "apici", "apici-storico", "apici-storico", "apici-storico", "suolo", "scottature-media", "scottature-max",
        "vigoria", "vigoria-2", "chioma-2", "chioma-1", "fattori-sfe", "prod-vitigno", .. Produzione, .. Indice, "nidi-tabella",
    ];

    /// <summary>Le 75 slide del report inglese di Qlik, in ordine (con le ripetizioni dell'automazione ENG).</summary>
    public static readonly IReadOnlyList<string> SlideEn =
    [
        "copertina", "sommario", "caratterizzazione", .. Meteo,
        "gestione-idrica", "apici", "apici-storico", "prealba", "prealba-storico", "suolo", "scottature-media", "scottature-max",
        "vigoria", "vigoria-2", "chioma-2", "chioma-1", "fattori-sfe", "fattori-sfe", .. Produzione,
        "fenologia", "bbch-foglie", "bbch-grappoli", "bbch-storico-foglie", "bbch-storico-grappoli",
        "cruscotto", "foglie", "grappoli", "esca", "voli", "nidi", "nidi-tabella", .. Indice,
        "termo-4", "termico-4", "caldi-4", "pioggia-4",
    ];

    public static IReadOnlyList<string> Slide(LinguaReport lingua) => lingua == LinguaReport.En ? SlideEn : SlideIt;

    /// <summary>«{Azienda} - Report fine stagione {annata} [ENG] - {AAAA-MM-GG HHmm}.pptx».</summary>
    public static string NomeFile(string azienda, int annata, LinguaReport lingua, DateTimeOffset adesso, string estensione = ".pptx") =>
        Path.ChangeExtension(NomeFileReport.Crea(azienda,
            $"{(lingua == LinguaReport.En ? "End of season report" : "Report fine stagione")} {annata.ToString(CultureInfo.InvariantCulture)}", adesso), estensione);

    public static byte[] Crea(DatiFineStagione dati, LinguaReport lingua, DateTimeOffset adesso)
    {
        var g = new Generatore(dati, lingua, adesso);
        foreach (var chiave in Slide(lingua))
        {
            g.Aggiungi(chiave);
        }
        return g.Ppt.Salva();
    }

    private sealed class Generatore
    {
        private readonly DatiFineStagione _d;
        private readonly LinguaReport _lingua;
        private readonly DateTimeOffset _adesso;
        private readonly CultureInfo _c;
        private readonly IReadOnlyDictionary<int, string> _nomi;
        private readonly IReadOnlyList<MeteoFase>? _fasi;
        private bool _indiceFatto;

        public PresentazionePptx Ppt { get; }

        public Generatore(DatiFineStagione d, LinguaReport lingua, DateTimeOffset adesso)
        {
            _d = d;
            _lingua = lingua;
            _adesso = adesso;
            _c = TestiReport.Cultura(lingua);
            _nomi = (d.Campo?.Vigneti.Select(v => (v.IdVigneto, v.Nome)) ?? [])
                .Concat(d.Meteo?.Vigneti.Select(v => (v.Id, v.Nome)) ?? [])
                .GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.First().Item2);
            _fasi = d.Meteo is { } m && CalcoloMeteoReport.VignetiStorici(m).Count > 0 ? MeteoPerFase.Calcola(m, d.Annata) : null;
            Ppt = new PresentazionePptx($"{d.Azienda} - {T("titolo", d.Annata)}",
                $"{T("titolo", d.Annata)} · {d.Azienda} · {T("generato.il", adesso.ToOffset(TimeSpan.FromHours(1)).ToString("dd/MM/yyyy", _c))}");
        }

        private string T(string chiave, params object[] valori) => TestiReport.Testo(_lingua, chiave, valori);

        private string Titolo(string chiave) => chiave == "gestione-idrica" ? T("slide.gestione-idrica", _d.Annata) : T($"slide.{chiave}");

        private string N(double? v, string formato = "N1") => v is { } x ? x.ToString(formato, _c) : "—";

        private string N(decimal? v, string formato = "N1") => v is { } x ? x.ToString(formato, _c) : "—";

        private string Nome(int id) => _nomi.TryGetValue(id, out var n) ? n : $"{id}";

        private string Vuota => T("slide.vuota");

        public void Aggiungi(string chiave)
        {
            var titolo = Titolo(chiave);
            switch (chiave)
            {
                case "copertina":
                    Ppt.Copertina(T("titolo", _d.Annata), _d.Azienda, [T("generato.il", _adesso.ToString("d", _c))], []);
                    break;
                case "sommario":
                    Ppt.Testo(titolo, null, SezioniReport.Tutte.Select(s => $"{s.Numero}. {SezioniReport.Titolo(s, _lingua, _d.Annata)}"), 1500);
                    break;
                case "caratterizzazione":
                    Ppt.Divisore(SezioniReport.Titolo(SezioniReport.Tutte[0], _lingua, _d.Annata));
                    break;
                case "gestione-idrica":
                    Ppt.Divisore(titolo);
                    break;
                case var k when k.StartsWith("termo-", StringComparison.Ordinal) || k.StartsWith("termico-", StringComparison.Ordinal)
                                || k.StartsWith("freddi-", StringComparison.Ordinal) || k.StartsWith("caldi-", StringComparison.Ordinal)
                                || k.StartsWith("pioggia-", StringComparison.Ordinal) && char.IsDigit(k[^1]):
                    FaseMeteo(titolo, k);
                    break;
                case var k when k.StartsWith("giorno-", StringComparison.Ordinal):
                    Giornaliero(titolo, int.Parse(k[^1..], CultureInfo.InvariantCulture) - 1);
                    break;
                case "pioggia-storico" or "pioggia-agraria":
                    Cumulata(titolo, chiave == "pioggia-agraria");
                    break;
                case "pioggia-periodo":
                    PioggiaPeriodo(titolo);
                    break;
                case "cruscotto" or "foglie" or "grappoli" or "esca" or "voli" or "nidi" or "nidi-tabella" or "fenologia"
                    or "bbch-foglie" or "bbch-grappoli" or "bbch-storico-foglie" or "bbch-storico-grappoli":
                    SlideSanita(titolo, chiave);
                    break;
                case "apici" or "apici-storico" or "prealba" or "prealba-storico" or "suolo" or "scottature-media" or "scottature-max":
                    SlideIdrica(titolo, chiave);
                    break;
                case "vigoria" or "vigoria-2" or "chioma-2" or "chioma-1" or "fattori-sfe":
                    SlideVigoria(titolo, chiave);
                    break;
                case "prod-vitigno" or "prod-vigneto" or "prod-vigneto-anno" or "prod-confronto" or "prod-stime" or "fallanze":
                    SlideProduzione(titolo, chiave);
                    break;
                case var k when Indice.Contains(k):
                    SlideIndice(k);
                    break;
                default:
                    throw new InvalidOperationException($"Slide sconosciuta: {chiave}");
            }
        }

        // ── Meteo (fogli 4–25) ──────────────────────────────────────────────────────────────────────────────

        private void FaseMeteo(string titolo, string chiave)
        {
            var fase = int.Parse(chiave[^1..], CultureInfo.InvariantCulture);
            if (_fasi is null)
            {
                Ppt.Testo(titolo, null, [T("slide.meteo-vuoto")]);
                return;
            }
            var f = _fasi[fase - 1];
            var annate = f.Serie.Select(p => p.Annata.ToString(CultureInfo.InvariantCulture)).ToList();
            SerieReport Serie(CampoMeteo campo) => new(T($"campo.{campo}"), f.Serie.Select(p => (double?)p.Valori.GetValueOrDefault(campo)).ToList());
            TesseraReport Riquadro(RiquadroMeteo r) => new(T($"campo.{r.Campo}"), N(r.Valori.Annata),
                $"{T("kpi.media", 10)}: {N(r.Valori.Media)} · {T("kpi.devst")}: {N(r.Valori.DevSt)}");
            var nota = T($"fase.{fase}");
            switch (chiave[..chiave.IndexOf('-')])
            {
                case "termo":
                    var t = f.TermoPluviometrico;
                    Ppt.GraficoSerie(titolo, nota, annate, [Serie(CampoMeteo.Pioggia)], false,
                    [
                        new(T("campo.TMed"), N(t.TMed30.Annata), $"{T("kpi.media", 30)}: {N(t.TMed30.Media)} · {T("kpi.media", 5)}: {N(t.TMed5.Media)}"),
                        new(T("campo.Pioggia"), N(t.Pioggia30.Annata, "N0"), $"{T("kpi.media", 30)}: {N(t.Pioggia30.Media, "N0")} · {T("kpi.media", 5)}: {N(t.Pioggia5.Media, "N0")}"),
                        new(T("kpi.var", _d.Annata), N(t.Pioggia30.ScartoPct, "P0"), T("kpi.media", 30)),
                        new(T("kpi.var", _d.Annata), N(t.Pioggia5.ScartoPct, "P0"), T("kpi.media", 5)),
                    ], Vuota, "0");
                    break;
                case "termico":
                    Ppt.GraficoSerie(titolo, nota, annate, MeteoPerFase.Temperature.Select(Serie).ToList(), true,
                        f.Termico.Select(Riquadro).ToList(), Vuota);
                    break;
                case "freddi" or "caldi":
                    Ppt.GraficoSerie(titolo, nota, annate, f.GiorniTemperatura.Select(r => Serie(r.Campo)).ToList(), false,
                        f.GiorniTemperatura.Select(Riquadro).ToList(), Vuota, "0");
                    break;
                default:
                    Ppt.GraficoSerie(titolo, nota, annate, f.GiorniPioggia.Select(r => Serie(r.Campo)).ToList(), false,
                        f.GiorniPioggia.Select(Riquadro).ToList(), Vuota, "0");
                    break;
            }
        }

        private void Giornaliero(string titolo, int periodo)
        {
            if (_d.Meteo is not { } m || _fasi is null)
            {
                Ppt.Testo(titolo, null, [T("slide.meteo-vuoto")]);
                return;
            }
            var punti = MeteoPerFase.Giornalieri(m, _d.Annata)[periodo];
            var pioggia = punti.Sum(p => p.Pioggia ?? 0);
            Ppt.GraficoSerie(titolo, $"{T("campo.Pioggia")}: {pioggia.ToString("N0", _c)}", punti.Select(p => p.Data.ToString("dd/MM", _c)).ToList(),
            [
                new(T("campo.TMax"), punti.Select(p => (double?)p.TMax).ToList(), "C0392B"),
                new(T("campo.TMed"), punti.Select(p => (double?)p.TMed).ToList(), "8DC63E"),
                new(T("campo.TMin"), punti.Select(p => (double?)p.TMin).ToList(), "5F727F"),
                new(T("campo.Pioggia"), punti.Select(p => (double?)p.Pioggia).ToList(), "3C6FB4"),
            ], true, [], Vuota);
        }

        private void Cumulata(string titolo, bool agraria)
        {
            if (_d.Meteo is not { } m || _fasi is null)
            {
                Ppt.Testo(titolo, null, [T("slide.meteo-vuoto")]);
                return;
            }
            var settimane = PioggiaCumulata.Calcola(m, _d.Annata, agraria);
            Ppt.GraficoSerie(titolo, T("campo.Pioggia"), settimane.Select(s => s.Settimana.ToString(CultureInfo.InvariantCulture)).ToList(),
            [
                new(T("slide.cumulata-storico"), settimane.Select(s => s.Storico).ToList(), "5F727F"),
                new(T("slide.cumulata-anno", _d.Annata), settimane.Select(s => s.Anno).ToList(), "3C6FB4"),
            ], true, [], Vuota, "0");
        }

        private void PioggiaPeriodo(string titolo)
        {
            if (_d.Meteo is not { } m || _fasi is null)
            {
                Ppt.Testo(titolo, null, [T("slide.meteo-vuoto")]);
                return;
            }
            Ppt.Tabella(titolo, T("pioggia.vigneti", string.Join(", ", m.Vigneti.Where(v => v.DatiMeteoStorico).Select(v => v.Nome))),
            [
                new(T("pioggia.fase"), Peso: 2), new(T("pioggia.annata", _d.Annata), true), new(T("pioggia.media10"), true),
                new(T("pioggia.media30"), true), new(T("pioggia.var10"), true), new(T("pioggia.var10pct"), true),
                new(T("pioggia.var30"), true), new(T("pioggia.var30pct"), true),
            ],
            CalcoloMeteoReport.PioggiaPerFase(m, _d.Annata).Select(r => (IReadOnlyList<string?>)
            [
                T($"fase.{r.Fase}"), N(r.Annata, "N0"), N(r.Media10, "N0"), N(r.Media30, "N0"), N(r.Variazione10, "N0"),
                N(r.VariazionePct10, "P0"), N(r.Variazione30, "N0"), N(r.VariazionePct30, "P0"),
            ]).ToList(), Vuota);
        }

        // ── Sanità (fogli 26–36 e 71) ───────────────────────────────────────────────────────────────────────

        private (List<string> Voci, List<SerieReport> Serie) PerVigneto(IEnumerable<(string Nome, IReadOnlyDictionary<int, double> Valori)> serie)
        {
            var elenco = serie.Where(s => s.Valori.Count > 0).ToList();
            var vigneti = elenco.SelectMany(s => s.Valori.Keys).Distinct().OrderBy(Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
            return (vigneti.Select(Nome).ToList(), elenco.Select(s => new SerieReport(s.Nome,
                vigneti.Select(id => s.Valori.TryGetValue(id, out var v) ? (double?)v : null).ToList())).ToList());
        }

        private (List<string> Voci, List<SerieReport> Serie) PerSettimana(IEnumerable<(string Nome, IReadOnlyDictionary<int, double> Valori)> serie)
        {
            var elenco = serie.Where(s => s.Valori.Count > 0).ToList();
            var settimane = elenco.SelectMany(s => s.Valori.Keys).Distinct().Order().ToList();
            return (settimane.Select(s => T("apici.settimana", s)).ToList(), elenco.Select(s => new SerieReport(s.Nome,
                settimane.Select(w => s.Valori.TryGetValue(w, out var v) ? (double?)v : null).ToList())).ToList());
        }

        private void SlideSanita(string titolo, string chiave)
        {
            if (_d.Campo is not { } campo)
            {
                Ppt.Testo(titolo, null, [T("campo.non-disponibile")]);
                return;
            }
            var a = _d.Annata;
            switch (chiave)
            {
                case "cruscotto":
                    var c = CalcoloSanitaReport.Cruscotto(campo, a);
                    Ppt.GraficoSerie(titolo, T("san.cruscotto"), c.PerTipo.Select(t => t.Tipo).ToList(),
                        [new SerieReport(T("san.n-rilievi"), c.PerTipo.Select(t => (double?)t.Rilievi).ToList())], false,
                    [
                        new(T("san.n-rilievi"), c.Rilievi.ToString("N0", _c)), new(T("san.n-organi"), c.Organi.ToString("N0", _c)),
                        new(T("san.monitorati"), $"{c.Monitorati} / {c.Totale}"),
                        new(T("san.perc"), c.Totale > 0 ? ((double)c.Monitorati / c.Totale).ToString("P0", _c) : "—"),
                    ], Vuota, "0");
                    break;
                case "foglie" or "grappoli":
                    var danno = CalcoloSanitaReport.Danno(campo, a, chiave == "grappoli");
                    var (voci, serie) = PerVigneto(danno.Select(s => (T($"tipo.{s.Tipo}"), s.Serie.PerVigneto)));
                    Ppt.GraficoSerie(titolo, T(chiave == "foglie" ? "san.foglie-grafico" : "san.grappoli-grafico", a), voci, serie, false, [], Vuota, "0%");
                    break;
                case "esca":
                    var esca = CalcoloSanitaReport.MalDellEsca(campo, a);
                    var (vociEsca, serieEsca) = PerVigneto(esca.Diffusione.Select(s => (T($"esca.{s.Nome}"), s.PerVigneto)));
                    Ppt.GraficoSerie(titolo, T("san.esca-grafico", a), vociEsca, serieEsca, false,
                    [
                        new(T("san.n-rilievi"), esca.Rilievi.ToString("N0", _c)),
                        new(T("san.esca-piante"), esca.PianteControllate.ToString("N0", _c)),
                        new(T("esca.0"), esca.OrganiPerValore.GetValueOrDefault(0).ToString("N0", _c)),
                        new(T("esca.100"), esca.OrganiPerValore.GetValueOrDefault(100).ToString("N0", _c)),
                    ], Vuota, "0.0%");
                    break;
                case "voli":
                    var (vociVoli, serieVoli) = PerSettimana(CalcoloSanitaReport.VoliTignole(campo, a)
                        .Select(v => (campo.Sanita?.NomiTipi.GetValueOrDefault(v.Tipo) ?? $"{v.Tipo}", v.Serie.PerSettimana)));
                    Ppt.GraficoSerie(titolo, T("san.voli-grafico"), vociVoli, serieVoli, true, [], Vuota);
                    break;
                case "nidi":
                    // Task #204: il barchart di Qlik (diffusione massima per vigna e vigneto), le tre generazioni nelle tessere.
                    var massime = CalcoloSanitaReport.TignoleMassime(campo, a);
                    var vignaNidi = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Vigna ?? "");
                    var ordineNidi = massime.OrderBy(m => vignaNidi.GetValueOrDefault(m.Key, ""), StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(m => Nome(m.Key), StringComparer.CurrentCultureIgnoreCase).ToList();
                    Ppt.GraficoSerie(titolo, T("san.nidi-max"), ordineNidi.Select(m => Nome(m.Key)).ToList(),
                        [new SerieReport(T("san.diffusione"), ordineNidi.Select(m => (double?)m.Value).ToList())], false,
                        CalcoloSanitaReport.NidiTignole(campo, a).Select(g => new TesseraReport(T("san.generazione", g.Numero), N(g.Valore),
                            $"{T("san.controllati", g.Numero)}: {N(g.QuotaControllata, "P0")}")).ToList(), Vuota, "0.0");
                    break;
                case "nidi-tabella":
                    Ppt.Tabella(titolo, T("san.nidi"),
                    [
                        new(T("sfe.data")), new(T("suolo.vigna"), Peso: 1.4), new(T("suolo.vigneto"), Peso: 1.6), new(T("san.eta-epoca"), true),
                        new(T("san.diffusione"), true), new(T("san.nidi-n"), true), new(T("san.note"), Peso: 2),
                    ],
                    CalcoloSanitaReport.RilieviNidi(campo, a).Select(r => (IReadOnlyList<string?>)
                    [
                        r.Data.ToString("d", _c), campo.Vigneti.FirstOrDefault(v => v.IdVigneto == r.IdVigneto)?.Vigna, Nome(r.IdVigneto),
                        r.EtaEpoca?.ToString(CultureInfo.InvariantCulture), N(r.PercDiffusione), N(r.NrFormeMobiliNidi, "N0"), r.Note,
                    ]).ToList(), Vuota);
                    break;
                case "fenologia":
                    // Task #204: i quattro distributionplot di Qlik (vitigno, un punto per anno, giorno come data sul calendario
                    // dell'annata, come Day_Ref) e i KPI in quattro tessere.
                    var fasi = CalcoloSanitaReport.Fenologia(campo, a);
                    var origine = (new DateTime(a, 1, 1) - new DateTime(1899, 12, 30)).Days - 1;
                    Ppt.Pannelli(titolo, T("feno.giorno"), fasi.Select(f =>
                    {
                        var vitigni = f.Vitigni.Select(v => v.Vitigno).ToList();
                        var anniFase = f.Vitigni.SelectMany(v => v.PerAnno.Keys).Where(x => x <= a && x > a - 10).Distinct().Order().ToList();
                        return new PannelloPunti(T("feno.confronto", T($"feno.{f.Fase}").ToLower(_c)), vitigni,
                            anniFase.Select(x => new SerieReport(x.ToString(CultureInfo.InvariantCulture),
                                f.Vitigni.Select(v => v.PerAnno.TryGetValue(x, out var g) ? (double?)(g + origine) : null).ToList())).ToList(), "dd/mm");
                    }).ToList(),
                    fasi.Select(f => new TesseraReport($"{T($"feno.{f.Fase}")} · {a}", Giorno(f.Annata),
                        $"{a - 1}: {Giorno(f.AnnataPrima)} · {T("san.kpi-5")}: {Giorno(f.Media5)} · {T("san.kpi-10")}: {Giorno(f.Media10)}")).ToList(),
                    Vuota);
                    break;
                case "bbch-foglie" or "bbch-grappoli":
                    var (vociB, serieB) = PerSettimana(CalcoloSanitaReport.BbchPerVitigno(campo, a, chiave == "bbch-grappoli")
                        .Select(s => (s.Nome, s.PerSettimana)));
                    Ppt.GraficoSerie(titolo, T(chiave == "bbch-foglie" ? "san.bbch-foglie" : "san.bbch-grappoli"), vociB, serieB, true, [], Vuota, "0");
                    break;
                default:
                    var grappoli = chiave == "bbch-storico-grappoli";
                    var (vociS, serieS) = PerSettimana(CalcoloSanitaReport.BbchStorico(campo, a, grappoli)
                        .Select(s => (T($"san.{s.Nome}"), s.PerSettimana)));
                    Ppt.GraficoSerie(titolo, T(grappoli ? "san.bbch-grappoli-media" : "san.bbch-foglie-media"), vociS, serieS, true, [], Vuota, "0");
                    break;
            }
        }

        private string Giorno(double? giorno) => giorno is { } g ? CalcoloSanitaReport.GiornoMese(g) : "—";

        // ── Gestione idrica, scottature (fogli 37–44) ───────────────────────────────────────────────────────

        private void SlideIdrica(string titolo, string chiave)
        {
            if (_d.Campo is not { } campo)
            {
                Ppt.Testo(titolo, null, [T("campo.non-disponibile")]);
                return;
            }
            var a = _d.Annata;
            var rilievi = campo.Rilievi ?? [];
            switch (chiave)
            {
                case "apici":
                    var scala = CalcoloStatoIdricoReport.Scala;
                    Ppt.Quadranti(titolo, T("apici.titolo"), CalcoloStatoIdricoReport.TreAnni(rilievi, a).Select(x => new QuadranteReport(
                        T("apici.anno", x.Anno), scala.Minimo, scala.Massimo, scala.Fasce.Select(f => new FasciaReport(f.Da, f.A, f.Colore)).ToList(),
                        x.Valore, x.Classe is { } k ? T("apici.testo", T($"apici.{k}")) : null, N(x.Valore, "N2"))).ToList());
                    break;
                case "apici-storico":
                    Linee(titolo, T("apici.storico"), CalcoloStatoIdricoReport.Storico(rilievi, a));
                    break;
                case "prealba":
                    Linee(titolo, null, CalcoloStatoIdricoReport.PreAlbaPerVigna(rilievi, campo.Vigneti, a));
                    break;
                case "prealba-storico":
                    Linee(titolo, null, CalcoloStatoIdricoReport.PreAlbaStorico(rilievi, a));
                    break;
                case "suolo":
                    var s = CalcoloCampoReport.Suolo(campo.Suolo);
                    Ppt.Tabella(titolo,
                        $"{T("suolo.capacita")}: {N(s.CapacitaDiCampo)} · {T("suolo.awc")}: {N(s.AcquaDisponibile)} · {T("suolo.awc-vigneto")}: {N(s.AwcRealeVigneto)}",
                    [
                        new(T("suolo.vigna"), Peso: 1.4), new(T("suolo.vigneto"), Peso: 1.6), new(T("suolo.capacita"), true), new(T("suolo.awc"), true),
                        new(T("suolo.appassimento"), true), new(T("suolo.profondita"), true), new(T("suolo.pietrosita"), true),
                        new(T("suolo.awc-vigneto"), true), new(T("suolo.awc-vigna"), true), new(T("suolo.conducibilita"), true),
                    ],
                    campo.Suolo.Select(v => (IReadOnlyList<string?>)
                    [
                        v.Vigna, v.Nome, N(v.CapacitaDiCampo), N(v.AcquaDisponibile), N(v.PuntoDiAppassimento), N(v.Profondita, "N2"),
                        N(v.Pietrosita), N(v.AwcRealeVigneto), N(v.AwcRealeVigna), N(v.Conducibilita, "N2"),
                    ]).ToList(), T("suolo.nessuno"));
                    break;
                default:
                    var sc = CalcoloCampoReport.Scottature(chiave == "scottature-media" ? campo.ScottatureVista : campo.ScottatureRilievi,
                        campo.Superfici, a);
                    // Task #204: lo scatterplot di Qlik (esposizione × orientamento del vigneto, l'intensità accanto al nome);
                    // senza esposizione/orientamento in anagrafica resta la tabella.
                    var anagrafica = campo.Vigneti.ToDictionary(v => v.IdVigneto);
                    var puntiSc = sc.Vigneti.Where(v => anagrafica.GetValueOrDefault(v.IdVigneto) is { Esposizione: not null, Orientamento: not null })
                        .Select(v => new PuntoReport(anagrafica[v.IdVigneto].Esposizione!.Value, anagrafica[v.IdVigneto].Orientamento!.Value,
                            $"{Nome(v.IdVigneto)} {N(v.Intensita)}")).ToList();
                    if (puntiSc.Count > 0)
                    {
                        Ppt.Dispersione(titolo, T("scottature.grafico"), T("scottature.esposizione"), T("scottature.orientamento"),
                            [new SerieXYReport(T("scottature.intensita"), puntiSc, "C0392B")],
                            [
                                new(T("scottature.danno"), sc.Danno.ToString("P1", _c)), new(T("scottature.diffusione"), sc.Diffusione.ToString("P1", _c)),
                                new(T("scottature.rilievi"), sc.Righe.ToString(_c)),
                            ], Vuota);
                        break;
                    }
                    Ppt.Tabella(titolo,
                        $"{T("scottature.danno")}: {sc.Danno.ToString("P1", _c)} · {T("scottature.diffusione")}: {sc.Diffusione.ToString("P1", _c)} · {T("scottature.rilievi")}: {sc.Righe}",
                        [new(T("suolo.vigneto"), Peso: 2), new(T("scottature.intensita"), true), new(T("scottature.diffusione-max"), true)],
                        sc.Vigneti.Select(v => (IReadOnlyList<string?>)[Nome(v.IdVigneto), N(v.Intensita), N(v.Diffusione)]).ToList(), Vuota);
                    break;
            }
        }

        private void Linee(string titolo, string? nota, IReadOnlyList<LineaStatoIdrico> linee)
        {
            var (voci, serie) = PerSettimana(linee.Select(l => (l.Nome, l.PerSettimana)));
            Ppt.GraficoSerie(titolo, nota, voci, serie, true, [], Vuota);
        }

        // ── Vigoria, chioma, SFE (fogli 45–49) ──────────────────────────────────────────────────────────────

        private void SlideVigoria(string titolo, string chiave)
        {
            if (_d.Campo is not { } campo)
            {
                Ppt.Testo(titolo, null, [T("campo.non-disponibile")]);
                return;
            }
            var a = _d.Annata;
            switch (chiave)
            {
                case "vigoria":
                    var anni = CalcoloVigoriaReport.TreAnni(campo.Vigoria, a);
                    var c = anni[0];
                    var scala = CalcoloVigoriaReport.Scala;
                    Ppt.Quadranti(titolo,
                        $"{T("vigoria.legno")}: {N(c.LegnoPotatura, "N2")} · {T("vigoria.sfe")}: {N(c.SfePerMetro, "N2")} · {T("vigoria.visiva")}: {N(c.StatoVegetativo, "N2")} · {T("vigoria.chioma")}: {N(c.GestioneChioma, "N2")}",
                        anni.Select(x => new QuadranteReport(T("vigoria.media", x.Anno), scala.Minimo, scala.Massimo,
                            scala.Fasce.Select(f => new FasciaReport(f.Da, f.A, f.Colore)).ToList(), x.Punteggio,
                            x.Classe is { } k ? T($"classe.{k}") : null, N(x.Punteggio, "N2"))).ToList());
                    break;
                case "vigoria-2":
                    var perVigneto = CalcoloVigoriaReport.PerVignetoEAnno(campo.Vigoria);
                    // Task #204: il distributionplot di Qlik (vigneto, un punto per anno).
                    var anniTabella = perVigneto.Keys.Select(k => k.Anno).Where(x => x <= a && x > a - 10).Distinct().Order().ToList();
                    var idsVigoria = perVigneto.Keys.Select(k => k.IdVigneto).Distinct().OrderBy(Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
                    Ppt.GraficoSerie(titolo, T("vigoria.per-vigneto"), idsVigoria.Select(Nome).ToList(),
                        anniTabella.Select(x => new SerieReport(x.ToString(CultureInfo.InvariantCulture),
                            idsVigoria.Select(id => perVigneto.TryGetValue((id, x), out var p) ? (double?)p : null).ToList())).ToList(),
                        false, [], T("vigoria.nessuna"), "0.0", soloPunti: true);
                    break;
                case "chioma-2" or "chioma-1":
                    // Task #204: lo scatterplot di Qlik SFE/produzione per vigneto, con i KPI nelle tessere (ultimi due anni: l'anno
                    // prima nel dettaglio).
                    var xa = CalcoloChiomaReport.Anno(campo, a);
                    var xp = CalcoloChiomaReport.Anno(campo, a - 1);
                    var dueAnni = chiave == "chioma-2";
                    string? Prima(string valore) => dueAnni ? $"{a - 1}: {valore}" : null;
                    var tessere = new TesseraReport[]
                    {
                        new($"{a} · {T("chioma.sfe")}", N(xa.Sfe, "N2"),
                            dueAnni ? Prima(N(xp.Sfe, "N2")) : xa.SfeDevSt is { } d ? T("chioma.devst", d.ToString("N2", _c)) : null),
                        new(T("chioma.altezza"), N(xa.AltezzaCm, "N0"), Prima(N(xp.AltezzaCm, "N0"))),
                        new(T("chioma.spazi"), N(xa.SpaziVuoti, "P0"), Prima(N(xp.SpaziVuoti, "P0"))),
                        new(T("chioma.ordine"), N(xa.OrdineGermogli, "P0"), Prima(N(xp.OrdineGermogli, "P0"))),
                    };
                    var punti = CalcoloChiomaReport.SfeProduzione(campo, a);
                    Ppt.Dispersione(titolo, dueAnni ? T("chioma.ultimi-due") : T("chioma.sfe-produzione"), T("chioma.sfe"), T("chioma.produzione"),
                        [new SerieXYReport(T("chioma.sfe-produzione"), punti.Select(p => new PuntoReport(p.Sfe, p.Produzione, Nome(p.IdVigneto))).ToList())],
                        tessere, Vuota);
                    break;
                default:
                    Ppt.Tabella(titolo, null,
                    [
                        new(T("sfe.data")), new(T("suolo.vigneto"), Peso: 1.6), new(T("sfe.sottovigneto"), Peso: 1.2), new(T("sfe.vitigno"), Peso: 1.2),
                        new(T("sfe.eta"), true, 0.7), new(T("sfe.gestione"), Peso: 1.2), new(T("sfe.per-vite"), true), new(T("chioma.altezza"), true),
                        new(T("sfe.larghezza"), true), new(T("chioma.spazi"), true), new(T("chioma.ordine"), true), new(T("sfe.distanza"), true),
                    ],
                    CalcoloChiomaReport.Fattori(campo, a).Select(f => (IReadOnlyList<string?>)
                    [
                        f.Data.ToString("d", _c), Nome(f.IdVigneto),
                        f.IdSubVigneto is int sub && campo.SottoVigneti?.TryGetValue(sub, out var ns) == true ? ns : f.IdSubVigneto?.ToString(CultureInfo.InvariantCulture),
                        f.Vitigno, f.EtaImpianto?.ToString(CultureInfo.InvariantCulture), f.GestioneChioma, N(f.SfePerVite, "N2"), N(f.AltezzaChioma, "N0"),
                        N(f.LarghezzaChioma, "N0"), N(f.SpaziVuoti, "N2"), N(f.OrdineGermogli, "N2"), N(f.DistanzaPiante, "N2"),
                    ]).ToList(), T("sfe.nessuno"));
                    break;
            }
        }

        // ── Produzione, fallanze (fogli 50–55) ──────────────────────────────────────────────────────────────

        private void SlideProduzione(string titolo, string chiave)
        {
            if (_d.Campo is not { } campo)
            {
                Ppt.Testo(titolo, null, [T("campo.non-disponibile")]);
                return;
            }
            var a = _d.Annata;
            var perAnno = CalcoloProduzioneReport.PerVignetoEAnno(campo);
            var vigna = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Vigna);
            switch (chiave)
            {
                case "prod-vitigno":
                    var vitigni = CalcoloProduzioneReport.PerVitigno(campo, a);
                    Ppt.GraficoSerie(titolo, T("prod.vitigno"), vitigni.Select(v => v.Vitigno).ToList(),
                        [new SerieReport(T("prod.totale"), vitigni.Select(v => (double?)v.Quintali).ToList())], false, [], T("prod.nessuna"), "0");
                    break;
                case "prod-vigneto":
                    // Task #204: il barchart di Qlik (vigna e vigneto, q/ha dell'annata).
                    var dellAnno = perAnno.Where(p => p.Anno == a && p.QuintaliEttaro is not null)
                        .OrderBy(p => vigna.GetValueOrDefault(p.IdVigneto) ?? "", StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(p => Nome(p.IdVigneto), StringComparer.CurrentCultureIgnoreCase).ToList();
                    Ppt.GraficoSerie(titolo, T("prod.vigneto"), dellAnno.Select(p => Nome(p.IdVigneto)).ToList(),
                        [new SerieReport(T("prod.ettaro"), dellAnno.Select(p => p.QuintaliEttaro).ToList())], false, [], T("prod.nessuna"), "0");
                    break;
                case "prod-vigneto-anno":
                    // Task #204: lo scatterplot di Qlik (età del vigneto × q/ha, un punto per vigneto e vendemmia).
                    var puntiProd = perAnno.Where(p => p.Anno <= a && p.Eta is not null && p.QuintaliEttaro is not null)
                        .Select(p => new PuntoReport(p.Eta!.Value, p.QuintaliEttaro!.Value, $"{Nome(p.IdVigneto)} {T("prod.ven", p.Anno)}")).ToList();
                    Ppt.Dispersione(titolo, T("prod.vigneto-anno"), T("prod.eta"), T("prod.ettaro"),
                        [new SerieXYReport(T("prod.ettaro"), puntiProd)], [], T("prod.nessuna"), "0");
                    break;
                case "prod-confronto":
                    // Task #204: il distributionplot di Qlik (anno, un punto per vigneto); con molti vigneti un colore solo.
                    var anni = perAnno.Select(p => p.Anno).Where(x => x <= a).Distinct().Order().ToList();
                    var celle = perAnno.ToDictionary(p => (p.IdVigneto, p.Anno), p => p.QuintaliEttaro);
                    var idsProd = perAnno.Select(p => p.IdVigneto).Distinct().OrderBy(Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
                    Ppt.GraficoSerie(titolo, T("prod.confronto"), anni.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
                        idsProd.Select(id => new SerieReport(Nome(id), anni.Select(x => celle.TryGetValue((id, x), out var q) ? q : null).ToList(),
                            idsProd.Count > 15 ? "3C3950" : null)).ToList(),
                        false, [], T("prod.nessuna"), "0", soloPunti: true);
                    break;
                case "prod-stime":
                    Ppt.Tabella(titolo, T("prod.stime"),
                    [
                        new(T("suolo.vigna"), Peso: 1.3), new(T("suolo.vigneto"), Peso: 1.6), new(T("prod.ha"), true, 0.6), new(T("prod.reale"), true),
                        new(T("prod.stima"), true), new(T("prod.differenza"), true), new(T("prod.differenza-ha"), true), new(T("prod.fallanze"), true),
                        new(T("prod.allevamento"), true),
                    ],
                    CalcoloProduzioneReport.Scostamenti(campo, a).Select(s => (IReadOnlyList<string?>)
                    [
                        s.Vigna, s.Nome, N(s.Ettari, "N2"), N(s.Reale, "N0"), N(s.Stima, "N0"), N(s.Differenza, "N0"), N(s.DifferenzaEttaro, "N0"),
                        N(s.PercFallanze), N(s.PercInAllevamento),
                    ]).ToList(), T("prod.nessuna"));
                    break;
                default:
                    var f = CalcoloFallanzeReport.Calcola(campo, a);
                    var ordinati = f.Vigneti.OrderBy(v => Nome(v.IdVigneto), StringComparer.CurrentCultureIgnoreCase).ToList();
                    Ppt.GraficoSerie(titolo, T("fallanze.capitozzate"), ordinati.Select(v => Nome(v.IdVigneto)).ToList(),
                    [
                        new(T("fallanze.fallanze"), ordinati.Select(v => v.Fallanze).ToList(), "C0392B"),
                        new(T("fallanze.allevamento"), ordinati.Select(v => v.InAllevamento).ToList(), "8DC63E"),
                        new(T("fallanze.esca"), ordinati.Select(v => v.Esca).ToList(), "3C3950"),
                    ], false,
                    [
                        new(T("fallanze.ettari"), N(f.Ettari, "N2")), new(T("fallanze.piante"), N(f.PianteMancanti, "N0")),
                        new(T("fallanze.ettari-mancanti"), N(f.EttariMancanti, "N2")),
                    ], T("fallanze.nessuna"));
                    break;
            }
        }

        // ── Indice Bigot (fogli 56–70): le slide del pilota, coi titoli del report fine stagione ─────────────────

        private void SlideIndice(string chiave)
        {
            if (_indiceFatto)
            {
                return;
            }
            _indiceFatto = true;
            var titoli = new List<string>
            {
                Titolo("ib-istogramma"), Titolo("ib-vigneti"), Titolo("ib-vigna"), Titolo("ib-vigna"),
                Titolo("ib-indicatori-1"), Titolo("ib-indicatori-2"), Titolo("ib-indicatori-3"),
            };
            titoli.AddRange(Storici.Select(Titolo));
            var filtro = new FiltroIndice();
            if (_d.Indice is { Pronto: true } report && CalcoloIndiceBigot.AnnoScelto(filtro.Righe(report), _d.Annata) is { } anno)
            {
                PowerPointIndiceBigot.SlideIndice(Ppt, report, filtro, anno, _adesso, titoli, _lingua == LinguaReport.En);
                return;
            }
            // Senza indice le slide ci sono lo stesso (una per foglio di Qlik), con la spiegazione.
            foreach (var k in Indice)
            {
                Ppt.Testo(Titolo(k), null, [T("slide.ib-vuoto")]);
            }
        }
    }
}
