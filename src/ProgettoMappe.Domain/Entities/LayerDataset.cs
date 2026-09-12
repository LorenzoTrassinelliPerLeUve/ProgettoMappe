namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Dataset di un livello informativo agronomico (NDVI, vigore, produzione, meteo, ...).
/// Il tipo è una stringa estendibile (vedi <see cref="LayerTipi"/>), non un elenco chiuso:
/// un nuovo tipo di layer non richiede modifiche al modello o all'applicazione. Legenda,
/// stile e metadati restano stringhe JSON per non legare il modello a un dataset specifico.
/// Non ancora utilizzato dalle API: predisposizione per dopo il primo MVP (Azienda → Vigneto
/// → geometrie → mappa).
/// </summary>
public class LayerDataset
{
    public int Id { get; set; }

    public int AziendaId { get; set; }

    public Azienda? Azienda { get; set; }

    /// <summary>Null se il dataset è a livello di azienda e non di singolo vigneto.</summary>
    public int? VignetoId { get; set; }

    public Vigneto? Vigneto { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Codice del tipo di dataset (es. "ndvi", "vigore"...). Vedi <see cref="LayerTipi"/> per valori noti, non esaustivi.</summary>
    public string Tipo { get; set; } = string.Empty;

    public string? Descrizione { get; set; }

    public DateTime? DataRilievo { get; set; }

    /// <summary>Campagna/annata di riferimento (es. "2025").</summary>
    public string? Campagna { get; set; }

    /// <summary>Origine del dato (es. "satellite Sentinel-2", "rilievo drone", "sensore in campo").</summary>
    public string? Origine { get; set; }

    /// <summary>Definizione della legenda (classi/colori/soglie) in formato JSON, specifica per tipo di dataset.</summary>
    public string? LegendaJson { get; set; }

    /// <summary>Stile di visualizzazione MapLibre (paint/layout) in formato JSON.</summary>
    public string? StileJson { get; set; }

    /// <summary>Metadati aggiuntivi in formato JSON, liberi per tipo di dataset.</summary>
    public string? MetadatiJson { get; set; }

    /// <summary>Geometrie del dataset in GeoJSON (Point/MultiPoint/LineString/Polygon/MultiPolygon), quando applicabile.</summary>
    public string? DatiGeoJson { get; set; }

    /// <summary>URL di tile raster o vettoriali, per dataset non rappresentabili come GeoJSON (es. raster NDVI, vector tiles).</summary>
    public string? TileUrl { get; set; }
}
