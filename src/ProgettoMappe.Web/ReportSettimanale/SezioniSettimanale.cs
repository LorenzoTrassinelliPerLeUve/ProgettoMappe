namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>Una slide del report settimanale: chiave del titolo in <see cref="TestiSettimanale"/>, foglio Qlik, se è nel report ENG.</summary>
public sealed record SezioneSettimanale(string Chiave, string FoglioQlik, bool Eng = true);

/// <summary>
/// Le slide nell'ordine dell'automazione Qlik PPT_REPORT_SETTIAMANALE (docs/specifiche/report-settimanale/automazione.md):
/// 18 in italiano, 12 nel report ENG (senza grafico foglia, confronti BBCH e considerazioni).
/// </summary>
public static class SezioniSettimanale
{
    public static readonly IReadOnlyList<SezioneSettimanale> Tutte =
    [
        new("sezione.copertina", "Report"),
        new("sezione.dashboard", "Dashboard"),
        new("sezione.sanita-grappoli", "Sanità Grappoli KPI"),
        new("sezione.grafico-grappoli", "Grafico Grappoli"),
        new("sezione.sanita-foglia", "Sanità Foglia KPI"),
        new("sezione.grafico-foglia", "Grafico Foglia", Eng: false),
        new("sezione.fenologia", "Fenologia"),
        new("sezione.bbch-vitigno-foglie", "Confronto BBCH - Anno in Corso - Vitigno", Eng: false),
        new("sezione.bbch-vitigno-grappoli", "Confronto BBCH - Anno in Corso - Vitigno (1)", Eng: false),
        new("sezione.bbch-storico-foglie", "Confronto BBCH - Storico", Eng: false),
        new("sezione.bbch-storico-grappoli", "Confronto BBCH - Storico (1)", Eng: false),
        new("sezione.vigoria", "Vigoria"),
        new("sezione.germogli", "Lunghezza Germogli"),
        new("sezione.deficit-idrico", "Deficit Idrico"),
        new("sezione.tignole", "Tignole"),
        new("sezione.tignole-larve", "Tignole (1)"),
        new("sezione.esca", "Mal dell'Esca"),
        new("sezione.considerazioni", "CONSIDERAZIONI", Eng: false),
    ];

    public static IEnumerable<SezioneSettimanale> Della(ProgettoMappe.Web.ReportFineStagione.LinguaReport lingua) =>
        lingua == ProgettoMappe.Web.ReportFineStagione.LinguaReport.En ? Tutte.Where(s => s.Eng) : Tutte;
}
