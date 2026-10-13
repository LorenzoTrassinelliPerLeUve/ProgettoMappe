using System.Globalization;

namespace ProgettoMappe.Web.Formati;

/// <summary>Distanza dal vigneto più vicino («Dove sono»): approssimata, come si dice a voce.</summary>
public static class FormatoDistanza
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Sotto 1 km a 10 m (minimo 10 m), poi in km con un decimale: 118 → «120 m», 1240 → «1,2 km».</summary>
    public static string Testo(double metri)
    {
        var arrotondati = Math.Max(10, Math.Round(metri / 10, MidpointRounding.AwayFromZero) * 10);
        return arrotondati < 1000
            ? arrotondati.ToString("0", Italiano) + " m"
            : (metri / 1000).ToString(metri < 10_000 ? "#,##0.#" : "#,##0", Italiano) + " km";
    }

    /// <summary>«Sei a 120 m da Chardonnay Nemška Vas».</summary>
    public static string Messaggio(double metri, string vigneto) => $"Sei a {Testo(metri)} da {vigneto}";
}
