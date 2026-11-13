using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>I quattro riquadri di un anno del foglio «Gestione della chioma».</summary>
public sealed record ChiomaAnno(int Anno, double? Sfe, double? SfeDevSt, double? AltezzaCm, double? SpaziVuoti, double? OrdineGermogli);

/// <summary>Un punto del grafico «SFE/produzione per vigneto»: superficie in ettari come grandezza della bolla.</summary>
public sealed record PuntoSfeProduzione(int IdVigneto, double Sfe, double Produzione, double Ettari);

/// <summary>Una riga del foglio 49 «Fattori SFE»: l'ultimo rilievo 117 dell'anno per vigneto e sottovigneto.</summary>
public sealed record FattoriSfe(DateOnly Data, int IdVigneto, int? IdSubVigneto, string? Vitigno, int? EtaImpianto, string? GestioneChioma,
    double? SfePerVite, double? AltezzaChioma, double? LarghezzaChioma, double? SpaziVuoti, double? OrdineGermogli, double? DistanzaPiante);

/// <summary>
/// Fogli 47–49 «Gestione della chioma» e «Fattori SFE» (task #203):
/// <list type="bullet">
/// <item>SFE = <c>Avg({IdParametro='SFE', anno} Avg)</c> di <c>VistaIndicePerParametro</c> (media e dev. st. sulle righe);</item>
/// <item>altezza = <c>avg(aggr(max(AltezzaChioma), IdVigneto))</c>, spazi vuoti = <c>1 - avg(aggr(avg(SpaziVuoti), IdVigneto))</c>,
///   ordine germogli = <c>avg(aggr(avg(FormaAllevamento), IdVigneto))</c>: su tutti i rilievi dell'anno, di qualsiasi tipo;</item>
/// <item>fattori SFE: rilievi 117 dell'ultimo giorno (<c>FlagMaxRilievo117</c>) per vigneto, sottovigneto e anno, raggruppati
///   come le dimensioni della tabella di Qlik (giorno, vigneto, sottovigneto, gestione della chioma).</item>
/// </list>
/// </summary>
public static class CalcoloChiomaReport
{
    public static ChiomaAnno Anno(CampoReportFineStagioneDto campo, int anno)
    {
        var sfe = (campo.Parametri ?? []).Where(p => p.Anno == anno && p.Parametro == "SFE" && p.Valore is not null)
            .Select(p => p.Valore!.Value).ToList();
        var rilievi = (campo.Rilievi ?? []).Where(r => r.Anno == anno).ToList();
        double? MediaVigneti(Func<RilievoFineStagioneDto, double?> campoRilievo, bool massimo)
        {
            var valori = rilievi.GroupBy(r => r.IdVigneto)
                .Select(g => g.Select(campoRilievo).OfType<double>().ToList())
                .Where(v => v.Count > 0).Select(v => massimo ? v.Max() : v.Average()).ToList();
            return valori.Count == 0 ? null : valori.Average();
        }
        var spazi = MediaVigneti(r => r.SpaziVuoti, false);
        return new ChiomaAnno(anno, sfe.Count == 0 ? null : sfe.Average(), DevSt(sfe), MediaVigneti(r => r.AltezzaChioma, true),
            1 - spazi, MediaVigneti(r => r.FormaAllevamento, false));
    }

    /// <summary>Lo scatter «SFE/produzione per vigneto»: solo i vigneti con entrambi i valori nell'anno.</summary>
    public static IReadOnlyList<PuntoSfeProduzione> SfeProduzione(CampoReportFineStagioneDto campo, int anno)
    {
        var parametri = (campo.Parametri ?? []).Where(p => p.Anno == anno && p.Valore is not null).ToList();
        double? Media(int id, string parametro) =>
            parametri.Where(p => p.IdVigneto == id && p.Parametro == parametro).Select(p => p.Valore!.Value).ToList()
                is { Count: > 0 } v ? v.Average() : null;
        return parametri.Select(p => p.IdVigneto).Distinct()
            .Select(id => (id, Sfe: Media(id, "SFE"), Prod: Media(id, "PRODUZIONE")))
            .Where(x => x.Sfe is not null && x.Prod is not null)
            .Select(x => new PuntoSfeProduzione(x.id, x.Sfe!.Value, x.Prod!.Value,
                (campo.Superfici.TryGetValue(x.id, out var s) ? s ?? 0 : 0) / 10_000))
            .ToList();
    }

    public static IReadOnlyList<FattoriSfe> Fattori(CampoReportFineStagioneDto campo, int anno)
    {
        var vigneti = campo.Vigneti.ToDictionary(v => v.IdVigneto);
        var gestioni = campo.GestioniChioma ?? new Dictionary<int, string>();
        var oggi = DateTime.Today.Year;
        return (campo.Rilievi ?? []).Where(r => r.TipoRilievo == 117 && r.Anno == anno)
            .GroupBy(r => (r.IdVigneto, r.IdSubVigneto))
            .SelectMany(g =>
            {
                var ultimo = g.Max(r => r.Data.Date);
                return g.Where(r => r.Data.Date == ultimo);
            })
            .GroupBy(r => (Data: DateOnly.FromDateTime(r.Data), r.IdVigneto, r.IdSubVigneto, r.GestioneChioma))
            .Select(g =>
            {
                vigneti.TryGetValue(g.Key.IdVigneto, out var v);
                double? M(Func<RilievoFineStagioneDto, double?> f) =>
                    g.Select(f).OfType<double>().ToList() is { Count: > 0 } l ? l.Average() : null;
                return new FattoriSfe(g.Key.Data, g.Key.IdVigneto, g.Key.IdSubVigneto, v?.Vitigno,
                    v?.AnnoImpianto is int a ? oggi - a : null,
                    g.Key.GestioneChioma is int gc && gestioni.TryGetValue(gc, out var d) ? d : null,
                    M(r => r.SfePerVite), M(r => r.AltezzaChioma), M(r => r.LarghezzaChioma), M(r => r.SpaziVuoti),
                    M(r => r.FormaAllevamento), M(r => r.DistanzaPiante));
            })
            .OrderBy(f => f.IdVigneto).ThenBy(f => f.IdSubVigneto).ThenBy(f => f.Data)
            .ToList();
    }

    /// <summary>Deviazione standard campionaria, come <c>StDev</c> di Qlik (null con meno di due valori).</summary>
    public static double? DevSt(IReadOnlyList<double> valori)
    {
        if (valori.Count < 2)
        {
            return null;
        }
        var media = valori.Average();
        return Math.Sqrt(valori.Sum(v => (v - media) * (v - media)) / (valori.Count - 1));
    }
}
