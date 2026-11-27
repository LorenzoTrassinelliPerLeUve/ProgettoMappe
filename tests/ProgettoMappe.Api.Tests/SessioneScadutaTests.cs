using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgettoMappe.Web.Accesso;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Task #222: dopo un rilascio con chiavi nuove i cookie del browser non si decifrano più. Niente 400: si torna
/// all'accesso con «la sessione è scaduta» e i cookie vecchi cancellati. Il riavvio si simula con due Web su
/// cartelle di chiavi diverse.
/// </summary>
public sealed class SessioneScadutaTests : IDisposable
{
    private readonly List<string> _cartelle = [];

    private WebApplicationFactory<AccessoOptions> Web()
    {
        var cartella = Path.Combine(Path.GetTempPath(), "mappe-chiavi-" + Guid.NewGuid().ToString("N"));
        _cartelle.Add(cartella);
        return new WebApplicationFactory<AccessoOptions>().WithWebHostBuilder(b => b
            .UseSetting("Accesso:Abilitato", "true")
            .UseSetting("DataProtection:Cartella", cartella));
    }

    private static HttpClient Client(WebApplicationFactory<AccessoOptions> web) =>
        web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

    public void Dispose()
    {
        foreach (var cartella in _cartelle.Where(Directory.Exists))
            Directory.Delete(cartella, true);
    }

    [Fact]
    public async Task Modulo_di_prima_del_riavvio_torna_all_accesso_non_400()
    {
        // Prima del riavvio: il browser apre la pagina di accesso e riceve cookie e token.
        string cookie, token;
        await using (var prima = Web())
        {
            var pagina = await Client(prima).GetAsync("/accesso?ReturnUrl=%2Favvisi");
            var html = await pagina.Content.ReadAsStringAsync();
            cookie = pagina.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(".AspNetCore.Antiforgery."));
            cookie = cookie[..cookie.IndexOf(';')];
            token = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
            Assert.NotEmpty(token);
        }

        // Dopo il riavvio (chiavi nuove) invia il modulo.
        await using var dopo = Web();
        var richiesta = new HttpRequestMessage(HttpMethod.Post, "/accesso?ReturnUrl=%2Favvisi")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = "accesso",
                ["__RequestVerificationToken"] = token,
                ["Dati.NomeUtente"] = "mario",
                ["Dati.Password"] = "x",
            }),
        };
        richiesta.Headers.Add("Cookie", cookie);
        var risposta = await Client(dopo).SendAsync(richiesta);

        Assert.Equal(HttpStatusCode.Redirect, risposta.StatusCode);
        Assert.Equal("/accesso?scaduta=1&ReturnUrl=%2Favvisi", risposta.Headers.Location?.OriginalString);
        var nomeCookie = cookie[..cookie.IndexOf('=')];
        Assert.Contains(risposta.Headers.GetValues("Set-Cookie"), c => c.StartsWith(nomeCookie + "=;") && c.Contains("expires=Thu, 01 Jan 1970"));

        var accesso = await Client(dopo).GetAsync(risposta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, accesso.StatusCode);
        Assert.Contains("accedi di nuovo", await accesso.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cookie_di_accesso_non_decifrabile_si_cancella_e_si_torna_all_accesso()
    {
        await using var web = Web();
        var richiesta = new HttpRequestMessage(HttpMethod.Get, "/avvisi");
        richiesta.Headers.Add("Cookie", SessioneScaduta.NomeCookieAccesso + "=CfDJ8vecchio-cookie-di-ieri");

        var risposta = await Client(web).SendAsync(richiesta);

        Assert.Equal(HttpStatusCode.Redirect, risposta.StatusCode);
        var dove = risposta.Headers.Location!.ToString();
        Assert.Contains("/accesso?ReturnUrl=%2Favvisi", dove);
        Assert.EndsWith("&scaduta=1", dove);
        Assert.Contains(risposta.Headers.GetValues("Set-Cookie"), c => c.StartsWith(SessioneScaduta.NomeCookieAccesso + "=;"));
    }

    [Fact]
    public async Task Cookie_vecchio_sulla_pagina_di_accesso_mostra_il_messaggio()
    {
        await using var web = Web();
        var richiesta = new HttpRequestMessage(HttpMethod.Get, "/accesso");
        richiesta.Headers.Add("Cookie", SessioneScaduta.NomeCookieAccesso + "=CfDJ8vecchio");

        var risposta = await Client(web).SendAsync(richiesta);

        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Contains("accedi di nuovo", await risposta.Content.ReadAsStringAsync());
        Assert.Contains(risposta.Headers.GetValues("Set-Cookie"), c => c.StartsWith(SessioneScaduta.NomeCookieAccesso + "=;"));
    }

    [Fact]
    public async Task Accesso_senza_cookie_vecchi_non_parla_di_sessione_scaduta()
    {
        await using var web = Web();

        var html = await Client(web).GetStringAsync("/accesso");

        Assert.DoesNotContain("accedi di nuovo", html);
    }

    [Fact]
    public async Task Esci_con_token_vecchio_torna_all_accesso_non_400()
    {
        await using var web = Web();
        var richiesta = new HttpRequestMessage(HttpMethod.Post, "/accesso/esci")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = "vecchio" }),
        };
        richiesta.Headers.Add("Cookie", SessioneScaduta.NomeCookieAccesso + "=CfDJ8vecchio");

        var risposta = await Client(web).SendAsync(richiesta);

        Assert.Equal(HttpStatusCode.Redirect, risposta.StatusCode);
        Assert.Equal("/accesso?scaduta=1", risposta.Headers.Location?.OriginalString);
    }

    [Fact]
    public void Cartella_delle_chiavi_non_scrivibile_torna_alle_predefinite()
    {
        Assert.Null(CartellaChiavi.Prepara("", out var nessuno));
        Assert.Null(nessuno);

        Assert.Null(CartellaChiavi.Prepara("/proc/mappe-chiavi-non-si-puo", out var problema));
        Assert.NotNull(problema);
    }

    [Fact]
    public void Cartella_delle_chiavi_si_crea_se_manca()
    {
        var cartella = Path.Combine(Path.GetTempPath(), "mappe-chiavi-" + Guid.NewGuid().ToString("N"), "chiavi-web");
        _cartelle.Add(Path.GetDirectoryName(cartella)!);

        Assert.Equal(cartella, CartellaChiavi.Prepara(cartella, out _));
        Assert.True(Directory.Exists(cartella));
        Assert.Empty(Directory.GetFiles(cartella));
    }
}
