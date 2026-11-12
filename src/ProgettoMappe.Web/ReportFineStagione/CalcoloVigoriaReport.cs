using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Vigoria dell'azienda in un anno: media dei vigneti (ciascuno = media delle sue righe dell'anno).</summary>
public sealed record VigoriaAnno(int Anno, double? Punteggio, string? Classe, double? LegnoPotatura, double? SfePerMetro,
    double? StatoVegetativo, double? GestioneChioma);

/// <summary>
/// Fogli 45–46 «Vigoria» del report fine stagione (task #201, tappa 3), schema S11: ogni KPI è
/// <c>avg(aggr(avg({Anno_Vigoria = anno} campo), IdVigneto))</c>, per l'annata e le due prima (in Qlik
/// <c>Max(Anno_Vigoria)</c>, <c>−1</c>, <c>−2</c>); classe = «Valutazione Vigoria» di Qlik (S02).
/// </summary>
public static class CalcoloVigoriaReport
{
    /// <summary>Scala e fasce del contachilometri «Vigoria media» di Qlik: 0–10, limiti 0,86 / 2,24 / 3,325 / 5,229 / 6,7.</summary>
    public static readonly ScalaQuadrante Scala = new(0, 10,
    [
        new(0, 0.86, "#f8981d"), new(0.86, 2.24, "#ffcf02"), new(2.24, 3.325, "#46c646"),
        new(3.325, 5.229, "#276e27"), new(5.229, 6.7, "#ffcf02"), new(6.7, 10, "#f8981d"),
    ]);

    /// <summary>Le soglie di Qlik dall'alto: = 10 eccessiva, ≥ 8,8 … ≥ 1 molto bassa; sotto 1 nessuna classe.</summary>
    public static string? Classe(double? punteggio) => punteggio switch
    {
        null => null,
        10 => "eccessiva",
        >= 8.8 => "molto-alta",
        >= 7.5 => "alta",
        >= 6.2 => "medio-alta",
        >= 4.9 => "media",
        >= 3.6 => "medio-bassa",
        >= 2.3 => "bassa",
        >= 1 => "molto-bassa",
        _ => null,
    };

    /// <summary>L'annata e le due prima, dalla più recente.</summary>
    public static IReadOnlyList<VigoriaAnno> TreAnni(IReadOnlyList<VigoriaDto> righe, int annata) =>
        Enumerable.Range(0, 3).Select(i => Anno(righe, annata - i)).ToList();

    public static VigoriaAnno Anno(IReadOnlyList<VigoriaDto> righe, int anno)
    {
        var dellAnno = righe.Where(r => r.Anno == anno).ToList();
        double? Kpi(Func<VigoriaDto, double?> campo) => MediaDiMedie(dellAnno, campo);
        var punteggio = Kpi(r => r.Punteggio);
        return new VigoriaAnno(anno, punteggio, Classe(punteggio), Kpi(r => r.LegnoPotatura), Kpi(r => r.SfePerMetro),
            Kpi(r => r.StatoVegetativo), Kpi(r => r.GestioneChioma));
    }

    /// <summary>Foglio 46: punteggio di ogni vigneto per anno (media delle sue righe).</summary>
    public static IReadOnlyDictionary<(int IdVigneto, int Anno), double> PerVignetoEAnno(IReadOnlyList<VigoriaDto> righe) =>
        righe.Where(r => r.Punteggio is not null).GroupBy(r => (r.IdVigneto, r.Anno))
            .ToDictionary(g => g.Key, g => g.Average(r => r.Punteggio!.Value));

    private static double? MediaDiMedie(IReadOnlyList<VigoriaDto> righe, Func<VigoriaDto, double?> campo)
    {
        var medie = righe.GroupBy(r => r.IdVigneto)
            .Select(g => g.Select(campo).OfType<double>().ToList())
            .Where(v => v.Count > 0).Select(v => v.Average()).ToList();
        return medie.Count == 0 ? null : medie.Average();
    }
}
