using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>Testi e formati del DSS Bilancio Idrico nelle pagine (task #335). Puro, testato.</summary>
public static class TestiBilancio
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");
    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    /// <summary>Il suggerimento in parole.</summary>
    public static string Suggerimento(string codice) => codice switch
    {
        "IRRIGARE" => "Irrigare",
        "NON_IRRIGARE" => "Non irrigare",
        "N_A" => "Non applicabile",
        _ => "Dato non valido",
    };

    /// <summary>Lettera per la striscia dei prossimi giorni.</summary>
    public static string Lettera(string codice) => codice switch
    {
        "IRRIGARE" => "I",
        "NON_IRRIGARE" => "·",
        "N_A" => "–",
        _ => "?",
    };

    /// <summary>Classe CSS del suggerimento (colore).</summary>
    public static string Classe(string codice) => codice switch
    {
        "IRRIGARE" => "bi-irrigare",
        "NON_IRRIGARE" => "bi-non-irrigare",
        "N_A" => "bi-na",
        _ => "bi-non-valido",
    };

    public static string Mpa(double? v) => v is { } x ? x.ToString("0.00", It) + " MPa" : "—";

    public static string Mm(double? v) => v is { } x ? x.ToString("0.0", It) + " mm" : "—";

    public static string Ore(double? v) => v is { } x ? x.ToString("0.0", It) + " h" : "—";

    public static string Numero(double? v, string formato = "0.00") => v is { } x ? x.ToString(formato, It) : "—";

    public static string Giorno(DateOnly d) => d.ToString("ddd d/M", It);

    /// <summary>«Calcolato il 18/11/2026 alle 04:12» in ora italiana.</summary>
    public static string Calcolato(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FusoItalia).ToString("'il' d/M/yyyy 'alle' HH:mm", It);

    /// <summary>La riga sul calcolo da cui vengono i numeri.</summary>
    public static string Calcolo(CalcoloBilancioDto? c) => c is null
        ? "Non ancora calcolato: il calcolo gira ogni notte alle 4, oppure premi «Ricalcola ora»."
        : c.CalcolatoIlUtc == default
            ? "Nessun calcolo riuscito."
            : $"Calcolato {Calcolato(c.CalcolatoIlUtc)}, stagione dal {c.Da:d/M} al {c.A:d/M/yyyy} · motore {c.VersioneMotore} · parametri {c.VersioneParametri}.";

    /// <summary>
    /// La soglia di oggi, o perché manca: «senza stile» se il vigneto non ha lo stile del vino, «senza fase» se lo stile c'è ma
    /// oggi manca il BBCH (task #340; il perché è nei dati mancanti del vigneto).
    /// </summary>
    public static string Soglia(StatoVignetoBilancioDto s) => s.SogliaMpa is { } x ? Mpa(x) : s.HaStile ? "senza fase" : "senza stile";

    /// <summary>Margine fra Pdlwp e soglia: negativo = sotto la soglia (stress oltre il voluto).</summary>
    public static string Margine(double? pdlwp, double? soglia) =>
        pdlwp is { } p && soglia is { } s ? (p - s >= 0 ? "+" : "") + (p - s).ToString("0.00", It) : "—";

    /// <summary>La criticità in parole (vuota se nessuna).</summary>
    public static string Criticita(StatoVignetoBilancioDto s) =>
        string.Join(", ", new[] { s.CriticitaPdlwp ? "Pdlwp vicino alla soglia" : null, s.CriticitaDeficit ? "acqua sotto la riserva facile" : null }
            .OfType<string>());
}
