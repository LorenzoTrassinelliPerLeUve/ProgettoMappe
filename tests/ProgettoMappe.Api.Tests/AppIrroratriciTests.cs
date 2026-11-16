using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Irroratrici dell'azienda per le app di Jan (task #259): l'Api vera, utente e 4Grapes finti. L'utente è il consulente di
/// <see cref="LettureDifesaFinte"/>: abbinato a 425 (aggiornamenti abilitati) e 426 (sola lettura), non a 500.
/// </summary>
public class AppIrroratriciTests
{
    internal const string Email = "agronomo@esempio.it";
    internal const string Password = "vendemmia-2026";
    internal const string Segreto = "segreto-della-suite";
    internal const int IdUtente = PermessiFinti.Consulente;

    internal static string Impronta(string testo) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(testo)));

    /// <summary>La persona finta (anche per <see cref="AppRilieviTests"/>): abbinata a 425 e 426, con le abilitazioni date.</summary>
    internal sealed class Persona : IUtentiRepository, IPermessiRepository, IAccessoUnicoRepository
    {
        public bool Perleuve { get; set; }
        public HashSet<string> Abilitazioni { get; } = ["SUITE"];
        public List<AbilitazionePersonale> Personali { get; } = [];

        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CredenzialiUtente>>(string.Equals(loginName.Trim(), Email, StringComparison.OrdinalIgnoreCase)
                ? [new CredenzialiUtente(IdUtente, "Anna", "Verdi", Impronta(Password))]
                : []);

        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(idUtente == IdUtente
                ? new PermessiUtente(IdUtente, true, false, false, false, Perleuve, null, Abilitazioni)
                : null);

        public Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<AnagraficaUtente?>(idUtente == IdUtente ? new AnagraficaUtente(IdUtente, Email, "Anna", "Verdi", true) : null);

        public Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AziendaUtente>>([new AziendaUtente(425, "Azienda A", false), new AziendaUtente(426, "Azienda B", false)]);

        public Task<IReadOnlyList<AbilitazionePersonale>> GetAbilitazioniPersonaliAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AbilitazionePersonale>>(Personali);

        public Task<AbilitazionePersonale?> GetAbilitazionePersonaleAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Personali.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlySet<string>> GetCodiciAbilitazioneAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string> { "SUITE", "SUITE_TRATTORISTA" });

        public Task<bool> EsisteLoginAsync(string loginName, CancellationToken ct = default) => Task.FromResult(false);

        public Task<bool> AbbinatoAsync(int idUtente, int idEnte, CancellationToken ct = default) => Task.FromResult(idEnte is 425 or 426);
    }

    private static (WebApplicationFactory<AccessoUnicoOptions> Api, Persona Persona, DbFinto Db) Avvia()
    {
        var persona = new Persona();
        var db = new DbFinto();
        db.Metti("Irroratrice", 1, new() { ["IdIrroratrice"] = 1, ["Descrizione"] = "Atomizzatore", ["IdTipoIrroratrice"] = 1 });
        var letture = new LettureDifesaFinte(db);
        var api = new WebApplicationFactory<AccessoUnicoOptions>().WithWebHostBuilder(b =>
        {
            // La chiave del Web c'è: le rotte /api/app/ non la vogliono.
            b.UseSetting("ChiaveApi:Valore", "chiave-del-web");
            b.UseSetting("AccessoUnico:ChiavePrivata", "");
            b.UseSetting("AccessoUnico:App:suite:SegretoSha256", Impronta(Segreto));
            b.UseSetting("AccessoUnico:App:suite:Abilitazione", "SUITE");
            b.UseSetting("AccessoUnico:App:altra:SegretoSha256", Impronta(Segreto));
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IUtentiRepository>(persona);
                s.AddSingleton<IPermessiRepository>(persona);
                s.AddSingleton<IAccessoUnicoRepository>(persona);
                s.AddSingleton<ILettureDifesaPortale>(letture);
                s.AddSingleton<ILetturePortale>(letture);
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
            });
        });
        return (api, persona, db);
    }

    private static HttpRequestMessage Richiesta(HttpMethod metodo, string url, string? token, object? corpo = null, string? app = "suite")
    {
        var r = new HttpRequestMessage(metodo, url);
        if (app is not null)
        {
            r.Headers.Add(AppChiamante.IntestazioneNome, app);
            r.Headers.Add(AppChiamante.IntestazioneSegreto, Segreto);
        }

        if (token is not null) r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null) r.Content = JsonContent.Create(corpo);
        return r;
    }

    private static async Task<(HttpClient Client, string Token)> EntraAsync(WebApplicationFactory<AccessoUnicoOptions> api, string app = "suite")
    {
        var client = api.CreateClient();
        var risposta = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/accesso/token", null, new RichiestaTokenDto(Email, Password), app));
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        return (client, (await risposta.Content.ReadFromJsonAsync<TokenAccessoDto>())!.AccessToken);
    }

    private static IrroratriceAppSalvaDto Regolazione(int irroratrice = 1, decimal volume = 300m, int? ugelli = 12, decimal? portata = 1.2m) =>
        new(irroratrice, 65, volume, 6.5m, "Antideriva ad induzione d'aria", ugelli, portata, 8m);

    private static async Task<string?> TitoloAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ProblemDetails>())?.Title;

    [Fact]
    public async Task Inserimento_modifica_e_cancellazione_con_LogOp_a_nome_dell_agronomo()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);

        var nuova = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici", token, Regolazione()));
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var esito = (await nuova.Content.ReadFromJsonAsync<EsitoAccessoDto>())!;
        Assert.Equal(1, esito.Id);
        var riga = db.Tabella("Irroratrice_Ente")[1];
        Assert.Equal(425, riga["IdEnte"]);
        Assert.Equal(12, riga["NumeroUgelli"]);
        Assert.Equal(8m, riga["Pressionebar"]);

        // In modifica i facoltativi assenti restano com'erano.
        var modifica = await client.SendAsync(Richiesta(HttpMethod.Put, "/api/app/aziende/425/irroratrici/1", token,
            Regolazione(volume: 450m, ugelli: null, portata: null)));
        Assert.Equal(HttpStatusCode.OK, modifica.StatusCode);
        riga = db.Tabella("Irroratrice_Ente")[1];
        Assert.Equal(450m, riga["VolumeAcqua"]);
        Assert.Equal(12, riga["NumeroUgelli"]);
        Assert.Equal(1.2m, riga["PortataLminuto"]);

        var lista = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", token));
        var righe = (await lista.Content.ReadFromJsonAsync<List<IrroratriceAziendaDto>>())!;
        var letta = Assert.Single(righe);
        Assert.True(letta.Modificabile);
        Assert.Equal(1.2m, letta.PortataLminuto);
        var una = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici/1", token));
        Assert.Equal(450m, (await una.Content.ReadFromJsonAsync<IrroratriceAziendaDto>())!.VolumeAcqua);
        var catalogo = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/irroratrici", token));
        Assert.Equal("Atomizzatore", Assert.Single((await catalogo.Content.ReadFromJsonAsync<IrroratriciDto>())!.Righe).Descrizione);

        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/aziende/425/irroratrici/1", token))).StatusCode);
        Assert.Empty(db.Tabella("Irroratrice_Ente"));
        Assert.Equal(["nuovo", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
        Assert.All(db.LogOp, l => Assert.Equal(IdUtente, l.IdUtente));
        Assert.All(db.LogOp, l => Assert.Equal(1, l.IdRiga));
    }

    [Fact]
    public async Task Permesso_negato_fuori_dalle_proprie_aziende_senza_aggiornamenti_o_da_trattorista()
    {
        var (api, persona, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);
        db.Metti("Irroratrice_Ente", 7, new() { ["Id"] = 7, ["IdEnte"] = 426, ["IdIrroratrice"] = 1, ["BBCH"] = 53,
            ["VolumeAcqua"] = 200m, ["VelocitaAvanzamento"] = 6m, ["TipologiaUgelli"] = "Turbolenza" });

        // 426: abbinata ma senza aggiornamenti abilitati → si legge, non si scrive.
        var lista = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/426/irroratrici", token));
        Assert.False(Assert.Single((await lista.Content.ReadFromJsonAsync<List<IrroratriceAziendaDto>>())!).Modificabile);
        var put = await client.SendAsync(Richiesta(HttpMethod.Put, "/api/app/aziende/426/irroratrici/7", token, Regolazione()));
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Contains("aggiornamenti abilitati", await TitoloAsync(put));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/aziende/426/irroratrici/7", token))).StatusCode);
        // 500: non è sua.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/500/irroratrici", token))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/500/irroratrici", token, Regolazione()))).StatusCode);
        // La riga 7 è di 426: dall'indirizzo di 425 non esiste.
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/aziende/425/irroratrici/7", token))).StatusCode);

        // Dati sbagliati: 400 con il campo in italiano; irroratrice che non c'è.
        var vuota = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici", token,
            new IrroratriceAppSalvaDto(1, null, 300m, 6m, " ")));
        Assert.Equal(HttpStatusCode.BadRequest, vuota.StatusCode);
        Assert.Contains("Fase BBCH (bbch): campo obbligatorio.", await TitoloAsync(vuota));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici", token, Regolazione(irroratrice: 99)))).StatusCode);

        // Solo trattorista della suite su 425: legge, non scrive.
        persona.Abilitazioni.Clear();
        persona.Personali.Add(new AbilitazionePersonale(1, IdUtente, "SUITE_TRATTORISTA", 425, true, null, null));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", token))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici", token, Regolazione()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/426/irroratrici", token))).StatusCode);

        // Perleuve: tutte le aziende, anche 500.
        persona.Perleuve = true;
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/500/irroratrici", token, Regolazione()))).StatusCode);
        Assert.Equal(500, Assert.Single(db.Tabella("Irroratrice_Ente").Values, r => (int)r["Id"]! != 7)["IdEnte"]);
    }

    [Fact]
    public async Task Token_mancante_alterato_o_di_un_altra_app_401_e_app_sconosciuta_403()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);
        var (_, tokenAltra) = await EntraAsync(api, "altra");

        var senza = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici", null, Regolazione()));
        Assert.Equal(HttpStatusCode.Unauthorized, senza.StatusCode);
        Assert.Contains("Token mancante", await TitoloAsync(senza));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", tokenAltra))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", token + "x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", null, app: null))).StatusCode);
        var sconosciuta = Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", token, app: null);
        sconosciuta.Headers.Add(AppChiamante.IntestazioneNome, "suite");
        sconosciuta.Headers.Add(AppChiamante.IntestazioneSegreto, "sbagliato");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(sconosciuta)).StatusCode);
        // Senza segreto il token semplice non basta: non ha lo scopo delle irroratrici.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", token, app: null))).StatusCode);
        Assert.Empty(db.Tabella("Irroratrice_Ente"));
    }

    [Fact]
    public async Task App_browser_senza_segreto_con_lo_scopo_portale_irroratrici()
    {
        var (api, _, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();
        var chiavi = api.Services.GetRequiredService<ChiaviFirma>();

        string Token(string scopi, string app = "suite") => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "http://localhost/",
            Audience = app,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = IdUtente.ToString(), ["scope"] = scopi },
            SigningCredentials = new SigningCredentials(chiavi.Chiave, SecurityAlgorithms.RsaSha256),
        });

        var ok = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/aziende/425/irroratrici",
            Token("openid perleuve portale.irroratrici"), Regolazione(), app: null));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var senzaScopo = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", Token("openid perleuve"), app: null));
        Assert.Equal(HttpStatusCode.Forbidden, senzaScopo.StatusCode);
        Assert.Contains("portale.irroratrici", await TitoloAsync(senzaScopo));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/aziende/425/irroratrici", Token("portale.irroratrici", "inventata"), app: null))).StatusCode);
    }

    [Fact]
    public void Le_api_per_le_app_saltano_la_chiave_del_web_e_passano_dal_proxy()
    {
        Assert.True(Controllers.AccessoUnicoController.DellApp("/api/app/aziende/425/irroratrici/3"));
        Assert.False(Controllers.AccessoUnicoController.DellApp("/api/portale/irroratrici-azienda"));
        Assert.Contains("/api/app/{**resto}", ProgettoMappe.Web.Services.ProxyAccessoUnico.Percorsi);
    }
}
