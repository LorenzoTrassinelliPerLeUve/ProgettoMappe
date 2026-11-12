using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Una riga del foglio 27 di Qlik («Confronto Precipitazioni Storico per periodo»), in mm; null = nessun dato.</summary>
public sealed record PioggiaFase(
    int Fase,
    decimal? Annata,
    decimal? Media10,
    decimal? Media30,
    decimal? Variazione10,
    decimal? VariazionePct10,
    decimal? Variazione30,
    decimal? VariazionePct30);

/// <summary>
/// Le regole meteo dello script Qlik del report fine stagione (task #201) e le misure della sezione
/// «Caratterizzazione dell'annata». Tutte le misure di Qlik usano solo i vigneti con <c>DatiMeteoStorico = 1</c>
/// e non escludono le previsioni (in un'annata chiusa non ce ne sono).
/// </summary>
public static class CalcoloMeteoReport
{
    /// <summary>
    /// Annata agraria: dal 1° novembre al 31 ottobre, col numero dell'anno in cui finisce (come
    /// <c>MeteoGiornaliero.AnnataAgraria</c>, che Mappe non può leggere, e <c>DOY_ANNATA_AGRARIA</c> dello script).
    /// </summary>
    public static int AnnataAgraria(DateOnly data) => data.Month >= 11 ? data.Year + 1 : data.Year;

    /// <summary>
    /// <c>ID_FASE_FENOLOGICA</c> dello script: date fisse, non il BBCH. 1 = periodo invernale (1/11–14/3),
    /// 2 = germogliamento–fioritura (15/3–31/5), 3 = fioritura–invaiatura (giugno e luglio), 4 = invaiatura–vendemmia
    /// (agosto–ottobre).
    /// </summary>
    public static int Fase(DateOnly data) => data.Month switch
    {
        11 or 12 or 1 or 2 => 1,
        3 => data.Day <= 14 ? 1 : 2,
        4 or 5 => 2,
        8 or 9 or 10 => 4,
        _ => 3,
    };

    /// <summary>Le annate con almeno un giorno di meteo nei vigneti con meteo storico, dalla più recente.</summary>
    public static IReadOnlyList<int> Annate(MeteoReportFineStagioneDto meteo)
    {
        var storici = VignetiStorici(meteo);
        return meteo.Giorni.Where(g => storici.Contains(g.IdVigneto)).Select(g => AnnataAgraria(g.Data))
            .Distinct().OrderDescending().ToList();
    }

    /// <summary>I vigneti che entrano nelle misure di Qlik (<c>DatiMeteoStorico = 1</c>).</summary>
    public static HashSet<int> VignetiStorici(MeteoReportFineStagioneDto meteo) =>
        meteo.Vigneti.Where(v => v.DatiMeteoStorico).Select(v => v.Id).ToHashSet();

    /// <summary>
    /// Foglio 27, una riga per fase. Annata = <c>Avg(aggr(Sum(Precipitazione), IdVigneto, Fase))</c> nell'annata;
    /// media N anni = <c>Avg(aggr(Sum(Precipitazione), IdVigneto, AnnataAgraria, Fase))</c> sulle annate da
    /// annata−N ad annata−1; variazione assoluta = annata − media; variazione % = annata / media − 1.
    /// Un vigneto con righe ma senza pioggia conta zero (la Sum di Qlik su soli NULL fa 0).
    /// </summary>
    public static IReadOnlyList<PioggiaFase> PioggiaPerFase(MeteoReportFineStagioneDto meteo, int annata)
    {
        var storici = VignetiStorici(meteo);
        // Somma della pioggia per (fase, vigneto, annata), solo per le annate che servono.
        var somme = meteo.Giorni
            .Where(g => storici.Contains(g.IdVigneto))
            .Select(g => (Giorno: g, Annata: AnnataAgraria(g.Data)))
            .Where(x => x.Annata <= annata && x.Annata >= annata - 30)
            .GroupBy(x => (Fase: Fase(x.Giorno.Data), x.Giorno.IdVigneto, x.Annata))
            .Select(gr => (gr.Key.Fase, gr.Key.Annata, Somma: gr.Sum(x => x.Giorno.Precipitazione ?? 0m)))
            .ToList();

        return Enumerable.Range(1, 4).Select(fase =>
        {
            var diFase = somme.Where(s => s.Fase == fase).ToList();
            var corrente = Media(diFase.Where(s => s.Annata == annata).Select(s => s.Somma));
            var media10 = Media(diFase.Where(s => s.Annata >= annata - 10 && s.Annata <= annata - 1).Select(s => s.Somma));
            var media30 = Media(diFase.Where(s => s.Annata >= annata - 30 && s.Annata <= annata - 1).Select(s => s.Somma));
            return new PioggiaFase(fase, corrente, media10, media30,
                corrente - media10, Percentuale(corrente, media10), corrente - media30, Percentuale(corrente, media30));
        }).ToList();
    }

    private static decimal? Media(IEnumerable<decimal> valori)
    {
        var lista = valori.ToList();
        return lista.Count == 0 ? null : lista.Average();
    }

    private static decimal? Percentuale(decimal? valore, decimal? media) =>
        valore is { } v && media is { } m && m != 0 ? v / m - 1 : null;
}
