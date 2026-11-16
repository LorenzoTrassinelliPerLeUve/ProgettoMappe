namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Ruoli e abilitazioni di un utente 4Grapes, come li usa il portale (<c>AuthorizePortalAttribute</c>):
/// i ruoli sono le colonne <c>Is*</c> e <c>IdApp</c> di <c>dbo.Utente</c>, le abilitazioni i codici di
/// <c>dbo.VistaAbilitazioniUtente</c> (che le ricava dai ruoli incrociandoli con la tabella <c>Abilitazione</c>).
/// I codici sono tenuti in maiuscolo: nel DB c'è anche «Decision4Grapes» scritto così, e il portale li
/// confronta con l'enum ignorando le maiuscole. <c>Ridotti = true</c>: mancano i GRANT su ruoli e abilitazioni, si conosce
/// solo <c>IsAppEnabled</c> e si danno le <see cref="AbilitazioniAppEnabled"/>. <c>IsDSSUser</c> (task #296: linea e rischio
/// del vigneto) si legge a parte: senza il permesso su quella colonna vale false e gli altri ruoli restano.
/// </summary>
public record PermessiUtente(
    int IdUtente,
    bool IsAppEnabled,
    bool IsSysAdmin,
    bool IsMasterUser,
    bool IsGuest,
    bool IsUtentePerleuve,
    int? IdApp,
    IReadOnlySet<string> Abilitazioni,
    bool Ridotti = false,
    bool IsDSSUser = false)
{
    /// <summary>
    /// Le abilitazioni che in 4Grapes hanno tutti gli utenti con l'app abilitata (riga <c>AppEnabled = 1</c> della tabella
    /// <c>Abilitazione</c>: dal dba, 14/11, le 11 abilitazioni con 490 utenti). Si usano solo finché Mappe non può leggere
    /// ruoli e abilitazioni (<see cref="Ridotti"/>): nessuna sezione da MasterUser né da amministratore.
    /// </summary>
    public static readonly IReadOnlySet<string> AbilitazioniAppEnabled = new HashSet<string>(StringComparer.Ordinal)
    {
        "ANALISI_DEI_TERRENI", "ANALISI_UVE", "FALLANZE", "GALLERIA_FOTOGRAFICA", "INTORNOAME", "INVIA_PREVISIONI_METEO",
        "METEOGRAMMI", "PREVISIONI_METEO", "PRODUZIONE", "RILIEVI", "VIGNETI",
    };

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
