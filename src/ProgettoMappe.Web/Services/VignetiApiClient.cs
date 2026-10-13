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
}
