using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>I dati del report settimanale di un'azienda: quelli della pagina (rilievi fino alla settimana e campo dello snapshot).</summary>
public sealed record DatiSettimanale(string Azienda, SettimanaReport Settimana, RilieviSettimanaDto Rilievi, CampoReportFineStagioneDto? Campo);

/// <summary>
/// Il PowerPoint del report settimanale (task #213) al posto delle automazioni Qlik PPT_REPORT_SETTIAMANALE (18 slide) e
/// PPT_REPORT_ENG (12): una slide per foglio nell'ordine di <see cref="SezioniSettimanale"/>, con grafici e quadranti veri
/// di <see cref="PresentazionePptx"/> e i numeri di <see cref="CalcoloSettimanale"/> (gli stessi della pagina).
/// </summary>
public static class PowerPointSettimanale
{
    /// <summary>
    /// Nome come le automazioni: «{AAMMGG} - {azienda} - Report settimanale» (data di oggi) e
    /// «{WEEK_REF} - {azienda} - Weekly Report»; i caratteri non ammessi nei nomi di file diventano «_».
    /// </summary>
    public static string NomeFile(string azienda, SettimanaReport settimana, LinguaReport lingua, DateTimeOffset adesso, string estensione = ".pptx")
    {
        var oggi = TimeZoneInfo.ConvertTime(adesso, ProgettoMappe.Web.Tematismi.TematismiBigot.FusoItalia);
        var nome = lingua == LinguaReport.En
            ? $"{SettimanaQlik.Etichetta(settimana.Numero)} - {azienda} - Weekly Report"
            : $"{oggi.ToString("yyMMdd", CultureInfo.InvariantCulture)} - {azienda} - Report settimanale";
        var vietati = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        return new string(nome.Select(c => vietati.Contains(c) ? '_' : c).ToArray()).Trim() + estensione;
    }

    public static byte[] Crea(DatiSettimanale dati, LinguaReport lingua, DateTimeOffset adesso)
    {
        var g = new Generatore(dati, lingua, adesso);
        foreach (var sezione in SezioniSettimanale.Della(lingua))
        {
            g.Aggiungi(sezione.Chiave);
        }
        return g.Ppt.Salva();
    }

    private sealed class Generatore
    {
        private readonly DatiSettimanale _d;
        private readonly LinguaReport _lingua;
        private readonly DateTimeOffset _adesso;
        private readonly CultureInfo _c;
        private readonly List<RilievoSettimanaDto> _sel;
        private readonly IReadOnlyDictionary<int, double?> _superfici;
        private readonly Dictionary<int, VignetoFineStagioneDto> _vigneti;
        private readonly List<int> _settimane;
        private readonly List<string> _etichette;
        private readonly int _anno;

        public PresentazionePptx Ppt { get; }

        public Generatore(DatiSettimanale d, LinguaReport lingua, DateTimeOffset adesso)
        {
            _d = d;
            _lingua = lingua;
            _adesso = adesso;
            _c = TestiReport.Cultura(lingua);
            _anno = d.Settimana.Anno;
            _sel = d.Rilievi.Rilievi.Where(r => d.Settimana.Contiene(r.Data)).ToList();
            _superfici = d.Campo?.Superfici ?? new Dictionary<int, double?>();
            _vigneti = (d.Campo?.Vigneti ?? []).ToDictionary(v => v.IdVigneto);
            _settimane = Enumerable.Range(1, d.Settimana.Numero).ToList();
            _etichette = _settimane.Select(SettimanaQlik.Etichetta).ToList();
            Ppt = new PresentazionePptx($"{d.Azienda} - {T("titolo")}",
                $"{T("titolo")} · {d.Azienda} · {d.Settimana.Etichetta(lingua)} · {T("generato.il", Oggi.ToString("dd/MM/yyyy", _c))}");
        }

        private DateTime Oggi => TimeZoneInfo.ConvertTime(_adesso, ProgettoMappe.Web.Tematismi.TematismiBigot.FusoItalia).DateTime;

        private string T(string chiave, params object[] valori) => TestiSettimanale.Testo(_lingua, chiave, valori);

        private string Al => T("cruscotto.al", _d.Settimana.A.ToString("dd/MM", CultureInfo.InvariantCulture));

        private string NomeTipo(int tipo) =>
            _d.Rilievi.NomiTipi.TryGetValue(tipo, out var n) && n.Length > 0 ? n : tipo == 0 ? T("rilievi.bbch") : tipo.ToString(CultureInfo.InvariantCulture);

        private static IReadOnlyList<FasciaReport> Fasce(ScalaQuadrante s) => s.Fasce.Select(f => new FasciaReport(f.Da, f.A, f.Colore)).ToList();

