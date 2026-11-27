using System.Globalization;

namespace ProgettoMappe.Contracts.Accesso;

/// <summary>
/// Testi delle pagine di accesso (task #400): quella di Mappe (<c>/accesso</c>, Web) e quella dell'accesso unico Perleuve
/// (<c>/perleuve/*</c>, Api) dicono le stesse cose con le stesse parole, in italiano e in inglese. Stanno qui perché li
/// usano tutti e due i progetti. Una lingua in più = una colonna in più in <see cref="Voci"/>.
/// </summary>
public static class TestiAccesso
{
    public const string Italiano = "it";
    public const string Inglese = "en";

    /// <summary>Parametro dell'indirizzo con la lingua scelta (<c>?lingua=en</c>).</summary>
    public const string Parametro = "lingua";

    public static readonly IReadOnlyList<(string Codice, string Nome)> Lingue = [(Italiano, "Italiano"), (Inglese, "English")];

    /// <summary>
    /// La lingua della pagina: quella scelta (parametro o campo del modulo), altrimenti la preferita del browser. Italiano
    /// se il browser preferisce l'italiano o non dice niente; ogni altra lingua (croato, macedone, inglese…) → inglese.
    /// </summary>
    public static string Lingua(string? scelta, string? acceptLanguage = null)
    {
        if (Normalizza(scelta) is { } l) return l;
        var preferita = (acceptLanguage ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((voce, i) =>
            {
                var parti = voce.Split(';', StringSplitOptions.TrimEntries);
                var q = parti.Skip(1).Where(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                    .Select(p => double.TryParse(p[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0)
                    .DefaultIfEmpty(1).First();
                return (Tag: parti[0], Q: q, Ordine: i);
            })
            .Where(v => v.Q > 0 && v.Tag != "*")
            .OrderByDescending(v => v.Q).ThenBy(v => v.Ordine)
            .Select(v => v.Tag)
            .FirstOrDefault();
        return preferita is null || preferita.StartsWith(Italiano, StringComparison.OrdinalIgnoreCase) ? Italiano : Inglese;
    }

    /// <summary>«it»/«en» (anche «en-GB», «IT»…) oppure null se non è una lingua delle pagine.</summary>
    public static string? Normalizza(string? lingua)
    {
        var codice = lingua?.Trim();
        if (string.IsNullOrEmpty(codice)) return null;
        if (codice.Length > 2 && codice[2] is '-' or '_') codice = codice[..2];
        return Lingue.Select(x => x.Codice).FirstOrDefault(c => string.Equals(c, codice, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Collegamento del piè di pagina (privacy, assistenza): un indirizzo http(s) o un percorso del sito; un'email diventa
    /// <c>mailto:</c>. Altro (vuoto, «javascript:»…) non si mostra.
    /// </summary>
    public static string? CollegamentoPiede(string? valore)
    {
        var v = valore?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Contains('@') && !v.Contains(':') && !v.Contains('/')) return "mailto:" + v;
        if (v.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) && v.Contains('@')) return v;
        if (v[0] == '/') return v.Length == 1 || v[1] is not '/' and not '\\' ? v : null;
        return Uri.TryCreate(v, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp) ? v : null;
    }

    /// <summary>Il testo nella lingua (chiave sconosciuta = eccezione: è un errore di chi scrive la pagina).</summary>
    public static string T(string lingua, string chiave) =>
        Voci.TryGetValue(chiave, out var v) ? (lingua == Inglese ? v.En : v.It) : throw new KeyNotFoundException(chiave);

    public static readonly IReadOnlyDictionary<string, (string It, string En)> Voci = new Dictionary<string, (string, string)>
    {
        ["titolo"] = ("Accedi", "Sign in"),
        ["marchio"] = ("Accesso unico", "Single sign-on"),
        ["stai-entrando"] = ("Stai entrando in", "You are signing in to"),
        ["torni-a"] = ("Dopo l'accesso torni a", "After signing in you will go back to"),
        ["mappe"] = ("Mappe 4Grapes", "4Grapes Maps"),
        ["ingresso"] = ("Ingresso Perleuve: le tue app", "Perleuve home: your apps"),
        ["credenziali"] = ("Usa l'email e la password di 4Grapes: valgono per tutte le app Perleuve.",
            "Use your 4Grapes email and password: they work for all Perleuve apps."),
        ["email"] = ("Email", "Email"),
        ["password"] = ("Password", "Password"),
        ["mostra"] = ("Mostra", "Show"),
        ["nascondi"] = ("Nascondi", "Hide"),
        ["mostra-password"] = ("Mostra la password", "Show password"),
        ["entra"] = ("Entra", "Sign in"),
        ["password-dimenticata"] = ("Password dimenticata?", "Forgot your password?"),
        ["dal-portale"] = ("Si recupera dal portale 4Grapes.", "You can reset it on the 4Grapes portal."),
        ["errore-vuoti"] = ("Scrivi email e password.", "Enter your email and password."),
        ["errore-credenziali"] = ("Email o password non corretti. La password è quella di 4Grapes.",
            "Incorrect email or password. Use your 4Grapes password."),
        ["bloccato"] = ("Troppi tentativi sbagliati: riprova fra 15 minuti.", "Too many failed attempts: try again in 15 minutes."),
        ["non-disponibile"] = ("L'accesso non è disponibile in questo momento: riprova fra poco.",
            "Sign-in is not available right now: please try again shortly."),
        ["uscito"] = ("Sei uscito.", "You have signed out."),
        ["scaduta"] = ("La sessione è scaduta: accedi di nuovo.", "Your session has expired: please sign in again."),
        ["lingua"] = ("Lingua", "Language"),
        ["privacy"] = ("Privacy", "Privacy"),
        ["assistenza"] = ("Assistenza", "Support"),
    };
}
