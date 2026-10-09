namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>Riga di <c>dbo.Utente</c> che serve al login (mai esposta fuori dall'Api).</summary>
public record CredenzialiUtente(int Id, string Nome, string Cognome, string ShaPassword);

public interface IUtentiRepository
{
    /// <summary>
    /// Utenti attivi e abilitati all'app con questo LoginName. Può restituirne più d'uno: in 4Grapes
    /// <c>LoginName</c> non è unico (vedi <see cref="VerificaPassword.Scegli"/>).
    /// </summary>
    Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default);
}
