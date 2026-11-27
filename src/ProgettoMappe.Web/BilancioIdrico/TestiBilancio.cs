using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>Testi e formati del DSS Bilancio Idrico nelle pagine (task #335). Puro, testato.</summary>
public static class TestiBilancio
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");
    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    /// <summary>Il suggerimento in parole.</summary>
    public static string Suggerimento(string codice) => codice switch
    {
        "IRRIGARE" => "Irrigare",
        "NON_IRRIGARE" => "Non irrigare",
        "N_A" => "Senza suggerimento",
        _ => "Dato non valido",
    };

    /// <summary>Lettera per la striscia dei prossimi giorni.</summary>
    public static string Lettera(string codice) => codice switch
    {
        "IRRIGARE" => "I",
        "NON_IRRIGARE" => "·",
        "N_A" => "–",
        _ => "?",
    };

    /// <summary>Classe CSS del suggerimento (colore).</summary>
    public static string Classe(string codice) => codice switch
    {
        "IRRIGARE" => "bi-irrigare",
        "NON_IRRIGARE" => "bi-non-irrigare",
        "N_A" => "bi-na",
        _ => "bi-non-valido",
    };

    public static string Mpa(double? v) => v is { } x ? x.ToString("0.00", It) + " MPa" : "—";

    public static string Mm(double? v) => v is { } x ? x.ToString("0.0", It) + " mm" : "—";

    public static string Ore(double? v) => v is { } x ? x.ToString("0.0", It) + " h" : "—";

    public static string Numero(double? v, string formato = "0.00") => v is { } x ? x.ToString(formato, It) : "—";

    public static string Giorno(DateOnly d) => d.ToString("ddd d/M", It);

    /// <summary>
    /// Task #383: «Meteo preso dal vigneto 1234, a 3,2 km, per 12 giorni (dal 10/7 al 21/7)», più «e da altri N vicini» se il più
    /// vicino non aveva il dato tutti i giorni.
    /// </summary>
    public static string MeteoVicino(MeteoVicinoBilancioDto m)
    {
        var giorni = m.Giorni == 1 ? $"per 1 giorno ({m.Dal:d/M})" : $"per {m.Giorni} giorni (dal {m.Dal:d/M} al {m.Al:d/M})";
        var altri = m.Fonti switch { <= 1 => "", 2 => " e, in qualche giorno, da 1 altro vicino", _ => $" e, in qualche giorno, da altri {m.Fonti - 1} vicini" };
        return $"La sua azienda non ha meteo: meteo preso dal vigneto {m.IdVignetoFonte}, a {m.DistanzaKm.ToString("0.0", It)} km, {giorni}{altri}.";
    }

    /// <summary>«Calcolato il 18/11/2026 alle 04:12» in ora italiana.</summary>
    public static string Calcolato(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FusoItalia).ToString("'il' d/M/yyyy 'alle' HH:mm", It);

    /// <summary>La riga sul calcolo da cui vengono i numeri.</summary>
    public static string Calcolo(CalcoloBilancioDto? c) => c is null
        ? "Non ancora calcolato: il calcolo gira ogni notte alle 4, oppure premi «Ricalcola ora»."
        : c.CalcolatoIlUtc == default
            ? "Nessun calcolo riuscito."
            : $"Calcolato {Calcolato(c.CalcolatoIlUtc)}, stagione dal {c.Da:d/M} al {c.A:d/M/yyyy} · motore {c.VersioneMotore} · parametri {c.VersioneParametri}.";

    /// <summary>
    /// Task #349: quale insieme di regole usa il calcolo, in parole («Regole di Matteo Frandoli (18/11/2026)» o «Regole del
    /// vecchio motore 4g»), dal nome del preset in testa alla versione dei parametri; null senza calcolo.
    /// </summary>
    public static string? Preset(CalcoloBilancioDto? c) => c?.VersioneParametri switch
    {
        null => null,
        var v when v.StartsWith("ComeMatteo", StringComparison.Ordinal) =>
            "Regole in uso: risposte di Matteo Frandoli del 18/11/2026 (preset «ComeMatteo»).",
        var v when v.StartsWith("ComeOggi", StringComparison.Ordinal) => "Regole in uso: quelle del vecchio motore di 4g (preset «ComeOggi»).",
        var v => $"Regole in uso: {v}.",
    };

    /// <summary>Da dove viene un valore del suolo o lo stile, in parole.</summary>
    public static string Fonte(string? fonte, int vignetiNelSettore = 0) => fonte switch
    {
        "analisi" => "analisi del suolo del vigneto",
        "settore" => vignetiNelSettore > 1 ? $"media del settore ({vignetiNelSettore} vigneti con analisi)" : "media del settore",
        "calibrazione" => "calibrazione",
        "rilievo" => "rilievo delle radici (145)",
        "ripiego" => "valore di ripiego",
        "configurazione" => "configurazione",
        "target" => "dal target di concimazione",
        _ => "manca",
    };

    /// <summary>
    /// La soglia di oggi, o perché manca: «senza stile» se il vigneto non ha lo stile del vino, «senza fase» se lo stile c'è ma
    /// oggi manca il BBCH (task #340; il perché è nei dati mancanti del vigneto).
    /// </summary>
    public static string Soglia(StatoVignetoBilancioDto s) => s.SogliaMpa is { } x ? Mpa(x) : s.HaStile ? "senza fase" : "senza stile";

    /// <summary>Margine fra Pdlwp e soglia: negativo = sotto la soglia (stress oltre il voluto).</summary>
    public static string Margine(double? pdlwp, double? soglia) =>
        pdlwp is { } p && soglia is { } s ? (p - s >= 0 ? "+" : "") + (p - s).ToString("0.00", It) : "—";

    /// <summary>La criticità in parole (vuota se nessuna).</summary>
    public static string Criticita(StatoVignetoBilancioDto s) =>
        string.Join(", ", new[] { s.CriticitaPdlwp ? "stress al limite" : null, s.CriticitaDeficit ? "riserva d'acqua bassa" : null }
            .OfType<string>());

    /// <summary>«Aggiornato gio 26/11 alle 04:17» (task #378): la riga corta in cima; i dettagli del calcolo vanno a parte.</summary>
    public static string Aggiornato(CalcoloBilancioDto? c) => c is null
        ? "Non ancora calcolato: il calcolo gira ogni notte alle 4, oppure premi «Ricalcola ora»."
        : c.CalcolatoIlUtc == default
            ? "Nessun calcolo riuscito."
            : "Aggiornato " + TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(c.CalcolatoIlUtc, DateTimeKind.Utc), FusoItalia)
                .ToString("ddd d/M 'alle' HH:mm", It);

    /// <summary>
    /// Lo stress della vite di oggi in parole (task #378): il Pdlwp con la soglia e da che parte sta, senza MPa nudi.
    /// Il Pdlwp è negativo: sotto la soglia (più negativo) = più stress di quello voluto.
    /// </summary>
    public static string Stress(double? pdlwp, double? soglia) => (pdlwp, soglia) switch
    {
        (null, _) => "—",
        ({ } p, null) => Mpa(p),
        ({ } p, { } s) => $"{Mpa(p)} · soglia {Mpa(s)}" + (p < s ? " (oltre la soglia)" : ""),
    };

    /// <summary>«57 di 217 mm (26%)»: l'acqua nel suolo rispetto alla capacità.</summary>
    public static string Acqua(double aw, double? awc) => awc is > 0
        ? $"{aw.ToString("0", It)} di {awc.Value.ToString("0", It)} mm ({(aw / awc.Value * 100).ToString("0", It)}%)"
        : $"{aw.ToString("0", It)} mm (capacità del suolo non nota)";

    /// <summary>Il riassunto di oggi in cima alla pagina dell'azienda (task #378).</summary>
    public sealed record RiassuntoOggi(int Irrigare, int NonIrrigare, int SenzaSuggerimento, double? MmMassimi, string? Perche);

    /// <summary>
    /// Quanti vigneti irrigare oggi, quanti no, quanti senza suggerimento e, se nessuno ha un suggerimento, il perché principale
    /// (lo stile del vino o la fase), così chi apre la pagina capisce subito cosa fare.
    /// </summary>
    public static RiassuntoOggi Riassunto(IReadOnlyList<StatoVignetoBilancioDto> vigneti)
    {
        var irrigare = vigneti.Where(v => v.Suggerimento == "IRRIGARE").ToList();
        var non = vigneti.Count(v => v.Suggerimento == "NON_IRRIGARE");
        var senza = vigneti.Where(v => v.Suggerimento is not ("IRRIGARE" or "NON_IRRIGARE")).ToList();
        string? perche = null;
        if (senza.Count > 0)
        {
            var senzaStile = senza.Count(v => !v.HaStile);
            var senzaFase = senza.Count(v => v.HaStile && v.SogliaMpa is null);
            perche = senzaStile >= senzaFase && senzaStile > 0
                ? $"a {senzaStile} {(senzaStile == 1 ? "vigneto" : "vigneti")} manca lo stile del vino (si imposta nella configurazione)"
                : senzaFase > 0
                    ? $"{senzaFase} {(senzaFase == 1 ? "vigneto è" : "vigneti sono")} senza fase BBCH oggi (serve un rilievo della fenologia)"
                    : "dati non validi: vedi «Dati mancanti» in fondo";
        }
        return new(irrigare.Count, non, senza.Count, irrigare.Count > 0 ? irrigare.Max(v => v.IrrigazioneSuggeritaMm) : null, perche);
    }

    /// <summary>
    /// Cosa fare per un dato mancante e a chi tocca (task #378), dal codice dell'anomalia dell'Api; null se non c'è niente da fare.
    /// </summary>
    public static string? CosaFare(string codice) => codice switch
    {
        "suolo" => "Inserire l'analisi del suolo (rilievo 144) in 4Grapes, oppure scrivere l'AWC nella calibrazione della configurazione.",
        "stile" => "Scegliere lo stile del vino nella configurazione di Mappe (o il target di concimazione in 4Grapes).",
        "fenologia" => "Registrare un rilievo della fenologia (BBCH) con l'app 4Grapes.",
        "radici" => "Facoltativo: rilievo delle radici (145) in 4Grapes, o profondità e larghezza delle radici nella calibrazione.",
        "non-attivi" => "Controllare il vigneto in 4Grapes, oppure toglierlo dal settore nella configurazione.",
        "senza-settore" => "Se il vigneto è irrigato, metterlo in un settore nella configurazione: arrivano le ore di irrigazione.",
        "meteo" => "Avvisare Perleuve: mancano dati della stazione meteo dell'azienda in 4Grapes.",
        "quote" => "Controllare le quote dei vigneti nei settori, nella configurazione.",
        "tempo-scaduto" => "Niente da fare: si riprova da solo al prossimo calcolo.",
        var c when c.StartsWith("permesso", StringComparison.Ordinal) => "Serve un permesso sul database 4Grapes: lo concede il proprietario.",
        _ => null,
    };

    /// <summary>
    /// Per le note dei vigneti: le anomalie senza quella «senza settore» (già in «Da configurare») e senza quelle che valgono per
    /// tutti i vigneti (già in «Dati mancanti» una volta sola): ripeterle su ogni riga copriva quelle del singolo vigneto.
    /// </summary>
    public static IReadOnlyList<string> AnomalieRiga(StatoVignetoBilancioDto v, IReadOnlyList<AnomaliaBilancioDto> azienda, int vignetiAzienda)
    {
        var giaDette = azienda.Where(a => a.Codice == "senza-settore" || (vignetiAzienda > 1 && a.Vigneti.Count >= vignetiAzienda))
            .Select(a => a.Testo).ToHashSet();
        return v.Anomalie.Where(a => !giaDette.Contains(a)).ToList();
    }
}
