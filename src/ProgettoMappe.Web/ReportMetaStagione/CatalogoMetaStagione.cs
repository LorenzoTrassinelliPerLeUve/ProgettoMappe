using System.Globalization;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportMetaStagione;

/// <summary>
/// Una sezione del report metà stagione: numero, id stabile, chiave del titolo in <see cref="TestiReport"/> e la sezione del fine
/// stagione che ne disegna il contenuto (null = la disegna la pagina: Indice Bigot provvisorio, «Da qui alla vendemmia»).
/// </summary>
public sealed record SezioneMeta(string Numero, string Id, string ChiaveTitolo, string? SezioneFineStagione);

/// <summary>
/// Le sezioni del report metà stagione (task #420) e il loro catalogo per la reportistica, id <c>rms.*</c> (non cambiarli: test in
/// <c>ReportMetaStagioneTests</c>). Edizione = annata. Le sezioni riusano quelle del fine stagione (stessi componenti, stesse slide):
/// <see cref="ComeFineStagione"/> traduce scelte e commenti negli id <c>rfs.*</c> che i componenti leggono, e nasconde le sezioni del
/// fine stagione che a metà stagione non ci sono (scottature, nutrizione, SFE, fallanze, potature, maturazione, ad hoc, degustazione).
/// </summary>
public static class CatalogoMetaStagione
{
    public const string Report = "rms";

    public const string Copertina = "rms.copertina";

    public const string IndiceBigot = "rms.indice-bigot";

    public const string DaFare = "rms.da-fare";

    public static readonly IReadOnlyList<SezioneMeta> Sezioni =
    [
        new("I", "rms.caratterizzazione", "sezione.caratterizzazione", "rfs.caratterizzazione"),
        new("II", "rms.sanita", "sezione.sanita", "rfs.sanita"),
        new("III", "rms.gestione-idrica", "sezione.gestione-idrica", "rfs.gestione-idrica"),
        new("IV", "rms.vigoria", "sezione.vigoria", "rfs.vigoria"),
        new("V", "rms.produzione", "sezione.produzione", "rfs.produzione"),
        new("VI", IndiceBigot, "meta.sezione.indice-bigot", null),
        new("VII", DaFare, "meta.sezione.da-fare", null),
    ];

    public static string Titolo(SezioneMeta s, LinguaReport lingua, int annata) => TestiReport.Testo(lingua, s.ChiaveTitolo, annata);

    public static readonly IReadOnlyList<ElementoReport> Elementi =
    [
        new(Copertina, TipoElementoReport.Sezione, null, "Copertina e commento generale", Nascondibile: false),
        .. Sezioni.Select(s => new ElementoReport(s.Id, TipoElementoReport.Sezione, null,
            $"{s.Numero}. {Titolo(s, LinguaReport.It, DateTime.Today.Year)}")),
    ];

    /// <summary>Il numero di ogni sezione del fine stagione usata, per le intestazioni dei componenti.</summary>
    public static IReadOnlyDictionary<string, string> Numeri { get; } =
        Sezioni.Where(s => s.SezioneFineStagione is not null).ToDictionary(s => s.SezioneFineStagione!, s => s.Numero);

    /// <summary>
    /// Le scelte del metà stagione negli id del fine stagione: visibili solo le sezioni del metà stagione non nascoste, commenti
    /// spostati sull'id <c>rfs.*</c> corrispondente (quelli della copertina, dell'Indice Bigot e del «da fare» restano <c>rms.*</c>).
    /// </summary>
    public static ScelteApplicate ComeFineStagione(ScelteApplicate scelte)
    {
        var visibili = Sezioni.Where(s => s.SezioneFineStagione is not null && scelte.Visibile(s.Id))
            .Select(s => s.SezioneFineStagione!).ToHashSet();
        var nascosti = CatalogoFineStagione.Elementi.Where(e => e.Nascondibile && !visibili.Contains(e.Id)).Select(e => e.Id).ToHashSet();
        var commenti = new Dictionary<string, string>();
        foreach (var s in Sezioni)
        {
            if (scelte.Commento(s.Id) is { } c)
                commenti[s.SezioneFineStagione ?? s.Id] = c;
        }
        if (scelte.Commento(Copertina) is { } generale)
            commenti[CatalogoFineStagione.Copertina] = generale;
        return new ScelteApplicate(nascosti, commenti);
    }

    public static Task<ScelteApplicate> ScelteAsync(Services.ReportisticaApiClient api, int azienda, int annata, LinguaReport lingua,
        Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct = default) =>
        ScelteApplicate.LeggiAsync(api, Report, Elementi, azienda, annata.ToString(CultureInfo.InvariantCulture),
            TestiReport.Codice(lingua), logger, ct);
}
