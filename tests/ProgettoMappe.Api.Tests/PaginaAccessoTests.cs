using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Contracts;
using ProgettoMappe.Contracts.Accesso;
using ProgettoMappe.Web.Accesso;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Task #400: la pagina di accesso di Mappe (<c>/accesso</c>) è neutra (niente menù), dice dove si entra, chiede l'email,
/// ha «Mostra», «Password dimenticata?», IT/EN e il piè di pagina. L'accesso unico dell'Api è in <see cref="OidcPerleuveTests"/>.
/// </summary>
public sealed class PaginaAccessoTests : IDisposable
{
    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "mappe-accesso-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_cartella)) Directory.Delete(_cartella, true);
    }

    private sealed class VerificaFinta : IVerificaCredenziali
    {
        public Task<UtenteAccessoDto?> VerificaAsync(string nomeUtente, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(nomeUtente == "mario.rossi@esempio.it" && password == "giusta" ? new UtenteAccessoDto(7, "Mario Rossi") : null);
    }

    private WebApplicationFactory<AccessoOptions> Web(string? privacy = null, string? assistenza = null) =>
        new WebApplicationFactory<AccessoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("Accesso:Abilitato", "true");
            b.UseSetting("Accesso:AccessoUnico:Abilitato", "false"); // la pagina del login di prima (SSO spento)
            b.UseSetting("DataProtection:Cartella", _cartella);
            if (privacy is not null) b.UseSetting("Accesso:Privacy", privacy);
            if (assistenza is not null) b.UseSetting("Accesso:Assistenza", assistenza);
            b.ConfigureTestServices(s => s.AddSingleton<IVerificaCredenziali, VerificaFinta>());
        });

    private static async Task<string> PaginaAsync(HttpClient client, string indirizzo, string? lingueBrowser = null)
    {
        var richiesta = new HttpRequestMessage(HttpMethod.Get, indirizzo);
        if (lingueBrowser is not null) richiesta.Headers.Add("Accept-Language", lingueBrowser);
        var risposta = await client.SendAsync(richiesta);
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        return await risposta.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Pagina_neutra_senza_menu_con_email_mostra_password_e_recupero()
    {
        await using var web = Web();
        var html = await PaginaAsync(web.CreateClient(), "/accesso");

        Assert.DoesNotContain("Bilancio idrico", html);
        Assert.DoesNotContain("Report settimanale", html);
        Assert.DoesNotContain("href=\"portale\"", html);
        Assert.Contains("Stai entrando in: <strong>Mappe 4Grapes</strong>", html);
        Assert.Matches("<input name=\"Dati.NomeUtente\"[^>]*type=\"email\"[^>]*autocomplete=\"username\"", html);
        Assert.Contains("autocomplete=\"current-password\"", html);
        Assert.Contains("class=\"mostra-password\"", html);
        Assert.Contains("onclick=\"var i=this.previousElementSibling", html);
        Assert.Contains("href=\"/perleuve/password-dimenticata?lingua=it\"", html);
        Assert.Contains("Perleuve · 4Grapes", html);
        Assert.DoesNotContain(">Privacy<", html); // non configurata: non compare
    }

    [Theory]
    [InlineData(null, "/accesso", "Accedi")]
    [InlineData("hr-HR,hr;q=0.9", "/accesso", "Sign in")]
    [InlineData("en-GB", "/accesso", "Forgot your password?")]
    [InlineData("en-GB", "/accesso?lingua=it", "Password dimenticata?")]
    [InlineData(null, "/accesso?lingua=en", "You are signing in to")]
    public async Task Lingua_dal_browser_o_scelta(string? lingueBrowser, string indirizzo, string atteso)
    {
        await using var web = Web();
        Assert.Contains(atteso, await PaginaAsync(web.CreateClient(), indirizzo, lingueBrowser));
    }

    [Fact]
    public async Task Piede_con_privacy_e_assistenza_se_configurate()
    {
        await using var web = Web("https://www.esempio.it/privacy", "assistenza@esempio.it");
        var html = await PaginaAsync(web.CreateClient(), "/accesso");

        Assert.Contains("<a href=\"https://www.esempio.it/privacy\">Privacy</a>", html);
        Assert.Contains("<a href=\"mailto:assistenza@esempio.it\">assistenza@esempio.it</a>", html);
    }

    /// <summary>Decisione del proprietario (27/11): nel piè «Perleuve · 4Grapes · support@4grapes.it», niente privacy per ora.</summary>
    [Fact]
    public async Task Piede_predefinito_con_l_email_dell_assistenza_e_senza_privacy()
    {
        await using var web = Web();
        var html = await PaginaAsync(web.CreateClient(), "/accesso");

        Assert.Contains("<a href=\"mailto:support@4grapes.it\">support@4grapes.it</a>", html);
        Assert.DoesNotContain(">Privacy<", html);
    }

    [Fact]
    public async Task Email_inesistente_e_password_sbagliata_danno_lo_stesso_messaggio()
    {
        await using var web = Web();
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        async Task<string> EntraAsync(string email, string password)
        {
            var pagina = await PaginaAsync(client, "/accesso?lingua=en");
            var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
            var risposta = await client.PostAsync("/accesso?lingua=en", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = "accesso", ["__RequestVerificationToken"] = token,
                ["Dati.NomeUtente"] = email, ["Dati.Password"] = password,
            }));
            return await risposta.Content.ReadAsStringAsync();
        }

        var inesistente = await EntraAsync("nessuno@esempio.it", "giusta");
        var sbagliata = await EntraAsync("mario.rossi@esempio.it", "sbagliata");
        const string messaggio = "Incorrect email or password.";
        Assert.Contains(messaggio, inesistente);
        Assert.Contains(messaggio, sbagliata);
        Assert.Contains("value=\"mario.rossi@esempio.it\"", sbagliata);
    }

    [Theory]
    [InlineData(null, null, "it")]
    [InlineData(null, "", "it")]
    [InlineData(null, "it-IT,it;q=0.9,en;q=0.8", "it")]
    [InlineData(null, "mk-MK,mk;q=0.9", "en")]
    [InlineData(null, "en;q=0.5, it;q=0.9", "it")]
    [InlineData("EN", "it-IT", "en")]
    [InlineData("en-US", null, "en")]
    [InlineData("xx", "it", "it")]
    public void Lingua_della_pagina(string? scelta, string? acceptLanguage, string attesa) =>
        Assert.Equal(attesa, TestiAccesso.Lingua(scelta, acceptLanguage));

    [Fact]
    public void Ogni_testo_ha_italiano_e_inglese() =>
        Assert.All(TestiAccesso.Voci, v => Assert.False(string.IsNullOrWhiteSpace(v.Value.It) || string.IsNullOrWhiteSpace(v.Value.En), v.Key));

    [Theory]
    [InlineData("assistenza@esempio.it", "mailto:assistenza@esempio.it")]
    [InlineData("mailto:a@esempio.it", "mailto:a@esempio.it")]
    [InlineData("https://www.esempio.it/privacy", "https://www.esempio.it/privacy")]
    [InlineData("/privacy", "/privacy")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("//altro.esempio", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Collegamenti_del_piede(string? valore, string? atteso) =>
        Assert.Equal(atteso, TestiAccesso.CollegamentoPiede(valore));
}
