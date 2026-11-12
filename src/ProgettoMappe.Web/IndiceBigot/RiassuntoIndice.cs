using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Come raggruppare i vigneti nella tabella riassuntiva.</summary>
public enum Raggruppamento
{
    Vigna,
    Vitigno,
}

/// <summary>Valore di un parametro in una riga della tabella: numero (per ordinare e per Excel) e testo mostrato.</summary>
public sealed record CellaParametro(double? Numero, string? Testo);

/// <summary>
/// Una riga della tabella per vigna o vitigno (foglio 4 di Qlik). <see cref="Id"/> null = vigneti senza
/// vigna (o senza vitigno). <see cref="Indice"/>: media semplice degli indici dei vigneti con 9 parametri
/// (la formula di Qlik); <see cref="IndicePesato"/>: la stessa media pesata con la superficie;
/// <see cref="IndicePrima"/>: <see cref="Indice"/> dell'anno prima. <see cref="Parametri"/>: per Id del
/// parametro; per «ETA VIGNETO» l'età d'impianto come in Qlik.
/// </summary>
public sealed record RigaRiassunto(
    int? Id,
    string Nome,
    int Vigneti,
    int VignetiCompleti,
    double? Ettari,
    double? Indice,
    double? IndicePesato,
    double? IndicePrima,
    IReadOnlyDictionary<string, CellaParametro> Parametri)
{
    public double? Differenza => Indice is { } a && IndicePrima is { } b ? a - b : null;
}

/// <summary>
/// Un quadrante dei fogli «INDICATORI IB» di Qlik: <see cref="Punteggio"/> (lancetta, scala 0–massimo del
/// parametro: media fra i vigneti con 9 parametri, come l'istogramma) e <see cref="Valore"/> (la misura del
/// quadrante di Qlik, con <see cref="Testo"/> leggibile).
/// </summary>
public sealed record Indicatore(ParametroIndice Parametro, double? Punteggio, double? Valore, string? Testo)
{
    /// <summary>Punteggio in proporzione al massimo, fra 0 e 1 (null senza punteggio).</summary>
    public double? Frazione => Punteggio is { } p ? Math.Clamp(p / Parametro.Massimo, 0, 1) : null;

    /// <summary>
    /// Fascia del quadrante: 0 sotto metà del massimo, 1 fino a tre quarti, 2 sopra. Le fasce dei quadranti di
    /// Qlik non sono leggibili: queste sono provvisorie (il punteggio è sempre «più alto = meglio»).
    /// </summary>
    public int? Fascia => Frazione switch
    {
        null => null,
        < 0.5 => 0,
        < 0.75 => 1,
        _ => 2,
    };
}

