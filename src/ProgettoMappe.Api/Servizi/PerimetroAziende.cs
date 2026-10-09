using Microsoft.Extensions.Options;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Servizi;

/// <summary>Sezione "Perimetro" della configurazione dell'Api.</summary>
public sealed class PerimetroOptions
{
    public const string Sezione = "Perimetro";

    /// <summary>
    /// IdGruppo 4Grapes delle aziende che l'app può mostrare (richiesta del proprietario:
    /// 1, 3, 4 = progetto VTS FVG / extra FVG / Estero). Vuoto = nessuna restrizione.
    /// </summary>
    public int[] Gruppi { get; set; } = [];
}

/// <summary>
/// Le aziende che l'Api può mostrare: solo i componenti dei gruppi in Perimetro:Gruppi.
/// Vale per tutti gli endpoint di dati (aziende, vigneti, GeoJSON, indici BIGOT, gruppi):
/// un'azienda fuori perimetro risponde come se non esistesse. Anche il filtro Gruppo offre solo
/// i gruppi configurati. Scoped: i gruppi si leggono una volta per richiesta.
/// </summary>
public sealed class PerimetroAziende
{
    private readonly IGruppiRepository _gruppi;
    private readonly HashSet<int> _idGruppi;
    private IReadOnlyList<Gruppo>? _gruppiVisibili;

    public PerimetroAziende(IGruppiRepository gruppi, IOptions<PerimetroOptions> opzioni)
    {
        _gruppi = gruppi;
        _idGruppi = opzioni.Value.Gruppi.ToHashSet();
    }

    private bool Ristretto => _idGruppi.Count > 0;

    /// <summary>I gruppi visibili: solo quelli configurati (tutti, se il perimetro è vuoto).</summary>
    public async Task<IReadOnlyList<Gruppo>> GetGruppiAsync(CancellationToken ct = default)
    {
        if (_gruppiVisibili is null)
        {
            var tutti = await _gruppi.GetGruppiAsync(ct);
            _gruppiVisibili = Ristretto ? tutti.Where(g => _idGruppi.Contains(g.Id)).ToList() : tutti;
        }

        return _gruppiVisibili;
    }

    /// <summary>Id delle aziende ammesse, o null se non c'è restrizione.</summary>
    public async Task<IReadOnlySet<int>?> GetAziendeAsync(CancellationToken ct = default) =>
        Ristretto ? (await GetGruppiAsync(ct)).SelectMany(g => g.AziendeIds).ToHashSet() : null;

    public async Task<bool> ContieneAsync(int aziendaId, CancellationToken ct = default) =>
        await GetAziendeAsync(ct) is not { } ammesse || ammesse.Contains(aziendaId);
}
