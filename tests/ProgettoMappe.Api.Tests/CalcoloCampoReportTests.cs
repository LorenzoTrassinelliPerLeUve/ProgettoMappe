using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class CalcoloCampoReportTests
{
    [Fact]
    public void Scottature_PonderateSullaSuperficie_MassimoPerVigneto()
    {
        var superfici = new Dictionary<int, double?> { [1] = 1000, [2] = 3000, [3] = null };
        List<ScottaturaDto> righe =
        [
            new(1, 2026, null, 10, 50), new(1, 2026, null, 20, 40),   // vigneto 1: max 20 × 1000, max 50 × 1000
            new(2, 2026, null, 4, 10),                                // vigneto 2: 4 × 3000, 10 × 3000
            new(3, 2026, null, 90, 90),                               // senza superficie: non pesa
            new(2, 2025, null, 99, 99),                               // altra annata
        ];

        var s = CalcoloCampoReport.Scottature(righe, superfici, 2026);

        Assert.Equal((20.0 * 1000 + 4 * 3000) / 4000 / 100, s.Danno, 9);
        Assert.Equal((50.0 * 1000 + 10 * 3000) / 4000 / 100, s.Diffusione, 9);
        Assert.Equal(4, s.Righe);
        Assert.Equal(3, s.Vigneti[0].IdVigneto);        // dal più colpito
        Assert.Equal(20, s.Vigneti.Single(v => v.IdVigneto == 1).Intensita);
    }

    [Fact]
    public void Scottature_SenzaRighe_Zero()
    {
        var s = CalcoloCampoReport.Scottature([], new Dictionary<int, double?>(), 2026);
        Assert.Equal(0, s.Danno);
        Assert.Equal(0, s.Righe);
        Assert.Empty(s.Vigneti);
    }

    [Fact]
    public void Suolo_MediaDeiValoriPresenti()
    {
        List<SuoloVignetoDto> suolo =
        [
            new(1, "A", "V", 180, 190, 100, 1.0, 10, 150, 160, 2),
            new(2, "B", "V", 200, null, 110, null, 20, 170, 160, null),
        ];
        var s = CalcoloCampoReport.Suolo(suolo);
        Assert.Equal(190, s.CapacitaDiCampo);
        Assert.Equal(190, s.AcquaDisponibile);
        Assert.Equal(1.0, s.Profondita);
        Assert.Equal(2, s.Conducibilita);
        Assert.Null(CalcoloCampoReport.Suolo([]).Pietrosita);
    }
}
