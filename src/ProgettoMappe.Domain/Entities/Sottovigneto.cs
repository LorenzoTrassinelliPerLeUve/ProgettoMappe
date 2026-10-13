namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Suddivisione di un vigneto con un poligono proprio (4Grapes <c>V_SubVigneto</c>, task #66).
/// Solo per la mappa e la scheda: i tematismi restano per vigneto.
/// </summary>
public class Sottovigneto
{
    public int Id { get; set; }

    public int IdVigneto { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Superficie in ettari (4Grapes la registra in m²); null se non attendibile.</summary>
    public decimal? SuperficieEttari { get; set; }

    /// <summary>Poligono in GeoJSON (WGS84); null se mancante, illeggibile o oltre la soglia.</summary>
    public string? GeometriaGeoJson { get; set; }
}
