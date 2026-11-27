using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using ProgettoMappe.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>Database SQLite di OpenIddict (app registrate, autorizzazioni, codici e refresh token): è di Mappe, non di 4Grapes.</summary>
public sealed class DatiAccesso(DbContextOptions<DatiAccesso> opzioni) : DbContext(opzioni);

/// <summary>
/// Fornitore di identità Perleuve con OpenID Connect (task #234, giro 2): OpenIddict (server certificato OpenID, libero,
/// mantenuto, integrato in ASP.NET Core) con gli utenti e le password di 4Grapes. Authorization Code + PKCE, refresh
/// token, userinfo, logout. Le pagine (accesso, ingresso) sono in <see cref="Controllers.OidcController"/>.
/// </summary>
public static class OidcPerleuve
{
    public const string SchemaCookie = "Perleuve";
    public const string ScopoPerleuve = "perleuve";

    /// <summary>
    /// Scopo per le API delle app sulle irroratrici (task #259): un'app «browser», senza segreto, chiama
    /// <c>/api/app/…/irroratrici</c> solo con un access token che ha questo scopo.
    /// </summary>
    public const string ScopoIrroratrici = "portale.irroratrici";

    /// <summary>Scopo per i rilievi delle app (task #265): <c>/api/app/prove-irroratrice</c> e <c>/api/app/riassunti-potatura</c>.</summary>
    public const string ScopoRilievi = "portale.rilievi";

