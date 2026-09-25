namespace ProgettoMappe.Web.Options;

/// <summary>Ruolo di una sorgente cartografica nella mappa.</summary>
public enum RuoloSorgente
{
    /// <summary>Mappa di base vettoriale: uno style MapLibre completo (Tipo "style").</summary>
    Basemap,

    /// <summary>Immagini satellitari/aeree: raster sopra la basemap e sotto i vigneti (Tipo "raster").</summary>
    Imagery,

    /// <summary>Modello digitale del terreno per il 3D (Tipo "raster-dem").</summary>
    Terrain,
}

/// <summary>
/// Una sorgente cartografica configurabile (sezione <c>Map:Sorgenti:{Id}</c>), indipendente dal
/// provider: MapLibre riceve solo URL, zoom, attribuzione e codifica. Una sola classe piatta,
/// validata da <see cref="ConfigurazioneMappa"/>: i campi usati dipendono da <see cref="Tipo"/>.
/// </summary>
public class SorgenteMappa
{
    /// <summary>Ricavato dalla chiave del dizionario <c>Map:Sorgenti</c>, non va scritto a mano.</summary>
    public string Id { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public RuoloSorgente? Ruolo { get; set; }

    /// <summary>"style", "raster", "raster-dem" (previsto "wms", non ancora supportato).</summary>
    public string Tipo { get; set; } = string.Empty;

    public string? StyleUrl { get; set; }

    /// <summary>Template XYZ (es. ".../{z}/{x}/{y}.png"). Ha la precedenza su <see cref="TileJsonUrl"/>.</summary>
    public string? TileUrl { get; set; }

    public string? TileJsonUrl { get; set; }

    public string? Attribution { get; set; }

    public int? MinZoom { get; set; }

    /// <summary>
    /// Zoom massimo realmente servito: oltre, MapLibre riusa (sovra-campiona) le tile disponibili
    /// senza nuove richieste. Obbligatorio per "raster-dem": non ci si fida del TileJSON, che per
    /// Mapterhorn non lo dichiara.
    /// </summary>
    public int? MaxZoom { get; set; }

    public int? TileSize { get; set; }

    /// <summary>Solo "raster-dem": "terrarium" o "mapbox".</summary>
    public string? Encoding { get; set; }

    /// <summary>
    /// NOME della chiave in <c>Map:ApiKeys</c> (mai il valore): gli URL la richiamano con il
    /// segnaposto <c>{apiKey}</c>. Una chiave usata dal browser è comunque visibile al client:
    /// la protezione vera è la restrizione per HTTP Origin nel pannello del provider.
    /// </summary>
    public string? ApiKeyName { get; set; }

    /// <summary>Area coperta [ovest, sud, est, nord], facoltativa (es. ortofoto regionali).</summary>
    public double[] Bounds { get; set; } = [];

    public bool Enabled { get; set; } = true;
}
