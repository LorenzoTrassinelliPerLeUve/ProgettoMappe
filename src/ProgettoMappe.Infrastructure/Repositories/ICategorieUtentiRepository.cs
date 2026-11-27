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
}
