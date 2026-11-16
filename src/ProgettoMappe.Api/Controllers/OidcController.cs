using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Fornitore di identità Perleuve (OpenID Connect, task #234 giro 2): endpoint <c>/connect/*</c> (OpenIddict in
/// passthrough), pagina di accesso <c>/perleuve/accesso</c> e «Ingresso Perleuve» <c>/perleuve/ingresso</c> con le app a
/// cui la persona è abilitata. Il pubblico arriva dal proxy del Web (stesso dominio di Mappe). Registrato solo con
/// <c>AccessoUnico:Database</c> usabile (vedi <see cref="OidcPerleuve.Aggiungi"/>).
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class OidcController : Controller
{
    private readonly ServizioProfilo _profili;
    private readonly LimitiAccessoUnico _limiti;
    private readonly AccessoUnicoOptions _opzioni;
    private readonly ILogger<OidcController> _logger;

    public OidcController(ServizioProfilo profili, LimitiAccessoUnico limiti, IOptions<AccessoUnicoOptions> opzioni, ILogger<OidcController> logger)
    {
        _profili = profili;
        _limiti = limiti;
        _opzioni = opzioni.Value;
        _logger = logger;
    }

    [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var richiesta = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Richiesta OpenID Connect mancante");
        var accesso = await HttpContext.AuthenticateAsync(OidcPerleuve.SchemaCookie);
        var profilo = accesso.Succeeded && IdUtente(accesso.Principal) is { } id ? await ProfiloAsync(id, ct) : null;
        if (profilo is null)
        {
            if (richiesta.HasPromptValue(PromptValues.None))
                return Rifiuta(Errors.LoginRequired, "Accesso richiesto.");
            await HttpContext.SignOutAsync(OidcPerleuve.SchemaCookie);
            var qui = Request.PathBase + Request.Path + QueryString.Create(
                Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());
            return Redirect("/perleuve/accesso?ritorno=" + Uri.EscapeDataString(qui));
        }

        if (!_opzioni.App.TryGetValue(richiesta.ClientId ?? "", out var app) || ServizioProfilo.NonAbilitato(profilo, app))
        {
            _logger.LogInformation("OpenID Connect: utente {IdUtente} non abilitato all'app {App}", profilo.IdUtente, richiesta.ClientId);
            return Rifiuta(Errors.AccessDenied, "La tua utenza 4Grapes non è abilitata a questa app.");
        }

        return SignIn(OidcPerleuve.Principal(profilo, richiesta.ClientId!, richiesta.GetScopes()),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>Codice → token e rinnovo con refresh token: il profilo si rilegge ogni volta da 4Grapes.</summary>
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Token(CancellationToken ct)
    {
        var richiesta = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("Richiesta OpenID Connect mancante");
        if (!richiesta.IsAuthorizationCodeGrantType() && !richiesta.IsRefreshTokenGrantType())
            return Rifiuta(Errors.UnsupportedGrantType, "Tipo di richiesta non previsto.");

        var esito = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var profilo = IdUtente(esito.Principal) is { } id ? await ProfiloAsync(id, ct) : null;
        if (profilo is null)
            return Rifiuta(Errors.InvalidGrant, "L'utenza non è più attiva in 4Grapes.");
        if (!_opzioni.App.TryGetValue(richiesta.ClientId ?? "", out var app) || ServizioProfilo.NonAbilitato(profilo, app))
            return Rifiuta(Errors.InvalidGrant, "La tua utenza 4Grapes non è più abilitata a questa app.");

        return SignIn(OidcPerleuve.Principal(profilo, richiesta.ClientId!, esito.Principal!.GetScopes()),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> UserInfo(CancellationToken ct)
    {
        var esito = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var profilo = IdUtente(esito.Principal) is { } id ? await ProfiloAsync(id, ct) : null;
        if (profilo is null)
            return Challenge(new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "L'utenza non è più attiva in 4Grapes.",
            }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return Ok(OidcPerleuve.InfoUtente(profilo));
    }

    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(OidcPerleuve.SchemaCookie);
        return SignOut(new AuthenticationProperties { RedirectUri = "/perleuve/accesso?uscito=1" },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/perleuve/accesso")]
    public IActionResult Accesso(string? ritorno, bool uscito = false) =>
        Pagina("Accesso Perleuve", ModuloAccesso(RitornoSicuro(ritorno), uscito ? "Sei uscito." : null, null));

    [HttpPost("~/perleuve/accesso")]
    public async Task<IActionResult> AccessoInvio([FromForm] string? email, [FromForm] string? password, [FromForm] string? ritorno,
        CancellationToken ct)
    {
        var dove = RitornoSicuro(ritorno);
        email = email?.Trim() ?? "";
        password ??= "";
        if (email.Length is 0 or > 50 || password.Length is 0 or > 256)
            return Pagina("Accesso Perleuve", ModuloAccesso(dove, null, "Scrivi email e password."), StatusCodes.Status401Unauthorized);
        if (_limiti.ErroriEmail.Conteggio(email) >= _opzioni.MaxErrori)
            return Pagina("Accesso Perleuve", ModuloAccesso(dove, null, "Troppi tentativi sbagliati: riprova fra 15 minuti."),
                StatusCodes.Status429TooManyRequests);

        try
        {
            var id = await _profili.VerificaAsync(email, password, ct);
            var profilo = id is { } i ? await _profili.ProfiloAsync(i, ct) : null;
            if (profilo is null)
            {
                _limiti.ErroriEmail.Conta(email);
                return Pagina("Accesso Perleuve", ModuloAccesso(dove, null, "Email o password sbagliate. La password è quella di 4Grapes."),
                    StatusCodes.Status401Unauthorized);
            }

            _limiti.ErroriEmail.Azzera(email);
            var identita = new ClaimsIdentity(OidcPerleuve.SchemaCookie, Claims.Name, Claims.Role);
            identita.AddClaim(new Claim(Claims.Subject, profilo.IdUtente.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            identita.AddClaim(new Claim(Claims.Name, profilo.Nome));
            await HttpContext.SignInAsync(OidcPerleuve.SchemaCookie, new ClaimsPrincipal(identita));
            return Redirect(dove);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Accesso Perleuve: lettura da 4Grapes non riuscita (errore SQL {Numero})", ex.Number);
            return Pagina("Accesso Perleuve", ModuloAccesso(dove, null, "4Grapes non risponde: riprova fra poco."),
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>«Ingresso Perleuve»: chi sei e le app a cui sei abilitato, con i collegamenti.</summary>
    [HttpGet("~/perleuve/ingresso")]
    public async Task<IActionResult> Ingresso(CancellationToken ct)
    {
        var accesso = await HttpContext.AuthenticateAsync(OidcPerleuve.SchemaCookie);
        var profilo = accesso.Succeeded && IdUtente(accesso.Principal) is { } id ? await ProfiloAsync(id, ct) : null;
        if (profilo is null)
            return Redirect("/perleuve/accesso?ritorno=" + Uri.EscapeDataString("/perleuve/ingresso"));

        var h = HtmlEncoder.Default;
        var corpo = new StringBuilder();
        corpo.Append($"<p class=\"saluto\">Ciao {h.Encode(profilo.Nome)}</p><p class=\"piccolo\">{h.Encode(profilo.Email)}</p>");
        corpo.Append("<ul class=\"app\"><li><a href=\"/\">Mappe<span>Mappa dei vigneti e report</span></a></li>");
        foreach (var (nome, app) in AppDi(profilo))
            corpo.Append($"<li><a href=\"{h.Encode(app.Indirizzo!)}\">{h.Encode(string.IsNullOrWhiteSpace(app.Nome) ? nome : app.Nome)}</a></li>");
        corpo.Append("</ul><form method=\"post\" action=\"/perleuve/esci\"><button class=\"secondario\">Esci</button></form>");
        return Pagina("Ingresso Perleuve", corpo.ToString());
    }

    [HttpPost("~/perleuve/esci")]
    public async Task<IActionResult> Esci()
    {
        await HttpContext.SignOutAsync(OidcPerleuve.SchemaCookie);
        return Redirect("/perleuve/accesso?uscito=1");
    }

    /// <summary>Le app con un indirizzo, accese, a cui l'utente è abilitato (in ordine di nome).</summary>
    public IEnumerable<(string Nome, AppEsternaOptions App)> AppDi(ProfiloAccessoDto profilo) =>
        _opzioni.App
            .Where(a => a.Value.Abilitata && !string.IsNullOrWhiteSpace(a.Value.Indirizzo) && !ServizioProfilo.NonAbilitato(profilo, a.Value))
            .OrderBy(a => a.Value.Nome ?? a.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => (a.Key, a.Value));

    /// <summary>Solo percorsi di questo sito: niente rimandi verso altri domini.</summary>
    public static string RitornoSicuro(string? ritorno) =>
        ritorno is { Length: > 1 } r && r[0] == '/' && r[1] is not '/' and not '\\' && !r.Contains('\r') && !r.Contains('\n')
            ? r
            : "/perleuve/ingresso";

    private async Task<ProfiloAccessoDto?> ProfiloAsync(int id, CancellationToken ct)
    {
        try
        {
            return await _profili.ProfiloAsync(id, ct);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Accesso Perleuve: lettura da 4Grapes non riuscita (errore SQL {Numero})", ex.Number);
            return null;
        }
    }

    private static int? IdUtente(ClaimsPrincipal? principal) =>
        int.TryParse(principal?.FindFirst(Claims.Subject)?.Value, out var id) ? id : null;

    private IActionResult Rifiuta(string errore, string descrizione) =>
        Forbid(new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = errore,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = descrizione,
        }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    private string ModuloAccesso(string ritorno, string? avviso, string? errore)
    {
        var h = HtmlEncoder.Default;
        var testo = new StringBuilder();
        if (avviso is not null) testo.Append($"<p class=\"avviso\">{h.Encode(avviso)}</p>");
        if (errore is not null) testo.Append($"<p class=\"errore\" role=\"alert\">{h.Encode(errore)}</p>");
        testo.Append($"""
            <form method="post" action="/perleuve/accesso">
              <input type="hidden" name="ritorno" value="{h.Encode(ritorno)}">
              <label>Email<input name="email" type="email" autocomplete="username" required autofocus></label>
              <label>Password<input name="password" type="password" autocomplete="current-password" required></label>
              <button>Entra</button>
            </form>
            <p class="piccolo">Un solo accesso per tutte le app Perleuve: email e password sono quelle di 4Grapes.</p>
            """);
        if (_opzioni.RecuperoAttivo())
            testo.Append("<p class=\"piccolo\"><a href=\"/perleuve/password-dimenticata\">Password dimenticata?</a></p>");
        else if (!string.IsNullOrWhiteSpace(_opzioni.PasswordDimenticata))
            testo.Append($"<p class=\"piccolo\"><a href=\"{h.Encode(_opzioni.PasswordDimenticata)}\">Password dimenticata?</a> Si recupera dal portale 4Grapes.</p>");
        return testo.ToString();
    }

    private ContentResult Pagina(string titolo, string corpo, int stato = StatusCodes.Status200OK) => Pagina(Response, titolo, corpo, stato);

    /// <summary>Le pagine Perleuve (accesso, ingresso, password dimenticata): HTML semplice, senza script, mai in cache né in un frame.</summary>
    public static ContentResult Pagina(HttpResponse response, string titolo, string corpo, int stato = StatusCodes.Status200OK)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers["X-Frame-Options"] = "DENY";
        return new ContentResult
        {
            StatusCode = stato,
            ContentType = "text/html; charset=utf-8",
            Content = $$"""
                <!doctype html><html lang="it"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                <title>{{HtmlEncoder.Default.Encode(titolo)}}</title><style>
                body{margin:0;font-family:"Source Sans 3",system-ui,sans-serif;background:#f4f6f2;color:#3c3950;display:flex;min-height:100vh;align-items:center;justify-content:center}
                main{background:#fff;border-radius:12px;box-shadow:0 2px 12px #0002;padding:2rem;width:min(26rem,92vw)}
                h1{font-family:Montserrat,system-ui,sans-serif;font-size:1.4rem;margin:0 0 1rem;border-bottom:4px solid #8dc63e;padding-bottom:.5rem}
                label{display:block;margin:.8rem 0;font-weight:600}input{display:block;width:100%;box-sizing:border-box;margin-top:.3rem;padding:.7rem;font-size:1rem;border:1px solid #5f727f;border-radius:6px}
                button{margin-top:1rem;width:100%;min-height:44px;font-size:1rem;background:#8dc63e;color:#fff;border:0;border-radius:6px;font-weight:700;cursor:pointer}
                button.secondario{background:#5f727f}.piccolo{font-size:.9rem;color:#5f727f}.errore{color:#b3261e;font-weight:600}.avviso{color:#3c3950}
                .app{list-style:none;padding:0}.app a{display:block;padding:.9rem 1rem;margin:.5rem 0;border:1px solid #8dc63e;border-radius:8px;color:#3c3950;text-decoration:none;font-weight:700}
                .app a span{display:block;font-weight:400;font-size:.9rem;color:#5f727f}.saluto{font-size:1.1rem;font-weight:700;margin:0}
                </style></head><body><main><h1>{{HtmlEncoder.Default.Encode(titolo)}}</h1>{{corpo}}</main></body></html>
                """,
        };
    }
}
