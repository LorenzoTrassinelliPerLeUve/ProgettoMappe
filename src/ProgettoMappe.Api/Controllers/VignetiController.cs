using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class VignetiController : ControllerBase
{
    private readonly CacheVignetiAziende _vigneti;
    private readonly PerimetroAziende _perimetro;

    public VignetiController(CacheVignetiAziende vigneti, PerimetroAziende perimetro)
    {
        _vigneti = vigneti;
        _perimetro = perimetro;
    }

    /// <summary>
    /// Elenco dei vigneti (ordinato per nome) e FeatureCollection GeoJSON pronta per MapLibre, da una
    /// sola lettura: la geometria viaggia solo nel GeoJSON (le geometrie mancanti o non valide vengono
    /// scartate). Un'azienda fuori perimetro ha zero vigneti, come un'azienda inesistente.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/vigneti")]
    public async Task<ActionResult<VignetiAziendaDto>> GetVignetiPerAzienda(int aziendaId, CancellationToken ct)
    {
        IReadOnlyList<Vigneto> vigneti = await _perimetro.ContieneAsync(aziendaId, ct)
            ? await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)
            : [];

        return Ok(new VignetiAziendaDto(
            vigneti.OrderBy(v => v.Nome).Select(Dto).ToList(),
            JsonSerializer.SerializeToElement(VignetoGeoJsonBuilder.BuildFeatureCollection(vigneti))));
    }

    private static VignetoDto Dto(Vigneto v) =>
        new(v.Id, v.AziendaId, v.Nome, v.Varieta, v.SuperficieEttari, !string.IsNullOrWhiteSpace(v.GeometriaGeoJson),
            v.VignaId, v.NomeVigna, v.VitignoId, v.GeometriaNonAttendibile, v.AnnoImpianto);
}
