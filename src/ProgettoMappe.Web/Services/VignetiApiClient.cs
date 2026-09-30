using System.Net.Http.Json;
using System.Text.Json;

namespace ProgettoMappe.Web.Services;

public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    string? GeometriaGeoJson,
    int? VignaId = null,
    string? Vigna = null,
    int? VitignoId = null);

/// <summary>Riga dell'indice BIGOT di un vigneto in un anno (valori così come in 4Grapes).</summary>
public record IndiceBigotDto(
    int Anno,
    int IdVigneto,
    int? NumParametri,
    int? Punteggio,
    int? Peso,
    string? Vigoria,
    decimal? Sfe,
    decimal? Produzione,
    decimal? SfeKg,
    int? EtaVigneto,
    string? Morfologia,
    decimal? Ampelopatie,
    string? RegimeIdrico,
    decimal? Biodiversita);

/// <summary>Pronto = false: il server sta ancora caricando gli indici (riprovare più tardi).</summary>
public record IndiciBigotAziendaDto(bool Pronto, List<IndiceBigotDto> Indici);

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

    /// <summary>Indici BIGOT (tutti gli anni) dei vigneti dell'azienda.</summary>
    public async Task<IndiciBigotAziendaDto> GetIndiciBigotAsync(int aziendaId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<IndiciBigotAziendaDto>($"api/aziende/{aziendaId}/indici-bigot", ct)
        ?? new IndiciBigotAziendaDto(false, []);
}
