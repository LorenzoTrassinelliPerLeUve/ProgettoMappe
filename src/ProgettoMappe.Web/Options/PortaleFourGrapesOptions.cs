using System.Globalization;

namespace ProgettoMappe.Web.Options;

/// <summary>
/// Pulsante «Apri in 4Grapes» della scheda del vigneto. <see cref="IndirizzoVigneto"/> è il modello
/// dell'indirizzo della pagina del vigneto nel portale, con <c>{id}</c> al posto dell'IdVigneto
/// (es. <c>https://portale.esempio/vigneti/{id}</c>). Vuoto = pulsante nascosto: l'indirizzo non è
/// nel database 4Grapes (dba, 2026-10-13) e non va inventato.
/// </summary>
public sealed class PortaleFourGrapesOptions
{
    public const string SectionName = "PortaleFourGrapes";

    public string? IndirizzoVigneto { get; set; }

    /// <summary>L'indirizzo per il vigneto, o null se il modello manca, non ha {id} o non è un indirizzo http(s) assoluto.</summary>
    public static string? Link(string? modello, int idVigneto)
    {
        if (string.IsNullOrWhiteSpace(modello) || !modello.Contains("{id}", StringComparison.Ordinal))
        {
            return null;
        }

        var indirizzo = modello.Trim().Replace("{id}", idVigneto.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return Uri.TryCreate(indirizzo, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : null;
    }
}
