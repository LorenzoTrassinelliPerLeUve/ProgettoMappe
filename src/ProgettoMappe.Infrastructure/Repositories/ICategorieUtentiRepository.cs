namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// I segnali di 4Grapes da cui si ricava la categoria di un utente (task #401, regole del dba in
/// dba/uscite/2026-11-27-categorie-utenti/ANALISI.md finché non c'è la vista <c>app.UtenteCategoria</c>).
/// Un segnale null = colonna non leggibile da Mappe (GRANT mancante), non «falso».
/// </summary>
/// <param name="LegatoConsulenza">Legato (<c>Utente_Ente</c> attivo) ad almeno un'azienda dei gruppi della consulenza.</param>
/// <param name="AbbonamentoPagato"><c>IdAbbonamentoAttivo</c> valorizzato (Stripe).</param>
/// <param name="AbbonamentoTesto">Il campo testo <c>Abbonamento</c>: etichetta di provenienza (ACADEMY 5, LAORE…).</param>
public record SegnaliCategoria(
    int IdUtente,
    string? LoginName,
    bool? IsUtentePerleuve,
    bool LegatoConsulenza,
    bool? AbbonamentoPagato,
    string? AbbonamentoTesto,
    bool IsAppEnabled);

public interface ICategorieUtentiRepository
{
    /// <summary>I segnali dell'utente attivo con questo Id, null se non c'è. Un errore SQL diverso dai permessi esce come eccezione.</summary>
    Task<SegnaliCategoria?> GetSegnaliAsync(int idUtente, IReadOnlyCollection<int> gruppiConsulenza, CancellationToken ct = default);

    /// <summary>La categoria di <c>app.UtenteCategoria</c> (valori in <see cref="CategorieUtente"/>), «dormiente» se l'utente non c'è.</summary>
    Task<string> GetCategoriaAsync(int idUtente, CancellationToken ct = default);
}

/// <summary>I valori di <c>app.UtenteCategoria.Categoria</c> (dba, 27/11): una sola categoria per utente, la prima regola che vale.</summary>
public static class CategorieUtente
{
    public const string Interno = "interno";
    public const string Consulenza = "consulenza";
    public const string Abbonato = "abbonato";
    public const string AcademyProgetti = "academy_progetti";
    public const string Dormiente = "dormiente";
}