    /// <summary>Registra OpenIddict se c'è un database usabile (<c>AccessoUnico:Database</c>); altrimenti OIDC resta spento.</summary>
    public static bool Aggiungi(WebApplicationBuilder builder)
    {
        var opzioni = builder.Configuration.GetSection(AccessoUnicoOptions.Sezione).Get<AccessoUnicoOptions>() ?? new();
        var database = PercorsoDati.Scegli(opzioni.Database);
        if (database is null)
            return false;

        // Chiavi dei cookie dell'accesso accanto al database, così un riavvio non fa uscire tutti.
        var chiaviCookie = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(database))!, "chiavi-accesso");
        try
        {
            Directory.CreateDirectory(chiaviCookie);
            builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(chiaviCookie));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        builder.Services.AddDbContext<DatiAccesso>(o =>
        {
            o.UseSqlite($"Data Source={database}");
            o.UseOpenIddict();
        });

        builder.Services.AddAuthentication()
            .AddCookie(SchemaCookie, o =>
            {
                // Task #403: HttpOnly, SameSite=Lax, solo sull'host dell'accesso (CookieDominio vuoto). Secure: dietro Caddy la
                // richiesta arriva come https (X-Forwarded-Proto dal loopback), quindi il cookie esce Secure; http solo nei test.
                o.Cookie.Name = "perleuve.accesso";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                if (!string.IsNullOrWhiteSpace(opzioni.CookieDominio))
                    o.Cookie.Domain = opzioni.CookieDominio.Trim();
                o.LoginPath = "/perleuve/accesso";
                // Inattività: il cookie scade se non si usa (scorrevole); il tetto assoluto lo controlla OnValidatePrincipal.
                o.ExpireTimeSpan = opzioni.Inattivita();
                o.SlidingExpiration = true;
                o.Events.OnValidatePrincipal = async contesto =>
                {
                    var adesso = contesto.HttpContext.RequestServices.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow;
                    var regole = contesto.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AccessoUnicoOptions>>().Value;
                    if (SessioneAccesso.Valida(contesto.Principal, adesso, regole))
                        return;
                    contesto.RejectPrincipal();
                    await contesto.HttpContext.SignOutAsync(SchemaCookie);
                    await contesto.HttpContext.RequestServices.GetRequiredService<RegistroAccessi>().AnnotaAsync("scaduta",
                        int.TryParse(contesto.Principal?.FindFirst(Claims.Subject)?.Value, out var id) ? id : null,
                        sessione: SessioneAccesso.Sessione(contesto.Principal), ip: contesto.HttpContext.Connection.RemoteIpAddress);
                };
            });
        builder.Services.AddSingleton(sp => new RegistroAccessi(database, sp.GetRequiredService<ILogger<RegistroAccessi>>()));
        builder.Services.AddHttpClient(UscitaUnica.ClientHttp);
        builder.Services.AddSingleton<UscitaUnica>();

        builder.Services.AddOpenIddict()
            .AddCore(o => o.UseEntityFrameworkCore().UseDbContext<DatiAccesso>())
            .AddServer(o =>
            {
                o.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetEndSessionEndpointUris("connect/logout");
                o.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange().AllowRefreshTokenFlow();
                o.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess, ScopoPerleuve, ScopoIrroratrici, ScopoRilievi);
                o.RegisterClaims(Claims.Subject, Claims.Email, Claims.Name, Claims.Role, "ruoli", "abilitazioni", "aziende", "tutte_le_aziende",
                    "abilitazioni_azienda", SessioneAccesso.ClaimSessione, Claims.AuthenticationMethodReference);
                // Le app di Jan leggono l'access token come JWT firmato (come il token semplice), quindi non cifrato.
                o.DisableAccessTokenEncryption();
                // Task #403: codice 5 minuti, access e id token al massimo 60.
                o.SetAuthorizationCodeLifetime(AccessoUnicoOptions.DurataCodice)
                    .SetAccessTokenLifetime(opzioni.DurataOidc()).SetIdentityTokenLifetime(opzioni.DurataOidc());                if (Uri.TryCreate(opzioni.Indirizzo, UriKind.Absolute, out var indirizzo))
                    o.SetIssuer(indirizzo);
                o.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    // L'https lo fa Caddy davanti al Web; l'Api riceve http dal proxy del Web sulla macchina.
                    .DisableTransportSecurityRequirement();
            });

        // Stesse chiavi del token semplice: una sola chiave pubblica da dare alle app.
        builder.Services.AddOptions<OpenIddictServerOptions>().Configure<ChiaviFirma>((o, chiavi) =>
        {
            o.SigningCredentials.Add(new SigningCredentials(chiavi.Chiave, SecurityAlgorithms.RsaSha256));
            o.EncryptionCredentials.Add(new EncryptingCredentials(chiavi.Cifratura, SecurityAlgorithms.RsaOAEP,
                SecurityAlgorithms.Aes256CbcHmacSha512));
        });

        builder.Services.AddHostedService<RegistrazioneApp>();
        return true;
    }

    /// <summary>L'identità che finisce nei token di OpenID Connect: le stesse claim del token semplice.</summary>
    public static ClaimsPrincipal Principal(ProfiloAccessoDto profilo, string clientId, IEnumerable<string> scopi, string? sessione = null)
    {
        var identita = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        // sid = la sessione dell'accesso unico (task #403): l'app la usa per riconoscere l'uscita unica.
        if (!string.IsNullOrEmpty(sessione))
            identita.SetClaim(SessioneAccesso.ClaimSessione, sessione);
        identita.SetClaims(Claims.AuthenticationMethodReference, ["pwd"]);
        identita.SetClaim(Claims.Subject, profilo.IdUtente.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .SetClaim(Claims.Email, profilo.Email)
            .SetClaim(Claims.Name, profilo.Nome)
            .SetClaims(Claims.Role, [.. profilo.Ruoli])
            .SetClaims("ruoli", [.. profilo.Ruoli])
            .SetClaims("abilitazioni", [.. profilo.Abilitazioni]);
        identita.AddClaim(new Claim("aziende", JsonSerializer.Serialize(TokenAccesso.Aziende(profilo)), JsonClaimValueTypes.JsonArray));
        identita.AddClaim(new Claim("abilitazioni_azienda", JsonSerializer.Serialize(TokenAccesso.AbilitazioniAzienda(profilo)),
            JsonClaimValueTypes.JsonArray));
        identita.AddClaim(new Claim("tutte_le_aziende", profilo.TutteLeAziende ? "true" : "false", ClaimValueTypes.Boolean));
        identita.AddClaim(new Claim("permessi_ridotti", profilo.PermessiRidotti ? "true" : "false", ClaimValueTypes.Boolean));
        identita.SetScopes(scopi);
        identita.SetAudiences(clientId);
        identita.SetDestinations(static claim => claim.Type switch
        {
            // Nell'id_token solo chi è; ruoli, abilitazioni e aziende anche lì se l'app chiede lo scopo «perleuve».
            Claims.Name or Claims.Email when claim.Subject!.HasScope(Scopes.Profile) || claim.Subject!.HasScope(Scopes.Email)
                => [Destinations.AccessToken, Destinations.IdentityToken],
            SessioneAccesso.ClaimSessione or Claims.AuthenticationMethodReference => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Role or "ruoli" or "abilitazioni" or "aziende" or "tutte_le_aziende" or "permessi_ridotti" or "abilitazioni_azienda"
                when claim.Subject!.HasScope(ScopoPerleuve) => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken],
        });
        return new ClaimsPrincipal(identita);
    }

    /// <summary>Le risposte di /connect/userinfo: il profilo con i nomi delle claim.</summary>
    public static Dictionary<string, object> InfoUtente(ProfiloAccessoDto profilo) => new()
    {
        [Claims.Subject] = profilo.IdUtente.ToString(System.Globalization.CultureInfo.InvariantCulture),
        [Claims.Email] = profilo.Email,
        [Claims.Name] = profilo.Nome,
        ["ruoli"] = profilo.Ruoli,
        ["abilitazioni"] = profilo.Abilitazioni,
        ["aziende"] = TokenAccesso.Aziende(profilo),
        ["tutte_le_aziende"] = profilo.TutteLeAziende,
        ["abilitazioni_azienda"] = TokenAccesso.AbilitazioniAzienda(profilo),
        ["permessi_ridotti"] = profilo.PermessiRidotti,
    };
}

