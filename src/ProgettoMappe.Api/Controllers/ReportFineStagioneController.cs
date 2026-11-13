using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Report fine stagione (task #201): il meteo giornaliero per vigneto dell'azienda e i controlli sui dati. Si legge
/// dallo snapshot notturno del dba (con lo storico); se non c'è, il meteo viene da 4Grapes (solo dal 2026).
/// Fuori perimetro 404; se 4Grapes non risponde 503.
/// </summary>
[ApiController]
[Route("api/aziende/{aziendaId:int}/report-fine-stagione")]
public class ReportFineStagioneController : ControllerBase
{
    private readonly IFonteMeteoReport _fonte;
    private readonly SnapshotReportFineStagione _snapshot;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<ReportFineStagioneController> _logger;

    public ReportFineStagioneController(IFonteMeteoReport fonte, SnapshotReportFineStagione snapshot, PerimetroAziende perimetro,
        ILogger<ReportFineStagioneController> logger)
    {
        _fonte = fonte;
        _snapshot = snapshot;
        _perimetro = perimetro;
        _logger = logger;
    }

    [HttpGet("meteo")]
    public async Task<ActionResult<MeteoReportFineStagioneDto>> GetMeteo(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        try
        {
            var letto = await _snapshot.LeggiMeteoAsync(aziendaId, ct) ?? await _fonte.LeggiAsync(aziendaId, ct);
            return Ok(new MeteoReportFineStagioneDto(
                letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)).ToList(),
                letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione,
                    g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)).ToList(),
                letto.StoricoDisponibile,
                letto.Snapshot));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Meteo del report fine stagione non leggibile da 4Grapes (azienda {Azienda})", aziendaId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>Suolo e scottature (snapshot, tappa 3); 204 se lo snapshot non c'è.</summary>
    [HttpGet("campo")]
    public async Task<ActionResult<CampoReportFineStagioneDto>> GetCampo(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        return await _snapshot.LeggiCampoAsync(aziendaId, ct) is { } c ? Ok(Dto(c)) : NoContent();
    }

    /// <summary>I dati di campo letti dallo snapshot nel DTO della pagina (anche per i test sui dati veri).</summary>
    public static CampoReportFineStagioneDto Dto(ProgettoMappe.Domain.Entities.CampoReportLetto c)
    {
        static ScottaturaDto Dto(ProgettoMappe.Domain.Entities.ScottaturaVigneto s) => new(s.IdVigneto, s.Anno, s.Organo, s.Intensita, s.Diffusione);
        return new CampoReportFineStagioneDto(c.Superfici,
            c.Suolo.Select(s => new SuoloVignetoDto(s.IdVigneto, s.Nome, s.Vigna, s.CapacitaDiCampo, s.AcquaDisponibile,
                s.PuntoDiAppassimento, s.Profondita, s.Pietrosita, s.AwcRealeVigneto, s.AwcRealeVigna, s.Conducibilita)).ToList(),
            c.ScottatureVista.Select(Dto).ToList(),
            c.ScottatureRilievi.Select(Dto).ToList(),
            c.Vigoria.Select(v => new VigoriaDto(v.IdVigneto, v.Anno, v.LegnoPotatura, v.SfePerMetro, v.StatoVegetativo,
                v.GestioneChioma, v.Punteggio)).ToList(),
            c.Vigneti.Select(v => new VignetoFineStagioneDto(v.IdVigneto, v.Nome, v.Vigna, v.Vitigno, v.AnnoImpianto, v.Superficie,
                v.NumeroPianteContate, v.SullaFila, v.TraFila)).ToList(),
            c.Produzione.Select(p => new ProduzioneDto(p.IdVigneto, p.Anno, p.Stima, p.RealeVigneto, p.RealeEttaro,
                p.PercFallanze, p.PercInAllevamento)).ToList(),
            (c.Rilievi ?? []).Select(r => new RilievoFineStagioneDto(r.IdVigneto, r.IdSubVigneto, r.Data, r.Anno, r.TipoRilievo, r.Organo,
                r.EtaEpoca, r.NrOrgani, r.PercDiffusione, r.PercIntensita, r.NrFormeMobiliNidi, r.CatturePerTrappola, r.SfePerVite,
                r.StatoIdrico, r.Bbch, r.AltezzaChioma, r.LarghezzaChioma, r.DistanzaPiante, r.SpaziVuoti, r.FormaAllevamento,
                r.GestioneChioma, r.IdUtente, r.Note, r.PotenzialeIdrico)).ToList(),
            (c.Collezione ?? []).Select(x => new CollezioneEscaDto(x.IdVigneto, x.Anno, x.Valore, x.NumOrgani)).ToList(),
            (c.Parametri ?? []).Select(x => new ParametroIndiceFineStagioneDto(x.IdVigneto, x.Anno, x.Parametro, x.Valore)).ToList(),
            c.GestioniChioma ?? new Dictionary<int, string>(),
            c.SottoVigneti ?? new Dictionary<int, string>(),
            c.Sanita is not { } s ? null : new SanitaFineStagioneDto(
                s.Conteggi.Select(x => new ConteggioRilieviDto(x.Anno, x.TipoRilievo, x.Rilievi, x.Organi)).ToList(),
                s.NomiTipi, s.VignetiMonitorati,
                s.Fasi.Select(x => new FaseFenologicaDto(x.IdVigneto, x.Anno, x.Fase, x.Giorno)).ToList()));
    }

    /// <summary>I controlli del dba sui dati (snapshot); lista vuota se lo snapshot non c'è.</summary>
    [HttpGet("controlli")]
    public async Task<ActionResult<IReadOnlyList<ControlloReportDto>>> GetControlli(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return NotFound();
        }

        var controlli = await _snapshot.LeggiControlliAsync(aziendaId, ct) ?? [];
        return Ok(controlli.Select(c => new ControlloReportDto(c.Codice, c.Titolo, c.Perche, c.Gravita, c.Annata, c.Colonne, c.Righe)).ToList());
    }
}
