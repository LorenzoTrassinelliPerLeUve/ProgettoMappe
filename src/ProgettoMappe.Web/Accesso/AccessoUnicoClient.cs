using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Web.Portale;

namespace ProgettoMappe.Web.Accesso;

/// <summary>Sezione <c>Accesso:AccessoUnico</c> del Web (task #403).</summary>
public sealed class AccessoUnicoClientOptions
{
    /// <summary>Acceso = «Entra» porta all'accesso unico (OpenID Connect, codice + PKCE); spento = il login di Mappe di prima.</summary>
    public bool Abilitato { get; set; }

    /// <summary>
    /// Indirizzo dell'accesso unico (domani https://accesso.4grapes.it). Vuoto = lo stesso sito della pagina di Mappe: l'Api
    /// è inoltrata dal Web sotto lo stesso dominio, quindi funziona con ogni indirizzo di Mappe (sslip, 4grapes.it).
    /// </summary>
    public string? Autorita { get; set; }

    /// <summary>client_id di Mappe nell'accesso unico (<c>AccessoUnico:App:mappe</c> dell'Api).</summary>
    public string ClientId { get; set; } = "mappe";

    /// <summary>
    /// Il file dei segreti delle app scritto dall'Api (<c>AccessoUnico:FileSegreti</c>, più percorsi con «;», il primo che
    /// esiste): Mappe ci legge il suo client_secret a ogni accesso. Mai nel repository.
    /// </summary>
    public string? FileSegreti { get; set; }

    /// <summary>
    /// Le chiamate fra server (discovery, chiavi, token) vanno direttamente all'Api (<c>Api:BaseUrl</c>) con gli stessi
    /// X-Forwarded-Proto/Host del browser, così l'issuer è lo stesso. Spento = si passa dall'indirizzo pubblico.
    /// </summary>
    public bool InoltraAllApi { get; set; } = true;
}

/// <summary>
/// Mappe client OpenID Connect dell'accesso unico Perleuve (task #403). Il browser va a <c>/connect/authorize</c>: se la
/// sessione dell'accesso unico c'è già torna subito con il codice (nessuna domanda), altrimenti passa dalla pagina di
/// accesso. Il cookie di Mappe resta quello di prima (<c>mappe.accesso</c>, stesse claim: Id 4Grapes per il Portale) più
/// <c>sid</c>, che serve all'uscita unica: canale browser <c>/accesso/uscita-frontale</c> e canale server
/// <c>/accesso/uscita-retro</c> chiudono la sessione con quel sid (<see cref="SessioniChiuse"/>).
/// </summary>
public static class AccessoUnicoClient
{
    public const string Schema = "perleuve";
    public const string ClaimSessione = "sid";
    public const string PercorsoRitorno = "/accesso/ritorno";
    public const string PercorsoUscito = "/accesso/uscito";

