namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Indirizzo a cui tornare dopo il login: solo percorsi locali, così un link costruito ad arte
/// (<c>?ReturnUrl=https://altro-sito</c>) non porta l'utente fuori dalla mappa.
/// </summary>
public static class UrlRitorno
{
    public const string Predefinito = "/";

    public static string Sicuro(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url[0] != '/')
            return Predefinito;

        // "//host" e "/\host" sono interpretati dal browser come indirizzi di un altro sito.
        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
            return Predefinito;

        // Caratteri di controllo (a capo, tab...) non hanno motivo di stare in un percorso.
        return url.Any(char.IsControl) ? Predefinito : url;
    }
}
