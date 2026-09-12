using System.Net.Http.Json;

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
}
