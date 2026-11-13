using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Una settimana dei fogli 23–24: pioggia cumulata storica e dell'anno, pioggia della settimana (mm).</summary>
public sealed record PioggiaSettimana(int Settimana, double? Storico, double? Anno, double? Settimanale);

/// <summary>
/// Fogli 23 «Confronto precipitazioni storico» (anno solare, <c>Week_Ref</c>) e 24 «… annata agraria»
/// (<c>week(DOY_ANNATA_AGRARIA)</c>, dal 1° novembre) del report fine stagione (task #203), schema S17:
/// <list type="bullet">
/// <item>storico per settimana = somma della pioggia di tutti gli anni e vigneti / anni distinti / vigneti distinti;</item>
/// <item>anno per settimana = somma della pioggia dell'anno / vigneti distinti dell'anno;</item>
/// <item>le linee sono cumulate (<c>RangeSum(Above(...))</c>), le barre no.</item>
/// </list>
/// Solo vigneti con meteo storico. La settimana è <c>Week()</c> di Qlik sul numero del giorno (dell'anno o dell'annata
/// agraria), come le altre settimane del report: così il 1° gennaio non finisce nella settimana 53.
/// </summary>
public static class PioggiaCumulata
{
    public static IReadOnlyList<PioggiaSettimana> Calcola(MeteoReportFineStagioneDto meteo, int annata, bool agraria) =>
        Calcola(meteo, annata, agraria, PrimoGiornoAgrario);

    /// <summary>Il numero del 1° novembre in <c>DOY_ANNATA_AGRARIA</c> (1 = si conta da uno).</summary>
    public const int PrimoGiornoAgrario = 1;

    public static IReadOnlyList<PioggiaSettimana> Calcola(MeteoReportFineStagioneDto meteo, int annata, bool agraria, int primoGiorno)
    {
        // Qlik fa prima aggr(avg(Precipitazione), IdVigneto, Data): un giorno doppio dello stesso vigneto (San Giacomo,
        // vigneto 19433) conta una volta sola, con la media.
        var righe = MeteoPerFase.Righe(meteo).Where(r => r.Pioggia is not null)
            .GroupBy(r => (r.IdVigneto, r.Data))
            .Select(g => g.Count() == 1 ? g.First() : g.First() with { Pioggia = g.Average(r => r.Pioggia!.Value) })
            .ToList();
        if (righe.Count == 0)
        {
            return [];
        }
        int Anno(RigaMeteo r) => agraria ? r.Annata : r.Data.Year;
        int Settimana(RigaMeteo r) => agraria
            ? SettimanaDaGiorno(r.Data.DayNumber - new DateOnly(r.Annata - 1, 11, 1).DayNumber + primoGiorno)
            : SettimanaSolare(r.Data);

        var anni = righe.Select(Anno).Distinct().Count();
        var vigneti = righe.Select(r => r.IdVigneto).Distinct().Count();
        var dellAnno = righe.Where(r => Anno(r) == annata).ToList();
        var vignetiAnno = dellAnno.Select(r => r.IdVigneto).Distinct().Count();

        var storico = righe.GroupBy(Settimana).ToDictionary(g => g.Key, g => (double)g.Sum(r => r.Pioggia!.Value) / anni / vigneti);
        var anno = dellAnno.GroupBy(Settimana).ToDictionary(g => g.Key, g => (double)g.Sum(r => r.Pioggia!.Value) / vignetiAnno);

        double cumulataStorico = 0, cumulataAnno = 0;
        var ultimaSettimanaAnno = anno.Count == 0 ? 0 : anno.Keys.Max();
        return storico.Keys.Union(anno.Keys).Order().Select(s =>
        {
            cumulataStorico += storico.GetValueOrDefault(s);
            double? settimanale = anno.TryGetValue(s, out var v) ? v : null;
            cumulataAnno += settimanale ?? 0;
            return new PioggiaSettimana(s, cumulataStorico, s <= ultimaSettimanaAnno ? cumulataAnno : null, settimanale);
        }).ToList();
    }

    /// <summary>
    /// La settimana del foglio 23 (<c>Week_Ref_MeteoGiornaliero</c>): la settimana ISO della data vera, come dicono le
    /// etichette della verità di Qlik («01 - 04 Gen», «02 - 11 Gen» nel 2026). I primi giorni di gennaio che per l'ISO sono
    /// della settimana 52/53 dell'anno prima restano nella settimana 1, per non finire in fondo all'anno.
    /// </summary>
    public static int SettimanaSolare(DateOnly data)
    {
        var settimana = ISOWeek.GetWeekOfYear(data.ToDateTime(TimeOnly.MinValue));
        return data.Month == 1 && settimana >= 52 ? 1 : settimana;
    }

    /// <summary><c>Week(n)</c> di Qlik sul numero del giorno: la settimana ISO della data n giorni dopo il 30/12/1899.</summary>
    public static int SettimanaDaGiorno(int giorno) => ISOWeek.GetWeekOfYear(new DateTime(1899, 12, 30).AddDays(giorno));
}
