using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>
/// Il catalogo del report settimanale per la reportistica (task #416, pilota della tappa R2): una voce per sezione di
/// <see cref="SezioniSettimanale"/>, id <c>rs.{chiave senza «sezione.»}</c>. La copertina non si nasconde e il suo commento è
/// il commento generale. Gli id non cambiano più (li controlla <c>ReportisticaTests</c>): una sezione rinominata tiene il suo id.
/// </summary>
public static class CatalogoSettimanale
{
    public const string Report = "rs";

    public const string Copertina = "rs.copertina";

    public static string Id(string chiaveSezione) => Report + "." + chiaveSezione["sezione.".Length..];

    public static readonly IReadOnlyList<ElementoReport> Elementi = SezioniSettimanale.Tutte
        .Select(s => new ElementoReport(Id(s.Chiave), TipoElementoReport.Sezione, null,
            s.Chiave == "sezione.copertina" ? TestiSettimanale.Testo(LinguaReport.It, "commento.generale")
                : TestiSettimanale.Testo(LinguaReport.It, s.Chiave, DateTime.Today.Year),
            Nascondibile: s.Chiave != "sezione.copertina"))
        .ToList();

    /// <summary>
    /// Le scelte dell'edizione (azienda × settimana) per la lingua. Se l'Api non le dà (errore, report senza backend) il report
    /// esce intero e resta un Warning: meglio un report completo che nessun report.
    /// </summary>
    public static async Task<ScelteApplicate> ScelteAsync(Services.ReportisticaApiClient api, int azienda, SettimanaReport settimana,
        LinguaReport lingua, Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct = default)
    {
        try
        {
            return ScelteApplicate.Da(Elementi, await api.GetEdizioneAsync(Report, azienda, settimana.Codice, ct), TestiReport.Codice(lingua));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(logger,
                "Report settimanale: scelte dell'azienda {Azienda} per {Settimana} non lette ({Messaggio}), report intero", azienda,
                settimana.Codice, ex.Message);
            return ScelteApplicate.Tutto;
        }
    }
}

/// <summary>
/// I dati congelati di una pubblicazione del settimanale (task #416): quelli da cui pagina e PowerPoint ridisegnano il report
/// (rilievi e campo dello snapshot), più le scelte già risolte e il momento della generazione. Li scrive e li rilegge solo il
/// Web; l'Api li conserva come testo. <see cref="Formato"/> cresce se cambia la forma.
/// </summary>
public sealed record ContenutoSettimanale(int Formato, string Azienda, string Settimana, string Lingua, RilieviSettimanaDto Rilievi,
    CampoReportFineStagioneDto? Campo, IReadOnlyList<string> Nascosti, IReadOnlyDictionary<string, string> Commenti, DateTimeOffset Generato)
{
    public const int FormatoAttuale = 1;

    private static readonly JsonSerializerOptions Opzioni = new(JsonSerializerDefaults.Web);

    public static ContenutoSettimanale Di(DatiSettimanale dati, LinguaReport lingua, ScelteApplicate scelte, DateTimeOffset adesso) =>
        new(FormatoAttuale, dati.Azienda, dati.Settimana.Codice, TestiReport.Codice(lingua), dati.Rilievi, dati.Campo,
            scelte.Nascosti.Order().ToList(), scelte.Commenti, adesso);

    /// <summary>Le scelte congelate (nel JSON come elenco: un insieme non si rilegge).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ScelteApplicate Scelte => new(Nascosti.ToHashSet(), Commenti);

    public string Json() => JsonSerializer.Serialize(this, Opzioni);

    /// <summary>Null se il testo non si legge o è di un formato che questo Web non conosce.</summary>
    public static ContenutoSettimanale? Leggi(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ContenutoSettimanale>(json, Opzioni) is { Formato: FormatoAttuale } c ? c : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public DatiSettimanale? Dati() =>
        SettimanaReport.DaCodice(Settimana) is { } s ? new DatiSettimanale(Azienda, s, Rilievi, Campo) : null;
}
