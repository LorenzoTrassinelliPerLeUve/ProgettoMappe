using System.Text.Json;
using System.Text.Json.Nodes;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.GeoJson;

/// <summary>
/// Costruisce una FeatureCollection GeoJSON a partire dai vigneti, scartando in modo
/// esplicito le geometrie mancanti o non valide invece di far fallire l'intera risposta.
/// </summary>
public static class VignetoGeoJsonBuilder
{
    public static JsonObject BuildFeatureCollection(IEnumerable<Vigneto> vigneti)
    {
        var features = new JsonArray();

        foreach (var vigneto in vigneti)
        {
            var geometria = ParseGeometriaOrNull(vigneto.GeometriaGeoJson);
            if (geometria is null)
            {
                continue;
            }

            features.Add(new JsonObject
            {
                ["type"] = "Feature",
                ["properties"] = new JsonObject
                {
                    ["id"] = vigneto.Id,
                    ["nome"] = vigneto.Nome,
                    ["varieta"] = vigneto.Varieta,
                    ["superficieEttari"] = vigneto.SuperficieEttari,
                },
                ["geometry"] = geometria,
            });
        }

        return new JsonObject
        {
            ["type"] = "FeatureCollection",
            ["features"] = features,
        };
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
