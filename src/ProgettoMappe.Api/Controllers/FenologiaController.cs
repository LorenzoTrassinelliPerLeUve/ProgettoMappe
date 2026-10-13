using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Fenologia dei vigneti: ultima fase BBCH rilevata per anno (rilievi di 4Grapes).</summary>
[ApiController]
[Route("api")]
public class FenologiaController : ControllerBase
{
    private readonly CacheVignetiAziende _vigneti;
    private readonly IFenologiaRepository _fenologia;
    private readonly PerimetroAziende _perimetro;

    public FenologiaController(CacheVignetiAziende vigneti, IFenologiaRepository fenologia, PerimetroAziende perimetro)
    {
        _vigneti = vigneti;
        _fenologia = fenologia;
        _perimetro = perimetro;
    }

    /// <summary>
    /// Fenologia (tutti gli anni) dei soli vigneti dell'azienda. Non resta mai in attesa di 4Grapes:
    /// se i dati non sono ancora in memoria risponde subito con Pronto = false.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/fenologia")]
    public async Task<ActionResult<FenologiaAziendaDto>> GetFenologia(int aziendaId, CancellationToken ct)
    {
        // Fuori perimetro: nessun vigneto, quindi nessun dato (come un'azienda inesistente).
        var vigneti = await _perimetro.ContieneAsync(aziendaId, ct)
            ? await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)
            : [];
        var fenologia = await _fenologia.GetPerVignetiAsync(vigneti.Select(v => v.Id).ToList(), ct);
        if (fenologia is null)
        {
            return Ok(FenologiaAziendaDto.NonPronta);
        }

        return Ok(new FenologiaAziendaDto(true,
            fenologia.OrderBy(f => f.IdVigneto).ThenBy(f => f.Anno)
                .Select(f => new FenologiaVignetoDto(f.Anno, f.IdVigneto, f.Bbch, f.Descrizione, f.Data,
                    f.Precedenti.Select(p => new FaseRilevataDto(p.Bbch, p.Descrizione, p.Data)).ToList()))
                .ToList(),
            _fenologia.Stato.UltimoAggiornamento));
    }

    /// <summary>Stato della copia della fenologia: pronta, in caricamento, ora dell'ultimo aggiornamento.</summary>
    [HttpGet("fenologia/stato")]
    public ActionResult<StatoIndiciBigotDto> GetStato() => Ok(StatoDto());

    /// <summary>Ricarico a comando: 202 se la rilettura è partita, 409 se ce n'era già una in corso.</summary>
    [HttpPost("fenologia/ricarica")]
    public ActionResult<StatoIndiciBigotDto> Ricarica([FromServices] IHostApplicationLifetime vita)
    {
        var avviato = _fenologia.AvviaRicarico(vita.ApplicationStopping);
        return avviato ? Accepted(StatoDto()) : Conflict(StatoDto());
    }

    private StatoIndiciBigotDto StatoDto()
    {
        var stato = _fenologia.Stato;
        return new StatoIndiciBigotDto(stato.Pronta, stato.InCaricamento, stato.UltimoAggiornamento, stato.UltimoErrore);
    }
}
