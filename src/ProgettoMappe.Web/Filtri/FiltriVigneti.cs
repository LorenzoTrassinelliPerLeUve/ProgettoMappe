using ProgettoMappe.Web.Services;

namespace ProgettoMappe.Web.Filtri;

/// <summary>Voce di un filtro: Id (o <see cref="FiltriVigneti.NonSpecificato"/>), nome e numero di vigneti.</summary>
public sealed record OpzioneFiltro(int Id, string Nome, int Conteggio);

/// <summary>
/// Filtri a cascata sui vigneti di un'azienda (Vigna → Vitigno), calcolati sui dati già caricati.
/// Null = nessun filtro; <see cref="NonSpecificato"/> seleziona i vigneti senza vigna/vitigno.
/// </summary>
public static class FiltriVigneti
{
    public const int NonSpecificato = -1;

    public static IReadOnlyList<OpzioneFiltro> Vigne(IEnumerable<VignetoDto> vigneti) =>
        Opzioni(vigneti, v => v.VignaId, v => v.Vigna, "Vigna");

    /// <summary>Vitigni presenti tra i vigneti della vigna scelta (tutti se vignaId è null).</summary>
    public static IReadOnlyList<OpzioneFiltro> Vitigni(IEnumerable<VignetoDto> vigneti, int? vignaId) =>
        Opzioni(Applica(vigneti, vignaId, null), v => v.VitignoId, v => v.Varieta, "Vitigno");

    public static IReadOnlyList<VignetoDto> Applica(IEnumerable<VignetoDto> vigneti, int? vignaId, int? vitignoId) =>
        vigneti.Where(v => Corrisponde(v.VignaId, vignaId) && Corrisponde(v.VitignoId, vitignoId)).ToList();

    private static bool Corrisponde(int? valore, int? filtro) =>
        filtro is null || (filtro == NonSpecificato ? valore is null : valore == filtro);

    private static IReadOnlyList<OpzioneFiltro> Opzioni(IEnumerable<VignetoDto> vigneti,
        Func<VignetoDto, int?> id, Func<VignetoDto, string?> nome, string etichetta)
    {
        var lista = vigneti.ToList();
        var opzioni = lista
            .Where(v => id(v) is not null)
            .GroupBy(v => id(v)!.Value)
            .Select(g => new OpzioneFiltro(g.Key,
                g.Select(nome).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim() ?? $"{etichetta} {g.Key}",
                g.Count()))
            .OrderBy(o => o.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var senza = lista.Count(v => id(v) is null);
        if (senza > 0)
        {
            opzioni.Add(new OpzioneFiltro(NonSpecificato, "Non specificato", senza));
        }

        return opzioni;
    }
}
