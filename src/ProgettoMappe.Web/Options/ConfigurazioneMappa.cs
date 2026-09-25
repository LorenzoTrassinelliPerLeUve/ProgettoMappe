using Microsoft.Extensions.Logging;

namespace ProgettoMappe.Web.Options;

/// <summary>
/// Sorgente validata e pronta per MapLibre, con l'eventuale <c>{apiKey}</c> già sostituito.
/// Contiene quindi valori che finiscono nel browser (dove una chiave è comunque visibile): non
/// va mai loggata, e <see cref="ToString"/> espone solo Id e ruolo.
/// </summary>
public sealed record SorgenteRisolta(
    string Id,
    string Nome,
    RuoloSorgente Ruolo,
    string Tipo,
    string? StyleUrl,
    string? TileUrl,
    string? TileJsonUrl,
    string? Attribution,
    int? MinZoom,
    int? MaxZoom,
    int? TileSize,
    string? Encoding,
    double[]? Bounds)
{
    public override string ToString() => $"{Ruolo} '{Id}'";
}

/// <summary>Sorgente scartata in validazione: solo Id e motivo, mai URL o chiavi.</summary>
public sealed record SorgenteScartata(string Id, string Motivo);

/// <summary>
/// Configurazione cartografica validata una volta all'avvio (singleton). Al browser arrivano solo
/// le sorgenti abilitate, complete e con la chiave configurata; gli Id di default puntano sempre
/// a sorgenti valide del ruolo giusto, altrimenti sono null (il JS resta sullo style interno).
/// </summary>
public sealed class ConfigurazioneMappa
{
    public const string SegnapostoApiKey = "{apiKey}";

    private static readonly string[] TipiSupportati = ["style", "raster", "raster-dem"];
    private static readonly string[] EncodingDem = ["terrarium", "mapbox"];

    public required IReadOnlyList<SorgenteRisolta> Sorgenti { get; init; }

    public required IReadOnlyList<SorgenteScartata> Scartate { get; init; }

    public string? BasemapPredefinitaId { get; init; }

    public string? BasemapFallbackId { get; init; }

    /// <summary>True se la basemap configurata come predefinita non è utilizzabile e si parte dalla riserva.</summary>
    public bool BasemapPredefinitaSostituita { get; init; }

    public string? ImageryPredefinitaId { get; init; }

    public string? TerrainId { get; init; }

    public double TerrainExaggeration { get; init; }

    public int TimeoutStyleMs { get; init; }

    public bool ModalitaSviluppatore { get; init; }

    public IEnumerable<SorgenteRisolta> DelRuolo(RuoloSorgente ruolo) => Sorgenti.Where(s => s.Ruolo == ruolo);

    public static ConfigurazioneMappa Crea(MapOptions opzioni, ILogger? logger = null)
    {
        var valide = new List<SorgenteRisolta>();
        var scartate = new List<SorgenteScartata>();

        foreach (var (id, sorgente) in opzioni.Sorgenti.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var esito = Risolvi(id, sorgente, opzioni.ApiKeys);
            if (esito.Risolta is not null)
            {
                valide.Add(esito.Risolta);
            }
            else
            {
                scartate.Add(new SorgenteScartata(id, esito.Motivo!));
                logger?.LogWarning("Sorgente cartografica '{Id}' non utilizzata: {Motivo}", id, esito.Motivo);
            }
        }

        string? IdValido(string? id, RuoloSorgente ruolo, string impostazione)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (valide.Any(s => s.Id == id && s.Ruolo == ruolo))
            {
                return id;
            }

            logger?.LogWarning("{Impostazione} = '{Id}' non indica una sorgente {Ruolo} valida: ignorata", impostazione, id, ruolo);
            return null;
        }

        var fallback = IdValido(opzioni.BasemapFallback, RuoloSorgente.Basemap, nameof(MapOptions.BasemapFallback));
        var predefinita = IdValido(opzioni.BasemapPredefinita, RuoloSorgente.Basemap, nameof(MapOptions.BasemapPredefinita));
        var sostituita = !string.IsNullOrWhiteSpace(opzioni.BasemapPredefinita) && predefinita is null;

