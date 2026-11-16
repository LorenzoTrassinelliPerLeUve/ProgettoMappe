namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Anagrafica dell'utente per il token dell'accesso unico (task #234). <c>LoginName</c> è l'email (dba, 16/11).</summary>
public record AnagraficaUtente(int IdUtente, string LoginName, string Nome, string Cognome, bool IsAppEnabled);

/// <summary>Riga di <c>Utente_Ente</c> attiva con il nome dell'azienda (<c>NomeCommerciale</c>, altrimenti <c>RagioneSociale</c>).</summary>
public record AziendaUtente(int IdEnte, string Nome, bool Gestione, string? Ruolo = null);

/// <summary>
/// Letture per l'accesso unico delle app esterne. Ruoli e abilitazioni vengono da <see cref="IPermessiRepository"/>,
/// la verifica della password da <see cref="IUtentiRepository"/>. Errori SQL come eccezioni.
/// </summary>
public interface IAccessoUnicoRepository
{
    /// <summary>L'utente attivo (<c>StatoEntita = 1</c>) con questo Id, null se non c'è.</summary>
    Task<AnagraficaUtente?> GetAnagraficaAsync(int idUtente, CancellationToken ct = default);

    /// <summary>Aziende abbinate (<c>Utente_Ente.StatoEntita = 1</c>, <c>IdEnte &gt; 0</c>), ordinate per nome.</summary>
    Task<IReadOnlyList<AziendaUtente>> GetAziendeAsync(int idUtente, CancellationToken ct = default);
}
