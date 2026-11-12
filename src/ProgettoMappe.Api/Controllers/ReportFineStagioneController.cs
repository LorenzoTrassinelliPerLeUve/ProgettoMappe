using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report fine stagione (task #201): il meteo giornaliero per vigneto dell'azienda, letto a richiesta (una
/// query per azienda, indice su IdVigneto + Data). Fuori perimetro 404; se 4Grapes non risponde 503.
/// </summary>
[ApiController]
[Route("api/aziende/{aziendaId:int}/report-fine-stagione")]
public class ReportFineStagioneController : ControllerBase
{
    private readonly IFonteMeteoReport _fonte;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<ReportFineStagioneController> _logger;

    public ReportFineStagioneController(IFonteMeteoReport fonte, PerimetroAziende perimetro, ILogger<ReportFineStagioneController> logger)
    {
        _fonte = fonte;
        _perimetro = perimetro;
        _logger = logger;
    }

    [HttpGet("meteo")]
    public async Task<ActionResult<MeteoReportFineStagioneDto>> GetMeteo(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        try
        {
            var letto = await _fonte.LeggiAsync(aziendaId, ct);
            return Ok(new MeteoReportFineStagioneDto(
                letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)).ToList(),
                letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione,
                    g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)).ToList(),
                letto.StoricoDisponibile));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Meteo del report fine stagione non leggibile da 4Grapes (azienda {Azienda})", aziendaId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
