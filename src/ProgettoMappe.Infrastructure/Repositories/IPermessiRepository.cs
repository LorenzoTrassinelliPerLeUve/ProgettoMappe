namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Ruoli e abilitazioni di un utente 4Grapes, come li usa il portale (<c>AuthorizePortalAttribute</c>):
/// i ruoli sono le colonne <c>Is*</c> e <c>IdApp</c> di <c>dbo.Utente</c>, le abilitazioni i codici di
/// <c>dbo.VistaAbilitazioniUtente</c> (che le ricava dai ruoli incrociandoli con la tabella <c>Abilitazione</c>).
/// I codici sono tenuti in maiuscolo: nel DB c'è anche «Decision4Grapes» scritto così, e il portale li
/// confronta con l'enum ignorando le maiuscole.
/// </summary>
public record PermessiUtente(
    int IdUtente,
    bool IsAppEnabled,
    bool IsSysAdmin,
    bool IsMasterUser,
    bool IsGuest,
    bool IsUtentePerleuve,
    int? IdApp,
    IReadOnlySet<string> Abilitazioni)
{
    public bool Ha(string abilitazione) => Abilitazioni.Contains(abilitazione.ToUpperInvariant());
}

public interface IPermessiRepository
{
    /// <summary>
    /// I permessi dell'utente attivo (<c>StatoEntita = 1</c>) con questo Id, null se non c'è.
    /// Un errore SQL (es. GRANT mancante) esce come eccezione: chi chiama decide cosa mostrare.
    /// </summary>
    Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default);
}
