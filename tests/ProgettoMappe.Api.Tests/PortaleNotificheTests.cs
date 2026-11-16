using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Tappa 5c (task #284): 21 Notifiche sull'Api vera con 4Grapes, push e posta finti (righe di Notifica, destinatari come il portale,
/// LogOp «inviato», simulazione), e il client Firebase con un finto Google.
/// </summary>
public class PortaleNotificheTests
{
    private sealed class PushFinta(bool vera) : IPush
    {
        public List<MessaggioPush> Mandate { get; } = [];
        public bool Vera => vera;

        public Task<EsitoPush> InviaAsync(IReadOnlyList<MessaggioPush> messaggi, CancellationToken ct = default)
        {
            Mandate.AddRange(messaggi);
            var rifiutate = messaggi.Count(m => m.Topic.Contains("rifiutato"));
            return Task.FromResult(new EsitoPush(false, messaggi.Count - rifiutate, rifiutate, rifiutate > 0 ? "topic rifiutato" : null));
        }
    }

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

    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db, PushFinta Push, PostaFinta Posta)
        Avvia(bool vere = true)
    {
        var db = new DbFinto();
        var push = new PushFinta(vere);
        var posta = new PostaFinta(vere);
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiFinti>();
                s.AddSingleton<ILettureAmministrazionePortale>(new LettureAmministrazioneFinte(db));
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
                s.AddSingleton<IPush>(push);
                s.AddSingleton<IPosta>(posta);
            }));
        db.Metti("TipoNotifica", 1, new() { ["IdTipoNotifica"] = 1, ["Descrizione"] = "Info" });
        db.Metti("Utente", 50, new() { ["idUtente"] = 50, ["LoginName"] = "luca@esempio.it", ["IsAppEnabled"] = true });
        db.Metti("Utente", 51, new() { ["idUtente"] = 51, ["LoginName"] = "sara rossi@esempio.it", ["IsAppEnabled"] = true });
        db.Metti("Utente", 52, new() { ["idUtente"] = 52, ["LoginName"] = "spento@esempio.it", ["IsAppEnabled"] = false });
        db.Metti("GruppoUtenti", 7, new() { ["Id"] = 7, ["Descrizione"] = "Tecnici" });
        db.Metti("ComponenteGruppoUtenti", new ChiaveDoppia(7, 52), new() { ["IdGruppo"] = 7, ["IdUtente"] = 52 });
        return (api, db, push, posta);
    }

    private static NotificaInviaDto Avviso(int[]? gruppi = null, int[]? utenti = null) =>
        new("Peronospora", 1, "Rischio alto da domani: trattare entro 48 ore.", gruppi ?? [], utenti ?? []);

    [Fact]
    public async Task SenzaScelte_TuttiConLApp_RigheNotificaPushEmailEUnaRigaDiLogOp()
    {
        var (api, db, push, posta) = Avvia();
        await using var _a = api;
        // Utente finto con INVIA_NOTIFICHE: il Master ha le abilitazioni della difesa, qui si usa quello dell'amministrazione.
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var risposta = await client.PostAsJsonAsync("api/portale/notifiche", Avviso());
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        var esito = (await risposta.Content.ReadFromJsonAsync<EsitoNotificaDto>())!;
        Assert.Equal(2, esito.Destinatari);
        Assert.False(esito.PushSimulate);
        Assert.Equal(2, esito.PushInviate);
        Assert.Equal(2, esito.EmailInviate);

        Assert.Equal([50, 51], db.Tabella("Notifica").Values.Select(r => (int)r["IdUtente"]!).Order());
        Assert.All(db.Tabella("Notifica").Values, r => Assert.Equal("Peronospora", r["Testata"]));
        // Topic come il portale: «user-» + email con l'escape (% → -).
        Assert.Equal(["user-luca-40esempio.it", "user-sara-20rossi-40esempio.it"], push.Mandate.Select(m => m.Topic));
        Assert.Equal(["luca@esempio.it", "sara rossi@esempio.it"], Assert.Single(posta.Mandate).Destinatari);
        var log = Assert.Single(db.LogOp);
        Assert.Equal(("inviato", "Notifica"), (log.Operazione, log.Tabella));
        Assert.Contains("\"Destinatari\":2", log.Json);
    }

    [Fact]
    public async Task GruppiEUtenti_UnioneSenzaDoppi_ErroriDellaPushNonAnnullano()
    {
        var (api, db, push, _) = Avvia();
        await using var _a = api;
        db.Metti("Utente", 53, new() { ["idUtente"] = 53, ["LoginName"] = "rifiutato@esempio.it", ["IsAppEnabled"] = true });
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var esito = (await (await client.PostAsJsonAsync("api/portale/notifiche", Avviso([7], [52, 53]) with { AncheEmail = false }))
            .Content.ReadFromJsonAsync<EsitoNotificaDto>())!;
        Assert.Equal(2, esito.Destinatari); // 52 dal gruppo e scelto: una volta sola
        Assert.Equal(1, esito.PushNonPartite);
        Assert.Equal("topic rifiutato", esito.Errore);
        Assert.Equal(0, esito.EmailInviate);
        Assert.Equal(2, db.Tabella("Notifica").Count);
        Assert.Equal(2, push.Mandate.Count);
    }

    [Fact]
    public async Task SenzaCredenziali_SimulaENonChiama()
    {
        var (api, db, push, posta) = Avvia(vere: false);
        await using var _a = api;
        var esito = (await (await Client(api, PermessiFinti.AmministratoreCompleto).PostAsJsonAsync("api/portale/notifiche", Avviso()))
            .Content.ReadFromJsonAsync<EsitoNotificaDto>())!;
        Assert.True(esito.PushSimulate);
        Assert.True(esito.PostaSimulata);
        Assert.Empty(push.Mandate);
        Assert.Empty(posta.Mandate);
        Assert.Contains("simulate", Assert.Single(db.LogOp).Json);
    }

    [Fact]
    public async Task Controlli_EPermessi()
    {
        var (api, db, _, _) = Avvia();
        await using var _a = api;
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var vuota = await client.PostAsJsonAsync("api/portale/notifiche", new NotificaInviaDto(" ", null, "", null, null));
        var errori = await ErroriAsync(vuota);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Testata"]);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Corpo"]);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["IdTipoNotifica"]);
        var icona = await client.PostAsJsonAsync("api/portale/notifiche", Avviso() with { IdTipoNotifica = 99 });
        Assert.Equal(["Scegli un'icona dell'elenco."], (await ErroriAsync(icona))["IdTipoNotifica"]);
        db.Tabella("ComponenteGruppoUtenti").Clear();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/portale/notifiche", Avviso([7]))).StatusCode);
        Assert.Empty(db.LogOp);

        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.SoloUtenti).GetAsync("api/portale/notifiche")).StatusCode);
        var modulo = await client.GetFromJsonAsync<NotificheDto>("api/portale/notifiche");
        Assert.Equal("Info", Assert.Single(modulo!.Tipi).Testo);
        Assert.True(modulo.PushVera);
    }

    [Fact]
    public void Email_SoloTesto_ACapo() =>
        Assert.Equal("<h3>A &lt;b&gt;</h3><p>riga 1<br>riga 2</p><p style=\"color:#5f727f\">4Grapes · Perleuve</p>",
            PortaleAmministrazioneController.HtmlEmail("A <b>", "riga 1\r\nriga 2"));

    // ---- Client Firebase con un finto Google ----

    private sealed class FintoGoogle : HttpMessageHandler
    {
        public List<(string Url, string? Autorizzazione, string Corpo)> Richieste { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var corpo = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Richieste.Add((request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), corpo));
            if (request.RequestUri.Host == "oauth2.googleapis.com")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"g-tok","expires_in":3600}""") };
            return new HttpResponseMessage(corpo.Contains("rifiutato") ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent("""{"name":"x"}"""),
            };
        }
    }

    private sealed class Fabbrica(HttpMessageHandler gestore) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(gestore, disposeHandler: false);
    }

    [Fact]
    public async Task Firebase_TokenDaAccountDiServizio_PoiUnMessaggioPerTopic()
    {
        using var rsa = RSA.Create(2048);
        var file = Path.Combine(Path.GetTempPath(), $"firebase-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["project_id"] = "grapes-prova", ["client_email"] = "fcm@grapes-prova.iam.gserviceaccount.com",
            ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(), ["token_uri"] = "https://oauth2.googleapis.com/token",
        }));
        var google = new FintoGoogle();
        var push = new PushFirebase(new Fabbrica(google), Options.Create(new FirebaseOptions { File = file }), TimeProvider.System,
            NullLogger<PushFirebase>.Instance);

        var esito = await push.InviaAsync([new("T", "C", "user-a-40b.it"), new("T", "C", "user-rifiutato"), new("T", "C", "user-c")]);

        Assert.True(push.Vera);
        Assert.Equal((false, 2, 1), (esito.Simulato, esito.Inviati, esito.Errori));
        Assert.StartsWith("Firebase ha risposto 404 per user-rifiutato", esito.PrimoErrore);
        Assert.Equal(4, google.Richieste.Count); // un token solo
        Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer", google.Richieste[0].Corpo);
        Assert.Equal("https://fcm.googleapis.com/v1/projects/grapes-prova/messages:send", google.Richieste[1].Url);
        Assert.Equal("Bearer g-tok", google.Richieste[1].Autorizzazione);
        var messaggio = JsonDocument.Parse(google.Richieste[1].Corpo).RootElement.GetProperty("message");
        Assert.Equal("user-a-40b.it", messaggio.GetProperty("topic").GetString());
        Assert.Equal("high", messaggio.GetProperty("android").GetProperty("priority").GetString());

        // L'asserzione è un JWT RS256 firmato con la chiave del file.
        var jwt = PushFirebase.Asserzione("a@b", rsa.ExportPkcs8PrivateKeyPem(), "https://oauth2.googleapis.com/token", DateTimeOffset.UtcNow);
        var parti = jwt.Split('.');
        static byte[] Da64(string s) => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/').PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        Assert.True(rsa.VerifyData(Encoding.ASCII.GetBytes($"{parti[0]}.{parti[1]}"), Da64(parti[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        Assert.Contains("firebase.messaging", Encoding.UTF8.GetString(Da64(parti[1])));
    }

    [Fact]
    public async Task Firebase_SenzaFile_Simula()
    {
        var google = new FintoGoogle();
        var push = new PushFirebase(new Fabbrica(google), Options.Create(new FirebaseOptions { File = "/non/esiste.json" }),
            TimeProvider.System, NullLogger<PushFirebase>.Instance);
        var esito = await push.InviaAsync([new("T", "C", "user-a")]);
        Assert.False(push.Vera);
        Assert.True(esito.Simulato);
        Assert.Empty(google.Richieste);
    }

    [Theory]
    [InlineData("mario.rossi@esempio.it", "user-mario.rossi-40esempio.it")]
    [InlineData("a b@c.it", "user-a-20b-40c.it")]
    public void TopicUtente_ComeIlPortale(string login, string atteso) => Assert.Equal(atteso, PushFirebase.TopicUtente(login));

    [Fact]
    public void Taglia_A120() => Assert.Equal(120, PushFirebase.Taglia(new string('x', 300)).Length);
}
