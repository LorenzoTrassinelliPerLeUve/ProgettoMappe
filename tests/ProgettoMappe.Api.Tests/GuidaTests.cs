using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgettoMappe.Web.Guida;
using Xunit;

namespace ProgettoMappe.Api.Tests;

// Il Web si avvia per davvero (TestServer): AccessoOptions sta nell'assembly del Web e fa da
// "punto d'ingresso" (anche l'Api ha un Program, e i due nomi si scontrerebbero).
public class GuidaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task La_guida_risponde_anche_senza_login(bool accessoAbilitato)
    {
        await using var web = new WebApplicationFactory<ProgettoMappe.Web.Accesso.AccessoOptions>()
            .WithWebHostBuilder(b => b.UseSetting("Accesso:Abilitato", accessoAbilitato.ToString()));
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var risposta = await client.GetAsync("/guida");

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var html = await risposta.Content.ReadAsStringAsync();
        Assert.Contains("Come si usa", html);
        Assert.Contains("href=\"https://mappe.37-27-212-47.sslip.io\"", html);
        Assert.Contains("href=\"guida\"", html);
    }

    [Fact]
    public void Il_markdown_diventa_elenco_grassetti_e_link_senza_html_libero()
    {
        var html = TestoGuida.InHtml("1. **Entra**: https://esempio.it\n2. <script>x</script>");

        Assert.Contains("<ol>", html);
        Assert.Contains("<strong>Entra</strong>", html);
        Assert.Contains("<a href=\"https://esempio.it\">", html);
        Assert.DoesNotContain("<script>", html);
    }
}
