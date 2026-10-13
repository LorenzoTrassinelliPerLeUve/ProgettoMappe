using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Avvisi;

/// <summary>Gli avvisi di un'azienda, per la pagina Avvisi.</summary>
public sealed record AvvisiAzienda(int AziendaId, string Azienda, IReadOnlyList<AvvisoDto> Avvisi);

/// <summary>Raggruppamento e testi della pagina Avvisi e della pagina iniziale (task #70).</summary>
public static class ElencoAvvisi
{
    /// <summary>
    /// Gli avvisi per azienda, nell'ordine in cui arrivano dall'Api (azienda, regola, i più vecchi prima);
    /// con <paramref name="regola"/> solo quelli di quella regola. Le aziende senza avvisi non compaiono.
    /// </summary>
    public static IReadOnlyList<AvvisiAzienda> PerAzienda(IEnumerable<AvvisoDto> avvisi, string? regola = null) =>
        avvisi.Where(a => regola is null || a.Regola == regola)
            .GroupBy(a => a.AziendaId)
            .Select(g => new AvvisiAzienda(g.Key, g.First().Azienda, g.ToList()))
            .ToList();

    /// <summary>«12 avvisi in 4 aziende» per la pagina iniziale; null se gli avvisi non sono ancora pronti.</summary>
    public static string? Riassunto(AvvisiDto avvisi)
    {
        if (!avvisi.Pronto)
        {
            return null;
        }

        var aziende = avvisi.Avvisi.Select(a => a.AziendaId).Distinct().Count();
        return avvisi.Avvisi.Count switch
        {
            0 => "Nessun avviso",
            1 => "1 avviso in 1 azienda",
            var n => $"{n} avvisi in {(aziende == 1 ? "1 azienda" : $"{aziende} aziende")}",
        };
    }

    /// <summary>Le regole rimaste senza dati nell'ultimo calcolo: «Senza rilievi, Stazione ferma».</summary>
    public static string? NonDisponibili(AvvisiDto avvisi) =>
        avvisi.Regole.Where(r => !r.Disponibile).Select(r => r.Breve).ToList() is { Count: > 0 } nomi ? string.Join(", ", nomi) : null;
}
