namespace ProgettoMappe.Web.Report;

/// <summary>Una voce del menù Report: pagina dati (inserire, preparare) o pagina del report.</summary>
public enum TipoVoceReport { Dati, Report, Territorio }

/// <summary>
/// Una voce del menù Report. <paramref name="Indirizzo"/> null = non ancora fatta: si vede solo nella pagina «Report» come
/// «in arrivo» con la sua <paramref name="Tappa"/> del progetto (docs/specifiche/reportistica/PROGETTO.md §6), mai nella tendina.
/// </summary>
public sealed record VoceReport(string Titolo, TipoVoceReport Tipo, string? Indirizzo, string? Tappa = null)
{
    public bool Pronta => Indirizzo is not null;
}

public sealed record GruppoReport(string Titolo, string Icona, string Frase, IReadOnlyList<VoceReport> Voci);

/// <summary>
/// Il menù «Report» unico (task #413, tappa R0 della Reportistica): sostituisce nella barra alta Indice Bigot, Report fine
/// stagione, Report settimanale, Settimanale LAORE, Report analisi del suolo e Decision 4Grapes Nutrizione. Unica fonte per la
/// tendina della barra (<c>Components/Layout/TendinaReport</c>) e per la pagina <c>/report</c>; i test controllano che ogni
/// indirizzo sia una pagina che esiste.
/// </summary>
public static class MenuReport
{
    public const string Percorso = "/report";

    private const TipoVoceReport D = TipoVoceReport.Dati, R = TipoVoceReport.Report, T = TipoVoceReport.Territorio;

    public static readonly IReadOnlyList<GruppoReport> Gruppi =
    [
        new("Analisi suolo", "🟫", "Le analisi del terreno: inserirle e farne il report",
        [
            new("Inserimento analisi suolo", D, "/nutrizione/analisi-dei-terreni"),
            new("Report analisi suolo", R, "/report-analisi-suolo"),
            new("Prepara il report del suolo", D, "/report/prepara?report=suolo"),
            new("Decision 4Grapes Nutrizione", D, "/nutrizione"),
        ]),
        new("Maturazione uve", "🍇", "Le analisi delle uve verso la vendemmia",
        [
            new("Inserimento analisi uve", D, "/nutrizione/analisi-uve"),
            new("Report maturazione uve", R, null, "R9"),
        ]),
        new("Meteo", "🌦", "I dati meteo che entrano in 4Grapes e il loro report",
        [
            new("Flussi in entrata", D, null, "R1"),
            new("Dati giornalieri e correzioni", D, null, "R7"),
            new("Stazioni meteo", D, null, "R7"),
            new("Report meteo", R, null, "R8"),
        ]),
        new("Indice Bigot", "📈", "La qualità del vigneto, anno per anno",
        [
            new("Report Indice Bigot", R, "/indice-bigot"),
            new("Prepara il report Indice Bigot", D, "/report/prepara?report=ib"),
            new("Obiettivi dei parametri", D, null, "R6"),
            new("A che punto sono", D, null, "R6"),
        ]),
        new("Report settimanale", "🗓", "La settimana in vigna, per azienda o per territorio",
        [
            new("Report settimanale", R, "/report-settimanale"),
            new("Prepara e pubblica", D, "/report-settimanale/prepara"),
            new("Settimanale LAORE", T, "/report-settimanale-laore"),
        ]),
        new("Report metà stagione", "☀", "L'annata fino all'invaiatura",
        [
            new("Report metà stagione", R, null, "R5"),
        ]),
        new("Report fine stagione", "🍂", "Il riassunto dell'annata",
        [
            new("Report fine stagione", R, "/report-fine-stagione"),
            new("Prepara il report fine stagione", D, "/report/prepara?report=rfs"),
        ]),
        new("Tutti i report", "📋", "Preparare, pubblicare, ritrovare",
        [
            new("Da preparare questa settimana", D, null, "R2"),
            new("Pubblicati di recente", D, null, "R2"),
            new("Archivio per azienda", D, null, "R2"),
        ]),
    ];

    /// <summary>Le pagine che oggi stanno sotto «Report», per accendere la voce nella barra.</summary>
    public static IEnumerable<string> Indirizzi =>
        Gruppi.SelectMany(g => g.Voci).Where(v => v.Pronta).Select(v => v.Indirizzo!.Split('?')[0]).Append(Percorso).Distinct();

    /// <summary>Vero se il percorso (senza query, con o senza «/» iniziale) è una pagina del menù Report o una sua sotto-pagina.</summary>
    public static bool Contiene(string percorso)
    {
        var p = "/" + percorso.Split('?', '#')[0].Trim('/');
        return Indirizzi.Any(i => p.Equals(i, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith(i + "/", StringComparison.OrdinalIgnoreCase));
    }

    public static string Etichetta(TipoVoceReport tipo) => tipo switch
    {
        TipoVoceReport.Dati => "dati",
        TipoVoceReport.Territorio => "territorio",
        _ => "report",
    };
}
