using System.Globalization;
using System.Text.Json;

namespace ProgettoMappe.Infrastructure.GeoJson;

/// <summary>
/// Converte geometrie WKT (Well-Known Text, es. <c>Vigneto.Poligono.STAsText()</c>) in GeoJSON.
/// Le coordinate vengono sempre trattate come WGS84 (lon/lat in gradi): l'SRID dichiarato sui
/// dati reali di 4Grapes non è uniforme (vedi CLAUDE.md), ma i valori sono comunque sempre
/// gradi geografici, in ordine (lon, lat) coerente con GeoJSON — nessuno scambio di coordinate
/// necessario. Restituisce null per WKT vuoto/non riconosciuto invece di lanciare eccezioni.
/// </summary>
public static class WktGeoJsonConverter
{
    public static string? ToGeoJson(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        var testo = wkt.Trim();
        var indiceParentesi = testo.IndexOf('(');
        if (indiceParentesi < 0)
        {
            return null;
        }

        var tipo = testo[..indiceParentesi].Trim().ToUpperInvariant();
        var corpo = testo[indiceParentesi..];

        object? coordinate = tipo switch
        {
            "POINT" => ParsePunto(TogliParentesiEsterne(corpo)),
            "MULTIPOINT" => ParseMultiPunto(corpo),
            "LINESTRING" => ParseListaPunti(corpo),
            "MULTILINESTRING" => ParseListaListaPunti(corpo),
            "POLYGON" => ParseListaListaPunti(corpo),
            "MULTIPOLYGON" => ParseListaListaListaPunti(corpo),
            _ => null,
        };

        if (coordinate is null)
        {
            return null;
        }

        var geoJsonType = tipo switch
        {
            "POINT" => "Point",
            "MULTIPOINT" => "MultiPoint",
            "LINESTRING" => "LineString",
            "MULTILINESTRING" => "MultiLineString",
            "POLYGON" => "Polygon",
            "MULTIPOLYGON" => "MultiPolygon",
            _ => throw new InvalidOperationException("Tipo WKT non gestito."),
        };

        return JsonSerializer.Serialize(new { type = geoJsonType, coordinates = coordinate });
    }

    private static string TogliParentesiEsterne(string testo)
    {
        var t = testo.Trim();
        return t.StartsWith('(') && t.EndsWith(')') ? t[1..^1].Trim() : t;
    }

    /// <summary>Divide una lista al livello più esterno rispettando l'annidamento delle parentesi.</summary>
    private static List<string> DividiLivelloEsterno(string testo)
    {
        var risultato = new List<string>();
        var profondita = 0;
        var inizio = 0;

        for (var i = 0; i < testo.Length; i++)
        {
            switch (testo[i])
            {
                case '(':
                    profondita++;
                    break;
                case ')':
                    profondita--;
                    break;
                case ',' when profondita == 0:
                    risultato.Add(testo[inizio..i].Trim());
                    inizio = i + 1;
                    break;
            }
        }

        risultato.Add(testo[inizio..].Trim());
        return risultato;
    }

    private static double[] ParsePunto(string testo)
    {
        var numeri = testo.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return
        [
            double.Parse(numeri[0], CultureInfo.InvariantCulture),
            double.Parse(numeri[1], CultureInfo.InvariantCulture),
        ];
    }

    /// <summary>Es. "(x y, x y, ...)" -> lista di punti.</summary>
    private static List<double[]> ParseListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesiEsterne(testo)).Select(ParsePunto).ToList();

    /// <summary>Es. "((x y, ...), (x y, ...))" -> lista di liste di punti (anelli di un poligono).</summary>
    private static List<List<double[]>> ParseListaListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesiEsterne(testo)).Select(ParseListaPunti).ToList();

    /// <summary>Es. "(((x y, ...), (...)), ((x y, ...)))" -> lista di poligoni (MultiPolygon).</summary>
    private static List<List<List<double[]>>> ParseListaListaListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesiEsterne(testo)).Select(ParseListaListaPunti).ToList();

    /// <summary>MULTIPOINT ammette sia "(1 2, 3 4)" sia "((1 2), (3 4))": gestisce entrambe.</summary>
    private static List<double[]> ParseMultiPunto(string testo) =>
        DividiLivelloEsterno(TogliParentesiEsterne(testo)).Select(p => ParsePunto(TogliParentesiEsterne(p))).ToList();
}
