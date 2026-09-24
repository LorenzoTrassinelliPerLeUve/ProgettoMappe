using System.Text.Json;
using System.Text.Json.Nodes;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.GeoJson;

/// <summary>
/// Costruisce una FeatureCollection GeoJSON a partire dai vigneti, scartando in modo
/// esplicito le geometrie mancanti o non valide invece di far fallire l'intera risposta.
/// Ogni Feature ha il membro GeoJSON <c>id</c> = IdVigneto (usato da MapLibre per il
/// feature-state) e, quando calcolabile, il <c>bbox</c> [west, south, east, north]; la
/// FeatureCollection ha il bbox complessivo. Il client usa questi dati per selezione e
/// movimento camera, senza dipendere dalle feature che MapLibre ha renderizzato.
/// </summary>
public static class VignetoGeoJsonBuilder
{
    public static JsonObject BuildFeatureCollection(IEnumerable<Vigneto> vigneti)
    {
        var features = new JsonArray();
        double[]? bboxComplessivo = null;

        foreach (var vigneto in vigneti)
        {
            var geometria = ParseGeometriaOrNull(vigneto.GeometriaGeoJson);
            if (geometria is null)
            {
                continue;
            }

            var feature = new JsonObject
            {
                ["type"] = "Feature",
                ["id"] = vigneto.Id,
                ["properties"] = new JsonObject
                {
                    ["id"] = vigneto.Id,
                    ["nome"] = vigneto.Nome,
                    ["varieta"] = vigneto.Varieta,
                    ["superficieEttari"] = vigneto.SuperficieEttari,
                },
                ["geometry"] = geometria,
            };

            var bbox = CalcolaBbox(geometria);
            if (bbox is not null)
            {
                feature["bbox"] = ToJsonArray(bbox);
                bboxComplessivo = Unisci(bboxComplessivo, bbox);
            }

            features.Add(feature);
        }

        var featureCollection = new JsonObject
        {
            ["type"] = "FeatureCollection",
            ["features"] = features,
        };

        if (bboxComplessivo is not null)
        {
            featureCollection["bbox"] = ToJsonArray(bboxComplessivo);
        }

        return featureCollection;
    }

    /// <summary>
    /// Bbox [minLon, minLat, maxLon, maxLat] di una geometria GeoJSON: considera tutte le
    /// posizioni di <c>coordinates</c> a qualsiasi profondità (Point … MultiPolygon, anelli
    /// interni compresi) e le <c>geometries</c> di una GeometryCollection. Null se non ci sono
    /// posizioni valide.
    /// </summary>
    public static double[]? CalcolaBbox(JsonNode? geometria)
    {
        double[]? bbox = null;
        Raccogli(geometria?["coordinates"], ref bbox);

        if (geometria?["geometries"] is JsonArray geometrie)
        {
            foreach (var parte in geometrie)
            {
                if (CalcolaBbox(parte) is { } bboxParte)
                {
                    bbox = Unisci(bbox, bboxParte);
                }
            }
        }

        return bbox;
    }

    private static void Raccogli(JsonNode? nodo, ref double[]? bbox)
    {
        if (nodo is not JsonArray array || array.Count == 0)
        {
            return;
        }

        // Una posizione è un array che inizia con un numero: [lon, lat, (alt)].
        if (array[0] is JsonValue)
        {
            if (array.Count >= 2
                && array[0]!.GetValueKind() == JsonValueKind.Number
                && array[1]!.GetValueKind() == JsonValueKind.Number)
            {
                var lon = array[0]!.GetValue<double>();
                var lat = array[1]!.GetValue<double>();
                if (double.IsFinite(lon) && double.IsFinite(lat))
                {
                    bbox = Unisci(bbox, [lon, lat, lon, lat]);
                }
            }

            return;
        }

        foreach (var figlio in array)
        {
            Raccogli(figlio, ref bbox);
        }
    }

    private static double[] Unisci(double[]? a, double[] b)
    {
        return a is null
            ? b
            : [Math.Min(a[0], b[0]), Math.Min(a[1], b[1]), Math.Max(a[2], b[2]), Math.Max(a[3], b[3])];
    }

    private static JsonArray ToJsonArray(double[] bbox)
    {
        return new JsonArray(bbox.Select(v => (JsonNode)JsonValue.Create(v)).ToArray());
    }

    private static JsonNode? ParseGeometriaOrNull(string? geometriaGeoJson)
    {
        if (string.IsNullOrWhiteSpace(geometriaGeoJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(geometriaGeoJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
