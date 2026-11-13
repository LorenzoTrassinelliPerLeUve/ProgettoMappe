using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report settimanale (task #213): i rilievi dell'azienda fra due date (la settimana la sceglie il Web). Dallo snapshot
/// notturno del dba; se il file non c'è, da 4Grapes. Fuori perimetro 404, periodo non valido (o oltre 62 giorni) 400,
/// 4Grapes che non risponde 503.
/// </summary>
[ApiController]
[Route("api/aziende/{aziendaId:int}/report-settimanale")]
public class ReportSettimanaleController : ControllerBase
{
    private const int GiorniMassimi = 62;

    private readonly IFonteRilieviSettimana _fonte;
    private readonly SnapshotReportFineStagione _snapshot;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<ReportSettimanaleController> _logger;

    public ReportSettimanaleController(IFonteRilieviSettimana fonte, SnapshotReportFineStagione snapshot, PerimetroAziende perimetro,
        ILogger<ReportSettimanaleController> logger)
    {
        _fonte = fonte;
        _snapshot = snapshot;
        _perimetro = perimetro;
        _logger = logger;
    }

    [HttpGet("rilievi")]
    public async Task<ActionResult<RilieviSettimanaDto>> GetRilievi(int aziendaId, DateOnly da, DateOnly a, CancellationToken ct)
    {
        if (a < da || a.DayNumber - da.DayNumber > GiorniMassimi)
        {
            return BadRequest();
        }
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        try
        {
            var letti = await _snapshot.LeggiRilieviAsync(aziendaId, da, a, ct) ?? await _fonte.LeggiAsync(aziendaId, da, a, ct);
            return Ok(new RilieviSettimanaDto(
                letti.Rilievi.Select(r => new RilievoSettimanaDto(r.IdVigneto, r.Data, r.TipoRilievo, r.Bbch)).ToList(),
                letti.NomiTipi, letti.Fonte, letti.Snapshot));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Rilievi del report settimanale non leggibili da 4Grapes (azienda {Azienda})", aziendaId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
