using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>Un punto del grafico, già in coordinate SVG.</summary>
public sealed record PuntoGrafico(double X, double Y, DateOnly Data, decimal Media);

/// <summary>
/// Geometria del piccolo grafico NDVI della scheda (task #84), disegnato a mano in SVG senza librerie:
/// asse orizzontale da gennaio a dicembre (gli anni si sovrappongono per giorno dell'anno), verticale
/// da 0 a 1 (i valori fuori si schiacciano ai bordi).
/// </summary>
public static class GraficoNdvi
{
    public const double Larghezza = 300;
    public const double Altezza = 110;

    /// <summary>Spazio a sinistra per le etichette 0 / 0,5 / 1 e in basso per i mesi.</summary>
    public const double MargineSinistro = 22;
    public const double MargineDestro = 6;
    public const double MargineAlto = 6;
    public const double MargineBasso = 16;

    public static readonly string[] Mesi = ["G", "F", "M", "A", "M", "G", "L", "A", "S", "O", "N", "D"];

    public static double X(DateOnly data)
    {
        var giorni = DateTime.IsLeapYear(data.Year) ? 366 : 365;
        return MargineSinistro + (data.DayOfYear - 1) / (double)(giorni - 1) * (Larghezza - MargineSinistro - MargineDestro);
    }

    public static double Y(decimal valore)
    {
        var v = Math.Clamp((double)valore, 0, 1);
        return Altezza - MargineBasso - v * (Altezza - MargineAlto - MargineBasso);
    }

    /// <summary>X dell'inizio di un mese (1–12), su un anno non bisestile.</summary>
    public static double XMese(int mese) => X(new DateOnly(2025, mese, 1));

    public static IReadOnlyList<PuntoGrafico> Punti(IEnumerable<PuntoNdviDto> serie) =>
        serie.OrderBy(p => p.Data).Select(p => new PuntoGrafico(X(p.Data), Y(p.Media), p.Data, p.Media)).ToList();

    /// <summary>L'attributo points di una polyline: «22.0,50.3 30.1,48.0».</summary>
    public static string Linea(IEnumerable<PuntoGrafico> punti) =>
        string.Join(" ", punti.Select(p => $"{N(p.X)},{N(p.Y)}"));

    public static string N(double valore) => valore.ToString("0.#", CultureInfo.InvariantCulture);
}
