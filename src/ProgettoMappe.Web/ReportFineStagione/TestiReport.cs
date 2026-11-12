using System.Globalization;

namespace ProgettoMappe.Web.ReportFineStagione;

public enum LinguaReport
{
    It,
    En,
}

/// <summary>
/// Tutti i testi del report fine stagione (task #201), in un dizionario unico IT/EN: la pagina, il PowerPoint e il
/// PDF li prendono da qui, così le due lingue hanno gli stessi calcoli e cambiano solo le parole. I testi inglesi sono
/// quelli dell'app Qlik «REPORT FINE STAGIONE ENG» (refusi corretti); dove l'ENG non ha l'oggetto sono tradotti.
/// I segnaposto <c>{0}</c> si riempiono con <see cref="Testo(LinguaReport, string, object[])"/>.
/// </summary>
public static class TestiReport
{
    private static readonly Dictionary<string, (string It, string En)> Testi = new()
    {
        ["titolo"] = ("Report di Fine Stagione {0}", "End of Season Report {0}"),
        ["pagina.sottotitolo"] = ("Il report di fine stagione per azienda e annata, con gli stessi numeri del report Qlik.",
            "The end of season report by estate and season, with the same figures as the Qlik report."),
        ["filtro.azienda"] = ("Azienda", "Estate"),
        ["filtro.scegli"] = ("Scegli un'azienda…", "Choose an estate…"),
        ["filtro.annata"] = ("Annata", "Season"),
        ["filtro.lingua"] = ("Lingua", "Language"),
        ["sommario"] = ("Sommario", "Summary"),
        ["in.arrivo"] = ("in arrivo", "coming soon"),
        ["generato.il"] = ("generato il {0}", "generated on {0}"),
        ["carico"] = ("Carico il meteo dell'azienda…", "Loading the estate weather data…"),
        ["errore.meteo"] = ("Il meteo di questa azienda non è disponibile ora.", "The weather data for this estate is not available now."),
        ["riprova"] = ("Riprova", "Retry"),

        ["sezione.caratterizzazione"] = ("Caratterizzazione dell’annata {0}", "Season characterization {0}"),
        ["sezione.gestione-idrica"] = ("Gestione Idrica", "Water management"),
        ["sezione.scottature"] = ("Scottature ed Evoluzione Stress Idrico", "Sunburns and water stress"),
        ["sezione.vigoria"] = ("Analisi della Vigoria", "Vigor analysis"),
        ["sezione.nutrizione"] = ("Nutrizione", "Fertilization"),
        ["sezione.sfe"] = ("SFE", "EFS"),
        ["sezione.produzione"] = ("Analisi Produzione", "Production"),
        ["sezione.fallanze"] = ("Fallanze", "Failures"),
        ["sezione.potature"] = ("Potature", "Pruning"),
        ["sezione.sanita"] = ("Analisi Sanità delle Uve e Difesa", "Grape health and protection"),
        ["sezione.indice-bigot"] = ("Indice Bigot", "Bigot index"),
        ["sezione.maturazione"] = ("Andamento della Maturazione", "Ripening"),
        ["sezione.ad-hoc"] = ("Sezione «Ad Hoc»", "Section «Ad Hoc»"),
        ["sezione.degustazione"] = ("Degustazione Vini {0}", "Wine tasting {0}"),

        ["fase.1"] = ("1. Periodo Invernale", "1. Winter period"),
        ["fase.2"] = ("2. Germogliamento - Fioritura", "2. Budbreak - flowering"),
        ["fase.3"] = ("3. Fioritura - Invaiatura", "3. Flowering - veraison"),
        ["fase.4"] = ("4. Invaiatura - Vendemmia", "4. Veraison - harvest"),

        ["pioggia.titolo"] = ("Confronto Precipitazioni Storico", "Rainfall comparison"),
        ["pioggia.fase"] = ("Periodo", "Period"),
        ["pioggia.annata"] = ("Precipitazione Annata Agraria {0}", "Rainfall growing year {0}"),
        ["pioggia.media10"] = ("Precipitazione Ultimi 10 anni", "Rainfall last 10 years"),
        ["pioggia.media30"] = ("Precipitazione Ultimi 30 anni", "Rainfall last 30 years"),
        ["pioggia.var10"] = ("Variazione Assoluta Ultimi 10 anni", "Absolute change last 10 years"),
        ["pioggia.var10pct"] = ("Variazione % Ultimi 10 anni", "% change last 10 years"),
        ["pioggia.var30"] = ("Variazione Assoluta Ultimi 30 anni", "Absolute change last 30 years"),
        ["pioggia.var30pct"] = ("Variazione % Ultimi 30 anni", "% change last 30 years"),
        ["pioggia.mm"] = ("mm", "mm"),
        ["pioggia.vigneti"] = ("Media dei vigneti con meteo storico: {0}.", "Average of the vineyards with historical weather data: {0}."),
        ["pioggia.nessun-vigneto"] = ("Nessun vigneto dell'azienda ha il meteo storico in 4Grapes: il report Qlik qui è vuoto.",
            "No vineyard of this estate has historical weather data in 4Grapes: the Qlik report is empty here."),
        ["pioggia.senza-storico"] = ("Lo storico meteo (fino al 2025) non è ancora letto dal portale: i confronti con gli ultimi 10 e 30 anni restano vuoti.",
            "Historical weather data (up to 2025) is not read by the portal yet: comparisons with the last 10 and 30 years stay empty."),
    };

    /// <summary>Le chiavi del dizionario (per i test: ogni chiave ha le due lingue).</summary>
    public static IEnumerable<string> Chiavi => Testi.Keys;

    public static string Testo(LinguaReport lingua, string chiave, params object[] valori)
    {
        if (!Testi.TryGetValue(chiave, out var t))
        {
            throw new KeyNotFoundException($"Testo del report fine stagione mancante: {chiave}");
        }

        var modello = lingua == LinguaReport.En ? t.En : t.It;
        return valori.Length == 0 ? modello : string.Format(Cultura(lingua), modello, valori);
    }

    public static CultureInfo Cultura(LinguaReport lingua) =>
        CultureInfo.GetCultureInfo(lingua == LinguaReport.En ? "en-GB" : "it-IT");

    /// <summary>«it»/«en» del link (assente o sconosciuto = italiano).</summary>
    public static LinguaReport DaCodice(string? codice) =>
        string.Equals(codice, "en", StringComparison.OrdinalIgnoreCase) ? LinguaReport.En : LinguaReport.It;

    public static string Codice(LinguaReport lingua) => lingua == LinguaReport.En ? "en" : "it";
}
