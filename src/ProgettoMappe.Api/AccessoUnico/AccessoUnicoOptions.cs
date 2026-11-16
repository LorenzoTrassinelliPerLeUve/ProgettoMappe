namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// Sezione "AccessoUnico" dell'Api (task #234, docs/accesso-unico.md): l'accesso unico Perleuve con le credenziali di
/// 4Grapes, per le app esterne (le app di Jan): token semplice e OpenID Connect. Nessun segreto nel repository: le chiavi
/// e i segreti delle app stanno in file creati da Mappe nella cartella dei dati.
/// </summary>
public sealed class AccessoUnicoOptions
{
    public const string Sezione = "AccessoUnico";

    /// <summary>Campo <c>iss</c> del token semplice: le app controllano che sia questo.</summary>
    public string Emittente { get; set; } = "mappe";

    /// <summary>Durata dei token in minuti (fuori da 5–1440 → 60).</summary>
    public int DurataMinuti { get; set; } = 60;

    /// <summary>
    /// File PEM della chiave privata RSA che firma i token (più percorsi con «;», vedi <see cref="PercorsoDati"/>). Se manca
    /// si crea (permessi 600); accanto nasce «….cifratura.pem» per OpenID Connect. Nessun percorso usabile = chiavi in
    /// memoria (Warning): i token valgono solo fino al riavvio dell'Api.
    /// </summary>
    public string? ChiavePrivata { get; set; }

    /// <summary>File JSON dei segreti delle app, creato da Mappe (vedi <see cref="SegretiApp"/>); più percorsi con «;».</summary>
    public string? FileSegreti { get; set; }

    /// <summary>Database SQLite di OpenID Connect (app registrate, codici, refresh token); più percorsi con «;». Vuoto = OIDC spento.</summary>
    public string? Database { get; set; }

    /// <summary>Indirizzo pubblico (issuer), es. https://mappe.…/; vuoto = dall'indirizzo della richiesta (dietro il proxy del Web).</summary>
    public string? Indirizzo { get; set; }

    /// <summary>Pagina del portale 4Grapes per recuperare la password: si rimanda lì, Mappe non manda email.</summary>
    public string? PasswordDimenticata { get; set; } = "https://portale.4grapes.it";

    /// <summary>Errori di password per email prima del blocco (15 minuti), come il login del Web.</summary>
    public int MaxErrori { get; set; } = 5;

    /// <summary>Le app che possono chiamare, per nome (intestazione <c>X-Mappe-App</c>, client_id di OpenID Connect).</summary>
    public Dictionary<string, AppEsternaOptions> App { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TimeSpan Durata() => DurataMinuti is >= 5 and <= 1440 ? TimeSpan.FromMinutes(DurataMinuti) : TimeSpan.FromMinutes(60);
}

public sealed class AppEsternaOptions
{
    /// <summary>Nome mostrato nell'Ingresso Perleuve (vuoto = la chiave).</summary>
    public string? Nome { get; set; }

    /// <summary>Indirizzo dell'app per il collegamento nell'Ingresso Perleuve (vuoto = non compare).</summary>
    public string? Indirizzo { get; set; }

    /// <summary>
    /// OpenID Connect: «server» (app con un server, client_secret = il segreto dell'app) o «browser» (Angular senza server,
    /// client pubblico, solo PKCE). Senza <see cref="RedirectUri"/> l'app non usa OpenID Connect.
    /// </summary>
    public string Tipo { get; set; } = "server";

    /// <summary>Indirizzi di ritorno dopo l'accesso (redirect_uri), esatti.</summary>
    public List<string> RedirectUri { get; set; } = [];

    /// <summary>Indirizzi di ritorno dopo l'uscita (post_logout_redirect_uri).</summary>
    public List<string> PostLogoutRedirectUri { get; set; } = [];

    /// <summary>
    /// SHA-256 esadecimale di un segreto alternativo (intestazione <c>X-Mappe-App-Segreto</c>); di solito non serve: il
    /// segreto lo crea Mappe nel file <see cref="AccessoUnicoOptions.FileSegreti"/>.
    /// </summary>
    public string? SegretoSha256 { get; set; }

    /// <summary>false = l'app non ottiene né rinnova token (403), senza togliere la riga.</summary>
    public bool Abilitata { get; set; } = true;

    /// <summary>
    /// Abilitazione di 4Grapes richiesta per entrare in questa app (es. «4SPRAYERS»); vuota = basta avere l'app
    /// abilitata (<c>IsAppEnabled</c>). Chi non ce l'ha riceve 403 (OpenID Connect: access_denied).
    /// </summary>
    public string? Abilitazione { get; set; }

    /// <summary>Richieste al minuto di questa app (token + me), oltre → 429.</summary>
    public int LimiteAlMinuto { get; set; } = 60;
}
