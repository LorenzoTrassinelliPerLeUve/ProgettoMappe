using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>Un vigneto nella pagina «Oggi»: il nome e i trattamenti del giorno (testo e stato del calendario).</summary>
public sealed record VignetoOggi(int Id, string Nome, IReadOnlyList<PulsanteTrattamentoDto> Trattamenti);

/// <summary>
/// Cosa c'è da fare oggi nella difesa (pagina iniziale del Portale nuovo, task #387), dal calendario dei trattamenti:
/// da fare oggi = suggeriti (anche per altre avversità) o automatici nella colonna di oggi; fatti = effettivi nei giorni prima e oggi;
/// prossimi = vigneti con un suggerito nei giorni dopo e niente da fare oggi.
/// </summary>
public sealed record RiepilogoOggi(
    IReadOnlyList<VignetoOggi> DaFareOggi,
    int FattiNeiGiorniPrima,
    int GiorniPrima,
    IReadOnlyList<(VignetoOggi Vigneto, DateOnly Giorno)> Prossimi)
{
    public static readonly IReadOnlySet<string> StatiDaFare = new HashSet<string> { "suggerito", "suggerito-altre", "automatico" };

    public static RiepilogoOggi Calcola(TrattamentiGiornalieriDto dati)
    {
        var oggi = dati.Giorni.ToList().IndexOf(dati.Data);
        if (oggi < 0)
        {
            return new([], 0, 0, []);
        }

        var daFare = new List<VignetoOggi>();
        var prossimi = new List<(VignetoOggi, DateOnly)>();
        var fatti = 0;
        foreach (var riga in dati.Vigneti)
        {
            var colonna = riga.Giorni[oggi].Pulsanti.Where(p => StatiDaFare.Contains(p.Stato)).ToList();
            if (colonna.Count > 0)
            {
                daFare.Add(new VignetoOggi(riga.Id, riga.Nome, colonna));
            }
            else if (Enumerable.Range(oggi + 1, riga.Giorni.Count - oggi - 1)
                         .FirstOrDefault(i => riga.Giorni[i].Pulsanti.Any(p => StatiDaFare.Contains(p.Stato)), -1) is var dopo and >= 0)
            {
                prossimi.Add((new VignetoOggi(riga.Id, riga.Nome,
                    riga.Giorni[dopo].Pulsanti.Where(p => StatiDaFare.Contains(p.Stato)).ToList()), dati.Giorni[dopo]));
            }

            // Anche oggi (task #389: Mirafiore il 30/5/2025 aveva 176 effettivi e 176 suggeriti nello stesso giorno).
            fatti += riga.Giorni.Take(oggi + 1).Sum(g => g.Pulsanti.Count(p => p.Stato == "effettivo"));
        }

        return new(daFare, fatti, oggi, prossimi.OrderBy(p => p.Item2).ToList());
    }

    /// <summary>Le parole al posto dei colori del portale (una sola legenda per tutto il Portale nuovo).</summary>
    public static string Stato(string stato) => stato switch
    {
        "suggerito" => "Da fare",
        "suggerito-altre" => "Da fare (altre avversità)",
        "automatico" => "Proposto in automatico",
        "effettivo" => "Fatto",
        "esclusi" => "Escluso",
        _ => "",
    };

    /// <summary>«PERONOSPORA | OIDIO» → «Peronospora, oidio»: maiuscole solo all'inizio.</summary>
    public static string Avversita(string testo)
    {
        var parti = testo.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.ToLowerInvariant()).ToList();
        if (parti.Count == 0) return "";
        var frase = string.Join(", ", parti);
        return char.ToUpperInvariant(frase[0]) + frase[1..];
    }
}
