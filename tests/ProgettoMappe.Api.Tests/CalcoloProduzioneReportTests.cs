using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class CalcoloProduzioneReportTests
{
    private static CampoReportFineStagioneDto Campo(params ProduzioneDto[] produzione) => new(
        new Dictionary<int, double?>(), [], [], [], [],
        [
            new(1, "Nord", "Alta", "Merlot", 2010, 20000),
            new(2, "Sud", "Alta", "Merlot", null, 10000),
            new(3, "Est", "Bassa", "Refosco", 2020, 5000),
        ],
        produzione);

    [Fact]
    public void PerVitigno_SommaInQuintali_SoloVendemmieDellAnnata()
    {
        var campo = Campo(
            new(1, 2026, false, 12000, 6000, 2, 1), new(2, 2026, false, 8000, 8000, null, null),
            new(3, 2026, false, 3000, 6000, null, null), new(1, 2026, true, 99999, null, null, null),
            new(1, 2025, false, 50000, null, null, null));

        var vitigni = CalcoloProduzioneReport.PerVitigno(campo, 2026);

        Assert.Equal([("Merlot", 200.0), ("Refosco", 30.0)], vitigni.Select(v => (v.Vitigno, v.Quintali)));
    }

    [Fact]
    public void PerVignetoEAnno_QuintaliEdEta()
    {
        var campo = Campo(new(1, 2026, false, 12000, 6000, null, null), new(2, 2025, false, null, null, null, null));
        var righe = CalcoloProduzioneReport.PerVignetoEAnno(campo);
        var r = Assert.Single(righe);                       // la riga senza valori non conta
        Assert.Equal(60, r.QuintaliEttaro);
        Assert.Equal(120, r.Quintali);
        Assert.Equal(16, r.Eta);
    }

    [Fact]
    public void Scostamenti_StimaMenoReale()
    {
        var campo = Campo(new(1, 2026, false, 12000, 6000, 2.5, 1), new(1, 2026, true, 15000, 7500, null, null),
            new(3, 2025, false, 1, 1, null, null));
        var s = Assert.Single(CalcoloProduzioneReport.Scostamenti(campo, 2026));
        Assert.Equal(2, s.Ettari);
        Assert.Equal(3000, s.Differenza);
        Assert.Equal(1500, s.DifferenzaEttaro);
        Assert.Equal(2.5, s.PercFallanze);
    }
}
