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

    /// <summary>Aziende con numeri e punteggio per la pagina iniziale: una chiamata sola.</summary>
    public async Task<RiepilogoAziendeDto> GetRiepilogoAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<RiepilogoAziendeDto>("api/aziende/riepilogo", ct)
        ?? new RiepilogoAziendeDto(false, null, []);

    public async Task<List<GruppoDto>> GetGruppiAsync(CancellationToken ct = default)
    {
        var gruppi = await _http.GetFromJsonAsync<List<GruppoDto>>("api/gruppi", ct);
        return gruppi ?? [];
    }
}
