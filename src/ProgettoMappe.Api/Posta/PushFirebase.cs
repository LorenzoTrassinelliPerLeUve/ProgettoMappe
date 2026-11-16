using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgettoMappe.Api.Posta;

/// <summary>
/// Configurazione delle push sul telefono (sezione <c>Notifiche:Firebase</c>, task #284): <see cref="File"/> è il JSON
/// dell'account di servizio di Firebase del progetto dell'app 4Grapes (<c>project_id</c>, <c>client_email</c>, <c>private_key</c>;
/// lo dà Dario, lo mette il proprietario fuori dal repository). Senza file = push simulate e registrate.
/// </summary>
public sealed class FirebaseOptions
{
    public string? File { get; set; }

    /// <summary>Il progetto se il file non lo dice (il portale lo ha scritto nel codice: «grapes-160318»).</summary>
    public string ProgettoId { get; set; } = "grapes-160318";
}

/// <summary>Una push: titolo e testo (il portale taglia il corpo a 120 caratteri) verso un topic.</summary>
public sealed record MessaggioPush(string Titolo, string Testo, string Topic);

/// <param name="Simulato">true = senza file delle credenziali: niente è partito.</param>
/// <param name="Inviati">I topic a cui la push è arrivata a Firebase.</param>
/// <param name="Errori">Quanti topic Firebase ha rifiutato, con il primo errore.</param>
public sealed record EsitoPush(bool Simulato, int Inviati, int Errori, string? PrimoErrore);

public interface IPush
{
    /// <summary>true se le push partono davvero (file delle credenziali presente).</summary>
    bool Vera { get; }

    Task<EsitoPush> InviaAsync(IReadOnlyList<MessaggioPush> messaggi, CancellationToken ct = default);
}

/// <summary>
/// Firebase Cloud Messaging, API HTTP v1 come <c>FCMPushNotification.SendNotification</c> del portale: token OAuth dall'account di
/// servizio (JWT RS256, scopo <c>firebase.messaging</c>) e <c>POST projects/{id}/messages:send</c> per topic, con priorità alta e
/// suono «default» su Android e iOS. Un topic rifiutato non ferma gli altri (il portale si fermava al primo errore).
/// </summary>
public sealed class PushFirebase : IPush
{
    public const string NomeClient = "firebase";
    private const string Scopo = "https://www.googleapis.com/auth/firebase.messaging";
    private readonly IHttpClientFactory _http;
    private readonly FirebaseOptions _opzioni;
    private readonly TimeProvider _tempo;
    private readonly ILogger<PushFirebase> _log;
    private readonly SemaphoreSlim _blocco = new(1, 1);
    private string? _token;
    private DateTimeOffset _scadenza;

    public PushFirebase(IHttpClientFactory http, Microsoft.Extensions.Options.IOptions<FirebaseOptions> opzioni, TimeProvider tempo,
        ILogger<PushFirebase> log)
    {
        _http = http;
        _opzioni = opzioni.Value;
        _tempo = tempo;
        _log = log;
    }

    public bool Vera => System.IO.File.Exists(_opzioni.File ?? "");

    /// <summary>Il topic dell'utente: «user-» + email, con l'escape del portale (<c>EscapeTopic</c>: % → -).</summary>
    public static string TopicUtente(string loginName) => Uri.EscapeDataString("user-" + loginName).Replace("%", "-");

    /// <summary>Il corpo come lo manda il portale: al massimo 120 caratteri (<c>MaxCaratteri(120)</c>).</summary>
    public static string Taglia(string testo) => testo.Length > 120 ? testo[..120] : testo;

