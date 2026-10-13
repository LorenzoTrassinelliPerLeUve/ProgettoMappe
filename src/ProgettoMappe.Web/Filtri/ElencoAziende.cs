using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Filtri;

public enum OrdineAziende
{
    Nome,
    Punteggio,
    Pioggia,
}

/// <summary>Linguetta della pagina iniziale: <paramref name="IdGruppo"/> null = «Tutte».</summary>
public sealed record LinguettaGruppo(int? IdGruppo, string Nome, int Conteggio);

/// <summary>
/// Ricerca, linguette per gruppo e ordinamento delle schede della pagina iniziale. Tutto sul
/// riepilogo già caricato: nessuna chiamata all'Api.
/// </summary>
public static class ElencoAziende
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>
    /// Aziende il cui nome contiene il testo (anche parziale, senza badare a maiuscole e accenti),
    /// del gruppo scelto (null = tutte), nell'ordine scelto. Per punteggio: dal più alto, quelle
    /// senza punteggio in fondo; a pari punteggio per nome. Per pioggia (<paramref name="pioggia"/>: mm
    /// degli ultimi 7 giorni per azienda): dalla più piovosa, quelle senza dato meteo in fondo.
    /// </summary>
    public static IReadOnlyList<RiepilogoAziendaDto> Filtra(
        IEnumerable<RiepilogoAziendaDto> aziende, string? testo, GruppoDto? gruppo, OrdineAziende ordine,
        IReadOnlyDictionary<int, decimal?>? pioggia = null)
    {
        var scelte = Cerca(aziende, testo).Where(a => gruppo is null || gruppo.AziendeIds.Contains(a.Id));
        var perNome = StringComparer.Create(Italiano, ignoreCase: true);
        decimal? Pioggia(RiepilogoAziendaDto a) => pioggia?.GetValueOrDefault(a.Id);

        return (ordine switch
            {
                OrdineAziende.Punteggio => scelte.OrderBy(a => a.PunteggioBigot is null).ThenByDescending(a => a.PunteggioBigot).ThenBy(a => a.Nome, perNome),
                OrdineAziende.Pioggia => scelte.OrderBy(a => Pioggia(a) is null).ThenByDescending(Pioggia).ThenBy(a => a.Nome, perNome),
                _ => scelte.OrderBy(a => a.Nome, perNome),
            })
            .ToList();
    }

    /// <summary>
    /// «Tutte» più un gruppo per linguetta (in ordine di Id: VTS FVG, VTS extra FVG, VTS Estero),
    /// con quante aziende mostrerebbe con la ricerca attuale. I gruppi senza aziende non compaiono.
    /// </summary>
    public static IReadOnlyList<LinguettaGruppo> Linguette(
        IReadOnlyList<RiepilogoAziendaDto> aziende, IEnumerable<GruppoDto> gruppi, string? testo)
    {
        var trovate = Cerca(aziende, testo).Select(a => a.Id).ToHashSet();
        var presenti = aziende.Select(a => a.Id).ToHashSet();

        return gruppi
            .Where(g => g.AziendeIds.Any(presenti.Contains))
            .OrderBy(g => g.Id)
            .Select(g => new LinguettaGruppo(g.Id, g.Nome, g.AziendeIds.Distinct().Count(trovate.Contains)))
            .Prepend(new LinguettaGruppo(null, "Tutte", trovate.Count))
            .ToList();
    }

    /// <summary>12.345 → «12,3 ha» (un decimale basta per un totale aziendale).</summary>
    public static string Ettari(decimal ettari) => ettari.ToString("#,##0.#", Italiano) + " ha";

    /// <summary>72.5 → «72,5», 72 → «72».</summary>
    public static string Punteggio(decimal punteggio) => punteggio.ToString("0.#", Italiano);

    private static IEnumerable<RiepilogoAziendaDto> Cerca(IEnumerable<RiepilogoAziendaDto> aziende, string? testo)
    {
        var cercato = RicercaMappa.Normalizza(testo);
        return cercato.Length == 0
            ? aziende
            : aziende.Where(a => RicercaMappa.Normalizza(a.Nome).Contains(cercato, StringComparison.Ordinal));
    }
}
