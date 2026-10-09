using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class VignetiController : ControllerBase
{
    private readonly IVignetiRepository _vigneti;
    private readonly PerimetroAziende _perimetro;

    public VignetiController(IVignetiRepository vigneti, PerimetroAziende perimetro)
    {
        _vigneti = vigneti;
        _perimetro = perimetro;
    }

    [HttpGet("aziende/{aziendaId:int}/vigneti")]
    public async Task<ActionResult<IEnumerable<VignetoDto>>> GetVignetiPerAzienda(int aziendaId, CancellationToken ct)
    {
        var vigneti = await VignetiDelPerimetroAsync(aziendaId, ct);

        return Ok(vigneti
            .OrderBy(v => v.Nome)
            .Select(VignetoDto.Da));
    }

    /// <summary>FeatureCollection GeoJSON pronta per MapLibre: le geometrie mancanti o non valide vengono scartate.</summary>
    [HttpGet("aziende/{aziendaId:int}/vigneti/geojson")]
    public async Task<ActionResult<object>> GetVignetiGeoJson(int aziendaId, CancellationToken ct)
    {
        var vigneti = await VignetiDelPerimetroAsync(aziendaId, ct);

        return Ok(VignetoGeoJsonBuilder.BuildFeatureCollection(vigneti));
    }

    [HttpGet("vigneti/{id:int}")]
    public async Task<ActionResult<VignetoDto>> GetVigneto(int id, CancellationToken ct)
    {
        var vigneto = await _vigneti.GetVignetoAsync(id, ct);

        return vigneto is null || !await _perimetro.ContieneAsync(vigneto.AziendaId, ct)
            ? NotFound()
            : Ok(VignetoDto.Da(vigneto));
    }

    /// <summary>Un'azienda fuori perimetro ha zero vigneti, come un'azienda inesistente.</summary>
    private async Task<IReadOnlyList<Vigneto>> VignetiDelPerimetroAsync(int aziendaId, CancellationToken ct) =>
        await _perimetro.ContieneAsync(aziendaId, ct) ? await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct) : [];
}
