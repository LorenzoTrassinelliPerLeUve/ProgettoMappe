using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportAnalisiSuolo;
using Xunit;
using P = DocumentFormat.OpenXml.Presentation;

namespace ProgettoMappe.Api.Tests;

/// <summary>Report analisi del suolo (task #317): colonne calcolate, medie come Qlik, PowerPoint da 52 slide.</summary>
public class ReportAnalisiSuoloTests
{
    private static AnalisiSuoloDto Analisi(string id, string vigneto, string? punto, int? tessitura, params (string, double)[] valori) =>
        new(id, 1, vigneto, "Vigna A", "Merlot", null, 2025, new DateOnly(2025, 3, 1), 40, punto, null, null, tessitura,
            valori.ToDictionary(v => v.Item1, v => v.Item2));

    private static readonly TessituraSuoloDto Franco = new(7, 40, 40, 20, "Franco", 10, 30, 150, 1.4, 1.2, 20, 28, 1.2, 1.6, 13, 17);

    [Fact]
    public void ColonneCalcolate_FormuleVerificateDalDba()
    {
        var c = CampiSuolo.Calcola(Analisi("1", "V", "P1", 7, ("Suolo_CO_perc", 1.0), ("Suolo_N_totale_perc", 0.1),
            ("Suolo_CSC", 20), ("Suolo_Ca_scambiabile_ppm", 2004), ("Suolo_Mg_scambiabile_ppm", 243), ("Suolo_K_scambiabile_ppm", 391),
            ("Suolo_Na_scambiabile_ppm", 229.9), ("Suolo_aciditascambio_meq", 2), ("Suolo_CaCO3_totali_perc", 10),
            // valori del laboratorio: si ignorano quando Mappe può calcolare (decisione del 17/11)
            ("Suolo_SO_perc", 9), ("Suolo_C_N", 99), ("Suolo_K2", 9)), Franco);

        Assert.Equal(1.724, c[CampiSuolo.So], 6);
        Assert.Equal(1.0, c[CampiSuolo.Azoto], 6); // 0,1 è in %: × 10 → 1 ‰
        Assert.Equal(10, c[CampiSuolo.CN], 6);
        Assert.Equal(10, c[CampiSuolo.CaMeq], 6);
        Assert.Equal(50, c[CampiSuolo.CaCsc], 6);
        Assert.Equal(10, c[CampiSuolo.MgCsc], 6);
        Assert.Equal(5, c[CampiSuolo.KCsc], 6);
        Assert.Equal(70, c[CampiSuolo.Saturazione], 6);
        Assert.Equal(10, c[CampiSuolo.AciditaCsc], 6);
        Assert.Equal(2, c[CampiSuolo.MgK], 6);
        Assert.Equal(5, c[CampiSuolo.CaMg], 6);
        Assert.Equal(10, c[CampiSuolo.CaK], 6);
        Assert.Equal(1.724 / 20 * 100, c[CampiSuolo.SoArgilla], 6);
        Assert.Equal(1200.0 / (400 * 230) * 100, c[CampiSuolo.K2], 6);
    }

    [Fact]
    public void SenzaDatiPerCalcolare_SiUsaIlValoreDelLaboratorio()
    {
        var c = CampiSuolo.Calcola(Analisi("1", "V", "P1", null, ("Suolo_SO_perc", 2), ("Suolo_N_totale_perc", 1.5), ("Suolo_K2", 1.1)), null);
        Assert.Equal(2, c[CampiSuolo.So]);
        Assert.Equal(1.5, c[CampiSuolo.Azoto]); // sopra 0,4 è già in ‰
        Assert.Equal(2 / 1.724 / 0.15, c[CampiSuolo.CN], 6); // CO ricavato dalla SO
        Assert.Equal(1.1, c[CampiSuolo.K2]);
        Assert.False(c.ContainsKey(CampiSuolo.CaCsc));
    }

    [Fact]
    public void Azoto_PermilleHaLaPrecedenza() =>
        Assert.Equal(2.0, CampiSuolo.AzotoPermille(2.0, 0.1));

    [Fact]
    public void Barre_MediaPerVignaEPunto_TessituraUnaVoltaPerTerna()
    {
        var dati = new AnalisiSuoloAziendaDto(5, "Azienda", [
            Analisi("1", "V1", "P2", 7, ("Suolo_ph_h20", 7)),
            Analisi("2", "V1", "P2", 7, ("Suolo_ph_h20", 8)),
            Analisi("3", "V1", "P10", null, ("Suolo_ph_h20", 6)),
            Analisi("4", "V2", null, null),
        ], [Franco]);
        var righe = CalcoloAnalisiSuolo.Righe(dati);
        var pkcl = CalcoloAnalisiSuolo.Fogli.Single(f => f.Titolo == "pH in KCL") with { Campo = CampiSuolo.PhH2o };
        var barre = CalcoloAnalisiSuolo.Barre(righe, pkcl);
        Assert.Equal(["P10", "P2"], barre.Select(b => b.Voce)); // testi in ordine alfabetico come Qlik, senza valore niente barra
        Assert.Equal(7.5, barre[1].Valore);
        Assert.Equal(7, CalcoloAnalisiSuolo.Kpi(righe, CampiSuolo.PhH2o));

        var so = CalcoloAnalisiSuolo.Fogli.First(f => f.Limiti == LimitiSuolo.SostanzaOrganica);
        Assert.Equal((20.0, 28.0), CalcoloAnalisiSuolo.Limiti(righe, so.Limiti));
        Assert.Equal(400, CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.Argilla * 20)); // due analisi, una terna
    }

    [Fact]
    public void Fogli_52ComeLAutomazione_ConFerroEdtaAlPostoDelRameDoppio()
    {
        var f = CalcoloAnalisiSuolo.Fogli;
        Assert.Equal(52, f.Count);
        Assert.Single(f, x => x.Campo == CampiSuolo.CuEdta);
        Assert.Single(f, x => x.Campo == CampiSuolo.FeEdta);
        Assert.Equal(CampiSuolo.FeEdta, f[38].Campo);
        Assert.Equal(TipoFoglioSuolo.Considerazioni, f[^1].Tipo);
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
    public void PowerPoint_52Slide_ValidoAncheSenzaDati(bool conDati)
    {
        var dati = new AnalisiSuoloAziendaDto(5, "Azienda", conDati
            ? [Analisi("1", "V1", "P1", 7, ("Suolo_ph_h20", 7), ("Suolo_CO_perc", 1), ("Suolo_CSC", 15), ("Suolo_Ca_scambiabile_ppm", 2000),
                ("Suolo_P_assimilabile_Olsen_ppm", 12), ("Suolo_P_assimilabile_TerreniAcidi_ppm", 20))]
            : [], [Franco]);
        var righe = CalcoloAnalisiSuolo.Righe(dati);
        var file = PowerPointAnalisiSuolo.Crea("Azienda", righe, new SelezioneSuolo(Anno: 2025), DateTimeOffset.UtcNow);
        Assert.Equal(52, Slide(file));
    }

    [Fact]
    public void NomeFile_ComeLAutomazione() =>
        Assert.Equal("Report Analisi del Terreno - Az. _ prova.pptx", PowerPointAnalisiSuolo.NomeFile("Az. / prova"));
}
