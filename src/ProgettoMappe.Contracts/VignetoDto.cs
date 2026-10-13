using System.Text.Json;

namespace ProgettoMappe.Contracts;

/// <summary>
/// Varieta = nome del vitigno (4Grapes Vitigno.Vitigno); VignaId/Vigna/VitignoId servono ai filtri.
/// HaGeometria: il vigneto ha un poligono (la geometria vera viaggia solo nel GeoJSON di
/// <see cref="VignetiAziendaDto"/>). GeometriaNonAttendibile: poligono oltre la soglia, HaGeometria false.
/// </summary>
public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    bool HaGeometria,
    int? VignaId = null,
    string? Vigna = null,
    int? VitignoId = null,
    bool GeometriaNonAttendibile = false,
    int? AnnoImpianto = null);

/// <summary>
/// Vigneti di un'azienda in una risposta sola: l'elenco (ordinato per nome) e la FeatureCollection
/// GeoJSON pronta per MapLibre (geometrie mancanti o non valide scartate).
/// </summary>
public record VignetiAziendaDto(IReadOnlyList<VignetoDto> Vigneti, JsonElement GeoJson);

/// <summary>Non ancora esposto da nessun endpoint: predisposizione per dopo il primo MVP (vedi ProgettoMappe.Domain.Entities.LayerDataset).</summary>
public record LayerDatasetDto(
    int Id,
    int AziendaId,
    int? VignetoId,
    string Nome,
    string Tipo,
    string? Descrizione,
    DateTime? DataRilievo,
    string? Campagna,
    string? Origine,
    string? LegendaJson,
    string? StileJson,
    string? MetadatiJson,
    string? DatiGeoJson,
    string? TileUrl);