/// <summary>
/// Tabella per vigna/vitigno (foglio 4 «IB TABELLARE VIGNA/VITIGNO MAX ANNO») e quadranti (fogli 5–7
/// «INDICATORI IB») dell'app Qlik, task #194. Formule del foglio 4, per gruppo:
/// <list type="bullet">
/// <item>Indice = media semplice degli indici dei vigneti con 9 parametri.</item>
/// <item>Vigoria: media di <c>Avg</c> su <b>tutti</b> i vigneti (in Qlik senza il filtro dei 9 parametri), poi classe.</item>
/// <item>SFE, produzione, SFE/kg, morfologia: media di <c>Avg</c> sui vigneti con 9 parametri; morfologia in classe.</item>
/// <item>Sanità = 100 − media di <c>Max</c> delle ampelopatie; stress idrico = media di <c>Max</c> (in classe);
/// biodiversità = media di <c>Valore</c>; sempre sui vigneti con 9 parametri.</item>
/// <item>Età = anno corrente − <c>AnnoImpianto</c>, media sui vigneti del gruppo (<c>AVG(EtaImpianto)</c>, senza anno).</item>
/// </list>
/// Quadranti: la media fra i vigneti della media del vigneto, su tutti i vigneti (senza il filtro dei 9);
/// stress idrico da <c>Avg</c> (non da <c>Max</c> come nelle tabelle), età = anno dell'indice − <c>AnnoImpianto</c>.
/// </summary>
public static class RiassuntoIndice
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");

    private static bool Di(RigaIndiceParametroDto r, string parametro) =>
        string.Equals(r.IdParametro, parametro, StringComparison.OrdinalIgnoreCase);

    private static double? Media(IEnumerable<double?> valori)
    {
        var presenti = valori.Where(v => v is not null).Select(v => v!.Value).ToList();
        return presenti.Count > 0 ? presenti.Average() : null;
    }

    /// <summary>
    /// Le righe della tabella nell'anno: un gruppo per vigna (o vitigno) dei vigneti che passano il filtro di vigna e
    /// vitigno (il vigneto scelto non conta) e che hanno un indice nell'anno. Ordine: nome.
    /// </summary>
    public static IReadOnlyList<RigaRiassunto> Tabella(IndiceBigotReportDto report, FiltroIndice filtro, Raggruppamento per,
        int anno, int annoCorrente)
    {
        var senzaVigneto = filtro with { Vigneto = null, Sottovigneto = null };
        var vigneti = senzaVigneto.Vigneti(report);
        var righe = senzaVigneto.Righe(report);
        var indici = CalcoloIndiceBigot.Indici(righe).ToDictionary(i => (i.Id, i.Anno));
        var conIndice = vigneti.Where(v => indici.ContainsKey((v.Id, anno))).ToList();
        var nomi = per == Raggruppamento.Vigna
            ? report.Vigne.ToDictionary(v => v.Id, v => v.Nome)
            : report.Vitigni.ToDictionary(v => v.Id, v => v.Nome);
        var righePerVigneto = righe.Where(r => r.Anno == anno).ToLookup(r => r.IdVigneto);

        return conIndice
            .GroupBy(v => per == Raggruppamento.Vigna ? v.IdVigna : v.IdVitigno)
            .Select(g =>
            {
                var membri = g.ToList();
                var completi = membri.Where(v => indici[(v.Id, anno)].Completo).ToList();
                var tutte = membri.SelectMany(v => righePerVigneto[v.Id]).ToList();
                var diCompleti = completi.SelectMany(v => righePerVigneto[v.Id]).ToList();

                var pesati = completi.Where(v => v.Superficie is not null).ToList();
                var sommaSuperfici = pesati.Sum(v => v.Superficie!.Value);
                double? pesato = pesati.Count > 0 && sommaSuperfici != 0
                    ? pesati.Sum(v => indici[(v.Id, anno)].Punteggio * v.Superficie!.Value) / sommaSuperfici
                    : null;
                var superfici = membri.Where(v => v.Superficie is not null).ToList();

                // L'anno prima conta i vigneti del gruppo con 9 parametri in quell'anno (anche se quest'anno sono incompleti).
                var primaTutti = vigneti.Where(v => (per == Raggruppamento.Vigna ? v.IdVigna : v.IdVitigno) == g.Key)
                    .Select(v => indici.GetValueOrDefault((v.Id, anno - 1))).Where(i => i is { Completo: true }).ToList();

                var parametri = new Dictionary<string, CellaParametro>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in CalcoloIndiceBigot.Parametri)
                {
                    if (p.Id == "ETA VIGNETO")
                    {
                        var eta = Media(membri.Select(v => v.AnnoImpianto is { } a ? (double?)(annoCorrente - a) : null));
                        parametri[p.Id] = new CellaParametro(eta, eta?.ToString("0.#", Cultura));
                        continue;
                    }
                    var origine = p.Id == "VIGORIA" ? tutte : diCompleti;
                    var delParametro = origine.Where(r => Di(r, p.Id)).ToList();
                    parametri[p.Id] = new CellaParametro(CalcoloIndiceBigot.ValoreNumerico(p, delParametro), CalcoloIndiceBigot.Valore(p, delParametro));
                }

                return new RigaRiassunto(
                    g.Key,
                    g.Key is { } id ? nomi.GetValueOrDefault(id, $"{(per == Raggruppamento.Vigna ? "Vigna" : "Vitigno")} {id}")
                        : per == Raggruppamento.Vigna ? "Senza vigna" : "Senza vitigno",
                    membri.Count,
                    completi.Count,
                    superfici.Count > 0 ? superfici.Sum(v => v.Superficie!.Value) / 10_000 : null,
                    completi.Count > 0 ? completi.Average(v => indici[(v.Id, anno)].Punteggio) : null,
                    pesato,
                    primaTutti.Count > 0 ? primaTutti.Average(i => i!.Punteggio) : null,
                    parametri);
            })
            .OrderBy(r => r.Id is null)
            .ThenBy(r => r.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// I nove quadranti nell'anno sulle righe date (già filtrate). <paramref name="anniImpianto"/>: AnnoImpianto per
    /// IdVigneto (per l'età).
    /// </summary>
    public static IReadOnlyList<Indicatore> Indicatori(IReadOnlyCollection<RigaIndiceParametroDto> righe,
        IReadOnlyDictionary<int, int?> anniImpianto, int anno)
    {
        var dellAnno = righe.Where(r => r.Anno == anno).ToList();
        var punteggi = CalcoloIndiceBigot.Istogramma(righe, anno).ToDictionary(b => b.Parametro.Id, b => b.Punteggio);

        return CalcoloIndiceBigot.Parametri.Select(p =>
        {
            double? valore;
            if (p.Id == "ETA VIGNETO")
            {
                valore = Media(dellAnno.Select(r => r.IdVigneto).Distinct()
                    .Select(id => anniImpianto.GetValueOrDefault(id) is { } a ? (double?)(anno - a) : null));
            }
            else
            {
                var perVigneto = dellAnno.Where(r => Di(r, p.Id)).GroupBy(r => r.IdSubVigneto ?? r.IdVigneto)
                    .Select(g => p.Id.ToUpperInvariant() switch
                    {
                        "AMPELOPATIE" => Media(g.Select(r => r.Max)) is { } danno ? 100 - danno : null,
                        "BIODIVERSITA" => Media(g.Select(r => r.Valore)),
                        _ => Media(g.Select(r => r.Avg)),
                    });
                valore = Media(perVigneto);
            }
            return new Indicatore(p, punteggi.GetValueOrDefault(p.Id), valore, valore is { } v ? Testo(p, v) : null);
        }).ToList();
    }

    /// <summary>Testo del valore di un quadrante, con l'unità come nei titoli dei quadranti di Qlik.</summary>
    public static string Testo(ParametroIndice parametro, double v) => parametro.Id.ToUpperInvariant() switch
    {
        "SFE" => $"{v.ToString("0.00", Cultura)} m²/vite",
        "SFEKG" => $"{v.ToString("0.00", Cultura)} m²/kg",
        "PRODUZIONE" => $"{v.ToString("0.00", Cultura)} kg/vite",
        "AMPELOPATIE" => $"{v.ToString("0", Cultura)} % sanità",
        "VIGORIA" => $"{v.ToString("0.0", Cultura)} · {CalcoloIndiceBigot.ClasseVigoria(v)}",
        "MORFOLOGIA" => $"{v.ToString("0.0", Cultura)} · {CalcoloIndiceBigot.ClasseMorfologia(v) ?? "–"}",
        "REGIME IDRICO" => $"{v.ToString("0.0", Cultura)} · {CalcoloIndiceBigot.ClasseStressIdrico(v) ?? "–"}",
        "ETA VIGNETO" => $"{v.ToString("0.#", Cultura)} anni",
        _ => v.ToString("0.0", Cultura),
    };
}
