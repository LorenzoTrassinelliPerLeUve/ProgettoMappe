namespace ProgettoMappe.Api.Contracts;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    string? GeometriaGeoJson);

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
