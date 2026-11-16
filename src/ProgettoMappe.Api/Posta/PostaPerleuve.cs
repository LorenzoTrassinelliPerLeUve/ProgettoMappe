using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgettoMappe.Api.Posta;

/// <summary>
/// Configurazione della posta Perleuve (sezione <c>Posta</c>, decisione E4 del proprietario, 16/11): Microsoft Graph con l'app
/// Entra «agente-m365-invio» (solo Mail.Send). <see cref="FileSegreti"/> è un JSON con <c>tenant</c>, <c>client_id</c>,
/// <c>client_secret</c> (in produzione <c>/var/lib/mappe/segreti/m365-invio.json</c>, lo mette il boss: mai nel repository).
/// </summary>
public sealed class PostaOptions
{
    public string? FileSegreti { get; set; }

    /// <summary>La casella da cui parte la posta (bollettini@perleuve.it).</summary>
    public string? Mittente { get; set; }

    /// <summary>«vere» = si spedisce davvero; qualsiasi altro valore (predefinito «prova») = invio simulato e registrato.</summary>
    public string? Modo { get; set; }

    /// <summary>Cartella dove resta la copia di ogni bollettino inviato (vuota = nessuna copia).</summary>
    public string? Archivio { get; set; }
}

/// <summary>Un messaggio: a ogni destinatario arriva una mail sua (non vede gli altri, come i messaggi di SendGrid del portale).</summary>
public sealed record MessaggioPosta(IReadOnlyList<string> Destinatari, string Oggetto, string Html);

/// <param name="Simulato">true = modo prova o segreti mancanti: niente è partito.</param>
/// <param name="Errore">null = tutto partito; altrimenti cosa è andato storto (senza segreti).</param>
public sealed record EsitoPosta(bool Simulato, IReadOnlyList<string> Inviati, string? Errore)
{
    public bool Riuscito => Errore is null;
}

public interface IPosta
{
    /// <summary>true se le mail partono davvero (modo «vere» e segreti presenti).</summary>
    bool Vera { get; }

    Task<EsitoPosta> InviaAsync(MessaggioPosta messaggio, CancellationToken ct = default);
}

/// <summary>
/// Invio con Microsoft Graph: token <c>client_credentials</c> (scopo <c>https://graph.microsoft.com/.default</c>, tenuto fino a un
/// minuto dalla scadenza) e <c>POST /users/{mittente}/sendMail</c>, una chiamata per destinatario. Si ferma al primo errore:
/// l'esito dice chi l'ha già ricevuta. In modo prova, o se il file dei segreti non c'è (anteprima), simula e lo scrive nel log.
/// </summary>
public sealed class PostaGraph : IPosta
{
    public const string NomeClient = "graph";
    private readonly IHttpClientFactory _http;
    private readonly PostaOptions _opzioni;
    private readonly TimeProvider _tempo;
    private readonly ILogger<PostaGraph> _log;
    private readonly SemaphoreSlim _blocco = new(1, 1);
    private string? _token;
    private DateTimeOffset _scadenza;

    public PostaGraph(IHttpClientFactory http, Microsoft.Extensions.Options.IOptions<PostaOptions> opzioni, TimeProvider tempo,
        ILogger<PostaGraph> log)
    {
        _http = http;
        _opzioni = opzioni.Value;
        _tempo = tempo;
        _log = log;
    }

