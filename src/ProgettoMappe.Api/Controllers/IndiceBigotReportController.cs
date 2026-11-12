using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report «Indice Bigot» (task #189, pilota del portale al posto di Qlik): serve lo snapshot notturno
/// per azienda, senza mai interrogare 4Grapes durante la richiesta.
/// </summary>
[ApiController]
[Route("api")]
public class IndiceBigotReportController : ControllerBase
{
    private readonly SnapshotIndiceBigot _snapshot;
    private readonly LetturaSnapshotIndice _lettura;
    private readonly PerimetroAziende _perimetro;

    public IndiceBigotReportController(SnapshotIndiceBigot snapshot, LetturaSnapshotIndice lettura, PerimetroAziende perimetro)
    {
        _snapshot = snapshot;
        _lettura = lettura;
        _perimetro = perimetro;
    }

    /// <summary>Anagrafica e righe per parametro (tutti gli anni, vigneti e sottovigneti) dell'azienda.</summary>
    [HttpGet("aziende/{aziendaId:int}/indice-bigot")]
    public async Task<ActionResult<IndiceBigotReportDto>> GetAzienda(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        var dati = await _snapshot.AziendaAsync(aziendaId, ct);
        if (dati is null)
        {
            return Ok(new IndiceBigotReportDto(false, null, aziendaId, "", [], [], [], [], []));
        }

        return Ok(new IndiceBigotReportDto(true, dati.Snapshot, aziendaId, dati.Ente?.Nome ?? $"Azienda {aziendaId}",
            dati.Vigne.Select(v => new VignaReportDto(v.Id, v.Nome)).ToList(),
            dati.Vitigni.Select(v => new VitignoReportDto(v.Id, v.Nome)).ToList(),
            dati.Vigneti.Select(v => new VignetoReportDto(v.Id, v.Nome, v.IdVigna, v.IdVitigno, v.Superficie, v.AnnoImpianto)).ToList(),
            dati.Sottovigneti.Select(s => new SottovignetoReportDto(s.Id, s.Nome, s.IdVigneto)).ToList(),
            dati.Righe.Select(r => new RigaIndiceParametroDto(r.Anno, r.IdVigneto, r.IdSubVigneto, r.IdParametro,
                r.Min, r.Max, r.Avg, r.Valore, r.Score, r.Punteggio)).ToList()));
    }

    /// <summary>Stato dello snapshot: ora della lettura in uso, righe, lettura in corso, ultimo errore.</summary>
    [HttpGet("indice-bigot/stato")]
    public async Task<ActionResult<StatoSnapshotIndiceDto>> GetStato(CancellationToken ct) => Ok(await StatoAsync(ct));

    /// <summary>Rifà lo snapshot subito, in background: 202 se partito, 409 se ce n'è già uno in corso.</summary>
    [HttpPost("indice-bigot/ricarica")]
    public async Task<ActionResult<StatoSnapshotIndiceDto>> Ricarica([FromServices] IHostApplicationLifetime vita, CancellationToken ct)
    {
        var avviato = _lettura.Avvia(vita.ApplicationStopping);
        var stato = await StatoAsync(ct);
        return avviato ? Accepted(stato) : Conflict(stato);
    }

    private async Task<StatoSnapshotIndiceDto> StatoAsync(CancellationToken ct)
    {
        var info = await _snapshot.InfoAsync(ct);
        return new StatoSnapshotIndiceDto(info is not null, _lettura.InCorso, info?.Snapshot, _lettura.UltimoErrore, info?.Righe ?? 0);
    }
}
