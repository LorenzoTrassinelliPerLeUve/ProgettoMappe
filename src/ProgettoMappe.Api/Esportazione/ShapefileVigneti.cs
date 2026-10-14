using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using NetTopologySuite.IO.Esri.Dbf.Fields;
using NetTopologySuite.IO.Esri.Shapefiles.Writers;

namespace ProgettoMappe.Api.Esportazione;

/// <summary>
/// Shapefile poligonale in uno zip (.shp .shx .dbf .prj e .cpg), WGS84, .dbf in UTF-8, più uno
/// shapefile di punti per i vigneti senza poligono. Le geometrie GeoJSON Polygon/MultiPolygon
/// diventano MultiPolygon, gli altri tipi non entrano nello strato dei poligoni. Anelli aperti si chiudono, anelli con meno di
/// quattro vertici si scartano; gli anelli esterni vanno in senso orario (regola dello shapefile).
/// </summary>
public static class ShapefileVigneti
{
    /// <summary>WGS84 nel formato .prj di ESRI (lo stesso che scrive QGIS per EPSG:4326).</summary>
    public const string ProiezioneWgs84 =
        "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]]," +
        "PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";

    private static readonly GeometryFactory Fabbrica = new(new PrecisionModel(), 4326);

    /// <summary>
    /// Lo zip con lo shapefile dei poligoni (<paramref name="nomeBase"/>.*) e, se ci sono vigneti
    /// senza poligono, quello dei loro punti (<paramref name="nomeBase"/>-punti.*): uno shapefile ha
    /// un solo tipo di geometria.
    /// </summary>
    public static byte[] Scrivi(string nomeBase, IReadOnlyList<VignetoEsportato> vigneti,
        IReadOnlyList<SottovignetoEsportato>? sottovigneti = null, IReadOnlyList<FilareEsportato>? filari = null)
    {
        using var zip = new MemoryStream();
        using (var archivio = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            ScriviStrato(archivio, nomeBase, ShapeType.Polygon, vigneti.Where(v => !v.Vigneto.SoloPunto), Poligoni);
            var punti = vigneti.Where(v => v.Vigneto.SoloPunto).ToList();
            if (punti.Count > 0)
            {
                ScriviStrato(archivio, nomeBase + "-punti", ShapeType.Point, punti, Punto);
            }

            // Sottovigneti (task #66): terzo shapefile, solo se ce n'è almeno uno con un'area.
            var conForma = (sottovigneti ?? []).Where(s => Poligoni(EsportazioneVigneti.Geometria(s.Sottovigneto)) is not null).ToList();
            if (conForma.Count > 0)
            {
                ScriviStrato(archivio, nomeBase + "-sottovigneti", ShapeType.Polygon, conForma, EsportazioneVigneti.CampiSottovigneti,
                    s => Poligoni(EsportazioneVigneti.Geometria(s.Sottovigneto)));
            }

            // Filari (task #95): shapefile di linee «-filari», solo se ce ne sono.
            if (filari is { Count: > 0 })
            {
                ScriviStrato(archivio, nomeBase + "-filari", ShapeType.PolyLine, filari, EsportazioneVigneti.CampiFilari, f => Linea(f.Geometria));
            }
        }

        return zip.ToArray();
    }

    private static void ScriviStrato(ZipArchive archivio, string nome, ShapeType tipo,
        IEnumerable<VignetoEsportato> vigneti, Func<JsonNode?, Geometry?> geometriaDi) =>
        ScriviStrato(archivio, nome, tipo, vigneti, EsportazioneVigneti.Campi, v => geometriaDi(EsportazioneVigneti.Geometria(v.Vigneto)));

    private static void ScriviStrato<T>(ZipArchive archivio, string nome, ShapeType tipo,
        IEnumerable<T> elementi, IReadOnlyList<CampoEsportato<T>> campi, Func<T, Geometry?> geometriaDi)
    {
        var shp = new MemoryStream();
        var shx = new MemoryStream();
        var dbf = new MemoryStream();
        var prj = new MemoryStream();

        var opzioni = new ShapefileWriterOptions(tipo, Campi(campi))
        {
            Encoding = Encoding.UTF8,
            Projection = ProiezioneWgs84,
        };

        using (var scrittore = Shapefile.OpenWrite(shp, shx, dbf, prj, opzioni))
        {
            foreach (var v in elementi)
            {
                if (geometriaDi(v) is not { } geometria)
                {
                    continue;
                }

                foreach (var campo in campi)
                {
                    scrittore.Fields[campo.Nome].Value = campo.Tipo == TipoCampo.Testo
                        ? Taglia(campo.Valore(v) as string)
                        : campo.Valore(v);
                }
                scrittore.Geometry = geometria;
                scrittore.Write();
            }
        }

        Aggiungi(archivio, nome + ".shp", shp.ToArray());
        Aggiungi(archivio, nome + ".shx", shx.ToArray());
        Aggiungi(archivio, nome + ".dbf", dbf.ToArray());
        Aggiungi(archivio, nome + ".prj", prj.ToArray());
        Aggiungi(archivio, nome + ".cpg", Encoding.ASCII.GetBytes("UTF-8"));
    }

