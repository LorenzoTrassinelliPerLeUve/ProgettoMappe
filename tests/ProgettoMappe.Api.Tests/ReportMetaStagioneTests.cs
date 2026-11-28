using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportMetaStagione;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Report metà stagione (task #420, tappa R5 della Reportistica): catalogo con id stabili, data di taglio (invaiatura), dati tagliati
/// allo stesso giorno per tutti gli anni, PowerPoint con le sole sezioni del metà stagione, pubblicazione congelata.
/// </summary>
public sealed class ReportMetaStagioneTests
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 28, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Oggi = new(2026, 11, 28);

    private static ScelteApplicate Scelte(IEnumerable<string> nascosti, params (string Id, string Testo)[] commenti) =>
        new(nascosti.ToHashSet(), commenti.ToDictionary(c => c.Id, c => c.Testo));

    private static CampoReportFineStagioneDto Campo(IReadOnlyList<FaseFenologicaDto>? fasi = null,
        IReadOnlyList<RilievoFineStagioneDto>? rilievi = null, IReadOnlyList<ProduzioneDto>? produzione = null) =>
        new(new Dictionary<int, double?>(), [], [new ScottaturaDto(1, 2026, 2, 10, 20)], [], [],
            [new VignetoFineStagioneDto(1, "Vigneto uno", "Vigna", "Glera", 2000, 10000)], produzione ?? [], rilievi ?? [],
            Sanita: new SanitaFineStagioneDto([new ConteggioRilieviDto(2026, 12, 99, 999, 1)], new Dictionary<int, string>(),
                new Dictionary<int, int> { [2026] = 7 }, fasi ?? []));

    private static RilievoFineStagioneDto Rilievo(DateTime data, int tipo = 12, double organi = 10) =>
        new(1, null, data, data.Year, tipo, 2, null, organi, null, 5, null, null, null, null, null, null, null, null, null, null, null);

    /// <summary>Gli id non cambiano più: si cambia il titolo, non l'id.</summary>
    [Fact]
    public void Catalogo_IdStabiliUniciEValidi()
    {
        Assert.Equal(
        [
            "rms.copertina", "rms.caratterizzazione", "rms.sanita", "rms.gestione-idrica", "rms.vigoria", "rms.produzione",
            "rms.indice-bigot", "rms.da-fare",
        ], CatalogoMetaStagione.Elementi.Select(e => e.Id));
        Assert.All(CatalogoMetaStagione.Elementi, e => Assert.True(ReportisticaController.IdValido("rms", e.Id), e.Id));
        Assert.Single(CatalogoMetaStagione.Elementi, e => !e.Nascondibile);
        Assert.Contains("rms", ReportisticaController.Report);
        // Le sezioni riusate esistono nel fine stagione.
        var rfs = CatalogoFineStagione.Elementi.Select(e => e.Id).ToHashSet();
        Assert.All(CatalogoMetaStagione.Sezioni.Where(s => s.SezioneFineStagione is not null), s => Assert.Contains(s.SezioneFineStagione!, rfs));
        Assert.Equal("II", CatalogoMetaStagione.Numeri["rfs.sanita"]);
        Assert.Equal("Report metà stagione", DefinizionePrepara.Di("rms").Titolo);
        Assert.Contains(MenuReport.Gruppi.SelectMany(g => g.Voci), v => v.Indirizzo == "/report-meta-stagione");
    }

    [Fact]
    public void ComeFineStagione_NascondeQuelloCheNonCEESpostaICommenti()
    {
        var scelte = Scelte(["rms.vigoria"], ("rms.copertina", "Generale."), ("rms.sanita", "Oidio sotto controllo."),
            ("rms.da-fare", "Sfogliare a fine luglio."));

        var rfs = CatalogoMetaStagione.ComeFineStagione(scelte);

        Assert.True(rfs.Visibile("rfs.sanita"));
        Assert.True(rfs.Visibile("rfs.caratterizzazione"));
        Assert.False(rfs.Visibile("rfs.vigoria")); // nascosta nel metà stagione
        Assert.False(rfs.Visibile("rfs.scottature")); // non c'è nel metà stagione
        Assert.False(rfs.Visibile("rfs.nutrizione"));
        Assert.False(rfs.Visibile("rfs.indice-bigot"));
        Assert.Equal("Oidio sotto controllo.", rfs.Commento("rfs.sanita"));
        Assert.Equal("Generale.", rfs.Commento("rfs.copertina"));
        Assert.Equal("Sfogliare a fine luglio.", rfs.Commento("rms.da-fare"));
    }

    [Fact]
    public void Taglio_InvaiaturaDellAnnataPoiStoricaPoiFissa()
    {
        // Giorno 213 di Qlik (su 366) = 31/7; 203 = 21/7.
        Assert.Equal(new DateOnly(2026, 7, 31), TaglioMetaStagione.DaGiornoQlik(2026, 213));
        Assert.Equal(new DateOnly(2025, 3, 1), TaglioMetaStagione.DaGiornoQlik(2025, 61));

        var annata = Campo([new(1, 2026, 3, 203), new(2, 2026, 3, 205), new(1, 2025, 3, 220), new(1, 2026, 4, 260)]);
        Assert.Equal(new TaglioReport(new DateOnly(2026, 7, 22), OrigineTaglio.InvaiaturaAnnata, false),
            TaglioMetaStagione.Scegli(annata, 2026, null, Oggi));

        var storica = Campo([new(1, 2024, 3, 210), new(1, 2025, 3, 220)]);
        Assert.Equal(new TaglioReport(new DateOnly(2026, 8, 2), OrigineTaglio.InvaiaturaStorica, false),
            TaglioMetaStagione.Scegli(storica, 2026, null, Oggi));

        Assert.Equal(new TaglioReport(new DateOnly(2026, 7, 31), OrigineTaglio.Fissa, false), TaglioMetaStagione.Scegli(null, 2026, null, Oggi));

        // Una data scelta dentro la stagione vince; fuori stagione si torna all'invaiatura.
        Assert.Equal(new TaglioReport(new DateOnly(2026, 7, 10), OrigineTaglio.Richiesta, false),
            TaglioMetaStagione.Scegli(annata, 2026, new DateOnly(2026, 7, 10), Oggi));
        Assert.Equal(OrigineTaglio.InvaiaturaAnnata, TaglioMetaStagione.Scegli(annata, 2026, new DateOnly(2026, 11, 2), Oggi).Origine);

        // Mai oltre ieri: la stagione non ancora vissuta non si confronta.
        Assert.Equal(new TaglioReport(new DateOnly(2026, 7, 14), OrigineTaglio.Fissa, true),
            TaglioMetaStagione.Scegli(null, 2026, null, new DateOnly(2026, 7, 15)));

        Assert.Equal([2026, 2025], TaglioMetaStagione.Annate(Meteo(), Oggi));
        Assert.Equal([2025], TaglioMetaStagione.Annate(Meteo(), new DateOnly(2026, 5, 31)));
    }

    /// <summary>1 mm al giorno, Tmed 20, dal 1/11/2024 al 31/10/2026, più un giorno di previsione.</summary>
    private static MeteoReportFineStagioneDto Meteo()
    {
        var giorni = new List<GiornoMeteoReportDto>();
        for (var d = new DateOnly(2024, 11, 1); d <= new DateOnly(2026, 10, 31); d = d.AddDays(1))
            giorni.Add(new GiornoMeteoReportDto(1, d, false, 1m, 10m, 20m, 30m));
        giorni.Add(new GiornoMeteoReportDto(1, new DateOnly(2026, 7, 1), true, 50m, 10m, 20m, 30m));
        return new MeteoReportFineStagioneDto([new VignetoMeteoReportDto(1, "Vigneto uno", 1, "Vigna", true)], giorni, true);
    }

    [Fact]
    public void Meteo_TagliatoAlloStessoGiornoPerOgniAnnata_ConfrontoOnesto()
    {
        var fino = new DateOnly(2026, 7, 15);
        var tagliato = TaglioMetaStagione.Meteo(Meteo(), fino);

        Assert.DoesNotContain(tagliato.Giorni, g => g.Previsione);
        Assert.Contains(tagliato.Giorni, g => g.Data == new DateOnly(2025, 11, 1)); // inizio dell'annata agraria
        Assert.Contains(tagliato.Giorni, g => g.Data == new DateOnly(2025, 7, 15));
        Assert.DoesNotContain(tagliato.Giorni, g => g.Data == new DateOnly(2025, 7, 16)); // stesso taglio anche nel 2025
        Assert.DoesNotContain(tagliato.Giorni, g => g.Data.Month is 8 or 9 or 10);

        // Fase 3 (giugno e luglio): 30 + 15 giorni nell'annata e nella media dell'anno prima, scarto zero.
        var fase3 = CalcoloMeteoReport.PioggiaPerFase(tagliato, 2026).Single(r => r.Fase == 3);
        Assert.Equal(45m, fase3.Annata);
        Assert.Equal(45m, fase3.Media10);
        Assert.Equal(0m, fase3.Variazione10);
        Assert.Null(CalcoloMeteoReport.PioggiaPerFase(tagliato, 2026).Single(r => r.Fase == 4).Annata);
        Assert.Equal(3, TaglioMetaStagione.UltimaFase(fino));
        Assert.Equal(2, TaglioMetaStagione.UltimaFase(new DateOnly(2026, 5, 20)));
    }

    [Fact]
    public void Campo_RilieviFinoAlTaglio_StimeDiProduzione_SenzaVendemmiaNeScottature()
    {
        var campo = Campo(
            [new(1, 2026, 1, 100), new(1, 2026, 3, 205), new(1, 2026, 4, 260), new(1, 2025, 4, 255)],
            [Rilievo(new DateTime(2026, 6, 10, 9, 0, 0)), Rilievo(new DateTime(2026, 6, 10, 9, 0, 0), organi: 5),
             Rilievo(new DateTime(2026, 7, 2)), Rilievo(new DateTime(2026, 8, 20)), Rilievo(new DateTime(2025, 7, 20)),
             Rilievo(new DateTime(2025, 6, 1))],
            [new(1, 2026, true, 1000, 5000, null, null), new(1, 2026, false, 1200, 6000, null, null), new(1, 2025, false, 900, 4500, null, null)]);

        var tagliato = TaglioMetaStagione.Campo(campo, new DateOnly(2026, 7, 15))!;

        Assert.Equal([new DateTime(2026, 6, 10, 9, 0, 0), new DateTime(2026, 6, 10, 9, 0, 0), new DateTime(2026, 7, 2), new DateTime(2025, 6, 1)],
            tagliato.Rilievi!.Select(r => r.Data));
        Assert.Equal([1, 3], tagliato.Sanita!.Fasi.Where(f => f.Anno == 2026).Select(f => f.Fase));
        Assert.DoesNotContain(tagliato.Sanita.Fasi, f => f.Fase == 4);
        // Cruscotto ricontato: due rilievi distinti nel 2026 (10/6 e 2/7), 25 organi; un vigneto monitorato.
        var conteggio = Assert.Single(tagliato.Sanita.Conteggi, c => c.Anno == 2026);
        Assert.Equal((2, 25d), (conteggio.Rilievi, conteggio.Organi));
        Assert.Equal(1, tagliato.Sanita.VignetiMonitorati[2026]);
        // Produzione: nell'annata solo la stima; gli anni passati restano.
        Assert.Equal([(2026, true), (2025, false)], tagliato.Produzione.Select(p => (p.Anno, p.Stima)));
        Assert.Empty(tagliato.ScottatureVista);
        Assert.Null(TaglioMetaStagione.Campo(null, new DateOnly(2026, 7, 15)));
    }

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

    [Theory]
    [InlineData(LinguaReport.It)]
    [InlineData(LinguaReport.En)]
    public void PowerPoint_SoloLeSezioniDelMetaStagione_FinoAlTaglio(LinguaReport lingua)
    {
        var dati = DatiMetaStagione.Taglia(Meteo(), Campo([new(1, 2026, 3, 203)]), 2026, null, Oggi);
        Assert.Equal(new DateOnly(2026, 7, 21), dati.Taglio.Fino);
        var scelte = Scelte(["rms.vigoria"], ("rms.copertina", "Annata calda."), ("rms.sanita", "Oidio sotto controllo."),
            ("rms.da-fare", "Sfogliare a fine luglio."), ("rms.indice-bigot", "Mancano i rilievi SFE."));

        var slide = Slide(PowerPointMetaStagione.Crea("Prova", dati, lingua, Adesso, scelte));

        var titolo = TestiReport.Testo(lingua, "meta.titolo", 2026);
        Assert.Contains(titolo, slide[0].Testo);
        Assert.Contains(PowerPointMetaStagione.RigaTaglio(dati.Taglio, lingua), slide[0].Testo);
        Assert.Contains("II. ", slide[1].Testo); // sommario del metà stagione
        Assert.DoesNotContain("IV. ", slide[1].Testo); // vigoria nascosta
        Assert.Contains("Annata calda.", slide[2].Testo);
        // Niente fase 4 del meteo, scottature, vigoria, SFE, fallanze, Indice Bigot completo.
        foreach (var chiave in new[] { "slide.termo-4", "slide.giorno-4", "slide.scottature-media", "slide.vigoria", "slide.fallanze",
                     "slide.chioma-1", "slide.ib-istogramma" })
            Assert.DoesNotContain(slide, s => s.Titolo == TestiReport.Testo(lingua, chiave));
        Assert.Contains(slide, s => s.Titolo == TestiReport.Testo(lingua, "slide.termo-3"));
        Assert.Contains(slide, s => s.Titolo == TestiReport.Testo(lingua, "slide.giorno-3"));
        Assert.Contains(slide, s => s.Testo.Contains("Oidio sotto controllo.", StringComparison.Ordinal));
        // In fondo: Indice Bigot provvisorio, «Da qui alla vendemmia», nota.
        var indice = slide.FindIndex(s => s.Testo.Contains("Mancano i rilievi SFE.", StringComparison.Ordinal));
        var daFare = slide.FindIndex(s => s.Testo.Contains("Sfogliare a fine luglio.", StringComparison.Ordinal));
        Assert.True(indice > 0 && daFare > indice);
        Assert.StartsWith("VII. ", slide[daFare].Titolo);
        Assert.Equal(TestiReport.Testo(lingua, "meta.note.titolo"), slide[^1].Titolo);
        Assert.DoesNotContain(slide, s => s.Titolo.StartsWith(TestiReport.Testo(lingua, "titolo", 2026), StringComparison.Ordinal));
    }

    [Fact]
    public void SlideAmmesse_SecondoIlTaglio()
    {
        var luglio = new DateOnly(2026, 7, 20);
        Assert.True(PowerPointMetaStagione.SlideAmmessa("termo-3", luglio));
        Assert.False(PowerPointMetaStagione.SlideAmmessa("caldi-4", luglio));
        Assert.True(PowerPointMetaStagione.SlideAmmessa("giorno-3", luglio));
        Assert.False(PowerPointMetaStagione.SlideAmmessa("giorno-4", luglio));
        Assert.False(PowerPointMetaStagione.SlideAmmessa("ib-storico-sfe", luglio));
        Assert.True(PowerPointMetaStagione.SlideAmmessa("apici-storico", luglio));
        Assert.True(PowerPointMetaStagione.SlideAmmessa("bbch-storico-foglie", luglio));
        Assert.False(PowerPointMetaStagione.SlideAmmessa("termo-3", new DateOnly(2026, 5, 20)));
        Assert.False(PowerPointMetaStagione.SlideAmmessa("giorno-3", new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void Contenuto_SiRileggeConIlSuoTaglio()
    {
        var dati = DatiMetaStagione.Taglia(Meteo(), Campo(), 2026, new DateOnly(2026, 7, 10), Oggi);
        var json = ContenutoReport<DatiMetaStagione>.Di("rms", "Prova", "2026", "it", dati, Scelte(["rms.vigoria"]), Adesso).Json();

        var letto = ContenutoReport<DatiMetaStagione>.Leggi(json, "rms");

        Assert.NotNull(letto);
        Assert.Equal(dati.Taglio, letto.Dati.Taglio);
        Assert.Equal(dati.Meteo.Giorni.Count, letto.Dati.Meteo.Giorni.Count);
        Assert.False(letto.Scelte.Visibile("rms.vigoria"));
        Assert.Null(ContenutoReport<DatiMetaStagione>.Leggi(json, "rfs"));
    }
}
