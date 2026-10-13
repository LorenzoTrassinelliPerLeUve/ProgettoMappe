using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Rischio oidio/peronospora, vigoria e potenziale idrico (viste statistiche di 4Grapes).</summary>
[ApiController]
[Route("api")]
public class StatisticheVignetiController : ControllerBase
{
    private readonly CacheVignetiAziende _vigneti;
    private readonly IStatisticheVignetiRepository _statistiche;
    private readonly PerimetroAziende _perimetro;

    public StatisticheVignetiController(CacheVignetiAziende vigneti, IStatisticheVignetiRepository statistiche, PerimetroAziende perimetro)
    {
        _vigneti = vigneti;
        _statistiche = statistiche;
        _perimetro = perimetro;
    }

    /// <summary>
    /// Statistiche (tutti gli anni) dei soli vigneti dell'azienda. Non resta mai in attesa di
    /// 4Grapes: se i dati non sono ancora in memoria risponde subito con Pronto = false.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/statistiche")]
    public async Task<ActionResult<StatisticheAziendaDto>> GetStatistiche(int aziendaId, CancellationToken ct)
    {
        // Fuori perimetro: nessun vigneto, quindi nessun dato (come un'azienda inesistente).
        var vigneti = await _perimetro.ContieneAsync(aziendaId, ct)
            ? await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)
            : [];
        var statistiche = await _statistiche.GetPerVignetiAsync(vigneti.Select(v => v.Id).ToList(), ct);
        if (statistiche is null)
        {
            return Ok(StatisticheAziendaDto.NonPronte);
        }

        return Ok(new StatisticheAziendaDto(true,
            statistiche.Rischi.OrderBy(r => r.IdVigneto).ThenBy(r => r.Anno)
                .Select(r => new RischioVignetoDto(r.Anno, r.IdVigneto, r.Oidio, r.Peronospora)).ToList(),
            statistiche.Vigoria.OrderBy(r => r.IdVigneto).ThenBy(r => r.Anno)
                .Select(r => new VigoriaVignetoDto(r.Anno, r.IdVigneto, r.Punteggio)).ToList(),
            statistiche.PotenzialeIdrico.OrderBy(r => r.IdVigneto)
                .Select(r => new PotenzialeIdricoVignetoDto(r.IdVigneto, r.PotenzialeIdrico)).ToList(),
            _statistiche.Stato.UltimoAggiornamento));
    }

    /// <summary>Stato della copia delle statistiche: pronta, in caricamento, ora dell'ultimo aggiornamento.</summary>
    [HttpGet("statistiche/stato")]
    public ActionResult<StatoIndiciBigotDto> GetStato() => Ok(StatoDto());

    /// <summary>
    /// Ricarico a comando (Api interna, protetta dalla chiave condivisa): 202 se la rilettura è
    /// partita, 409 se ce n'era già una in corso; l'esito si legge poi da <c>statistiche/stato</c>.
    /// </summary>
    [HttpPost("statistiche/ricarica")]
    public ActionResult<StatoIndiciBigotDto> Ricarica([FromServices] IHostApplicationLifetime vita)
    {
        var avviato = _statistiche.AvviaRicarico(vita.ApplicationStopping);
        return avviato ? Accepted(StatoDto()) : Conflict(StatoDto());
    }

    private StatoIndiciBigotDto StatoDto()
    {
        var stato = _statistiche.Stato;
        return new StatoIndiciBigotDto(stato.Pronta, stato.InCaricamento, stato.UltimoAggiornamento, stato.UltimoErrore);
    }
}
