using ProgettoMappe.Web.Report;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>
/// Il catalogo del report fine stagione per la reportistica (task #418, tappa R4): la copertina (non si nasconde, il suo commento è
/// il commento generale) e le 14 sezioni di <see cref="SezioniReport"/>, id <c>rfs.{chiave senza «sezione.»}</c>. Edizione = annata.
/// Le sezioni che in Qlik si compilavano a mano (<see cref="SezioneReport.Tappa"/> = 0: nutrizione, potature, maturazione, ad hoc,
/// degustazione) hanno come contenuto il commento del consulente. Gli id non cambiano più (test in <c>ReportisticaR4Tests</c>).
/// </summary>
public static class CatalogoFineStagione
{
    public const string Report = "rfs";

    public const string Copertina = "rfs.copertina";

    public static string Id(SezioneReport sezione) => Report + "." + sezione.Chiave["sezione.".Length..];

    public static readonly IReadOnlyList<ElementoReport> Elementi =
    [
        new(Copertina, TipoElementoReport.Sezione, null, "Copertina e commento generale", Nascondibile: false),
        .. SezioniReport.Tutte.Select(s => new ElementoReport(Id(s), TipoElementoReport.Sezione, null,
            $"{s.Numero}. {SezioniReport.Titolo(s, LinguaReport.It, DateTime.Today.Year)}")),
    ];

    /// <summary>Le sezioni scritte dal consulente: senza commento non hanno contenuto.</summary>
    public static bool Manuale(SezioneReport sezione) => sezione.Tappa == 0;

    /// <summary>
    /// La sezione di una slide del PowerPoint (<see cref="PowerPointFineStagione.SlideIt"/>/<see cref="PowerPointFineStagione.SlideEn"/>),
    /// come la pagina: il meteo nella caratterizzazione, apici, pre-alba e suolo nella gestione idrica, chioma e fattori nella SFE…
    /// Copertina e sommario stanno con la copertina.
    /// </summary>
    public static string SezioneDellaSlide(string slide) => slide switch
    {
        "copertina" or "sommario" => Copertina,
        "gestione-idrica" or "apici" or "apici-storico" or "prealba" or "prealba-storico" or "suolo" => "rfs.gestione-idrica",
        "scottature-media" or "scottature-max" => "rfs.scottature",
        "vigoria" or "vigoria-2" => "rfs.vigoria",
        "chioma-1" or "chioma-2" or "fattori-sfe" => "rfs.sfe",
        "prod-vitigno" or "prod-vigneto" or "prod-vigneto-anno" or "prod-confronto" or "prod-stime" => "rfs.produzione",
        "fallanze" => "rfs.fallanze",
        "cruscotto" or "foglie" or "grappoli" or "esca" or "voli" or "nidi" or "nidi-tabella" or "fenologia" or "bbch-foglie"
            or "bbch-grappoli" or "bbch-storico-foglie" or "bbch-storico-grappoli" => "rfs.sanita",
        var s when s.StartsWith("ib-", StringComparison.Ordinal) => "rfs.indice-bigot",
        _ => "rfs.caratterizzazione",
    };

    public static Task<ScelteApplicate> ScelteAsync(Services.ReportisticaApiClient api, int azienda, int annata, LinguaReport lingua,
        Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct = default) =>
        ScelteApplicate.LeggiAsync(api, Report, Elementi, azienda, annata.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TestiReport.Codice(lingua), logger, ct);
}
