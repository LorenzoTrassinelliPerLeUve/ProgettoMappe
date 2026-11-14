using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;

namespace ProgettoMappe.Web.ReportSettimanaleLaore;

/// <summary>
/// Le sezioni del report LAORE nell'ordine dei 17 fogli dell'app Qlik (nessuna automazione PowerPoint usa quest'app:
/// principale, task #216). Il foglio «Filtri» è la copertina.
/// </summary>
public static class SezioniLaore
{
    public static readonly IReadOnlyList<string> Tutte =
    [
        "sezione.copertina", "sezione.dashboard",
        "sezione.bbch-foglie", "sezione.bbch-grappoli", "sezione.bbch-storico-foglie", "sezione.bbch-storico-grappoli",
        .. CalcoloLaore.Malattie.Select(m => m.Chiave),
        "sezione.cryptoblabes", "sezione.lobesia", "sezione.esca",
    ];
}

/// <summary>
/// I testi del report LAORE, solo in italiano (l'app non ha la versione ENG). Le chiavi che non sono qui (esca, controlli,
/// scarico, date dello snapshot…) vengono da <see cref="TestiSettimanale"/>: un testo comune si scrive una volta sola.
/// </summary>
public static class TestiLaore
{
    private static readonly Dictionary<string, string> Testi = new()
    {
        ["titolo"] = "Report settimanale LAORE",
        ["pagina.sottotitolo"] = "Il report settimanale del progetto LAORE (Sardegna) per provincia, come l'app Qlik «REPORT SETTIMANALE - LAORE».",
        ["filtro.provincia"] = "Provincia",
        ["filtro.comune"] = "Comune",
        ["filtro.tutte"] = "Tutte",
        ["filtro.tutti"] = "Tutti",
        ["territorio.sardegna"] = "Sardegna",
        ["errore.luoghi"] = "I vigneti del perimetro LAORE non si leggono adesso.",
        ["nessuno.snapshot"] = "I dati LAORE non sono ancora nello snapshot del dba: il report si riempie appena arrivano.",
        ["presenze"] = "Mal dell'esca rilevato: {0} · Tignole presenti: {1}",
        ["si"] = "sì",
        ["no"] = "no",
        ["sezione.copertina"] = "Filtri",
        ["sezione.dashboard"] = "Dashboard",
        ["sezione.bbch-foglie"] = "Fenologia – Foglie",
        ["sezione.bbch-grappoli"] = "Fenologia – Grappoli",
        ["sezione.bbch-storico-foglie"] = "Fenologia – Foglie, confronto storico",
        ["sezione.bbch-storico-grappoli"] = "Fenologia – Grappoli, confronto storico",
        ["sezione.peronospora-int-foglia"] = "Danno da Peronospora su Foglia {0}",
        ["sezione.peronospora-diff-foglia"] = "Diffusione Peronospora su Foglia {0}",
        ["sezione.oidio-int-foglia"] = "Danno da Oidio su Foglia {0}",
        ["sezione.oidio-diff-foglia"] = "Diffusione Oidio su Foglia {0}",
        ["sezione.peronospora-int-grappolo"] = "Danno da Peronospora su Grappolo {0}",
        ["sezione.peronospora-diff-grappolo"] = "Diffusione Peronospora su Grappolo {0}",
        ["sezione.oidio-int-grappolo"] = "Danno da Oidio su Grappolo {0}",
        ["sezione.oidio-diff-grappolo"] = "Diffusione Oidio su Grappolo {0}",
        ["sezione.cryptoblabes"] = "Catture per trappola – Cryptoblabes",
        ["sezione.lobesia"] = "Catture per trappola – Lobesia botrana",
        ["sezione.esca"] = "Diffusione Mal dell'Esca {0}",
        ["bbch"] = "BBCH",
        ["bbch.anno"] = "BBCH Anno in Corso",
        ["bbch.precedente"] = "BBCH Anno Precedente",
        ["bbch.storico"] = "BBCH Media Storica",
        ["nota.settimana"] = "Rilievi della settimana {0}, per provincia.",
        ["nota.stagione"] = "Media per vigneto e settimana, poi sui vigneti; dall'inizio dell'anno a questa settimana.",
        ["nota.storico"] = "Media storica = i cinque anni prima.",
        ["catture"] = "Catture per trappola",
        ["provincia"] = "Provincia",
        ["vigneti.perimetro"] = "Vigneti del perimetro",
    };

    public static IEnumerable<string> Chiavi => Testi.Keys;

    public static string Testo(string chiave, params object[] valori) =>
        Testi.TryGetValue(chiave, out var testo)
            ? valori.Length == 0 ? testo : string.Format(TestiReport.Cultura(LinguaReport.It), testo, valori)
            : TestiSettimanale.Testo(LinguaReport.It, chiave, valori);
}
