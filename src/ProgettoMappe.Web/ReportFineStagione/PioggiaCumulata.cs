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

        // I count(distinct Anno) e count(distinct IdVigneto) di Qlik si calcolano dentro la dimensione del grafico, cioè per
        // settimana: una settimana in cui un vigneto non ha dati divide per i vigneti che li hanno (parità del 13/11).
        var dellAnno = righe.Where(r => Anno(r) == annata).ToList();
        var storico = righe.GroupBy(Settimana).ToDictionary(g => g.Key, g => (double)g.Sum(r => r.Pioggia!.Value)
            / g.Select(Anno).Distinct().Count() / g.Select(r => r.IdVigneto).Distinct().Count());
        var anno = dellAnno.GroupBy(Settimana).ToDictionary(g => g.Key, g => (double)g.Sum(r => r.Pioggia!.Value)
            / g.Select(r => r.IdVigneto).Distinct().Count());

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
    /// La settimana del foglio 23 (<c>Week_Ref_MeteoGiornaliero</c>): la settimana della data vera, domenica–sabato (vedi
    /// <see cref="SpostamentoSettimana"/>). I primi giorni di gennaio che finirebbero nella settimana 52/53 dell'anno prima
    /// restano nella settimana 1, per non finire in fondo all'anno.
    /// </summary>
    public static int SettimanaSolare(DateOnly data)
    {
        var settimana = ISOWeek.GetWeekOfYear(data.ToDateTime(TimeOnly.MinValue).AddDays(SpostamentoSettimana));
        return data.Month == 1 && settimana >= 52 ? 1 : settimana;
    }

    /// <summary>
    /// La settimana di <c>Week_Ref</c> di Qlik va da domenica a sabato: la domenica è della settimana dopo (+1 giorno sulla
    /// settimana ISO). Trovato con la parità del 13/11 (con le settimane ISO tornavano molti meno numeri).
    /// </summary>
    public const int SpostamentoSettimana = 1;

    /// <summary><c>Week(n)</c> di Qlik sul numero del giorno: la settimana ISO della data n giorni dopo il 30/12/1899.</summary>
    public static int SettimanaDaGiorno(int giorno) =>
        ISOWeek.GetWeekOfYear(new DateTime(1899, 12, 30).AddDays(giorno + SpostamentoSettimana));
}
