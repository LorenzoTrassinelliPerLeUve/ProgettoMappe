using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Infrastructure.Data;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class VignetiController : ControllerBase
{
    private readonly ProgettoMappeDbContext _db;

    public VignetiController(ProgettoMappeDbContext db)
    {
        _db = db;
    }

    [HttpGet("aziende/{aziendaId:int}/vigneti")]
    public async Task<ActionResult<IEnumerable<VignetoDto>>> GetVignetiPerAzienda(int aziendaId, CancellationToken ct)
    {
        var vigneti = await _db.Vigneti
            .Where(v => v.AziendaId == aziendaId)
            .OrderBy(v => v.Nome)
            .Select(v => new VignetoDto(v.Id, v.AziendaId, v.Nome, v.Varieta, v.SuperficieEttari, v.GeometriaGeoJson))
            .ToListAsync(ct);

        return Ok(vigneti);
    }

    [HttpGet("vigneti/{id:int}")]
    public async Task<ActionResult<VignetoDto>> GetVigneto(int id, CancellationToken ct)
    {
        var vigneto = await _db.Vigneti
            .Where(v => v.Id == id)
            .Select(v => new VignetoDto(v.Id, v.AziendaId, v.Nome, v.Varieta, v.SuperficieEttari, v.GeometriaGeoJson))
            .FirstOrDefaultAsync(ct);

        return vigneto is null ? NotFound() : Ok(vigneto);
    }

    [HttpGet("vigneti/{vignetoId:int}/layer")]
    public async Task<ActionResult<IEnumerable<LayerMappaDto>>> GetLayerPerVigneto(int vignetoId, CancellationToken ct)
    {
        var layer = await _db.Layer
            .Where(l => l.VignetoId == vignetoId)
            .OrderBy(l => l.Nome)
            .Select(l => new LayerMappaDto(l.Id, l.VignetoId, l.Nome, l.Tipo.ToString(), l.Descrizione, l.DataRilievo, l.DatiGeoJson))
            .ToListAsync(ct);

        return Ok(layer);
    }
}
