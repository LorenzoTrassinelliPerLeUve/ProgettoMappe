using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Accesso unico per le app esterne (task #234): l'Api vera con un utente finto al posto di 4Grapes.</summary>
public class AccessoUnicoTests
{
    private const string Email = "mario.rossi@esempio.it";
    private const string Password = "vendemmia-2026";
    private const string SegretoApp = "segreto-di-4sprayers";
    private const int IdUtente = 4242;

    private static string Impronta(string segreto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(segreto)));

    private sealed class Finto : IUtentiRepository, IPermessiRepository, IAccessoUnicoRepository
    {
        public bool AppAbilitata { get; set; } = true;
        public HashSet<string> Abilitazioni { get; } = ["RILIEVI", "IRRORATRICI"];

        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CredenzialiUtente>>(
                string.Equals(loginName.Trim(), Email, StringComparison.OrdinalIgnoreCase) && AppAbilitata
                    ? [new CredenzialiUtente(IdUtente, "Mario", "Rossi", Impronta(Password))]
                    : []);

        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(idUtente == IdUtente
                ? new PermessiUtente(IdUtente, AppAbilitata, false, true, false, true, null, Abilitazioni)
                : null);

        public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<AnagraficaUtente?>(idUtente == IdUtente ? new AnagraficaUtente(IdUtente, Email, "Mario", "Rossi", AppAbilitata) : null);

        public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AziendaUtente>>([new AziendaUtente(425, "Cantina Uno", true), new AziendaUtente(46, "Vigne Due", false)]);
    }

    private static (WebApplicationFactory<AccessoUnicoOptions> Api, Finto Finto) Avvia(Action<IWebHostBuilder>? altro = null)
    {
        var finto = new Finto();
        var api = new WebApplicationFactory<AccessoUnicoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ChiaveApi:Valore", "chiave-del-web");
            b.UseSetting("AccessoUnico:ChiavePrivata", "");
            b.UseSetting("AccessoUnico:PasswordDimenticata", "https://portale.esempio/password");
            b.UseSetting("AccessoUnico:App:4sprayers:SegretoSha256", Impronta(SegretoApp));
            b.UseSetting("AccessoUnico:App:4sprayers:Abilitazione", "irroratrici");
            b.UseSetting("AccessoUnico:App:spenta:SegretoSha256", Impronta(SegretoApp));
            b.UseSetting("AccessoUnico:App:spenta:Abilitata", "false");
            b.UseSetting("AccessoUnico:App:gestionale:SegretoSha256", Impronta(SegretoApp));
            b.UseSetting("AccessoUnico:App:gestionale:Abilitazione", "GESTIONALE");
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IUtentiRepository>(finto);
                s.AddSingleton<IPermessiRepository>(finto);
                s.AddSingleton<IAccessoUnicoRepository>(finto);
            });
            altro?.Invoke(b);
        });
        return (api, finto);
    }

    private static HttpRequestMessage Richiesta(HttpMethod metodo, string url, string app = "4sprayers", string segreto = SegretoApp,
        object? corpo = null, string? token = null)
    {
        var r = new HttpRequestMessage(metodo, url);
        r.Headers.Add(AppChiamante.IntestazioneNome, app);
        r.Headers.Add(AppChiamante.IntestazioneSegreto, segreto);
        if (corpo is not null) r.Content = JsonContent.Create(corpo);
        if (token is not null) r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return r;
    }

    private static HttpRequestMessage Login(string password = Password, string app = "4sprayers", string segreto = SegretoApp) =>
        Richiesta(HttpMethod.Post, "/api/accesso/token", app, segreto, new RichiestaTokenDto(" MARIO.rossi@esempio.it ", password));

    [Fact]
    public async Task Password_giusta_da_un_token_firmato_con_ruoli_abilitazioni_e_aziende()
    {
        var (api, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();

        var risposta = await client.SendAsync(Login());
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var dati = (await risposta.Content.ReadFromJsonAsync<TokenAccessoDto>())!;
        Assert.Equal("Bearer", dati.TokenType);
        Assert.Equal(3600, dati.ExpiresIn);
        Assert.Equal(Email, dati.Utente.Email);
        Assert.Equal("Mario Rossi", dati.Utente.Nome);
        Assert.Equal(["MasterUser", "UtentePerleuve"], dati.Utente.Ruoli);
        Assert.Equal(["IRRORATRICI", "RILIEVI"], dati.Utente.Abilitazioni);

        // La firma si verifica solo con la chiave pubblica di api/accesso/chiavi, come farà un'app.
        var jwks = await client.GetStringAsync("/api/accesso/chiavi");
        var chiave = new JsonWebKeySet(jwks).Keys.Single();
        Assert.False(chiave.HasPrivateKey);
        var esito = await new JsonWebTokenHandler().ValidateTokenAsync(dati.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = "mappe",
            ValidAudience = "4sprayers",
            IssuerSigningKey = chiave,
        });
        Assert.True(esito.IsValid, esito.Exception?.Message);
        Assert.Equal("4242", esito.Claims["sub"]);
        Assert.Equal(Email, esito.Claims["email"]);

        // Il contenuto del token nel JSON grezzo: aziende con gestione.
        var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(dati.AccessToken.Split('.')[1])).RootElement;
        Assert.Equal("RS256", JsonDocument.Parse(Base64UrlEncoder.Decode(dati.AccessToken.Split('.')[0])).RootElement.GetProperty("alg").GetString());
        var aziende = payload.GetProperty("aziende").EnumerateArray().ToList();
        Assert.Equal(425, aziende[0].GetProperty("id").GetInt32());
        Assert.True(aziende[0].GetProperty("gestione").GetBoolean());
        Assert.Equal("Vigne Due", aziende[1].GetProperty("nome").GetString());
        Assert.Equal(["MasterUser", "UtentePerleuve"], payload.GetProperty("ruoli").EnumerateArray().Select(r => r.GetString()));
        Assert.False(payload.GetProperty("permessi_ridotti").GetBoolean());
    }

    [Fact]
    public async Task Password_sbagliata_401_con_il_rimando_al_portale_e_dopo_cinque_errori_429()
    {
        var (api, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();

        var risposta = await client.SendAsync(Login("sbagliata"));
        Assert.Equal(HttpStatusCode.Unauthorized, risposta.StatusCode);
        var problema = JsonDocument.Parse(await risposta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("https://portale.esempio/password", problema.GetProperty("passwordDimenticata").GetString());

        for (var i = 0; i < 4; i++) await client.SendAsync(Login("sbagliata"));
        // Bloccato anche con la password giusta, finché non passano 15 minuti.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(Login())).StatusCode);
    }

    [Fact]
    public async Task Utente_senza_app_abilitata_in_4grapes_non_entra()
    {
        var (api, finto) = Avvia();
        await using var _a = api;
        finto.AppAbilitata = false;
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().SendAsync(Login())).StatusCode);
    }

    [Fact]
    public async Task App_disabilitata_sconosciuta_o_con_segreto_sbagliato_403()
    {
        var (api, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Login(app: "spenta"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Login(app: "altra"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Login(segreto: "indovinato"))).StatusCode);
        var senza = new HttpRequestMessage(HttpMethod.Post, "/api/accesso/token") { Content = JsonContent.Create(new RichiestaTokenDto(Email, Password)) };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(senza)).StatusCode);
    }

    [Fact]
    public async Task Utente_senza_l_abilitazione_dell_app_403()
    {
        var (api, _) = Avvia();
        await using var _a = api;
        var risposta = await api.CreateClient().SendAsync(Login(app: "gestionale"));
        Assert.Equal(HttpStatusCode.Forbidden, risposta.StatusCode);
        Assert.Contains("non è abilitata a gestionale", await risposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_rilegge_il_profilo_e_rifiuta_token_di_altre_app_o_alterati()
    {
        var (api, finto) = Avvia(b => b.UseSetting("AccessoUnico:App:altra:SegretoSha256", Impronta("altro")));
        await using var _a = api;
        var client = api.CreateClient();
        var token = (await (await client.SendAsync(Login())).Content.ReadFromJsonAsync<TokenAccessoDto>())!.AccessToken;

        finto.Abilitazioni.Add("CONCIMAZIONI");
        var me = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/accesso/me", token: token));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains("CONCIMAZIONI", (await me.Content.ReadFromJsonAsync<ProfiloAccessoDto>())!.Abilitazioni);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/accesso/me", "altra", "altro", token: token))).StatusCode);
        var alterato = token[..^4] + (token[^4] == 'A' ? "BBBB" : "AAAA");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/accesso/me", token: alterato))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/accesso/me"))).StatusCode);

        // Tolta l'app in 4Grapes, il token ancora valido non basta più.
        finto.AppAbilitata = false;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/accesso/me", token: token))).StatusCode);
    }

    [Fact]
    public async Task Le_altre_rotte_dell_api_vogliono_ancora_la_chiave_del_web()
    {
        var (api, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/accesso/verifica", corpo: new RichiestaAccessoDto(Email, Password)))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/aziende"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/accesso/chiavi")).StatusCode);
    }

    [Fact]
    public async Task Oltre_il_limite_al_minuto_dell_app_429()
    {
        var (api, _) = Avvia(b => b.UseSetting("AccessoUnico:App:4sprayers:LimiteAlMinuto", "3"));
        await using var _a = api;
        var client = api.CreateClient();
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Login())).StatusCode);
        var oltre = await client.SendAsync(Login());
        Assert.Equal(HttpStatusCode.TooManyRequests, oltre.StatusCode);
        Assert.Equal("60", oltre.Headers.GetValues("Retry-After").Single());
    }

    private sealed class Orologio : TimeProvider
    {
        public DateTimeOffset Adesso { get; set; } = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Adesso;
    }

    [Fact]
    public async Task Token_scaduto_non_vale_e_la_durata_viene_dalla_configurazione()
    {
        var orologio = new Orologio();
        var opzioni = Options.Create(new AccessoUnicoOptions { DurataMinuti = 30 });
        var token = new TokenAccesso(new ChiaviFirma(RSA.Create(2048)), opzioni, orologio);
        var profilo = new ProfiloAccessoDto(7, "a@b.it", "A B", [], [], [], false);
        var jwt = token.Crea(profilo, "4sprayers");

        Assert.Equal(7, await token.VerificaAsync(jwt, "4sprayers"));
        orologio.Adesso += TimeSpan.FromMinutes(29);
        Assert.Equal(7, await token.VerificaAsync(jwt, "4sprayers"));
        orologio.Adesso += TimeSpan.FromMinutes(2);
        Assert.Null(await token.VerificaAsync(jwt, "4sprayers"));

        // Un token firmato da un'altra chiave (es. dopo un cambio di chiave) non vale.
        var altro = new TokenAccesso(new ChiaviFirma(RSA.Create(2048)), opzioni, new Orologio());
        Assert.Null(await altro.VerificaAsync(token.Crea(profilo, "4sprayers"), "4sprayers"));
    }

    [Fact]
    public void La_chiave_privata_si_crea_una_volta_e_poi_si_rilegge()
    {
        var cartella = Path.Combine(Path.GetTempPath(), "mappe-chiave-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(cartella);
            var percorso = Path.Combine(cartella, "accesso-unico.pem");
            Assert.True(File.Exists(Path.Combine(cartella, "accesso-unico.cifratura.pem")) is false);
            var opzioni = Options.Create(new AccessoUnicoOptions { ChiavePrivata = percorso });
            var prima = new ChiaviFirma(opzioni, NullLogger<ChiaviFirma>.Instance);
            Assert.True(File.Exists(percorso));
            Assert.True(File.Exists(Path.Combine(cartella, "accesso-unico.cifratura.pem")));
            if (!OperatingSystem.IsWindows())
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(percorso));
            Assert.Equal(prima.Kid, new ChiaviFirma(opzioni, NullLogger<ChiaviFirma>.Instance).Kid);
        }
        finally
        {
            if (Directory.Exists(cartella)) Directory.Delete(cartella, true);
        }
    }

    [Theory]
    [InlineData(60, 60)]
    [InlineData(5, 5)]
    [InlineData(1440, 1440)]
    [InlineData(0, 60)]
    [InlineData(4, 60)]
    [InlineData(100000, 60)]
    public void Durata_fuori_scala_usa_un_ora(int minuti, int attesi) =>
        Assert.Equal(TimeSpan.FromMinutes(attesi), new AccessoUnicoOptions { DurataMinuti = minuti }.Durata());
}
