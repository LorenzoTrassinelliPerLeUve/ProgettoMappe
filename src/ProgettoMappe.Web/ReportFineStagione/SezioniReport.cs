namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Una sezione del report: numero romano, chiave del titolo in <see cref="TestiReport"/>, tappa in cui arriva.</summary>
public sealed record SezioneReport(string Numero, string Chiave, int Tappa, bool ConAnnata = false);

/// <summary>
/// Il modello del documento: le sezioni nell'ordine del sommario di Qlik (foglio 4 di «REPORT FINE STAGIONE»).
/// Le sezioni senza fogli nell'app Qlik (nutrizione, fallanze, potature, maturazione, «ad hoc», degustazione) le
/// compila il consulente a mano nel PowerPoint: qui restano titoli (<see cref="SezioneReport.Tappa"/> = 0).
/// </summary>
public static class SezioniReport
{
    /// <summary>La tappa del lavoro già fatta (le sezioni con tappa più alta si mostrano «in arrivo»).</summary>
    public const int TappaFatta = 4;

    public static readonly IReadOnlyList<SezioneReport> Tutte =
    [
        new("I", "sezione.caratterizzazione", 1, ConAnnata: true),
        new("II", "sezione.gestione-idrica", 3),
        new("III", "sezione.scottature", 3),
        new("IV", "sezione.vigoria", 3),
        new("V", "sezione.nutrizione", 0),
        new("VI", "sezione.sfe", 4),
        new("VII", "sezione.produzione", 3),
        new("VIII", "sezione.fallanze", 4),
        new("IX", "sezione.potature", 0),
        new("X", "sezione.sanita", 4),
        new("XI", "sezione.indice-bigot", 3),
        new("XII", "sezione.maturazione", 0),
        new("XIII", "sezione.ad-hoc", 0),
        new("XIV", "sezione.degustazione", 0, ConAnnata: true),
    ];

    /// <summary>Il titolo nella lingua scelta; la degustazione è dei vini dell'annata prima, come in Qlik (2025 nel 2026).</summary>
    public static string Titolo(SezioneReport sezione, LinguaReport lingua, int annata) =>
        !sezione.ConAnnata ? TestiReport.Testo(lingua, sezione.Chiave)
        : TestiReport.Testo(lingua, sezione.Chiave, sezione.Chiave == "sezione.degustazione" ? annata - 1 : annata);
}
