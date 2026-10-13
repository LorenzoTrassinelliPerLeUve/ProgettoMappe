using System.Data;
using System.Globalization;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Letture di colonna che non lanciano eccezioni per i dati: NULL, tipo inatteso o valore non
/// convertibile diventano null (il chiamante sceglie il valore di riserva o salta la riga).
/// Così una sola riga sporca di 4Grapes non manda in errore un intero elenco.
/// Una colonna inesistente resta un errore: è un errore della query, non dei dati.
/// </summary>
public static class LetturaSicura
{
    public static object? Valore(this IDataRecord riga, string colonna)
    {
        var indice = riga.GetOrdinal(colonna);
        return riga.IsDBNull(indice) ? null : riga.GetValue(indice);
    }

    public static int? Intero(this IDataRecord riga, string colonna) => riga.Valore(colonna) switch
    {
        int n => n,
        short n => n,
        byte n => n,
        long n when n is >= int.MinValue and <= int.MaxValue => (int)n,
        decimal d when d == decimal.Truncate(d) && d is >= int.MinValue and <= int.MaxValue => (int)d,
        string s when int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
        _ => null,
    };

    public static decimal? Decimale(this IDataRecord riga, string colonna) => riga.Valore(colonna) switch
    {
        decimal d => d,
        int n => n,
        short n => n,
        byte n => n,
        long n => n,
        double d when double.IsFinite(d) && Math.Abs(d) < 7.9e28 => (decimal)d,
        float f when float.IsFinite(f) && Math.Abs(f) < 7.9e28f => (decimal)f,
        string s when decimal.TryParse(s.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
        _ => null,
    };

    public static double? Reale(this IDataRecord riga, string colonna) => riga.Valore(colonna) switch
    {
        double d when double.IsFinite(d) => d,
        float f when float.IsFinite(f) => f,
        decimal d => (double)d,
        int n => n,
        long n => n,
        _ => null,
    };

    /// <summary>Testo ripulito dagli spazi ai bordi; vuoto o solo spazi → null.</summary>
    public static string? Testo(this IDataRecord riga, string colonna)
    {
        var testo = riga.Valore(colonna) switch
        {
            null => null,
            string s => s,
            var altro => Convert.ToString(altro, CultureInfo.InvariantCulture),
        };
        return string.IsNullOrWhiteSpace(testo) ? null : testo.Trim();
    }

    public static DateOnly? Data(this IDataRecord riga, string colonna) => riga.Valore(colonna) switch
    {
        DateTime d => DateOnly.FromDateTime(d),
        DateTimeOffset d => DateOnly.FromDateTime(d.DateTime),
        DateOnly d => d,
        _ => null,
    };
}
