using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Ultimo trattamento fatto e linea di difesa per vigneto (task #68).</summary>
[ApiController]
[Route("api")]
public class DifesaController : ControllerBase
{
    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<DifesaController> _logger;

    public DifesaController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<DifesaController> logger)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
    }

    /// <summary>
    /// Una riga per ogni vigneto dell'azienda: ultimo giorno di trattamento (Data null = mai trattato),
    /// linea di difesa e classe di rischio. Tutte le aziende del perimetro si leggono insieme al più
    /// ogni 10 minuti. Se 4Grapes non risponde: 503 (la mappa dice «non raggiungibili»).
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/difesa")]
    public async Task<ActionResult<DifesaAziendaDto>> GetDifesa(int aziendaId, CancellationToken ct)
    {
        // Fuori perimetro: nessun vigneto, quindi nessun dato (come un'azienda inesistente).
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(new DifesaAziendaDto(true, []));
        }

        var vigneti = await _cache.GetVignetiPerAziendaAsync(aziendaId, ct);
        DifesaLettaAlle letta;
        try
        {
            letta = await _cache.GetDifesaAsync(await _perimetro.GetAziendeAsync(ct), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trattamenti e linee di difesa non leggibili da 4Grapes");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(Componi(vigneti.Select(v => v.Id), letta));
    }

    /// <summary>Una riga per vigneto (nell'ordine degli Id), anche per quelli senza trattamenti né linea.</summary>
    public static DifesaAziendaDto Componi(IEnumerable<int> idVigneti, DifesaLettaAlle letta)
    {
        var trattamenti = letta.Difesa.Trattamenti.GroupBy(t => t.IdVigneto).ToDictionary(g => g.Key, g => g.MaxBy(t => t.Data)!);
        var linee = letta.Difesa.Linee.DistinctBy(l => l.IdVigneto).ToDictionary(l => l.IdVigneto);
        return new DifesaAziendaDto(true,
            idVigneti.Distinct().Order()
                .Select(id =>
                {
                    var t = trattamenti.GetValueOrDefault(id);
                    var l = linee.GetValueOrDefault(id);
                    return new DifesaVignetoDto(id, t?.Data, t?.PrincipiAttivi ?? [], t?.Prodotti ?? [], t?.Avversita ?? [],
                        l?.IdLinea, l?.IdLinea is null ? null : l.Linea, l?.Rischio);
                })
                .ToList(),
            letta.Letta);
    }
}
