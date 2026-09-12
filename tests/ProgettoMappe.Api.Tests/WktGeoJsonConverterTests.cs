using System.Text.Json;
using ProgettoMappe.Infrastructure.GeoJson;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class WktGeoJsonConverterTests
{
    [Fact]
    public void ToGeoJson_restituisce_null_per_input_vuoto_o_non_valido()
    {
        Assert.Null(WktGeoJsonConverter.ToGeoJson(null));
        Assert.Null(WktGeoJsonConverter.ToGeoJson(""));
        Assert.Null(WktGeoJsonConverter.ToGeoJson("non è WKT"));
    }

    [Fact]
    public void ToGeoJson_converte_un_point()
    {
        var geoJson = WktGeoJsonConverter.ToGeoJson("POINT (13.5 45.9)");

        var json = JsonDocument.Parse(geoJson!).RootElement;
        Assert.Equal("Point", json.GetProperty("type").GetString());
        var coordinate = json.GetProperty("coordinates");
        Assert.Equal(13.5, coordinate[0].GetDouble());
        Assert.Equal(45.9, coordinate[1].GetDouble());
    }

    [Fact]
    public void ToGeoJson_converte_un_polygon_con_un_solo_anello()
    {
        var geoJson = WktGeoJsonConverter.ToGeoJson(
            "POLYGON ((13.0 45.0, 13.1 45.0, 13.1 45.1, 13.0 45.1, 13.0 45.0))");

        var json = JsonDocument.Parse(geoJson!).RootElement;
        Assert.Equal("Polygon", json.GetProperty("type").GetString());

        var anelli = json.GetProperty("coordinates");
        Assert.Equal(1, anelli.GetArrayLength());
        var primoAnello = anelli[0];
        Assert.Equal(5, primoAnello.GetArrayLength());
        Assert.Equal(13.0, primoAnello[0][0].GetDouble());
        Assert.Equal(45.0, primoAnello[0][1].GetDouble());
    }

    [Fact]
    public void ToGeoJson_converte_un_multipolygon()
    {
        var geoJson = WktGeoJsonConverter.ToGeoJson(
            "MULTIPOLYGON (((0 0, 1 0, 1 1, 0 1, 0 0)), ((10 10, 11 10, 11 11, 10 11, 10 10)))");

        var json = JsonDocument.Parse(geoJson!).RootElement;
        Assert.Equal("MultiPolygon", json.GetProperty("type").GetString());

        var poligoni = json.GetProperty("coordinates");
        Assert.Equal(2, poligoni.GetArrayLength());
        Assert.Equal(10.0, poligoni[1][0][0][0].GetDouble());
    }
}
