using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Esperienze;

/// <summary>Cosa può fare un'esperienza su una pagina: niente (non c'è nel menù), guardarla, cambiarne i dati.</summary>
public enum Accesso { Nascosto, Vede, Modifica }

/// <summary>
/// Una pagina o sezione e il suo accesso nelle tre esperienze (task #401). <paramref name="Sezione"/> = sezione del Portale
/// (si apre dentro la pagina dell'esperienza), <paramref name="Fuori"/> = un'altra pagina di Mappe o un'app di Jan
/// (<c>{azienda}</c> = l'azienda scelta; senza azienda vale <paramref name="FuoriSenzaAzienda"/>).
/// </summary>
public sealed record VoceEsperienza(
    string Chiave, string Titolo, string Frase,
    Accesso Interno, Accesso Consulenza, Accesso Abbonato,
    string? Sezione = null, string? Fuori = null, string? FuoriSenzaAzienda = null, string? Nota = null)
{
    public Accesso Per(string esperienza) => esperienza switch
    {
        TipiEsperienza.Interno => Interno,
        TipiEsperienza.Consulenza => Consulenza,
        TipiEsperienza.Abbonato => Abbonato,
        _ => Accesso.Nascosto,
    };

    public bool App => Fuori?.StartsWith("https://", StringComparison.Ordinal) == true;
}

public sealed record GruppoEsperienza(string Titolo, string Icona, IReadOnlyList<VoceEsperienza> Voci);

/// <summary>Il menù di un'esperienza: pagina iniziale, gruppi, barra del telefono (quattro chiavi; null = pagina iniziale).</summary>
public sealed record MenuEsperienza(string Esperienza, string Nome, string Iniziale, string Frase,
    IReadOnlyList<GruppoEsperienza> Gruppi, IReadOnlyList<(string Testo, string Icona, string? Voce)> Barra);

/// <summary>
/// La tabella pagine × esperienze della proposta (docs/specifiche/tre-esperienze/PROPOSTA.md) e i tre menù. Unica fonte:
/// la tabella della proposta la ripete, i test controllano che ogni sezione del Portale e ogni pagina di Mappe ci sia, che
/// ogni voce visibile stia una volta sola nel menù della sua esperienza e che le nascoste non ci siano.
/// </summary>
public static class MenuEsperienze
{
    public const string Percorso = "esperienza";

    private const Accesso N = Accesso.Nascosto, V = Accesso.Vede, M = Accesso.Modifica;

