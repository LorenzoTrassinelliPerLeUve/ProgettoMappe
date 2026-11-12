using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportFineStagione;

/// <summary>Foglio 50: produzione totale dell'annata per vitigno, in quintali.</summary>
public sealed record ProduzioneVitigno(string Vitigno, double Quintali);

/// <summary>Fogli 51–53: produzione di un vigneto (quintali/ha e quintali) in un anno, con l'età del vigneto.</summary>
public sealed record ProduzioneVignetoAnno(int IdVigneto, int Anno, double? QuintaliEttaro, double? Quintali, int? Eta);

/// <summary>Foglio 54: produzione reale contro stima da conta grappoli (kg), per vigneto.</summary>
public sealed record ScostamentoStima(int IdVigneto, string Nome, string? Vigna, double? Ettari, double? Reale, double? Stima,
    double? Differenza, double? DifferenzaEttaro, double? PercFallanze, double? PercInAllevamento);

/// <summary>
/// Produzione del report fine stagione (task #201, tappa 3; fogli 50–54, schema S13). Qlik scrive l'anno fisso
/// (<c>{'2026'}</c>, <c>Year(Today())</c>, <c>Max(Anno_ProduzioneGrappoli)</c>): qui è sempre l'annata scelta.
/// kg / 100 = quintali, come in Qlik. Vigneti senza riga dell'anno = esclusi dalle medie (Avg di Qlik).
/// </summary>
public static class CalcoloProduzioneReport
{
    /// <summary>Foglio 50: <c>Sum(ProduzioneRealeVigneto) / 100</c> per vitigno, solo vendemmie dell'annata.</summary>
    public static IReadOnlyList<ProduzioneVitigno> PerVitigno(CampoReportFineStagioneDto campo, int annata)
    {
        var vitigni = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.Vitigno ?? "—");
        return campo.Produzione.Where(p => !p.Stima && p.Anno == annata && p.RealeVigneto is not null)
            .GroupBy(p => vitigni.GetValueOrDefault(p.IdVigneto, "—"))
            .Select(g => new ProduzioneVitigno(g.Key, g.Sum(p => p.RealeVigneto!.Value) / 100))
            .OrderByDescending(p => p.Quintali).ToList();
    }

    /// <summary>Fogli 51–53: per vigneto e anno, <c>Avg(ProduzioneRealeEttaro)/100</c> e <c>Avg(ProduzioneRealeVigneto)/100</c>.</summary>
    public static IReadOnlyList<ProduzioneVignetoAnno> PerVignetoEAnno(CampoReportFineStagioneDto campo)
    {
        var impianto = campo.Vigneti.ToDictionary(v => v.IdVigneto, v => v.AnnoImpianto);
        return campo.Produzione.Where(p => !p.Stima)
            .GroupBy(p => (p.IdVigneto, p.Anno))
            .Select(g => new ProduzioneVignetoAnno(g.Key.IdVigneto, g.Key.Anno,
                Media(g.Select(p => p.RealeEttaro)) / 100, Media(g.Select(p => p.RealeVigneto)) / 100,
                impianto.GetValueOrDefault(g.Key.IdVigneto) is { } a ? g.Key.Anno - a : null))
            .Where(p => p.QuintaliEttaro is not null || p.Quintali is not null)
            .ToList();
    }

    /// <summary>Foglio 54: una riga per vigneto con vendemmia o stima nell'annata; differenza = stima − reale.</summary>
    public static IReadOnlyList<ScostamentoStima> Scostamenti(CampoReportFineStagioneDto campo, int annata) =>
        campo.Vigneti
            .Select(v =>
            {
                var reali = campo.Produzione.Where(p => !p.Stima && p.Anno == annata && p.IdVigneto == v.IdVigneto).ToList();
                var stime = campo.Produzione.Where(p => p.Stima && p.Anno == annata && p.IdVigneto == v.IdVigneto).ToList();
                var reale = Media(reali.Select(p => p.RealeVigneto));
                var stima = Media(stime.Select(p => p.RealeVigneto));
                return new ScostamentoStima(v.IdVigneto, v.Nome, v.Vigna, v.Superficie / 10000, reale, stima, stima - reale,
                    Media(stime.Select(p => p.RealeEttaro)) - Media(reali.Select(p => p.RealeEttaro)),
                    Media(reali.Select(p => p.PercFallanze)), Media(reali.Select(p => p.PercInAllevamento)));
            })
            .Where(s => s.Reale is not null || s.Stima is not null)
            .OrderBy(s => s.Vigna).ThenBy(s => s.Nome).ToList();

    private static double? Media(IEnumerable<double?> valori)
    {
        var lista = valori.OfType<double>().ToList();
        return lista.Count == 0 ? null : lista.Average();
    }
}
