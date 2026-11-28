using System.Net.Http.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Services;

/// <summary>
/// Il backend dei report dall'Api (task #416, tappa R2): edizione, scelte, commenti, stati e pubblicazioni. Le scritture
/// portano l'utente entrato (<c>X-Mappe-Utente</c>) e l'autore, mai dal browser.
/// </summary>
public class ReportisticaApiClient(HttpClient http)
{
    private static string Base(string report, int azienda, string edizione) =>
        $"api/reportistica/{Uri.EscapeDataString(report)}/aziende/{azienda}/edizioni/{Uri.EscapeDataString(edizione)}";

    /// <summary>L'edizione; null se l'azienda è fuori perimetro o il report non ha ancora il backend.</summary>
    public async Task<EdizioneReportDto?> GetEdizioneAsync(string report, int azienda, string edizione, CancellationToken ct = default)
    {
        using var risposta = await http.GetAsync(Base(report, azienda, edizione), ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<EdizioneReportDto>(ct);
    }

    public Task<EsitoFilari<EdizioneReportDto>> SalvaSceltaAsync(string report, int azienda, string edizione, SalvaSceltaReportDto richiesta,
        int? idUtente, CancellationToken ct = default) =>
        InviaAsync<EdizioneReportDto, SalvaSceltaReportDto>(HttpMethod.Put, Base(report, azienda, edizione) + "/scelte", richiesta, idUtente, ct);

    public Task<EsitoFilari<EdizioneReportDto>> SalvaCommentoAsync(string report, int azienda, string edizione, SalvaCommentoReportDto richiesta,
        int? idUtente, CancellationToken ct = default) =>
        InviaAsync<EdizioneReportDto, SalvaCommentoReportDto>(HttpMethod.Put, Base(report, azienda, edizione) + "/commenti", richiesta, idUtente, ct);

    public Task<EsitoFilari<EdizioneReportDto>> CambiaStatoAsync(string report, int azienda, string edizione, CambiaStatoReportDto richiesta,
        int? idUtente, CancellationToken ct = default) =>
        InviaAsync<EdizioneReportDto, CambiaStatoReportDto>(HttpMethod.Put, Base(report, azienda, edizione) + "/stato", richiesta, idUtente, ct);

    public Task<EsitoFilari<PubblicazioneReportDto>> PubblicaAsync(string report, int azienda, string edizione, PubblicaReportDto richiesta,
        int? idUtente, CancellationToken ct = default) =>
        InviaAsync<PubblicazioneReportDto, PubblicaReportDto>(HttpMethod.Post, Base(report, azienda, edizione) + "/pubblicazioni", richiesta, idUtente, ct);

    public Task<EsitoFilari<EdizioneReportDto>> RitiraAsync(string report, int azienda, string edizione, string lingua, int versione,
        RitiraReportDto richiesta, int? idUtente, CancellationToken ct = default) =>
        InviaAsync<EdizioneReportDto, RitiraReportDto>(HttpMethod.Post,
            $"{Base(report, azienda, edizione)}/pubblicazioni/{lingua}/{versione}/ritira", richiesta, idUtente, ct);

    /// <summary>L'ultima pubblicazione in vista nella lingua (o la versione chiesta); null se non c'è.</summary>
    public async Task<ContenutoPubblicatoDto?> GetPubblicataAsync(string report, int azienda, string edizione, string lingua, int? versione = null,
        CancellationToken ct = default)
    {
        var indirizzo = $"{Base(report, azienda, edizione)}/pubblicazioni/{lingua}" + (versione is { } v ? $"?versione={v}" : "");
        using var risposta = await http.GetAsync(indirizzo, ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<ContenutoPubblicatoDto>(ct);
    }

    /// <summary>Il PowerPoint congelato di una versione; null se non c'è.</summary>
    public async Task<byte[]?> GetPptxAsync(string report, int azienda, string edizione, string lingua, int versione, CancellationToken ct = default)
    {
        using var risposta = await http.GetAsync($"{Base(report, azienda, edizione)}/pubblicazioni/{lingua}/{versione}/pptx", ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadAsByteArrayAsync(ct);
    }

    /// <summary>Gli obiettivi Bigot dell'annata (task #419): predefiniti e, con l'azienda, i suoi; null se l'azienda è fuori perimetro.</summary>
    public async Task<ObiettiviBigotDto?> GetObiettiviBigotAsync(int anno, int? azienda, CancellationToken ct = default)
    {
        using var risposta = await http.GetAsync($"api/indice-bigot/obiettivi?anno={anno}" + (azienda is { } a ? $"&azienda={a}" : ""), ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<ObiettiviBigotDto>(ct);
    }

    public async Task<IReadOnlyList<ObiettivoBigotDto>> GetStoricoObiettivoAsync(int anno, string parametro, int? azienda, int? vigneto,
        CancellationToken ct = default)
    {
        var indirizzo = $"api/indice-bigot/obiettivi/storico?anno={anno}&parametro={Uri.EscapeDataString(parametro)}"
                        + (azienda is { } a ? $"&azienda={a}" : "") + (vigneto is { } v ? $"&vigneto={v}" : "");
        return await http.GetFromJsonAsync<List<ObiettivoBigotDto>>(indirizzo, ct) ?? [];
    }

    public Task<EsitoFilari<ObiettiviBigotDto>> SalvaObiettivoBigotAsync(SalvaObiettivoBigotDto richiesta, int? idUtente, CancellationToken ct = default) =>
        InviaAsync<ObiettiviBigotDto, SalvaObiettivoBigotDto>(HttpMethod.Put, "api/indice-bigot/obiettivi", richiesta, idUtente, ct);

    public Task<EsitoFilari<ObiettiviBigotDto>> CopiaObiettiviBigotAsync(CopiaObiettiviBigotDto richiesta, int? idUtente, CancellationToken ct = default) =>
        InviaAsync<ObiettiviBigotDto, CopiaObiettiviBigotDto>(HttpMethod.Post, "api/indice-bigot/obiettivi/copia", richiesta, idUtente, ct);

    private async Task<EsitoFilari<T>> InviaAsync<T, TCorpo>(HttpMethod metodo, string indirizzo, TCorpo corpo, int? idUtente, CancellationToken ct)
        where T : class
    {
        using var messaggio = Esperienze.RichiestaConUtente.Crea(metodo, indirizzo, corpo, idUtente);
        using var risposta = await http.SendAsync(messaggio, ct);
        if (risposta.IsSuccessStatusCode)
            return new EsitoFilari<T>(await risposta.Content.ReadFromJsonAsync<T>(ct), null);
        var testo = (await risposta.Content.ReadAsStringAsync(ct)).Trim().Trim('"');
        return new EsitoFilari<T>(null, testo.Length is > 0 and < 300 && !testo.StartsWith('{') ? testo : $"Richiesta non riuscita ({(int)risposta.StatusCode}).");
    }
}
