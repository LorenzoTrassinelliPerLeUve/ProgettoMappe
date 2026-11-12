using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class CalcoloVigoriaReportTests
{
    [Theory]
    [InlineData(10.0, "eccessiva")]
    [InlineData(9.99, "molto-alta")]
    [InlineData(8.8, "molto-alta")]
    [InlineData(7.5, "alta")]
    [InlineData(6.2, "medio-alta")]
    [InlineData(4.9, "media")]
    [InlineData(3.6, "medio-bassa")]
    [InlineData(2.3, "bassa")]
    [InlineData(1.0, "molto-bassa")]
    [InlineData(0.9, null)]
    public void Classe_SoglieDiQlik(double punteggio, string? attesa) =>
        Assert.Equal(attesa, CalcoloVigoriaReport.Classe(punteggio));

    [Fact]
    public void TreAnni_MediaDelleMediePerVigneto()
    {
        List<VigoriaDto> righe =
        [
            new(1, 2026, 4, null, null, null, 4), new(1, 2026, 6, null, null, null, 6),   // vigneto 1: 5
            new(2, 2026, 8, null, null, null, 8),                                          // vigneto 2: 8
            new(1, 2025, null, null, null, null, 3),
        ];

        var anni = CalcoloVigoriaReport.TreAnni(righe, 2026);

        Assert.Equal([2026, 2025, 2024], anni.Select(a => a.Anno));
        Assert.Equal(6.5, anni[0].Punteggio);            // (5 + 8) / 2, non (4 + 6 + 8) / 3
        Assert.Equal("medio-alta", anni[0].Classe);
        Assert.Equal(6.5, anni[0].LegnoPotatura);
        Assert.Null(anni[0].SfePerMetro);
        Assert.Equal(3, anni[1].Punteggio);
        Assert.Null(anni[2].Punteggio);
        Assert.Equal(5, CalcoloVigoriaReport.PerVignetoEAnno(righe)[(1, 2026)]);
    }
}
