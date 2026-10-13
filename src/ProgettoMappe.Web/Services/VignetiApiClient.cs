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

    /// <summary>Fenologia (ultima fase BBCH per anno, tutti gli anni) dei vigneti dell'azienda.</summary>
    public async Task<FenologiaAziendaDto> GetFenologiaAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FenologiaAziendaDto>($"api/aziende/{aziendaId}/fenologia", ct)
        ?? FenologiaAziendaDto.NonPronta;

    /// <summary>File dei vigneti preparato dall'Api (vedi EsportazioneVigneti.IndirizzoApi); null se l'azienda non c'è.</summary>
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