    /// <summary>Tutte le pagine di oggi, nell'ordine della proposta.</summary>
    public static readonly IReadOnlyList<VoceEsperienza> Voci =
    [
        // Pagine di Mappe.
        new("aziende", "Aziende", "L'elenco delle aziende con vigneti, ettari e indice", V, N, N, Fuori: "/"),
        new("mappa", "Mappa", "I vigneti sulla mappa, con i tematismi", M, V, M, Fuori: "/mappa/{azienda}", FuoriSenzaAzienda: "/",
            Nota: "modifica = filari disegnati"),
        new("stampa", "Stampa della mappa", "Il foglio A4/A3 dei vigneti", V, V, V, Fuori: "/stampa/{azienda}", FuoriSenzaAzienda: "/"),
        new("avvisi", "Avvisi", "Quello che non va, vigneto per vigneto", V, V, V, Fuori: "/avvisi?azienda={azienda}", FuoriSenzaAzienda: "/avvisi"),
        new("bilancio-idrico", "Bilancio idrico", "Acqua nel suolo e quando irrigare", V, V, V,
            Fuori: "/bilancio-idrico?azienda={azienda}", FuoriSenzaAzienda: "/bilancio-idrico"),
        new("bilancio-configurazione", "Configura il bilancio idrico", "Stile del vino, suolo, impianti, irrigazioni", M, N, M,
            Fuori: "/bilancio-idrico/configurazione?azienda={azienda}", FuoriSenzaAzienda: "/bilancio-idrico/configurazione",
            Nota: "abbonati: stile del vino, impianti e irrigazioni; «Per esperti» solo interni"),
        new("indice-bigot", "Indice Bigot", "La qualità del vigneto, anno per anno", V, V, V,
            Fuori: "/indice-bigot?azienda={azienda}", FuoriSenzaAzienda: "/indice-bigot"),
        new("report-settimanale", "Report settimanale", "La settimana in vigna, da scaricare", V, V, V,
            Fuori: "/report-settimanale?azienda={azienda}", FuoriSenzaAzienda: "/report-settimanale"),
        new("report-settimanale-prepara", "Prepara il settimanale", "Nascondi, commenta e pubblica il report per il cliente", M, N, M,
            Fuori: "/report-settimanale/prepara?azienda={azienda}", FuoriSenzaAzienda: "/report-settimanale/prepara",
            Nota: "task #416: il cliente in consulenza vede solo i report pubblicati"),
        new("report-fine-stagione", "Report di fine stagione", "Il riassunto dell'annata", V, V, V,
            Fuori: "/report-fine-stagione?azienda={azienda}", FuoriSenzaAzienda: "/report-fine-stagione"),
        new("report-laore", "Report LAORE", "Il settimanale per provincia e comune", V, N, N, Fuori: "/report-settimanale-laore"),
        new("report-suolo", "Report del suolo", "Le analisi del terreno in un report", V, V, V,
            Fuori: "/report-analisi-suolo?azienda={azienda}", FuoriSenzaAzienda: "/report-analisi-suolo"),
        new("report", "Tutti i report", "Il menù Report: dati da inserire e report", V, V, V, Fuori: "/report"),
        new("nutrizione", "Decision 4Grapes Nutrizione", "Il menù del vecchio sotto-portale", V, N, N, Fuori: "/nutrizione",
            Nota: "le sue sezioni sono già nelle voci del Portale"),
        new("guida", "Guida", "Come si usa", V, V, V, Fuori: "/guida"),

        // Sezioni del Portale (Api/Portale/SezioniPortale).
        new("home", "Home del Portale", "Lo smistamento verso le sezioni", N, N, N, Sezione: "home",
            Nota: "la sostituisce la pagina iniziale di ogni esperienza"),
        new("indicatori", "Rischio di infezione", "Peronospora e oidio per azienda e comune", V, N, V, Sezione: "indicatori"),
        new("vigneti", "Vigneti", "Elenco e scheda di ogni vigneto", M, V, M, Sezione: "vigneti",
            Nota: "abbonati: solo i propri vigneti"),
        new("rilievi", "Scarica i rilievi", "Tutti i rilievi in Excel", V, V, V, Sezione: "rilievi"),
        new("produzione", "Vendemmia e obiettivo", "Rilievi di vendemmia e obiettivo produttivo", M, V, M, Sezione: "produzione"),
        new("fallanze", "Fallanze", "Piante mancanti e da rimpiazzare", M, V, M, Sezione: "fallanze"),
        new("analisi-uve", "Maturazione delle uve", "Analisi delle uve", M, V, M, Sezione: "analisi-uve"),
        new("analisi-terreni", "Analisi del suolo", "Le analisi di laboratorio", M, V, M, Sezione: "analisi-terreni"),
        new("galleria", "Foto", "Le foto dei rilievi", M, V, M, Sezione: "galleria"),
        new("trattamenti", "Trattamenti", "Cosa trattare, cosa è stato fatto", M, V, M, Sezione: "trattamenti",
            Nota: "consulenza: fatti e consigliati, senza modificare"),
        new("bollettini", "Bollettini", "Prepara e manda il bollettino", M, V, V, Sezione: "bollettini",
            Nota: "consulenza e abbonati: quelli ricevuti (archivio)"),
        new("linee-difesa", "Linee di difesa", "Prodotti e dosi per linea", M, N, V, Sezione: "linee-difesa"),
        new("soglie-trattamenti", "Soglie", "Quando scatta un trattamento", M, N, N, Sezione: "soglie-trattamenti"),
        new("avversita", "Avversità", "L'elenco delle avversità", V, N, N, Sezione: "avversita"),
        new("irroratrici", "Catalogo irroratrici", "I modelli di irroratrice", M, N, V, Sezione: "irroratrici"),
        new("irroratrici-azienda", "Irroratrici dell'azienda", "Le macchine dell'azienda", M, V, M, Sezione: "irroratrici-azienda"),
        new("concimazioni", "Concimazioni", "Unità, fattori, obiettivo e resoconto", M, V, M, Sezione: "concimazioni",
            Nota: "consulenza: il resoconto"),
        new("asportazioni", "Asportazioni", "Quanto porta via la vendemmia", V, V, V, Sezione: "asportazioni"),
        new("dashboard-nutrizione", "Numeri del database", "Quanti utenti, vigneti, rilievi", V, N, N, Sezione: "dashboard-nutrizione"),
        new("notifiche", "Notifiche", "Un messaggio sul telefono degli utenti", M, N, N, Sezione: "notifiche"),
        new("utenti", "Utenti", "Chi entra e cosa può fare", M, N, N, Sezione: "utenti"),
        new("abilitazioni", "Ruoli e permessi", "Cosa vede ogni ruolo", M, N, N, Sezione: "abilitazioni"),
        new("gruppi-utenti", "Gruppi di utenti", "Per mandare messaggi a più persone", M, N, N, Sezione: "gruppi-utenti"),
        new("impostazioni", "Impostazioni", "Valori comuni a tutti", M, N, N, Sezione: "impostazioni"),
        new("account", "Il mio profilo", "Nome, lingua, password", M, M, M, Sezione: "account"),

        // App di Jan (collegamenti con l'accesso unico).
        new("app-suite", "Suite e turni", "L'app di Jan per il lavoro in squadra", M, N, M, Fuori: "https://suite.77-42-19-27.sslip.io"),
        new("app-potatura", "Potatura", "Il riassunto della potatura", M, N, M, Fuori: "https://potatura.77-42-19-27.sslip.io"),
        new("app-4sprayers", "4Sprayers", "Prove e regolazione delle irroratrici", M, N, M, Fuori: "https://4sprayers.77-42-19-27.sslip.io"),
    ];

