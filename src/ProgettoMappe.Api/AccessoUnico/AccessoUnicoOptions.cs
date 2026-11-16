namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// Sezione "AccessoUnico" dell'Api (task #234, docs/accesso-unico.md): il token per le app esterne (le app di Jan)
/// con le credenziali di 4Grapes. Nessun segreto in chiaro: la chiave privata sta in un file, delle app si scrive
/// solo l'impronta SHA-256 del segreto.
/// </summary>
public sealed class AccessoUnicoOptions
{
    public const string Sezione = "AccessoUnico";

    /// <summary>Campo <c>iss</c> del token: le app controllano che sia questo.</summary>
    public string Emittente { get; set; } = "mappe";

    /// <summary>Durata del token in minuti (fuori da 5–1440 → 60).</summary>
    public int DurataMinuti { get; set; } = 60;

    /// <summary>
    /// File PEM della chiave privata RSA che firma i token. Se manca si crea (permessi 600). Vuoto o non scrivibile =
    /// chiave in memoria (Warning): i token valgono solo fino al riavvio dell'Api.
    /// </summary>
    public string? ChiavePrivata { get; set; }

    /// <summary>Pagina del portale 4Grapes per recuperare la password: si rimanda lì, Mappe non manda email.</summary>
    public string? PasswordDimenticata { get; set; }

    /// <summary>Errori di password per email prima del blocco (15 minuti), come il login del Web.</summary>
    public int MaxErrori { get; set; } = 5;

    /// <summary>Le app che possono chiamare, per nome (intestazione <c>X-Mappe-App</c>).</summary>
    public Dictionary<string, AppEsternaOptions> App { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TimeSpan Durata() => DurataMinuti is >= 5 and <= 1440 ? TimeSpan.FromMinutes(DurataMinuti) : TimeSpan.FromMinutes(60);
}

public sealed class AppEsternaOptions
{
    /// <summary>SHA-256 esadecimale del segreto dell'app (intestazione <c>X-Mappe-App-Segreto</c>); il segreto non si scrive.</summary>
    public string? SegretoSha256 { get; set; }

    /// <summary>false = l'app non ottiene né rinnova token (403), senza togliere la riga.</summary>
    public bool Abilitata { get; set; } = true;

    /// <summary>
    /// Abilitazione di 4Grapes richiesta per entrare in questa app (es. «IRRORATRICI»); vuota = basta avere l'app
    /// abilitata (<c>IsAppEnabled</c>). Chi non ce l'ha riceve 403.
    /// </summary>
    public string? Abilitazione { get; set; }

    /// <summary>Richieste al minuto di questa app (token + me), oltre → 429.</summary>
    public int LimiteAlMinuto { get; set; } = 60;
}
