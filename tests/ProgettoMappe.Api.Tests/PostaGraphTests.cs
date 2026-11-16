using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Posta;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>La posta Perleuve con un finto Graph (task #280): modo prova, token client_credentials, sendMail, errori.</summary>
public class PostaGraphTests
{
    /// <summary>Un finto Graph: risponde al token e a sendMail, e ricorda le richieste.</summary>
    private sealed class FintoGraph : HttpMessageHandler
    {
        public List<(string Url, string? Autorizzazione, string Corpo)> Richieste { get; } = [];

        public HttpStatusCode StatoPer(string destinatario) => destinatario.StartsWith("rifiutato", StringComparison.Ordinal)
            ? HttpStatusCode.Forbidden
            : HttpStatusCode.Accepted;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var corpo = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Richieste.Add((request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), corpo));
            if (request.RequestUri.Host == "login.microsoftonline.com")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"tok-1","expires_in":3600}""") };
            var a = JsonDocument.Parse(corpo).RootElement.GetProperty("message").GetProperty("toRecipients")[0]
                .GetProperty("emailAddress").GetProperty("address").GetString()!;
            return new HttpResponseMessage(StatoPer(a)) { Content = new StringContent(StatoPer(a) == HttpStatusCode.Accepted ? "" : """{"error":"no"}""") };
        }
    }

    private sealed class Fabbrica(HttpMessageHandler gestore) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(gestore, disposeHandler: false);
    }

    private static (PostaGraph Posta, FintoGraph Graph) Crea(string modo, bool conSegreti = true)
    {
        var file = Path.Combine(Path.GetTempPath(), $"posta-{Guid.NewGuid():N}.json");
        if (conSegreti) File.WriteAllText(file, """{"tenant":"t-finto","client_id":"c-finto","client_secret":"s-finto"}""");
        var graph = new FintoGraph();
        var posta = new PostaGraph(new Fabbrica(graph), Options.Create(new PostaOptions
        {
            FileSegreti = file, Mittente = "bollettini@perleuve.it", Modo = modo,
        }), TimeProvider.System, NullLogger<PostaGraph>.Instance);
        return (posta, graph);
    }

    private static readonly MessaggioPosta Messaggio = new(["a@esempio.it", "b@esempio.it"], "Avviso Trattamento", "<p>ciao</p>");

    [Theory]
    [InlineData("prova", true)]
    [InlineData("vere", false)] // in anteprima il file dei segreti non c'è
    public async Task Prova_OSenzaSegreti_SimulaENonChiamaGraph(string modo, bool conSegreti)
    {
        var (posta, graph) = Crea(modo, conSegreti);

        var esito = await posta.InviaAsync(Messaggio);

        Assert.False(posta.Vera);
        Assert.True(esito.Simulato);
        Assert.True(esito.Riuscito);
        Assert.Empty(graph.Richieste);
    }

    [Fact]
    public async Task Vere_TokenClientCredentialsUnaVolta_PoiUnaSendMailPerDestinatario()
    {
        var (posta, graph) = Crea("vere");

        var esito = await posta.InviaAsync(Messaggio);
        await posta.InviaAsync(Messaggio with { Destinatari = ["c@esempio.it"] });

        Assert.True(esito.Riuscito);
        Assert.False(esito.Simulato);
        Assert.Equal(["a@esempio.it", "b@esempio.it"], esito.Inviati);
        var token = Assert.Single(graph.Richieste, r => r.Url.Contains("login.microsoftonline.com"));
        Assert.Equal("https://login.microsoftonline.com/t-finto/oauth2/v2.0/token", token.Url);
        Assert.Contains("grant_type=client_credentials", token.Corpo);
        Assert.Contains("scope=https%3A%2F%2Fgraph.microsoft.com%2F.default", token.Corpo);
        var invii = graph.Richieste.Where(r => r.Url.Contains("sendMail")).ToList();
        Assert.Equal(3, invii.Count);
        Assert.All(invii, r =>
        {
            Assert.Equal("https://graph.microsoft.com/v1.0/users/bollettini%40perleuve.it/sendMail", r.Url);
            Assert.Equal("Bearer tok-1", r.Autorizzazione);
        });
        var m = JsonDocument.Parse(invii[0].Corpo).RootElement.GetProperty("message");
        Assert.Equal(("Avviso Trattamento", "HTML", "<p>ciao</p>"), (m.GetProperty("subject").GetString(),
            m.GetProperty("body").GetProperty("contentType").GetString(), m.GetProperty("body").GetProperty("content").GetString()));
        Assert.Equal(1, m.GetProperty("toRecipients").GetArrayLength());
    }

    [Fact]
    public async Task ErroreDiGraph_SiFerma_EDiceChiLHaGiaRicevuta()
    {
        var (posta, _) = Crea("vere");

        var esito = await posta.InviaAsync(Messaggio with { Destinatari = ["a@esempio.it", "rifiutato@esempio.it", "c@esempio.it"] });

        Assert.False(esito.Riuscito);
        Assert.Equal(["a@esempio.it"], esito.Inviati);
        Assert.Contains("403", esito.Errore);
        Assert.DoesNotContain("s-finto", esito.Errore);
    }
}