        private static QuadranteReport Quadrante(string titolo, ScalaQuadrante s, double? valore, string? testo, string? sotto = null) =>
            new(titolo, s.Minimo, s.Massimo, Fasce(s), valore, testo, sotto);

        public void Aggiungi(string chiave)
        {
            switch (chiave)
            {
                case "sezione.copertina":
                    Ppt.Copertina(T("titolo"), _d.Azienda,
                        [_d.Settimana.Etichetta(_lingua), Al, _d.Rilievi.Snapshot is { } s
                            ? T("dati.snapshot", TimeZoneInfo.ConvertTime(s, ProgettoMappe.Web.Tematismi.TematismiBigot.FusoItalia).ToString("g", _c))
                            : T("dati.diretti")], []);
                    break;

                case "sezione.dashboard":
                    var c = CalcoloSettimanale.Cruscotto(_sel, _vigneti.Count);
                    Ppt.Grafico(T(chiave), Al, T("cruscotto.rilievi"),
                        c.PerTipo.Select(t => new BarraReport(NomeTipo(t.Tipo), t.Rilievi)).ToList(),
                        [
                            new(T("cruscotto.rilievi"), c.Rilievi.ToString("N0", _c)),
                            new(T("cruscotto.organi"), c.Organi.ToString("N0", _c)),
                            new(T("cruscotto.monitorati"), c.Monitorati.ToString(_c)),
                            new(T("cruscotto.totali"), c.VignetiTotali.ToString(_c)),
                            new(T("cruscotto.percentuale"), c.VignetiTotali > 0 ? ((double)c.Monitorati / c.VignetiTotali).ToString("P0", _c) : "—"),
                        ], T("vuoto"), formato: "0");
                    break;

                case "sezione.sanita-grappoli" or "sezione.sanita-foglia":
                    var kpi = CalcoloSettimanale.Sanita(_sel, _superfici,
                        chiave == "sezione.sanita-grappoli" ? CalcoloSettimanale.Grappolo : CalcoloSettimanale.Foglia);
                    Ppt.Tabella(T(chiave), Al,
                        [new("", Peso: 2), new(T("kpi.intensita"), true), new(T("kpi.diffusione"), true), new(T("kpi.senza-danno"), true), new(T("kpi.copertura"), true)],
                        kpi.Select(k => (IReadOnlyList<string?>)[T(k.Avversita.Chiave), k.Intensita.ToString("P1", _c), k.Diffusione.ToString("P1", _c),
                            k.SenzaDanno?.ToString("P0", _c) ?? T("kpi.in-attesa"), k.Copertura.ToString("P0", _c)]).ToList(), T("vuoto"));
                    break;

                case "sezione.grafico-grappoli" or "sezione.grafico-foglia":
                    var danni = CalcoloSettimanale.Sanita(_sel, _superfici,
                        chiave == "sezione.grafico-grappoli" ? CalcoloSettimanale.Grappolo : CalcoloSettimanale.Foglia);
                    Ppt.Grafico(T(chiave, _anno), Al, T("kpi.intensita") + " %",
                        danni.Select(k => new BarraReport(T(k.Avversita.Chiave), k.Intensita * 100)).ToList(), [], T("vuoto"), formato: "0.00");
                    break;

                case "sezione.fenologia":
                    var fasi = _d.Campo?.Sanita?.Fasi ?? [];
                    var periodi = CalcoloSettimanale.PeriodiFase(_anno);
                    Ppt.Tabella(T(chiave), fasi.Count == 0 ? T("fase.nessuna") : null,
                        [new("", Peso: 2), new(T("fase.annata", _anno), true), new(T("fase.precedente", _anno - 1), true),
                            new(T("fase.storico5"), true), new(T("fase.storico10"), true)],
                        fasi.Count == 0 ? [] : Enumerable.Range(1, 4).Select(f => (IReadOnlyList<string?>)new[] { T($"fase.{f}") }
                            .Concat(periodi.Select(p => CalcoloSettimanale.GiornoFase(fasi, f, p.Anni) is { } gg ? CalcoloSanitaReport.GiornoMese(gg) : "—"))
                            .ToList()).ToList(), T("fase.nessuna"));
                    break;

                case "sezione.bbch-vitigno-foglie" or "sezione.bbch-vitigno-grappoli":
                    var codiciV = chiave.EndsWith("foglie") ? CalcoloSettimanale.BbchFoglie : CalcoloSettimanale.BbchGrappoli;
                    Ppt.GraficoSerie(T(chiave), T("settimana"), _etichette,
                        CalcoloSettimanale.BbchPerVitigno(_sel, codiciV, _vigneti.ToDictionary(v => v.Key, v => v.Value.Vitigno), _anno, _settimane)
                            .Select(s => new SerieReport(s.Nome, s.Valori)).ToList(), true, [], T("vuoto"));
                    break;

                case "sezione.bbch-storico-foglie" or "sezione.bbch-storico-grappoli":
                    var codici = chiave.EndsWith("foglie") ? CalcoloSettimanale.BbchFoglie : CalcoloSettimanale.BbchGrappoli;
                    var tutti = _d.Rilievi.Rilievi;
                    Ppt.GraficoSerie(T(chiave), T("settimana"), _etichette,
                    [
                        new(T("bbch.anno"), CalcoloSettimanale.BbchPerSettimana(_sel, codici, a => a == _anno, _settimane), "8DC63E"),
                        new(T("bbch.precedente"), CalcoloSettimanale.BbchPerSettimana(tutti, codici, a => a == _anno - 1, _settimane), "5F727F"),
                        new(T("bbch.storica"), CalcoloSettimanale.BbchPerSettimana(tutti, codici, a => a >= _anno - 5 && a <= _anno - 1, _settimane), "3C3950"),
                    ], true, [], T("vuoto"));
                    break;

                case "sezione.vigoria":
                    Ppt.Quadranti(T(chiave), null, new[] { _anno - 2, _anno - 1, _anno }.Select(a =>
                    {
                        var v = CalcoloSettimanale.Vigoria(_d.Campo?.Vigoria ?? [], a);
                        return Quadrante(T("vigoria.media", a), CalcoloSettimanale.ScalaVigoria, v, v?.ToString("N2", _c),
                            CalcoloSettimanale.ClasseVigoria(v) is { } cl ? T(cl) : null);
                    }).ToList());
                    break;

                case "sezione.germogli":
                    Ppt.Testo(T(chiave), null, [T("in.attesa.dati")]);
                    break;

                // Una slide per foglio come l'automazione: i contachilometri dei fogli con un grafico diventano tessere accanto.
                case "sezione.deficit-idrico":
                    Ppt.GraficoSerie(T(chiave), T("idrico.andamento"), _etichette,
                        CalcoloSettimanale.StatoIdricoPerVigna(_sel, _vigneti.ToDictionary(v => v.Key, v => v.Value.Vigna), _settimane)
                            .Select(s => new SerieReport(s.Nome, s.Valori)).ToList(), true,
                        new[] { _anno, _anno - 1, _anno - 2 }.Select(a =>
                        {
                            var v = CalcoloSettimanale.StatoIdrico(a == _anno ? _sel : _d.Rilievi.Rilievi, a);
                            return new TesseraReport(T("idrico.titolo", a), v?.ToString("N2", _c) ?? "—",
                                CalcoloStatoIdricoReport.Classe(v) is { } cl ? T("idrico." + cl) : null);
                        }).ToList(), T("vuoto"), "0.0");
                    break;

                case "sezione.tignole":
                    Ppt.GraficoSerie(T(chiave), T("settimana"), _etichette,
                        CalcoloSettimanale.Catture(_sel, _settimane).Select(s => new SerieReport(NomeTipo(s.Tipo), s.Valori)).ToList(),
                        true, [], T("vuoto"));
                    break;

                case "sezione.tignole-larve":
                    Ppt.Grafico(T(chiave), T("tignole.diffusione"), "%",
                        CalcoloSettimanale.DiffusioneTignole(_sel).OrderByDescending(x => x.Value)
                            .Select(x => new BarraReport(_vigneti.TryGetValue(x.Key, out var v) ? v.Nome : x.Key.ToString(CultureInfo.InvariantCulture), x.Value))
                            .ToList(),
                        CalcoloSettimanale.Generazioni.Select(gen =>
                        {
                            var (valore, copertura) = CalcoloSettimanale.Generazione(_sel, _superfici, gen.Codici);
                            return new TesseraReport(T(gen.Chiave), valore.ToString("N1", _c), T("tignole.controllati", copertura.ToString("P0", _c)));
                        }).ToList(), T("vuoto"));
                    break;

                case "sezione.esca":
                    var esca = CalcoloSettimanale.Esca(_sel, _d.Campo?.Collezione ?? [], _anno);
                    Ppt.Tessere(T(chiave), Al,
                        new[] { new TesseraReport(T("esca.rilievi"), esca.Rilievi.ToString(_c)), new(T("esca.piante"), esca.Piante.ToString("N0", _c)) }
                            .Concat(CalcoloSettimanale.ValoriEsca.Select(v => new TesseraReport(T("esca." + v), esca.PerValore.GetValueOrDefault(v).ToString("N0", _c))))
                            .ToList(), T("vuoto"));
                    break;

                case "sezione.considerazioni":
                    Ppt.Testo(T(chiave), null, [T("considerazioni.testo")]);
                    break;
            }
        }
    }
}
