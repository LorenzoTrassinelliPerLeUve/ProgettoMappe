using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.BilancioIdrico;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// DSS Bilancio Idrico, fase 2 (task #333): configurazione nel database di Mappe (<see cref="ArchivioConfigurazioneBilancio"/>)
/// e calcolo di una stagione. Solo aziende del perimetro (fuori = 404). Ogni scrittura vuole l'autore (lo mette il Web
/// dall'utente entrato, come i filari: senza, 403) e diventa una versione nuova con data; niente pagine in questa fase.
/// </summary>
[ApiController]
[Route("api/bilancio-idrico/aziende/{aziendaId:int}")]
public class BilancioIdricoController(PerimetroAziende perimetro, ArchivioConfigurazioneBilancio archivio, CacheVignetiAziende vigneti,
    ILogger<BilancioIdricoController> logger) : ControllerBase
{
    /// <summary>La configurazione valida dell'azienda (ultima versione di ogni voce).</summary>
    [HttpGet("configurazione")]
    public async Task<ActionResult<ConfigurazioneAzienda>> Configurazione(int aziendaId, CancellationToken ct) =>
        await perimetro.ContieneAsync(aziendaId, ct) ? await archivio.LeggiAsync(aziendaId, ct) : NotFound();

    /// <summary>Le voci con versione, autore e data dell'ultima modifica (anche quelle tolte).</summary>
    [HttpGet("configurazione/voci")]
    public async Task<ActionResult<IReadOnlyList<VoceBilancioDto>>> Voci(int aziendaId, CancellationToken ct) =>
        await perimetro.ContieneAsync(aziendaId, ct) ? (await archivio.UltimeAsync(aziendaId, ct)).Select(Dto).ToList() : NotFound();

    /// <summary>Lo storico di una voce dell'azienda.</summary>
    [HttpGet("configurazione/{tipo}/storico")]
    public async Task<ActionResult<IReadOnlyList<VoceBilancioDto>>> Storico(int aziendaId, string tipo, [FromQuery] string chiave, CancellationToken ct)
    {
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        var storico = await archivio.StoricoAsync(tipo, chiave, ct);
        return storico.Where(v => v.IdEnte == aziendaId || v.IdEnte == 0).Select(Dto).ToList();
    }

    /// <summary>
    /// Scrive (o toglie) una voce. Settori, quote, impianti e shift devono essere dell'azienda; quote e stili su vigneti
    /// dell'azienda. Le tabelle del modello sono comuni a tutte le aziende.
    /// </summary>
    [HttpPut("configurazione/{tipo}")]
    public async Task<ActionResult<VoceBilancioDto>> Salva(int aziendaId, string tipo, SalvaVoceBilancioDto richiesta,
        [FromServices] RicalcoloBilancioIdrico? ricalcolo, CancellationToken ct)
    {
        var autore = richiesta.Autore?.Trim();
        if (string.IsNullOrEmpty(autore))
            return StatusCode(StatusCodes.Status403Forbidden, "Per cambiare la configurazione serve un utente entrato.");
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        if (VoceConfigurazione.DaJson(tipo, aziendaId, richiesta.Dati) is not { } voce)
            return BadRequest($"Voce «{tipo}» non valida per l'azienda {aziendaId}.");
        // Fase 3 (task #335, pagine di configurazione): numero 0 = settore, shift o irrigazione nuovi, il numero lo dà l'archivio.
        if (!richiesta.Elimina)
            voce = voce.Dati switch
            {
                SettoreIrriguo { IdSettore: 0 } s => VoceConfigurazione.Di(s with { IdSettore = await archivio.UltimoIdAsync(TipiVoce.Settore, ct) + 1 }),
                ShiftIrriguo { IdShift: 0 } s => VoceConfigurazione.Di(s with { IdShift = await archivio.UltimoIdAsync(TipiVoce.Shift, ct) + 1 }),
                IrrigazioneRegistrata { IdEvento: 0 } i => VoceConfigurazione.Di(i with { IdEvento = await archivio.UltimoIdAsync(TipiVoce.Irrigazione, ct) + 1 }),
                _ => voce,
            };

        var conf = await archivio.LeggiAsync(aziendaId, ct);
        var settoriAzienda = conf.Settori.Select(s => s.IdSettore).ToHashSet();
        var vignetiAzienda = (await vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)).Select(v => v.Id).ToHashSet();
        var errore = voce.Dati switch
        {
            QuotaVignetoSettore q when !settoriAzienda.Contains(q.IdSettore) => "settore di un'altra azienda",
            QuotaVignetoSettore q when !vignetiAzienda.Contains(q.IdVigneto) => "vigneto di un'altra azienda",
            QuotaVignetoSettore q when !richiesta.Elimina && conf.Quote.Where(x => x.IdVigneto == q.IdVigneto && x.IdSettore != q.IdSettore)
                .Sum(x => x.Percentuale) + q.Percentuale > 100 + 1e-9 => "le quote del vigneto supererebbero il 100%",
            ImpiantoSettore i when !settoriAzienda.Contains(i.IdSettore) => "settore di un'altra azienda",
            SettoreNelloShift s when !settoriAzienda.Contains(s.IdSettore) || conf.Shift.All(x => x.IdShift != s.IdShift) => "settore o shift di un'altra azienda",
            StileVinoVigneto s when !vignetiAzienda.Contains(s.IdVigneto) => "vigneto di un'altra azienda",
            CalibrazioneVigneto c when !vignetiAzienda.Contains(c.IdVigneto) => "vigneto di un'altra azienda",
            IrrigazioneRegistrata i when i.IdSettore is { } se && !settoriAzienda.Contains(se) => "settore di un'altra azienda",
            IrrigazioneRegistrata i when i.IdShift is { } sh && conf.Shift.All(x => x.IdShift != sh) => "shift di un'altra azienda",
            IrrigazioneRegistrata i when !richiesta.Elimina && i.DurataOre is not > 0 && i.VolumeMm is not > 0 && i.VolumeUtileMm is not > 0
                => "serve la durata in ore o il volume in mm",
            SettoreIrriguo s when (await archivio.StoricoAsync(TipiVoce.Settore, voce.Chiave, ct)).Any(v => v.IdEnte != aziendaId) => "numero di settore già usato",
            ShiftIrriguo s when (await archivio.StoricoAsync(TipiVoce.Shift, voce.Chiave, ct)).Any(v => v.IdEnte != aziendaId) => "numero di shift già usato",
            _ => null,
        };
        if (errore is not null)
            return BadRequest(errore);

        if (richiesta.Elimina)
            voce = voce with { Dati = null };
        var nota = richiesta.Nota is { Length: > 500 } n ? n[..500] : richiesta.Nota;
        await archivio.SalvaAsync([voce], autore.Length > 100 ? autore[..100] : autore, nota, ct);
        logger.LogInformation("Bilancio idrico, azienda {Azienda}: {Tipo} {Chiave} {Azione} da {Autore}", aziendaId, voce.Tipo, voce.Chiave,
            richiesta.Elimina ? "tolta" : "salvata", autore);
        // Fase 3 (task #335): la modifica si vede dopo il ricalcolo dell'azienda, in background. Le tabelle del modello valgono
        // per tutti: task #349, giro intero in background (Matteo: «ricalcolo quasi immediato»).
        if (voce.Tipo != TipiVoce.Modello)
            ricalcolo?.AvviaAzienda(aziendaId);
        else
            ricalcolo?.AvviaTutto(HttpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
        return Dto((await archivio.StoricoAsync(voce.Tipo, voce.Chiave, ct))[0]);
    }

    /// <summary>
    /// Calcola la stagione <paramref name="anno"/> dell'azienda con i dati di 4Grapes e la configurazione di Mappe e
    /// restituisce totali e anomalie dei dati (task #333, punto D). 503 senza connessione a 4Grapes.
    /// </summary>
    [HttpPost("calcola")]
    public async Task<ActionResult<RiepilogoCalcolo>> Calcola(int aziendaId, [FromQuery] int anno, [FromServices] CalcoloBilancioAzienda? calcolo,
        CancellationToken ct)
    {
        if (calcolo is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "4Grapes non è configurato.");
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        if (anno < 2000 || anno > DateTime.Today.Year + 1)
            return BadRequest("Anno non valido.");
        return (await calcolo.CalcolaAsync(aziendaId, anno, ct)).Riepilogo;
    }

    private static VoceBilancioDto Dto(VersioneVoce v) =>
        new(v.Tipo, v.Chiave, v.Versione, v.DatiJson is null ? null : JsonDocument.Parse(v.DatiJson).RootElement.Clone(), v.Autore,
            v.CreatoIlUtc, v.Nota);
}
