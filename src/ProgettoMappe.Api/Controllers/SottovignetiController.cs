using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Controllers;

/// <summary>Sottovigneti dei vigneti di un'azienda (task #66), per la mappa e la scheda.</summary>
[ApiController]
[Route("api")]
public class SottovignetiController : ControllerBase
{
    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<SottovignetiController> _logger;

    public SottovignetiController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<SottovignetiController> logger)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
    }

    /// <summary>
    /// Elenco e FeatureCollection dei sottovigneti, letti da 4Grapes al più ogni minuto per azienda.
    /// Fuori perimetro: nessuno. Se 4Grapes non risponde: 503 (i vigneti restano sulla mappa).
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/sottovigneti")]
    public async Task<ActionResult<SottovignetiAziendaDto>> GetSottovigneti(int aziendaId, CancellationToken ct)
    {
        IReadOnlyList<Sottovigneto> sottovigneti = [];
        if (await _perimetro.ContieneAsync(aziendaId, ct))
        {
            try
            {
                sottovigneti = await _cache.GetSottovignetiPerAziendaAsync(aziendaId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sottovigneti dell'azienda {AziendaId} non leggibili da 4Grapes", aziendaId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        return Ok(Componi(sottovigneti));
    }

    // «Filare 2» prima di «Filare 10».
    private static readonly StringComparer OrdineNomi =
        StringComparer.Create(CultureInfo.GetCultureInfo("it-IT"), CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    public static SottovignetiAziendaDto Componi(IReadOnlyList<Sottovigneto> sottovigneti) =>
        new(true,
            sottovigneti.OrderBy(s => s.IdVigneto).ThenBy(s => s.Nome, OrdineNomi)
                .Select(s => new SottovignetoDto(s.Id, s.IdVigneto, s.Nome, s.SuperficieEttari, !string.IsNullOrWhiteSpace(s.GeometriaGeoJson)))
                .ToList(),
            JsonSerializer.SerializeToElement(VignetoGeoJsonBuilder.BuildSottovigneti(sottovigneti)));
}
