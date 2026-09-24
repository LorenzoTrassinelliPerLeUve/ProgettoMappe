using System.Net.Http.Json;
using System.Text.Json;

namespace ProgettoMappe.Web.Services;

public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    string? GeometriaGeoJson);

public class VignetiApiClient
{
    private readonly HttpClient _http;

    public VignetiApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VignetoDto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
    {
        var vigneti = await _http.GetFromJsonAsync<List<VignetoDto>>($"api/aziende/{aziendaId}/vigneti", ct);
        return vigneti ?? [];
    }

    /// <summary>FeatureCollection GeoJSON dei vigneti dell'azienda, pronta per MapLibre.</summary>
    public Task<JsonElement> GetVignetiGeoJsonAsync(int aziendaId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<JsonElement>($"api/aziende/{aziendaId}/vigneti/geojson", ct);
}
