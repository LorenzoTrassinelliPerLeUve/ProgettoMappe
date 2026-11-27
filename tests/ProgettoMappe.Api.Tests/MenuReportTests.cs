using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgettoMappe.Web.Portale;
using ProgettoMappe.Web.Report;
using Xunit;

namespace ProgettoMappe.Api.Tests;

// Menù «Report» unico (task #413, tappa R0 della Reportistica).
public class MenuReportTests
{
    private static readonly string[] VociTolteDallaBarra =
        ["indice-bigot", "report-fine-stagione", "report-settimanale", "report-settimanale-laore", "report-analisi-suolo", "nutrizione"];

    private static IReadOnlyList<string> Pagine()
    {
        var cartella = Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "Components", "Pages");
        return Directory.GetFiles(cartella, "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadLines).Where(r => r.StartsWith("@page \"", StringComparison.Ordinal))
            .Select(r => r.Split('"')[1]).ToList();
    }

    [Fact]
    public void OgniVocePronta_ApreUnaPaginaCheEsiste()
    {
        var pagine = Pagine();
        var voci = MenuReport.Gruppi.SelectMany(g => g.Voci).Where(v => v.Pronta).ToList();

        Assert.NotEmpty(voci);
        Assert.All(voci, v =>
        {
            var indirizzo = v.Indirizzo!.Split('?')[0];
            if (indirizzo.StartsWith("/nutrizione/", StringComparison.Ordinal))
            {
                Assert.Contains(MenuNutrizione.Voci, n => "/nutrizione/" + n.Chiave == indirizzo);
            }
            else
            {
                Assert.Contains(indirizzo, pagine);
            }
        });
        Assert.Contains(MenuReport.Percorso, pagine);
    }

    [Fact]
    public void LeVociInArrivo_HannoLaLoroTappa_ELeAltreNo()
    {
        var voci = MenuReport.Gruppi.SelectMany(g => g.Voci).ToList();
        Assert.All(voci, v => Assert.Equal(v.Pronta, v.Tappa is null));
        Assert.All(voci.Where(v => !v.Pronta), v => Assert.Matches("^R[0-9]+$", v.Tappa!));
    }

    [Fact]
    public void LaBarraAlta_HaReportAlPostoDelleSeiVoci()
    {
        var layout = File.ReadAllText(Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "Components", "Layout", "MainLayout.razor"));

        Assert.Contains("<TendinaReport />", layout);
        Assert.Contains("href=\"report\"", layout);
        Assert.All(VociTolteDallaBarra, v => Assert.DoesNotContain($"href=\"{v}\"", layout));
        // Tutte le voci tolte restano raggiungibili dal menù Report.
        Assert.All(VociTolteDallaBarra, v => Assert.Contains("/" + v, MenuReport.Indirizzi));
    }

    [Theory]
    [InlineData("report", true)]
    [InlineData("/report-settimanale?azienda=425", true)]
    [InlineData("indice-bigot/parita", true)]
    [InlineData("nutrizione/analisi-uve", true)]
    [InlineData("", false)]
    [InlineData("bilancio-idrico", false)]
    [InlineData("reportistica", false)]
    [InlineData("portale", false)]
    public void Contiene_RiconosceLePagineDelMenu(string percorso, bool atteso) =>
        Assert.Equal(atteso, MenuReport.Contiene(percorso));

    [Fact]
    public async Task LaGuida_MostraLaTendinaReport()
    {
        await using var web = new WebApplicationFactory<ProgettoMappe.Web.Accesso.AccessoOptions>()
            .WithWebHostBuilder(b => b.UseSetting("Accesso:Abilitato", "false"));
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var risposta = await client.GetAsync("/guida");

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var html = await risposta.Content.ReadAsStringAsync();
        Assert.Contains("class=\"tendina-report", html);
        Assert.Contains("href=\"report-settimanale\"", html);
        Assert.Contains("href=\"report\"", html);
        Assert.Contains("js/menu.js", html);
    }
}
