using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Web.Accesso;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Task #403: single sign-on. Mappe Web client OIDC dell'accesso unico (giro completo con l'Api vera in memoria),
/// rientro senza password in un'altra app, uscita unica (canale server verso Mappe), regole della sessione, registro
/// degli accessi e «I tuoi servizi».
/// </summary>
public sealed class SingleSignOnTests : IDisposable
{
    private const string Email = "mario.rossi@esempio.it";
    private const string Password = "vendemmia-2026";
    private const string Sito = "https://localhost";
    private const string RitornoApp = "https://4sprayers.esempio/callback";

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "mappe-sso-" + Guid.NewGuid().ToString("N"));

    public SingleSignOnTests() => Directory.CreateDirectory(_cartella);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_cartella, true);
    }

    private sealed class Finto : IUtentiRepository, IPermessiRepository, IAccessoUnicoRepository, ICategorieUtentiRepository
    {
        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CredenzialiUtente>>(string.Equals(loginName.Trim(), Email, StringComparison.OrdinalIgnoreCase)
                ? [new CredenzialiUtente(4242, "Mario", "Rossi", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Password))))]
                : []);

        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(new PermessiUtente(idUtente, true, false, true, false, false, null, new HashSet<string> { "4SPRAYERS" }));

        public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<AnagraficaUtente?>(new AnagraficaUtente(idUtente, Email, "Mario", "Rossi", true));

        public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AziendaUtente>>([new AziendaUtente(425, "Cantina Uno", true, "Titolare")]);

        public Task<IReadOnlyList<AbilitazionePersonale>> GetAbilitazioniPersonaliAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AbilitazionePersonale>>([]);

        public Task<AbilitazionePersonale?> GetAbilitazionePersonaleAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<AbilitazionePersonale?>(null);

        public Task<IReadOnlySet<string>> GetCodiciAbilitazioneAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public Task<bool> EsisteLoginAsync(string loginName, CancellationToken ct = default) => Task.FromResult(false);

        public Task<bool> AbbinatoAsync(int idUtente, int idEnte, CancellationToken ct = default) => Task.FromResult(false);

        public Task<SegnaliCategoria?> GetSegnaliAsync(int idUtente, IReadOnlyCollection<int> gruppiConsulenza, CancellationToken ct = default) =>
            Task.FromResult<SegnaliCategoria?>(null);

        public Task<string> GetCategoriaAsync(int idUtente, CancellationToken ct = default) => Task.FromResult(CategorieUtente.Consulenza);
    }

    /// <summary>Gestore HTTP da collegare dopo (Api e Web si chiamano a vicenda).</summary>
    private sealed class Rinvio : HttpMessageHandler
    {
        public HttpMessageHandler? Verso { get; set; }
        public List<string> Percorsi { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage richiesta, CancellationToken ct)
        {
            Percorsi.Add(richiesta.RequestUri!.AbsolutePath);
            return new HttpMessageInvoker(Verso!, false).SendAsync(richiesta, ct);
        }
    }

    private WebApplicationFactory<AccessoUnicoOptions> Api(Rinvio versoWeb) =>
        new WebApplicationFactory<AccessoUnicoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("AccessoUnico:Database", Path.Combine(_cartella, "accesso.db"));
            b.UseSetting("AccessoUnico:FileSegreti", Path.Combine(_cartella, "segreti.json"));
            b.UseSetting("AccessoUnico:ChiavePrivata", Path.Combine(_cartella, "chiave.pem"));
            b.UseSetting("AccessoUnico:App:mappe:Nome", "Mappe");
            b.UseSetting("AccessoUnico:App:mappe:RedirectUri:0", Sito + "/accesso/ritorno");
            b.UseSetting("AccessoUnico:App:mappe:PostLogoutRedirectUri:0", Sito + "/accesso/uscito");
            b.UseSetting("AccessoUnico:App:mappe:FrontChannelLogoutUri", "/accesso/uscita-frontale");
            b.UseSetting("AccessoUnico:App:mappe:BackChannelLogoutUri", "/accesso/uscita-retro");
            b.UseSetting("AccessoUnico:App:4sprayers:Nome", "4Sprayers");
            b.UseSetting("AccessoUnico:App:4sprayers:Indirizzo", "https://4sprayers.esempio/");
            b.UseSetting("AccessoUnico:App:4sprayers:Abilitazione", "4SPRAYERS");
            b.UseSetting("AccessoUnico:App:4sprayers:Tipo", "browser");
            b.UseSetting("AccessoUnico:App:4sprayers:RedirectUri:0", RitornoApp);
            b.UseSetting("AccessoUnico:App:turni:Nome", "Turni");
            b.UseSetting("AccessoUnico:App:turni:Indirizzo", "https://turni.esempio/");
            b.UseSetting("AccessoUnico:App:turni:Categorie:0", "interno");
            b.UseSetting("AccessoUnico:App:cantina:Nome", "Cantina");
            b.UseSetting("AccessoUnico:App:cantina:Indirizzo", "https://cantina.esempio/");
            b.UseSetting("AccessoUnico:App:cantina:Categorie:0", "consulenza");
            var finto = new Finto();
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IUtentiRepository>(finto);
                s.AddSingleton<IPermessiRepository>(finto);
                s.AddSingleton<IAccessoUnicoRepository>(finto);
                s.AddSingleton<ICategorieUtentiRepository>(finto);
                s.AddHttpClient(UscitaUnica.ClientHttp).ConfigurePrimaryHttpMessageHandler(() => versoWeb);
            });
        });

    private WebApplicationFactory<AccessoOptions> Web(Rinvio versoApi) =>
        new WebApplicationFactory<AccessoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("Accesso:Abilitato", "true");
            b.UseSetting("Accesso:PaginaIniziale", "/mappa");
            b.UseSetting("Accesso:AccessoUnico:Abilitato", "true");
            b.UseSetting("Accesso:AccessoUnico:FileSegreti", Path.Combine(_cartella, "nessuno.json") + ";" + Path.Combine(_cartella, "segreti.json"));
            b.UseSetting("Api:BaseUrl", Sito);
            b.UseSetting("DataProtection:Cartella", Path.Combine(_cartella, "chiavi-web"));
            b.ConfigureTestServices(s => s.AddSingleton(new GestoreApi(versoApi)));
        });

    /// <summary>Un browser: /connect e /perleuve vanno all'Api (come fa il proxy del Web), il resto al Web; redirect seguiti a mano.</summary>
    private sealed class Browser(HttpClient api, HttpClient web)
    {
        public List<string> Visitati { get; } = [];

        public async Task<HttpResponseMessage> VaiAsync(string indirizzo, int passi = 10)
        {
            HttpResponseMessage risposta = null!;
            for (var i = 0; i < passi; i++)
            {
                var u = new Uri(new Uri(Sito), indirizzo);
                Visitati.Add(u.PathAndQuery);
                var client = u.AbsolutePath.StartsWith("/connect/") || u.AbsolutePath.StartsWith("/perleuve/") ? api : web;
                risposta = await client.GetAsync(u.PathAndQuery);
                if (risposta.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.Found) || risposta.Headers.Location is null)
                    return risposta;
                if (risposta.Headers.Location.IsAbsoluteUri && risposta.Headers.Location.Authority != new Uri(Sito).Authority)
                    return risposta; // fuori dal sito (un'altra app)
                indirizzo = risposta.Headers.Location.OriginalString;
            }
            return risposta;
        }

        public Task<HttpResponseMessage> EntraAsync(string ritorno) => api.PostAsync("/perleuve/accesso", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["email"] = Email, ["password"] = Password, ["ritorno"] = ritorno }));
    }

    [Fact]
    public async Task Giro_completo_Mappe_entra_con_l_accesso_unico_un_altra_app_senza_password_e_Esci_chiude_tutto()
    {
        var versoWeb = new Rinvio();
        var versoApi = new Rinvio();
        await using var api = Api(versoWeb);
        await using var web = Web(versoApi);
        var opzioni = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri(Sito) };
        var clientApi = api.CreateClient(opzioni);
        var clientWeb = web.CreateClient(opzioni);
        versoApi.Verso = api.Server.CreateHandler();
        versoWeb.Verso = web.Server.CreateHandler();
        var browser = new Browser(clientApi, clientWeb);

        // 1. Una pagina di Mappe senza sessione → /accesso (rimando) → accesso unico → pagina di accesso Perleuve.
        var pagina = await browser.VaiAsync("/portale/vigneti/excel");
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        Assert.Contains(browser.Visitati, v => v.StartsWith("/accesso/entra"));
        var authorize = browser.Visitati.Single(v => v.StartsWith("/connect/authorize"));
        Assert.Contains("client_id=mappe", authorize);
        Assert.Contains("code_challenge_method=S256", authorize);
        Assert.StartsWith("/perleuve/accesso?ritorno=", browser.Visitati[^1]);
        var html = await pagina.Content.ReadAsStringAsync();
        Assert.Contains("Mappe", html); // «Stai entrando in: Mappe»

        // 2. Password una volta sola → codice → Mappe scambia il codice (dall'Api, con PKCE e segreto) → cookie di Mappe.
        var login = await browser.EntraAsync(Uri.UnescapeDataString(browser.Visitati[^1]["/perleuve/accesso?ritorno=".Length..]));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        browser.Visitati.Clear();
        var dentro = await browser.VaiAsync(login.Headers.Location!.OriginalString, passi: 3);
        Assert.True(browser.Visitati.Any(v => v.StartsWith("/accesso/ritorno?code=")),
            $"{(int)dentro.StatusCode} {dentro.Headers.Location} {await dentro.Content.ReadAsStringAsync()}");
        Assert.Equal("/portale/vigneti/excel", browser.Visitati[^1]); // tornato alla pagina chiesta, ora da utente entrato
        Assert.NotEqual(HttpStatusCode.Redirect, dentro.StatusCode);
        Assert.Contains(versoApi.Percorsi, p => p == "/connect/token");
        Assert.NotEqual("/accesso", (await clientWeb.GetAsync("/portale/vigneti/excel")).Headers.Location?.AbsolutePath);

        // 3. Un'altra app (4Sprayers, client «browser» come le app di Jan): entra senza pagina di accesso.
        browser.Visitati.Clear();
        var sfida = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes("verificatore-di-prova-abbastanza-lungo-1234567890")));
        var altra = await browser.VaiAsync("/connect/authorize?client_id=4sprayers&response_type=code&scope=openid&state=s"
            + "&redirect_uri=" + Uri.EscapeDataString(RitornoApp) + "&code_challenge=" + sfida + "&code_challenge_method=S256");
        Assert.StartsWith(RitornoApp + "?code=", altra.Headers.Location!.AbsoluteUri);
        Assert.DoesNotContain(browser.Visitati, v => v.StartsWith("/perleuve/accesso"));

        // 4. «I tuoi servizi»: Mappe e le app abilitate della sua categoria (consulenza: Cantina sì, Turni solo interni no).
        var servizi = await (await clientApi.GetAsync("/perleuve/servizi")).Content.ReadAsStringAsync();
        Assert.Contains("I tuoi servizi", servizi);
        Assert.Contains("4Sprayers", servizi);
        Assert.True(servizi.Contains("Cantina"), servizi[servizi.IndexOf("<main", StringComparison.Ordinal)..]);
        Assert.DoesNotContain("Turni", servizi);

        // 5. Esci da Mappe = uscita unica: Mappe → /connect/logout (id_token_hint) → pagina «Sei uscito» con il riquadro
        //    del canale browser; il canale server ha avvisato Mappe, che non accetta più il suo cookie.
        var antiforgery = await EsciAsync(clientWeb);
        Assert.StartsWith(Sito + "/connect/logout", antiforgery.Headers.Location!.AbsoluteUri);
        Assert.Contains("id_token_hint=", antiforgery.Headers.Location.Query);
        var uscita = await clientApi.GetAsync(antiforgery.Headers.Location.PathAndQuery);
        var testoUscita = await uscita.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, uscita.StatusCode);
        Assert.Contains("Sei uscito da tutte le app", testoUscita);
        Assert.Contains("/accesso/uscita-frontale?iss=", testoUscita);
        Assert.Contains("/accesso/uscito", testoUscita); // poi si torna a Mappe, che rimanda all'accesso
        Assert.Contains(versoWeb.Percorsi, p => p == "/accesso/uscita-retro");
        Assert.Equal("/accesso", (await clientWeb.GetAsync("/portale/vigneti/excel")).Headers.Location?.AbsolutePath);

        // 6. Dopo l'uscita la sessione dell'accesso unico non c'è più: un'app torna alla pagina di accesso.
        browser.Visitati.Clear();
        await browser.VaiAsync("/connect/authorize?client_id=4sprayers&response_type=code&scope=openid&state=s"
            + "&redirect_uri=" + Uri.EscapeDataString(RitornoApp) + "&code_challenge=" + sfida + "&code_challenge_method=S256");
        Assert.StartsWith("/perleuve/accesso", browser.Visitati[^1]);

        // 7. Registro degli accessi.
        var registro = await api.Services.GetRequiredService<RegistroAccessi>().UltimiAsync();
        var eventi = registro.Select(r => r.Evento + ":" + r.App).ToList();
        Assert.Contains("accesso:", eventi);
        Assert.Contains("app:mappe", eventi);
        Assert.Contains("app:4sprayers", eventi);
        Assert.Contains(registro, r => r.Evento == "uscita" && r.IdUtente == 4242 && r.Dettaglio!.Contains("1"));
        Assert.DoesNotContain(registro, r => r.Dettaglio?.Contains(Password) == true);
    }

    /// <summary>«Esci» di Mappe: il modulo con il token antiforgery della pagina (qui: preso da una pagina del Web).</summary>
    private static async Task<HttpResponseMessage> EsciAsync(HttpClient web)
    {
        // Il token antiforgery lo dà qualsiasi pagina con un modulo; senza, Mappe non fa uscire (risponde col rimando a «/»).
        const string modello = "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"";
        var token = System.Text.RegularExpressions.Regex.Match(await (await web.GetAsync("/guida")).Content.ReadAsStringAsync(), modello);
        if (!token.Success)
            token = System.Text.RegularExpressions.Regex.Match(await (await web.GetAsync("/avvisi")).Content.ReadAsStringAsync(), modello);
        Assert.True(token.Success, "token antiforgery non trovato");
        return await web.PostAsync("/accesso/esci", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
        }));
    }

    [Fact]
    public async Task In_http_diretto_sulla_macchina_la_pagina_risponde_200_come_prima_per_i_controlli()
    {
        await using var web = Web(new Rinvio());
        var client = web.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://127.0.0.1") });
        var risposta = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Equal("/accesso", risposta.RequestMessage!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public void Sessione_dell_accesso_unico_massimo_10_ore_e_token_brevi()
    {
        var opzioni = new AccessoUnicoOptions();
        var inizio = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);
        ClaimsPrincipal Principal(bool conSid = true) => new(new ClaimsIdentity(
            conSid ? [new Claim("sid", "abc"), SessioneAccesso.ClaimDiInizio(inizio)] : [SessioneAccesso.ClaimDiInizio(inizio)], "prova"));

        Assert.True(SessioneAccesso.Valida(Principal(), inizio.AddHours(9).AddMinutes(59), opzioni));
        Assert.False(SessioneAccesso.Valida(Principal(), inizio.AddHours(10), opzioni));
        Assert.False(SessioneAccesso.Valida(Principal(conSid: false), inizio.AddMinutes(1), opzioni));
        Assert.False(SessioneAccesso.Valida(new ClaimsPrincipal(new ClaimsIdentity()), inizio, opzioni));

        Assert.Equal(TimeSpan.FromMinutes(60), opzioni.Inattivita());
        Assert.Equal(TimeSpan.FromHours(10), opzioni.SessioneMassima());
        Assert.Equal(TimeSpan.FromMinutes(60), new AccessoUnicoOptions { DurataMinuti = 480 }.DurataOidc());
        Assert.Equal(TimeSpan.FromMinutes(30), new AccessoUnicoOptions { DurataMinuti = 30 }.DurataOidc());
        Assert.Equal(TimeSpan.FromMinutes(5), AccessoUnicoOptions.DurataCodice);
        Assert.Equal(TimeSpan.FromHours(10), new AccessoUnicoOptions { SessioneOreMassime = 99 }.SessioneMassima());
    }

    [Fact]
    public void Secondo_fattore_predisposto_ma_spento()
    {
        Assert.False(new SecondoFattoreOptions().Richiesto("interno"));
        Assert.True(new SecondoFattoreOptions { Attivo = true }.Richiesto("interno"));
        Assert.False(new SecondoFattoreOptions { Attivo = true }.Richiesto("consulenza"));
    }

    [Fact]
    public void Indirizzi_d_uscita_relativi_valgono_sul_sito_dell_accesso()
    {
        Assert.Equal("https://prova-mappe.esempio/accesso/uscita-retro",
            UscitaUnica.Indirizzo("/accesso/uscita-retro", "https://prova-mappe.esempio/")!.AbsoluteUri);
        Assert.Equal("https://app.esempio/esci", UscitaUnica.Indirizzo("https://app.esempio/esci", "https://accesso.esempio/")!.AbsoluteUri);
        Assert.Null(UscitaUnica.Indirizzo("//altro.esempio/x", "https://accesso.esempio/"));
        Assert.Null(UscitaUnica.Indirizzo("javascript:alert(1)", "https://accesso.esempio/"));
        Assert.Null(UscitaUnica.Indirizzo("", "https://accesso.esempio/"));
    }

    [Fact]
    public async Task Un_logout_token_falso_non_chiude_niente()
    {
        using var rsa = RSA.Create(2048);
        var configurazione = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = Sito + "/" };
        configurazione.SigningKeys.Add(new RsaSecurityKey(rsa.ExportParameters(false)));
        using var altra = RSA.Create(2048);
        var chiaviGiuste = new ChiaviFirma(rsa);
        var chiaviFalse = new ChiaviFirma(altra);
        var opzioni = Microsoft.Extensions.Options.Options.Create(new AccessoUnicoOptions());

        string Token(ChiaviFirma chiavi, string aud) => new UscitaUnica(null!, chiavi, opzioni,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UscitaUnica>.Instance).LogoutToken(Sito + "/", aud, "4242", "sessione-1");

        Assert.Equal("sessione-1", await AccessoUnicoClient.SidDaLogoutTokenAsync(Token(chiaviGiuste, "mappe"), configurazione, "mappe"));
        Assert.Null(await AccessoUnicoClient.SidDaLogoutTokenAsync(Token(chiaviFalse, "mappe"), configurazione, "mappe"));
        Assert.Null(await AccessoUnicoClient.SidDaLogoutTokenAsync(Token(chiaviGiuste, "4sprayers"), configurazione, "mappe"));
        Assert.Null(await AccessoUnicoClient.SidDaLogoutTokenAsync("non-un-token", configurazione, "mappe"));
        Assert.Null(await AccessoUnicoClient.SidDaLogoutTokenAsync(null, configurazione, "mappe"));
    }
}
