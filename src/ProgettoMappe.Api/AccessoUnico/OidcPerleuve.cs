using System.Security.Claims;
using System.Text.Json;
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
                o.Cookie.Name = "perleuve.accesso";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.LoginPath = "/perleuve/accesso";
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.SlidingExpiration = true;
            });

        builder.Services.AddOpenIddict()
            .AddCore(o => o.UseEntityFrameworkCore().UseDbContext<DatiAccesso>())
            .AddServer(o =>
            {
                o.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetEndSessionEndpointUris("connect/logout");
                o.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange().AllowRefreshTokenFlow();
                o.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess, ScopoPerleuve, ScopoIrroratrici);
                o.RegisterClaims(Claims.Subject, Claims.Email, Claims.Name, Claims.Role, "ruoli", "abilitazioni", "aziende", "tutte_le_aziende", "abilitazioni_azienda");
                // Le app di Jan leggono l'access token come JWT firmato (come il token semplice), quindi non cifrato.
                o.DisableAccessTokenEncryption();
                o.SetAccessTokenLifetime(opzioni.Durata()).SetIdentityTokenLifetime(opzioni.Durata());
                if (Uri.TryCreate(opzioni.Indirizzo, UriKind.Absolute, out var indirizzo))
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
    public static ClaimsPrincipal Principal(ProfiloAccessoDto profilo, string clientId, IEnumerable<string> scopi)
    {
        var identita = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
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
/// hanno almeno un <c>RedirectUri</c>. Le app «server» usano come client_secret il segreto creato da Mappe.
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
            var descrittore = Descrittore(clientId, app, segreti.Di(clientId));
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

        logger.LogInformation("OpenID Connect: app registrate {App}",
            string.Join(", ", opzioni.Value.App.Where(a => a.Value.RedirectUri.Count > 0).Select(a => a.Key)));
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>Null = l'app non usa OpenID Connect (nessun redirect, disabilitata, o «server» senza segreto).</summary>
    public static OpenIddictApplicationDescriptor? Descrittore(string clientId, AppEsternaOptions app, string? segreto)
    {
        var browser = string.Equals(app.Tipo, "browser", StringComparison.OrdinalIgnoreCase);
        if (!app.Abilitata || app.RedirectUri.Count == 0 || (!browser && string.IsNullOrEmpty(segreto)))
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
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };
        foreach (var uri in app.RedirectUri)
            descrittore.RedirectUris.Add(new Uri(uri));
        foreach (var uri in app.PostLogoutRedirectUri)
            descrittore.PostLogoutRedirectUris.Add(new Uri(uri));
        return descrittore;
    }
}
