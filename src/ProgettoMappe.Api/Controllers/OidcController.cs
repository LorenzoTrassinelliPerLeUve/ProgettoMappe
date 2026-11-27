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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using ProgettoMappe.Contracts;
using ProgettoMappe.Contracts.Accesso;
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
    public IActionResult Accesso(string? ritorno, bool uscito = false, string? lingua = null)
    {
        var dove = RitornoSicuro(ritorno);
        var l = LinguaPagina(lingua, dove);
        return PaginaAccesso(dove, l, uscito ? TestiAccesso.T(l, "uscito") : null, null, null);
    }

    [HttpPost("~/perleuve/accesso")]
    public async Task<IActionResult> AccessoInvio([FromForm] string? email, [FromForm] string? password, [FromForm] string? ritorno,
        [FromForm] string? lingua, CancellationToken ct)
    {
        var dove = RitornoSicuro(ritorno);
        var l = LinguaPagina(lingua, dove);
        email = email?.Trim() ?? "";
        password ??= "";
        if (email.Length is 0 or > 50 || password.Length is 0 or > 256)
            return PaginaAccesso(dove, l, null, "errore-vuoti", email, StatusCodes.Status401Unauthorized);
        if (_limiti.ErroriEmail.Conteggio(email) >= _opzioni.MaxErrori)
            return PaginaAccesso(dove, l, null, "bloccato", email, StatusCodes.Status429TooManyRequests);

        try
        {
            var id = await _profili.VerificaAsync(email, password, ct);
            var profilo = id is { } i ? await _profili.ProfiloAsync(i, ct) : null;
            if (profilo is null)
            {
                // Stesso messaggio per email inesistente e password sbagliata: non dice quali email esistono.
                _limiti.ErroriEmail.Conta(email);
                return PaginaAccesso(dove, l, null, "errore-credenziali", email, StatusCodes.Status401Unauthorized);
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
            return PaginaAccesso(dove, l, null, "non-disponibile", email, StatusCodes.Status503ServiceUnavailable);
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

    /// <summary>
    /// La lingua della pagina: quella scelta, altrimenti <c>ui_locales</c> della richiesta dell'app (OpenID Connect),
    /// altrimenti quella del browser (<see cref="TestiAccesso.Lingua"/>).
    /// </summary>
    private string LinguaPagina(string? scelta, string ritorno) =>
        TestiAccesso.Lingua(TestiAccesso.Normalizza(scelta) ?? UiLocales(ritorno), Request.Headers.AcceptLanguage.ToString());

    private static string? UiLocales(string ritorno) =>
        ParametriAuthorize(ritorno) is { } q && q.TryGetValue(Parameters.UiLocales, out var v)
            ? v.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(TestiAccesso.Normalizza).FirstOrDefault(x => x is not null)
            : null;

    /// <summary>I parametri della richiesta OpenID Connect, se il ritorno è <c>/connect/authorize?…</c>.</summary>
    private static Dictionary<string, StringValues>? ParametriAuthorize(string ritorno)
    {
        var i = ritorno.IndexOf('?');
        return i > 0 && ritorno[..i] == "/connect/authorize" ? QueryHelpers.ParseQuery(ritorno[i..]) : null;
    }

    /// <summary>
    /// «Stai entrando in» (task #400): l'app che chiede l'accesso (client_id, solo se registrata) e dove si torna (l'host del
    /// redirect_uri, solo se è fra quelli registrati dell'app: un indirizzo inventato non si mostra mai). Senza richiesta di
    /// un'app si entra nell'Ingresso Perleuve; la richiesta di un'app sconosciuta non mostra niente.
    /// </summary>
    public (string Nome, string? Torna)? Destinazione(string ritorno, string lingua)
    {
        if (ParametriAuthorize(ritorno) is not { } q)
            return (TestiAccesso.T(lingua, "ingresso"), null);
        var id = q.TryGetValue(Parameters.ClientId, out var c) ? c.ToString() : "";
        if (!_opzioni.App.TryGetValue(id, out var app))
            return null;
        var nome = string.IsNullOrWhiteSpace(app.Nome) ? id : app.Nome.Trim();
        var redirect = q.TryGetValue(Parameters.RedirectUri, out var r) ? r.ToString() : null;
        var registrati = _opzioni.PermettiLocalhost ? app.RedirectUri.Concat(app.RedirectUriProva) : app.RedirectUri;
        return redirect is not null && registrati.Contains(redirect, StringComparer.Ordinal) && Uri.TryCreate(redirect, UriKind.Absolute, out var u)
            ? (nome, u.IsDefaultPort ? u.Host : u.Authority)
            : (nome, null);
    }

    private ContentResult PaginaAccesso(string ritorno, string lingua, string? avviso, string? errore, string? email,
        int stato = StatusCodes.Status200OK)
    {
        var h = HtmlEncoder.Default;
        string T(string chiave) => h.Encode(TestiAccesso.T(lingua, chiave));
        var testo = new StringBuilder();
        if (Destinazione(ritorno, lingua) is var (nome, torna))
        {
            testo.Append($"<p class=\"destinazione\">{T("stai-entrando")}: <strong>{h.Encode(nome)}</strong>");
            if (torna is not null) testo.Append($"<span>{T("torni-a")} {h.Encode(torna)}</span>");
            testo.Append("</p>");
        }
        testo.Append($"<p class=\"piccolo\">{T("credenziali")}</p>");
        if (avviso is not null) testo.Append($"<p class=\"avviso\" role=\"status\">{h.Encode(avviso)}</p>");
        if (errore is not null) testo.Append($"<p class=\"errore\" role=\"alert\">{T(errore)}</p>");
        testo.Append($"""
            <form method="post" action="/perleuve/accesso">
              <input type="hidden" name="ritorno" value="{h.Encode(ritorno)}">
              <input type="hidden" name="lingua" value="{lingua}">
              <label>{T("email")}<input name="email" type="email" inputmode="email" autocomplete="username" autocapitalize="none" spellcheck="false" maxlength="50" required{(errore is null ? " autofocus" : "")} value="{h.Encode(email ?? "")}"></label>
              {CampoPassword("password", "current-password", lingua, autofocus: errore is not null)}
              <button>{T("entra")}</button>
            </form>
            """);
        if (_opzioni.RecuperoAttivo())
            testo.Append($"<p class=\"piccolo\"><a href=\"/perleuve/password-dimenticata?{TestiAccesso.Parametro}={lingua}\">{T("password-dimenticata")}</a></p>");
        else if (!string.IsNullOrWhiteSpace(_opzioni.PasswordDimenticata))
            testo.Append($"<p class=\"piccolo\"><a href=\"{h.Encode(_opzioni.PasswordDimenticata)}\">{T("password-dimenticata")}</a> {T("dal-portale")}</p>");
        return Pagina(Response, TestiAccesso.T(lingua, "titolo"), testo.ToString(), stato, lingua,
            "/perleuve/accesso?ritorno=" + Uri.EscapeDataString(ritorno));
    }

    /// <summary>JS del pulsante «Mostra»: l'unico script delle pagine Perleuve, scritto nell'attributo.</summary>
    public const string ScriptMostraPassword =
        "var i=this.previousElementSibling,m=i.type==='password';i.type=m?'text':'password';"
        + "this.textContent=m?this.dataset.nascondi:this.dataset.mostra;this.setAttribute('aria-pressed',m)";

    /// <summary>Campo password con «Mostra/Nascondi» (senza JavaScript il pulsante non fa niente e il campo resta nascosto).</summary>
    public static string CampoPassword(string nome, string autocomplete, string lingua, bool autofocus = false, string? etichetta = null,
        string attributi = "")
    {
        var h = HtmlEncoder.Default;
        string T(string chiave) => h.Encode(TestiAccesso.T(lingua, chiave));
        return $"<label>{h.Encode(etichetta ?? TestiAccesso.T(lingua, "password"))}<span class=\"campo-password\">"
            + $"<input name=\"{nome}\" type=\"password\" autocomplete=\"{autocomplete}\" maxlength=\"256\" required{attributi}{(autofocus ? " autofocus" : "")}>"
            + $"<button type=\"button\" class=\"mostra\" aria-pressed=\"false\" aria-label=\"{T("mostra-password")}\" data-mostra=\"{T("mostra")}\" "
            + $"data-nascondi=\"{T("nascondi")}\" onclick=\"{ScriptMostraPassword}\">{T("mostra")}</button></span></label>";
    }

    private ContentResult Pagina(string titolo, string corpo, int stato = StatusCodes.Status200OK) => Pagina(Response, titolo, corpo, stato);

    /// <summary>
    /// Le pagine Perleuve (accesso, ingresso, password dimenticata): HTML semplice, mai in cache né in un frame. Pagina
    /// neutra (task #400): marchio Perleuve · 4Grapes in cima, niente menù di Mappe, piè di pagina con privacy e assistenza
    /// (<see cref="AccessoUnicoOptions.Privacy"/>, <see cref="AccessoUnicoOptions.Assistenza"/>). Con
    /// <paramref name="percorsoLingue"/> in cima si sceglie la lingua (lo stesso indirizzo con <c>lingua=</c>).
    /// </summary>
    public static ContentResult Pagina(HttpResponse response, string titolo, string corpo, int stato = StatusCodes.Status200OK,
        string lingua = TestiAccesso.Italiano, string? percorsoLingue = null)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers["X-Frame-Options"] = "DENY";
        var h = HtmlEncoder.Default;
        var opzioni = response.HttpContext.RequestServices.GetService<IOptions<AccessoUnicoOptions>>()?.Value ?? new AccessoUnicoOptions();
        var lingue = percorsoLingue is null ? "" : $"<nav class=\"lingue\" aria-label=\"{h.Encode(TestiAccesso.T(lingua, "lingua"))}\">"
            + string.Join(" ", TestiAccesso.Lingue.Select(x => x.Codice == lingua
                ? $"<span aria-current=\"true\">{x.Nome}</span>"
                : $"<a href=\"{h.Encode(ConLingua(percorsoLingue, x.Codice))}\" hreflang=\"{x.Codice}\" lang=\"{x.Codice}\">{x.Nome}</a>"))
            + "</nav>";
        var piede = new StringBuilder("<span>Perleuve · 4Grapes</span>");
        if (TestiAccesso.CollegamentoPiede(opzioni.Privacy) is { } privacy)
            piede.Append($" <a href=\"{h.Encode(privacy)}\">{h.Encode(TestiAccesso.T(lingua, "privacy"))}</a>");
        if (TestiAccesso.CollegamentoPiede(opzioni.Assistenza) is { } assistenza)
            piede.Append($" <a href=\"{h.Encode(assistenza)}\">{h.Encode(TestiAccesso.TestoAssistenza(assistenza, lingua))}</a>");
        return new ContentResult
        {
            StatusCode = stato,
            ContentType = "text/html; charset=utf-8",
            Content = $$"""
                <!doctype html><html lang="{{lingua}}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                <meta name="theme-color" content="#8dc63e"><meta name="robots" content="noindex">
                <title>{{h.Encode(titolo)}} · Perleuve 4Grapes</title><style>
                @font-face{font-family:"Montserrat";src:url("/fonts/Montserrat-wght.ttf") format("truetype");font-weight:100 900;font-display:swap}
                @font-face{font-family:"Source Sans 3";src:url("/fonts/SourceSans3-wght.ttf") format("truetype");font-weight:200 900;font-display:swap}
                body{margin:0;font-family:"Source Sans 3",system-ui,sans-serif;background:#f4f6f2;color:#3c3950;display:flex;flex-direction:column;min-height:100vh}
                .testata{display:flex;align-items:center;gap:.8rem;padding:.8rem 1.2rem;background:#fff;border-bottom:3px solid #8dc63e}
                .marchio{display:grid;padding-left:.6rem;border-left:4px solid #8dc63e;font-family:Montserrat,system-ui,sans-serif;font-weight:700;font-size:.72rem;line-height:1.05;letter-spacing:.22em;text-transform:uppercase;color:#212331}
                .nome{display:grid;padding-left:.8rem;border-left:1px solid #d6dbd2;font-family:Montserrat,system-ui,sans-serif;font-weight:700;letter-spacing:.06em;text-transform:uppercase}
                .nome small{font-family:"Source Sans 3",system-ui,sans-serif;font-weight:400;letter-spacing:0;text-transform:none;color:#5f727f;font-size:.8rem}
                .lingue{margin-left:auto;font-size:.9rem}.lingue a,.lingue span{display:inline-block;padding:.6rem .4rem}.lingue span{font-weight:700}
                main{background:#fff;border-radius:12px;box-shadow:0 2px 12px #0002;padding:2rem;width:min(26rem,92vw);box-sizing:border-box;margin:2.5rem auto 1.5rem}
                h1{font-family:Montserrat,system-ui,sans-serif;font-size:1.4rem;margin:0 0 1rem;border-bottom:4px solid #8dc63e;padding-bottom:.5rem}
                label{display:block;margin:.8rem 0;font-weight:600}input{display:block;width:100%;box-sizing:border-box;margin-top:.3rem;padding:.7rem;font-size:1rem;border:1px solid #5f727f;border-radius:6px;font-family:inherit}
                input:focus{outline:2px solid #8dc63e;outline-offset:1px}
                .campo-password{display:flex;gap:.4rem}.campo-password input{flex:1;min-width:0}
                .campo-password .mostra{margin:.3rem 0 0;width:auto;min-width:5.5rem;background:#fff;color:#3c3950;border:1px solid #5f727f;font-weight:600}
                button{margin-top:1rem;width:100%;min-height:44px;font-size:1rem;background:#8dc63e;color:#fff;border:0;border-radius:6px;font-weight:700;cursor:pointer;font-family:inherit}
                button.secondario{background:#5f727f}.piccolo{font-size:.9rem;color:#5f727f}a{color:#3c3950}
                .errore{color:#b3261e;font-weight:600;background:#fdecea;border:1px solid #f3b8b3;border-radius:6px;padding:.5rem .7rem}.avviso{color:#3c3950}
                .destinazione{margin:0 0 .6rem;padding:.6rem .8rem;background:#f1f8e6;border-left:4px solid #8dc63e;border-radius:4px}
                .destinazione span{display:block;font-size:.9rem;color:#5f727f}
                .app{list-style:none;padding:0}.app a{display:block;padding:.9rem 1rem;margin:.5rem 0;border:1px solid #8dc63e;border-radius:8px;color:#3c3950;text-decoration:none;font-weight:700}
                .app a span{display:block;font-weight:400;font-size:.9rem;color:#5f727f}.saluto{font-size:1.1rem;font-weight:700;margin:0}
                footer{margin-top:auto;padding:1rem;text-align:center;font-size:.85rem;color:#5f727f}footer a{color:#5f727f}footer a::before{content:"·";display:inline-block;margin-right:.3rem;color:#5f727f}
                </style></head><body><header class="testata"><span class="marchio" role="img" aria-label="Perleuve"><span>Per</span><span>Le</span><span>Uve</span></span>
                <span class="nome">4Grapes<small>{{h.Encode(TestiAccesso.T(lingua, "marchio"))}}</small></span>{{lingue}}</header>
                <main><h1>{{h.Encode(titolo)}}</h1>{{corpo}}</main>
                <footer>{{piede}}</footer></body></html>
                """,
        };
    }

    /// <summary>Lo stesso indirizzo (locale) con la lingua scelta.</summary>
    public static string ConLingua(string percorso, string lingua) =>
        percorso + (percorso.Contains('?') ? "&" : "?") + TestiAccesso.Parametro + "=" + Uri.EscapeDataString(lingua);
}
