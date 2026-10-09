using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Verifica delle credenziali per il login del Web. L'Api è interna (127.0.0.1): la chiama solo
/// il Web dal server, che limita i tentativi.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccessoController : ControllerBase
{
    private readonly IUtentiRepository _utenti;
    private readonly ILogger<AccessoController> _logger;

    public AccessoController(IUtentiRepository utenti, ILogger<AccessoController> logger)
    {
        _utenti = utenti;
        _logger = logger;
    }

    /// <summary>200 con l'utente se le credenziali sono valide, 401 altrimenti, 503 se 4Grapes non risponde.</summary>
    [HttpPost("verifica")]
    public async Task<ActionResult<UtenteAccessoDto>> Verifica(RichiestaAccessoDto richiesta, CancellationToken ct)
    {
        var loginName = richiesta.NomeUtente?.Trim() ?? "";
        var password = richiesta.Password ?? "";
        if (loginName.Length is 0 or > 50 || password.Length is 0 or > 256)
            return Unauthorized();

        IReadOnlyList<CredenzialiUtente> candidati;
        try
        {
            candidati = await _utenti.GetCredenzialiAsync(loginName, ct);
        }
        catch (SqlException ex)
        {
            // Es. manca il GRANT SELECT su dbo.Utente: si logga il motivo, mai le credenziali.
            _logger.LogError(ex, "Lettura di dbo.Utente non riuscita (errore SQL {Numero})", ex.Number);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var utente = VerificaPassword.Scegli(candidati, password);
        if (utente is null)
            return Unauthorized();

        return Ok(new UtenteAccessoDto(utente.Id, VerificaPassword.NomeVisualizzato(utente, loginName)));
    }
}
