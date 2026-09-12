using ProgettoMappe.Domain.Enums;

namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Livello informativo associato a un vigneto (es. NDVI, mappa di vigore): mese 3 (gestione layer)
/// e mese 4 (precision farming) del piano.
/// </summary>
public class LayerMappa
{
    public int Id { get; set; }

    public int VignetoId { get; set; }

    public Vigneto? Vigneto { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoLayer Tipo { get; set; }

    public string? Descrizione { get; set; }

    public DateTime? DataRilievo { get; set; }

    /// <summary>Dati del layer in formato GeoJSON (poligoni/celle con relativi valori), pronti per MapLibre.</summary>
    public string? DatiGeoJson { get; set; }
}
