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
}
