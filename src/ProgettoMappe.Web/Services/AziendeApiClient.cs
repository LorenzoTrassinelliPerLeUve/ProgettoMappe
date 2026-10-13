using System.Net.Http.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Services;

public class AziendeApiClient
{
    private readonly HttpClient _http;

    public AziendeApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<AziendaDto>> GetAziendeAsync(CancellationToken ct = default)
    {
        var aziende = await _http.GetFromJsonAsync<List<AziendaDto>>("api/aziende", ct);
        return aziende ?? [];
    }

    public async Task<List<GruppoDto>> GetGruppiAsync(CancellationToken ct = default)
    {
        var gruppi = await _http.GetFromJsonAsync<List<GruppoDto>>("api/gruppi", ct);
        return gruppi ?? [];
    }
}
