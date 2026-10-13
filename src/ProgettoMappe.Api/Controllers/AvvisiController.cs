using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Avvisi;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Avvisi dalle regole semplici di <c>Avvisi:Regole</c> (task #70): vigneti e stazioni da guardare.
/// Rispondono sempre subito dall'ultimo calcolo (ogni ora, <see cref="AggiornamentoAvvisi"/>);
/// prima del primo calcolo Pronto = false.
/// </summary>
[ApiController]
[Route("api")]
public class AvvisiController : ControllerBase
{
    private readonly CalcoloAvvisi _calcolo;
    private readonly PerimetroAziende _perimetro;

    public AvvisiController(CalcoloAvvisi calcolo, PerimetroAziende perimetro)
    {
        _calcolo = calcolo;
        _perimetro = perimetro;
    }

    /// <summary>Tutti gli avvisi delle aziende del perimetro, ordinati per azienda.</summary>
    [HttpGet("avvisi")]
    public ActionResult<AvvisiDto> GetTutti() => Ok(_calcolo.Ultimi);

    /// <summary>Gli avvisi di un'azienda; fuori perimetro nessuno (come un'azienda inesistente).</summary>
    [HttpGet("aziende/{aziendaId:int}/avvisi")]
    public async Task<ActionResult<AvvisiDto>> GetAzienda(int aziendaId, CancellationToken ct)
    {
        var avvisi = RegoleAvvisi.PerAzienda(_calcolo.Ultimi, aziendaId);
        return Ok(await _perimetro.ContieneAsync(aziendaId, ct) ? avvisi : avvisi with { Avvisi = [] });
    }
}