    public static void Aggiungi(AuthenticationBuilder auth, IServiceCollection servizi, IConfiguration configurazione, AccessoOptions accesso)
    {
        var opzioni = accesso.AccessoUnico;
        servizi.AddHttpContextAccessor();
        servizi.AddSingleton<SessioniChiuse>();
        servizi.TryAddGestoreApi();
        servizi.AddSingleton(sp => new ConfigurazioniPerSito(opzioni, configurazione["Api:BaseUrl"],
            sp.GetRequiredService<IHttpContextAccessor>(), sp.GetRequiredService<GestoreApi>()));

        auth.AddOpenIdConnect(Schema, o =>
        {
            o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.SignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.ClientId = opzioni.ClientId;
            o.ResponseType = OpenIdConnectResponseType.Code;
            o.UsePkce = true;
            // Il codice torna nell'indirizzo (GET), non in un modulo POST: va con i cookie Lax e senza JavaScript.
            o.ResponseMode = OpenIdConnectResponseMode.Query;
            o.Scope.Clear();
            foreach (var scopo in new[] { "openid", "profile", "email", "perleuve" })
                o.Scope.Add(scopo);
            o.CallbackPath = PercorsoRitorno;
            o.SignedOutCallbackPath = PercorsoUscito;
            o.SignedOutRedirectUri = "/perleuve/accesso?uscito=1";
            o.RemoteSignOutPath = "/accesso/uscita-oidc-non-usata";
            o.MapInboundClaims = false;
            o.GetClaimsFromUserInfoEndpoint = false;
            o.SaveTokens = false;
            o.TokenValidationParameters.NameClaimType = "name";
            // Stesso sito dell'accesso: Lax basta (il ritorno è una navigazione); niente SameSite=None.
            o.CorrelationCookie.SameSite = SameSiteMode.Lax;
            o.NonceCookie.SameSite = SameSiteMode.Lax;
            o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            o.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Events.OnAuthorizationCodeReceived = contesto =>
            {
                if (Segreto(opzioni) is { } segreto)
                    contesto.TokenEndpointRequest!.ClientSecret = segreto;
                return Task.CompletedTask;
            };
            o.Events.OnTokenValidated = contesto => ApriSessione(contesto, accesso);
            o.Events.OnRemoteFailure = contesto =>
            {
                var negato = contesto.Failure?.Message.Contains("access_denied", StringComparison.Ordinal) == true;
                contesto.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AccessoUnicoClient))
                    .LogWarning("Accesso unico non riuscito: {Errore}", contesto.Failure?.Message);
                contesto.Response.Redirect(negato ? "/accesso/negato" : "/perleuve/accesso");
                contesto.HandleResponse();
                return Task.CompletedTask;
            };
        });
        servizi.AddOptions<OpenIdConnectOptions>(Schema).Configure<ConfigurazioniPerSito>((o, c) =>
        {
            o.ConfigurationManager = c;
            o.Backchannel = c.Client;
        });
    }

    /// <summary>Dal token dell'accesso unico al cookie di Mappe: le stesse claim del login di prima, più sid.</summary>
    private static Task ApriSessione(TokenValidatedContext contesto, AccessoOptions accesso)
    {
        var token = contesto.Principal!;
        if (!int.TryParse(token.FindFirstValue("sub"), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture,
                out var id) || id <= 0)
        {
            contesto.Fail("sub non valido");
            return Task.CompletedTask;
        }

        var claim = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.FindFirstValue("email") ?? id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, token.FindFirstValue("name") ?? $"Utente {id}"),
            new(UtentePortale.ClaimIdUtente, id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        if (token.FindFirstValue(ClaimSessione) is { Length: > 0 } sid)
            claim.Add(new Claim(ClaimSessione, sid));
        contesto.Principal = new ClaimsPrincipal(new ClaimsIdentity(claim, CookieAuthenticationDefaults.AuthenticationScheme));
        contesto.Properties!.IsPersistent = false;
        contesto.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(accesso.DurataSessione());
        // Solo l'id_token (serve come id_token_hint all'uscita): il cookie resta piccolo.
        var idToken = contesto.TokenEndpointResponse?.IdToken ?? contesto.ProtocolMessage?.IdToken;
        if (!string.IsNullOrEmpty(idToken))
            contesto.Properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = idToken }]);
        return Task.CompletedTask;
    }

    /// <summary>Il client_secret di Mappe dal file dei segreti dell'Api; null = nessun file leggibile (l'Api risponderà invalid_client).</summary>
    public static string? Segreto(AccessoUnicoClientOptions opzioni)
    {
        foreach (var percorso in (opzioni.FileSegreti ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                if (!File.Exists(percorso))
                    continue;
                var segreti = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(percorso));
                return segreti?.FirstOrDefault(s => string.Equals(s.Key, opzioni.ClientId, StringComparison.OrdinalIgnoreCase)).Value;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
            }
        }
        return null;
    }

    /// <summary>
    /// Canale server dell'uscita unica: valida il logout_token (firma con le chiavi dell'accesso unico, issuer, audience =
    /// Mappe, evento di logout, sid) e restituisce il sid da chiudere; null = token non valido.
    /// </summary>
    public static async Task<string?> SidDaLogoutTokenAsync(string? logoutToken, OpenIdConnectConfiguration configurazione, string clientId)
    {
        if (string.IsNullOrEmpty(logoutToken))
            return null;
        var esito = await new JsonWebTokenHandler().ValidateTokenAsync(logoutToken, new TokenValidationParameters
        {
            ValidIssuer = configurazione.Issuer,
            ValidAudience = clientId,
            IssuerSigningKeys = configurazione.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        });
        if (!esito.IsValid || esito.SecurityToken is not JsonWebToken jwt || jwt.TryGetPayloadValue<object>("nonce", out _)
            || !jwt.TryGetPayloadValue<JsonElement>("events", out var eventi)
            || eventi.ValueKind != JsonValueKind.Object || !eventi.TryGetProperty("http://schemas.openid.net/event/backchannel-logout", out _))
            return null;
        return jwt.TryGetPayloadValue<string>("sid", out var sid) && !string.IsNullOrEmpty(sid) ? sid : null;
    }

    /// <summary>Le rotte dell'accesso unico nel Web: entra, esci, uscita (canale browser e server), accesso negato.</summary>
    public static void Mappa(WebApplication app)
    {
        app.MapGet("/accesso/entra", (string? returnUrl, Microsoft.Extensions.Options.IOptions<AccessoOptions> accesso) =>
            Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = UrlRitorno.Sicuro(string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/" ? accesso.Value.PaginaIniziale : returnUrl),
            }, [Schema])).AllowAnonymous();

        app.MapGet("/accesso/negato", () => Results.Content(
            "<!doctype html><html lang=\"it\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>Mappe</title>"
            + "<body style=\"font-family:system-ui;max-width:30rem;margin:3rem auto;padding:0 1rem\"><h1>Mappe non è abilitata</h1>"
            + "<p>La tua utenza 4Grapes non può usare Mappe. Se pensi sia un errore, scrivi all'assistenza.</p>"
            + "<p><a href=\"/perleuve/servizi\">I tuoi servizi</a></p></body></html>", "text/html; charset=utf-8")).AllowAnonymous();

        // Canale browser: aperto in un riquadro nascosto dalla pagina «Sei uscito» dell'accesso unico (stesso sito).
        app.MapGet("/accesso/uscita-frontale", async (string? sid, HttpContext contesto, SessioniChiuse chiuse) =>
        {
            contesto.Response.Headers.CacheControl = "no-store";
            if (!string.IsNullOrEmpty(sid))
            {
                chiuse.Chiudi(sid);
                if (contesto.User.FindFirstValue(ClaimSessione) == sid)
                    await contesto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            return Results.Ok();
        }).AllowAnonymous();

        // Canale server: POST del logout_token firmato dall'accesso unico.
        app.MapPost("/accesso/uscita-retro", async (HttpContext contesto, ConfigurazioniPerSito configurazioni, SessioniChiuse chiuse,
            Microsoft.Extensions.Options.IOptions<AccessoOptions> accesso, ILoggerFactory log) =>
        {
            contesto.Response.Headers.CacheControl = "no-store";
            var form = contesto.Request.HasFormContentType ? await contesto.Request.ReadFormAsync() : null;
            var configurazione = await configurazioni.GetConfigurationAsync(contesto.RequestAborted);
            var sid = await SidDaLogoutTokenAsync(form?["logout_token"], configurazione, accesso.Value.AccessoUnico.ClientId);
            if (sid is null)
            {
                log.CreateLogger(typeof(AccessoUnicoClient)).LogWarning("Uscita unica: logout_token non valido");
                return Results.BadRequest();
            }
            chiuse.Chiudi(sid);
            return Results.Ok();
        }).AllowAnonymous().DisableAntiforgery();
    }

    /// <summary>Per il cookie di Mappe: una sessione chiusa dall'uscita unica non vale più (anche in un'altra scheda).</summary>
    public static async Task ControllaSessione(CookieValidatePrincipalContext contesto)
    {
        if (contesto.Principal?.FindFirstValue(ClaimSessione) is { } sid
            && contesto.HttpContext.RequestServices.GetService<SessioniChiuse>()?.Chiusa(sid) == true)
        {
            contesto.RejectPrincipal();
            await contesto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

/// <summary>I sid chiusi dall'uscita unica nelle ultime 24 ore (in memoria: oltre, i cookie di Mappe sono comunque scaduti).</summary>
public sealed class SessioniChiuse(TimeProvider orologio)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _chiuse = new(StringComparer.Ordinal);

    public void Chiudi(string sid)
    {
        var adesso = orologio.GetUtcNow();
        _chiuse[sid] = adesso;
        foreach (var vecchia in _chiuse.Where(c => adesso - c.Value > TimeSpan.FromHours(24)).Select(c => c.Key).ToList())
            _chiuse.TryRemove(vecchia, out _);
    }

    public bool Chiusa(string sid) => _chiuse.ContainsKey(sid);
}

/// <summary>Il gestore HTTP verso l'Api (nei test: quello del server di prova dell'Api).</summary>
public sealed class GestoreApi(HttpMessageHandler gestore)
{
    public HttpMessageHandler Gestore { get; } = gestore;
}

internal static class EstensioniGestoreApi
{
    public static void TryAddGestoreApi(this IServiceCollection servizi)
    {
        if (servizi.All(s => s.ServiceType != typeof(GestoreApi)))
            servizi.AddSingleton(new GestoreApi(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }));
    }
}

