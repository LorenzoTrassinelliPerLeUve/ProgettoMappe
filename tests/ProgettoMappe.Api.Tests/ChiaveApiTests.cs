using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Servizi;
using Xunit;
using ChiaveApiWeb = ProgettoMappe.Web.Services.ChiaveApi;

namespace ProgettoMappe.Api.Tests;

/// <summary>Chiave condivisa Web → Api (intestazione X-Mappe-Chiave) e popup del vigneto come testo.</summary>
public class ChiaveApiTests
{
    private const string Chiave = "chiave-di-prova";

    /// <summary>Passa una richiesta nel controllo: true se arriva al resto dell'Api.</summary>
    private static (bool passata, int stato) Richiesta(string? configurata, string percorso, params string[] intestazioni)
    {
        var passata = false;
        var controllo = new ControlloChiaveApi(
            _ => { passata = true; return Task.CompletedTask; },
            Options.Create(new ChiaveApiOptions { Valore = configurata }),
            NullLogger<ControlloChiaveApi>.Instance);

        var contesto = new DefaultHttpContext();
        contesto.Request.Path = percorso;
        if (intestazioni.Length > 0)
            contesto.Request.Headers[ControlloChiaveApi.Intestazione] = intestazioni;

        controllo.InvokeAsync(contesto).GetAwaiter().GetResult();
        return (passata, contesto.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/aziende")]
    [InlineData("/api/aziende/1/vigneti")]
    [InlineData("/api/aziende/1/vigneti/geojson")]
    [InlineData("/api/aziende/1/indici-bigot")]
    [InlineData("/api/accesso/verifica")]
    public void Senza_chiave_la_richiesta_e_rifiutata(string percorso)
    {
        var (passata, stato) = Richiesta(Chiave, percorso);

        Assert.False(passata);
        Assert.Equal(StatusCodes.Status403Forbidden, stato);
    }

    [Theory]
    [InlineData("")]
    [InlineData("chiave-sbagliata")]
    [InlineData("CHIAVE-DI-PROVA")]
    [InlineData("chiave-di-prova-e-altro")]
    public void Con_chiave_sbagliata_la_richiesta_e_rifiutata(string ricevuta)
    {
        var (passata, stato) = Richiesta(Chiave, "/api/aziende", ricevuta);

        Assert.False(passata);
        Assert.Equal(StatusCodes.Status403Forbidden, stato);
    }

    [Fact]
    public void Intestazione_ripetuta_non_vale()
    {
        Assert.False(Richiesta(Chiave, "/api/aziende", Chiave, Chiave).passata);
    }

    [Fact]
    public void Con_la_chiave_giusta_la_richiesta_passa()
    {
        var (passata, stato) = Richiesta(Chiave, "/api/aziende", Chiave);

        Assert.True(passata);
        Assert.Equal(StatusCodes.Status200OK, stato);
    }

    [Fact]
    public void Fuori_da_api_il_controllo_non_si_applica()
    {
        Assert.True(Richiesta(Chiave, "/swagger/index.html").passata);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Senza_chiave_configurata_il_controllo_e_spento(string? configurata)
    {
        Assert.True(Richiesta(configurata, "/api/aziende").passata);
    }

    private static HttpClient ClientWeb(string? chiave)
    {
        var valori = new Dictionary<string, string?> { ["Api:BaseUrl"] = "http://127.0.0.1:5201" };
        if (chiave is not null)
            valori["ChiaveApi:Valore"] = chiave;
        var client = new HttpClient();
        ChiaveApiWeb.Configura(client, new ConfigurationBuilder().AddInMemoryCollection(valori).Build());
        return client;
    }

    [Fact]
    public void Il_web_manda_la_chiave_configurata_nell_intestazione_che_l_api_controlla()
    {
        using var client = ClientWeb(Chiave);

        Assert.Equal(ControlloChiaveApi.Intestazione, ChiaveApiWeb.Intestazione);
        Assert.Equal([Chiave], client.DefaultRequestHeaders.GetValues(ControlloChiaveApi.Intestazione));
        Assert.Equal(new Uri("http://127.0.0.1:5201"), client.BaseAddress);
    }

    [Fact]
    public void Senza_chiave_configurata_il_web_non_manda_l_intestazione()
    {
        using var client = ClientWeb(null);

        Assert.False(client.DefaultRequestHeaders.Contains(ChiaveApiWeb.Intestazione));
    }

    [Fact]
    public void La_chiave_non_e_nei_file_di_configurazione_del_repository()
    {
        var src = Path.GetDirectoryName(ConfigurazioneMappaTests.TrovaCartellaWeb())!;
        foreach (var file in Directory.GetFiles(src, "appsettings*.json", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;
            Assert.DoesNotContain("ChiaveApi", File.ReadAllText(file));
        }
    }

    [Fact]
    public void Il_nome_del_vigneto_non_entra_mai_come_html()
    {
        // I nomi arrivano da 4Grapes: in mappa.js solo textContent/setText/setDOMContent.
        var js = File.ReadAllText(Path.Combine(ConfigurazioneMappaTests.TrovaCartellaWeb(), "wwwroot", "js", "mappa.js"));

        Assert.DoesNotContain("setHTML", js);
        Assert.DoesNotContain("innerHTML", js);
        Assert.DoesNotContain("insertAdjacentHTML", js);
        Assert.Contains("nome.textContent = feature.properties.nome", js);
    }
}
