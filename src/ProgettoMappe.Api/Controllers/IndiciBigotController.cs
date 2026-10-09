using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class IndiciBigotController : ControllerBase
{
    private readonly IVignetiRepository _vigneti;
    private readonly IIndiciBigotRepository _indici;
    private readonly PerimetroAziende _perimetro;

    public IndiciBigotController(IVignetiRepository vigneti, IIndiciBigotRepository indici, PerimetroAziende perimetro)
    {
        _vigneti = vigneti;
        _indici = indici;
        _perimetro = perimetro;
    }

    /// <summary>
    /// Indici BIGOT (tutti gli anni) dei vigneti dell'azienda. Non resta mai in attesa della
    /// vista 4Grapes: se i dati non sono ancora in memoria risponde subito con Pronto = false.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/indici-bigot")]
    public async Task<ActionResult<IndiciBigotAziendaDto>> GetIndiciBigot(int aziendaId, CancellationToken ct)
    {
        // Fuori perimetro: nessun vigneto, quindi nessun indice (come un'azienda inesistente).
        var vigneti = await _perimetro.ContieneAsync(aziendaId, ct)
            ? await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)
            : [];
        var indici = await _indici.GetPerVignetiAsync(vigneti.Select(v => v.Id).ToList(), ct);

        if (indici is null)
        {
            return Ok(new IndiciBigotAziendaDto(false, []));
        }

        return Ok(new IndiciBigotAziendaDto(true, indici
            .OrderBy(i => i.IdVigneto).ThenBy(i => i.Anno)
            .Select(i => new IndiceBigotDto(i.Anno, i.IdVigneto, i.NumParametri, i.Punteggio, i.Peso, i.Vigoria,
                i.Sfe, i.Produzione, i.SfeKg, i.EtaVigneto, i.Morfologia, i.Ampelopatie, i.RegimeIdrico, i.Biodiversita))
            .ToList()));
    }
}