    private static VoceEsperienza Voce(string chiave) => Voci.First(v => v.Chiave == chiave);

    private static GruppoEsperienza Gruppo(string titolo, string icona, params string[] chiavi) =>
        new(titolo, icona, chiavi.Select(Voce).ToList());

    public static readonly MenuEsperienza Interno = new(TipiEsperienza.Interno, "Perleuve", "Oggi",
        "Il lavoro: cosa c'è da fare oggi in tutte le aziende che segui.",
        [
            Gruppo("Aziende e vigneti", "🗺", "aziende", "mappa", "vigneti", "galleria", "stampa"),
            Gruppo("Rilievi e monitoraggio", "🔎", "rilievi", "fallanze", "analisi-uve", "indicatori", "avvisi"),
            Gruppo("Difesa", "🛡", "trattamenti", "bollettini", "linee-difesa", "soglie-trattamenti", "avversita",
                "irroratrici-azienda", "irroratrici"),
            Gruppo("Nutrizione, acqua e suolo", "🌱", "bilancio-idrico", "bilancio-configurazione", "concimazioni",
                "analisi-terreni", "asportazioni", "nutrizione"),
            Gruppo("Report da preparare", "📄", "report", "report-settimanale", "report-settimanale-prepara", "report-fine-stagione", "report-laore", "report-suolo",
                "indice-bigot", "produzione"),
            Gruppo("Persone e impostazioni", "👥", "utenti", "abilitazioni", "gruppi-utenti", "notifiche", "impostazioni",
                "dashboard-nutrizione", "account"),
            Gruppo("App", "📱", "app-suite", "app-potatura", "app-4sprayers", "guida"),
        ],
        [("Oggi", "☀", null), ("Difesa", "🛡", "trattamenti"), ("Rilievi", "🔎", "rilievi"), ("Aziende", "🗺", "aziende")]);

