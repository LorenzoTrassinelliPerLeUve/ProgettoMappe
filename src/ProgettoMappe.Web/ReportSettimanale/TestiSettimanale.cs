using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>
/// I testi del report settimanale (task #213) in un dizionario unico IT/EN, come <see cref="TestiReport"/>: pagina,
/// PowerPoint e PDF li prenderanno da qui. I titoli inglesi vanno ripresi dall'app Qlik «REPORT SETTIMANALE - ENG»
/// quando arriva la struttura (task #211); oggi sono tradotti.
/// </summary>
public static class TestiSettimanale
{
    private static readonly Dictionary<string, (string It, string En)> Testi = new()
    {
        ["titolo"] = ("Report Settimanale", "Weekly Report"),
        ["pagina.sottotitolo"] = ("Il report settimanale per azienda e settimana, con gli stessi numeri del report Qlik.",
            "The weekly report by estate and week, with the same figures as the Qlik report."),
        ["filtro.azienda"] = ("Azienda", "Estate"),
        ["filtro.scegli"] = ("Scegli un'azienda…", "Choose an estate…"),
        ["filtro.settimana"] = ("Settimana", "Week"),
        ["filtro.lingua"] = ("Lingua", "Language"),
        ["settimana.etichetta"] = ("Settimana {0} del {1} ({2}–{3})", "Week {0} of {1} ({2}–{3})"),
        ["carico"] = ("Carico i rilievi della settimana…", "Loading the week's surveys…"),
        ["errore"] = ("I rilievi di questa azienda non sono disponibili ora.", "The surveys for this estate are not available now."),
        ["errore.aziende"] = ("L'elenco delle aziende non è disponibile ora.", "The estate list is not available now."),
        ["riprova"] = ("Riprova", "Retry"),
        ["sommario"] = ("Sommario", "Summary"),
        ["in.arrivo"] = ("in arrivo", "coming soon"),
        ["generato.il"] = ("generato il {0}", "generated on {0}"),
        ["dati.snapshot"] = ("Dati dello snapshot notturno del {0}.", "Data from the nightly snapshot of {0}."),
        ["dati.diretti"] = ("Dati letti ora da 4Grapes (snapshot non disponibile): solo il numero dei rilievi.",
            "Data read now from 4Grapes (snapshot not available): survey counts only."),
        ["rilievi.settimana"] = ("Rilievi nella settimana: {0}", "Surveys in the week: {0}"),
        ["rilievi.nessuno"] = ("Nessun rilievo nella settimana.", "No surveys in the week."),
        ["rilievi.vigneti"] = ("Vigneti rilevati: {0}", "Vineyards surveyed: {0}"),
        ["rilievi.tipo"] = ("Tipo di rilievo", "Survey type"),
        ["rilievi.numero"] = ("Rilievi", "Surveys"),
        ["rilievi.bbch"] = ("Fenologia (BBCH)", "Phenology (BBCH)"),
        ["contenuto.in.arrivo"] = ("Contenuto della sezione in arrivo (tappa 2).", "Section content coming soon (stage 2)."),
        ["foglio.qlik"] = ("Foglio Qlik: {0}", "Qlik sheet: {0}"),

        ["sezione.dashboard"] = ("Dashboard", "Dashboard"),
        ["sezione.fenologia"] = ("Fenologia", "Phenology"),
        ["sezione.bbch-vitigno"] = ("Confronto BBCH - Anno in corso - Vitigno", "BBCH comparison - Current year - Variety"),
        ["sezione.bbch-storico"] = ("Confronto BBCH - Storico", "BBCH comparison - History"),
        ["sezione.sanita-grappoli"] = ("Sanità grappoli", "Bunch health"),
        ["sezione.sanita-foglia"] = ("Sanità foglia", "Leaf health"),
        ["sezione.grafico-foglia"] = ("Grafico foglia", "Leaf chart"),
        ["sezione.grafico-grappoli"] = ("Grafico grappoli", "Bunch chart"),
        ["sezione.vigoria"] = ("Vigoria", "Vigor"),
        ["sezione.germogli"] = ("Lunghezza germogli", "Shoot length"),
        ["sezione.deficit-idrico"] = ("Deficit idrico", "Water deficit"),
        ["sezione.deficit-anni"] = ("Deficit idrico: confronto anni", "Water deficit: year comparison"),
        ["sezione.tignole"] = ("Tignole", "Grape moths"),
        ["sezione.esca"] = ("Mal dell'esca", "Esca"),
        ["sezione.considerazioni"] = ("Considerazioni", "Remarks"),
        ["sezione.vigoria-sub"] = ("Vigoria dei sottovigneti", "Sub-vineyard vigor"),
        ["sezione.foto"] = ("Foto", "Photos"),
    };

    public static IEnumerable<string> Chiavi => Testi.Keys;

    public static string Testo(LinguaReport lingua, string chiave, params object[] valori)
    {
        if (!Testi.TryGetValue(chiave, out var testo))
        {
            return chiave;
        }
        var modello = lingua == LinguaReport.En ? testo.En : testo.It;
        return valori.Length == 0 ? modello : string.Format(TestiReport.Cultura(lingua), modello, valori);
    }
}
