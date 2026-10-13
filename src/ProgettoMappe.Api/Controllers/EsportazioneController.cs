using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api")]
public class EsportazioneController : ControllerBase
{
    private static readonly TimeZoneInfo FusoItalia = Fuso();

    private readonly CacheVignetiAziende _vigneti;
    private readonly IAziendeRepository _aziende;
    private readonly IIndiciBigotRepository _indici;
    private readonly IStatisticheVignetiRepository? _statistiche;
    private readonly IFenologiaRepository? _fenologia;
    private readonly PerimetroAziende _perimetro;
    private readonly TimeProvider _orologio;
    private readonly ILogger _logger;

    public EsportazioneController(CacheVignetiAziende vigneti, IAziendeRepository aziende, IIndiciBigotRepository indici,
        PerimetroAziende perimetro, TimeProvider? orologio = null, IStatisticheVignetiRepository? statistiche = null,
        IFenologiaRepository? fenologia = null, ILogger<EsportazioneController>? logger = null)
    {
        _vigneti = vigneti;
        _aziende = aziende;
        _indici = indici;
        _perimetro = perimetro;
        _orologio = orologio ?? TimeProvider.System;
        _statistiche = statistiche;
        _fenologia = fenologia;
        _logger = logger ?? (ILogger)Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Vigneti dell'azienda (con geometria) come file GeoJSON, KML o shapefile zip, nome
    /// azienda-AAAAMMGG. <paramref name="ids"/> (separati da virgola) restringe ai vigneti filtrati
    /// nella mappa; <paramref name="anno"/> aggiunge indici BIGOT, rischi, vigoria e fenologia di quell'anno
    /// (il potenziale idrico, senza anno, c'è sempre). Un'azienda
    /// fuori perimetro o inesistente risponde 404, un formato sconosciuto o ids illeggibili 400.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/vigneti/esporta")]
    public Task<IActionResult> Esporta(int aziendaId, [FromQuery] string? formato, [FromQuery] int? anno,
        [FromQuery] string? ids, CancellationToken ct) =>
        EsportaAsync(aziendaId, formato, anno, ids, null, ct);

    /// <summary>
    /// Come il GET, con il confronto fra due anni calcolato dal Web (che ha classi e soglie): le
    /// colonne conf_param, anno_da, variaz e var_classe. Un confronto senza anno o anno uguale → 400.
    /// </summary>
    [HttpPost("aziende/{aziendaId:int}/vigneti/esporta")]
    public Task<IActionResult> EsportaConConfronto(int aziendaId, [FromQuery] string? formato, [FromQuery] int? anno,
        [FromQuery] string? ids, [FromBody] ConfrontoEsportatoDto confronto, CancellationToken ct) =>
        anno is null || confronto.AnnoDa == anno || string.IsNullOrWhiteSpace(confronto.Parametro)
            ? Task.FromResult<IActionResult>(BadRequest("confronto: serve un anno diverso da anno_da e il parametro"))
            : EsportaAsync(aziendaId, formato, anno, ids, confronto, ct);

    private async Task<IActionResult> EsportaAsync(int aziendaId, string? formato, int? anno, string? ids,
        ConfrontoEsportatoDto? confronto, CancellationToken ct)
    {
        if (!EsportazioneVigneti.TryFormato(formato, out var formatoFile))
        {
            return BadRequest("formato: geojson, kml o shp");
        }

        HashSet<int>? scelti = null;
        if (!string.IsNullOrWhiteSpace(ids))
        {
            scelti = [];
            foreach (var parte in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(parte, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    return BadRequest("ids: numeri separati da virgola");
                }
                scelti.Add(id);
            }
        }

        if (!await _perimetro.ContieneAsync(aziendaId, ct) || await _aziende.GetAziendaAsync(aziendaId, ct) is not { } azienda)
        {
            return NotFound();
        }

        IEnumerable<Vigneto> vigneti = (await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)).OrderBy(v => v.Nome);
        if (scelti is not null)
        {
            vigneti = vigneti.Where(v => scelti.Contains(v.Id));
        }
        var elenco = vigneti.ToList();

        Dictionary<int, IndiceBigotVigneto> indici = [];
        if (anno is not null
            && await _indici.GetPerVignetiAsync(elenco.Select(v => v.Id).ToList(), ct) is { } righe)
        {
            indici = righe.Where(r => r.Anno == anno).DistinctBy(r => r.IdVigneto).ToDictionary(r => r.IdVigneto);
        }

        // Statistiche 4Grapes: rischi e vigoria dell'anno scelto, potenziale idrico sempre (non ha anno).
        var statistiche = _statistiche is null ? null
            : await _statistiche.GetPerVignetiAsync(elenco.Select(v => v.Id).ToList(), ct);
        var rischi = (statistiche?.Rischi ?? []).Where(r => r.Anno == anno).DistinctBy(r => r.IdVigneto).ToDictionary(r => r.IdVigneto);
        var vigoria = (statistiche?.Vigoria ?? []).Where(r => r.Anno == anno).DistinctBy(r => r.IdVigneto).ToDictionary(r => r.IdVigneto);
        var potenziale = (statistiche?.PotenzialeIdrico ?? []).DistinctBy(r => r.IdVigneto).ToDictionary(r => r.IdVigneto);

        // Fenologia dell'anno scelto: l'ultima fase rilevata e il suo giorno.
        var fenologia = anno is null || _fenologia is null ? null
            : await _fenologia.GetPerVignetiAsync(elenco.Select(v => v.Id).ToList(), ct);
        var fasi = (fenologia ?? []).Where(f => f.Anno == anno).DistinctBy(f => f.IdVigneto).ToDictionary(f => f.IdVigneto);

        var oggi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_orologio.GetUtcNow(), FusoItalia).DateTime);

        // Ultimo rilievo (senza anno): se 4Grapes non risponde il file esce lo stesso, senza.
        Dictionary<int, UltimoRilievo> rilievi = [];
        try
        {
            rilievi = (await _vigneti.GetUltimiRilieviAsync(aziendaId, ct)).Rilievi
                .GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.MaxBy(r => r.Data)!);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Esportazione dell'azienda {AziendaId} senza ultimi rilievi: lettura fallita", aziendaId);
        }

        // Ultimo trattamento e linea di difesa (senza anno): anche questi, se mancano, il file esce lo stesso.
        Dictionary<int, UltimoTrattamento> trattamenti = [];
        Dictionary<int, LineaDifesaVigneto> linee = [];
        try
        {
            var difesa = (await _vigneti.GetDifesaAsync(await _perimetro.GetAziendeAsync(ct), ct)).Difesa;
            trattamenti = difesa.Trattamenti.GroupBy(t => t.IdVigneto).ToDictionary(g => g.Key, g => g.MaxBy(t => t.Data)!);
            linee = difesa.Linee.DistinctBy(l => l.IdVigneto).ToDictionary(l => l.IdVigneto);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Esportazione dell'azienda {AziendaId} senza trattamenti: lettura fallita", aziendaId);
        }

        int? GiorniDa(DateTime? data) => data is { } d ? Math.Max(0, oggi.DayNumber - DateOnly.FromDateTime(d).DayNumber) : null;

        var variazioni = (confronto?.Vigneti ?? []).DistinctBy(v => v.IdVigneto).ToDictionary(v => v.IdVigneto);
        var esportati = elenco
            .Select(v =>
            {
                var rilievo = rilievi.GetValueOrDefault(v.Id);
                var trattamento = trattamenti.GetValueOrDefault(v.Id);
                return new VignetoEsportato(v, azienda.Nome, anno, indici.GetValueOrDefault(v.Id),
                    rischi.GetValueOrDefault(v.Id), vigoria.GetValueOrDefault(v.Id), potenziale.GetValueOrDefault(v.Id),
                    confronto, variazioni.GetValueOrDefault(v.Id), fasi.GetValueOrDefault(v.Id),
                    rilievo, rilievo is null ? null : oggi.DayNumber - DateOnly.FromDateTime(rilievo.Data).DayNumber,
                    trattamento, GiorniDa(trattamento?.Data), linee.GetValueOrDefault(v.Id));
            })
            .ToList();

        // Sottovigneti dei vigneti esportati, strato a parte; senza, se 4Grapes non risponde.
        var nomiVigneti = elenco.ToDictionary(v => v.Id, v => v.Nome);
        List<SottovignetoEsportato> sottovigneti = [];
        try
        {
            sottovigneti = (await _vigneti.GetSottovignetiPerAziendaAsync(aziendaId, ct))
                .Where(s => nomiVigneti.ContainsKey(s.IdVigneto))
                .Select(s => new SottovignetoEsportato(s, azienda.Nome, nomiVigneti[s.IdVigneto]))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Esportazione dell'azienda {AziendaId} senza sottovigneti: lettura fallita", aziendaId);
        }

        var nomeBase = EsportazioneVigneti.NomeBase(azienda.Nome, aziendaId, oggi);
        return File(EsportazioneVigneti.Scrivi(formatoFile, nomeBase, esportati, sottovigneti),
            EsportazioneVigneti.TipoContenuto(formatoFile),
            EsportazioneVigneti.NomeFile(nomeBase, formatoFile));
    }

    private static TimeZoneInfo Fuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
