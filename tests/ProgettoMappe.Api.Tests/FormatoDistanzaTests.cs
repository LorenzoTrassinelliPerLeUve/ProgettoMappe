using ProgettoMappe.Web.Formati;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class FormatoDistanzaTests
{
    [Theory]
    [InlineData(118, "120 m")]
    [InlineData(3, "10 m")]
    [InlineData(994, "990 m")]
    [InlineData(996, "1 km")]
    [InlineData(1240, "1,2 km")]
    [InlineData(12_600, "13 km")]
    [InlineData(1_234_000, "1.234 km")]
    public void ArrotondaComeSiDiceAVoce(double metri, string atteso) =>
        Assert.Equal(atteso, FormatoDistanza.Testo(metri));

    [Fact]
    public void MessaggioColNomeDelVigneto() =>
        Assert.Equal("Sei a 120 m da Chardonnay Nemška Vas", FormatoDistanza.Messaggio(121, "Chardonnay Nemška Vas"));
}
