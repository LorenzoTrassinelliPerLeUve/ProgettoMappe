using System.Text.Json.Nodes;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Domain.Entities;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class VignetoGeoJsonBuilderTests
{
    [Fact]
    public void BuildFeatureCollection_include_solo_i_vigneti_con_geometria_valida()
    {
        var vigneti = new[]
        {
            new Vigneto
            {
                Id = 1,
                Nome = "Con geometria valida",
                GeometriaGeoJson = """{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]}"""
            },
            new Vigneto { Id = 2, Nome = "Senza geometria", GeometriaGeoJson = null },
            new Vigneto { Id = 3, Nome = "Geometria non valida", GeometriaGeoJson = "{non è json valido" },
        };

        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(vigneti);

        var features = featureCollection["features"]!.AsArray();
        Assert.Single(features);
        Assert.Equal(1, features[0]!["properties"]!["id"]!.GetValue<int>());
    }

    [Fact]
    public void BuildFeatureCollection_restituisce_una_FeatureCollection_vuota_senza_vigneti()
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection([]);

        Assert.Equal("FeatureCollection", featureCollection["type"]!.GetValue<string>());
        Assert.Empty(featureCollection["features"]!.AsArray());
    }

    // --- Feature.id ---

    [Fact]
    public void Feature_id_e_il_membro_GeoJSON_uguale_a_IdVigneto()
    {
        var feature = BuildFeatureSingola(10279, Quadrato(0, 0, 1));

        Assert.Equal(10279, feature["id"]!.GetValue<int>());
        Assert.Equal(10279, feature["properties"]!["id"]!.GetValue<int>());
    }

    // --- bbox della Feature ---

    [Fact]
    public void Bbox_di_un_polygon()
    {
        var feature = BuildFeatureSingola(1, """{"type":"Polygon","coordinates":[[[11.1,43.2],[11.4,43.2],[11.3,43.5],[11.1,43.2]]]}""");

        Assert.Equal([11.1, 43.2, 11.4, 43.5], Bbox(feature));
    }

    [Fact]
    public void Bbox_di_un_polygon_con_piu_ring_considera_tutti_gli_anelli()
    {
        // Anello esterno 0..10, foro interno 2..4: il bbox è quello dell'esterno.
        var feature = BuildFeatureSingola(1, """
            {"type":"Polygon","coordinates":[
              [[0,0],[10,0],[10,10],[0,10],[0,0]],
              [[2,2],[4,2],[4,4],[2,4],[2,2]]]}
            """);

        Assert.Equal([0.0, 0.0, 10.0, 10.0], Bbox(feature));
    }

    [Fact]
    public void Bbox_di_un_multipolygon_unisce_tutte_le_parti()
    {
        var feature = BuildFeatureSingola(1, """
            {"type":"MultiPolygon","coordinates":[
              [[[23.80,46.10],[23.85,46.10],[23.85,46.15],[23.80,46.10]]],
              [[[23.90,46.20],[23.95,46.20],[23.95,46.25],[23.90,46.20]]]]}
            """);

        Assert.Equal([23.80, 46.10, 23.95, 46.25], Bbox(feature));
    }

    [Fact]
    public void Bbox_con_coordinate_negative()
    {
        // Es. vigneti in Argentina/California: longitudini e latitudini negative.
        var feature = BuildFeatureSingola(1, """{"type":"Polygon","coordinates":[[[-68.5,-38.9],[-68.2,-38.9],[-68.2,-38.6],[-68.5,-38.9]]]}""");

        Assert.Equal([-68.5, -38.9, -68.2, -38.6], Bbox(feature));
    }

    [Fact]
    public void Bbox_di_un_point_e_degenere_ma_presente()
    {
        var feature = BuildFeatureSingola(1, """{"type":"Point","coordinates":[13.5,45.9]}""");

        Assert.Equal([13.5, 45.9, 13.5, 45.9], Bbox(feature));
    }

    [Fact]
    public void Senza_geometria_nessuna_feature_e_nessun_bbox()
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(
            [new Vigneto { Id = 1, Nome = "Fercal", GeometriaGeoJson = null }]);

        Assert.Empty(featureCollection["features"]!.AsArray());
        Assert.Null(featureCollection["bbox"]);
        Assert.Null(VignetoGeoJsonBuilder.CalcolaBbox(null));
    }

    [Fact]
    public void Geometria_senza_posizioni_valide_non_ha_bbox()
    {
        var feature = BuildFeatureSingola(1, """{"type":"Polygon","coordinates":[]}""");

        Assert.Null(feature["bbox"]);
    }

    // --- bbox della FeatureCollection ---

    [Fact]
    public void Bbox_della_FeatureCollection_unisce_piu_vigneti()
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(
        [
            new Vigneto { Id = 1, Nome = "A", GeometriaGeoJson = Quadrato(11.0, 43.0, 0.1) },
            new Vigneto { Id = 2, Nome = "B", GeometriaGeoJson = Quadrato(11.5, 43.4, 0.2) },
        ]);

        Assert.Equal([11.0, 43.0, 11.7, 43.6], ToDoubleArray(featureCollection["bbox"]), new ApprossimatoComparer());
    }

    [Fact]
    public void Bbox_della_FeatureCollection_ignora_i_vigneti_senza_geometria()
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(
        [
            new Vigneto { Id = 1, Nome = "Con geometria", GeometriaGeoJson = Quadrato(23.8, 46.1, 0.05) },
            new Vigneto { Id = 2, Nome = "Fercal", GeometriaGeoJson = null },
            new Vigneto { Id = 3, Nome = "MUGUREL", GeometriaGeoJson = "" },
        ]);

        Assert.Single(featureCollection["features"]!.AsArray());
        Assert.Equal([23.8, 46.1, 23.85, 46.15], ToDoubleArray(featureCollection["bbox"]), new ApprossimatoComparer());
    }

    [Fact]
    public void Senza_geometrie_valide_la_FeatureCollection_non_ha_bbox()
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(
        [
            new Vigneto { Id = 1, Nome = "Fercal", GeometriaGeoJson = null },
            new Vigneto { Id = 2, Nome = "Non valida", GeometriaGeoJson = "{non è json" },
        ]);

        Assert.False(featureCollection.ContainsKey("bbox"));
    }

    private static JsonNode BuildFeatureSingola(int id, string geometriaGeoJson)
    {
        var featureCollection = VignetoGeoJsonBuilder.BuildFeatureCollection(
            [new Vigneto { Id = id, Nome = "Test", GeometriaGeoJson = geometriaGeoJson }]);

        return featureCollection["features"]!.AsArray().Single()!;
    }

    private static string Quadrato(double lon, double lat, double lato) =>
        FormattableString.Invariant(
            $$"""{"type":"Polygon","coordinates":[[[{{lon}},{{lat}}],[{{lon + lato}},{{lat}}],[{{lon + lato}},{{lat + lato}}],[{{lon}},{{lat + lato}}],[{{lon}},{{lat}}]]]}""");

    private static double[] Bbox(JsonNode feature) => ToDoubleArray(feature["bbox"]);

    private static double[] ToDoubleArray(JsonNode? nodo) =>
        nodo!.AsArray().Select(v => v!.GetValue<double>()).ToArray();

    /// <summary>Le somme in virgola mobile (es. 11.5 + 0.2) non sono esatte.</summary>
    private sealed class ApprossimatoComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) < 1e-9;

        public int GetHashCode(double obj) => 0;
    }
}
