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

    public static readonly IReadOnlyList<string> Tutte = [Interno, Consulenza, Abbonato];

    public static bool Valida(string? esperienza) => esperienza is not null && Tutte.Contains(esperienza);
}

/// <summary>
/// La categoria dell'utente e l'esperienza che ne viene. <c>Categoria</c>: perleuve, consulenza, abbonamento,
/// academy-progetti, dormiente. <c>Motivo</c> = da quale segnale (per «Vedi come…» e per capire gli errori);
/// <c>Provvisoria</c> = decisa con le regole del dba invece che dalla vista <c>app.UtenteCategoria</c>.
/// </summary>
public record EsperienzaUtenteDto(
    int IdUtente,
    string Categoria,
    string Esperienza,
    string Motivo,
    bool Provvisoria,
    IReadOnlyList<string> SegnaliMancanti);
