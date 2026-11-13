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
        ["settimana.etichetta"] = ("Settimana {0} {1}", "Week {0} {1}"),
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

        ["sezione.copertina"] = ("Report", "Report"),
        ["sezione.dashboard"] = ("Numerosità di rilievi eseguiti per tipologia", "Number of observations per type"),
        ["sezione.sanita-grappoli"] = ("Sanità Grappoli", "Health KPI"),
        ["sezione.grafico-grappoli"] = ("Danno da Ampelopatie su Grappolo {0}", "Damages on the cluster {0}"),
        ["sezione.sanita-foglia"] = ("Sanità Foglie", "Leaves health"),
        ["sezione.grafico-foglia"] = ("Danno da Ampelopatie su Foglia {0}", "Damages on the leaves {0}"),
        ["sezione.fenologia"] = ("Fenologia", "Phenology"),
        ["sezione.bbch-vitigno-foglie"] = ("Fenologia - Foglie", "Phenology - Leaves"),
        ["sezione.bbch-vitigno-grappoli"] = ("Fenologia - Grappoli", "Phenology - Clusters"),
        ["sezione.bbch-storico-foglie"] = ("Fenologia - Foglie (media tutti i vitigni)", "Phenology - Leaves (all varieties)"),
        ["sezione.bbch-storico-grappoli"] = ("Fenologia - Grappoli (media tutti i vitigni)", "Phenology - Clusters (all varieties)"),
        ["sezione.vigoria"] = ("Vigoria", "Vigour"),
        ["sezione.germogli"] = ("Andamento lunghezza germogli (cm)", "Shoot lenght trend (cm)"),
        ["sezione.deficit-idrico"] = ("Valutazione dello Stress Idrico - Metodo degli apici", "Water stress"),
        ["sezione.tignole"] = ("Andamento Volo Tignole", "Catches per trap"),
        ["sezione.tignole-larve"] = ("Presenza Tignole (valutazione nidi larvali)", "Grape moths"),
        ["sezione.esca"] = ("Mal dell'Esca", "Esca disease"),
        ["sezione.considerazioni"] = ("Considerazioni", "Remarks"),

        ["kpi.intensita"] = ("Intensità", "Intensity"),
        ["kpi.diffusione"] = ("Diffusione", "Incidence"),
        ["kpi.senza-danno"] = ("Vigneti con Danno", "Vineyards with damage"),
        ["kpi.copertura"] = ("Vigneti monitorati", "Monitored vineyards"),
        ["kpi.in-attesa"] = ("in attesa dei dati del dba", "waiting for data"),
        ["avv.peronospora"] = ("Peronospora", "Downy mildew"),
        ["avv.oidio"] = ("Oidio", "Powdery mildew"),
        ["avv.botrite"] = ("Botrite", "Botrytis"),
        ["avv.marciumi"] = ("Marciumi secondari", "Secondary rots"),
        ["avv.blackrot"] = ("Black rot", "Black rot"),
        ["avv.scottature"] = ("Scottature", "Sun burns"),
        ["avv.fitotossicita"] = ("Fitotossicità", "Phytotoxicity"),
        ["avv.grandine"] = ("Grandine", "Hail"),
        ["avv.gelata"] = ("Gelata (gemme)", "Frost (buds)"),
        ["avv.tignole"] = ("Tignole", "Grape moths"),
        ["avv.filatura"] = ("Filatura", "Bunch stem necrosis"),
        ["avv.acinellatura"] = ("Acinellatura", "Millerandage"),
        ["cruscotto.rilievi"] = ("N° rilievi", "N° observations"),
        ["cruscotto.organi"] = ("N° organi controllati", "N° organs checked"),
        ["cruscotto.monitorati"] = ("N° vigneti monitorati", "N° monitored vineyards"),
        ["cruscotto.totali"] = ("N° vigneti totale", "N° vineyards"),
        ["cruscotto.percentuale"] = ("% vigneti monitorati", "% monitored vineyards"),
        ["cruscotto.al"] = ("RILIEVI FATTI al {0}", "OBSERVATIONS up to {0}"),
        ["vigoria.media"] = ("Vigoria media {0}", "Average vigour {0}"),
        ["vigoria.eccessiva"] = ("Vigoria Eccessiva", "Excessive vigour"),
        ["vigoria.molto-alta"] = ("Vigoria Molto Alta", "Very high vigour"),
        ["vigoria.alta"] = ("Vigoria Alta", "High vigour"),
        ["vigoria.medio-alta"] = ("Vigoria Medio/Alta", "Medium/high vigour"),
        ["vigoria.media-classe"] = ("Vigoria Media", "Medium vigour"),
        ["vigoria.medio-bassa"] = ("Vigoria Medio/Bassa", "Medium/low vigour"),
        ["vigoria.bassa"] = ("Vigoria Bassa", "Low vigour"),
        ["vigoria.molto-bassa"] = ("Vigoria Molto Bassa", "Very low vigour"),
        ["idrico.titolo"] = ("Stato Idrico {0}", "Water status {0}"),
        ["idrico.molto-severo"] = ("Stress idrico Molto severo", "Very severe water stress"),
        ["idrico.severo"] = ("Stress idrico Severo", "Severe water stress"),
        ["idrico.medio"] = ("Stress idrico Medio", "Medium water stress"),
        ["idrico.lieve"] = ("Stress idrico Lieve", "Mild water stress"),
        ["idrico.assente"] = ("Stress idrico Assente", "No water stress"),
        ["idrico.andamento"] = ("Andamento deficit idrico per vigna", "Water deficit trend by block"),
        ["tignole.prima"] = ("Tignole larve di prima generazione", "First generation larvae"),
        ["tignole.seconda"] = ("Tignole larve di seconda generazione", "Second generation larvae"),
        ["tignole.terza"] = ("Tignole larve di terza generazione", "Third generation larvae"),
        ["tignole.controllati"] = ("vigneti controllati {0}", "vineyards checked {0}"),
        ["tignole.diffusione"] = ("Diffusione Tignole massima rilevata durante la stagione", "Moths incidence"),
        ["esca.rilievi"] = ("Numero Rilievi", "Number of observations"),
        ["esca.piante"] = ("Numero di Piante Controllate", "Plants checked"),
        ["esca.0"] = ("Sintomi Assenti", "No symptoms"),
        ["esca.10"] = ("Tigratura poche foglie", "Few tiger-striped leaves"),
        ["esca.30"] = ("Punti vegetativi inattivi o stentati", "Inactive or stunted growing points"),
        ["esca.40"] = ("Tralci disseccati o avvizziti", "Dried or withered shoots"),
        ["esca.50"] = ("Tigratura Estesa", "Extended tiger-stripe"),
        ["esca.60"] = ("Tigratura + tralci disseccati o avvizziti", "Tiger-stripe + dried shoots"),
        ["esca.100"] = ("Colpo Apoplettico", "Apoplexy"),
        ["bbch.anno"] = ("BBCH Anno in Corso", "BBCH current year"),
        ["bbch.precedente"] = ("BBCH Anno Precedente", "BBCH previous year"),
        ["bbch.storica"] = ("BBCH Media Storica", "BBCH historical average"),
        ["settimana"] = ("Settimana di Riferimento", "Reference week"),
        ["in.attesa.dati"] = ("In attesa dei dati del dba (task #212): lo snapshot non ha ancora questo campo.",
            "Waiting for the data (task #212): the snapshot does not have this field yet."),
        ["fenologia.in.arrivo"] = ("Date del modello BBCH e confronto per vitigno: in arrivo nella prossima tappa.",
            "BBCH model dates and comparison by variety: coming in the next stage."),
        ["considerazioni.testo"] = ("Le considerazioni le scrive il tecnico nel PowerPoint.", "Remarks are written by the agronomist in the PowerPoint."),
        ["nessun.dato"] = ("Nessun dato fino a questa settimana.", "No data up to this week."),
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
