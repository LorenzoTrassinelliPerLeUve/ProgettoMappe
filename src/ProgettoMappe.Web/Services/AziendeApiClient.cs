using System.Net.Http.Json;

namespace ProgettoMappe.Web.Services;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

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
}
