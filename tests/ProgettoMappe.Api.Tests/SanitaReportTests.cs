using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Sezione X «Sanità» del report fine stagione (task #203, fogli 26–36 di Qlik).</summary>
public class SanitaReportTests
{
    private static RilievoFineStagioneDto R(int vigneto, DateTime data, int tipo, int? organo = null, double? intensita = null,
        double? diffusione = null, double? organi = null, double? catture = null, int? eta = null, int? bbch = null) =>
        new(vigneto, null, data, data.Year, tipo, organo, eta, organi, diffusione, intensita, null, catture, null, null, bbch,
            null, null, null, null, null, null);

    private static CampoReportFineStagioneDto Campo(IReadOnlyList<RilievoFineStagioneDto> rilievi,
        IReadOnlyList<CollezioneEscaDto>? collezione = null, SanitaFineStagioneDto? sanita = null) => new(
        new Dictionary<int, double?>(), [], [], [], [],
        [new(1, "Nord", "Alta", "Merlot", 2010, 30000), new(2, "Sud", "Bassa", "Refosco", null, 10000)],
        [], rilievi, collezione ?? [], [], null, null, sanita);

    [Fact]
    public void Danno_MassimoPerVignetoSugliOrganiGiusti_AcinellaturaSenzaOrgano()
    {
        var campo = Campo(
        [
            R(1, new DateTime(2026, 6, 1), 12, organo: 1, intensita: 10), R(1, new DateTime(2026, 7, 1), 12, organo: 3, intensita: 30),
            R(1, new DateTime(2026, 7, 1), 12, organo: 2, intensita: 80), // grappolo: non conta per le foglie
            R(2, new DateTime(2026, 7, 1), 11, organo: 4, intensita: -5),
            R(2, new DateTime(2026, 7, 1), 168, organo: 9, intensita: 20),
            R(1, new DateTime(2025, 7, 1), 12, organo: 1, intensita: 99),
        ]);

        var foglie = CalcoloSanitaReport.Danno(campo, 2026, grappolo: false).ToDictionary(s => s.Tipo, s => s.Serie.PerVigneto);
        var grappoli = CalcoloSanitaReport.Danno(campo, 2026, grappolo: true).ToDictionary(s => s.Tipo, s => s.Serie.PerVigneto);

        Assert.Equal(0.3, foglie[12][1], 6);
        Assert.Equal(0, foglie[11][2]);
        Assert.Equal(0.8, grappoli[12][1], 6);
        Assert.Equal(0.2, grappoli[168][2], 6);
    }

    [Fact]
    public void Esca_OrganiConValoreSuOrganiControllati()
    {
        var campo = Campo(
            [R(1, new DateTime(2026, 8, 1), 9, organi: 100), R(1, new DateTime(2026, 9, 1), 9, organi: 100)],
            [new(1, 2026, 100, 4), new(1, 2026, 10, 10), new(1, 2026, 0, 186), new(1, 2025, 100, 50)]);

        var esca = CalcoloSanitaReport.MalDellEsca(campo, 2026);

        Assert.Equal((2, 200.0), (esca.Rilievi, esca.PianteControllate));
        Assert.Equal(0.02, esca.Diffusione.Single(s => s.Nome == "100").PerVigneto[1], 6);
        Assert.Equal(186, esca.OrganiPerValore[0]);
    }

    [Fact]
    public void Tignole_NidiPerGenerazioneEQuotaDiSuperficie()
    {
        var campo = Campo(
        [
            R(1, new DateTime(2026, 6, 1), 13, diffusione: 10, eta: 1310), R(1, new DateTime(2026, 6, 8), 13, diffusione: 30, eta: 1310),
            R(2, new DateTime(2026, 6, 1), 13, diffusione: 10, eta: 410),
            R(1, new DateTime(2026, 7, 1), 148, catture: 4), R(2, new DateTime(2026, 7, 1), 148, catture: 2),
        ]);

        var generazioni = CalcoloSanitaReport.NidiTignole(campo, 2026);
        var voli = Assert.Single(CalcoloSanitaReport.VoliTignole(campo, 2026));

        Assert.Equal(20, generazioni[0].Valore);
        Assert.Equal(1, generazioni[0].QuotaControllata);
        Assert.Null(generazioni[1].Valore);
        Assert.Equal(3, voli.Serie.PerSettimana.Values.Single());
    }

    [Fact]
    public void Fenologia_MediePerVitignoEStorici()
    {
        var sanita = new SanitaFineStagioneDto([new(2026, 12, 5, 50)], new Dictionary<int, string> { [12] = "PERONOSPORA" },
            new Dictionary<int, int> { [2026] = 1 },
            [new(1, 2026, 2, 150), new(2, 2026, 2, 160), new(1, 2025, 2, 140), new(1, 2021, 2, 130), new(1, 2018, 2, 120), new(1, 2015, 2, 100)]);
        var campo = Campo([], sanita: sanita);

        var fioritura = CalcoloSanitaReport.Fenologia(campo, 2026)[1];
        var cruscotto = CalcoloSanitaReport.Cruscotto(campo, 2026);

        // 5 anni = 2021–2025, 10 anni = 2016–2025 (il 2015 resta fuori).
        Assert.Equal((155.0, 140.0, 135.0, 130.0),(fioritura.Annata!.Value, fioritura.AnnataPrima!.Value, fioritura.Media5!.Value, fioritura.Media10!.Value));
        Assert.Equal(["Merlot", "Refosco"], fioritura.Vitigni.Select(v => v.Vitigno));
        Assert.Equal("04/06", CalcoloSanitaReport.GiornoMese(155, 2026));
        Assert.Equal((5, 1, 2), (cruscotto.Rilievi, cruscotto.Monitorati, cruscotto.Totale));
        Assert.Equal("PERONOSPORA", cruscotto.PerTipo.Single().Tipo);
    }

    [Fact]
    public void Bbch_SettimaneEValoriDelleFoglie()
    {
        var campo = Campo(
        [
            R(1, new DateTime(2026, 4, 20), 1, bbch: 12), R(2, new DateTime(2026, 4, 20), 1, bbch: 16),
            R(1, new DateTime(2026, 4, 21), 1, bbch: 60), // fuori dalle foglie
            R(1, new DateTime(2025, 4, 20), 1, bbch: 10),
        ]);

        var vitigni = CalcoloSanitaReport.BbchPerVitigno(campo, 2026, grappoli: false);
        var storico = CalcoloSanitaReport.BbchStorico(campo, 2026, grappoli: false);

        Assert.Equal(["Merlot", "Refosco"], vitigni.Select(s => s.Nome));
        Assert.Equal(12, vitigni[0].PerSettimana.Values.Single());
        Assert.Equal(14, storico[0].PerSettimana.Values.Single());
        Assert.Equal(10, storico[2].PerSettimana.Values.Single());
    }
}
