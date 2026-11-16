using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Il livello delle soglie scelto nei filtri della sezione 14: globali (niente), azienda, azienda + rischio, vigneto (con la
/// sua azienda e il suo rischio).
/// </summary>
public sealed record LivelloSoglie(int? IdEnte = null, int? IdRischio = null, int? IdVigneto = null)
{
    public bool Globale => IdEnte is null && IdVigneto is null;
}

/// <summary>
/// Le soglie dei trattamenti a livelli come <c>SoglieTrattamentiController</c> del portale (GetSoglieGlobali → FromEnte →
/// FromRischio → FromVigneto): ogni riga ha i suoi valori (null = non ancora scritti a quel livello) e quelli dei livelli
/// sopra, mostrati come nel portale («Soglia globale», «Soglia Ente», «Soglia {rischio}»).
/// </summary>
public static class CalcoloSoglie
{
    /// <summary>La riga di quel livello per l'avversità, con le stesse condizioni del portale; null se non c'è.</summary>
    public static SogliaTrattamentoDto? Trova(IEnumerable<SogliaTrattamentoDto> soglie, LivelloSoglie livello, int idAvversita) =>
        soglie.FirstOrDefault(s => s.IdAvversitaDaTrattare == idAvversita && (
            livello.IdVigneto is { } v ? s.IdVigneto == v
            : livello.IdRischio is { } r ? s.IdEnte == livello.IdEnte && s.IdRischio == r
            : livello.IdEnte is { } e ? s.IdEnte == e && s.IdRischio == null
            : s.ValoriGlobali));

    public static IReadOnlyList<SogliaRigaDto> Righe(IReadOnlyList<AvversitaDaTrattareDto> avversita,
        IReadOnlyList<SogliaTrattamentoDto> soglie, LivelloSoglie livello, string? nomeRischio)
    {
        var sopra = new List<(string Etichetta, LivelloSoglie Livello)>();
        if (!livello.Globale) sopra.Add(("Soglia globale", new LivelloSoglie()));
        if (livello.IdRischio is not null || livello.IdVigneto is not null) sopra.Add(("Soglia Ente", new LivelloSoglie(livello.IdEnte)));
        // Un vigneto senza rischio: il portale va in errore; qui il livello del rischio semplicemente non c'è.
        if (livello.IdVigneto is not null && livello.IdRischio is not null)
            sopra.Add(($"Soglia {nomeRischio}", livello with { IdVigneto = null }));

        return avversita.Select(a =>
        {
            var propria = Trova(soglie, livello, a.Id);
            var ereditate = sopra.Select(s => Trova(soglie, s.Livello, a.Id) is { } t
                    ? new SoglieEreditateDto(s.Etichetta, t.SogliaProbabilitaInfezione, t.SogliaPressione, t.SogliaProtezione)
                    : new SoglieEreditateDto(s.Etichetta, null, null, null))
                .ToList();
            return new SogliaRigaDto(a.Id, a.Descrizione, propria?.Id, propria?.SogliaProbabilitaInfezione, propria?.SogliaPressione,
                propria?.SogliaProtezione, ereditate);
        }).ToList();
    }

    /// <summary>Le colonne della riga nuova a quel livello (come il portale: globali con ValoriGlobali, gli altri con gli Id).</summary>
    public static Dictionary<string, object?> ChiaveNuova(LivelloSoglie livello, int idAvversita) =>
        livello.IdVigneto is { } v
            ? new() { ["IdAvversitaDaTrattare"] = idAvversita, ["IdVigneto"] = v, ["ValoriGlobali"] = false }
            : livello.IdRischio is { } r
                ? new() { ["IdAvversitaDaTrattare"] = idAvversita, ["IdEnte"] = livello.IdEnte, ["IdRischio"] = r, ["ValoriGlobali"] = false }
                : livello.IdEnte is { } e
                    ? new() { ["IdAvversitaDaTrattare"] = idAvversita, ["IdEnte"] = e, ["ValoriGlobali"] = false }
                    : new() { ["IdAvversitaDaTrattare"] = idAvversita, ["ValoriGlobali"] = true };
}
