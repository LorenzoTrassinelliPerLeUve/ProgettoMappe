using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Ultimo rilievo in campo per vigneto (task #45), di qualunque tipo.</summary>
[ApiController]
[Route("api")]
public class RilieviController : ControllerBase
{
    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<RilieviController> _logger;

    public RilieviController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<RilieviController> logger)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
    }

    /// <summary>
    /// Una riga per ogni vigneto dell'azienda: data, tipo, autore e foto dell'ultimo rilievo, o
    /// Data null se il vigneto non è mai stato rilevato. Letti da 4Grapes al più ogni 10 minuti
    /// per azienda. Se 4Grapes non risponde: 503 (la mappa dice «non raggiungibili»).
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/rilievi")]
    public async Task<ActionResult<UltimiRilieviAziendaDto>> GetUltimiRilievi(int aziendaId, CancellationToken ct)
    {
        // Fuori perimetro: nessun vigneto, quindi nessun dato (come un'azienda inesistente).
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(new UltimiRilieviAziendaDto(true, []));
        }

        var vigneti = await _cache.GetVignetiPerAziendaAsync(aziendaId, ct);
        RilieviLetti letti;
        try
        {
            letti = await _cache.GetUltimiRilieviAsync(aziendaId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ultimi rilievi dell'azienda {AziendaId} non leggibili da 4Grapes", aziendaId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(Componi(vigneti.Select(v => v.Id), letti));
    }

    /// <summary>Una riga per vigneto (nell'ordine degli Id), anche per quelli senza rilievi.</summary>
    public static UltimiRilieviAziendaDto Componi(IEnumerable<int> idVigneti, RilieviLetti letti)
    {
        var perVigneto = letti.Rilievi.GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.MaxBy(r => r.Data)!);
        return new UltimiRilieviAziendaDto(true,
            idVigneti.Distinct().Order()
                .Select(id => perVigneto.GetValueOrDefault(id) is { } r
                    ? new UltimoRilievoDto(id, r.Data, r.Tipo, r.Autore, r.ConFoto)
                    : new UltimoRilievoDto(id, null, null, null, false))
                .ToList(),
            letti.Letti);
    }
}
