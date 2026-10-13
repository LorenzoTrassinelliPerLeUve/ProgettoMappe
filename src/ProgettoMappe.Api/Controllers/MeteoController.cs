using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Meteo per azienda (task #67): pioggia dei sette giorni prima di oggi, temperature di ieri e
/// probabilità di infezione di oggi (modello classico). Tutte le aziende del perimetro si leggono
/// insieme e restano in memoria 30 minuti: la pagina iniziale e la mappa usano la stessa lettura.
/// Se 4Grapes non risponde: 503 (le pagine dicono «meteo non disponibile»).
/// </summary>
[ApiController]
[Route("api/aziende")]
public class MeteoController : ControllerBase
{
    private readonly CacheVignetiAziende _cache;
    private readonly PerimetroAziende _perimetro;
    private readonly ILogger<MeteoController> _logger;
    private readonly TimeProvider _orologio;

    public MeteoController(CacheVignetiAziende cache, PerimetroAziende perimetro, ILogger<MeteoController> logger, TimeProvider? orologio = null)
    {
        _cache = cache;
        _perimetro = perimetro;
        _logger = logger;
        _orologio = orologio ?? TimeProvider.System;
    }

    /// <summary>Tutte le aziende del perimetro: una riga per azienda (anche senza meteo).</summary>
    [HttpGet("meteo")]
    public async Task<ActionResult<MeteoAziendeDto>> GetTutte(CancellationToken ct)
    {
        var ammesse = await _perimetro.GetAziendeAsync(ct);
        if (await LeggiAsync(ammesse, ct) is not { } letto)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var aziende = ammesse ?? letto.Meteo.Giorni.Select(g => g.AziendaId).Concat(letto.Meteo.Infezioni.Select(i => i.AziendaId)).ToHashSet();
        return Ok(new MeteoAziendeDto(letto.Meteo.Oggi, aziende.Order().Select(id => Componi(id, letto.Meteo)).ToList(), letto.Letto));
    }

    /// <summary>Una sola azienda, dalla stessa lettura; fuori perimetro: senza meteo, come un'azienda senza stazione.</summary>
    [HttpGet("{aziendaId:int}/meteo")]
    public async Task<ActionResult<MeteoAziendaDto>> GetAzienda(int aziendaId, CancellationToken ct)
    {
        var ammesse = await _perimetro.GetAziendeAsync(ct);
        var oggi = Oggi(_orologio.GetUtcNow());
        if (ammesse is not null && !ammesse.Contains(aziendaId))
        {
            return Ok(Componi(aziendaId, new MeteoLetto(oggi, [], [], [])));
        }

        return await LeggiAsync(ammesse, ct) is { } letto
            ? Ok(Componi(aziendaId, letto.Meteo))
            : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private async Task<MeteoLettoAlle?> LeggiAsync(IReadOnlySet<int>? ammesse, CancellationToken ct)
    {
        try
        {
            return await _cache.GetMeteoAsync(ammesse, Oggi(_orologio.GetUtcNow()), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Meteo e infezioni non leggibili da 4Grapes");
            return null;
        }
    }

    /// <summary>
    /// Il meteo di un'azienda, senza accesso ai dati: i sette giorni prima di oggi (Pioggia null dove
    /// manca il dato), il totale dei giorni con il dato (null se nessuno: mai uno zero inventato),
    /// ieri e, per oidio e peronospora separati, la probabilità più alta fra i vigneti e quanti sono
    /// sopra la soglia di 4Grapes.
    /// </summary>
    public static MeteoAziendaDto Componi(int aziendaId, MeteoLetto letto)
    {
        var suoi = letto.Giorni.Where(g => g.AziendaId == aziendaId).GroupBy(g => g.Data).ToDictionary(g => g.Key, g => g.First());
        var giorni = Enumerable.Range(1, CacheVignetiAziende.GiorniMeteo).Reverse()
            .Select(i => letto.Oggi.AddDays(-i))
            .Select(d => suoi.GetValueOrDefault(d) is { } g
                ? new MeteoGiornoDto(d, Positivo(g.Pioggia), g.TemperaturaMin, g.TemperaturaMax)
                : new MeteoGiornoDto(d, null, null, null))
            .ToList();
        var conPioggia = giorni.Where(g => g.Pioggia is not null).ToList();
        var ieri = giorni[^1] is { } ultimo && (ultimo.Pioggia ?? ultimo.TemperaturaMin ?? ultimo.TemperaturaMax) is not null ? ultimo : null;

        var avversita = letto.Avversita.DistinctBy(a => a.Id).ToDictionary(a => a.Id);
        var infezioni = letto.Infezioni
            .Where(i => i.AziendaId == aziendaId)
            .GroupBy(i => i.IdAvversita)
            .OrderBy(g => g.Key == IMeteoRepository.Peronospora ? 0 : 1) // peronospora prima, come nel cartellino
            .Select(g =>
            {
                var perVigneto = g.GroupBy(i => i.IdVigneto).Select(v => v.Max(i => i.Percentuale)).ToList();
                var a = avversita.GetValueOrDefault(g.Key);
                var soglia = a?.Soglia;
                return new InfezioneAziendaDto(g.Key, a?.Nome ?? NomeNoto(g.Key), perVigneto.Max(), perVigneto.Count, soglia,
                    soglia is null ? 0 : perVigneto.Count(p => p >= soglia));
            })
            .ToList();

        return new MeteoAziendaDto(aziendaId, giorni, conPioggia.Count == 0 ? null : conPioggia.Sum(g => g.Pioggia!.Value), ieri, infezioni);
    }

    /// <summary>Il giorno di oggi in Italia (le date di 4Grapes sono in ora italiana).</summary>
    public static DateOnly Oggi(DateTimeOffset adesso) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(adesso, Fuso()).DateTime);

    /// <summary>Una pioggia negativa è un dato sporco: vale come mancante.</summary>
    private static decimal? Positivo(decimal? valore) => valore is >= 0 ? valore : null;

    private static string NomeNoto(int id) => id switch
    {
        IMeteoRepository.Oidio => "Oidio",
        IMeteoRepository.Peronospora => "Peronospora",
        _ => $"Avversità {id}",
    };

    private static TimeZoneInfo Fuso() =>
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Rome", out var fuso) ? fuso : TimeZoneInfo.Utc;
}
