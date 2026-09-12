namespace ProgettoMappe.Api.Contracts;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    string? GeometriaGeoJson);

public record LayerMappaDto(
    int Id,
    int VignetoId,
    string Nome,
    string Tipo,
    string? Descrizione,
    DateTime? DataRilievo,
    string? DatiGeoJson);
