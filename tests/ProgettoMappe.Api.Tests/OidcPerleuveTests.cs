using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Fornitore di identità Perleuve (OpenID Connect, task #234 giro 2): il giro completo come lo fa un'app di Jan.</summary>
public sealed class OidcPerleuveTests : IDisposable
{
    private const string Email = "mario.rossi@esempio.it";
    private const string Password = "vendemmia-2026";
    private const string Ritorno = "https://4sprayers.esempio/callback";

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "mappe-oidc-" + Guid.NewGuid().ToString("N"));

    public OidcPerleuveTests() => Directory.CreateDirectory(_cartella);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_cartella, true);
    }

    private sealed class Finto : IUtentiRepository, IPermessiRepository, IAccessoUnicoRepository
    {
        public HashSet<string> Abilitazioni { get; } = ["RILIEVI", "4SPRAYERS"];

        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CredenzialiUtente>>(string.Equals(loginName.Trim(), Email, StringComparison.OrdinalIgnoreCase)
                ? [new CredenzialiUtente(4242, "Mario", "Rossi", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Password))))]
                : []);

        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(new PermessiUtente(idUtente, true, false, true, false, false, null, Abilitazioni));

        public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<AnagraficaUtente?>(new AnagraficaUtente(idUtente, Email, "Mario", "Rossi", true));

        public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AziendaUtente>>([new AziendaUtente(425, "Cantina Uno", true, "Titolare")]);
    }

    private WebApplicationFactory<AccessoUnicoOptions> Avvia() =>
        new WebApplicationFactory<AccessoUnicoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("AccessoUnico:Database", Path.Combine(_cartella, "accesso.db"));
            b.UseSetting("AccessoUnico:FileSegreti", Path.Combine(_cartella, "segreti.json"));
            b.UseSetting("AccessoUnico:ChiavePrivata", Path.Combine(_cartella, "chiave.pem"));
            b.UseSetting("AccessoUnico:App:4sprayers:Nome", "4Sprayers");
            b.UseSetting("AccessoUnico:App:4sprayers:Indirizzo", "https://4sprayers.esempio/");
            b.UseSetting("AccessoUnico:App:4sprayers:Abilitazione", "4SPRAYERS");
            b.UseSetting("AccessoUnico:App:4sprayers:RedirectUri:0", Ritorno);
            b.UseSetting("AccessoUnico:App:gestionale:Abilitazione", "GESTIONALE");
            b.UseSetting("AccessoUnico:App:gestionale:Indirizzo", "https://gestionale.esempio/");
            b.UseSetting("AccessoUnico:App:gestionale:Tipo", "browser");
            b.UseSetting("AccessoUnico:App:gestionale:RedirectUri:0", "https://gestionale.esempio/callback");
            var finto = new Finto();
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IUtentiRepository>(finto);
                s.AddSingleton<IPermessiRepository>(finto);
                s.AddSingleton<IAccessoUnicoRepository>(finto);
            });
        });

    private static (string Verificatore, string Sfida) Pkce()
    {
        var verificatore = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (verificatore, Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verificatore))));
    }

    private static string Autorizza(string clientId, string redirect, string sfida) =>
        "/connect/authorize?" + string.Join("&", new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["redirect_uri"] = redirect, ["response_type"] = "code",
            ["scope"] = "openid profile email perleuve offline_access", ["state"] = "stato-1",
            ["code_challenge"] = sfida, ["code_challenge_method"] = "S256",
        }.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

    /// <summary>Authorize → pagina di accesso → di nuovo authorize → rimando all'app; restituisce dove rimanda.</summary>
    private static async Task<Uri> EntraAsync(HttpClient client, string autorizza)
    {
        var primo = await client.GetAsync(autorizza);
        Assert.Equal(HttpStatusCode.Redirect, primo.StatusCode);
        var accesso = primo.Headers.Location!.OriginalString;
        Assert.StartsWith("/perleuve/accesso?ritorno=", accesso);
        Assert.Contains("Password dimenticata", await client.GetStringAsync(accesso));

        var ritorno = Uri.UnescapeDataString(accesso["/perleuve/accesso?ritorno=".Length..]);
        var login = await client.PostAsync("/perleuve/accesso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = Email, ["password"] = Password, ["ritorno"] = ritorno,
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var dopo = await client.GetAsync(login.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, dopo.StatusCode);
        return dopo.Headers.Location!;
    }

    [Fact]
    public async Task Giro_completo_codice_pkce_token_userinfo_e_rinnovo()
    {
        await using var api = Avvia();
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (verificatore, sfida) = Pkce();

        var rimando = await EntraAsync(client, Autorizza("4sprayers", Ritorno, sfida));
        Assert.StartsWith(Ritorno, rimando.ToString());
        var query = System.Web.HttpUtility.ParseQueryString(rimando.Query);
        Assert.Equal("stato-1", query["state"]);

        var segreto = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(_cartella, "segreti.json")))!["4sprayers"];
        var token = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["code"] = query["code"]!, ["redirect_uri"] = Ritorno,
            ["client_id"] = "4sprayers", ["client_secret"] = segreto, ["code_verifier"] = verificatore,
        }));
        var testo = await token.Content.ReadAsStringAsync();
        Assert.True(token.IsSuccessStatusCode, testo);
        var risposta = JsonDocument.Parse(testo).RootElement;
        var accessToken = risposta.GetProperty("access_token").GetString()!;
        Assert.True(risposta.TryGetProperty("id_token", out _));

        // La firma si verifica con la chiave pubblica della discovery, come fa una libreria standard.
        var discovery = JsonDocument.Parse(await client.GetStringAsync("/.well-known/openid-configuration")).RootElement;
        var jwks = await client.GetStringAsync(new Uri(discovery.GetProperty("jwks_uri").GetString()!).PathAndQuery);
        var esito = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = discovery.GetProperty("issuer").GetString(),
            ValidAudience = "4sprayers",
            IssuerSigningKeys = new JsonWebKeySet(jwks).Keys,
        });
        Assert.True(esito.IsValid, esito.Exception?.Message);
        var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(accessToken.Split('.')[1])).RootElement;
        Assert.Equal("4242", payload.GetProperty("sub").GetString());
        Assert.Contains("4SPRAYERS", payload.GetProperty("abilitazioni").EnumerateArray().Select(a => a.GetString()));
        Assert.Equal("Titolare", payload.GetProperty("aziende")[0].GetProperty("ruolo").GetString());
        Assert.True(payload.GetProperty("tutte_le_aziende").GetBoolean());

        // Stessa chiave del token semplice.
        var chiaviSemplici = new JsonWebKeySet(await client.GetStringAsync("/api/accesso/chiavi")).Keys.Single();
        Assert.Contains(new JsonWebKeySet(jwks).Keys, k => k.Kid == chiaviSemplici.Kid);

        var info = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        info.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var utente = JsonDocument.Parse(await (await client.SendAsync(info)).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(Email, utente.GetProperty("email").GetString());
        Assert.Equal(425, utente.GetProperty("aziende")[0].GetProperty("id").GetInt32());

        var rinnovo = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["refresh_token"] = risposta.GetProperty("refresh_token").GetString()!,
            ["client_id"] = "4sprayers", ["client_secret"] = segreto,
        }));
        Assert.True(rinnovo.IsSuccessStatusCode, await rinnovo.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Senza_abilitazione_l_app_riceve_access_denied_e_l_ingresso_non_la_mostra()
    {
        await using var api = Avvia();
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (_, sfida) = Pkce();

        var rimando = await EntraAsync(client, Autorizza("gestionale", "https://gestionale.esempio/callback", sfida));
        Assert.StartsWith("https://gestionale.esempio/callback", rimando.ToString());
        Assert.Equal("access_denied", System.Web.HttpUtility.ParseQueryString(rimando.Query)["error"]);

        var ingresso = await client.GetStringAsync("/perleuve/ingresso");
        Assert.Contains("https://4sprayers.esempio/", ingresso);
        Assert.DoesNotContain("gestionale.esempio", ingresso);
        Assert.Contains("Mario Rossi", ingresso);
    }

    [Fact]
    public async Task Password_sbagliata_resta_sulla_pagina_e_i_ritorni_esterni_non_valgono()
    {
        await using var api = Avvia();
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var risposta = await client.PostAsync("/perleuve/accesso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = Email, ["password"] = "sbagliata", ["ritorno"] = "/perleuve/ingresso",
        }));
        Assert.Equal(HttpStatusCode.Unauthorized, risposta.StatusCode);
        Assert.Contains("password sbagliate", await risposta.Content.ReadAsStringAsync());

        Assert.Equal("/perleuve/ingresso", OidcController.RitornoSicuro("https://altro.esempio/"));
        Assert.Equal("/perleuve/ingresso", OidcController.RitornoSicuro("//altro.esempio/"));
        Assert.Equal("/perleuve/ingresso", OidcController.RitornoSicuro("/\\altro.esempio/"));
        Assert.Equal("/connect/authorize?x=1", OidcController.RitornoSicuro("/connect/authorize?x=1"));
    }

    [Fact]
    public async Task Un_redirect_non_registrato_non_riceve_codici()
    {
        await using var api = Avvia();
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (_, sfida) = Pkce();
        var risposta = await client.GetAsync(Autorizza("4sprayers", "https://cattivo.esempio/callback", sfida));
        Assert.Equal(HttpStatusCode.BadRequest, risposta.StatusCode);
    }

    [Fact]
    public void I_segreti_delle_app_si_creano_una_volta_con_permessi_600()
    {
        var file = Path.Combine(_cartella, "segreti-2.json");
        var opzioni = Microsoft.Extensions.Options.Options.Create(new AccessoUnicoOptions { FileSegreti = file });
        opzioni.Value.App["suite"] = new AppEsternaOptions();
        var primo = new SegretiApp(opzioni, Microsoft.Extensions.Logging.Abstractions.NullLogger<SegretiApp>.Instance).Di("suite");
        Assert.Equal(64, primo!.Length);
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(file));
        Assert.Equal(primo, new SegretiApp(opzioni, Microsoft.Extensions.Logging.Abstractions.NullLogger<SegretiApp>.Instance).Di("suite"));
    }
}