    public static readonly MenuEsperienza Consulenza = new(TipiEsperienza.Consulenza, "Azienda in consulenza", "La mia azienda",
        "Consultazione: com'è la tua azienda, cosa ti consigliamo, i report. Qui si guarda, non si configura.",
        [
            Gruppo("La mia azienda", "🏡", "mappa", "vigneti", "galleria", "rilievi", "avvisi"),
            Gruppo("Difesa", "🛡", "trattamenti", "bollettini", "irroratrici-azienda"),
            Gruppo("Acqua e suolo", "💧", "bilancio-idrico", "concimazioni", "analisi-terreni", "asportazioni"),
            Gruppo("Qualità e vendemmia", "🍇", "indice-bigot", "produzione", "analisi-uve", "fallanze"),
            Gruppo("Report", "📄", "report", "report-settimanale", "report-fine-stagione", "report-suolo", "stampa"),
            Gruppo("Io", "👤", "account", "guida"),
        ],
        [("Azienda", "🏡", null), ("Difesa", "🛡", "trattamenti"), ("Acqua", "💧", "bilancio-idrico"), ("Report", "📄", "report-settimanale")]);

    public static readonly MenuEsperienza Abbonato = new(TipiEsperienza.Abbonato, "Abbonato", "Oggi",
        "Le tue aziende: quello che fai in vigna e quello che ti dice 4Grapes.",
        [
            Gruppo("In vigna", "✍", "trattamenti", "produzione", "fallanze", "analisi-uve", "analisi-terreni", "galleria"),
            Gruppo("I miei vigneti", "🏡", "vigneti", "mappa", "irroratrici-azienda", "bilancio-configurazione", "concimazioni"),
            Gruppo("Consulta", "📊", "bilancio-idrico", "indice-bigot", "indicatori", "avvisi", "bollettini", "linee-difesa",
                "asportazioni", "rilievi"),
            Gruppo("Report", "📄", "report", "report-settimanale", "report-settimanale-prepara", "report-fine-stagione", "report-suolo", "stampa"),
            Gruppo("App e profilo", "📱", "app-suite", "app-potatura", "app-4sprayers", "irroratrici", "account", "guida"),
        ],
        [("Oggi", "☀", null), ("Trattamenti", "🛡", "trattamenti"), ("Vigneti", "🏡", "vigneti"), ("Acqua", "💧", "bilancio-idrico")]);

    public static MenuEsperienza Di(string esperienza) => esperienza switch
    {
        TipiEsperienza.Consulenza => Consulenza,
        TipiEsperienza.Abbonato => Abbonato,
        _ => Interno,
    };

    /// <summary>
    /// I gruppi con le voci che l'utente può aprire: una sezione del Portale solo se i permessi di 4Grapes la danno
    /// (l'esperienza restringe, non allarga), le pagine fuori dal Portale sempre. I gruppi vuoti spariscono.
    /// </summary>
    public static IReadOnlyList<GruppoEsperienza> Visibili(MenuEsperienza menu, PortaleUtenteDto utente)
    {
        var aperte = utente.Sezioni.Where(s => s.Accessibile).Select(s => s.Chiave).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return menu.Gruppi
            .Select(g => g with { Voci = g.Voci.Where(v => v.Sezione is null || aperte.Contains(v.Sezione)).ToList() })
            .Where(g => g.Voci.Count > 0)
            .ToList();
    }

    /// <summary>L'indirizzo di una voce. Le pagine dell'esperienza tengono l'azienda e il «Vedi come…» (<paramref name="come"/>).</summary>
    public static string Indirizzo(string esperienza, VoceEsperienza? voce, int? azienda, string? come = null)
    {
        if (voce?.Fuori is { } fuori)
        {
            return fuori.Contains("{azienda}", StringComparison.Ordinal)
                ? azienda is { } a ? fuori.Replace("{azienda}", a.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                    : voce.FuoriSenzaAzienda ?? "/"
                : fuori;
        }

        var query = new List<string>();
        if (come is not null) query.Add("come=" + come);
        if (azienda is { } b) query.Add("azienda=" + b);
        return $"/{Percorso}" + (voce is null ? "" : "/" + voce.Chiave) + (query.Count > 0 ? "?" + string.Join("&", query) : "");
    }
}
