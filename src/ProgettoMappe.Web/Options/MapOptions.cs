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

    /// <summary>Centro usato se <see cref="CentroIniziale"/> manca o non è valido.</summary>
    public static readonly double[] CentroPredefinito = [11.0, 43.5];

    /// <summary>
    /// Centro iniziale della mappa [longitudine, latitudine], prima che si selezioni un'azienda/vigneto.
    /// Parte vuoto di proposito: il configuration binder accoda gli elementi della configurazione
    /// a un array già valorizzato (un default [11, 43.5] diventerebbe [11, 43.5, 11, 43.5]).
    /// Leggerlo tramite <see cref="CentroInizialeValido"/>.
    /// </summary>
    public double[] CentroIniziale { get; set; } = [];

    /// <summary>
    /// <see cref="CentroIniziale"/> se è esattamente [lon, lat] con valori finiti, altrimenti
    /// <see cref="CentroPredefinito"/>: MapLibre rifiuta qualsiasi altra forma.
    /// </summary>
    public double[] CentroInizialeValido()
    {
        return CentroIniziale is [var lon, var lat] && double.IsFinite(lon) && double.IsFinite(lat)
            ? CentroIniziale
            : CentroPredefinito;
    }

    public double ZoomIniziale { get; set; } = 12;

    /// <summary>URL template per tile raster-dem (es. ".../{z}/{x}/{y}.png"). Vuoto/nullo = terreno 3D disattivato.</summary>
    public string? TerrainSourceUrl { get; set; }

    public int TerrainTileSize { get; set; } = 256;

    public double TerrainExaggeration { get; set; } = 1.2;
}
