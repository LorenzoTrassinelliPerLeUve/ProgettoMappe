using System.Net.Http.Json;

namespace ProgettoMappe.Web.Services;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

/// <summary>Gruppo di aziende; un'azienda può stare in più gruppi. Concluso = DataFine passata.</summary>
public record GruppoDto(int Id, string Nome, bool Concluso, List<int> AziendeIds);

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
