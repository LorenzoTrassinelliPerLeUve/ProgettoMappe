using ProgettoMappe.Web.Formati;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class FormatoSuperficieTests
{
    [Theory]
    [InlineData("1234.5", "1.234,5 ha")]
    [InlineData("0.35", "0,35 ha")]
    [InlineData("2", "2 ha")]
    [InlineData("12.3456", "12,35 ha")]
    [InlineData("0", "0 ha")]
    public void FormattaAllItaliana(string ettari, string atteso) =>
        Assert.Equal(atteso, FormatoSuperficie.Testo(decimal.Parse(ettari, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void SenzaSuperficieSpiegaPerche() =>
        Assert.Equal("superficie non registrata in 4Grapes", FormatoSuperficie.Testo(null));
}
