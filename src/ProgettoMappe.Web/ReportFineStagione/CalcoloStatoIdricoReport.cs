using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.IndiceBigot;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Stato idrico degli apici in un anno: media dei vigneti del loro massimo da maggio a settembre.</summary>
public sealed record StatoIdricoAnno(int Anno, double? Valore, string? Classe);

/// <summary>Una linea del grafico «Andamento deficit idrico»: valori per settimana (negativi, come in Qlik).</summary>
public sealed record LineaStatoIdrico(string Nome, IReadOnlyDictionary<int, double> PerSettimana);

/// <summary>
/// Fogli 38–41 «Valutazione stato idrico apici» (task #203): rilievi di tipo 146, <c>IdValutazioneStatoIdrico</c> 0–4,
/// solo da maggio a settembre (<c>Mese={'Mag'..'Set'}</c>).
/// <list type="bullet">
/// <item>contachilometri dell'anno e dei due prima: <c>avg(aggr(max(IdValutazioneStatoIdrico), IdVigneto))</c>;</item>
/// <item>linee per vigna nell'annata e per anno nello storico: <c>avg(aggr(avg(...), IdVigneto[, IdVigna], settimana[, anno])) * -1</c>.</item>
/// </list>
/// La settimana di Qlik è <c>Week(DayNumberOfYear(InizioRilievo))</c>: il numero del giorno letto come data del 1900
/// (giorno 1 = 31/12/1899), quindi la settimana ISO di quella data. Le etichette di <c>Week_Ref.qvd</c> non sono nel
/// database: qui la settimana si mostra col suo numero.
/// </summary>
public static class CalcoloStatoIdricoReport
{
    public const int Tipo = 146;

    /// <summary>Scala e fasce del contachilometri di Qlik: 0–4, limiti 0,5 / 1,04 / 2,18 / 3,175 / 3,875.</summary>
    public static readonly ScalaQuadrante Scala = new(0, 4,
    [
        new(0, 0.5, "#ffcf02"), new(0.5, 1.04, "#46c646"), new(1.04, 2.18, "#276e27"),
        new(2.18, 3.175, "#276e27"), new(3.175, 3.875, "#276e27"), new(3.875, 4, "#ffcf02"),
    ]);

    /// <summary>Il testo del KPI di Qlik: = 4 molto severo, ≥ 3 severo, ≥ 2 medio, ≥ 1 lieve, ≥ 0 assente.</summary>
    public static string? Classe(double? valore) => valore switch
    {
        null => null,
        >= 4 => "molto-severo",
        >= 3 => "severo",
        >= 2 => "medio",
        >= 1 => "lieve",
        >= 0 => "assente",
        _ => null,
    };

    /// <summary>La settimana di Qlik: <c>Week(DayNumberOfYear(data))</c>.</summary>
    public static int Settimana(DateTime data) => PioggiaCumulata.SettimanaDaGiorno(data.DayOfYear);

    private static IEnumerable<RilievoFineStagioneDto> Validi(IEnumerable<RilievoFineStagioneDto> rilievi) =>
        rilievi.Where(r => r.TipoRilievo == Tipo && r.StatoIdrico is not null && r.Data.Month is >= 5 and <= 9);

    public static IReadOnlyList<StatoIdricoAnno> TreAnni(IReadOnlyList<RilievoFineStagioneDto> rilievi, int annata) =>
        Enumerable.Range(0, 3).Select(i =>
        {
            var anno = annata - i;
            var massimi = Validi(rilievi).Where(r => r.Anno == anno).GroupBy(r => r.IdVigneto)
                .Select(g => (double)g.Max(r => r.StatoIdrico!.Value)).ToList();
            double? media = massimi.Count == 0 ? null : massimi.Average();
            return new StatoIdricoAnno(anno, media, Classe(media));
        }).ToList();

    /// <summary>Foglio 38: una linea per vigna nell'annata.</summary>
    public static IReadOnlyList<LineaStatoIdrico> PerVigna(IReadOnlyList<RilievoFineStagioneDto> rilievi,
        IReadOnlyList<VignetoFineStagioneDto> vigneti, int annata)
    {
        var vigne = vigneti.ToDictionary(v => v.IdVigneto, v => v.Vigna ?? "");
        return Validi(rilievi).Where(r => r.Anno == annata)
            .GroupBy(r => vigne.TryGetValue(r.IdVigneto, out var v) ? v : "")
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new LineaStatoIdrico(g.Key, Linea(g)))
            .ToList();
    }

    /// <summary>Fogli 39–41: una linea per anno, fino all'annata.</summary>
    public static IReadOnlyList<LineaStatoIdrico> Storico(IReadOnlyList<RilievoFineStagioneDto> rilievi, int annata) =>
        Validi(rilievi).Where(r => r.Anno <= annata).GroupBy(r => r.Anno).OrderBy(g => g.Key)
            .Select(g => new LineaStatoIdrico(g.Key.ToString(CultureInfo.InvariantCulture), Linea(g)))
            .ToList();

    private static IReadOnlyDictionary<int, double> Linea(IEnumerable<RilievoFineStagioneDto> righe) =>
        Linea(righe, r => -(double)r.StatoIdrico!.Value);

    private static IReadOnlyDictionary<int, double> Linea(IEnumerable<RilievoFineStagioneDto> righe, Func<RilievoFineStagioneDto, double> valore) =>
        righe.GroupBy(r => Settimana(r.Data))
            .ToDictionary(s => s.Key, s => s.GroupBy(r => r.IdVigneto).Select(v => v.Average(valore)).Average());

    /// <summary>Potenziale idrico pre-alba (tipo 165, solo nel report inglese di Qlik): rilievi con il valore.</summary>
    public const int TipoPreAlba = 165;

    private static IEnumerable<RilievoFineStagioneDto> PreAlba(IEnumerable<RilievoFineStagioneDto> rilievi) =>
        rilievi.Where(r => r.TipoRilievo == TipoPreAlba && r.PotenzialeIdrico is not null);

    /// <summary>Potenziale pre-alba dell'annata: una linea per vigna, media dei vigneti per settimana (MPa, com'è nel dato).</summary>
    public static IReadOnlyList<LineaStatoIdrico> PreAlbaPerVigna(IReadOnlyList<RilievoFineStagioneDto> rilievi,
        IReadOnlyList<VignetoFineStagioneDto> vigneti, int annata)
    {
        var vigne = vigneti.ToDictionary(v => v.IdVigneto, v => v.Vigna ?? "");
        return PreAlba(rilievi).Where(r => r.Anno == annata)
            .GroupBy(r => vigne.TryGetValue(r.IdVigneto, out var v) ? v : "")
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new LineaStatoIdrico(g.Key, Linea(g, r => r.PotenzialeIdrico!.Value)))
            .ToList();
    }

    /// <summary>Potenziale pre-alba storico: una linea per anno.</summary>
    public static IReadOnlyList<LineaStatoIdrico> PreAlbaStorico(IReadOnlyList<RilievoFineStagioneDto> rilievi, int annata) =>
        PreAlba(rilievi).Where(r => r.Anno <= annata).GroupBy(r => r.Anno).OrderBy(g => g.Key)
            .Select(g => new LineaStatoIdrico(g.Key.ToString(CultureInfo.InvariantCulture), Linea(g, r => r.PotenzialeIdrico!.Value)))
            .ToList();
}
