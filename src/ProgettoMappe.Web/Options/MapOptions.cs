namespace ProgettoMappe.Web.Options;

/// <summary>
/// Configurazione del motore cartografico: provider ed endpoint arrivano da qui (config/
/// environment/user-secrets), mai hardcodati nel codice o nel JS. <see cref="StyleUrl"/>
/// copre basemap e, quando lo stile lo prevede, immagini satellitari/aeree; il terreno 3D è
/// opzionale e si attiva solo se <see cref="TerrainSourceUrl"/> è configurato.
/// </summary>
public class MapOptions
{
    public const string SectionName = "Map";

    /// <summary>URL di uno stile MapLibre. Di default uno stile demo, adatto solo allo sviluppo locale.</summary>
    public string StyleUrl { get; set; } = "https://demotiles.maplibre.org/style.json";

    /// <summary>Centro iniziale della mappa [longitudine, latitudine], prima che si selezioni un'azienda/vigneto.</summary>
    public double[] CentroIniziale { get; set; } = [11.0, 43.5];

    public double ZoomIniziale { get; set; } = 12;

    /// <summary>URL template per tile raster-dem (es. ".../{z}/{x}/{y}.png"). Vuoto/nullo = terreno 3D disattivato.</summary>
    public string? TerrainSourceUrl { get; set; }

    public int TerrainTileSize { get; set; } = 256;

    public double TerrainExaggeration { get; set; } = 1.2;
}