    public bool Vera => string.Equals(_opzioni.Modo?.Trim(), "vere", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(_opzioni.Mittente) && File.Exists(_opzioni.FileSegreti ?? "");

    public async Task<EsitoPosta> InviaAsync(MessaggioPosta messaggio, CancellationToken ct = default)
    {
        if (!Vera)
        {
            _log.LogInformation("Posta in prova: «{Oggetto}» a {Numero} destinatari non spedita", messaggio.Oggetto, messaggio.Destinatari.Count);
            return new EsitoPosta(true, messaggio.Destinatari, null);
        }

        var inviati = new List<string>();
        try
        {
            var token = await TokenAsync(ct);
            var client = _http.CreateClient(NomeClient);
            foreach (var a in messaggio.Destinatari)
            {
                using var richiesta = new HttpRequestMessage(HttpMethod.Post,
                    $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(_opzioni.Mittente!.Trim())}/sendMail")
                {
                    Content = JsonContent(Corpo(messaggio, a)),
                };
                richiesta.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var risposta = await client.SendAsync(richiesta, ct);
                if (!risposta.IsSuccessStatusCode)
                {
                    var dettaglio = await risposta.Content.ReadAsStringAsync(ct);
                    return Fallito(inviati, $"Graph ha risposto {(int)risposta.StatusCode} per {a}: {Corto(dettaglio)}");
                }

                inviati.Add(a);
            }

            return new EsitoPosta(false, inviati, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException
                                       or InvalidOperationException)
        {
            return Fallito(inviati, ex.Message);
        }
    }

    private EsitoPosta Fallito(List<string> inviati, string errore)
    {
        _log.LogWarning("Posta non spedita: {Errore} (già partite: {Numero})", errore, inviati.Count);
        return new EsitoPosta(false, inviati, errore);
    }

    /// <summary>Il JSON di <c>sendMail</c>: HTML, un destinatario, copia nella posta inviata del mittente.</summary>
    public static object Corpo(MessaggioPosta m, string destinatario) => new
    {
        message = new
        {
            subject = m.Oggetto,
            body = new { contentType = "HTML", content = m.Html },
            toRecipients = new[] { new { emailAddress = new { address = destinatario } } },
        },
        saveToSentItems = true,
    };

    private static StringContent JsonContent(object corpo) =>
        new(JsonSerializer.Serialize(corpo), System.Text.Encoding.UTF8, "application/json");

    private static string Corto(string testo) => testo.Length > 300 ? testo[..300] + "…" : testo;

    private sealed record Segreti(
        [property: JsonPropertyName("tenant")] string? Tenant,
        [property: JsonPropertyName("client_id")] string? ClientId,
        [property: JsonPropertyName("client_secret")] string? ClientSecret);

    private sealed record RispostaToken(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private async Task<string> TokenAsync(CancellationToken ct)
    {
        await _blocco.WaitAsync(ct);
        try
        {
            if (_token is not null && _tempo.GetUtcNow() < _scadenza)
            {
                return _token;
            }

            var segreti = JsonSerializer.Deserialize<Segreti>(await File.ReadAllTextAsync(_opzioni.FileSegreti!, ct));
            if (string.IsNullOrWhiteSpace(segreti?.Tenant) || string.IsNullOrWhiteSpace(segreti.ClientId)
                || string.IsNullOrWhiteSpace(segreti.ClientSecret))
            {
                throw new InvalidOperationException("Il file dei segreti della posta non ha tenant, client_id e client_secret");
            }

            using var risposta = await _http.CreateClient(NomeClient).PostAsync(
                $"https://login.microsoftonline.com/{Uri.EscapeDataString(segreti.Tenant.Trim())}/oauth2/v2.0/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = segreti.ClientId.Trim(),
                    ["client_secret"] = segreti.ClientSecret.Trim(),
                    ["scope"] = "https://graph.microsoft.com/.default",
                }), ct);
            if (!risposta.IsSuccessStatusCode)
            {
                // Il corpo dell'errore di Entra non contiene il segreto: basta il codice.
                throw new InvalidOperationException($"Token di Microsoft non ottenuto ({(int)risposta.StatusCode})");
            }

            var token = JsonSerializer.Deserialize<RispostaToken>(await risposta.Content.ReadAsStringAsync(ct));
            _token = token?.AccessToken ?? throw new InvalidOperationException("Token di Microsoft vuoto");
            _scadenza = _tempo.GetUtcNow().AddSeconds(Math.Max(0, token.ExpiresIn - 60));
            return _token;
        }
        finally
        {
            _blocco.Release();
        }
    }
}
