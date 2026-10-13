using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Api.Servizi;
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
    private readonly PerimetroAziende _perimetro;
    private readonly TimeProvider _orologio;

    public EsportazioneController(CacheVignetiAziende vigneti, IAziendeRepository aziende, IIndiciBigotRepository indici,
        PerimetroAziende perimetro, TimeProvider? orologio = null)
    {
        _vigneti = vigneti;
        _aziende = aziende;
        _indici = indici;
        _perimetro = perimetro;
        _orologio = orologio ?? TimeProvider.System;
    }

    /// <summary>
    /// Vigneti dell'azienda (con geometria) come file GeoJSON, KML o shapefile zip, nome
    /// azienda-AAAAMMGG. <paramref name="ids"/> (separati da virgola) restringe ai vigneti filtrati
    /// nella mappa; <paramref name="anno"/> aggiunge gli indici BIGOT di quell'anno. Un'azienda
    /// fuori perimetro o inesistente risponde 404, un formato sconosciuto o ids illeggibili 400.
    /// </summary>
    [HttpGet("aziende/{aziendaId:int}/vigneti/esporta")]
    public async Task<IActionResult> Esporta(int aziendaId, [FromQuery] string? formato, [FromQuery] int? anno,
        [FromQuery] string? ids, CancellationToken ct)
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

        var esportati = elenco
            .Select(v => new VignetoEsportato(v, azienda.Nome, anno, indici.GetValueOrDefault(v.Id)))
            .ToList();

        var oggi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_orologio.GetUtcNow(), FusoItalia).DateTime);
        var nomeBase = EsportazioneVigneti.NomeBase(azienda.Nome, aziendaId, oggi);
        return File(EsportazioneVigneti.Scrivi(formatoFile, nomeBase, esportati),
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
