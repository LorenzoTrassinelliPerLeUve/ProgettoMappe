using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.AnalisiSuolo;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report analisi del suolo (task #317, come l'app Qlik «ANALISI TERRENI - REPORT - NEW»): le analisi di laboratorio dei
/// rilievi 144, lette da 4Grapes a richiesta. Tutte le aziende con analisi, non solo il perimetro VTS (decisione del 17/11).
/// </summary>
[ApiController]
[Route("api/report-analisi-suolo")]
public class ReportAnalisiSuoloController : ControllerBase
{
    private readonly ILettureAnalisiSuolo _letture;
    private readonly ILogger<ReportAnalisiSuoloController> _logger;

    public ReportAnalisiSuoloController(ILettureAnalisiSuolo letture, ILogger<ReportAnalisiSuoloController> logger)
    {
        _letture = letture;
        _logger = logger;
    }

    [HttpGet("aziende")]
    public async Task<ActionResult<IReadOnlyList<AziendaAnalisiSuoloDto>>> GetAziende(CancellationToken ct)
    {
        try
        {
            return Ok((await _letture.AziendeAsync(ct)).OrderBy(a => a.RagioneSociale, StringComparer.CurrentCultureIgnoreCase).ToList());
        }
        catch (SqlException ex)
        {
            _logger.LogWarning("Aziende del report analisi del suolo non lette ({Numero}: {Messaggio})", ex.Number, ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("aziende/{id:int}")]
    public async Task<ActionResult<AnalisiSuoloAziendaDto>> GetAzienda(int id, CancellationToken ct)
    {
        try
        {
            return await _letture.AziendaAsync(id, ct) is { } dati ? Ok(dati) : NotFound();
        }
        catch (SqlException ex)
        {
            _logger.LogWarning("Analisi del suolo dell'azienda {Azienda} non lette ({Numero}: {Messaggio})", id, ex.Number, ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
