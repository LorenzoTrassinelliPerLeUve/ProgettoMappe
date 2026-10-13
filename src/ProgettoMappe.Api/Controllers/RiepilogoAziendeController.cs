using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

[ApiController]
[Route("api/aziende/riepilogo")]
public class RiepilogoAziendeController : ControllerBase
{
    /// <summary>Parametri della vista BIGOT: solo i vigneti completi hanno punteggi confrontabili.</summary>
    public const int ParametriBigot = 9;

    private readonly IAziendeRepository _aziende;
    private readonly CacheVignetiAziende _vigneti;
    private readonly IIndiciBigotRepository _indici;
    private readonly PerimetroAziende _perimetro;

    public RiepilogoAziendeController(
        IAziendeRepository aziende, CacheVignetiAziende vigneti, IIndiciBigotRepository indici, PerimetroAziende perimetro)
    {
        _aziende = aziende;
        _vigneti = vigneti;
        _indici = indici;
        _perimetro = perimetro;
    }

    /// <summary>
    /// Aziende del perimetro con numero di vigneti, ettari e punteggio BIGOT medio: tutto da dati
    /// in memoria (superfici per 10 minuti, indici BIGOT dalla loro copia). Se gli indici non sono
    /// ancora pronti risponde subito, senza punteggi.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RiepilogoAziendeDto>> GetRiepilogo(CancellationToken ct)
    {
        var ammesse = await _perimetro.GetAziendeAsync(ct);
        var aziende = (await _aziende.GetAziendeAsync(ct))
            .Where(a => ammesse is null || ammesse.Contains(a.Id))
            .ToList();
        var vigneti = await _vigneti.GetSuperficiAsync(ammesse, ct);
        var indici = await _indici.GetPerVignetiAsync(vigneti.Select(v => v.IdVigneto).ToList(), ct);

        return Ok(Calcola(aziende, vigneti, indici, DateTime.Today.Year));
    }

    /// <summary>
    /// Il calcolo, senza accesso ai dati. Anno dei punteggi: il più recente con indici fino a
    /// <paramref name="annoCorrente"/> (a inizio anno quello corrente può non averne ancora).
    /// </summary>
    public static RiepilogoAziendeDto Calcola(
        IReadOnlyList<Azienda> aziende,
        IReadOnlyList<SuperficieVigneto> vigneti,
        IReadOnlyList<IndiceBigotVigneto>? indici,
        int annoCorrente)
    {
        var anno = indici?.Where(i => i.Anno <= annoCorrente).Select(i => (int?)i.Anno).Max();
        var aziendaDi = vigneti.DistinctBy(v => v.IdVigneto).ToDictionary(v => v.IdVigneto, v => v.AziendaId);
        var punteggi = (indici ?? [])
            .Where(i => i.Anno == anno && i.NumParametri == ParametriBigot && i.Punteggio is not null)
            .Where(i => aziendaDi.ContainsKey(i.IdVigneto))
            .GroupBy(i => aziendaDi[i.IdVigneto])
            .ToDictionary(g => g.Key, g => (Media: g.Average(i => (decimal)i.Punteggio!.Value), Numero: g.Count()));
        var perAzienda = vigneti.ToLookup(v => v.AziendaId);

        return new RiepilogoAziendeDto(indici is not null, anno, aziende
            .OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(a =>
            {
                var suoi = perAzienda[a.Id].ToList();
                var punteggio = punteggi.GetValueOrDefault(a.Id);
                return new RiepilogoAziendaDto(
                    a.Id,
                    a.Nome,
                    suoi.Count,
                    suoi.Sum(v => v.SuperficieEttari ?? 0),
                    suoi.Count(v => v.SuperficieEttari is null),
                    punteggio.Numero > 0 ? Math.Round(punteggio.Media, 1) : null,
                    punteggio.Numero);
            })
            .ToList());
    }
}
