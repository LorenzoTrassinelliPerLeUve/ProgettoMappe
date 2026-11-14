using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Le letture delle sezioni del Portale (tappa 1), con le stesse query del portale 4Grapes. Le regole su chi vede cosa
/// (MasterUser, aziende abbinate) le applica il controller: qui si passano già gli insiemi permessi.
/// Gli errori SQL (es. GRANT mancante, numero 229) escono come eccezioni.
/// </summary>
public interface ILetturePortale
{
    /// <summary>Aziende abbinate all'utente (<c>Utente_Ente</c> con <c>StatoEntita = 1</c>), come <c>GetIdsEntiAbbinati</c>.</summary>
    Task<IReadOnlyList<AziendaPortaleDto>> AziendeAbbinateAsync(int idUtente, CancellationToken ct = default);

    /// <summary>Tutte le aziende (<c>IdEnte &gt; 0</c>), per i MasterUser, ordinate per RagioneSociale.</summary>
    Task<IReadOnlyList<AziendaPortaleDto>> TutteLeAziendeAsync(CancellationToken ct = default);

    /// <summary>Gli utenti che condividono con l'utente i rilievi di quell'azienda (<c>Condivisione</c>).</summary>
    Task<IReadOnlyList<int>> UtentiCheCondividonoAsync(int idUtente, int idAzienda, CancellationToken ct = default);

    /// <summary>
    /// <c>SELECT * FROM VistaExportRilievi_{lingua}</c> per azienda e anno; <paramref name="idUtenti"/> null = tutti gli
    /// utenti (export «dell'azienda» dei MasterUser).
    /// </summary>
    Task<TabellaPortaleDto> RilieviExportAsync(string lingua, IReadOnlyList<int>? idUtenti, int idAzienda, int anno,
        CancellationToken ct = default);

    Task<IReadOnlyList<AsportazioneDto>> AsportazioniAsync(int idAzienda, int? anno, CancellationToken ct = default);

    /// <summary>Foto dell'azienda nell'anno; <paramref name="idUtenteVigneti"/> = solo i vigneti abbinati a quell'utente (<c>VistaUtentiVigneti</c>).</summary>
    Task<IReadOnlyList<FotoGalleriaDto>> GalleriaAsync(int idAzienda, int anno, int? idUtenteVigneti, CancellationToken ct = default);
}
