namespace ProgettoMappe.Infrastructure.Repositories.InMemory;

/// <summary>
/// Senza 4Grapes non ci sono utenti (vedi <see cref="InMemoryUtentiRepository"/>): nessun permesso. Unica eccezione,
/// per le foto e le prove UX del Portale (task #387): <c>Portale:UtenteDiProva</c> dell'Api (un Id) dà a quell'Id tutti
/// i permessi sui dati di esempio. Non c'è nessuna credenziale: il login lo finge chi fa le foto, fuori dall'app.
/// </summary>
public class InMemoryPermessiRepository : IPermessiRepository
{
    /// <summary>Tutte le abilitazioni che le sezioni del Portale chiedono.</summary>
    public static readonly IReadOnlySet<string> TutteLeAbilitazioni = new HashSet<string>(StringComparer.Ordinal)
    {
        "ABILITAZIONI", "ANALISI_DEI_TERRENI", "ANALISI_UVE", "BOLLETTINI_AZIENDALI", "CONCIMAZIONI", "DASHBOARD",
        "FALLANZE", "GALLERIA_FOTOGRAFICA", "IMPOSTAZIONI", "INDICATORI_AZIENDALI", "INVIA_NOTIFICHE", "IRRORATRICI",
        "LINEE_DI_DIFESA", "PRODUZIONE", "RILIEVI", "SOGLIE_TRATTAMENTI", "TRATTAMENTI", "TRATTAMENTI_AZIENDALI",
        "TRATTAMENTI_AUTOMATICI", "UTENTI", "VIGNETI",
    };

    private readonly int? _utenteDiProva;

    public InMemoryPermessiRepository(int? utenteDiProva = null) => _utenteDiProva = utenteDiProva;

    public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default)
        => Task.FromResult(_utenteDiProva == idUtente
            ? new PermessiUtente(idUtente, IsAppEnabled: true, IsSysAdmin: false, IsMasterUser: true, IsGuest: false,
                IsUtentePerleuve: true, IdApp: null, TutteLeAbilitazioni)
            : null);
}
