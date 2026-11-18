using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.BilancioIdrico;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// DSS Bilancio Idrico, fase 3 (task #335): i risultati del ricalcolo notturno per la pagina dell'azienda e la scheda del
/// vigneto, e il ricalcolo a comando. Si legge solo da <see cref="ArchivioRisultatiBilancio"/>: mai un calcolo dentro una GET.
/// </summary>
[ApiController]
public class BilancioIdricoRisultatiController(PerimetroAziende perimetro, ArchivioRisultatiBilancio risultati,
    ArchivioConfigurazioneBilancio configurazione) : ControllerBase
{
    /// <summary>Stato di oggi dei vigneti (dal più urgente), settori e shift, anomalie e data del calcolo.</summary>
    [HttpGet("api/bilancio-idrico/aziende/{aziendaId:int}/stato")]
    public async Task<ActionResult<BilancioAziendaDto>> Stato(int aziendaId, [FromServices] RicalcoloBilancioIdrico? ricalcolo, CancellationToken ct)
    {
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        var oggi = RicalcoloBilancioIdrico.OggiInItalia();
        var (riuscito, errore) = await risultati.UltimoCalcoloAsync(aziendaId, ct);
        var calcolo = VistaBilancio.Calcolo(riuscito, errore, ricalcolo?.InAttesa(aziendaId) ?? false);
        if (riuscito is null)
            return new BilancioAziendaDto(aziendaId, oggi, calcolo, [], [], [], []);

        var conf = await configurazione.LeggiAsync(aziendaId, ct);
        var righe = await risultati.VignetiAsync(aziendaId, oggi.AddDays(-7), oggi.AddDays(VistaBilancio.GiorniProssimi), ct: ct);
        var anomalie = VistaBilancio.Anomalie(riuscito.Riepilogo, righe);
        var settori = await risultati.SettoriAsync(aziendaId, oggi.AddDays(-7), oggi, ct);
        var shift = await risultati.ShiftAsync(aziendaId, oggi.AddDays(-7), oggi, ct);
        return new BilancioAziendaDto(aziendaId, oggi, calcolo, VistaBilancio.StatoVigneti(righe, oggi, anomalie),
            VistaBilancio.Settori(settori, conf, oggi), VistaBilancio.Shift(shift, conf, oggi), anomalie);
    }

    /// <summary>La stagione di un vigneto con tutte le grandezze, le anomalie e (se ci sono) i potenziali misurati.</summary>
    [HttpGet("api/bilancio-idrico/aziende/{aziendaId:int}/vigneti/{vignetoId:int}")]
    public async Task<ActionResult<BilancioVignetoDto>> Vigneto(int aziendaId, int vignetoId, [FromServices] RicalcoloBilancioIdrico? ricalcolo,
        [FromServices] IPotenzialiMisurati? misurati, CancellationToken ct)
    {
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        var oggi = RicalcoloBilancioIdrico.OggiInItalia();
        var (riuscito, errore) = await risultati.UltimoCalcoloAsync(aziendaId, ct);
        var calcolo = VistaBilancio.Calcolo(riuscito, errore, ricalcolo?.InAttesa(aziendaId) ?? false);
        if (riuscito is null)
            return new BilancioVignetoDto(vignetoId, oggi, calcolo, [], [], [], [], null);

        var righe = await risultati.VignetiAsync(aziendaId, riuscito.Da, riuscito.A, vignetoId, ct);
        var anomalie = VistaBilancio.Anomalie(riuscito.Riepilogo, righe);
        var oggiRiga = VistaBilancio.Oggi(righe, oggi);
        IReadOnlyList<PotenzialeMisuratoDto> potenziali = [];
        string? nota = "Potenziali misurati non disponibili in questa installazione.";
        if (misurati is not null)
            (potenziali, nota) = await misurati.LeggiAsync(aziendaId, vignetoId, riuscito.Da, oggi, ct);
        return new BilancioVignetoDto(vignetoId, oggi, calcolo, VistaBilancio.Giorni(righe, oggi),
            VistaBilancio.AnomalieVigneto(vignetoId, anomalie, oggiRiga), oggiRiga?.Settori.Select(s => s.IdSettore).ToList() ?? [],
            potenziali, nota);
    }

    /// <summary>Ricalcola subito l'azienda (pochi secondi) e risponde con il calcolo nuovo; 409 se un calcolo è già in corso.</summary>
    [HttpPost("api/bilancio-idrico/aziende/{aziendaId:int}/ricalcola")]
    public async Task<ActionResult<CalcoloBilancioDto>> Ricalcola(int aziendaId, [FromServices] RicalcoloBilancioIdrico? ricalcolo, CancellationToken ct)
    {
        if (ricalcolo is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "4Grapes non è configurato: il bilancio idrico non si calcola.");
        if (!await perimetro.ContieneAsync(aziendaId, ct))
            return NotFound();
        var (calcolo, errore) = await ricalcolo.EseguiAziendaAsync(aziendaId, ct);
        if (calcolo is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Calcolo non riuscito: {errore}");
        return VistaBilancio.Calcolo(calcolo, null, false)!;
    }

    /// <summary>Stato del giro notturno.</summary>
    [HttpGet("api/bilancio-idrico/ricalcolo")]
    public async Task<ActionResult<StatoRicalcoloBilancioDto>> StatoGiro([FromServices] RicalcoloBilancioIdrico? ricalcolo, CancellationToken ct)
    {
        var ultimi = await risultati.UltimiCalcoliAsync(ct);
        var s = ricalcolo?.Stato ?? new RicalcoloBilancioIdrico.StatoGiro(false, null, null, 0, 0, 0);
        return new StatoRicalcoloBilancioDto(s.InCorso, s.InizioUtc, s.FineUtc, s.Aziende, s.Fatte, s.Fallite,
            ultimi.Count(c => c.Errore is null), ultimi.Count == 0 ? null : ultimi.Max(c => c.CalcolatoIlUtc));
    }

    /// <summary>Giro intero a comando, in background: 202, 409 se ce n'è già uno.</summary>
    [HttpPost("api/bilancio-idrico/ricalcolo")]
    public IActionResult AvviaGiro([FromServices] RicalcoloBilancioIdrico? ricalcolo, [FromServices] IHostApplicationLifetime vita)
    {
        if (ricalcolo is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "4Grapes non è configurato.");
        return ricalcolo.AvviaTutto(vita.ApplicationStopping) ? Accepted() : Conflict("Un giro è già in corso.");
    }
}