    public async Task<EsitoPush> InviaAsync(IReadOnlyList<MessaggioPush> messaggi, CancellationToken ct = default)
    {
        if (!Vera)
        {
            _log.LogInformation("Push in prova: {Numero} notifiche non spedite (manca il file delle credenziali di Firebase)", messaggi.Count);
            return new EsitoPush(true, messaggi.Count, 0, null);
        }

        int inviati = 0, errori = 0;
        string? primo = null;
        try
        {
            var (token, progetto) = await TokenAsync(ct);
            var client = _http.CreateClient(NomeClient);
            foreach (var m in messaggi)
            {
                using var richiesta = new HttpRequestMessage(HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(progetto)}/messages:send")
                {
                    Content = new StringContent(JsonSerializer.Serialize(Corpo(m)), Encoding.UTF8, "application/json"),
                };
                richiesta.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var risposta = await client.SendAsync(richiesta, ct);
                if (risposta.IsSuccessStatusCode)
                {
                    inviati++;
                    continue;
                }

                errori++;
                primo ??= $"Firebase ha risposto {(int)risposta.StatusCode} per {m.Topic}: {Corto(await risposta.Content.ReadAsStringAsync(ct))}";
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException
                                       or InvalidOperationException or CryptographicException or ArgumentException)
        {
            errori = messaggi.Count - inviati;
            primo ??= ex.Message;
        }

        if (errori > 0) _log.LogWarning("Push: {Errori} non partite su {Totale}: {Errore}", errori, messaggi.Count, primo);
        return new EsitoPush(false, inviati, errori, primo);
    }

    /// <summary>Il messaggio di FCM v1 con le stesse impostazioni del portale.</summary>
    public static object Corpo(MessaggioPush m) => new
    {
        message = new
        {
            topic = m.Topic,
            notification = new { title = m.Titolo, body = m.Testo },
            data = new Dictionary<string, string> { ["priority"] = "high" },
            android = new { priority = "high", notification = new { sound = "default" } },
            apns = new { payload = new { aps = new Dictionary<string, object> { ["sound"] = "default", ["content-available"] = 1 } } },
        },
    };

    private static string Corto(string testo) => testo.Length > 300 ? testo[..300] + "…" : testo;

    private sealed record Credenziali(
        [property: JsonPropertyName("project_id")] string? ProjectId,
        [property: JsonPropertyName("client_email")] string? ClientEmail,
        [property: JsonPropertyName("private_key")] string? PrivateKey,
        [property: JsonPropertyName("token_uri")] string? TokenUri);

    private sealed record RispostaToken(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private string _progetto = "";

    private async Task<(string Token, string Progetto)> TokenAsync(CancellationToken ct)
    {
        await _blocco.WaitAsync(ct);
        try
        {
            if (_token is not null && _tempo.GetUtcNow() < _scadenza)
            {
                return (_token, _progetto);
            }

            var c = JsonSerializer.Deserialize<Credenziali>(await System.IO.File.ReadAllTextAsync(_opzioni.File!, ct));
            if (string.IsNullOrWhiteSpace(c?.ClientEmail) || string.IsNullOrWhiteSpace(c.PrivateKey))
            {
                throw new InvalidOperationException("Il file delle credenziali di Firebase non ha client_email e private_key");
            }

            var uri = string.IsNullOrWhiteSpace(c.TokenUri) ? "https://oauth2.googleapis.com/token" : c.TokenUri.Trim();
            var assertion = Asserzione(c.ClientEmail.Trim(), c.PrivateKey, uri, _tempo.GetUtcNow());
            using var risposta = await _http.CreateClient(NomeClient).PostAsync(uri, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            }), ct);
            if (!risposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token di Google non ottenuto ({(int)risposta.StatusCode})");
            }

            var token = JsonSerializer.Deserialize<RispostaToken>(await risposta.Content.ReadAsStringAsync(ct));
            _token = token?.AccessToken ?? throw new InvalidOperationException("Token di Google vuoto");
            _scadenza = _tempo.GetUtcNow().AddSeconds(Math.Max(0, token.ExpiresIn - 60));
            _progetto = string.IsNullOrWhiteSpace(c.ProjectId) ? _opzioni.ProgettoId : c.ProjectId.Trim();
            return (_token, _progetto);
        }
        finally
        {
            _blocco.Release();
        }
    }

    /// <summary>Il JWT firmato RS256 dell'account di servizio (un'ora), da scambiare con il token di accesso.</summary>
    public static string Asserzione(string clientEmail, string chiavePem, string tokenUri, DateTimeOffset adesso)
    {
        static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var testata = B64(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var corpo = B64(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = clientEmail, scope = Scopo, aud = tokenUri, iat = adesso.ToUnixTimeSeconds(), exp = adesso.AddHours(1).ToUnixTimeSeconds(),
        }));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(chiavePem);
        var firma = rsa.SignData(Encoding.ASCII.GetBytes($"{testata}.{corpo}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{testata}.{corpo}.{B64(firma)}";
    }
}
