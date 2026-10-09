namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Verifica nome utente e password. L'implementazione reale leggerà la tabella 4Grapes
/// <c>Utente</c> (<c>LoginName</c>, <c>ShaPassword</c>): non esiste ancora perché colonne,
/// algoritmo SHA, sale e codifica dell'hash non sono confermati (non inventarli).
/// </summary>
public interface IVerificaCredenziali
{
    /// <summary>Il nome da mostrare se le credenziali sono valide, altrimenti null.</summary>
    Task<string?> VerificaAsync(string nomeUtente, string password, CancellationToken cancellationToken = default);
}
