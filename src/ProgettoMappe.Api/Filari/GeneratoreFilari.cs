using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProgettoMappe.Api.Esportazione;

namespace ProgettoMappe.Api.Filari;

/// <summary>Un filare: segmento dritto da (Lon1, Lat1) a (Lon2, Lat2), in gradi WGS84.</summary>
public readonly record struct Filare(double Lon1, double Lat1, double Lon2, double Lat2)
{
    /// <summary>[[lon, lat], [lon, lat]], come lo scambiano Web e mappa.</summary>
    public double[][] ComeArray() => [[Lon1, Lat1], [Lon2, Lat2]];

    /// <summary>Da [[lon, lat], [lon, lat]]; null se non sono due punti con coordinate valide.</summary>
    public static Filare? DaArray(double[][]? punti) =>
        punti is [{ Length: >= 2 } a, { Length: >= 2 } b]
        && Valida(a[0], a[1]) && Valida(b[0], b[1])
            ? new Filare(a[0], a[1], b[0], b[1])
            : null;

    private static bool Valida(double lon, double lat) =>
        double.IsFinite(lon) && double.IsFinite(lat) && lon is >= -180 and <= 180 && lat is >= -85 and <= 85;
}

/// <summary>Filari generati o misurati: elenco, numero, lunghezza totale (m) e direzione (gradi da nord, 0–180).</summary>
public sealed record FilariCalcolati(IReadOnlyList<Filare> Filari, double LunghezzaM, double? AzimutGradi)
{
    public int NumFilari => Filari.Count;
}

/// <summary>
/// Generazione automatica dei filari (task #95): dato il primo filare tracciato dall'utente (due punti),
/// l'interfila e uno scostamento laterale, linee parallele ogni interfila ritagliate dentro il poligono
/// del vigneto. Il calcolo si fa in metri, nella zona UTM del vigneto; i filari tornano in WGS84. Un
/// filare che il poligono taglia in più pezzi (rientranze, buchi) diventa più filari.
/// </summary>
public static class GeneratoreFilari
{
    /// <summary>Pezzi più corti di così (in metri) non sono filari: angoli e sfiori del bordo.</summary>
    public const double LunghezzaMinimaM = 1.0;

    /// <summary>Limite di sicurezza: oltre, l'interfila è sbagliata o il poligono non è un vigneto.</summary>
    public const int MaxFilari = 3000;

    public const double InterfilaMinimaM = 0.5;
    public const double InterfilaMassimaM = 20;
    public const double ScostamentoMassimoM = 50;

    private static readonly GeometryFactory Fabbrica = new();

