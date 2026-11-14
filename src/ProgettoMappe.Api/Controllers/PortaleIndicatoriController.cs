using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Sezione 3 «Indicatori aziendali» del Portale (abilitazione INDICATORI_AZIENDALI): dati da 4Grapes, formule in <see cref="CalcoloIndicatori"/>.</summary>
[Route("api/portale/indicatori")]
public class PortaleIndicatoriController : PortaleControllerBase
{
    private readonly ILettureIndicatori _letture;
    private readonly TimeProvider _tempo;

    public PortaleIndicatoriController(ILettureIndicatori letture, TimeProvider tempo)
    {
        _letture = letture;
        _tempo = tempo;
    }

    /// <param name="data">Giorno (aaaa-mm-gg); assente = oggi in ora italiana, come <c>DateTime.Today</c> del portale.</param>
    [HttpGet]
    public async Task<ActionResult<IndicatoriAziendaliDto>> Get(DateOnly? data, string? regione, string? provincia, string? comune,
        int? azienda, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("indicatori", ct);
        if (errore is not null)
        {
            return errore;
        }

        var giorno = data?.ToDateTime(TimeOnly.MinValue) ?? Oggi();
        try
        {
            var dati = await _letture.LeggiAsync(giorno, ct);
            var vignetiUtente = utente!.IsMasterUser ? null : await _letture.VignetiUtenteAsync(utente.IdUtente, ct);
            return Ok(CalcoloIndicatori.Calcola(giorno, new FiltriIndicatori(regione, provincia, comune, azienda), dati, vignetiUtente));
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i dati degli indicatori", ex);
        }
    }

    private DateTime Oggi()
    {
        var italia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        return TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), italia).Date;
    }
}
