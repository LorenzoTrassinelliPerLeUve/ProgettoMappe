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
/// Shapefile poligonale in uno zip (.shp .shx .dbf .prj e .cpg), WGS84, .dbf in UTF-8.
/// Un vigneto è un poligono: le geometrie GeoJSON Polygon/MultiPolygon diventano MultiPolygon,
/// gli altri tipi (punti, linee) non entrano. Anelli aperti si chiudono, anelli con meno di
/// quattro vertici si scartano; gli anelli esterni vanno in senso orario (regola dello shapefile).
/// </summary>
public static class ShapefileVigneti
{
    /// <summary>WGS84 nel formato .prj di ESRI (lo stesso che scrive QGIS per EPSG:4326).</summary>
    public const string ProiezioneWgs84 =
        "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]]," +
        "PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";

    private static readonly GeometryFactory Fabbrica = new(new PrecisionModel(), 4326);

    public static byte[] Scrivi(string nomeBase, IReadOnlyList<VignetoEsportato> vigneti)
    {
        var shp = new MemoryStream();
        var shx = new MemoryStream();
        var dbf = new MemoryStream();
        var prj = new MemoryStream();

        var opzioni = new ShapefileWriterOptions(ShapeType.Polygon, Campi())
        {
            Encoding = Encoding.UTF8,
            Projection = ProiezioneWgs84,
        };

        using (var scrittore = Shapefile.OpenWrite(shp, shx, dbf, prj, opzioni))
        {
            foreach (var v in vigneti)
            {
                if (Poligoni(EsportazioneVigneti.Geometria(v.Vigneto)) is not { } geometria)
                {
                    continue;
                }

                foreach (var campo in EsportazioneVigneti.Campi)
                {
                    scrittore.Fields[campo.Nome].Value = campo.Tipo == TipoCampo.Testo
                        ? Taglia(campo.Valore(v) as string)
                        : campo.Valore(v);
                }
                scrittore.Geometry = geometria;
                scrittore.Write();
            }
        }

        using var zip = new MemoryStream();
        using (var archivio = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            Aggiungi(archivio, nomeBase + ".shp", shp.ToArray());
            Aggiungi(archivio, nomeBase + ".shx", shx.ToArray());
            Aggiungi(archivio, nomeBase + ".dbf", dbf.ToArray());
            Aggiungi(archivio, nomeBase + ".prj", prj.ToArray());
            Aggiungi(archivio, nomeBase + ".cpg", Encoding.ASCII.GetBytes("UTF-8"));
        }

        return zip.ToArray();
    }

    private static DbfField[] Campi() => EsportazioneVigneti.Campi.Select(c => c.Tipo switch
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
