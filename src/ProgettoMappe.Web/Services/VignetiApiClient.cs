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

    /// <summary>File dei vigneti preparato dall'Api (vedi EsportazioneVigneti.IndirizzoApi); null se l'azienda non c'è.</summary>
    public async Task<FileEsportato?> EsportaAsync(string indirizzoApi, CancellationToken ct = default)
    {
        using var risposta = await _http.GetAsync(indirizzoApi, ct);
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
