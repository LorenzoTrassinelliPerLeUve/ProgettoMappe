namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Anagrafica dell'utente per il token dell'accesso unico (task #234). <c>LoginName</c> è l'email (dba, 16/11).</summary>
public record AnagraficaUtente(int IdUtente, string LoginName, string Nome, string Cognome, bool IsAppEnabled);

/// <summary>Riga di <c>Utente_Ente</c> attiva con il nome dell'azienda (<c>NomeCommerciale</c>, altrimenti <c>RagioneSociale</c>).</summary>
public record AziendaUtente(int IdEnte, string Nome, bool Gestione, string? Ruolo = null);

/// <summary>
/// Riga di <c>dbo.AbilitazioneUtente</c> (abilitazione data a una persona, task #237 del dba): <c>IdEnte</c> null = per
/// tutte le aziende. Codice in maiuscolo.
/// </summary>
public record AbilitazionePersonale(int Id, int IdUtente, string Codice, int? IdEnte, bool Attiva, DateOnly? DataInizio, DateOnly? DataFine)
{
    /// <summary>Vale oggi: attiva (<c>StatoEntita = 1</c>) e dentro le date (estremi compresi, date vuote = senza limite).</summary>
    public bool ValeIl(DateOnly giorno) => Attiva && (DataInizio is null || DataInizio <= giorno) && (DataFine is null || DataFine >= giorno);
}

/// <summary>
/// Letture per l'accesso unico delle app esterne. Ruoli e abilitazioni per categoria vengono da
/// <see cref="IPermessiRepository"/>, la verifica della password da <see cref="IUtentiRepository"/>. Errori SQL come eccezioni.
/// </summary>
public interface IAccessoUnicoRepository
{
    /// <summary>L'utente attivo (<c>StatoEntita = 1</c>) con questo Id, null se non c'è.</summary>
    Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default);

    /// <summary>Aziende abbinate (<c>Utente_Ente.StatoEntita = 1</c>, <c>IdEnte &gt; 0</c>), ordinate per nome.</summary>
    Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default);

    /// <summary>Tutte le righe di <c>dbo.AbilitazioneUtente</c> dell'utente (anche spente o scadute: filtra chi chiama).</summary>
    Task<IReadOnlyList<AbilitazionePersonale>> GetAbilitazioniPersonaliAsync(int idUtente, CancellationToken ct = default);

    /// <summary>Una riga di <c>dbo.AbilitazioneUtente</c> per Id, null se non c'è.</summary>
    Task<AbilitazionePersonale?> GetAbilitazionePersonaleAsync(int id, CancellationToken ct = default);

    /// <summary>I codici di <c>dbo.Abilitazione</c> (colonna <c>Id</c>), in maiuscolo.</summary>
    Task<IReadOnlySet<string>> GetCodiciAbilitazioneAsync(CancellationToken ct = default);

    /// <summary>
    /// Esiste già un utente (attivo o no) con questo LoginName, spazi e maiuscole esclusi: <c>LoginName</c> non è unico nel DB
    /// e chi crea utenti deve evitare i doppioni (dba, 16/11).
    /// </summary>
    Task<bool> EsisteLoginAsync(string loginName, CancellationToken ct = default);

    /// <summary>L'utente è abbinato all'azienda (<c>Utente_Ente</c> attiva).</summary>
    Task<bool> AbbinatoAsync(int idUtente, int idEnte, CancellationToken ct = default);
}
