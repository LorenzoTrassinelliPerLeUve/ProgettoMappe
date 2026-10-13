using System.Globalization;
using System.Text.Json;

namespace ProgettoMappe.Infrastructure.GeoJson;

/// <summary>
/// Converte geometrie WKT (Well-Known Text, es. <c>Vigneto.Poligono.STAsText()</c>) in GeoJSON.
/// Le coordinate vengono sempre trattate come WGS84 (lon/lat in gradi): l'SRID dichiarato sui
/// dati reali di 4Grapes non è uniforme (vedi CLAUDE.md), ma i valori sono comunque sempre
/// gradi geografici, in ordine (lon, lat) coerente con GeoJSON — nessuno scambio di coordinate
/// necessario. Non lancia mai eccezioni per il contenuto del WKT: vedi <see cref="TryToGeoJson"/>.
/// </summary>
public static class WktGeoJsonConverter
{
    /// <summary>GeoJSON, oppure null se il WKT manca, è vuoto o non è leggibile.</summary>
    public static string? ToGeoJson(string? wkt) => TryToGeoJson(wkt, out var geoJson) ? geoJson : null;

    /// <summary>
    /// true con <paramref name="geoJson"/> valorizzato se il WKT è stato convertito; true con
    /// null se non c'è una geometria (null, testo vuoto, "POLYGON EMPTY"); false se il WKT c'è
    /// ma è malformato (parentesi sbilanciate, numeri illeggibili o non finiti, punti con meno
    /// di due coordinate, tipo non gestito): il chiamante lo può contare come dato da correggere.
    /// </summary>
    public static bool TryToGeoJson(string? wkt, out string? geoJson)
    {
        geoJson = null;
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return true;
        }

        var testo = wkt.Trim();
        var indiceParentesi = testo.IndexOf('(');
        if (indiceParentesi < 0)
        {
            return testo.EndsWith(" EMPTY", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            geoJson = Converti(testo, indiceParentesi);
            return geoJson is not null;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? Converti(string testo, int indiceParentesi)
    {

        var tipo = testo[..indiceParentesi].Trim().ToUpperInvariant();
        var corpo = testo[indiceParentesi..];
        ControllaParentesi(corpo);

        object? coordinate = tipo switch
        {
            "POINT" => ParsePunto(TogliParentesi(corpo)),
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

    private static void ControllaParentesi(string testo)
    {
        var profondita = 0;
        foreach (var c in testo)
        {
            profondita += c switch { '(' => 1, ')' => -1, _ => 0 };
            if (profondita < 0)
            {
                throw new FormatException("Parentesi sbilanciate.");
            }
        }

        if (profondita != 0)
        {
            throw new FormatException("Parentesi sbilanciate.");
        }
    }

    /// <summary>Come <see cref="TogliParentesiEsterne"/>, ma le parentesi sono obbligatorie.</summary>
    private static string TogliParentesi(string testo)
    {
        var t = testo.Trim();
        return t.StartsWith('(') && t.EndsWith(')') ? t[1..^1].Trim() : throw new FormatException("Parentesi mancanti.");
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
        if (numeri.Length < 2)
        {
            throw new FormatException("Punto con meno di due coordinate.");
        }

        return [Coordinata(numeri[0]), Coordinata(numeri[1])];
    }

    private static double Coordinata(string testo) =>
        double.TryParse(testo, NumberStyles.Float, CultureInfo.InvariantCulture, out var valore) && double.IsFinite(valore)
            ? valore
            : throw new FormatException("Coordinata non leggibile.");

    /// <summary>Es. "(x y, x y, ...)" -> lista di punti.</summary>
    private static List<double[]> ParseListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesi(testo)).Select(ParsePunto).ToList();

    /// <summary>Es. "((x y, ...), (x y, ...))" -> lista di liste di punti (anelli di un poligono).</summary>
    private static List<List<double[]>> ParseListaListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesi(testo)).Select(ParseListaPunti).ToList();

    /// <summary>Es. "(((x y, ...), (...)), ((x y, ...)))" -> lista di poligoni (MultiPolygon).</summary>
    private static List<List<List<double[]>>> ParseListaListaListaPunti(string testo) =>
        DividiLivelloEsterno(TogliParentesi(testo)).Select(ParseListaListaPunti).ToList();

    /// <summary>MULTIPOINT ammette sia "(1 2, 3 4)" sia "((1 2), (3 4))": gestisce entrambe.</summary>
    private static List<double[]> ParseMultiPunto(string testo) =>
        DividiLivelloEsterno(TogliParentesi(testo)).Select(p => ParsePunto(TogliParentesiEsterne(p))).ToList();
}