/// <summary>
/// La configurazione OpenID Connect (discovery e chiavi) dell'accesso unico per il sito della richiesta: con
/// <see cref="AccessoUnicoClientOptions.Autorita"/> vuota l'accesso unico è sullo stesso dominio della pagina, quindi
/// ogni indirizzo di Mappe ha la sua (in cache, rinnovata dalla libreria). Le chiamate fra server vanno all'Api.
/// </summary>
public sealed class ConfigurazioniPerSito : IConfigurationManager<OpenIdConnectConfiguration>
{
    private readonly AccessoUnicoClientOptions _opzioni;
    private readonly IHttpContextAccessor _contesto;
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _siti = new(StringComparer.OrdinalIgnoreCase);
    private readonly HttpDocumentRetriever _documenti;

    public ConfigurazioniPerSito(AccessoUnicoClientOptions opzioni, string? api, IHttpContextAccessor contesto, GestoreApi gestore)
    {
        _opzioni = opzioni;
        _contesto = contesto;
        Client = new HttpClient(opzioni.InoltraAllApi && Uri.TryCreate(api, UriKind.Absolute, out var a)
            ? new InoltroAllApi(a) { InnerHandler = gestore.Gestore }
            : gestore.Gestore, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(15) };
        _documenti = new HttpDocumentRetriever(Client) { RequireHttps = false };
    }

