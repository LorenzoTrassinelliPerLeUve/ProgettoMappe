using ProgettoMappe.Web.Report;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>
/// Il catalogo del report Indice Bigot per la reportistica (task #418, tappa R4). Edizione = anno dell'indice. Le sezioni sono quelle
/// del PowerPoint (<see cref="PowerPointIndiceBigot"/>); nella pagina «indice e KPI» è la riga dei numeri, «parametri» le barre o i
/// quadranti, «per vigna e vitigno» la tabella con l'interruttore. Sottovigneti e note metodologiche sono solo nel PowerPoint.
/// Gli id non cambiano più (test in <c>ReportisticaR4Tests</c>).
/// </summary>
public static class CatalogoIndiceBigot
{
    public const string Report = "ib";

    public const string Copertina = "ib.copertina";
    public const string Indice = "ib.indice";
    public const string Vigneti = "ib.vigneti";
    public const string PerVigna = "ib.per-vigna";
    public const string PerVitigno = "ib.per-vitigno";
    public const string Parametri = "ib.parametri";
    public const string Storico = "ib.storico";
    public const string Sottovigneti = "ib.sottovigneti";
    public const string Note = "ib.note";

    public static readonly IReadOnlyList<ElementoReport> Elementi =
    [
        new(Copertina, TipoElementoReport.Sezione, null, "Copertina e commento generale", Nascondibile: false),
        new(Indice, TipoElementoReport.Sezione, null, "Indice per vigneto e numeri dell'anno"),
        new(Vigneti, TipoElementoReport.Sezione, null, "Vigneti nell'anno"),
        new(PerVigna, TipoElementoReport.Sezione, null, "Per vigna"),
        new(PerVitigno, TipoElementoReport.Sezione, null, "Per vitigno"),
        new(Parametri, TipoElementoReport.Sezione, null, "I nove parametri (quadranti)"),
        new(Storico, TipoElementoReport.Sezione, null, "Storici dell'indice e dei parametri"),
        new(Sottovigneti, TipoElementoReport.Sezione, null, "Sottovigneti (solo PowerPoint)"),
        new(Note, TipoElementoReport.Sezione, null, "Note metodologiche (solo PowerPoint)"),
    ];

    public static Task<ScelteApplicate> ScelteAsync(Services.ReportisticaApiClient api, int azienda, int anno,
        Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct = default) =>
        ScelteApplicate.LeggiAsync(api, Report, Elementi, azienda, anno.ToString(System.Globalization.CultureInfo.InvariantCulture), "it",
            logger, ct);
}
