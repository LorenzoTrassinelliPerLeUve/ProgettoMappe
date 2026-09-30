using ProgettoMappe.Web.Services;

namespace ProgettoMappe.Web.Filtri;

/// <summary>Filtro Gruppo → Azienda (un'azienda può appartenere a più gruppi).</summary>
public static class FiltriAziende
{
    /// <summary>Aziende del gruppo, nell'ordine dell'elenco ricevuto; tutte se il gruppo è null.</summary>
    public static IReadOnlyList<AziendaDto> DelGruppo(IEnumerable<AziendaDto> aziende, GruppoDto? gruppo)
    {
        if (gruppo is null)
        {
            return aziende.ToList();
        }

        var membri = gruppo.AziendeIds.ToHashSet();
        return aziende.Where(a => membri.Contains(a.Id)).ToList();
    }

    /// <summary>
    /// Azienda da mostrare dopo la scelta del gruppo: resta quella corrente se ne fa parte,
    /// altrimenti la prima del gruppo; null se il gruppo non ha aziende disponibili.
    /// </summary>
    public static int? AziendaDaMostrare(IReadOnlyList<AziendaDto> aziendeDelGruppo, int aziendaCorrente) =>
        aziendeDelGruppo.Any(a => a.Id == aziendaCorrente) ? aziendaCorrente : aziendeDelGruppo.FirstOrDefault()?.Id;

    public static string Etichetta(GruppoDto gruppo, int aziende) =>
        $"{gruppo.Nome} ({aziende}){(gruppo.Concluso ? " – concluso" : "")}";
}
