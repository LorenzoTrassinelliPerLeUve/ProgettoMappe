using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// NDVI da satellite per vigneto (task #84), dal database SQLite del programma notturno <c>ndvi/</c>.
/// Se il database non c'è ancora la risposta dice «non calcolato» (<c>Calcolato = false</c>), non è un errore.
/// </summary>
[ApiController]
[Route("api")]
public class NdviController : ControllerBase
{
    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<NdviController> _logger;
    private readonly TimeProvider _orologio;

    public NdviController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<NdviController> logger, TimeProvider? orologio = null)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    /// <summary>
    /// Una riga per ogni vigneto dell'azienda: l'ultimo passaggio pulito con un valore e la serie
    /// dell'anno <paramref name="anno"/> (assente = anno in corso) e del precedente.
    /// 503 se 4Grapes (elenco dei vigneti) o il database non si leggono.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/ndvi")]
    public async Task<ActionResult<NdviAziendaDto>> GetNdvi(int aziendaId, [FromQuery] int? anno, CancellationToken ct)
    {
        var annoSerie = anno is >= 2000 and <= 2100 ? anno.Value : MeteoController.Oggi(_orologio.GetUtcNow()).Year;
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(new NdviAziendaDto(true, true, annoSerie, []));
        }

        var letto = await LeggiAsync(aziendaId, ct);
        return letto is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable)
            : Ok(Componi(letto.Value.Vigneti, letto.Value.Ndvi.Ndvi, annoSerie, letto.Value.Ndvi.Letto));
    }

    /// <summary>I passaggi elaborati per l'azienda (puliti e no), dal più recente.</summary>
    [HttpGet("aziende/{aziendaId:int}/ndvi/passaggi")]
    public async Task<ActionResult<PassaggiNdviDto>> GetPassaggi(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(new PassaggiNdviDto(true, []));
        }

        var letto = await LeggiAsync(aziendaId, ct);
        return letto is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable)
            : Ok(Passaggi(letto.Value.Ndvi.Ndvi));
    }

    private async Task<(IReadOnlyList<int> Vigneti, NdviLettoAlle Ndvi)?> LeggiAsync(int aziendaId, CancellationToken ct)
    {
        try
        {
            var vigneti = (await _cache.GetVignetiPerAziendaAsync(aziendaId, ct)).Select(v => v.Id).Distinct().Order().ToList();
            return (vigneti, await _cache.GetNdviAsync(aziendaId, vigneti, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "NDVI dell'azienda {AziendaId} non leggibile", aziendaId);
            return null;
        }
    }

    public static PassaggiNdviDto Passaggi(NdviAzienda ndvi) =>
        new(ndvi.Calcolato, ndvi.Passaggi.OrderByDescending(p => p.Data).Select(p => new PassaggioNdviDto(p.Data, p.NuvolePct, p.Valido)).ToList());

    /// <summary>Una riga per vigneto (nell'ordine degli Id), anche per quelli senza nessun punto.</summary>
    public static NdviAziendaDto Componi(IEnumerable<int> idVigneti, NdviAzienda ndvi, int anno, DateTimeOffset? letto = null)
    {
        var perVigneto = ndvi.Punti.GroupBy(p => p.IdVigneto).ToDictionary(g => g.Key, g => g.OrderBy(p => p.Data).ToList());
        static List<PuntoNdviDto> Serie(IEnumerable<PuntoNdvi> punti, int anno) =>
            punti.Where(p => p.Data.Year == anno).Select(p => new PuntoNdviDto(p.Data, p.Media)).ToList();

        return new NdviAziendaDto(true, ndvi.Calcolato, anno,
            idVigneti.Distinct().Order()
                .Select(id =>
                {
                    var punti = perVigneto.GetValueOrDefault(id) ?? [];
                    var ultimo = punti.LastOrDefault();
                    return new NdviVignetoDto(id, ultimo?.Data, ultimo?.Media, ultimo?.NuvolePct, Serie(punti, anno), Serie(punti, anno - 1));
                })
                .ToList(),
            letto);
    }
}
