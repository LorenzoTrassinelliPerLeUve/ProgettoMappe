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

        ["periodo.1"] = ("Periodo Invernale 01/11 - 14/03", "Winter period 01/11 - 14/03"),
        ["periodo.2"] = ("Periodo Germogliamento - Fioritura 15/03 - 31/05", "Budbreak - flowering 15/03 - 31/05"),
        ["periodo.3"] = ("Periodo Fioritura - Invaiatura 01/06 - 31/07", "Flowering - veraison 01/06 - 31/07"),
        ["periodo.4"] = ("Periodo Invaiatura - Vendemmia 01/08 - 31/10", "Veraison - harvest 01/08 - 31/10"),
        ["fase.termo"] = ("Andamento termo pluviometrico", "Rainfalls and temperatures"),
        ["fase.termico"] = ("Andamento termico", "Temperature trend"),
        ["fase.freddi"] = ("Giorni Freddi", "Cold days"),
        ["fase.caldi"] = ("Giorni Caldi", "Hot days"),
        ["fase.pioggia"] = ("Giorni di Pioggia", "Rainy days"),
        ["fase.giornaliero"] = ("Andamento termo-pluviometrico giornaliero stagionale", "Seasonal temperatures and rainfalls trend"),
        ["kpi.annata"] = ("Annata {0}", "Season {0}"),
        ["kpi.media"] = ("Media ultimi {0} anni", "Average last {0} years"),
        ["kpi.var"] = ("Var. annata {0}", "Season change {0}"),
        ["kpi.devst"] = ("Dev. St. ultimi 10 anni", "St. dev. last 10 years"),
        ["kpi.confronto"] = ("Confronto con ultimi 10 anni", "Compared with last 10 years"),
        ["kpi.tmed-5-30"] = ("Temperatura media (5 e 30 anni)", "Average temperature (5 and 30 years)"),
        ["kpi.pioggia-5-30"] = ("Precipitazione media (5 e 30 anni)", "Average rainfall (5 and 30 years)"),
        ["campo.Pioggia"] = ("Precipitazioni mm", "Rainfalls mm"),
        ["campo.TMin"] = ("Temperatura minima", "Minimum temperature"),
        ["campo.TMed"] = ("Temperatura media", "Average temperature"),
        ["campo.TMax"] = ("Temperatura massima", "Maximum temperature"),
        ["campo.Pioggia2"] = ("Giorni di pioggia > 2 mm", "Rainy days > 2 mm"),
        ["campo.Pioggia10"] = ("Giorni di pioggia > 10 mm", "Rainy days > 10 mm"),
        ["campo.Pioggia20"] = ("Giorni di pioggia > 20 mm", "Rainy days > 20 mm"),
        ["campo.Gelo0"] = ("Giorni con temperatura < 0 °C", "Days with temperature < 0 °C"),
        ["campo.Gelo2"] = ("Giorni con temperatura < -2 °C", "Days with temperature < -2 °C"),
        ["campo.Gelo5"] = ("Giorni con temperatura < -5 °C", "Days with temperature < -5 °C"),
        ["campo.Caldo30"] = ("Giorni con temperatura > 30 °C", "Days with temperature > 30 °C"),
        ["campo.Caldo35"] = ("Giorni con temperatura > 35 °C", "Days with temperature > 35 °C"),
        ["meteo.nessun-dato"] = ("Nessun dato meteo per questo periodo.", "No weather data for this period."),

        ["suolo.titolo"] = ("Capacità Idrica del Suolo", "Soil water capacity"),
        ["suolo.capacita"] = ("Capacità di Campo mm/m", "Field capacity mm/m"),
        ["suolo.awc"] = ("AWC potenziale mm/m", "Potential AWC mm/m"),
        ["suolo.appassimento"] = ("Punto di Appassimento mm/m", "Wilting point mm/m"),
        ["suolo.profondita"] = ("Profondità del Suolo m", "Soil depth m"),
        ["suolo.pietrosita"] = ("Pietrosità del Suolo %", "Soil stoniness %"),
        ["suolo.awc-vigneto"] = ("AWC Reale Vigneto mm/m", "Actual AWC plot mm/m"),
        ["suolo.awc-vigna"] = ("AWC Reale Vigna mm/m", "Actual AWC vineyard mm/m"),
        ["suolo.conducibilita"] = ("Conducibilità idraulica a saturazione cm/h", "Saturated hydraulic conductivity cm/h"),
        ["suolo.vigna"] = ("Vigna", "Vineyard"),
        ["suolo.vigneto"] = ("Vigneto", "Plot"),
        ["suolo.nessuno"] = ("Nessun dato del suolo per questa azienda.", "No soil data for this estate."),
        ["scottature.media"] = ("Scottature - media ultimo giorno", "Sunburns - last day average"),
        ["scottature.max"] = ("Scottature - valore max", "Sunburns - max value"),
        ["scottature.danno"] = ("Danno Ponderato da Scottature", "Weighted sunburn damage"),
        ["scottature.diffusione"] = ("Diffusione Ponderata Sintomi Scottature", "Weighted sunburn symptoms spread"),
        ["scottature.rilievi"] = ("Numero di Rilievi", "Number of observations"),
        ["scottature.intensita"] = ("Intensità max %", "Max intensity %"),
        ["scottature.diffusione-max"] = ("Diffusione max %", "Max spread %"),
        ["vigoria.titolo"] = ("Vigoria", "Vigor"),
        ["vigoria.media"] = ("Vigoria media {0}", "Average vigor {0}"),
        ["vigoria.legno"] = ("Punteggio Legno di Potatura", "Pruning wood score"),
        ["vigoria.sfe"] = ("Punteggio SFE per Metro", "EFS per meter score"),
        ["vigoria.visiva"] = ("Punteggio Vigoria Visiva", "Visual vigor score"),
        ["vigoria.chioma"] = ("Punteggio gestione della Chioma", "Canopy management score"),
        ["vigoria.indice"] = ("Punteggio Indice di Vigoria", "Vigor index score"),
        ["vigoria.per-vigneto"] = ("Vigoria per vigneto e anno", "Vigor per plot and year"),
        ["vigoria.nessuna"] = ("Nessun dato di vigoria per questa azienda.", "No vigor data for this estate."),
        ["classe.eccessiva"] = ("Vigoria Eccessiva", "Excessive vigor"),
        ["classe.molto-alta"] = ("Vigoria Molto Alta", "Very high vigor"),
        ["classe.alta"] = ("Vigoria Alta", "High vigor"),
        ["classe.medio-alta"] = ("Vigoria Medio/Alta", "Medium/high vigor"),
        ["classe.media"] = ("Vigoria Media", "Medium vigor"),
        ["classe.medio-bassa"] = ("Vigoria Medio/Bassa", "Medium/low vigor"),
        ["classe.bassa"] = ("Vigoria Bassa", "Low vigor"),
        ["classe.molto-bassa"] = ("Vigoria Molto Bassa", "Very low vigor"),
        ["prod.vitigno"] = ("Produzioni per vitigno", "Production per variety"),
        ["prod.vigneto"] = ("Produzioni per vigneto", "Production per plot"),
        ["prod.confronto"] = ("Produzioni per vigneto, confronto anni (q/ha)", "Production comparison per plot (q/ha)"),
        ["prod.stime"] = ("Produzioni - scostamento con stime", "Production deviation from estimations"),
        ["prod.totale"] = ("Produzione totale (q)", "Total production (q)"),
        ["prod.ettaro"] = ("Produzione q/ha", "Production q/ha"),
        ["prod.eta"] = ("Età vigneto", "Plot age"),
        ["prod.vitigno-col"] = ("Vitigno", "Variety"),
        ["prod.ha"] = ("Ha", "Ha"),
        ["prod.reale"] = ("Produzione Reale (kg)", "Actual production (kg)"),
        ["prod.stima"] = ("Stime Produzione conta Grappoli (kg)", "Bunch count estimation (kg)"),
        ["prod.differenza"] = ("Differenza (kg)", "Difference (kg)"),
        ["prod.differenza-ha"] = ("Differenza ad Ettaro (kg)", "Difference per hectare (kg)"),
        ["prod.fallanze"] = ("% Fallanze", "% Failures"),
        ["prod.allevamento"] = ("% in allevamento", "% in training"),
        ["prod.nessuna"] = ("Nessuna produzione registrata per questa annata.", "No production recorded for this season."),
        ["campo.non-disponibile"] = ("Dati di campo non disponibili (snapshot dei dati assente).", "Field data not available (data snapshot missing)."),

        ["controlli.titolo"] =("Controlli sui dati prima di generare", "Data checks before generating"),
        ["controlli.ok"] = ("Tutti i controlli sono a posto per questa azienda.", "All checks are fine for this estate."),
        ["controlli.trovati"] = ("{0} controlli da guardare: i numeri del report potrebbero essere incompleti.",
            "{0} checks to review: the report figures may be incomplete."),
        ["controlli.righe"] = ("{0} righe", "{0} rows"),
        ["controlli.nessuno"] = ("Controlli non disponibili (snapshot dei dati assente).", "Checks not available (data snapshot missing)."),
        ["dati.snapshot"] = ("Dati 4Grapes del {0}", "4Grapes data as of {0}"),
        ["dati.diretti"] = ("Dati letti ora da 4Grapes, senza lo storico meteo", "Data read now from 4Grapes, without historical weather"),
        ["indice-bigot.apri"] = ("Apri l'Indice Bigot {0} dell'azienda", "Open the estate Bigot index {0}"),

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
