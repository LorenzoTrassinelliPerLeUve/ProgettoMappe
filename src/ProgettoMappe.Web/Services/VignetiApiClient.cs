using System.Net.Http.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Services;

public class VignetiApiClient
{
    private readonly HttpClient _http;

    public VignetiApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Elenco dei vigneti dell'azienda e FeatureCollection GeoJSON per MapLibre, in una chiamata sola.</summary>
    public async Task<VignetiAziendaDto> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<VignetiAziendaDto>($"api/aziende/{aziendaId}/vigneti", ct)
        ?? throw new InvalidOperationException($"Risposta vuota per i vigneti dell'azienda {aziendaId}");

    /// <summary>Indici BIGOT (tutti gli anni) dei vigneti dell'azienda.</summary>
    public async Task<IndiciBigotAziendaDto> GetIndiciBigotAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IndiciBigotAziendaDto>($"api/aziende/{aziendaId}/indici-bigot", ct)
        ?? new IndiciBigotAziendaDto(false, []);

    /// <summary>Rischi, vigoria (tutti gli anni) e potenziale idrico dei vigneti dell'azienda.</summary>
    public async Task<StatisticheAziendaDto> GetStatisticheAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<StatisticheAziendaDto>($"api/aziende/{aziendaId}/statistiche", ct)
        ?? StatisticheAziendaDto.NonPronte;

    /// <summary>Ultimo rilievo di ogni vigneto dell'azienda (anche quelli mai rilevati).</summary>
    public async Task<UltimiRilieviAziendaDto> GetUltimiRilieviAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<UltimiRilieviAziendaDto>($"api/aziende/{aziendaId}/rilievi", ct)
        ?? UltimiRilieviAziendaDto.NonPronti;

    /// <summary>Ultimo trattamento e linea di difesa di ogni vigneto dell'azienda (anche quelli senza).</summary>
    public async Task<DifesaAziendaDto> GetDifesaAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DifesaAziendaDto>($"api/aziende/{aziendaId}/difesa", ct)
        ?? DifesaAziendaDto.NonPronta;

