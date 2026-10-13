using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Stazioni meteo, dispositivi e trappole per azienda (task #69). Tutte le aziende del perimetro si
/// leggono insieme e restano in memoria 10 minuti: la mappa e la pagina iniziale usano la stessa
/// lettura. Il colore si calcola a ogni richiesta sul giorno di oggi. Se 4Grapes non risponde: 503.
/// </summary>
[ApiController]
[Route("api/aziende")]
public class DispositiviController : ControllerBase
{
    /// <summary>Oltre questi giorni dall'ultimo dato un dispositivo è «fermo».</summary>
    public const int GiorniRitardo = 10;

    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<DispositiviController> _logger;
    private readonly TimeProvider _orologio;

    public DispositiviController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<DispositiviController> logger,
        TimeProvider? orologio = null)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    /// <summary>Per la pagina iniziale: le aziende con segnaposti, con quanti sono fermi.</summary>
    [HttpGet("dispositivi")]
    public async Task<ActionResult<DispositiviAziendeDto>> GetTutte(CancellationToken ct)
    {
        if (await LeggiAsync(await _perimetro.GetAziendeAsync(ct), ct) is not { } letti)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(Riepiloga(letti.Dispositivi, MeteoController.Oggi(_orologio.GetUtcNow()), letti.Letti));
    }

    /// <summary>I segnaposti di un'azienda; fuori perimetro nessuno (come un'azienda inesistente).</summary>
    [HttpGet("{aziendaId:int}/dispositivi")]
    public async Task<ActionResult<DispositiviAziendaDto>> GetAzienda(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(new DispositiviAziendaDto(true, []));
        }

        var vigneti = await _cache.GetVignetiPerAziendaAsync(aziendaId, ct);
        if (await LeggiAsync(await _perimetro.GetAziendeAsync(ct), ct) is not { } letti)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(new DispositiviAziendaDto(true, Componi(aziendaId, vigneti, letti.Dispositivi, MeteoController.Oggi(_orologio.GetUtcNow())),
            letti.Letti));
    }

    private async Task<DispositiviLettiAlle?> LeggiAsync(IReadOnlySet<int>? ammesse, CancellationToken ct)
    {
        try
        {
            return await _cache.GetDispositiviAsync(ammesse, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stazioni, dispositivi e trappole non leggibili da 4Grapes");
            return null;
        }
    }

    /// <summary>
    /// Colore dall'ultimo dato: oggi o ieri attivo, fino a <see cref="GiorniRitardo"/> giorni in ritardo,
    /// oltre fermo, nessun dato mai. Una data futura (errore di 4Grapes) conta come oggi.
    /// </summary>
    public static (int? Giorni, StatoDispositivo Stato) Classifica(DateTime? ultimo, DateOnly oggi)
    {
        if (ultimo is not { } data)
        {
            return (null, StatoDispositivo.Mai);
        }

        var giorni = Math.Max(0, oggi.DayNumber - DateOnly.FromDateTime(data).DayNumber);
        return (giorni, giorni switch
        {
            <= 1 => StatoDispositivo.Attivo,
            <= GiorniRitardo => StatoDispositivo.InRitardo,
            _ => StatoDispositivo.Fermo,
        });
    }

    /// <summary>
    /// I segnaposti di un'azienda: prima le stazioni, poi i dispositivi, poi le trappole (per vigneto),
    /// ognuno in ordine di nome. Senza posizione propria si usa il centro del vigneto, se ha geometria.
    /// </summary>
    public static IReadOnlyList<DispositivoDto> Componi(int aziendaId, IEnumerable<Vigneto> vigneti, DispositiviLetti letti, DateOnly oggi)
    {
        var suoi = vigneti.DistinctBy(v => v.Id).ToDictionary(v => v.Id);
        double[]? CentroVigneto(int id) => suoi.GetValueOrDefault(id) is { } v ? VignetoGeoJsonBuilder.Centro(v.GeometriaGeoJson) : null;
        string NomeVigneto(int id) => suoi.GetValueOrDefault(id)?.Nome is { Length: > 0 } nome ? nome : $"Vigneto {id}";

        var stazioni = letti.Stazioni
            .Where(s => s.AziendaId == aziendaId)
            .DistinctBy(s => (s.IdStazione, s.IdVigneto))
            .Select(s =>
            {
                double[]? centro = s is { Longitudine: { } lon, Latitudine: { } lat } ? [lon, lat] : CentroVigneto(s.IdVigneto);
                var (giorni, stato) = Classifica(s.UltimoDato, oggi);
                return new DispositivoDto($"stazione-{s.IdStazione?.ToString() ?? "v" + s.IdVigneto}", TipoDispositivo.Stazione,
                    s.Nome ?? (s.IdStazione is { } n ? $"Stazione {n}" : "Stazione meteo"), s.IdVigneto, centro?[0], centro?[1],
                    s.UltimoDato, giorni, stato);
            })
            .OrderBy(d => d.Nome, StringComparer.CurrentCultureIgnoreCase);

        var sensori = letti.Sensori
            .Where(s => s.AziendaId == aziendaId)
            .DistinctBy(s => s.Codice)
            .Select(s =>
            {
                var centro = CentroVigneto(s.IdVigneto);
                return new DispositivoDto($"sensore-{s.Codice}", TipoDispositivo.Sensore, s.Codice, s.IdVigneto, centro?[0], centro?[1],
                    null, null, StatoDispositivo.NonDisponibile, Sensore: s.Sensore, StatoFourGrapes: s.Stato, Registrato: s.Registrato);
            })
            .OrderBy(d => d.Nome, StringComparer.CurrentCultureIgnoreCase);

        var trappole = letti.Trappole
            .Where(t => t.AziendaId == aziendaId)
            .GroupBy(t => t.IdVigneto)
            .Select(g =>
            {
                var centro = CentroVigneto(g.Key);
                var ultimo = g.Max(t => t.UltimoRilievo);
                var (giorni, stato) = Classifica(ultimo, oggi);
                return new DispositivoDto($"trappole-{g.Key}", TipoDispositivo.Trappole, NomeVigneto(g.Key), g.Key, centro?[0], centro?[1],
                    ultimo, giorni, stato, Trappole: g.Sum(t => t.Trappole));
            })
            .OrderBy(d => d.Nome, StringComparer.CurrentCultureIgnoreCase);

        return [.. stazioni, .. sensori, .. trappole];
    }

    /// <summary>
    /// Per azienda (solo quelle con segnaposti): quanti e quanti fermi. Senza posizione non serve qui:
    /// conta tutto quello che 4Grapes registra per l'azienda. Le trappole non contano fra i fermi
    /// (decisione del proprietario, 2026-10-13: fuori stagione nessuna ha rilievi recenti e il puntino
    /// sarebbe su quasi tutte le aziende); restano colorate sulla mappa.
    /// </summary>
    public static DispositiviAziendeDto Riepiloga(DispositiviLetti letti, DateOnly oggi, DateTimeOffset? letto = null)
    {
        var aziende = letti.Stazioni.Select(s => s.AziendaId)
            .Concat(letti.Sensori.Select(s => s.AziendaId))
            .Concat(letti.Trappole.Select(t => t.AziendaId))
            .Distinct()
            .Order();
        return new DispositiviAziendeDto(
            aziende.Select(id =>
                {
                    var tutti = Componi(id, [], letti, oggi);
                    return new RiepilogoDispositiviDto(id, tutti.Count, tutti.Count(d => d is { Stato: StatoDispositivo.Fermo, Tipo: not TipoDispositivo.Trappole }));
                })
                .ToList(),
            letto);
    }
}