/// <summary>
/// All'avvio: crea il database SQLite di OpenIddict se manca e registra (o aggiorna) le app di <c>AccessoUnico:App</c> che
/// hanno almeno un <c>RedirectUri</c> (o, con <c>PermettiLocalhost</c>, un <c>RedirectUriProva</c>). Le app «server» usano come
/// client_secret il segreto creato da Mappe. Le app già registrate si riscrivono a ogni avvio (indirizzi compresi: quelli tolti
/// dalla configurazione spariscono).
/// </summary>
public sealed class RegistrazioneApp(IServiceProvider servizi, IOptions<AccessoUnicoOptions> opzioni, SegretiApp segreti,
    ILogger<RegistrazioneApp> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = servizi.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatiAccesso>().Database.EnsureCreatedAsync(ct);
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var (clientId, app) in opzioni.Value.App)
        {
            var descrittore = Descrittore(clientId, app, segreti.Di(clientId), opzioni.Value.PermettiLocalhost);
            var esistente = await manager.FindByClientIdAsync(clientId, ct);
            if (descrittore is null)
            {
                if (esistente is not null)
                    await manager.DeleteAsync(esistente, ct);
                continue;
            }

            if (esistente is null)
                await manager.CreateAsync(descrittore, ct);
            else
                await manager.UpdateAsync(esistente, descrittore, ct);
        }

        var permetti = opzioni.Value.PermettiLocalhost;
        logger.LogInformation("OpenID Connect: app registrate {App} (indirizzi localhost {Localhost})",
            string.Join(", ", opzioni.Value.App.Where(a => Ritorni(a.Value, permetti).Any()).Select(a => a.Key)),
            permetti ? "permessi" : "spenti");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>Null = l'app non usa OpenID Connect (nessun redirect, disabilitata, o «server» senza segreto).</summary>
    public static OpenIddictApplicationDescriptor? Descrittore(string clientId, AppEsternaOptions app, string? segreto,
        bool permettiLocalhost = false)
    {
        var browser = string.Equals(app.Tipo, "browser", StringComparison.OrdinalIgnoreCase);
        var ritorni = Ritorni(app, permettiLocalhost).ToList();
        if (!app.Abilitata || ritorni.Count == 0 || (!browser && string.IsNullOrEmpty(segreto)))
            return null;

        var descrittore = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = browser ? ClientTypes.Public : ClientTypes.Confidential,
            ClientSecret = browser ? null : segreto,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = string.IsNullOrWhiteSpace(app.Nome) ? clientId : app.Nome,
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken, Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email, Permissions.Scopes.Profile, Permissions.Scopes.Roles,
                Permissions.Prefixes.Scope + OidcPerleuve.ScopoPerleuve,
                Permissions.Prefixes.Scope + OidcPerleuve.ScopoIrroratrici,
                Permissions.Prefixes.Scope + OidcPerleuve.ScopoRilievi,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };
        foreach (var uri in ritorni)
            descrittore.RedirectUris.Add(uri);
        foreach (var uri in Indirizzi(app.PostLogoutRedirectUri, permettiLocalhost ? app.PostLogoutRedirectUriProva : []))
            descrittore.PostLogoutRedirectUris.Add(uri);
        return descrittore;
    }

    /// <summary>Gli indirizzi di ritorno da registrare: quelli di produzione più, se permesso, quelli di prova su localhost.</summary>
    public static IEnumerable<Uri> Ritorni(AppEsternaOptions app, bool permettiLocalhost) =>
        Indirizzi(app.RedirectUri, permettiLocalhost ? app.RedirectUriProva : []);

    /// <summary>Produzione com'è; di prova solo indirizzi assoluti su localhost/127.0.0.1 (gli altri si scartano).</summary>
    private static IEnumerable<Uri> Indirizzi(IEnumerable<string> produzione, IEnumerable<string> prova) =>
        produzione.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => new Uri(u.Trim()))
            .Concat(prova.Select(u => Uri.TryCreate(u?.Trim(), UriKind.Absolute, out var p) && p.IsLoopback ? p : null)
                .OfType<Uri>())
            .Distinct();
}
