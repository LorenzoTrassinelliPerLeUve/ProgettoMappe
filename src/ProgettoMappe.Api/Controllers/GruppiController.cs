using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GruppiController : ControllerBase
{
    private readonly IGruppiRepository _gruppi;

    public GruppiController(IGruppiRepository gruppi)
    {
        _gruppi = gruppi;
    }

    /// <summary>Gruppi con le rispettive aziende (un'azienda può stare in più gruppi).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GruppoDto>>> GetGruppi(CancellationToken ct)
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var gruppi = await _gruppi.GetGruppiAsync(ct);

        return Ok(gruppi
            .OrderBy(g => g.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new GruppoDto(g.Id, g.Nome, g.DataFine is { } fine && fine < oggi, g.AziendeIds)));
    }
}
