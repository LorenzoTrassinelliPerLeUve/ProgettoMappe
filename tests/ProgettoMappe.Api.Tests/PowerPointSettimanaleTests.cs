using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using Xunit;
using Xunit.Abstractions;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>PowerPoint e parità del report settimanale (task #213, tappa 3): 18 slide IT, 12 ENG, nomi come le automazioni.</summary>
public class PowerPointSettimanaleTests
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 13, 20, 0, 0, TimeSpan.Zero);
    private static readonly SettimanaReport Settimana = new(2026, 46);

    private readonly ITestOutputHelper _uscita;

    public PowerPointSettimanaleTests(ITestOutputHelper uscita) => _uscita = uscita;

    private static DatiSettimanale Dati(bool conDati)
    {
        var t = new DateTime(2026, 6, 10);
        List<RilievoSettimanaDto> rilievi = conDati
            ?
            [
                new(1, t, 12, null, 2, null, 50, 30, 10), new(2, t, 146, null, StatoIdrico: 2), new(1, t, 0, 65),
                new(1, t, 13, null, 2, 1310, null, 4), new(2, t, 148, null, CatturePerTrappola: 7), new(1, t, 9, null, NrOrgani: 100),
                new(1, t.AddYears(-1), 0, 61),
            ]
            : [];
        var campo = new CampoReportFineStagioneDto(new Dictionary<int, double?> { [1] = 10000, [2] = 20000 }, [], [], [],
            [new(1, 2026, null, null, null, null, 6.5)],
            [new(1, "Vigneto Nord", "Vigna Alta", "Glera", 2010, 10000), new(2, "Vigneto Sud", "Vigna Bassa", "Pinot", 2012, 20000)], [],
            Collezione: [new(1, 2026, 0, 90), new(1, 2026, 10, 10)],
            Sanita: new SanitaFineStagioneDto([], new Dictionary<int, string>(), new Dictionary<int, int>(), [new(1, 2026, 2, 160), new(1, 2025, 2, 165)]));
        return new DatiSettimanale("Azienda / prova", Settimana,
            new RilieviSettimanaDto(rilievi, new Dictionary<int, string> { [12] = "PERONOSPORA" }, "snapshot", Adesso), conDati ? campo : null);
    }

    private static int Slide(byte[] file)
    {
        using var documento = PresentationDocument.Open(new MemoryStream(file), false);
        var errori = new OpenXmlValidator().Validate(documento).Select(e => $"{e.Path?.XPath}: {e.Description}").ToList();
        Assert.True(errori.Count == 0, string.Join("\n", errori.Take(10)));
        return documento.PresentationPart!.Presentation.SlideIdList!.Elements<P.SlideId>().Count();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PowerPoint_UnaSlidePerFoglio_ValidoAncheSenzaDati(bool conDati)
    {
        Assert.Equal(18, Slide(PowerPointSettimanale.Crea(Dati(conDati), LinguaReport.It, Adesso)));
        Assert.Equal(12, Slide(PowerPointSettimanale.Crea(Dati(conDati), LinguaReport.En, Adesso)));
    }

    [Fact]
    public void NomeFile_ComeLeAutomazioni()
    {
        Assert.Equal("261113 - Azienda _ prova - Report settimanale.pptx",
            PowerPointSettimanale.NomeFile("Azienda / prova", Settimana, LinguaReport.It, Adesso));
        Assert.Equal("46 - 14 Nov - Azienda _ prova - Weekly Report.pdf",
            PowerPointSettimanale.NomeFile("Azienda / prova", Settimana, LinguaReport.En, Adesso, ".pdf"));
    }

    [Fact]
    public void Misure_ConNomiStabili()
    {
        var m = ParitaSettimanale.Misure(Dati(true));
        // Peronospora sul grappolo: intensità 10 × 1 ha / 3 ha / 100.
        Assert.Equal(10.0 / 3 / 100, m["s3.peronospora.intensita"]!.Value, 6);
        Assert.Equal(2, m["s14.idrico.2026"]);
        Assert.Equal(6.5, m["s12.vigoria.2026"]);
        Assert.Equal(90, m["s17.valore.0"]);
        Assert.Equal(160, m["s7.fase2.annata"]);
        Assert.Equal(165, m["s7.fase2.precedente"]);
    }

    [Fact]
    public void Confronta_ConLaVerita()
    {
        const string json = """{"aziende":[{"azienda":"Azienda / prova","settimana":"2026-46","misure":{"s14.idrico.2026":2.0,"s12.vigoria.2026":7,"altro":1}}]}""";
        var confronti = ParitaSettimanale.Confronta(ParitaSettimanale.Misure(Dati(true)),
            ParitaSettimanale.Verita(json, "Azienda / prova", Settimana));
        Assert.Equal(2, confronti.Count);
        Assert.True(confronti.Single(c => c.Misura == "s14.idrico.2026").Torna);
        Assert.False(confronti.Single(c => c.Misura == "s12.vigoria.2026").Torna);
    }

    /// <summary>
    /// Parità sui dati veri (tappa 4): quando arriva la verità di Qlik del principale (task #211) in
    /// docs/specifiche/report-settimanale/verita-qlik.json, il confronto si fa con lo snapshot (MAPPE_RFS_SNAPSHOT). Senza
    /// file il test passa senza fare niente.
    /// </summary>
    [Fact]
    public void ParitaSettimanale_SullaVeritaDiQlik()
    {
        var verita = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "specifiche", "report-settimanale", "verita-qlik.json");
        if (!File.Exists(verita) || Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT") is not { Length: > 0 })
        {
            _uscita.WriteLine("Verità di Qlik o snapshot non disponibili: parità da fare (tappa 4).");
            return;
        }
        _uscita.WriteLine("Verità presente: il confronto per azienda va scritto con il formato vero della verità (tappa 4).");
    }
}
