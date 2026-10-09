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

    private sealed class OrologioFinto : TimeProvider
    {
        public DateTimeOffset Adesso { get; set; } = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Adesso;
    }

    [Fact]
    public void Dopo_cinque_errori_il_nome_utente_e_bloccato_anche_con_maiuscole_diverse()
    {
        var limite = new LimiteTentativi(new OrologioFinto());
        for (var i = 0; i < LimiteTentativi.MaxErrori - 1; i++)
            limite.RegistraErrore("mario@esempio.it");
        Assert.False(limite.Bloccato("mario@esempio.it"));

        limite.RegistraErrore(" MARIO@esempio.it ");

        Assert.True(limite.Bloccato("mario@esempio.it"));
        Assert.False(limite.Bloccato("altro@esempio.it"));
    }

    [Fact]
    public void Il_blocco_finisce_dopo_la_finestra()
    {
        var orologio = new OrologioFinto();
        var limite = new LimiteTentativi(orologio);
        for (var i = 0; i < LimiteTentativi.MaxErrori; i++)
            limite.RegistraErrore("mario");

        orologio.Adesso += LimiteTentativi.Finestra;

        Assert.False(limite.Bloccato("mario"));
        limite.RegistraErrore("mario");
        Assert.False(limite.Bloccato("mario"));
    }

    [Fact]
    public void Un_accesso_riuscito_azzera_gli_errori()
    {
        var limite = new LimiteTentativi(new OrologioFinto());
        for (var i = 0; i < LimiteTentativi.MaxErrori - 1; i++)
            limite.RegistraErrore("mario");

        limite.RegistraSuccesso("mario");
        limite.RegistraErrore("mario");

        Assert.False(limite.Bloccato("mario"));
    }
}
