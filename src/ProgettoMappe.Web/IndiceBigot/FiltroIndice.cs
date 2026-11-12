using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>I filtri della pagina «Indice Bigot»: vigna, vitigno, vigneto, sottovigneto (null = tutti).</summary>
public sealed record FiltroIndice(int? Vigna = null, int? Vitigno = null, int? Vigneto = null, int? Sottovigneto = null)
{
    /// <summary>I vigneti dell'azienda che passano vigna, vitigno e vigneto.</summary>
    public IReadOnlyList<VignetoReportDto> Vigneti(IndiceBigotReportDto report) =>
        report.Vigneti
            .Where(v => Vigna is null || v.IdVigna == Vigna)
            .Where(v => Vitigno is null || v.IdVitigno == Vitigno)
            .Where(v => Vigneto is null || v.Id == Vigneto)
            .ToList();

    /// <summary>
    /// Le righe da usare: con un sottovigneto scelto quelle della vista _Sub di quel sottovigneto,
    /// altrimenti quelle per vigneto (senza sottovigneto) dei vigneti filtrati.
    /// </summary>
    public IReadOnlyList<RigaIndiceParametroDto> Righe(IndiceBigotReportDto report)
    {
        if (Sottovigneto is { } sub)
        {
            return report.Righe.Where(r => r.IdSubVigneto == sub).ToList();
        }

        var vigneti = Vigneti(report).Select(v => v.Id).ToHashSet();
        return report.Righe.Where(r => r.IdSubVigneto is null && vigneti.Contains(r.IdVigneto)).ToList();
    }

    /// <summary>Superficie grezza per vigneto (peso della media), come in Qlik.</summary>
    public static IReadOnlyDictionary<int, double?> Superfici(IndiceBigotReportDto report) =>
        report.Vigneti.ToDictionary(v => v.Id, v => v.Superficie);
}
