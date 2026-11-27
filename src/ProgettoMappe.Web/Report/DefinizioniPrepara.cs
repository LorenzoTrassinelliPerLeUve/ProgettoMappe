using System.Globalization;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.ReportAnalisiSuolo;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;

namespace ProgettoMappe.Web.Report;

/// <summary>Un'edizione da scegliere nella pagina Prepara: il codice per l'Api («2026-47», «2026», «tutti») e l'etichetta.</summary>
public sealed record EdizionePrepara(string Codice, string Etichetta);

/// <summary>
/// Quello che la pagina «Prepara il report» (<c>/report/prepara</c>, task #418) deve sapere di un report: catalogo, edizioni,
/// indirizzi dell'anteprima e del PowerPoint, se ha due lingue e se si pubblica già una versione congelata (oggi solo il
/// settimanale; fine stagione, Indice Bigot e suolo hanno scelte, commenti e stati, la pubblicazione arriva dopo).
/// </summary>
public sealed record DefinizionePrepara(string Report, string Titolo, string Edizione, IReadOnlyList<ElementoReport> Catalogo,
    bool ConLingua, bool ConPubblica, bool AziendeSuolo, Func<DateOnly, IReadOnlyList<EdizionePrepara>> Edizioni,
    Func<int, string, string, string> Anteprima, Func<int, string, string, string?> PowerPoint,
    Func<string, bool>? SoloItaliano = null)
{
    public static readonly IReadOnlyList<DefinizionePrepara> Tutte =
    [
        new(CatalogoSettimanale.Report, "Report settimanale", "Settimana", CatalogoSettimanale.Elementi, true, true, false,
            oggi => SettimanaReport.Elenco(oggi).Select(s => new EdizionePrepara(s.Codice, s.Etichetta(LinguaReport.It))).ToList(),
            (a, e, l) => $"report-settimanale?azienda={a}&settimana={e}&lingua={l}",
            (a, e, l) => $"report-settimanale/powerpoint?azienda={a}&settimana={e}&lingua={l}",
            id => SezioniSettimanale.Tutte.Any(s => !s.Eng && CatalogoSettimanale.Id(s.Chiave) == id)),
        new(CatalogoFineStagione.Report, "Report fine stagione", "Annata", CatalogoFineStagione.Elementi, true, false, false,
            oggi => Anni(CalcoloMeteoReport.AnnataAgraria(oggi) - 1, 2015),
            (a, e, l) => $"report-fine-stagione?azienda={a}&annata={e}&lingua={l}",
            (a, e, l) => $"report-fine-stagione/powerpoint?azienda={a}&annata={e}&lingua={l}"),
        new(CatalogoIndiceBigot.Report, "Report Indice Bigot", "Anno dell'indice", CatalogoIndiceBigot.Elementi, false, false, false,
            oggi => Anni(oggi.Year, 2019),
            (a, e, _) => $"indice-bigot?azienda={a}&anno={e}",
            (a, e, _) => $"indice-bigot/powerpoint?azienda={a}&anno={e}"),
        new(CatalogoAnalisiSuolo.Report, "Report analisi del suolo", "Anno delle analisi", CatalogoAnalisiSuolo.Elementi, false, false, true,
            oggi => [new("tutti", "Tutti gli anni"), .. Anni(oggi.Year, 2000)],
            (a, e, _) => $"report-analisi-suolo?azienda={a}{(e == "tutti" ? "" : "&anno=" + e)}",
            (a, e, _) => $"report-analisi-suolo/powerpoint?azienda={a}{(e == "tutti" ? "" : "&anno=" + e)}"),
    ];

    /// <summary>La definizione del report, il settimanale se il codice non è noto.</summary>
    public static DefinizionePrepara Di(string? report) => Tutte.FirstOrDefault(d => d.Report == report) ?? Tutte[0];

    private static List<EdizionePrepara> Anni(int dal, int al) =>
        Enumerable.Range(al, dal - al + 1).Reverse()
            .Select(a => new EdizionePrepara(a.ToString(CultureInfo.InvariantCulture), a.ToString(CultureInfo.InvariantCulture))).ToList();
}
