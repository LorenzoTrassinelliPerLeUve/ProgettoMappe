using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Verifica nome utente e password (<see cref="VerificaCredenzialiApi"/>: tabella 4Grapes
/// <c>Utente</c> tramite l'Api).
/// </summary>
public interface IVerificaCredenziali
{
    /// <summary>
    /// L'utente (Id 4Grapes e nome da mostrare) se le credenziali sono valide, null se sono errate; eccezione se la
    /// verifica non è possibile (Api o database non raggiungibili).
    /// </summary>
    Task<UtenteAccessoDto?> VerificaAsync(string nomeUtente, string password, CancellationToken cancellationToken = default);
}
