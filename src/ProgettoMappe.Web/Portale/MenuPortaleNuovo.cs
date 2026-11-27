using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Una voce del menù nuovo del Portale (prototipo, task #387). <paramref name="Sezione"/> = la chiave della sezione di oggi
/// (<c>SezioniPortale</c> dell'Api) che decide chi la vede; <paramref name="Fuori"/> = un'altra pagina di Mappe (si vede sempre).
/// </summary>
public sealed record VoceMenuNuovo(string Chiave, string Titolo, string Frase, string? Sezione = null, string? Fuori = null);

/// <summary>Un gruppo del menù: un lavoro da fare, non una tabella del database.</summary>
public sealed record GruppoMenuNuovo(string Chiave, string Titolo, string Icona, IReadOnlyList<VoceMenuNuovo> Voci);

/// <summary>
/// Il menù del Portale raggruppato per lavoro (docs/specifiche/portale-ux/PROPOSTA.md): «Oggi» in cima, poi sette gruppi.
/// Ogni sezione di oggi compare una volta sola; le pagine di Mappe che fanno lo stesso lavoro stanno nel gruppo giusto.
/// </summary>
public static class MenuPortaleNuovo
{
    public const string Percorso = "portale-nuovo";

    public static readonly IReadOnlyList<GruppoMenuNuovo> Gruppi =
    [
        new("vigneti", "Vigneti e mappa", "🗺", [
            new("vigneti", "Vigneti", "Elenco e scheda di ogni vigneto, export", "vigneti"),
            new("galleria", "Foto", "Le foto dei rilievi", "galleria"),
            new("mappa", "Mappa", "I vigneti sulla mappa, con i tematismi", Fuori: "/"),
        ]),
        new("rilievi", "Rilievi e monitoraggio", "🔎", [
            new("rilievi", "Scarica i rilievi", "Tutti i rilievi in Excel", "rilievi"),
            new("fallanze", "Fallanze", "Piante mancanti e da rimpiazzare", "fallanze"),
            new("analisi-uve", "Maturazione delle uve", "Analisi delle uve", "analisi-uve"),
            new("indicatori", "Rischio di infezione", "Peronospora e oidio per azienda e comune", "indicatori"),
        ]),
        new("difesa", "Difesa", "🛡", [
            new("trattamenti", "Trattamenti", "Cosa trattare, cosa è stato fatto", "trattamenti"),
            new("linee-difesa", "Linee di difesa", "Prodotti e dosi per linea", "linee-difesa"),
            new("soglie-trattamenti", "Soglie", "Quando scatta un trattamento", "soglie-trattamenti"),
            new("irroratrici-azienda", "Irroratrici dell'azienda", "Le macchine dell'azienda", "irroratrici-azienda"),
        ]),
        new("nutrizione", "Nutrizione e suolo", "🌱", [
            new("concimazioni", "Concimazioni", "Unità, fattori, obiettivo e resoconto", "concimazioni"),
            new("analisi-terreni", "Analisi del suolo", "Le analisi di laboratorio", "analisi-terreni"),
            new("asportazioni", "Asportazioni", "Quanto porta via la vendemmia", "asportazioni"),
            new("bilancio-idrico", "Bilancio idrico", "Acqua nel suolo e irrigazione", Fuori: "/bilancio-idrico"),
            new("report-suolo", "Report del suolo", "Il report delle analisi", Fuori: "/report-analisi-suolo"),
        ]),
        new("produzione", "Produzione e vendemmia", "🍇", [
            new("produzione", "Vendemmia e obiettivo", "Rilievi di vendemmia e obiettivo produttivo", "produzione"),
            new("indice-bigot", "Indice Bigot", "L'indice della qualità del vigneto", Fuori: "/indice-bigot"),
            new("fine-stagione", "Report di fine stagione", "Il riassunto dell'annata", Fuori: "/report-fine-stagione"),
        ]),
        new("comunicare", "Avvisi e bollettini", "📣", [
            new("bollettini", "Bollettini", "Prepara e manda il bollettino", "bollettini"),
            new("notifiche", "Notifiche", "Un messaggio sul telefono degli utenti", "notifiche"),
            new("avvisi", "Avvisi", "Quello che non va, azienda per azienda", Fuori: "/avvisi"),
        ]),
        new("persone", "Persone e impostazioni", "👥", [
            new("utenti", "Utenti", "Chi entra e cosa può fare", "utenti"),
            new("gruppi-utenti", "Gruppi di utenti", "Per mandare messaggi a più persone", "gruppi-utenti"),
            new("abilitazioni", "Ruoli e permessi", "Cosa vede ogni ruolo", "abilitazioni"),
            new("avversita", "Avversità", "L'elenco delle avversità", "avversita"),
            new("irroratrici", "Catalogo irroratrici", "I modelli di irroratrice", "irroratrici"),
            new("dashboard-nutrizione", "Numeri del database", "Quanti utenti, vigneti, rilievi", "dashboard-nutrizione"),
            new("impostazioni", "Impostazioni", "Valori comuni a tutti", "impostazioni"),
            new("account", "Il mio profilo", "Nome, lingua, password", "account"),
        ]),
    ];

    /// <summary>I gruppi con le sole voci che l'utente può aprire (le pagine fuori dal Portale sempre); i gruppi vuoti spariscono.</summary>
    public static IReadOnlyList<GruppoMenuNuovo> Visibili(PortaleUtenteDto utente)
    {
        var aperte = utente.Sezioni.Where(s => s.Accessibile).Select(s => s.Chiave).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Gruppi
            .Select(g => g with { Voci = g.Voci.Where(v => v.Fuori is not null || aperte.Contains(v.Sezione!)).ToList() })
            .Where(g => g.Voci.Any(v => v.Fuori is null))
            .ToList();
    }

    /// <summary>La voce dal gruppo e dalla chiave dell'indirizzo, null se non c'è.</summary>
    public static (GruppoMenuNuovo Gruppo, VoceMenuNuovo Voce)? Trova(IReadOnlyList<GruppoMenuNuovo> gruppi, string? gruppo, string? voce) =>
        gruppi.Where(g => g.Chiave.Equals(gruppo, StringComparison.OrdinalIgnoreCase))
            .SelectMany(g => g.Voci.Where(v => v.Chiave.Equals(voce, StringComparison.OrdinalIgnoreCase)).Select(v => (g, v)))
            .Select(t => ((GruppoMenuNuovo, VoceMenuNuovo)?)t)
            .FirstOrDefault();

    /// <summary>L'indirizzo di una voce, con l'azienda scelta che resta quando si cambia sezione.</summary>
    public static string Indirizzo(GruppoMenuNuovo gruppo, VoceMenuNuovo voce, int? azienda) =>
        voce.Fuori is { } fuori
            ? fuori + (azienda is { } a && fuori != "/" ? $"?azienda={a}" : "")
            : $"/{Percorso}/{gruppo.Chiave}/{voce.Chiave}" + (azienda is { } b ? $"?azienda={b}" : "");
}
