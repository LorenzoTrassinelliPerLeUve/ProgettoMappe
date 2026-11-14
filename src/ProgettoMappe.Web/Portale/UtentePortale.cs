using System.Globalization;
using System.Security.Claims;

namespace ProgettoMappe.Web.Portale;

/// <summary>L'utente 4Grapes entrato, come lo vede il Portale: l'Id sta in un claim del cookie (messo da Accesso.razor).</summary>
public static class UtentePortale
{
    public const string ClaimIdUtente = "mappe:id_utente";

    /// <summary>L'IdUtente 4Grapes, null se non è entrato o se il cookie è di prima della task #225 (va rifatto l'accesso).</summary>
    public static int? IdUtente(ClaimsPrincipal? utente) =>
        utente?.FindFirstValue(ClaimIdUtente) is { } testo
        && int.TryParse(testo, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id
            : null;
}