        return new ConfigurazioneMappa
        {
            Sorgenti = valide,
            Scartate = scartate,
            BasemapPredefinitaId = predefinita ?? fallback,
            BasemapFallbackId = fallback,
            BasemapPredefinitaSostituita = sostituita,
            ImageryPredefinitaId = IdValido(opzioni.ImageryPredefinita, RuoloSorgente.Imagery, nameof(MapOptions.ImageryPredefinita)),
            TerrainId = IdValido(opzioni.TerrainPredefinito, RuoloSorgente.Terrain, nameof(MapOptions.TerrainPredefinito)),
            TerrainExaggeration = opzioni.TerrainExaggeration is > 0 and <= 10 ? opzioni.TerrainExaggeration : 1.0,
            TimeoutStyleMs = Math.Clamp(opzioni.TimeoutStyleSecondi, 2, 60) * 1000,
            ModalitaSviluppatore = opzioni.ModalitaSviluppatore,
        };
    }

    /// <summary>Valida una sorgente e, se valida, ne sostituisce la chiave negli URL.</summary>
    public static (SorgenteRisolta? Risolta, string? Motivo) Risolvi(
        string id, SorgenteMappa s, IReadOnlyDictionary<string, string> apiKeys)
    {
        if (!s.Enabled) return (null, "disabilitata");
        if (string.IsNullOrWhiteSpace(id)) return (null, "Id mancante");
        if (s.Ruolo is not { } ruolo) return (null, "Ruolo mancante o non valido");

        var tipo = s.Tipo.Trim().ToLowerInvariant();
        if (tipo == "wms") return (null, "Tipo 'wms' non ancora supportato");
        if (!TipiSupportati.Contains(tipo)) return (null, $"Tipo '{s.Tipo}' non supportato");

        var tipoAtteso = ruolo switch
        {
            RuoloSorgente.Basemap => "style",
            RuoloSorgente.Imagery => "raster",
            _ => "raster-dem",
        };
        if (tipo != tipoAtteso) return (null, $"il ruolo {ruolo} richiede Tipo '{tipoAtteso}'");

        if (tipo == "style")
        {
            if (string.IsNullOrWhiteSpace(s.StyleUrl)) return (null, "StyleUrl mancante");
        }
        else if (string.IsNullOrWhiteSpace(s.TileUrl) && string.IsNullOrWhiteSpace(s.TileJsonUrl))
        {
            return (null, "TileUrl o TileJsonUrl mancante");
        }

        if (tipo == "raster-dem")
        {
            if (s.Encoding is null || !EncodingDem.Contains(s.Encoding.Trim().ToLowerInvariant()))
                return (null, "Encoding mancante o non valido (terrarium|mapbox)");
            if (s.MaxZoom is null) return (null, "MaxZoom obbligatorio per raster-dem");
        }

        if (s.MinZoom is < 0 or > 24 || s.MaxZoom is < 0 or > 24) return (null, "MinZoom/MaxZoom fuori intervallo 0–24");
        if (s.MinZoom > s.MaxZoom) return (null, "MinZoom maggiore di MaxZoom");
        if (s.TileSize is <= 0) return (null, "TileSize non valido");

        double[]? bounds = null;
        if (s.Bounds.Length > 0)
        {
            if (s.Bounds is not [var w, var so, var e, var n]
                || !s.Bounds.All(double.IsFinite) || w >= e || so >= n)
            {
                return (null, "Bounds non valido (atteso [ovest, sud, est, nord])");
            }

            bounds = s.Bounds;
        }

        var urls = new[] { s.StyleUrl, s.TileUrl, s.TileJsonUrl };
        var usaSegnaposto = urls.Any(u => u?.Contains(SegnapostoApiKey, StringComparison.Ordinal) == true);
        string? chiave = null;

        if (!string.IsNullOrWhiteSpace(s.ApiKeyName))
        {
            if (!apiKeys.TryGetValue(s.ApiKeyName, out chiave) || string.IsNullOrWhiteSpace(chiave))
                return (null, $"chiave '{s.ApiKeyName}' non configurata");
            if (!usaSegnaposto) return (null, $"ApiKeyName impostato ma nessun URL contiene {SegnapostoApiKey}");
        }
        else if (usaSegnaposto)
        {
            return (null, $"URL con {SegnapostoApiKey} ma ApiKeyName mancante");
        }

        string? Sostituisci(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            return chiave is null ? url : url.Replace(SegnapostoApiKey, Uri.EscapeDataString(chiave), StringComparison.Ordinal);
        }

        var styleUrl = Sostituisci(s.StyleUrl);
        var tileUrl = Sostituisci(s.TileUrl);
        var tileJsonUrl = Sostituisci(s.TileJsonUrl);

        // I template XYZ contengono {z}/{x}/{y}: la verifica di URL assoluto si fa sulla parte fissa.
        foreach (var url in new[] { styleUrl, tileUrl, tileJsonUrl })
        {
            if (url is not null && !UrlAssolutoHttp(url)) return (null, "URL non assoluto o schema non http/https");
        }

        var risolta = new SorgenteRisolta(
            id,
            string.IsNullOrWhiteSpace(s.Nome) ? id : s.Nome,
            ruolo,
            tipo,
            tipo == "style" ? styleUrl : null,
            tipo == "style" ? null : tileUrl,
            tipo == "style" || tileUrl is not null ? null : tileJsonUrl,
            string.IsNullOrWhiteSpace(s.Attribution) ? null : s.Attribution,
            s.MinZoom,
            s.MaxZoom,
            s.TileSize,
            tipo == "raster-dem" ? s.Encoding!.Trim().ToLowerInvariant() : null,
            bounds);

        return (risolta, null);
    }

    private static bool UrlAssolutoHttp(string url)
    {
        var baseUrl = url.Split('{')[0];
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
