namespace ProgettoMappe.Contracts;

/// <summary>
/// Le tre esperienze dopo l'accesso (task #401, docs/specifiche/tre-esperienze/PROPOSTA.md): «interno» = il lavoro di
/// Perleuve (backend), «consulenza» = consultazione della propria azienda, «abbonato» = backend semplificato + consultazione.
/// </summary>
public static class TipiEsperienza
{
    public const string Interno = "interno";
    public const string Consulenza = "consulenza";
    public const string Abbonato = "abbonato";

    /// <summary>
    /// Account non attivo (decisione del proprietario, 27/11): dormiente con l'app disabilitata. Dopo l'accesso solo la
    /// pagina «account non attivo», nessun dato. Non è fra le esperienze di «Vedi come…».
    /// </summary>
    public const string Fuori = "fuori";

    public const string Supporto = "support@4grapes.it";

    public static readonly IReadOnlyList<string> Tutte = [Interno, Consulenza, Abbonato];

    public static bool Valida(string? esperienza) => esperienza is not null && Tutte.Contains(esperienza);

    /// <summary>Chi non può cambiare niente (consultazione o account non attivo). Esperienza non nota = non si sa: false.</summary>
    public static bool SoloConsultazione(string? esperienza) => esperienza is Consulenza or Fuori;
}

/// <summary>
/// La categoria dell'utente e l'esperienza che ne viene. <c>Categoria</c>: i valori di <c>app.UtenteCategoria</c>
/// (interno, consulenza, abbonato, academy_progetti, dormiente). <c>Motivo</c> = da quale segnale (per «Vedi come…» e per
/// capire gli errori); <c>Provvisoria</c> = decisa con le regole del dba perché la vista non si è potuta leggere.
/// </summary>
public record EsperienzaUtenteDto(
    int IdUtente,
    string Categoria,
    string Esperienza,
    string Motivo,
    bool Provvisoria,
    IReadOnlyList<string> SegnaliMancanti);