    /// <summary>
    /// I filari dentro <paramref name="poligonoGeoJson"/> (Polygon o MultiPolygon GeoJSON, WGS84), paralleli
    /// al segmento <paramref name="da"/>→<paramref name="a"/> (lon, lat), a distanza <paramref name="interfilaM"/>
    /// l'uno dall'altro; la griglia passa per il segmento spostato di <paramref name="scostamentoM"/> verso
    /// la sua sinistra. Ordinati da destra a sinistra e, sullo stesso allineamento, nel verso da→a.
    /// </summary>
    /// <exception cref="ArgumentException">Poligono assente, primo filare troppo corto, valori fuori intervallo o troppi filari.</exception>
    public static FilariCalcolati Genera(JsonNode? poligonoGeoJson, (double Lon, double Lat) da, (double Lon, double Lat) a,
        double interfilaM, double scostamentoM)
    {
        if (!double.IsFinite(interfilaM) || interfilaM is < InterfilaMinimaM or > InterfilaMassimaM)
        {
            throw new ArgumentException($"L'interfila va da {InterfilaMinimaM} a {InterfilaMassimaM} m.");
        }

        if (!double.IsFinite(scostamentoM) || Math.Abs(scostamentoM) > ScostamentoMassimoM)
        {
            throw new ArgumentException($"Lo scostamento va da -{ScostamentoMassimoM} a {ScostamentoMassimoM} m.");
        }

        if (ShapefileVigneti.Poligoni(poligonoGeoJson) is not { } poligoni)
        {
            throw new ArgumentException("Il vigneto non ha un poligono.");
        }

        var centro = poligoni.EnvelopeInternal.Centre;
        var utm = ProiezioneUtm.Per(centro.X, centro.Y);
        var area = Valido(Proietta(poligoni, utm));

        var (ax, ay) = utm.Avanti(da.Lon, da.Lat);
        var (bx, by) = utm.Avanti(a.Lon, a.Lat);
        var lunghezza = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        if (!double.IsFinite(lunghezza) || lunghezza < 0.5)
        {
            throw new ArgumentException("Il primo filare è troppo corto: servono due punti distinti.");
        }

        // d = direzione del filare, n = perpendicolare (a sinistra di d), o = origine della griglia.
        var (dx, dy) = ((bx - ax) / lunghezza, (by - ay) / lunghezza);
        var (nx, ny) = (-dy, dx);
        var (ox, oy) = (ax + nx * scostamentoM, ay + ny * scostamentoM);

        double tMin = double.MaxValue, tMax = double.MinValue, sMin = double.MaxValue, sMax = double.MinValue;
        foreach (var c in area.Coordinates)
        {
            var t = (c.X - ox) * nx + (c.Y - oy) * ny;
            var s = (c.X - ox) * dx + (c.Y - oy) * dy;
            (tMin, tMax, sMin, sMax) = (Math.Min(tMin, t), Math.Max(tMax, t), Math.Min(sMin, s), Math.Max(sMax, s));
        }

        var kMin = (long)Math.Ceiling(tMin / interfilaM - 1e-9);
        var kMax = (long)Math.Floor(tMax / interfilaM + 1e-9);
        if (kMax - kMin + 1 > MaxFilari)
        {
            throw new ArgumentException($"Verrebbero più di {MaxFilari} filari: controlla l'interfila.");
        }

        var pezzi = new List<(long K, double S, Coordinate Inizio, Coordinate Fine)>();
        for (var k = kMin; k <= kMax; k++)
        {
            var (px, py) = (ox + nx * k * interfilaM, oy + ny * k * interfilaM);
            var retta = Fabbrica.CreateLineString(
            [
                new Coordinate(px + dx * (sMin - 1), py + dy * (sMin - 1)),
                new Coordinate(px + dx * (sMax + 1), py + dy * (sMax + 1)),
            ]);

            foreach (var linea in Linee(area.Intersection(retta)))
            {
                if (linea.Length < LunghezzaMinimaM)
                {
                    continue;
                }

                // Inizio e fine nel verso del primo filare (l'intersezione può restituirli al contrario).
                var (p, q) = (linea.Coordinates[0], linea.Coordinates[^1]);
                var sp = (p.X - ox) * dx + (p.Y - oy) * dy;
                var sq = (q.X - ox) * dx + (q.Y - oy) * dy;
                pezzi.Add(sp <= sq ? (k, sp, p, q) : (k, sq, q, p));
            }
        }

        var filari = pezzi.OrderBy(p => p.K).ThenBy(p => p.S)
            .Select(p =>
            {
                var (lon1, lat1) = utm.Indietro(p.Inizio.X, p.Inizio.Y);
                var (lon2, lat2) = utm.Indietro(p.Fine.X, p.Fine.Y);
                return new Filare(lon1, lat1, lon2, lat2);
            })
            .ToList();

        var totale = pezzi.Sum(p => p.Inizio.Distance(p.Fine));
        return new FilariCalcolati(filari, totale, Azimut(dx, dy));
    }

