using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Quello che serve ai contenitori (task #210): l'indirizzo di salute dei due programmi e le chiavi
/// del Web su una cartella (volume), così un nuovo avvio non butta fuori chi è entrato.
/// </summary>
public class ContenitoreTests
{
    [Fact]
    public async Task Salute_del_web_risponde_senza_login()
    {
        await using var web = new WebApplicationFactory<ProgettoMappe.Web.Accesso.AccessoOptions>()
            .WithWebHostBuilder(b => b.UseSetting("Accesso:Abilitato", "true"));
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var risposta = await client.GetAsync("/salute");

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Equal("ok", await risposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Salute_dell_api_risponde_senza_chiave()
    {
        await using var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>()
            .WithWebHostBuilder(b => b.UseSetting("ChiaveApi:Valore", "segreta-di-prova"));
        var client = api.CreateClient();

        var salute = await client.GetAsync("/salute");
        var dati = await client.GetAsync("/api/aziende");

        Assert.Equal(HttpStatusCode.OK, salute.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, dati.StatusCode);
    }

    [Fact]
    public async Task Chiavi_del_web_nella_cartella_indicata()
    {
        var cartella = Path.Combine(Path.GetTempPath(), "mappe-chiavi-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var web = new WebApplicationFactory<ProgettoMappe.Web.Accesso.AccessoOptions>()
                .WithWebHostBuilder(b => b.UseSetting("DataProtection:Cartella", cartella));
            var client = web.CreateClient();

            // La pagina di login ha il modulo con l'antiforgery: la prima chiave nasce qui.
            var risposta = await client.GetAsync("/accesso");

            Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
            Assert.NotEmpty(Directory.GetFiles(cartella, "key-*.xml"));
        }
        finally
        {
            if (Directory.Exists(cartella))
                Directory.Delete(cartella, true);
        }
    }
}
