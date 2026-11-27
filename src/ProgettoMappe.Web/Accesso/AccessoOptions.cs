namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Login della mappa (sezione <c>Accesso</c>). Spento per default: va acceso solo dopo il GRANT
/// SELECT su <c>dbo.Utente</c> per l'utente dell'Api, altrimenti nessuno può entrare.
/// </summary>
public class AccessoOptions
{
    public const string SectionName = "Accesso";

    /// <summary>Se vero, tutte le pagine richiedono il login (tranne la pagina di login).</summary>
    public bool Abilitato { get; set; }

    /// <summary>Durata della sessione dal momento del login (non si rinnova con l'uso).</summary>
    public int DurataSessioneOre { get; set; } = 8;

    /// <summary>Piè della pagina di accesso (task #400): pagina della privacy (http(s) o percorso); vuoto = non compare.</summary>
    public string? Privacy { get; set; }

    /// <summary>Piè della pagina di accesso: contatto dell'assistenza (email o indirizzo http(s)); vuoto = non compare.</summary>
    public string? Assistenza { get; set; }

    /// <summary>
    /// Dove si va dopo l'accesso quando non c'è un ReturnUrl. «/» = la pagina iniziale di Mappe (rilascio del single
    /// sign-on, task #403); la #401 la porta a <c>/esperienza</c>.
    /// </summary>
    public string PaginaIniziale { get; set; } = "/";

    /// <summary>Single sign-on (task #403): Mappe entra con l'accesso unico Perleuve invece del suo login.</summary>
    public AccessoUnicoClientOptions AccessoUnico { get; set; } = new();

    public TimeSpan DurataSessione() => TimeSpan.FromHours(DurataSessioneOre is > 0 and <= 24 * 30 ? DurataSessioneOre : 8);
}