    /// <summary>
    /// Numero, lunghezza totale (m, in UTM della zona del loro centro) e direzione prevalente (quella del
    /// filare più lungo) di filari qualsiasi, per esempio ritoccati a mano. Nessun filare: 0 m, nessuna direzione.
    /// </summary>
    public static FilariCalcolati Misura(IReadOnlyList<Filare> filari)
    {
        if (filari.Count == 0)
        {
            return new FilariCalcolati(filari, 0, null);
        }

        var utm = ProiezioneUtm.Per(
            filari.Average(f => (f.Lon1 + f.Lon2) / 2),
            filari.Average(f => (f.Lat1 + f.Lat2) / 2));
        double totale = 0, piuLungo = -1, azimut = 0;
        foreach (var f in filari)
        {
            var (x1, y1) = utm.Avanti(f.Lon1, f.Lat1);
            var (x2, y2) = utm.Avanti(f.Lon2, f.Lat2);
            var l = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            totale += l;
            if (l > piuLungo && l > 0)
            {
                (piuLungo, azimut) = (l, Azimut((x2 - x1) / l, (y2 - y1) / l));
            }
        }

        return new FilariCalcolati(filari, totale, piuLungo > 0 ? azimut : null);
    }

    /// <summary>Lunghezza in metri di un filare (UTM della sua zona).</summary>
    public static double Lunghezza(Filare f)
    {
        var utm = ProiezioneUtm.Per((f.Lon1 + f.Lon2) / 2, (f.Lat1 + f.Lat2) / 2);
        var (x1, y1) = utm.Avanti(f.Lon1, f.Lat1);
        var (x2, y2) = utm.Avanti(f.Lon2, f.Lat2);
        return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }

    /// <summary>MultiLineString WKT in WGS84 («lon lat»), un segmento per filare, 7 decimali (~1 cm).</summary>
    public static string Wkt(IReadOnlyList<Filare> filari)
    {
        if (filari.Count == 0)
        {
            return "MULTILINESTRING EMPTY";
        }

        static string N(double v) => v.ToString("0.#######", CultureInfo.InvariantCulture);
        var testo = new StringBuilder("MULTILINESTRING (");
        for (var i = 0; i < filari.Count; i++)
        {
            var f = filari[i];
            testo.Append(i == 0 ? "(" : ", (")
                .Append(N(f.Lon1)).Append(' ').Append(N(f.Lat1)).Append(", ")
                .Append(N(f.Lon2)).Append(' ').Append(N(f.Lat2)).Append(')');
        }
        return testo.Append(')').ToString();
    }

    /// <summary>I filari di un WKT LineString/MultiLineString (primo e ultimo punto di ogni linea); vuoto se illeggibile.</summary>
    public static IReadOnlyList<Filare> DaWkt(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return [];
        }

        try
        {
            return Linee(new WKTReader().Read(wkt))
                .Where(l => l.NumPoints >= 2)
                .Select(l => new Filare(l.Coordinates[0].X, l.Coordinates[0].Y, l.Coordinates[^1].X, l.Coordinates[^1].Y))
                .ToList();
        }
        catch (ParseException)
        {
            return [];
        }
    }

    /// <summary>Gradi da nord in senso orario, ridotti a 0–180 (un filare non ha verso).</summary>
    private static double Azimut(double dx, double dy)
    {
        var gradi = Math.Atan2(dx, dy) * 180 / Math.PI;
        gradi = ((gradi % 180) + 180) % 180;
        return Math.Round(gradi, 1) >= 180 ? 0 : Math.Round(gradi, 1);
    }

    private static IEnumerable<LineString> Linee(Geometry geometria)
    {
        switch (geometria)
        {
            case LineString linea when !linea.IsEmpty:
                yield return linea;
                break;
            case GeometryCollection collezione:
                for (var i = 0; i < collezione.NumGeometries; i++)
                {
                    foreach (var linea in Linee(collezione.GetGeometryN(i)))
                    {
                        yield return linea;
                    }
                }
                break;
        }
    }

    private static Geometry Proietta(Geometry geometria, ProiezioneUtm utm)
    {
        var copia = geometria.Copy();
        copia.Apply(new Proiettore(utm));
        copia.GeometryChanged();
        return copia;
    }

    private static Geometry Valido(Geometry geometria) => geometria.IsValid ? geometria : geometria.Buffer(0);

    private sealed class Proiettore(ProiezioneUtm utm) : ICoordinateSequenceFilter
    {
        public bool Done => false;

        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence sequenza, int i)
        {
            var (x, y) = utm.Avanti(sequenza.GetX(i), sequenza.GetY(i));
            sequenza.SetX(i, x);
            sequenza.SetY(i, y);
        }
    }
}
