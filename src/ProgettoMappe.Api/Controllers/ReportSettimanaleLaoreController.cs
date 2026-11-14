using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report settimanale LAORE (task #217): non è per azienda ma per territorio, come l'app Qlik «REPORT SETTIMANALE - LAORE».
/// Il perimetro è quello dello script (aziende di tipo 1 con vigneti in Sardegna, decisione provvisoria del boss del
/// 14/11), non <c>Perimetro:Gruppi</c>. Solo dallo snapshot del dba: senza file o senza tabelle 204.
/// </summary>
[ApiController]
[Route("api/report-settimanale-laore")]
public class ReportSettimanaleLaoreController : ControllerBase
{
    /// <summary>Come il settimanale: l'anno della settimana e i cinque prima (media storica BBCH).</summary>
    private const int GiorniMassimi = 6 * 366;

    private readonly SnapshotReportFineStagione _snapshot;

    public ReportSettimanaleLaoreController(SnapshotReportFineStagione snapshot) => _snapshot = snapshot;

    [HttpGet("luoghi")]
    public async Task<ActionResult<LuoghiLaoreDto>> GetLuoghi(CancellationToken ct)
    {
        var luoghi = await _snapshot.LeggiLuoghiLaoreAsync(ct);
        return luoghi is null ? NoContent()
            : Ok(new LuoghiLaoreDto(luoghi.Select(l => new LuogoVignetoDto(l.IdVigneto, l.Provincia, l.Comune)).ToList()));
    }

    [HttpGet("rilievi")]
    public async Task<ActionResult<RilieviSettimanaDto>> GetRilievi(DateOnly da, DateOnly a, string? provincia, string? comune,
        CancellationToken ct)
    {
        if (a < da || a.DayNumber - da.DayNumber > GiorniMassimi)
        {
            return BadRequest();
        }
        var letti = await _snapshot.LeggiRilieviLaoreAsync(Vuoto(provincia), Vuoto(comune), da, a, ct);
        return letti is null ? NoContent() : Ok(ReportSettimanaleController.Dto(letti));
    }

    private static string? Vuoto(string? testo) => string.IsNullOrWhiteSpace(testo) ? null : testo.Trim();
}
