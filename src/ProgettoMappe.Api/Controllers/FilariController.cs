using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Filari;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.Filari;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Filari dei vigneti (task #95): generazione dal primo filare, archivio a versioni (SQLite di Mappe,
/// non 4Grapes) e strato per la mappa. Un vigneto si raggiunge sempre dalla sua azienda: fuori perimetro,
/// o se il vigneto non è dell'azienda, 404. Se l'archivio non si legge o non si scrive: 503.
/// </summary>
[ApiController]
[Route("api/aziende/{aziendaId:int}")]
public class FilariController : ControllerBase
{
    /// <summary>Limite dei filari in un salvataggio (come la generazione).</summary>
    public const int MaxFilariSalvati = GeneratoreFilari.MaxFilari;

    public const int LunghezzaMassimaNote = 500;

    private readonly CacheVignetiAziende _vigneti;
    private readonly PerimetroAziende _perimetro;
    private readonly ArchivioFilari _archivio;
    private readonly TimeProvider _orologio;
    private readonly ILogger _logger;

    public FilariController(CacheVignetiAziende vigneti, PerimetroAziende perimetro, ArchivioFilari archivio,
        TimeProvider? orologio = null, ILogger<FilariController>? logger = null)
    {
        _vigneti = vigneti;
        _perimetro = perimetro;
        _archivio = archivio;
        _orologio = orologio ?? TimeProvider.System;
        _logger = logger ?? (ILogger)Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Filari validi di tutti i vigneti dell'azienda: riassunti e FeatureCollection per la mappa.</summary>
    [HttpGet("filari")]
    public async Task<ActionResult<FilariAziendaDto>> GetFilariAzienda(int aziendaId, CancellationToken ct)
    {
        if (!await _perimetro.ContieneAsync(aziendaId, ct))
        {
            return Ok(Componi([]));
        }

        try
        {
            var ids = (await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)).Select(v => v.Id).ToList();
            return Ok(Componi(await _archivio.UltimeAsync(ids, ct)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Filari dell'azienda {AziendaId} non leggibili (archivio {Percorso})", aziendaId, _archivio.Percorso);
            return Ok(FilariAziendaDto.NonDisponibili);
        }
    }

    /// <summary>Storico delle versioni del vigneto, dalla più recente.</summary>
    [HttpGet("vigneti/{vignetoId:int}/filari")]
    public async Task<ActionResult<StoricoFilariDto>> GetStorico(int aziendaId, int vignetoId, CancellationToken ct)
    {
        if (await VignetoAsync(aziendaId, vignetoId, ct) is null)
        {
            return NotFound();
        }

        try
        {
            return Ok(new StoricoFilariDto((await _archivio.StoricoAsync(vignetoId, ct)).Select(Riassunto).ToList()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Archivio dei filari non leggibile ({Percorso})", _archivio.Percorso);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>Una versione con i filari.</summary>
    [HttpGet("vigneti/{vignetoId:int}/filari/{versione:int}")]
    public async Task<ActionResult<VersioneFilariDto>> GetVersione(int aziendaId, int vignetoId, int versione, CancellationToken ct)
    {
        if (await VignetoAsync(aziendaId, vignetoId, ct) is null)
        {
            return NotFound();
        }

        VersioneFilari? trovata;
        try
        {
            trovata = await _archivio.VersioneAsync(vignetoId, versione, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Archivio dei filari non leggibile ({Percorso})", _archivio.Percorso);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return trovata is null
            ? NotFound()
            : Ok(new VersioneFilariDto(Riassunto(trovata), GeneratoreFilari.DaWkt(trovata.GeometriaWkt).Select(f => f.ComeArray()).ToList()));
    }

    /// <summary>Filari paralleli al primo, dentro il poligono del vigneto (non salvati). 400 con il motivo se non si può.</summary>
    [HttpPost("vigneti/{vignetoId:int}/filari/genera")]
    public async Task<ActionResult<FilariGeneratiDto>> Genera(int aziendaId, int vignetoId, GeneraFilariDto richiesta, CancellationToken ct)
    {
        if (await VignetoAsync(aziendaId, vignetoId, ct) is not { } vigneto)
        {
            return NotFound();
        }

        if (richiesta.Da is not [var lon1, var lat1, ..] || richiesta.A is not [var lon2, var lat2, ..])
        {
            return BadRequest("Servono i due punti del primo filare.");
        }

        if (vigneto.SoloPunto || string.IsNullOrWhiteSpace(vigneto.GeometriaGeoJson))
        {
            return BadRequest("Il vigneto non ha un poligono.");
        }

        try
        {
            var calcolati = GeneratoreFilari.Genera(JsonNode.Parse(vigneto.GeometriaGeoJson), (lon1, lat1), (lon2, lat2),
                richiesta.InterfilaM, richiesta.ScostamentoM);
            return Ok(new FilariGeneratiDto(calcolati.Filari.Select(f => f.ComeArray()).ToList(), calcolati.NumFilari,
                Math.Round(calcolati.LunghezzaM, 1), calcolati.AzimutGradi));
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Salva una nuova versione dei filari del vigneto (la più recente è quella valida). Solo con l'autore,
    /// che mette il Web dall'utente entrato: senza, 403. Numero, lunghezza e direzione li calcola l'Api.
    /// </summary>
    [HttpPut("vigneti/{vignetoId:int}/filari")]
    public async Task<ActionResult<FilariVignetoDto>> Salva(int aziendaId, int vignetoId, SalvaFilariDto richiesta, CancellationToken ct)
    {
        var autore = richiesta.Autore?.Trim();
        if (string.IsNullOrEmpty(autore))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Per salvare i filari serve un utente entrato.");
        }

        if (await VignetoAsync(aziendaId, vignetoId, ct) is null)
        {
            return NotFound();
        }

        if (richiesta.Filari is null || richiesta.Filari.Count > MaxFilariSalvati)
        {
            return BadRequest($"Al massimo {MaxFilariSalvati} filari.");
        }

        var filari = new List<Filare>(richiesta.Filari.Count);
        foreach (var punti in richiesta.Filari)
        {
            if (Filare.DaArray(punti) is not { } filare)
            {
                return BadRequest("Ogni filare sono due punti [lon, lat] validi.");
            }
            filari.Add(filare);
        }

        if (!double.IsFinite(richiesta.InterfilaM) || richiesta.InterfilaM is < 0 or > GeneratoreFilari.InterfilaMassimaM
            || !double.IsFinite(richiesta.ScostamentoM) || Math.Abs(richiesta.ScostamentoM) > GeneratoreFilari.ScostamentoMassimoM)
        {
            return BadRequest("Interfila o scostamento fuori intervallo.");
        }

        var misura = GeneratoreFilari.Misura(filari);
        var note = string.IsNullOrWhiteSpace(richiesta.Note) ? null : richiesta.Note.Trim();
        if (note is { Length: > LunghezzaMassimaNote })
        {
            note = note[..LunghezzaMassimaNote];
        }

        var nuova = new VersioneFilari(vignetoId, 0, GeneratoreFilari.Wkt(filari), Math.Round(richiesta.InterfilaM, 2),
            misura.AzimutGradi, Math.Round(richiesta.ScostamentoM, 2), misura.NumFilari, Math.Round(misura.LunghezzaM, 1),
            autore.Length > 100 ? autore[..100] : autore,
            string.IsNullOrWhiteSpace(richiesta.AutoreNome) ? null : richiesta.AutoreNome.Trim(),
            _orologio.GetUtcNow().UtcDateTime, note);
        try
        {
            var salvata = await _archivio.SalvaAsync(nuova, ct);
            _logger.LogInformation("Filari del vigneto {VignetoId} salvati: versione {Versione}, {NumFilari} filari, da {Autore}",
                vignetoId, salvata.Versione, salvata.NumFilari, salvata.Autore);
            return Ok(Riassunto(salvata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Filari del vigneto {VignetoId} non salvati: archivio non scrivibile ({Percorso})", vignetoId, _archivio.Percorso);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "L'archivio dei filari non è scrivibile.");
        }
    }

    /// <summary>Il vigneto, se l'azienda è nel perimetro e il vigneto è suo; altrimenti null.</summary>
    private async Task<Vigneto?> VignetoAsync(int aziendaId, int vignetoId, CancellationToken ct) =>
        await _perimetro.ContieneAsync(aziendaId, ct)
            ? (await _vigneti.GetVignetiPerAziendaAsync(aziendaId, ct)).FirstOrDefault(v => v.Id == vignetoId)
            : null;

    public static FilariVignetoDto Riassunto(VersioneFilari v) =>
        new(v.IdVigneto, v.Versione, v.NumFilari, v.LunghezzaM, v.InterfilaM, v.AzimutGradi, v.ScostamentoM,
            v.Autore, AutoreBreve(v.AutoreNome) ?? v.Autore, v.CreatoIl, v.Note);

    /// <summary>«Lorenzo Trassinelli» → «L. Trassinelli» (come gli autori dei rilievi); null senza nome.</summary>
    public static string? AutoreBreve(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return null;
        }

        var parti = nome.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parti.Length == 2 ? FourGrapesRilieviRepository.Autore(parti[0], parti[1]) : FourGrapesRilieviRepository.Autore(null, parti[0]);
    }

    /// <summary>Riassunti (per IdVigneto) e FeatureCollection con una LineString per filare (idVigneto, numero, lunghezza).</summary>
    public static FilariAziendaDto Componi(IReadOnlyList<VersioneFilari> versioni)
    {
        var features = new JsonArray();
        foreach (var v in versioni.OrderBy(v => v.IdVigneto))
        {
            var numero = 0;
            foreach (var f in GeneratoreFilari.DaWkt(v.GeometriaWkt))
            {
                numero++;
                features.Add(new JsonObject
                {
                    ["type"] = "Feature",
                    ["properties"] = new JsonObject
                    {
                        ["idVigneto"] = v.IdVigneto,
                        ["numero"] = numero,
                        ["lunghezza"] = Math.Round(GeneratoreFilari.Lunghezza(f), 1),
                    },
                    ["geometry"] = new JsonObject
                    {
                        ["type"] = "LineString",
                        ["coordinates"] = new JsonArray(new JsonArray(f.Lon1, f.Lat1), new JsonArray(f.Lon2, f.Lat2)),
                    },
                });
            }
        }

        var collezione = new JsonObject { ["type"] = "FeatureCollection", ["features"] = features };
        return new FilariAziendaDto(true,
            versioni.Where(v => v.NumFilari > 0).OrderBy(v => v.IdVigneto).Select(Riassunto).ToList(),
            JsonSerializer.SerializeToElement(collezione));
    }
}
