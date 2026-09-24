using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AziendeController : ControllerBase
{
    private readonly IAziendeRepository _aziende;

    public AziendeController(IAziendeRepository aziende)
    {
        _aziende = aziende;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AziendaDto>>> GetAziende(CancellationToken ct)
    {
        var aziende = await _aziende.GetAziendeAsync(ct);

        return Ok(aziende
            .OrderBy(a => a.Nome)
            .Select(a => new AziendaDto(a.Id, a.Nome, a.Comune, a.Provincia)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AziendaDto>> GetAzienda(int id, CancellationToken ct)
    {
        var azienda = await _aziende.GetAziendaAsync(id, ct);

        return azienda is null
            ? NotFound()
            : Ok(new AziendaDto(azienda.Id, azienda.Nome, azienda.Comune, azienda.Provincia));
    }
}
