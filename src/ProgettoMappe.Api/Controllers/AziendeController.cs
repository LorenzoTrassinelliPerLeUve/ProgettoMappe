using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Infrastructure.Data;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AziendeController : ControllerBase
{
    private readonly ProgettoMappeDbContext _db;

    public AziendeController(ProgettoMappeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AziendaDto>>> GetAziende(CancellationToken ct)
    {
        var aziende = await _db.Aziende
            .OrderBy(a => a.Nome)
            .Select(a => new AziendaDto(a.Id, a.Nome, a.Comune, a.Provincia))
            .ToListAsync(ct);

        return Ok(aziende);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AziendaDto>> GetAzienda(int id, CancellationToken ct)
    {
        var azienda = await _db.Aziende
            .Where(a => a.Id == id)
            .Select(a => new AziendaDto(a.Id, a.Nome, a.Comune, a.Provincia))
            .FirstOrDefaultAsync(ct);

        return azienda is null ? NotFound() : Ok(azienda);
    }
}
