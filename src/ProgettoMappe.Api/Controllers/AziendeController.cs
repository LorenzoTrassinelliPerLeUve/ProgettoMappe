using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AziendeController : ControllerBase
{
    private readonly IAziendeRepository _aziende;
    private readonly PerimetroAziende _perimetro;

    public AziendeController(IAziendeRepository aziende, PerimetroAziende perimetro)
    {
        _aziende = aziende;
        _perimetro = perimetro;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AziendaDto>>> GetAziende(CancellationToken ct)
    {
        var aziende = await _aziende.GetAziendeAsync(ct);
        var ammesse = await _perimetro.GetAziendeAsync(ct);

        return Ok(aziende
            .Where(a => ammesse is null || ammesse.Contains(a.Id))
            .OrderBy(a => a.Nome)
            .Select(a => new AziendaDto(a.Id, a.Nome, a.Comune, a.Provincia)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AziendaDto>> GetAzienda(int id, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(id, ct))
        {
            return NotFound();
        }

        var azienda = await _aziende.GetAziendaAsync(id, ct);

        return azienda is null
            ? NotFound()
            : Ok(new AziendaDto(azienda.Id, azienda.Nome, azienda.Comune, azienda.Provincia));
    }
}
