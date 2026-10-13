using System.Globalization;

namespace ProgettoMappe.Web.Formati;

/// <summary>Superficie del vigneto come la legge l'utente nella scheda: all'italiana, in ettari.</summary>
public static class FormatoSuperficie
{
    public const string NonRegistrata = "superficie non registrata in 4Grapes";

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>1234.5 → «1.234,5 ha», 0.35 → «0,35 ha» (al massimo due decimali); null → <see cref="NonRegistrata"/>.</summary>
    public static string Testo(decimal? ettari) =>
        ettari is { } valore ? valore.ToString("#,##0.##", Italiano) + " ha" : NonRegistrata;
}
