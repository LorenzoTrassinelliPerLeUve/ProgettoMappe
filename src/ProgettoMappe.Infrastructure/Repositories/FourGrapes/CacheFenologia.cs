using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Copia in memoria della fenologia (dba, 2026-10-13): i rilievi BBCH di <c>VistaRilievoBBCH</c>
/// (~199.000 righe, 2008–2026, letti in 3–8 s) e i nomi delle fasi di <c>dbo.BBCH</c> (55 righe).
/// Si tiene solo il riassunto per vigneto e anno (<see cref="Riassumi"/>). La vista copre tutti i
/// vigneti di 4Grapes: il filtro per azienda lo fa chi chiede (<see cref="Per"/>).
/// </summary>
public sealed partial class CacheFenologia : CacheVista
{
    // Data è datetime2 con l'ora; Anno è l'anno di Data (per la vendemmia, la data di vendemmia).
    private const string QueryRilievi = """
        SELECT Anno, IdVigneto, Data, BBCH
        FROM dbo.VistaRilievoBBCH
        WHERE Anno IS NOT NULL AND IdVigneto IS NOT NULL AND Data IS NOT NULL AND BBCH IS NOT NULL
        """;

    private const string QueryFasi = "SELECT BBCH, DescrizioneBBCH FROM dbo.BBCH";

    private volatile IReadOnlyDictionary<int, IReadOnlyList<FenologiaVigneto>>? _copia;

    public CacheFenologia(IServiceScopeFactory scopeFactory, ILogger<CacheFenologia> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string Nome => "fenologia (rilievi BBCH)";

    public override bool Pronta => _copia is not null;

    /// <summary>Fenologia dei vigneti richiesti (tutti gli anni), o null se la cache non è ancora stata caricata.</summary>
    public IReadOnlyList<FenologiaVigneto>? Per(IEnumerable<int> idVigneti)
    {
        var copia = _copia;
        return copia is null
            ? null
            : idVigneti.Distinct().SelectMany(id => copia.TryGetValue(id, out var f) ? f : []).ToList();
    }

    protected override async Task<string> LeggiAsync(SqlConnection connessione, CancellationToken ct)
    {
        var fasi = new Dictionary<int, string>();
        foreach (var fase in await LeggiVistaAsync(connessione, QueryFasi, "BBCH", LeggiFase, ct))
        {
            fasi.TryAdd(fase.Bbch, fase.Descrizione);
        }

        var rilievi = await LeggiVistaAsync(connessione, QueryRilievi, "VistaRilievoBBCH", LeggiRilievo, ct);
        var senzaNome = rilievi.Select(r => r.Bbch).Where(b => !fasi.ContainsKey(b)).Distinct().Order().ToList();
        if (senzaNome.Count > 0)
        {
            // Oggi solo il 52, in una riga (dba): si mostra il numero senza nome.
            Logger.LogWarning("Fenologia: codici BBCH senza nome in dbo.BBCH: {Codici}", string.Join(", ", senzaNome));
        }

        var riassunto = Riassumi(rilievi, fasi);
        _copia = riassunto.GroupBy(f => f.IdVigneto)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FenologiaVigneto>)g.OrderBy(f => f.Anno).ToList());
        return $"{rilievi.Count} rilievi, {riassunto.Count} vigneti-anno, {fasi.Count} fasi";
    }

    /// <summary>
    /// Per vigneto e anno: la fase dell'ultimo giorno con rilievi (se quel giorno ci sono più rilievi
    /// con fasi diverse, la più avanzata: succede in ~18.000 giorni-vigneto) e le altre fasi viste nei
    /// giorni prima, ciascuna con il primo giorno in cui compare, dalla più vecchia.
    /// </summary>
    public static IReadOnlyList<FenologiaVigneto> Riassumi(IEnumerable<RilievoBbch> rilievi, IReadOnlyDictionary<int, string> fasi) =>
        rilievi.GroupBy(r => (r.IdVigneto, r.Anno))
            .Select(g =>
            {
                var ultimoGiorno = g.Max(r => r.Data);
                var bbch = g.Where(r => r.Data == ultimoGiorno).Max(r => r.Bbch);
                var precedenti = g.Where(r => r.Data < ultimoGiorno && r.Bbch != bbch)
                    .GroupBy(r => r.Bbch)
                    .Select(f => new FaseRilevata(f.Key, fasi.GetValueOrDefault(f.Key), f.Min(r => r.Data)))
                    .OrderBy(f => f.Data).ThenBy(f => f.Bbch)
                    .ToList();
                return new FenologiaVigneto(g.Key.Anno, g.Key.IdVigneto, bbch, fasi.GetValueOrDefault(bbch), ultimoGiorno, precedenti);
            })
            .OrderBy(f => f.IdVigneto).ThenBy(f => f.Anno)
            .ToList();

    /// <summary>Un rilievo; null se manca uno dei quattro valori.</summary>
    public static RilievoBbch? LeggiRilievo(IDataRecord r) =>
        r.Intero("Anno") is { } anno && r.Intero("IdVigneto") is { } id && r.Data("Data") is { } data && r.Intero("BBCH") is { } bbch
            ? new RilievoBbch(anno, id, data, bbch)
            : null;

    /// <summary>Una fase di dbo.BBCH: il nome senza il numero davanti («65 - Piena fioritura…» → «Piena fioritura…»).</summary>
    public static FaseBbch? LeggiFase(IDataRecord r) =>
        r.Intero("BBCH") is { } bbch && r.Testo("DescrizioneBBCH") is { } testo ? new FaseBbch(bbch, SenzaNumero(testo)) : null;

    public static string SenzaNumero(string descrizione) =>
        NumeroDavanti().Replace(descrizione, "", 1).Trim() is { Length: > 0 } nome ? nome : descrizione.Trim();

    /// <summary>Una riga di dbo.BBCH.</summary>
    public sealed record FaseBbch(int Bbch, string Descrizione);

    [GeneratedRegex(@"^\s*\d+\s*[-–]\s*")]
    private static partial Regex NumeroDavanti();
}
