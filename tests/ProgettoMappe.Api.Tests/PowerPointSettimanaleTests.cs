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

    /// <summary>La settimana dei rilievi di prova (10/6/2026).</summary>
    private static readonly SettimanaReport SettimanaGiugno = SettimanaReport.Di(new DateOnly(2026, 6, 10));

    private static DatiSettimanale Dati(bool conDati)
    {
        var t = new DateTime(2026, 6, 10);
        List<RilievoSettimanaDto> rilievi = conDati
            ?
            [
                new(1, t, 12, null, 2, null, 50, 30, 10), new(2, t, 146, null, StatoIdrico: 2), new(1, t, 0, 65),
                new(1, t, 13, null, 2, 1310, null, 4), new(2, t, 148, null, CatturePerTrappola: 7),
                new(1, t, 9, null, NrOrgani: 100, IdRilievo: "r9"), new(1, t, 113, null, LunghezzaGermogli: 12.5),
                new(1, t.AddYears(-1), 0, 61),
            ]
            : [];
        var campo = new CampoReportFineStagioneDto(new Dictionary<int, double?> { [1] = 10000, [2] = 20000 }, [], [], [],
            [new(1, 2026, null, null, null, null, 6.5)],
            [new(1, "Vigneto Nord", "Vigna Alta", "Glera", 2010, 10000), new(2, "Vigneto Sud", "Vigna Bassa", "Pinot", 2012, 20000)], [],
            Collezione: [new(1, 2026, 0, 90), new(1, 2026, 10, 10)],
            Sanita: new SanitaFineStagioneDto([], new Dictionary<int, string>(), new Dictionary<int, int>(), [new(1, 2026, 2, 160), new(1, 2025, 2, 165)]));
        return new DatiSettimanale("Azienda / prova", SettimanaGiugno,
            new RilieviSettimanaDto(rilievi, new Dictionary<int, string> { [12] = "PERONOSPORA" }, "snapshot", Adesso)
            {
                Esca = conDati ? [.. Enumerable.Repeat(new EscaSettimanaDto("r9", 0), 90), new("r9", 10), new("altro", 100)] : [],
                Danni = conDati ? [new(1, 2026, 20), new(2, 2025, 50)] : [],
                Meteo = conDati ? [new(1, 2026, SettimanaGiugno.Numero, 7, 0, 12, 20, 14, 27)] : [],
                Riepilogo = [new(2026, SettimanaGiugno.Numero, 2, 2, 7, 1, 1, 0, 0)],
            }, conDati ? campo : null);
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
        // 18 e 12 fogli dell'automazione più il meteo del portale.
        Assert.Equal(19, Slide(PowerPointSettimanale.Crea(Dati(conDati), LinguaReport.It, Adesso)));
        Assert.Equal(13, Slide(PowerPointSettimanale.Crea(Dati(conDati), LinguaReport.En, Adesso)));
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
        var d = Dati(true);
        var m = ParitaSettimanale.Misure(d, SelezioneSettimanale.Di(d));
        // Peronospora sul grappolo: intensità 10 × 1 ha / 3 ha / 100.
        Assert.Equal(10.0 / 3 / 100, m["s03.int.12.2-5"]!.Value, 6);
        Assert.Equal(2, m["s14.idrico.0"]);
        Assert.Equal(6.5, m["s12.vigoria.0"]);
        // La collezione dell'esca conta solo le righe dei rilievi della settimana.
        Assert.Equal(90, m["s17.valore.0"]);
        Assert.Equal(0, m["s17.valore.100"]);
        Assert.Equal(1, m["s17.rilievi"]);
        Assert.Equal(160, m["s7.fase2.annata"]);
        Assert.Equal(165, m["s7.fase2.precedente"]);
        // Danno dell'anno del report: solo il vigneto 1 (20%) sulla sua superficie.
        Assert.Equal(0.2, m["s3.danno"]!.Value, 6);
        // Qlik prende i danni di tutti gli anni: (20 × 1 + 50 × 2) / 3 / 100.
        Assert.Equal(0.4, ParitaSettimanale.Misure(d, SelezioneSettimanale.Di(d), _ => true)["s3.danno"]!.Value, 6);
        // Sanità del grappolo: nessun rilievo di botrite, marciumi o oidio.
        Assert.Equal(1.0, m["s3.sanita"]!.Value, 6);
    }

    [Fact]
    public void Selezione_SoloLaSettimanaPerIKpi_LaStagionePerLeLinee()
    {
        var d = Dati(true);
        var prima = d with { Settimana = new SettimanaReport(2026, SettimanaGiugno.Numero + 1) };
        var s = SelezioneSettimanale.Di(prima);
        Assert.Empty(s.Settimana);
        Assert.NotEmpty(s.Stagione);
        Assert.Equal(12.5, CalcoloSettimanale.GermogliPerVigneto(SelezioneSettimanale.Di(d).Settimana)[1]);
    }

    [Fact]
    public void Firma_DallaFormulaDiQlik()
    {
        FormulaQlik F(string formula) => new(3, "x", 0, formula, false);
        Assert.Equal("s03.int.12.2-5", ParitaSettimanale.Firma(F("if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={\"$(=Max(Anno))\"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))")));
        Assert.Equal("s22.28.tutti", ParitaSettimanale.Firma(F("sum(Aggr(count({<idTipoRilievo={'28'},Anno={\"$(=Max(Anno))\"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))")));
        Assert.Equal("s21.73", ParitaSettimanale.Firma(F("(sum(Aggr(avg({<idTipoRilievo={'73'},Anno={\"$(=Max(Anno))\"}>}Superficie),IdVigneto))/10000) / (sum(Aggr(avg(Superficie),IdVigneto))/10000)")));
        Assert.Equal("s12.vigoria.2", ParitaSettimanale.Firma(F("avg(aggr(avg({<Anno_Vigoria={\"$(=Max(Anno_Vigoria)-2)\"}>}Punteggio),IdVigneto))")));
        Assert.Equal("s17.valore.50", ParitaSettimanale.Firma(F("Count({<idTipoRilievo={\"9\"}, ValoreConteggiato={\"50\"},Anno={\"$(=Max(Anno))\"}>} ([Collezione.IdRow]))")));
        Assert.Null(ParitaSettimanale.Firma(F("IF(avg(aggr(max(x)))=4,'a','b')")));
        Assert.Null(ParitaSettimanale.Firma(new FormulaQlik(4, "x", 0, "if((sum(aggr(max({<idTipoRilievo={'12'}>}[PercIntensità]*Superficie)", true)));
    }

    [Fact]
    public void Verita_LeggeLeMisureNellOrdine_DateComeGiorno()
    {
        const string json = """
            {"letto_il":"2026-11-13T21:35:09","aziende":{"7":{"nome":"Prova","annate":{"46":{"selezione_riuscita":true,
             "anni_risolti":{"Anno":"2024"},"slide":{"Slide 7. Fenologia":{"kpi zFwGep":{"misure":{"ANNATA 2026":{"testo":"07/04","num":46484.5}}}},
             "Slide 3. Sanità":{"kpi SUdmLW":{"misure":{"Vigneti con Danno":{"testo":"-","num":null},"Vigneti monitorati":{"testo":"0%","num":0.0}}}}}}}}}}
            """;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(2024, ParitaSettimanale.Selezione(doc.RootElement, 7, 46)!.Anno);
        var numeri = ParitaSettimanale.Verita(doc.RootElement, 7, 46);
        // Day_Ref = giorno + Yearend(Today()) (31/12/2026 alle 23:59:59, seriale 46387,99…): 46484,5 − 46388.
        Assert.Equal(96.5, numeri.Single(n => n.Slide == 7).Valore);
        Assert.Equal(1, numeri.Single(n => n.Oggetto == "SUdmLW" && n.Colonna == "Vigneti monitorati").Posizione);
        Assert.Equal("zFwGep", numeri.Single(n => n.Slide == 7).Oggetto);
    }

    /// <summary>
    /// Parità sui dati veri: la verità di Qlik (docs/specifiche/report-settimanale/verita-qlik.json) contro lo snapshot
    /// (<c>MAPPE_RFS_SNAPSHOT</c>, altrimenti /srv/mappe-indice/rfs.sqlite). Senza snapshot il test passa senza fare niente.
    /// Si confrontano solo le selezioni riuscite in Qlik (<see cref="ParitaSettimanaleQlik.SelezioniValide"/>), con l'anno
    /// che Qlik ha davvero mostrato e i vigneti che tiene associati alla settimana.
    /// </summary>
    [Fact]
    public async Task ParitaSettimanale_SullaVeritaDiQlik()
    {
        var cartella = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "specifiche", "report-settimanale");
        // Lo snapshot della variabile o quello dell'Api di produzione (sola lettura), se c'è su questa macchina.
        var percorso = Environment.GetEnvironmentVariable("MAPPE_RFS_SNAPSHOT") is { Length: > 0 } p ? p : "/srv/mappe-indice/rfs.sqlite";
        if (!File.Exists(Path.Combine(cartella, "verita-qlik.json")) || !File.Exists(percorso))
        {
            _uscita.WriteLine("Verità di Qlik o snapshot non disponibili: parità non controllata.");
            return;
        }
        // Il PowerPoint sui dati veri resta valido, in italiano e in inglese.
        foreach (var d in await ParitaSettimanaleQlik.DatiAsync(cartella, percorso))
        {
            Assert.Equal(19, Slide(PowerPointSettimanale.Crea(d, LinguaReport.It, Adesso)));
            Assert.Equal(13, Slide(PowerPointSettimanale.Crea(d, LinguaReport.En, Adesso)));
        }
        var confronti = await ParitaSettimanaleQlik.ConfrontaAsync(cartella, percorso);
        foreach (var c in confronti.Where(c => !c.Torna))
        {
            _uscita.WriteLine(ParitaSettimanale.Descrivi(c));
        }
        var daCapire = confronti.Where(c => !c.Torna && c.Voluta is null).ToList();
        _uscita.WriteLine($"Confrontati {confronti.Count}, tornano {confronti.Count(c => c.Torna)}, voluti {confronti.Count(c => c.Voluta is not null)}, da capire {daCapire.Count}");
        Assert.True(confronti.Count > 0);
        Assert.True(daCapire.Count <= ParitaSettimanaleQlik.DaCapireMassimo, $"Confrontati {confronti.Count}, tornano {confronti.Count(c => c.Torna)}, da capire {daCapire.Count}:\n" +
            string.Join("\n", daCapire.Take(80).Select(ParitaSettimanale.Descrivi)));
    }
}