    /// <summary>Point GeoJSON → Point NTS; null se non è un punto con coordinate valide.</summary>
    public static Point? Punto(JsonNode? geometria)
    {
        if (geometria?["type"]?.GetValueKind() != JsonValueKind.String || geometria["type"]!.GetValue<string>() != "Point"
            || geometria["coordinates"] is not JsonArray { Count: >= 2 } p
            || p[0]?.GetValueKind() != JsonValueKind.Number || p[1]?.GetValueKind() != JsonValueKind.Number)
        {
            return null;
        }

        var lon = p[0]!.GetValue<double>();
        var lat = p[1]!.GetValue<double>();
        return double.IsFinite(lon) && double.IsFinite(lat) ? Fabbrica.CreatePoint(new Coordinate(lon, lat)) : null;
    }

    /// <summary>LineString GeoJSON → MultiLineString NTS (il tipo delle polilinee dello shapefile); null se non è una linea.</summary>
    public static MultiLineString? Linea(JsonNode? geometria)
    {
        if (geometria?["type"]?.GetValueKind() != JsonValueKind.String || geometria["type"]!.GetValue<string>() != "LineString"
            || geometria["coordinates"] is not JsonArray posizioni)
        {
            return null;
        }

        var punti = new List<Coordinate>();
        foreach (var posizione in posizioni)
        {
            if (posizione is JsonArray { Count: >= 2 } p
                && p[0]?.GetValueKind() == JsonValueKind.Number && p[1]?.GetValueKind() == JsonValueKind.Number)
            {
                punti.Add(new Coordinate(p[0]!.GetValue<double>(), p[1]!.GetValue<double>()));
            }
        }

        return punti.Count >= 2 ? Fabbrica.CreateMultiLineString([Fabbrica.CreateLineString(punti.ToArray())]) : null;
    }

    private static DbfField[] Campi<T>(IReadOnlyList<CampoEsportato<T>> campi) => campi.Select(c => c.Tipo switch
    {
        TipoCampo.Intero => (DbfField)new DbfNumericInt32Field(c.Nome),
        TipoCampo.Decimale => new DbfNumericDecimalField(c.Nome, 18, c.Decimali),
        _ => new DbfCharacterField(c.Nome, LunghezzaTesto),
    }).ToArray();

    /// <summary>Limite del .dbf: 254 byte per campo (in UTF-8 una lettera accentata ne usa due).</summary>
    private const int LunghezzaTesto = 254;

    private static string? Taglia(string? testo)
    {
        if (testo is null || Encoding.UTF8.GetByteCount(testo) <= LunghezzaTesto)
        {
            return testo;
        }

        var lunghezza = testo.Length;
        while (Encoding.UTF8.GetByteCount(testo.AsSpan(0, lunghezza)) > LunghezzaTesto)
        {
            lunghezza--;
        }
        return testo[..lunghezza];
    }

    private static void Aggiungi(ZipArchive archivio, string nome, byte[] contenuto)
    {
        using var voce = archivio.CreateEntry(nome, CompressionLevel.Optimal).Open();
        voce.Write(contenuto);
    }

    /// <summary>Polygon/MultiPolygon GeoJSON → MultiPolygon NTS (anello esterno orario); null se non è un'area.</summary>
    public static MultiPolygon? Poligoni(JsonNode? geometria)
    {
        var tipo = geometria?["type"]?.GetValueKind() == JsonValueKind.String ? geometria["type"]!.GetValue<string>() : null;
        var coordinate = geometria?["coordinates"] as JsonArray;
        IEnumerable<JsonNode?> parti = (tipo, coordinate) switch
        {
            ("Polygon", not null) => new JsonNode?[] { coordinate },
            ("MultiPolygon", not null) => coordinate,
            _ => Array.Empty<JsonNode?>(),
        };

        var poligoni = new List<Polygon>();
        foreach (var parte in parti)
        {
            if (parte is not JsonArray anelli)
            {
                continue;
            }

            var lineari = anelli.Select(Anello).ToList();
            if (lineari.Count == 0 || lineari[0] is not { } esterno)
            {
                continue;
            }

            var poligono = Fabbrica.CreatePolygon(esterno, lineari.Skip(1).OfType<LinearRing>().ToArray());
            poligono.Normalize(); // esterno orario, buchi antiorari
            poligoni.Add(poligono);
        }

        return poligoni.Count == 0 ? null : Fabbrica.CreateMultiPolygon(poligoni.ToArray());
    }

    private static LinearRing? Anello(JsonNode? nodo)
    {
        if (nodo is not JsonArray posizioni)
        {
            return null;
        }

        var punti = new List<Coordinate>();
        foreach (var posizione in posizioni)
        {
            if (posizione is JsonArray { Count: >= 2 } p
                && p[0]?.GetValueKind() == JsonValueKind.Number && p[1]?.GetValueKind() == JsonValueKind.Number)
            {
                var lon = p[0]!.GetValue<double>();
                var lat = p[1]!.GetValue<double>();
                if (double.IsFinite(lon) && double.IsFinite(lat))
                {
                    punti.Add(new Coordinate(lon, lat));
                }
            }
        }

        if (punti.Count > 0 && !punti[0].Equals2D(punti[^1]))
        {
            punti.Add(punti[0].Copy());
        }

        return punti.Count >= 4 ? Fabbrica.CreateLinearRing(punti.ToArray()) : null;
    }
}
