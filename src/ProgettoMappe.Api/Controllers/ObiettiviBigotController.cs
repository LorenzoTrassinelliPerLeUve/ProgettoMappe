using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Reportistica;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Obiettivi dei parametri dell'Indice Bigot (task #419, tappa R6 della Reportistica, PROGETTO.md §5.1), nel SQLite di Mappe
/// (<see cref="ArchivioReportistica"/>): predefinito (ente 0) → azienda → vigneto, per annata. Le scritture di chi è in consultazione
/// le ferma già <c>ControlloConsultazione</c>; l'autore lo mette il Web dall'utente entrato.
/// </summary>
[ApiController]
[Route("api/indice-bigot/obiettivi")]
public class ObiettiviBigotController(ArchivioReportistica archivio, PerimetroAziende perimetro, SnapshotIndiceBigot snapshot,
    TimeProvider? orologio = null, EsperienzeUtenti? esperienze = null) : ControllerBase
{
    private readonly TimeProvider _orologio = orologio ?? TimeProvider.System;

    public const string MessaggioPredefiniti = "I predefiniti valgono per tutte le aziende: li cambia solo Perleuve.";

    /// <summary>Gli obiettivi dell'annata: predefiniti e, con <paramref name="azienda"/>, quelli dell'azienda e dei suoi vigneti.</summary>
    [HttpGet]
    public async Task<ActionResult<ObiettiviBigotDto>> Get(int anno, int? azienda, CancellationToken ct)
    {
        if (anno is < ObiettiviBigot.AnnoDa or > ObiettiviBigot.AnnoA)
            return BadRequest("Annata non valida.");
        if (azienda is { } id && !await perimetro.ContieneAsync(id, ct))
            return NotFound();
        return await archivio.ObiettiviAsync(anno, azienda ?? 0, ct);
    }

    /// <summary>Le versioni di un obiettivo, dalla più recente.</summary>
    [HttpGet("storico")]
    public async Task<ActionResult<IReadOnlyList<ObiettivoBigotDto>>> Storico(int anno, string parametro, int? azienda, int? vigneto, CancellationToken ct)
    {
        if (ObiettiviBigot.Normalizza(parametro) is not { } p)
            return BadRequest("Parametro non valido.");
        if (azienda is { } id && !await perimetro.ContieneAsync(id, ct))
            return NotFound();
        return Ok(await archivio.StoricoObiettivoAsync(azienda ?? 0, azienda is null ? 0 : vigneto ?? 0, anno, p, ct));
    }

    [HttpPut]
    public async Task<ActionResult<ObiettiviBigotDto>> Salva(SalvaObiettivoBigotDto richiesta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Autore))
            return StatusCode(StatusCodes.Status403Forbidden, "Serve un utente entrato per cambiare gli obiettivi.");
        if (ObiettiviBigot.Controlla(richiesta) is { } errore)
            return BadRequest(errore);
        if (await ControllaAziendaAsync(richiesta.IdEnte, richiesta.IdVigneto, ct) is { } rifiuto)
            return rifiuto;

        await archivio.SalvaObiettivoAsync(richiesta with { Parametro = ObiettiviBigot.Normalizza(richiesta.Parametro)! },
            richiesta.Autore.Trim(), _orologio.GetUtcNow(), ct);
        return await archivio.ObiettiviAsync(richiesta.Anno, richiesta.IdEnte, ct);
    }

    /// <summary>«Copia dall'annata scorsa»: solo gli obiettivi che nell'annata nuova mancano.</summary>
    [HttpPost("copia")]
    public async Task<ActionResult<ObiettiviBigotDto>> Copia(CopiaObiettiviBigotDto richiesta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Autore))
            return StatusCode(StatusCodes.Status403Forbidden, "Serve un utente entrato per cambiare gli obiettivi.");
        if (richiesta.DaAnno is < ObiettiviBigot.AnnoDa or > ObiettiviBigot.AnnoA || richiesta.AdAnno is < ObiettiviBigot.AnnoDa or > ObiettiviBigot.AnnoA
            || richiesta.DaAnno == richiesta.AdAnno)
            return BadRequest("Annate non valide.");
        if (await ControllaAziendaAsync(richiesta.IdEnte, 0, ct) is { } rifiuto)
            return rifiuto;

        await archivio.CopiaObiettiviAsync(richiesta.IdEnte, richiesta.DaAnno, richiesta.AdAnno, richiesta.Autore.Trim(), _orologio.GetUtcNow(), ct);
        return await archivio.ObiettiviAsync(richiesta.AdAnno, richiesta.IdEnte, ct);
    }

    /// <summary>L'azienda nel perimetro e il vigneto suo (dall'anagrafica dello snapshot dell'indice).</summary>
    private async Task<ActionResult?> ControllaAziendaAsync(int idEnte, int idVigneto, CancellationToken ct)
    {
        if (idEnte == 0)
            return await InternoAsync(ct) ? null : StatusCode(StatusCodes.Status403Forbidden, MessaggioPredefiniti);
        if (!await perimetro.ContieneAsync(idEnte, ct))
            return NotFound();
        if (idVigneto == 0)
            return null;
        if (await snapshot.AziendaAsync(idEnte, ct) is not { } dati)
            return Conflict("I dati dell'indice non sono ancora pronti: riprova fra qualche minuto.");
        return dati.Vigneti.Any(v => v.Id == idVigneto) ? null : BadRequest("Il vigneto non è di questa azienda.");
    }

    /// <summary>L'utente di <c>X-Mappe-Utente</c> è un interno Perleuve (senza il servizio delle esperienze, solo nei test: sì).</summary>
    private async Task<bool> InternoAsync(CancellationToken ct)
    {
        if (esperienze is null)
            return true;
        if (!Request.Headers.TryGetValue(PortaleControllerBase.IntestazioneUtente, out var valore) || !int.TryParse(valore, out var idUtente))
            return false;
        return (await esperienze.GetAsync(idUtente, ct))?.Esperienza == TipiEsperienza.Interno;
    }
}
