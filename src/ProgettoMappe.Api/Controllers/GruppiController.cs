using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Servizi;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GruppiController : ControllerBase
{
    private readonly PerimetroAziende _perimetro;

    public GruppiController(PerimetroAziende perimetro)
    {
        _perimetro = perimetro;
    }

    /// <summary>Gruppi del perimetro con le rispettive aziende (un'azienda può stare in più gruppi).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GruppoDto>>> GetGruppi(CancellationToken ct)
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var gruppi = await _perimetro.GetGruppiAsync(ct);

        return Ok(gruppi
            .OrderBy(g => g.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new GruppoDto(g.Id, g.Nome, g.DataFine is { } fine && fine < oggi, g.AziendeIds)));
    }
}
