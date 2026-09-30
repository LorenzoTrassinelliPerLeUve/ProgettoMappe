namespace ProgettoMappe.Api.Contracts;

public record AziendaDto(int Id, string Nome, string? Comune, string? Provincia);

/// <summary>Gruppo di aziende; Concluso = DataFine passata (il gruppo resta selezionabile).</summary>
public record GruppoDto(int Id, string Nome, bool Concluso, IReadOnlyList<int> AziendeIds);

/// <summary>Varieta = nome del vitigno (4Grapes Vitigno.Vitigno); VignaId/Vigna/VitignoId servono ai filtri.</summary>
public record VignetoDto(
    int Id,
    int AziendaId,
    string Nome,
    string? Varieta,
    decimal? SuperficieEttari,
    string? GeometriaGeoJson,
    int? VignaId,
    string? Vigna,
    int? VitignoId)
{
    public static VignetoDto Da(ProgettoMappe.Domain.Entities.Vigneto v) =>
        new(v.Id, v.AziendaId, v.Nome, v.Varieta, v.SuperficieEttari, v.GeometriaGeoJson, v.VignaId, v.NomeVigna, v.VitignoId);
}

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
