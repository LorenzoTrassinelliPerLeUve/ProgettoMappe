using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;

namespace ProgettoMappe.Api.Esportazione;

/// <summary>
/// KML 2.2 (Google Earth, QGIS): un Placemark per vigneto con geometria, nome = nome del vigneto,
/// attributi in ExtendedData; cartella «Vigneti» e, se ce ne sono, cartella dei punti (vigneti
/// senza poligono) e dei sottovigneti (solo bordo). Geometrie GeoJSON Point/LineString/Polygon e Multi* (Multi* →
/// MultiGeometry). I nomi da 4Grapes passano dall'XmlWriter, che li codifica.
/// </summary>
public static class KmlVigneti
{
    private const string Ns = "http://www.opengis.net/kml/2.2";

    public static byte[] Scrivi(string nomeDocumento, IReadOnlyList<VignetoEsportato> vigneti,
        IReadOnlyList<SottovignetoEsportato>? sottovigneti = null)
    {
        using var flusso = new MemoryStream();
        var impostazioni = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
        using (var xml = XmlWriter.Create(flusso, impostazioni))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("kml", Ns);
            xml.WriteStartElement("Document");
            xml.WriteElementString("name", nomeDocumento);

            // Bordo verde marchio, riempimento semitrasparente (KML: aabbggrr).
            xml.WriteStartElement("Style");
            xml.WriteAttributeString("id", "vigneto");
            xml.WriteStartElement("LineStyle");
            xml.WriteElementString("color", "ff3ec68d");
            xml.WriteElementString("width", "2");
            xml.WriteEndElement();
            xml.WriteStartElement("PolyStyle");
            xml.WriteElementString("color", "553ec68d");
            xml.WriteEndElement();
            xml.WriteEndElement();

            // Punto: l'icona predefinita del programma, tinta di verde (nessun indirizzo esterno).
            xml.WriteStartElement("Style");
            xml.WriteAttributeString("id", "punto");
            xml.WriteStartElement("IconStyle");
            xml.WriteElementString("color", "ff3ec68d");
            xml.WriteEndElement();
            xml.WriteEndElement();

            // Sottovigneto: solo il bordo, sottile (il riempimento è quello del vigneto sotto).
            xml.WriteStartElement("Style");
            xml.WriteAttributeString("id", "sottovigneto");
            xml.WriteStartElement("LineStyle");
            xml.WriteElementString("color", "ff1f7a4f");
            xml.WriteElementString("width", "1");
            xml.WriteEndElement();
            xml.WriteStartElement("PolyStyle");
            xml.WriteElementString("fill", "0");
            xml.WriteEndElement();
            xml.WriteEndElement();

            ScriviCartella(xml, "Vigneti", "#vigneto", vigneti.Where(v => !v.Vigneto.SoloPunto));
            var punti = vigneti.Where(v => v.Vigneto.SoloPunto).ToList();
            if (punti.Count > 0)
            {
                ScriviCartella(xml, "Punti (vigneti senza poligono)", "#punto", punti);
            }

            var conForma = (sottovigneti ?? []).Where(s => EsportazioneVigneti.Geometria(s.Sottovigneto) is not null).ToList();
            if (conForma.Count > 0)
            {
                ScriviCartella(xml, "Sottovigneti", "#sottovigneto", conForma, EsportazioneVigneti.CampiSottovigneti,
                    s => EsportazioneVigneti.Geometria(s.Sottovigneto), s => $"sottovigneto-{s.Sottovigneto.Id}", s => s.Sottovigneto.Nome);
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        return flusso.ToArray();
    }

    /// <summary>Uno strato: una cartella KML con un Placemark per vigneto con geometria.</summary>
    private static void ScriviCartella(XmlWriter xml, string nome, string stile, IEnumerable<VignetoEsportato> vigneti) =>
        ScriviCartella(xml, nome, stile, vigneti, EsportazioneVigneti.Campi,
            v => EsportazioneVigneti.Geometria(v.Vigneto), v => $"vigneto-{v.Vigneto.Id}", v => v.Vigneto.Nome);

    private static void ScriviCartella<T>(XmlWriter xml, string nome, string stile, IEnumerable<T> elementi,
        IReadOnlyList<CampoEsportato<T>> campi, Func<T, JsonNode?> geometriaDi, Func<T, string> id, Func<T, string> nomeDi)
    {
        xml.WriteStartElement("Folder");
        xml.WriteElementString("name", nome);
        foreach (var v in elementi)
        {
            if (geometriaDi(v) is not { } geometria)
            {
                continue;
            }

            xml.WriteStartElement("Placemark");
            xml.WriteAttributeString("id", id(v));
            xml.WriteElementString("name", nomeDi(v));
            xml.WriteElementString("styleUrl", stile);
            xml.WriteStartElement("ExtendedData");
            foreach (var campo in campi)
            {
                xml.WriteStartElement("Data");
                xml.WriteAttributeString("name", campo.Nome);
                xml.WriteElementString("value", EsportazioneVigneti.Testo(campo.Valore(v)) ?? "");
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            ScriviGeometria(xml, geometria);
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static void ScriviGeometria(XmlWriter xml, JsonNode geometria)
    {
        var coordinate = geometria["coordinates"] as JsonArray;
        switch (geometria["type"]?.GetValueKind() == JsonValueKind.String ? geometria["type"]!.GetValue<string>() : null)
        {
            case "Point" when coordinate is not null:
                Punto(xml, coordinate);
                break;
            case "LineString" when coordinate is not null:
                Linea(xml, coordinate);
                break;
            case "Polygon" when coordinate is not null:
                Poligono(xml, coordinate);
                break;
            case "MultiPoint" when coordinate is not null:
                Multi(xml, coordinate, Punto);
                break;
            case "MultiLineString" when coordinate is not null:
                Multi(xml, coordinate, Linea);
                break;
            case "MultiPolygon" when coordinate is not null:
                Multi(xml, coordinate, Poligono);
                break;
            case "GeometryCollection" when geometria["geometries"] is JsonArray parti:
                xml.WriteStartElement("MultiGeometry");
                foreach (var parte in parti)
                {
                    if (parte is not null)
                    {
                        ScriviGeometria(xml, parte);
                    }
                }
                xml.WriteEndElement();
                break;
        }
    }

    private static void Multi(XmlWriter xml, JsonArray parti, Action<XmlWriter, JsonArray> scrivi)
    {
        xml.WriteStartElement("MultiGeometry");
        foreach (var parte in parti)
        {
            if (parte is JsonArray array)
            {
                scrivi(xml, array);
            }
        }
        xml.WriteEndElement();
    }

    private static void Punto(XmlWriter xml, JsonArray posizione)
    {
        xml.WriteStartElement("Point");
        xml.WriteElementString("coordinates", Posizioni([posizione]));
        xml.WriteEndElement();
    }

    private static void Linea(XmlWriter xml, JsonArray posizioni)
    {
        xml.WriteStartElement("LineString");
        xml.WriteElementString("coordinates", Posizioni(posizioni));
        xml.WriteEndElement();
    }

    private static void Poligono(XmlWriter xml, JsonArray anelli)
    {
        xml.WriteStartElement("Polygon");
        for (var i = 0; i < anelli.Count; i++)
        {
            if (anelli[i] is not JsonArray anello)
            {
                continue;
            }

            xml.WriteStartElement(i == 0 ? "outerBoundaryIs" : "innerBoundaryIs");
            xml.WriteStartElement("LinearRing");
            xml.WriteElementString("coordinates", Posizioni(anello));
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    /// <summary>"lon,lat lon,lat …" (KML: longitudine prima), numeri con il punto.</summary>
    private static string Posizioni(IEnumerable<JsonNode?> posizioni)
    {
        var testo = new StringBuilder();
        foreach (var posizione in posizioni)
        {
            if (posizione is JsonArray { Count: >= 2 } p
                && p[0]?.GetValueKind() == JsonValueKind.Number && p[1]?.GetValueKind() == JsonValueKind.Number)
            {
                if (testo.Length > 0)
                {
                    testo.Append(' ');
                }
                testo.Append(p[0]!.GetValue<double>().ToString("R", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(p[1]!.GetValue<double>().ToString("R", CultureInfo.InvariantCulture));
            }
        }

        return testo.ToString();
    }
}
