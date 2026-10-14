namespace ProgettoMappe.Api.Filari;

/// <summary>
/// Proiezione UTM su WGS84 (formule di Snyder, USGS Professional Paper 1395): metri sul piano, così
/// interfila e lunghezze dei filari sono distanze vere. La zona si sceglie dal punto di riferimento
/// (per un vigneto: il centro del poligono); errore sub-millimetrico a pochi gradi dal meridiano centrale.
/// </summary>
public sealed class ProiezioneUtm
{
    private const double A = 6378137.0;
    private const double F = 1 / 298.257223563;
    private const double K0 = 0.9996;
    private const double FalsoEst = 500000.0;
    private const double FalsoNordSud = 10000000.0;

    private static readonly double E2 = F * (2 - F);
    private static readonly double Ep2 = E2 / (1 - E2);
    private static readonly double E4 = E2 * E2;
    private static readonly double E6 = E4 * E2;
    private static readonly double E1 = (1 - Math.Sqrt(1 - E2)) / (1 + Math.Sqrt(1 - E2));

    private readonly double _lon0;

    public ProiezioneUtm(int zona, bool sud = false)
    {
        if (zona is < 1 or > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(zona), "La zona UTM va da 1 a 60.");
        }

        Zona = zona;
        Sud = sud;
        _lon0 = Radianti((zona - 1) * 6 - 180 + 3);
    }

    public int Zona { get; }

    public bool Sud { get; }

    /// <summary>La proiezione della zona che contiene il punto (lon, lat in gradi).</summary>
    public static ProiezioneUtm Per(double lon, double lat) => new(ZonaDi(lon), lat < 0);

    /// <summary>Zona UTM della longitudine (1–60), senza le eccezioni di Norvegia e Svalbard.</summary>
    public static int ZonaDi(double lon) => Math.Clamp((int)Math.Floor((lon + 180) / 6) + 1, 1, 60);

    /// <summary>(lon, lat) in gradi → (est, nord) in metri.</summary>
    public (double Est, double Nord) Avanti(double lon, double lat)
    {
        var phi = Radianti(lat);
        var seno = Math.Sin(phi);
        var coseno = Math.Cos(phi);
        var n = A / Math.Sqrt(1 - E2 * seno * seno);
        var t = Math.Tan(phi) * Math.Tan(phi);
        var c = Ep2 * coseno * coseno;
        var a = coseno * (Radianti(lon) - _lon0);
        var m = Meridiano(phi);

        var est = K0 * n * (a + (1 - t + c) * Math.Pow(a, 3) / 6
            + (5 - 18 * t + t * t + 72 * c - 58 * Ep2) * Math.Pow(a, 5) / 120) + FalsoEst;
        var nord = K0 * (m + n * Math.Tan(phi) * (a * a / 2
            + (5 - t + 9 * c + 4 * c * c) * Math.Pow(a, 4) / 24
            + (61 - 58 * t + t * t + 600 * c - 330 * Ep2) * Math.Pow(a, 6) / 720));
        return (est, Sud ? nord + FalsoNordSud : nord);
    }

    /// <summary>(est, nord) in metri → (lon, lat) in gradi.</summary>
    public (double Lon, double Lat) Indietro(double est, double nord)
    {
        var x = est - FalsoEst;
        var y = Sud ? nord - FalsoNordSud : nord;
        var mu = y / K0 / (A * (1 - E2 / 4 - 3 * E4 / 64 - 5 * E6 / 256));
        var phi1 = mu
            + (3 * E1 / 2 - 27 * Math.Pow(E1, 3) / 32) * Math.Sin(2 * mu)
            + (21 * E1 * E1 / 16 - 55 * Math.Pow(E1, 4) / 32) * Math.Sin(4 * mu)
            + 151 * Math.Pow(E1, 3) / 96 * Math.Sin(6 * mu)
            + 1097 * Math.Pow(E1, 4) / 512 * Math.Sin(8 * mu);

        var seno = Math.Sin(phi1);
        var coseno = Math.Cos(phi1);
        var c1 = Ep2 * coseno * coseno;
        var t1 = Math.Tan(phi1) * Math.Tan(phi1);
        var n1 = A / Math.Sqrt(1 - E2 * seno * seno);
        var r1 = A * (1 - E2) / Math.Pow(1 - E2 * seno * seno, 1.5);
        var d = x / (n1 * K0);

        var lat = phi1 - n1 * Math.Tan(phi1) / r1 * (d * d / 2
            - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * Ep2) * Math.Pow(d, 4) / 24
            + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * Ep2 - 3 * c1 * c1) * Math.Pow(d, 6) / 720);
        var lon = _lon0 + (d - (1 + 2 * t1 + c1) * Math.Pow(d, 3) / 6
            + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * Ep2 + 24 * t1 * t1) * Math.Pow(d, 5) / 120) / coseno;
        return (Gradi(lon), Gradi(lat));
    }

    private static double Meridiano(double phi) =>
        A * ((1 - E2 / 4 - 3 * E4 / 64 - 5 * E6 / 256) * phi
             - (3 * E2 / 8 + 3 * E4 / 32 + 45 * E6 / 1024) * Math.Sin(2 * phi)
             + (15 * E4 / 256 + 45 * E6 / 1024) * Math.Sin(4 * phi)
             - 35 * E6 / 3072 * Math.Sin(6 * phi));

    private static double Radianti(double gradi) => gradi * Math.PI / 180;

    private static double Gradi(double radianti) => radianti * 180 / Math.PI;
}
