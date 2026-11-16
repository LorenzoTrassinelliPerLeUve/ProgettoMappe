using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Sezione 22 «Accesso e password» (task #289) sull'Api vera con 4Grapes e posta finti: profilo, cambio password con quella attuale,
/// «Password dimenticata» → mail con il collegamento → «Reimposta password» (una volta sola), LogOp senza password né codice.
/// </summary>
public class PortaleAccountTests
{
    private const int Io = PermessiFinti.SenzaPermessi;
    private const string Email = "luca@esempio.it";

    private sealed class PostaFinta(bool vera) : IPosta
    {
        public List<MessaggioPosta> Mandate { get; } = [];
        public bool Vera => vera;

        public Task<EsitoPosta> InviaAsync(MessaggioPosta messaggio, CancellationToken ct = default)
        {
            Mandate.Add(messaggio);
            return Task.FromResult(new EsitoPosta(false, messaggio.Destinatari, null));
        }
    }

    /// <summary>Le credenziali come <c>FourGrapesUtentiRepository</c>: utenti attivi con l'app abilitata, LoginName senza maiuscole.</summary>
    private sealed class UtentiFinti(DbFinto db) : IUtentiRepository
    {
        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CredenzialiUtente>>(db.Tabella("Utente").Values
                .Where(r => string.Equals(((string)r["LoginName"]!).Trim(), loginName.Trim(), StringComparison.OrdinalIgnoreCase)
                            && Equals(r["StatoEntita"], (byte)1) && Equals(r["IsAppEnabled"], true))
                .Select(r => new CredenzialiUtente((int)r["idUtente"]!, (string)r["Nome"]!, (string)r["Cognome"]!, (string)r["ShaPassword"]!))
                .ToList());
    }

    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db, PostaFinta Posta) Avvia(
        string indirizzo = "https://mappe.esempio.it/")
    {
        var db = new DbFinto();
        var posta = new PostaFinta(true);
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("AccessoUnico:Indirizzo", indirizzo);
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiFinti>();
                s.AddSingleton<ILettureAmministrazionePortale>(new LettureAmministrazioneFinte(db));
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
                s.AddSingleton<IUtentiRepository>(new UtentiFinti(db));
                s.AddSingleton<IPosta>(posta);
            });
        });
        db.Metti("Utente", Io, Riga(Io, Email, "vecchia123"));
        return (api, db, posta);
    }

    private static Dictionary<string, object?> Riga(int id, string login, string password) => new()
    {
        ["idUtente"] = id, ["LoginName"] = login, ["Nome"] = "Luca", ["Cognome"] = "Bianchi", ["Lingua"] = "IT", ["Cellulare"] = "333",
        ["ShaPassword"] = PortaleAmministrazioneController.HashPassword(password), ["StatoEntita"] = (byte)1, ["IsAppEnabled"] = true,
        ["CodiceSicurezza"] = null,
    };

    private static void SenzaSegreti(DbFinto db) => Assert.All(db.LogOp, l =>
    {
        Assert.DoesNotContain("ShaPassword", l.Json);
        Assert.DoesNotContain("CodiceSicurezza", l.Json);
        Assert.DoesNotContain("reimposta?codice", l.Json);
    });

    [Fact]
    public async Task Profilo_LeggeESalva_SenzaCambiareEmail()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = Client(api, Io);

        var profilo = (await client.GetFromJsonAsync<ProfiloPortaleDto>("api/portale/account"))!;
        Assert.Equal(Email, profilo.LoginName);
        Assert.Contains("App Enabled", profilo.Ruoli);
        Assert.Equal("https://mappe.esempio.it/perleuve/password-dimenticata", profilo.RecuperoPassword);

        var vuoto = await client.PutAsJsonAsync("api/portale/account", new ProfiloSalvaDto(" ", "Bianchi", "XX", "333"));
        var errori = await ErroriAsync(vuoto);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Nome"]);
        Assert.Equal(["Scegli una lingua dell'elenco."], errori["Lingua"]);

        var ok = await client.PutAsJsonAsync("api/portale/account", new ProfiloSalvaDto(" Marco ", "Bianchi", "EN", "444"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var riga = db.Tabella("Utente")[Io];
        Assert.Equal(("Marco", "EN", Email), ((string)riga["Nome"]!, (string)riga["Lingua"]!, (string)riga["LoginName"]!));
        Assert.Equal(("modificato", "Utente"), (Assert.Single(db.LogOp).Operazione, db.LogOp[0].Tabella));
        SenzaSegreti(db);

        // Chi non ha l'app abilitata non entra nel Portale (regola «solo login» del portale).
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, 99).GetAsync("api/portale/account")).StatusCode);
    }

    [Fact]
    public async Task CambioPassword_ServeQuellaAttuale_AlmenoOttoCaratteri()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = Client(api, Io);
        db.Tabella("Utente")[Io]["CodiceSicurezza"] = Guid.NewGuid();

        var corta = await ErroriAsync(await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("vecchia123", " 1234567 ", " 1234567 ")));
        Assert.Equal(["La password deve avere almeno 8 caratteri."], corta["Nuova"]);
        var diverse = await ErroriAsync(await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("vecchia123", "nuova12345", "nuova12346")));
        Assert.Equal(["Le due password non sono uguali."], diverse["Conferma"]);
        var sbagliata = await ErroriAsync(await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("sbagliata", "nuova12345", "nuova12345")));
        Assert.Equal(["La password attuale non è giusta."], sbagliata["Attuale"]);
        Assert.Empty(db.LogOp);

        var ok = await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("vecchia123", "nuova12345", "nuova12345"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(PortaleAmministrazioneController.HashPassword("nuova12345"), db.Tabella("Utente")[Io]["ShaPassword"]);
        Assert.Null(db.Tabella("Utente")[Io]["CodiceSicurezza"]); // un collegamento «password dimenticata» aperto non vale più
        SenzaSegreti(db);
        Assert.DoesNotContain(PortaleAmministrazioneController.HashPassword("nuova12345"), db.LogOp[0].Json);

        // Dopo 5 errori: fermo per 15 minuti.
        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("sbagliata", "altra12345", "altra12345"));
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("api/portale/account/password", new CambioPasswordDto("nuova12345", "altra12345", "altra12345"))).StatusCode);
    }

    private static FormUrlEncodedContent Modulo(params (string Nome, string Valore)[] campi) =>
        new(campi.Select(c => new KeyValuePair<string, string>(c.Nome, c.Valore)));

    [Fact]
    public async Task PasswordDimenticata_MailCollegamento_ReimpostaUnaVolta()
    {
        var (api, db, posta) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();

        Assert.Contains("password-dimenticata", await client.GetStringAsync("/perleuve/password-dimenticata"));
        var richiesta = await client.PostAsync("/perleuve/password-dimenticata", Modulo(("email", " LUCA@esempio.it ")));
        Assert.Equal(HttpStatusCode.OK, richiesta.StatusCode);
        Assert.Contains("Vale un", await richiesta.Content.ReadAsStringAsync());

        var mail = Assert.Single(posta.Mandate);
        Assert.Equal(["LUCA@esempio.it"], mail.Destinatari);
        var codice = Assert.IsType<Guid>(db.Tabella("Utente")[Io]["CodiceSicurezza"]);
        var collegamento = WebUtility.HtmlDecode(Regex.Match(mail.Html, "href=\"([^\"]+)\"").Groups[1].Value);
        Assert.StartsWith("https://mappe.esempio.it/perleuve/reimposta?codice=", collegamento);
        Assert.DoesNotContain(codice.ToString("N"), collegamento); // il codice viaggia solo firmato
        var testo = Uri.UnescapeDataString(collegamento[(collegamento.IndexOf('=') + 1)..]);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/perleuve/reimposta?codice=" + Uri.EscapeDataString(testo))).StatusCode);
        var corta = await client.PostAsync("/perleuve/reimposta", Modulo(("codice", testo), ("password", "corta"), ("conferma", "corta")));
        Assert.Equal(HttpStatusCode.BadRequest, corta.StatusCode);
        Assert.Contains("almeno 8 caratteri", await corta.Content.ReadAsStringAsync());

        var ok = await client.PostAsync("/perleuve/reimposta", Modulo(("codice", testo), ("password", "nuova12345"), ("conferma", "nuova12345")));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Contains("Fatto: la password", await ok.Content.ReadAsStringAsync());
        Assert.Equal(PortaleAmministrazioneController.HashPassword("nuova12345"), db.Tabella("Utente")[Io]["ShaPassword"]);
        Assert.Null(db.Tabella("Utente")[Io]["CodiceSicurezza"]);

        // Una volta sola.
        var dinuovo = await client.PostAsync("/perleuve/reimposta", Modulo(("codice", testo), ("password", "altra12345"), ("conferma", "altra12345")));
        Assert.Equal(HttpStatusCode.BadRequest, dinuovo.StatusCode);
        Assert.Equal(PortaleAmministrazioneController.HashPassword("nuova12345"), db.Tabella("Utente")[Io]["ShaPassword"]);

        Assert.Equal(["modificato", "inviato", "modificato"], db.LogOp.Select(l => l.Operazione));
        SenzaSegreti(db);
    }

    [Fact]
    public async Task PasswordDimenticata_EmailSconosciutaODoppia_StessaRisposta_NessunaMail()
    {
        var (api, db, posta) = Avvia();
        await using var _a = api;
        db.Metti("Utente", 60, Riga(60, "doppio@esempio.it", "a12345678"));
        db.Metti("Utente", 61, Riga(61, "doppio@esempio.it", "b12345678"));
        var client = api.CreateClient();

        var conosciuta = await (await client.PostAsync("/perleuve/password-dimenticata", Modulo(("email", Email)))).Content.ReadAsStringAsync();
        foreach (var email in new[] { "nessuno@esempio.it", "doppio@esempio.it" })
        {
            var risposta = await client.PostAsync("/perleuve/password-dimenticata", Modulo(("email", email)));
            Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
            Assert.Equal(conosciuta, await risposta.Content.ReadAsStringAsync());
        }

        Assert.Single(posta.Mandate);
        // Limite: 3 richieste all'ora per email.
        for (var i = 0; i < 2; i++) await client.PostAsync("/perleuve/password-dimenticata", Modulo(("email", Email)));
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/perleuve/password-dimenticata", Modulo(("email", Email)))).StatusCode);
    }

    [Fact]
    public async Task Collegamento_ToccatoOAltroCodice_NonVale_SenzaIndirizzoSpento()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();
        var recupero = api.Services.GetRequiredService<RecuperoPassword>();

        // Firmato ma con un codice che in 4Grapes non c'è (ne è stato chiesto uno più nuovo).
        db.Tabella("Utente")[Io]["CodiceSicurezza"] = Guid.NewGuid();
        var vecchio = recupero.Codice(Io, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var r = await client.PostAsync("/perleuve/reimposta", Modulo(("codice", vecchio), ("password", "nuova12345"), ("conferma", "nuova12345")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/perleuve/reimposta?codice=finto")).StatusCode);
        // Scaduto.
        Assert.Null(recupero.Leggi(recupero.Codice(Io, Guid.NewGuid(), DateTimeOffset.UtcNow - RecuperoPassword.Validita - TimeSpan.FromMinutes(1))));
        Assert.Empty(db.LogOp);

        var (spenta, _, posta) = Avvia(indirizzo: "");
        await using var _s = spenta;
        var risposta = await spenta.CreateClient().PostAsync("/perleuve/password-dimenticata", Modulo(("email", Email)));
        Assert.Equal(HttpStatusCode.NotFound, risposta.StatusCode);
        Assert.Contains("portale.4grapes.it", await risposta.Content.ReadAsStringAsync());
        Assert.Empty(posta.Mandate);
    }
}
