using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Una voce del menù «Decision 4Grapes Nutrizione». <paramref name="Chiave"/> = indirizzo <c>/nutrizione/{Chiave}</c>;
/// <paramref name="Sezione"/> = la sezione del Portale che la mostra (e ne decide i permessi); <paramref name="Vista"/> = la
/// pagina della sezione da aprire; <paramref name="Gruppo"/> = il sotto-menù («Rilievi»), null = voce di primo livello.
/// </summary>
public sealed record VoceNutrizione(string Chiave, string Titolo, string Sezione, string? Vista = null, string? Gruppo = null);

/// <summary>
/// Il menù del sotto-portale Nutrizione del portale 4Grapes (<c>Views/_Layout/PortalNutrizione.cshtml</c>, righe 78-122),
/// nello stesso ordine e con i titoli italiani di <c>Tr.*</c> (task #293). Le pagine sono quelle del Portale di Mappe:
/// una voce si vede solo se l'utente può aprire la sua sezione (stesse abilitazioni del layout del portale: CONCIMAZIONI,
/// PRODUZIONE, ANALISI_DEI_TERRENI, ANALISI_UVE, FALLANZE, VIGNETI; la Dashboard, spenta nel portale, con DASHBOARD).
/// «Torna al portale 4Grapes» porta al menù del Portale di Mappe.
/// </summary>
public static class MenuNutrizione
{
    public const string Titolo = "Decision 4Grapes Nutrizione";
    public const string Rilievi = "Rilievi";

    public static readonly IReadOnlyList<VoceNutrizione> Voci =
    [
        new("dashboard", "Dashboard", "dashboard-nutrizione"),
        new("target-enologico", "Target Enologico", "concimazioni", "Target"),
        new("fattori-concimazioni", "Fattori Concimazioni", "concimazioni", "Fattori"),
        new("unita-concimazioni", "Unità Concimazioni", "concimazioni", "Unita"),
        new("resoconto-concimazioni", "Resoconto concimazioni", "concimazioni", "Resoconto"),
        new("obiettivo-produttivo", "Obiettivo produttivo", "produzione", "obiettivo"),
        new("asportazioni", "Asportazioni", "asportazioni"),
        new("analisi-dei-terreni", "Analisi dei terreni", "analisi-terreni", Gruppo: Rilievi),
        new("analisi-uve", "Analisi uve", "analisi-uve", Gruppo: Rilievi),
        new("fallanze", "Fallanze", "fallanze", Gruppo: Rilievi),
        new("dati-produzione", "Dati produzione", "produzione", "rilievi", Rilievi),
        new("vigneti", "Vigneti", "vigneti"),
    ];

    public static VoceNutrizione? Trova(string? chiave) =>
        Voci.FirstOrDefault(v => string.Equals(v.Chiave, chiave, StringComparison.OrdinalIgnoreCase));

    /// <summary>Le voci che l'utente può aprire (la sua sezione è accessibile), nell'ordine del menù.</summary>
    public static IReadOnlyList<VoceNutrizione> Visibili(PortaleUtenteDto utente) =>
        Voci.Where(v => utente.Sezioni.Any(s => s.Accessibile && string.Equals(s.Chiave, v.Sezione, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    /// <summary>
    /// Le voci a blocchi consecutivi con lo stesso gruppo, come il menù del portale: le voci in cima, il sotto-menù «Rilievi»,
    /// poi «Vigneti» di nuovo al primo livello.
    /// </summary>
    public static IReadOnlyList<(string? Gruppo, IReadOnlyList<VoceNutrizione> Voci)> Blocchi(IReadOnlyList<VoceNutrizione> voci)
    {
        var blocchi = new List<(string? Gruppo, IReadOnlyList<VoceNutrizione> Voci)>();
        foreach (var voce in voci)
        {
            if (blocchi.Count > 0 && blocchi[^1].Gruppo == voce.Gruppo)
            {
                ((List<VoceNutrizione>)blocchi[^1].Voci).Add(voce);
            }
            else
            {
                blocchi.Add((voce.Gruppo, new List<VoceNutrizione> { voce }));
            }
        }

        return blocchi;
    }
}
