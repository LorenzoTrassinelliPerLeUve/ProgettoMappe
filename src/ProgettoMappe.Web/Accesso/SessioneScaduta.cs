using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Cookie rimasti da prima di un riavvio con chiavi diverse (task #222): il cookie di accesso non si decifra più e
/// il token antiforgery del modulo non vale. Invece di un 400 secco il browser torna a <c>/accesso</c> con
/// «la sessione è scaduta», un token nuovo e i cookie vecchi cancellati.
/// </summary>
public static class SessioneScaduta
{
    public const string NomeCookieAccesso = "mappe.accesso";

    /// <summary>Parametro dell'indirizzo che fa comparire il messaggio sulla pagina di accesso.</summary>
    public const string Parametro = "scaduta";

    public const string Messaggio = "La sessione è scaduta: accedi di nuovo.";

    private const string ChiaveScaduta = "mappe.sessione-scaduta";

    /// <summary>
    /// Dopo <c>UseAuthentication</c>: un cookie di accesso che non si decifra (o scaduto) si cancella. La richiesta
    /// continua da utente non entrato; se serve il login, il rimando lo segna come sessione scaduta.
    /// </summary>
    public static async Task CookieAccessoAsync(HttpContext context, Func<Task> next)
    {
        if (context.Request.Cookies.ContainsKey(NomeCookieAccesso)
            && await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme) is { Failure: not null })
        {
            Cancella(context, NomeCookieAccesso);
            context.Items[ChiaveScaduta] = true;
        }
        await next();
    }

    /// <summary>
    /// Dopo <c>UseAntiforgery</c>: un modulo con un token non valido (di solito di prima di un riavvio) non arriva
    /// alla pagina, che risponderebbe 400; si cancella il cookie antiforgery e si torna all'accesso.
    /// </summary>
    public static async Task TokenModuloAsync(HttpContext context, Func<Task> next)
    {
        if (context.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false })
        {
            TornaAllAccesso(context);
            return;
        }
        await next();
    }

    /// <summary>Cancella i cookie vecchi (antiforgery e accesso) e rimanda a <c>/accesso?scaduta=1</c>.</summary>
    public static void TornaAllAccesso(HttpContext context)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie.Name;
        if (antiforgery is not null && context.Request.Cookies.ContainsKey(antiforgery))
            Cancella(context, antiforgery);
        if (context.Request.Cookies.ContainsKey(NomeCookieAccesso))
            Cancella(context, NomeCookieAccesso);
        context.Response.Redirect(IndirizzoAccesso(context, context.Request.Query["ReturnUrl"]));
    }

    /// <summary>Rimando al login della pagina protetta: se il cookie era scaduto, l'accesso lo dice.</summary>
    public static Task RimandoAlLogin(RedirectContext<CookieAuthenticationOptions> contesto)
    {
        if (contesto.Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            contesto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        var indirizzo = contesto.RedirectUri;
        if (Scaduta(contesto.HttpContext))
            indirizzo += (indirizzo.Contains('?') ? "&" : "?") + Parametro + "=1";
        contesto.Response.Redirect(indirizzo);
        return Task.CompletedTask;
    }

    /// <summary>Vero se in questa richiesta è stato buttato un cookie di accesso non più valido.</summary>
    public static bool Scaduta(HttpContext context) => context.Items.ContainsKey(ChiaveScaduta);

    internal static string IndirizzoAccesso(HttpContext context, string? urlRitorno)
    {
        var query = QueryString.Create(Parametro, "1");
        if (!string.IsNullOrEmpty(urlRitorno))
            query = query.Add("ReturnUrl", UrlRitorno.Sicuro(urlRitorno));
        return context.Request.PathBase + "/accesso" + query;
    }

    private static void Cancella(HttpContext context, string nome) =>
        context.Response.Cookies.Delete(nome, new CookieOptions
        {
            Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
            Secure = context.Request.IsHttps,
            HttpOnly = true,
        });
}
