using ProgettoMappe.Contracts;
using ProgettoMappe.Web.BilancioIdrico;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Bilancio idrico nel Web (task #335): testi della pagina e grafici della scheda.</summary>
public class BilancioIdricoWebTests
{
    private static GiornoBilancioDto G(DateOnly d, double aw, double? awc, double? pdlwp, double? soglia) =>
        new(d, false, 5, 0, 0, 79, 5, 0.7, 0.8, 2.8, aw, awc, 0.5, pdlwp, soglia, 10, 0, 0, "NON_IRRIGARE");

    [Fact]
    public void Testi_SuggerimentoMarginiECalcolo()
    {
        Assert.Equal("Irrigare", TestiBilancio.Suggerimento("IRRIGARE"));
        Assert.Equal("Dato non valido", TestiBilancio.Suggerimento("DATO_NON_VALIDO"));
        Assert.Equal("-0,10", TestiBilancio.Margine(-0.55, -0.45));
        Assert.Equal("+0,25", TestiBilancio.Margine(-0.2, -0.45));
        Assert.Equal("—", TestiBilancio.Margine(-0.2, null));
        Assert.Equal("-0,45 MPa", TestiBilancio.Mpa(-0.45));
        Assert.Contains("Non ancora calcolato", TestiBilancio.Calcolo(null));
        var c = new CalcoloBilancioDto(new DateTime(2026, 7, 3, 2, 10, 0, DateTimeKind.Utc), 2026, new(2026, 1, 1), new(2026, 7, 10), "1.2", "ComeOggi",
            900, null, null, false);
        Assert.StartsWith("Calcolato il 3/7/2026 alle 04:10", TestiBilancio.Calcolo(c));
    }

    [Fact]
    public void Soglia_SenzaStileOSenzaFase()
    {
        StatoVignetoBilancioDto S(double? soglia, bool haStile) => new(1, -0.3, soglia, "N_A", 0, 0, 100, 1, 0, 0, false, false, [], haStile, [], []);
        Assert.Equal("-0,40 MPa", TestiBilancio.Soglia(S(-0.4, true)));
        Assert.Equal("senza fase", TestiBilancio.Soglia(S(null, true)));
        Assert.Equal("senza stile", TestiBilancio.Soglia(S(null, false)));
    }

    [Fact]
    public void Grafici_PeriodoScaleELineeSpezzateDoveMancaIlDato()
    {
        var oggi = new DateOnly(2026, 7, 3);
        var giorni = Enumerable.Range(0, 100).Select(i => oggi.AddDays(-92 + i))
            .Select(d => G(d, 80, d.Day == 15 ? null : 120, -0.3, d.Month >= 6 ? -0.45 : null)).ToList();
        var b = new BilancioVignetoDto(1, oggi, null, giorni, [], [], [new PotenzialeMisuratoDto(oggi.AddDays(-3), -0.9, "x")], null);

        var acqua = GraficoBilancio.Acqua(b);
        Assert.Equal(oggi.AddDays(-GraficoBilancio.GiorniPrima), acqua.Da);
        Assert.Equal(oggi.AddDays(7), acqua.A);
        Assert.Equal(126, acqua.MaxY, 6);
        Assert.Equal(GraficoBilancio.Margine, acqua.X(acqua.Da), 6);
        // AWC manca il 15 di ogni mese: la linea si spezza (dal 3/5: 15/5, 15/6 → 3 tratti).
        Assert.Equal(3, acqua.Linee(giorni.Select(g => (g.Data, g.AwcMm))).Count);

        var potenziale = GraficoBilancio.Potenziale(b);
        Assert.Equal(0, potenziale.MaxY);
        Assert.Equal(-0.99, potenziale.MinY, 6);
        Assert.Contains(0.0, potenziale.Tacche());
        Assert.True(potenziale.Y(-0.9) > potenziale.Y(-0.3));
    }
}
