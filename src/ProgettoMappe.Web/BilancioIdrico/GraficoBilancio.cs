using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>
/// I due grafici della scheda (task #335, punto B), in coordinate SVG: acqua disponibile (AW e AWC, mm) e potenziale
/// fogliare (Pdlwp previsto, soglia, potenziali misurati, MPa). Asse x = giorni. Puro, testato.
/// </summary>
public sealed class GraficoBilancio
{
    public const double Larghezza = 320;
    public const double Altezza = 110;
    public const double Margine = 26;

    /// <summary>Giorni mostrati prima di oggi.</summary>
    public const int GiorniPrima = 60;

    public required DateOnly Da { get; init; }
    public required DateOnly A { get; init; }
    public required DateOnly Oggi { get; init; }
    public double MinY { get; init; }
    public double MaxY { get; init; }

    private static string N(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public double X(DateOnly d) => Margine + (d.DayNumber - Da.DayNumber) * (Larghezza - Margine - 4) / Math.Max(1, A.DayNumber - Da.DayNumber);

    public double Y(double v) => 4 + (MaxY - v) * (Altezza - 18) / (MaxY - MinY == 0 ? 1 : MaxY - MinY);

    /// <summary>Una linea (punti SVG) della grandezza; i giorni senza valore la spezzano in più tratti.</summary>
    public IReadOnlyList<string> Linee(IEnumerable<(DateOnly Data, double? Valore)> punti)
    {
        var tratti = new List<string>();
        var corrente = new List<string>();
        foreach (var (d, v) in punti.Where(p => p.Data >= Da && p.Data <= A).OrderBy(p => p.Data))
        {
            if (v is { } x)
            {
                corrente.Add($"{N(X(d))},{N(Y(x))}");
                continue;
            }
            if (corrente.Count > 1)
                tratti.Add(string.Join(' ', corrente));
            corrente.Clear();
        }
        if (corrente.Count > 1)
            tratti.Add(string.Join(' ', corrente));
        return tratti;
    }

    /// <summary>Tacche dell'asse y (3–5 valori tondi).</summary>
    public IReadOnlyList<double> Tacche()
    {
        var passo = Passo((MaxY - MinY) / 4);
        var tacche = new List<double>();
        for (var v = Math.Ceiling(MinY / passo) * passo; v <= MaxY + 1e-9; v += passo)
            tacche.Add(Math.Round(v, 6));
        return tacche;
    }

    private static double Passo(double grezzo)
    {
        if (grezzo <= 0)
            return 1;
        var potenza = Math.Pow(10, Math.Floor(Math.Log10(grezzo)));
        var m = grezzo / potenza;
        return (m <= 1 ? 1 : m <= 2 ? 2 : m <= 5 ? 5 : 10) * potenza;
    }

    /// <summary>Il periodo del grafico: da <see cref="GiorniPrima"/> giorni prima di oggi (non prima del calcolo) alla fine.</summary>
    public static (DateOnly Da, DateOnly A) Periodo(BilancioVignetoDto b)
    {
        if (b.Giorni.Count == 0)
            return (b.Oggi.AddDays(-GiorniPrima), b.Oggi);
        var primo = b.Giorni[0].Data;
        var ultimo = b.Giorni[^1].Data;
        var da = b.Oggi.AddDays(-GiorniPrima);
        return (da < primo ? primo : da, ultimo);
    }

    /// <summary>Grafico dell'acqua: da 0 al massimo di AWC e AW.</summary>
    public static GraficoBilancio Acqua(BilancioVignetoDto b)
    {
        var (da, a) = Periodo(b);
        var giorni = b.Giorni.Where(g => g.Data >= da && g.Data <= a).ToList();
        var max = giorni.Select(g => Math.Max(g.AwcMm ?? 0, g.AwMm)).DefaultIfEmpty(0).Max();
        return new GraficoBilancio { Da = da, A = a, Oggi = b.Oggi, MinY = 0, MaxY = max <= 0 ? 10 : max * 1.05 };
    }

    /// <summary>Grafico del potenziale: da 0 al minimo di Pdlwp, soglia e misurati (negativi).</summary>
    public static GraficoBilancio Potenziale(BilancioVignetoDto b)
    {
        var (da, a) = Periodo(b);
        var giorni = b.Giorni.Where(g => g.Data >= da && g.Data <= a).ToList();
        var min = giorni.SelectMany(g => new[] { g.PdlwpMpa, g.SogliaMpa }).OfType<double>()
            .Concat(b.Misurati.Where(m => m.Data >= da && m.Data <= a).Select(m => m.Mpa)).DefaultIfEmpty(-1).Min();
        return new GraficoBilancio { Da = da, A = a, Oggi = b.Oggi, MinY = Math.Min(-0.2, min * 1.1), MaxY = 0 };
    }
}
