namespace ProgettoMappe.Web.Options;

/// <summary>
/// Configurazione del motore cartografico: provider ed endpoint arrivano da qui (config/
/// environment/user-secrets), mai hardcodati nel codice o nel JS. Le sorgenti sono
/// provider-neutral (<see cref="SorgenteMappa"/>) e vengono validate/risolte da
/// <see cref="ConfigurazioneMappa"/> prima di arrivare al browser. La mappa nasce sempre con uno
/// style interno senza rete: la basemap esterna è un miglioramento, mai un prerequisito.
/// </summary>
public class MapOptions
{
    public const string SectionName = "Map";

    /// <summary>Centro usato se <see cref="CentroIniziale"/> manca o non è valido.</summary>
    public static readonly double[] CentroPredefinito = [11.0, 43.5];

    /// <summary>
    /// Centro iniziale della mappa [longitudine, latitudine], prima che si selezioni un'azienda/vigneto.
    /// Parte vuoto di proposito: il configuration binder accoda gli elementi della configurazione
    /// a un array già valorizzato (un default [11, 43.5] diventerebbe [11, 43.5, 11, 43.5]).
    /// Leggerlo tramite <see cref="CentroInizialeValido"/>.
    /// </summary>
    public double[] CentroIniziale { get; set; } = [];

    public double ZoomIniziale { get; set; } = 12;

    /// <summary>Sorgenti cartografiche per Id (dizionario: le sezioni di ambienti diversi si fondono per Id, non per indice).</summary>
    public Dictionary<string, SorgenteMappa> Sorgenti { get; set; } = new();

    /// <summary>Id della basemap da tentare all'avvio.</summary>
    public string? BasemapPredefinita { get; set; }

    /// <summary>Id della basemap di riserva (senza chiave), usata se la predefinita manca o fallisce.</summary>
    public string? BasemapFallback { get; set; }

    /// <summary>Id dell'imagery usata dal pulsante "Satellite".</summary>
    public string? ImageryPredefinita { get; set; }

    /// <summary>Id della sorgente "raster-dem" usata dal pulsante 3D.</summary>
    public string? TerrainPredefinito { get; set; }

    public double TerrainExaggeration { get; set; } = 1.2;

    /// <summary>Tempo massimo per scaricare e applicare uno style prima di passare alla riserva.</summary>
    public int TimeoutStyleSecondi { get; set; } = 10;

    /// <summary>Mostra i selettori tecnici (basemap/imagery per provider) usati nel benchmark.</summary>
    public bool ModalitaSviluppatore { get; set; }

    /// <summary>Valori delle API key per nome (solo user-secrets/variabili d'ambiente, mai committati).</summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new();

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
}
