using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Una barra del foglio 55 per vigneto (percentuali 0–100).</summary>
public sealed record FallanzeVigneto(int IdVigneto, double? Fallanze, double? InAllevamento, double? Esca);

/// <summary>I KPI del foglio 55: ettari dell'azienda, piante ed ettari mancanti.</summary>
public sealed record FallanzeAzienda(double Ettari, double? PianteMancanti, double? EttariMancanti, IReadOnlyList<FallanzeVigneto> Vigneti);

/// <summary>
/// Foglio 55 «Grafico fall-all-esca» (task #203): fallanze e piante in allevamento dalla stima di produzione
/// (<c>VistaProduzioneGrappoli</c>, media per vigneto nell'anno), danno da esca = <c>avg(PercIntensita)</c> dei rilievi
/// di tipo 9 nell'anno. «% capitozzate» (<c>Rilievo.PercCapitozzata</c>) non è nello snapshot: resta fuori.
/// Piante mancanti di Qlik = <c>(NumeroPianteContate &gt; 0 ? NumeroPianteContate : Superficie / SullaFila / TraFila × 10.000)
/// × PercFallanze / 100</c>; ettari mancanti = <c>PercFallanze × Superficie / 100 / 10.000</c> (Superficie in m²).
/// </summary>
public static class CalcoloFallanzeReport
{
    public static FallanzeAzienda Calcola(CampoReportFineStagioneDto campo, int anno)
    {
        var stime = campo.Produzione.Where(p => p.Stima && p.Anno == anno).GroupBy(p => p.IdVigneto)
            .ToDictionary(g => g.Key, g => (Fallanze: Media(g.Select(p => p.PercFallanze)), Allevamento: Media(g.Select(p => p.PercInAllevamento))));
        var esca = (campo.Rilievi ?? []).Where(r => r.TipoRilievo == 9 && r.Anno == anno).GroupBy(r => r.IdVigneto)
            .ToDictionary(g => g.Key, g => Media(g.Select(r => r.PercIntensita)));

        var vigneti = stime.Keys.Union(esca.Keys)
            .Select(id => new FallanzeVigneto(id, stime.TryGetValue(id, out var s) ? s.Fallanze : null,
                stime.TryGetValue(id, out var s2) ? s2.Allevamento : null, esca.TryGetValue(id, out var e) ? e : null))
            .Where(v => v.Fallanze is not null || v.InAllevamento is not null || v.Esca is not null)
            .ToList();

        double? piante = null, ettari = null;
        foreach (var v in campo.Vigneti)
        {
            if (!stime.TryGetValue(v.IdVigneto, out var s) || s.Fallanze is not { } fallanze)
            {
                continue;
            }
            var totali = v.NumeroPianteContate is > 0 ? v.NumeroPianteContate
                : v.Superficie is { } sup && v.SullaFila is > 0 && v.TraFila is > 0 ? sup / v.SullaFila / v.TraFila * 10_000 : null;
            if (totali is { } t)
            {
                piante = (piante ?? 0) + t * fallanze / 100;
            }
            if (v.Superficie is { } superficie)
            {
                ettari = (ettari ?? 0) + fallanze * superficie / 100 / 10_000;
            }
        }
        return new FallanzeAzienda(campo.Vigneti.Sum(v => v.Superficie ?? 0) / 10_000, piante, ettari, vigneti);
    }

    private static double? Media(IEnumerable<double?> valori) =>
        valori.OfType<double>().ToList() is { Count: > 0 } l ? l.Average() : null;
}
