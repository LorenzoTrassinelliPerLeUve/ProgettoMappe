using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class IndiciBigotController : ControllerBase
{
    private readonly CacheVignetiAziende _vigneti;
    private readonly IIndiciBigotRepository _indici;
    private readonly PerimetroAziende _perimetro;

    public IndiciBigotController(CacheVignetiAziende vigneti, IIndiciBigotRepository indici, PerimetroAziende perimetro)
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
            .ToList(), _indici.UltimoAggiornamento));
    }

    /// <summary>Stato della copia degli indici: pronta, in caricamento, ora dell'ultimo aggiornamento.</summary>
    [HttpGet("indici-bigot/stato")]
    public ActionResult<StatoIndiciBigotDto> GetStato() => Ok(StatoDto());

    /// <summary>
    /// Ricarico a comando (Api interna, protetta dalla chiave condivisa): la rilettura parte in
    /// background e la risposta arriva subito. 202 se è partita, 409 se ce n'era già una in corso;
    /// in entrambi i casi con lo stato. L'esito si legge poi da <c>indici-bigot/stato</c>.
    /// </summary>
    [HttpPost("indici-bigot/ricarica")]
    public ActionResult<StatoIndiciBigotDto> Ricarica([FromServices] IHostApplicationLifetime vita)
    {
        var avviato = _indici.AvviaRicarico(vita.ApplicationStopping);
        return avviato ? Accepted(StatoDto()) : Conflict(StatoDto());
    }

    private StatoIndiciBigotDto StatoDto()
    {
        var stato = _indici.Stato;
        return new StatoIndiciBigotDto(stato.Pronta, stato.InCaricamento, stato.UltimoAggiornamento, stato.UltimoErrore);
    }
}