    /// <summary>NDVI dei vigneti dell'azienda: ultimo passaggio pulito e serie dell'anno e del precedente.</summary>
    public async Task<NdviAziendaDto> GetNdviAsync(int aziendaId, int anno, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<NdviAziendaDto>($"api/aziende/{aziendaId}/ndvi?anno={anno}", ct)
        ?? NdviAziendaDto.NonPronto;

    /// <summary>Passaggi di Sentinel-2 elaborati per l'azienda, dal più recente.</summary>
    public async Task<PassaggiNdviDto> GetPassaggiNdviAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<PassaggiNdviDto>($"api/aziende/{aziendaId}/ndvi/passaggi", ct)
        ?? PassaggiNdviDto.Nessuno;

    /// <summary>Stazioni meteo, dispositivi e trappole dell'azienda, con posizione e colore.</summary>
    public async Task<DispositiviAziendaDto> GetDispositiviAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DispositiviAziendaDto>($"api/aziende/{aziendaId}/dispositivi", ct)
        ?? DispositiviAziendaDto.NonPronti;

    /// <summary>Sottovigneti dei vigneti dell'azienda: elenco per la scheda e GeoJSON per la mappa.</summary>
    public async Task<SottovignetiAziendaDto> GetSottovignetiAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<SottovignetiAziendaDto>($"api/aziende/{aziendaId}/sottovigneti", ct)
        ?? SottovignetiAziendaDto.NonPronti;

    /// <summary>Meteo dell'azienda: pioggia dei 7 giorni, temperature di ieri, probabilità di infezione di oggi.</summary>
    public async Task<MeteoAziendaDto?> GetMeteoAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<MeteoAziendaDto>($"api/aziende/{aziendaId}/meteo", ct);

    /// <summary>Fenologia (ultima fase BBCH per anno, tutti gli anni) dei vigneti dell'azienda.</summary>
    public async Task<FenologiaAziendaDto> GetFenologiaAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FenologiaAziendaDto>($"api/aziende/{aziendaId}/fenologia", ct)
        ?? FenologiaAziendaDto.NonPronta;

    /// <summary>Filari validi dei vigneti dell'azienda (task #95): riassunti per la scheda e GeoJSON per la mappa.</summary>
    public async Task<FilariAziendaDto> GetFilariAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FilariAziendaDto>($"api/aziende/{aziendaId}/filari", ct)
        ?? FilariAziendaDto.NonDisponibili;

    /// <summary>Tutte le versioni dei filari del vigneto, dalla più recente.</summary>
    public async Task<StoricoFilariDto> GetStoricoFilariAsync(int aziendaId, int vignetoId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<StoricoFilariDto>($"api/aziende/{aziendaId}/vigneti/{vignetoId}/filari", ct)
        ?? new StoricoFilariDto([]);

    /// <summary>Una versione dei filari con le linee; null se non c'è.</summary>
    public async Task<VersioneFilariDto?> GetVersioneFilariAsync(int aziendaId, int vignetoId, int versione, CancellationToken ct = default)
    {
        using var risposta = await _http.GetAsync($"api/aziende/{aziendaId}/vigneti/{vignetoId}/filari/{versione}", ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        risposta.EnsureSuccessStatusCode();
        return await risposta.Content.ReadFromJsonAsync<VersioneFilariDto>(ct);
    }

    /// <summary>Filari generati dal primo filare (non salvati); se l'Api non può (400) il motivo è in <see cref="EsitoFilari{T}.Errore"/>.</summary>
    public async Task<EsitoFilari<FilariGeneratiDto>> GeneraFilariAsync(int aziendaId, int vignetoId, GeneraFilariDto richiesta, CancellationToken ct = default)
    {
        using var risposta = await _http.PostAsJsonAsync($"api/aziende/{aziendaId}/vigneti/{vignetoId}/filari/genera", richiesta, ct);
        return await EsitoAsync<FilariGeneratiDto>(risposta, ct);
    }

    /// <summary>Salva una nuova versione dei filari; l'autore lo mette il chiamante (utente entrato).</summary>
    public async Task<EsitoFilari<FilariVignetoDto>> SalvaFilariAsync(int aziendaId, int vignetoId, SalvaFilariDto richiesta, CancellationToken ct = default)
    {
        using var risposta = await _http.PutAsJsonAsync($"api/aziende/{aziendaId}/vigneti/{vignetoId}/filari", richiesta, ct);
        return await EsitoAsync<FilariVignetoDto>(risposta, ct);
    }

    /// <summary>200 → il valore; 400/403/503 → il testo dell'Api come errore; altro → eccezione.</summary>
    private static async Task<EsitoFilari<T>> EsitoAsync<T>(HttpResponseMessage risposta, CancellationToken ct) where T : class
    {
        if (risposta.IsSuccessStatusCode)
        {
            return new EsitoFilari<T>(await risposta.Content.ReadFromJsonAsync<T>(ct), null);
        }

        if (risposta.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Forbidden
            or System.Net.HttpStatusCode.ServiceUnavailable)
        {
            var testo = (await risposta.Content.ReadAsStringAsync(ct)).Trim().Trim('"');
            // Gli errori di validazione di ASP.NET arrivano come JSON: al posto loro un messaggio semplice.
            return new EsitoFilari<T>(null, testo.Length is > 0 and < 300 && !testo.StartsWith('{') ? testo : "Richiesta non valida.");
        }

        risposta.EnsureSuccessStatusCode();
        return new EsitoFilari<T>(null, "Risposta inattesa.");
    }

    /// <summary>File dei vigneti dall'Api; con un confronto la richiesta è un POST con le variazioni.</summary>
    public async Task<FileEsportato?> EsportaAsync(string indirizzoApi, ConfrontoEsportatoDto? confronto = null, CancellationToken ct = default)
    {
        using var risposta = confronto is null
            ? await _http.GetAsync(indirizzoApi, ct)
            : await _http.PostAsJsonAsync(indirizzoApi, confronto, ct);
        if (risposta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        risposta.EnsureSuccessStatusCode();

        var intestazione = risposta.Content.Headers.ContentDisposition;
        var nome = intestazione?.FileNameStar ?? intestazione?.FileName?.Trim('"') ?? "vigneti";
        return new FileEsportato(
            await risposta.Content.ReadAsByteArrayAsync(ct),
            risposta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            nome);
    }
}