    /// <summary>Il client per discovery, chiavi e token.</summary>
    public HttpClient Client { get; }

    /// <summary>L'indirizzo dell'accesso unico per questa richiesta (senza barra finale).</summary>
    public string Autorita()
    {
        if (Uri.TryCreate(_opzioni.Autorita?.Trim(), UriKind.Absolute, out var fissa))
            return fissa.GetLeftPart(UriPartial.Path).TrimEnd('/');
        var richiesta = _contesto.HttpContext?.Request ?? throw new InvalidOperationException("Accesso unico: nessuna richiesta in corso");
        return $"{richiesta.Scheme}://{richiesta.Host}{richiesta.PathBase}".TrimEnd('/');
    }

    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) =>
        _siti.GetOrAdd(Autorita(), a => new ConfigurationManager<OpenIdConnectConfiguration>(a + "/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(), _documenti)).GetConfigurationAsync(cancel);

    public void RequestRefresh()
    {
        if (_contesto.HttpContext is not null && _siti.TryGetValue(Autorita(), out var c))
            c.RequestRefresh();
    }
}

/// <summary>
/// Manda all'Api (sulla macchina) le richieste fra server per l'accesso unico, con lo schema e l'host pubblici in
/// X-Forwarded-Proto/Host (l'Api li accetta solo dal loopback): l'issuer è lo stesso che vede il browser.
/// </summary>
public sealed class InoltroAllApi(Uri api) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage richiesta, CancellationToken ct)
    {
        if (richiesta.RequestUri is { IsAbsoluteUri: true } pubblico)
        {
            richiesta.Headers.Remove("X-Forwarded-Proto");
            richiesta.Headers.Remove("X-Forwarded-Host");
            richiesta.Headers.TryAddWithoutValidation("X-Forwarded-Proto", pubblico.Scheme);
            richiesta.Headers.TryAddWithoutValidation("X-Forwarded-Host", pubblico.Authority);
            richiesta.RequestUri = new Uri(api, pubblico.PathAndQuery);
        }
        return base.SendAsync(richiesta, ct);
    }
}
