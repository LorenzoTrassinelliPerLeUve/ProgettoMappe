using ProgettoMappe.Web.Filtri;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class LinkMappaTests
{
    [Fact]
    public void Lo_stato_completo_fa_andata_e_ritorno_nell_indirizzo()
    {
        var link = new LinkMappa(3, 12, -1, "punteggio", 2025, new VistaMappa(13.123456789, 45.987654321, 14.567), Da: 2022, Vigneto: 16792);

        var indirizzo = link.Indirizzo(42);

        Assert.Equal("mappa/42?gruppo=3&vigna=12&vitigno=-1&vigneto=16792&tema=punteggio&da=2022&anno=2025&centro=13.12346,45.98765&zoom=14.57", indirizzo);
        Assert.Equal(link with { Vista = new VistaMappa(13.12346, 45.98765, 14.57) }, LinkMappa.Leggi("https://host/" + indirizzo));
    }

    [Fact]
    public void Senza_stato_resta_solo_l_azienda()
    {
        Assert.Equal("mappa/7", LinkMappa.Vuoto.Indirizzo(7));
        Assert.Equal(LinkMappa.Vuoto, LinkMappa.Leggi("https://host/mappa/7"));
    }

    [Fact]
    public void Valori_non_validi_si_ignorano_senza_errori()
    {
        var link = LinkMappa.Leggi("/mappa/7?gruppo=abc&vigna=&anno=2025.5&da=ieri&vigneto=x&tema=%20&centro=13.1&zoom=14");

        Assert.Equal(LinkMappa.Vuoto, link);
    }

    [Theory]
    [InlineData("200,45", "10")] // longitudine fuori scala
    [InlineData("13,95", "10")] // latitudine fuori scala
    [InlineData("13,45", "30")] // zoom fuori scala
    [InlineData("13,45", "NaN")]
    [InlineData("13,45", "")] // centro senza zoom
    public void Un_inquadratura_non_valida_lascia_inquadrare_i_vigneti(string centro, string zoom)
    {
        Assert.Null(LinkMappa.Leggi($"/mappa/7?centro={centro}&zoom={zoom}").Vista);
    }

    [Fact]
    public void Il_frammento_non_entra_nei_valori_e_i_testi_vengono_codificati()
    {
        Assert.Equal("vigoria", LinkMappa.Leggi("/mappa/7?tema=vigoria#sopra").Tema);
        Assert.Equal("mappa/7?tema=a%26b", (LinkMappa.Vuoto with { Tema = "a&b" }).Indirizzo(7));
    }
}
