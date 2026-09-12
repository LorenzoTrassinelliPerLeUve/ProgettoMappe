using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class VignetiController : ControllerBase
{
    private readonly IVignetiRepository _vigneti;

    public VignetiController(IVignetiRepository vigneti)
    {
        _vigneti = vigneti;
    }

    [HttpGet("aziende/{aziendaId:int}/vigneti")]
    public async Task<ActionResult<IEnumerable<VignetoDto>>> GetVignetiPerAzienda(int aziendaId, CancellationToken ct)
    {
        var vigneti = await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct);

        return Ok(vigneti
            .OrderBy(v => v.Nome)
            .Select(v => new VignetoDto(v.Id, v.AziendaId, v.Nome, v.Varieta, v.SuperficieEttari, v.GeometriaGeoJson)));
    }

    /// <summary>FeatureCollection GeoJSON pronta per MapLibre: le geometrie mancanti o non valide vengono scartate.</summary>
    [HttpGet("aziende/{aziendaId:int}/vigneti/geojson")]
    public async Task<ActionResult<object>> GetVignetiGeoJson(int aziendaId, CancellationToken ct)
    {
        var vigneti = await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct);

        return Ok(VignetoGeoJsonBuilder.BuildFeatureCollection(vigneti));
    }

    [HttpGet("vigneti/{id:int}")]
    public async Task<ActionResult<VignetoDto>> GetVigneto(int id, CancellationToken ct)
    {
        var vigneto = await _vigneti.GetVignetoAsync(id, ct);

        return vigneto is null
            ? NotFound()
            : Ok(new VignetoDto(vigneto.Id, vigneto.AziendaId, vigneto.Nome, vigneto.Varieta, vigneto.SuperficieEttari, vigneto.GeometriaGeoJson));
    }
}
