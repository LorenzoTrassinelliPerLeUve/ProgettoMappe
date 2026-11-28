using System.Globalization;
using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.ReportAnalisiSuolo;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.Services;

namespace ProgettoMappe.Web.Report;

/// <summary>
/// I dati congelati di una pubblicazione di fine stagione, Indice Bigot o analisi del suolo (task #418), come
/// <see cref="ReportSettimanale.ContenutoSettimanale"/> per il settimanale: i dati da cui pagina e PowerPoint ridisegnano il
/// report, le scelte già risolte e il momento della generazione. Li scrive e li rilegge solo il Web; l'Api li conserva come
/// testo. <see cref="Formato"/> cresce se cambia la forma; <see cref="Report"/> impedisce di leggere i dati di un altro report.
/// </summary>
public sealed record ContenutoReport<T>(int Formato, string Report, string Azienda, string Edizione, string Lingua, T Dati,
    IReadOnlyList<string> Nascosti, IReadOnlyDictionary<string, string> Commenti, DateTimeOffset Generato)
{
    public const int FormatoAttuale = 1;

    private static readonly JsonSerializerOptions Opzioni = new(JsonSerializerDefaults.Web);

    public static ContenutoReport<T> Di(string report, string azienda, string edizione, string lingua, T dati, ScelteApplicate scelte,
        DateTimeOffset adesso) =>
        new(FormatoAttuale, report, azienda, edizione, lingua, dati, scelte.Nascosti.Order().ToList(), scelte.Commenti, adesso);

    /// <summary>Le scelte congelate (nel JSON come elenco: un insieme non si rilegge).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ScelteApplicate Scelte => new(Nascosti.ToHashSet(), Commenti);

    public string Json() => JsonSerializer.Serialize(this, Opzioni);

    /// <summary>Null se il testo non si legge, è di un formato che questo Web non conosce o è di un altro report.</summary>
    public static ContenutoReport<T>? Leggi(string json, string report)
    {
        try
        {
            return JsonSerializer.Deserialize<ContenutoReport<T>>(json, Opzioni) is { Formato: FormatoAttuale, Dati: not null } c && c.Report == report
                ? c : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>I dati del fine stagione che la pagina e il PowerPoint leggono dall'Api, congelati insieme.</summary>
public sealed record DatiFineStagioneCongelati(MeteoReportFineStagioneDto Meteo, CampoReportFineStagioneDto? Campo,
    IReadOnlyList<ControlloReportDto> Controlli, IndiceBigotReportDto? Indice);

/// <summary>La richiesta di pubblicazione pronta da mandare all'Api, o il motivo per cui non si pubblica.</summary>
public sealed record PreparazionePubblicazione(PubblicaReportDto? Richiesta, string? Errore);

/// <summary>
/// Congela fine stagione, Indice Bigot e analisi del suolo (task #418): rilegge i dati come la pagina, genera il PowerPoint con
/// le scelte e i commenti di adesso e prepara la richiesta per l'Api. Il report intero (senza i filtri della pagina): chi guarda
/// la versione pubblicata può filtrare sui dati congelati.
/// </summary>
public static class PubblicazioniReport
{
    public static async Task<PreparazionePubblicazione> PreparaAsync(VignetiApiClient api, string report, int azienda, string nomeAzienda,
        string edizione, LinguaReport lingua, ScelteApplicate scelte, string? nota, string? autore, DateTimeOffset adesso,
        CancellationToken ct = default)
    {
        var codiceLingua = TestiReport.Codice(lingua);
        var anno = int.TryParse(edizione, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ? a : (int?)null;
        switch (report)
        {
            case CatalogoFineStagione.Report when anno is { } annata:
            {
                if (await api.GetMeteoReportFineStagioneAsync(azienda, ct) is not { } meteo)
                    return new(null, "Il meteo di questa azienda non è disponibile ora: non pubblico.");
                var controlli = await api.GetControlliReportFineStagioneAsync(azienda, ct);
                var campo = await api.GetCampoReportFineStagioneAsync(azienda, ct);
                var indice = await api.GetIndiceBigotReportAsync(azienda, ct);
                var pptx = PowerPointFineStagione.Crea(new DatiFineStagione(nomeAzienda, annata, meteo, campo, indice), lingua, adesso, scelte);
                var contenuto = ContenutoReport<DatiFineStagioneCongelati>.Di(report, nomeAzienda, edizione, codiceLingua,
                    new(meteo, campo, controlli, indice), scelte, adesso);
                return new(new PubblicaReportDto(codiceLingua, meteo.Snapshot, nota, PowerPointFineStagione.NomeFile(nomeAzienda, annata, lingua, adesso),
                    contenuto.Json(), Convert.ToBase64String(pptx), autore), null);
            }
            case ReportMetaStagione.CatalogoMetaStagione.Report when anno is { } annata:
            {
                // Il taglio predefinito (l'invaiatura): la versione pubblicata lo porta con sé nei dati congelati.
                var oggi = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(adesso, Tematismi.TematismiBigot.FusoItalia).DateTime);
                if (await ReportMetaStagione.DatiMetaStagione.LeggiAsync(api, azienda, annata, null, oggi, ct) is not { } dati)
                    return new(null, "Il meteo di questa azienda non è disponibile ora: non pubblico.");
                var pptx = ReportMetaStagione.PowerPointMetaStagione.Crea(nomeAzienda, dati, lingua, adesso, scelte);
                var contenuto = ContenutoReport<ReportMetaStagione.DatiMetaStagione>.Di(report, nomeAzienda, edizione, codiceLingua, dati,
                    scelte, adesso);
                return new(new PubblicaReportDto(codiceLingua, dati.Meteo.Snapshot, nota,
                    ReportMetaStagione.PowerPointMetaStagione.NomeFile(nomeAzienda, annata, lingua, adesso), contenuto.Json(),
                    Convert.ToBase64String(pptx), autore), null);
            }
            case CatalogoIndiceBigot.Report when anno is not null:
            {
                if (await api.GetIndiceBigotReportAsync(azienda, ct) is not { Pronto: true } indice)
                    return new(null, "I dati dell'indice non sono pronti ora: non pubblico.");
                var pptx = PowerPointIndiceBigot.Crea(indice, new FiltroIndice(), anno, adesso, scelte);
                var contenuto = ContenutoReport<IndiceBigotReportDto>.Di(report, nomeAzienda, edizione, codiceLingua, indice, scelte, adesso);
                return new(new PubblicaReportDto(codiceLingua, indice.Snapshot, nota, PowerPointIndiceBigot.NomeFile(indice, anno, adesso),
                    contenuto.Json(), Convert.ToBase64String(pptx), autore), null);
            }
            case CatalogoAnalisiSuolo.Report when anno is not null || edizione == "tutti":
            {
                if (await api.GetAnalisiSuoloAsync(azienda, ct) is not { } dati)
                    return new(null, "Le analisi di questa azienda non sono disponibili ora: non pubblico.");
                var selezione = new SelezioneSuolo(anno);
                var righe = CalcoloAnalisiSuolo.Filtra(CalcoloAnalisiSuolo.Righe(dati), selezione);
                var pptx = PowerPointAnalisiSuolo.Crea(dati.RagioneSociale, righe, selezione, adesso, scelte);
                var contenuto = ContenutoReport<AnalisiSuoloAziendaDto>.Di(report, dati.RagioneSociale, edizione, codiceLingua, dati, scelte, adesso);
                return new(new PubblicaReportDto(codiceLingua, adesso, nota, PowerPointAnalisiSuolo.NomeFile(dati.RagioneSociale),
                    contenuto.Json(), Convert.ToBase64String(pptx), autore), null);
            }
            default:
                return new(null, "Questa edizione non si può pubblicare.");
        }
    }

    /// <summary>L'indirizzo del PowerPoint congelato di una versione (rotta <c>/report/pubblicato/powerpoint</c>).</summary>
    public static string IndirizzoPowerPoint(string report, int azienda, string edizione, string lingua, int versione) =>
        $"report/pubblicato/powerpoint?report={report}&azienda={azienda}&edizione={Uri.EscapeDataString(edizione)}&lingua={lingua}&versione={versione}";
}
