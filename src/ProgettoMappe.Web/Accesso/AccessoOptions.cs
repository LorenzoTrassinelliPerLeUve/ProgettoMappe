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

    public TimeSpan DurataSessione() => TimeSpan.FromHours(DurataSessioneOre is > 0 and <= 24 * 30 ? DurataSessioneOre : 8);
}
