using ProgettoMappe.Web.Accesso;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class AccessoTests
{
    [Theory]
    [InlineData("/mappa/12", "/mappa/12")]
    [InlineData("/mappa/12?tema=vigoria", "/mappa/12?tema=vigoria")]
    [InlineData("/", "/")]
    public void Url_di_ritorno_locale_viene_mantenuto(string url, string atteso)
    {
        Assert.Equal(atteso, UrlRitorno.Sicuro(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://altro-sito.example/")]
    [InlineData("//altro-sito.example/")]
    [InlineData("/\\altro-sito.example/")]
    [InlineData("mappa/12")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/mappa\r\nSet-Cookie: x=1")]
    public void Url_di_ritorno_esterno_o_non_valido_torna_alla_home(string? url)
    {
        Assert.Equal(UrlRitorno.Predefinito, UrlRitorno.Sicuro(url));
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(1, 1)]
    [InlineData(0, 8)]
    [InlineData(-3, 8)]
    [InlineData(100000, 8)]
    public void Durata_sessione_fuori_scala_usa_otto_ore(int ore, int oreAttese)
    {
        var opzioni = new AccessoOptions { DurataSessioneOre = ore };
        Assert.Equal(TimeSpan.FromHours(oreAttese), opzioni.DurataSessione());
    }

    [Fact]
    public void Accesso_e_spento_per_default()
    {
        Assert.False(new AccessoOptions().Abilitato);
    }
}
