using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.AnalisiSuolo;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Reportistica;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportAnalisiSuolo;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Reportistica R4 (task #418): cataloghi con id stabili di fine stagione, Indice Bigot e analisi del suolo, scelte e commenti
/// applicati ai loro PowerPoint, Api aperta ai tre report (il suolo sulle aziende con analisi, edizione «tutti»).
/// </summary>
public sealed class ReportisticaR4Tests : IDisposable
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "reportistica-r4-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_cartella))
            Directory.Delete(_cartella, true);
    }

    private static ScelteApplicate Scelte(IEnumerable<string> nascosti, params (string Id, string Testo)[] commenti) =>
        new(nascosti.ToHashSet(), commenti.ToDictionary(c => c.Id, c => c.Testo));

    /// <summary>Titolo e testo di ogni slide, dopo aver controllato che il file sia un PowerPoint valido.</summary>
    private static List<(string Titolo, string Testo)> Slide(byte[] file)
    {
        using var documento = PresentationDocument.Open(new MemoryStream(file), false);
        var errori = new OpenXmlValidator().Validate(documento).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errori.Count == 0, string.Join("\n", errori.Take(10)));
        var presentazione = documento.PresentationPart!;
        return presentazione.Presentation.SlideIdList!.Elements<P.SlideId>().Select(id =>
        {
            var parte = (SlidePart)presentazione.GetPartById(id.RelationshipId!);
            var titolo = parte.Slide.Descendants<P.Shape>()
                .FirstOrDefault(s => s.NonVisualShapeProperties!.NonVisualDrawingProperties!.Name == "Titolo");
            return (titolo is null ? "" : string.Concat(titolo.Descendants<A.Text>().Select(t => t.Text)),
                string.Join("|", parte.Slide.Descendants<A.Text>().Select(t => t.Text)));
        }).ToList();
    }

    /// <summary>
    /// Gli id si scrivono una volta e non cambiano più: le scelte e i commenti salvati li usano. Se questo test fallisce
    /// perché un id è cambiato, rimettere quello vecchio (si cambia il titolo, non l'id).
    /// </summary>
    [Fact]
    public void Cataloghi_IdStabiliUniciEValidi()
    {
        Assert.Equal(
        [
            "rfs.copertina", "rfs.caratterizzazione", "rfs.gestione-idrica", "rfs.scottature", "rfs.vigoria", "rfs.nutrizione", "rfs.sfe",
            "rfs.produzione", "rfs.fallanze", "rfs.potature", "rfs.sanita", "rfs.indice-bigot", "rfs.maturazione", "rfs.ad-hoc",
            "rfs.degustazione",
        ], CatalogoFineStagione.Elementi.Select(e => e.Id));
        Assert.Equal(
        [
            "ib.copertina", "ib.indice", "ib.vigneti", "ib.per-vigna", "ib.per-vitigno", "ib.parametri", "ib.storico", "ib.sottovigneti",
            "ib.note",
        ], CatalogoIndiceBigot.Elementi.Select(e => e.Id));
        Assert.Equal(
        [
            "suolo.generale", "suolo.fisica", "suolo.reazione", "suolo.sostanza-organica", "suolo.scambio", "suolo.fosforo",
            "suolo.microelementi", "suolo.idrosolubili", "suolo.considerazioni",
        ], CatalogoAnalisiSuolo.Elementi.Where(e => e.Sezione is null).Select(e => e.Id));
        // 49 fogli con un id proprio (le tre Considerazioni sono la loro sezione).
        Assert.Equal(49, CatalogoAnalisiSuolo.Elementi.Count(e => e.Sezione is not null));
        Assert.Contains(CatalogoAnalisiSuolo.Elementi, e => e.Id == "suolo.reazione.ph-in-h2o");
        Assert.Contains(CatalogoAnalisiSuolo.Elementi, e => e.Id == "suolo.reazione.carbonati-totali-caco3");
        Assert.Contains(CatalogoAnalisiSuolo.Elementi, e => e.Id == "suolo.sostanza-organica.sostanza-organica-limiti");
        Assert.Contains(CatalogoAnalisiSuolo.Elementi, e => e.Id == "suolo.microelementi.ferro-fe-assimilabile-edta-ppm");
        Assert.Contains(CatalogoAnalisiSuolo.Elementi, e => e.Id == "suolo.fisica.tessitura-usda");

        foreach (var (report, catalogo) in new[] { ("rfs", CatalogoFineStagione.Elementi), ("ib", CatalogoIndiceBigot.Elementi), ("suolo", CatalogoAnalisiSuolo.Elementi) })
        {
            Assert.Equal(catalogo.Count, catalogo.Select(e => e.Id).Distinct().Count());
            Assert.All(catalogo, e => Assert.True(ReportisticaController.IdValido(report, e.Id), e.Id));
            Assert.Single(catalogo, e => !e.Nascondibile);
            Assert.Contains(report, ReportisticaController.Report);
        }
        Assert.Equal(CalcoloAnalisiSuolo.Fogli.Count, CatalogoAnalisiSuolo.IdFogli.Count);
    }

    [Fact]
    public void FineStagione_OgniSlideHaUnaSezioneDelCatalogo()
    {
        var ids = CatalogoFineStagione.Elementi.Select(e => e.Id).ToHashSet();
        Assert.All(PowerPointFineStagione.SlideIt.Concat(PowerPointFineStagione.SlideEn),
            s => Assert.Contains(CatalogoFineStagione.SezioneDellaSlide(s), ids));
        Assert.Equal("rfs.caratterizzazione", CatalogoFineStagione.SezioneDellaSlide("pioggia-3"));
        Assert.Equal("rfs.sfe", CatalogoFineStagione.SezioneDellaSlide("chioma-1"));
        Assert.Equal("rfs.indice-bigot", CatalogoFineStagione.SezioneDellaSlide("ib-storico-sfe"));
        Assert.Equal(["rfs.nutrizione", "rfs.potature", "rfs.maturazione", "rfs.ad-hoc", "rfs.degustazione"],
            SezioniReport.Tutte.Where(CatalogoFineStagione.Manuale).Select(CatalogoFineStagione.Id));
    }

    [Fact]
    public void FineStagione_PowerPoint_SezioniNascosteCommentiESezioniDelConsulente()
    {
        var dati = new DatiFineStagione("Prova", 2026, null, null, null);
        var scelte = Scelte(["rfs.sanita", "rfs.potature"],
            ("rfs.copertina", "Annata difficile."), ("rfs.caratterizzazione", "Luglio molto caldo.\nPoca pioggia."),
            ("rfs.nutrizione", "Concimazione a marzo."), ("rfs.potature", "Nascosta: non esce."), ("rfs.sanita", "Nascosta anche questa."));

        var slide = Slide(PowerPointFineStagione.Crea(dati, LinguaReport.It, Adesso, scelte));

        var attese = PowerPointFineStagione.SlideIt.Count(s => CatalogoFineStagione.SezioneDellaSlide(s) != "rfs.sanita") + 4;
        Assert.Equal(attese, slide.Count);
        Assert.Equal("Commento del consulente", slide[2].Titolo); // dopo copertina e sommario
        Assert.Contains("Annata difficile.", slide[2].Testo);
        Assert.DoesNotContain("X. ", slide[1].Testo); // la sanità sparisce dal sommario
        Assert.Contains("V. ", slide[1].Testo);
        var caratterizzazione = slide.FindIndex(s => s.Testo.Contains("Luglio molto caldo.", StringComparison.Ordinal));
        Assert.StartsWith("Commento del consulente · ", slide[caratterizzazione].Titolo);
        Assert.Equal("rfs.gestione-idrica", CatalogoFineStagione.SezioneDellaSlide("gestione-idrica"));
        Assert.Contains("Concimazione a marzo.", slide[^1].Testo); // sezione scritta a mano, in fondo dopo il suo divisore
        Assert.StartsWith("V. ", slide[^2].Titolo);
        Assert.DoesNotContain(slide, s => s.Testo.Contains("Nascosta", StringComparison.Ordinal));

        // Senza scelte il file è quello di prima.
        Assert.Equal(PowerPointFineStagione.SlideIt.Count, Slide(PowerPointFineStagione.Crea(dati, LinguaReport.It, Adesso)).Count);
    }

    private static IndiceBigotReportDto ReportIndice()
    {
        string[] nove = ["VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA", "ETA VIGNETO"];
        var righe = nove.Select(p => new RigaIndiceParametroDto(2025, 1, null, p, null, 2, 3, 4, null, 8))
            .Concat(nove.Select(p => new RigaIndiceParametroDto(2025, 2, null, p, null, 2, 5, 4, null, 6))).ToList();
        return new IndiceBigotReportDto(true, Adesso.AddHours(-10), 7, "Azienda", [new(1, "Vigna Alta")], [new(10, "Merlot")],
            [new(1, "Uno", 1, 10, 1000, 2000), new(2, "Due", 1, 10, 3000, 2010)], [], righe);
    }

    [Fact]
    public void IndiceBigot_PowerPoint_SezioniNascosteECommenti()
    {
        var prima = Slide(PowerPointIndiceBigot.Crea(ReportIndice(), new FiltroIndice(), 2025, Adesso));
        var scelte = Scelte(["ib.storico", "ib.note"], ("ib.copertina", "Buona annata."), ("ib.vigneti", "Il vigneto Due è sotto media."),
            ("ib.storico", "Nascosto: non esce."));

        var dopo = Slide(PowerPointIndiceBigot.Crea(ReportIndice(), new FiltroIndice(), 2025, Adesso, scelte));

        Assert.Equal(prima.Count - PowerPointIndiceBigot.Storici.Count - 1 + 2, dopo.Count);
        Assert.Equal("Commento del consulente", dopo[1].Titolo);
        var vigneti = dopo.FindIndex(s => s.Titolo.StartsWith("Vigneti nel", StringComparison.Ordinal));
        Assert.Equal("Commento del consulente · Vigneti nell'anno", dopo[vigneti + 1].Titolo);
        Assert.DoesNotContain(dopo, s => s.Titolo.StartsWith("Storico", StringComparison.Ordinal) || s.Titolo == "Note metodologiche");
        Assert.DoesNotContain(dopo, s => s.Testo.Contains("Nascosto", StringComparison.Ordinal));
    }

    [Fact]
    public void Suolo_PowerPoint_FogliEArgomentiNascosti_ConsiderazioniDalCommento()
    {
        var righe = CalcoloAnalisiSuolo.Righe(new AnalisiSuoloAziendaDto(5, "Azienda", [], []));
        var scelte = ScelteApplicate.Da(CatalogoAnalisiSuolo.Elementi, new EdizioneReportDto("suolo", 5, "tutti", StatiReport.Bozza, null, null,
            [
                new SceltaReportDto("suolo.fosforo", false, false, 1, "a", Adesso),
                new SceltaReportDto("suolo.reazione.ph-in-kcl", false, true, 1, "a", Adesso),
            ],
            [
                new CommentoReportDto("suolo.generale", "it", "Suoli calcarei.", 1, "a", Adesso),
                new CommentoReportDto("suolo.considerazioni", "it", "Aumentare la sostanza organica.", 1, "a", Adesso),
                new CommentoReportDto("suolo.idrosolubili", "it", "Sodio alto in due punti.", 1, "a", Adesso),
            ], [], false), "it");

        Assert.Contains("suolo.fosforo.fosforo-p-assimilabile-olsen-ppm", scelte.Nascosti); // l'argomento porta via i suoi fogli
        var slide = Slide(PowerPointAnalisiSuolo.Crea("Azienda", righe, new SelezioneSuolo(), Adesso, scelte));

        // 52 − 4 (fosforo) − 1 (pH in KCL) − 3 riquadri vuoti + 1 considerazioni + generale + commento degli idrosolubili.
        Assert.Equal(52 - 4 - 1 - 3 + 1 + 1 + 1, slide.Count);
        Assert.Equal("Commento del consulente", slide[0].Titolo);
        Assert.Equal("Considerazioni", slide[^1].Titolo);
        Assert.Contains("Aumentare la sostanza organica.", slide[^1].Testo);
        Assert.Equal("Commento del consulente · Elementi idrosolubili", slide[^2].Titolo);
        Assert.DoesNotContain(slide, s => s.Titolo.Contains("Fosforo", StringComparison.Ordinal) || s.Titolo == "pH in KCL");

        Assert.Equal(52, Slide(PowerPointAnalisiSuolo.Crea("Azienda", righe, new SelezioneSuolo(), Adesso)).Count);
    }

    [Fact]
    public void Slug_DeiTitoliDelSuolo()
    {
        Assert.Equal("carbonati-totali-caco3", CatalogoAnalisiSuolo.Slug("Carbonati Totali - CaCO₃ (‰)"));
        Assert.Equal("ph-in-h2o", CatalogoAnalisiSuolo.Slug("pH in H₂O"));
        Assert.Equal("conducibilita-ms-cm", CatalogoAnalisiSuolo.Slug("Conducibilità (mS/cm)"));
        Assert.Matches(new Regex("^[a-z0-9-]+$"), CatalogoAnalisiSuolo.Slug("Composizione CSC (T) cmol(₊)/kg"));
    }

    private sealed class SuoloFinto(params int[] aziende) : ILettureAnalisiSuolo
    {
        public Task<IReadOnlyList<AziendaAnalisiSuoloDto>> AziendeAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AziendaAnalisiSuoloDto>>(aziende.Select(a => new AziendaAnalisiSuoloDto(a, $"Azienda {a}", 3)).ToList());

        public Task<AnalisiSuoloAziendaDto?> AziendaAsync(int idAzienda, CancellationToken ct = default) =>
            Task.FromResult<AnalisiSuoloAziendaDto?>(null);
    }

    private ReportisticaController Controller(ILettureAnalisiSuolo? suolo) =>
        new(new ArchivioReportistica(Options.Create(new ReportisticaOptions
            {
                Database = Path.Combine(_cartella, "reportistica.db"), DatabaseRipiego = "",
            })),
            new PerimetroAziende(new InMemoryGruppiRepository(), Options.Create(new PerimetroOptions())), null, suolo);

    [Fact]
    public async Task Api_TreReportNuovi_SuoloSulleAziendeConAnalisi()
    {
        Assert.True(ReportisticaController.EdizioneValida("tutti"));
        Assert.True(ReportisticaController.EdizioneValida("2025"));
        Assert.False(ReportisticaController.EdizioneValida("tutto"));

        var controller = Controller(new SuoloFinto(99999));
        // Il suolo non guarda il perimetro: l'azienda 99999 ha analisi, quindi c'è.
        var ed = await controller.Scelta("suolo", 99999, "tutti", new SalvaSceltaReportDto("suolo.fosforo", false, false, "mario"), default);
        Assert.Equal("suolo.fosforo", Assert.Single(ed.Value!.Scelte).IdElemento);
        Assert.IsType<NotFoundResult>((await controller.Get("suolo", 12345, "tutti", default)).Result);
        Assert.IsType<NotFoundResult>((await Controller(null).Get("suolo", 99999, "tutti", default)).Result);

        // Fine stagione e Indice Bigot sull'annata, nel perimetro (vuoto = nessuna restrizione).
        var rfs = await controller.Commento("rfs", 1, "2025", new SalvaCommentoReportDto("rfs.nutrizione", "en", "Fertilised in March.", "mario"), default);
        Assert.Equal("Fertilised in March.", Assert.Single(rfs.Value!.Commenti).Testo);
        var ib = await controller.Stato("ib", 1, "2025", new CambiaStatoReportDto(StatiReport.Pronto, "mario"), default);
        Assert.Equal(StatiReport.Pronto, ib.Value!.Stato);
        Assert.IsType<BadRequestObjectResult>((await controller.Scelta("ib", 1, "2025",
            new SalvaSceltaReportDto("rfs.sanita", false, false, "mario"), default)).Result);
    }

    /// <summary>
    /// Pubblicazione congelata (task #418, giro 2): i dati salvati si rileggono uguali e ridanno lo stesso PowerPoint; un
    /// contenuto di un altro report o di un formato sconosciuto non si legge (la pagina dice «non pubblicato», mai dati sbagliati).
    /// </summary>
    [Fact]
    public void Contenuto_TreReport_SiRileggeERidaLoStessoReport()
    {
        var scelte = Scelte(["ib.storico"], ("ib.copertina", "Buona annata."));
        var ib = ContenutoReport<IndiceBigotReportDto>.Di("ib", "Azienda", "2025", "it", ReportIndice(), scelte, Adesso).Json();
        var letto = ContenutoReport<IndiceBigotReportDto>.Leggi(ib, "ib")!;
        Assert.Equal("Buona annata.", letto.Scelte.Commento("ib.copertina"));
        Assert.False(letto.Scelte.Visibile("ib.storico"));
        Assert.Equal(Adesso, letto.Generato);
        Assert.Equal(
            Slide(PowerPointIndiceBigot.Crea(ReportIndice(), new FiltroIndice(), 2025, Adesso, scelte)).Select(s => s.Testo),
            Slide(PowerPointIndiceBigot.Crea(letto.Dati, new FiltroIndice(), 2025, Adesso, letto.Scelte)).Select(s => s.Testo));
        Assert.Null(ContenutoReport<IndiceBigotReportDto>.Leggi(ib, "suolo"));
        Assert.Null(ContenutoReport<IndiceBigotReportDto>.Leggi(ib.Replace("\"formato\":1", "\"formato\":99", StringComparison.Ordinal), "ib"));
        Assert.Null(ContenutoReport<IndiceBigotReportDto>.Leggi("non json", "ib"));

        var meteo = new MeteoReportFineStagioneDto([new(1, "A", null, null, true)],
            [new(1, new DateOnly(2025, 6, 1), false, 12, null, null, null)], false, Adesso);
        var rfs = ContenutoReport<DatiFineStagioneCongelati>.Di("rfs", "Prova", "2025", "en",
            new(meteo, null, [], null), Scelte([], ("rfs.nutrizione", "Fertilised in March.")), Adesso).Json();
        var lettoRfs = ContenutoReport<DatiFineStagioneCongelati>.Leggi(rfs, "rfs")!;
        Assert.Equal(12, Assert.Single(lettoRfs.Dati.Meteo.Giorni).Precipitazione);
        Assert.Equal("Fertilised in March.", lettoRfs.Scelte.Commento("rfs.nutrizione"));

        var suolo = ContenutoReport<AnalisiSuoloAziendaDto>.Di("suolo", "Azienda", "tutti", "it", new AnalisiSuoloAziendaDto(5, "Azienda", [], []),
            ScelteApplicate.Tutto, Adesso).Json();
        Assert.Equal(5, ContenutoReport<AnalisiSuoloAziendaDto>.Leggi(suolo, "suolo")!.Dati.IdAzienda);
    }

    [Fact]
    public void Prepara_IndirizziDelleVersioniPubblicate()
    {
        Assert.Equal("indice-bigot?azienda=7&anno=2025&versione=2", DefinizionePrepara.Di("ib").Versione(7, "2025", "it", 2));
        Assert.Equal("report-analisi-suolo?azienda=7&versione=1", DefinizionePrepara.Di("suolo").Versione(7, "tutti", "it", 1));
        Assert.Equal("report-fine-stagione?azienda=7&annata=2025&lingua=en&versione=3", DefinizionePrepara.Di("rfs").Versione(7, "2025", "en", 3));
        Assert.Equal("report/pubblicato/powerpoint?report=suolo&azienda=7&edizione=tutti&lingua=it&versione=1",
            PubblicazioniReport.IndirizzoPowerPoint("suolo", 7, "tutti", "it", 1));
    }

    [Fact]
    public async Task Api_PubblicaERileggeIlSuolo()
    {
        var controller = Controller(new SuoloFinto(99999));
        var contenuto = ContenutoReport<AnalisiSuoloAziendaDto>.Di("suolo", "Azienda", "tutti", "it",
            new AnalisiSuoloAziendaDto(99999, "Azienda", [], []), Scelte(["suolo.fosforo"]), Adesso).Json();
        var pptx = PowerPointAnalisiSuolo.Crea("Azienda", [], new SelezioneSuolo(), Adesso);
        var richiesta = new PubblicaReportDto("it", Adesso, null, "suolo.pptx", contenuto, Convert.ToBase64String(pptx), "mario");

        // Da bozza non si pubblica.
        Assert.IsType<ConflictObjectResult>((await controller.Pubblica("suolo", 99999, "tutti", richiesta, default)).Result);
        await controller.Stato("suolo", 99999, "tutti", new CambiaStatoReportDto(StatiReport.Pronto, "mario"), default);
        var pubblicata = await controller.Pubblica("suolo", 99999, "tutti", richiesta, default);
        Assert.Equal(1, pubblicata.Value!.Versione);

        var riletta = (await controller.Pubblicata("suolo", 99999, "tutti", "it", null, default)).Value!;
        var dati = ContenutoReport<AnalisiSuoloAziendaDto>.Leggi(riletta.Contenuto, "suolo")!;
        Assert.False(dati.Scelte.Visibile("suolo.fosforo"));
        Assert.Equal(99999, dati.Dati.IdAzienda);
    }
}
