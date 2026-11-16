using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 11 «Trattamenti» del Portale, in lettura (tappa 4a, task #269): la pagina dei trattamenti giornalieri
/// (<see cref="CalcoloTrattamenti"/>) e la scheda di un trattamento.
/// </summary>
[Route("api/portale/trattamenti")]
public class PortaleTrattamentiController : PortaleControllerBase
{
    private readonly ILettureTrattamenti _letture;
    private readonly TimeProvider _tempo;

    public PortaleTrattamentiController(ILettureTrattamenti letture, TimeProvider tempo)
    {
        _letture = letture;
        _tempo = tempo;
    }

    /// <param name="data">Giorno (aaaa-mm-gg); assente = oggi in ora italiana.</param>
    [HttpGet("giornalieri")]
    public async Task<ActionResult<TrattamentiGiornalieriDto>> Giornalieri(DateOnly? data, string? regione, string? provincia,
        string? comune, int azienda = 0, int gruppo = 0, int linea = 0, int vigneto = 0, int vitigno = 0, int rischio = 0,
        int tipo = 0, int consulente = 0, CancellationToken ct = default)
    {
        var (utente, errore) = await UtenteAsync("trattamenti", ct);
        if (errore is not null)
        {
            return errore;
        }

        var filtri = new FiltriTrattamenti(data ?? Oggi(), regione, provincia, comune, azienda, gruppo, linea, vigneto, vitigno,
            rischio, tipo, consulente);
        try
        {
            var dati = await _letture.LeggiAsync(CalcoloTrattamenti.Dal(filtri.Data), utente!.IsMasterUser, ct);
            var vignetiUtente = utente.IsMasterUser ? null : await _letture.VignetiUtenteAsync(utente.IdUtente, ct);
            return Ok(CalcoloTrattamenti.Calcola(filtri, dati, vignetiUtente));
        }
        catch (SqlException ex)
        {
            return ErroreLettura("i trattamenti", ex);
        }
    }

    /// <summary>
    /// La scheda. Chi non è MasterUser vede solo i trattamenti dei suoi vigneti (il portale apre qualsiasi Id: qui no,
    /// come fa già il suo «Elimina»).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchedaTrattamentoDto>> Scheda(int id, CancellationToken ct)
    {
        var (utente, errore) = await UtenteAsync("trattamenti", ct);
        if (errore is not null)
        {
            return errore;
        }

        try
        {
            var letta = await _letture.SchedaAsync(id, ct);
            if (letta is not { } trovata
                || (!utente!.IsMasterUser && !(await _letture.VignetiUtenteAsync(utente.IdUtente, ct)).Contains(trovata.IdVigneto)))
            {
                return Errore(StatusCodes.Status404NotFound, "Trattamento non trovato.");
            }

            return Ok(trovata.Scheda);
        }
        catch (SqlException ex)
        {
            return ErroreLettura("il trattamento", ex);
        }
    }

    private DateOnly Oggi()
    {
        var italia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_tempo.GetUtcNow(), italia).DateTime);
    }
}
