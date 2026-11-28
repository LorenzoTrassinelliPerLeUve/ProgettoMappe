using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportMetaStagione;

/// <summary>I dati del report metà stagione già tagliati (<see cref="TaglioMetaStagione"/>): quelli che si congelano alla pubblicazione.</summary>
public sealed record DatiMetaStagione(TaglioReport Taglio, MeteoReportFineStagioneDto Meteo, CampoReportFineStagioneDto? Campo)
{
    public int Annata => Taglio.Fino.Year;

    /// <summary>Legge e taglia i dati dell'Api; null se il meteo dell'azienda non c'è.</summary>
    public static async Task<DatiMetaStagione?> LeggiAsync(Services.VignetiApiClient api, int azienda, int annata, DateOnly? fino,
        DateOnly oggi, CancellationToken ct = default)
    {
        if (await api.GetMeteoReportFineStagioneAsync(azienda, ct) is not { } meteo)
            return null;
        var campo = await api.GetCampoReportFineStagioneAsync(azienda, ct);
        return Taglia(meteo, campo, annata, fino, oggi);
    }

    public static DatiMetaStagione Taglia(MeteoReportFineStagioneDto meteo, CampoReportFineStagioneDto? campo, int annata, DateOnly? fino,
        DateOnly oggi)
    {
        var taglio = TaglioMetaStagione.Scegli(campo, annata, fino, oggi);
        return new DatiMetaStagione(taglio, TaglioMetaStagione.Meteo(meteo, taglio.Fino), TaglioMetaStagione.Campo(campo, taglio.Fino));
    }
}

/// <summary>
/// PowerPoint del report metà stagione (task #420): le slide del fine stagione (<see cref="PowerPointFineStagione"/>) delle sole
/// sezioni del metà stagione, sui dati tagliati, senza le fasi meteo e i grafici giornalieri dopo il taglio; in fondo l'Indice Bigot
/// provvisorio, «Da qui alla vendemmia» (il testo del consulente) e la nota su come si legge il report.
/// </summary>
public static class PowerPointMetaStagione
{
    /// <summary>«{Azienda} - Report metà stagione {annata} - {AAAA-MM-GG HHmm}.pptx» (in inglese «Mid-season report»).</summary>
    public static string NomeFile(string azienda, int annata, LinguaReport lingua, DateTimeOffset adesso, string estensione = ".pptx") =>
        Path.ChangeExtension(NomeFileReport.Crea(azienda,
            $"{(lingua == LinguaReport.En ? "Mid-season report" : "Report metà stagione")} {annata.ToString(CultureInfo.InvariantCulture)}", adesso), estensione);

    /// <summary>Riga «Dati fino al …» con l'origine della data, come nella pagina.</summary>
    public static string RigaTaglio(TaglioReport taglio, LinguaReport lingua) =>
        $"{TestiReport.Testo(lingua, "meta.fino", taglio.Fino.ToString("d", TestiReport.Cultura(lingua)))} " +
        $"({TestiReport.Testo(lingua, $"meta.origine.{taglio.Origine}", taglio.Fino.Year)})";

    /// <summary>Vero se la slide del fine stagione entra nel metà stagione con questo taglio.</summary>
    public static bool SlideAmmessa(string slide, DateOnly fino)
    {
        if (slide.StartsWith("ib-", StringComparison.Ordinal))
            return false;
        var trattino = slide.LastIndexOf('-');
        if (trattino < 0 || !int.TryParse(slide[(trattino + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var n))
            return true;
        return slide[..trattino] switch
        {
            "termo" or "termico" or "freddi" or "caldi" or "pioggia" => n <= TaglioMetaStagione.UltimaFase(fino),
            // Grafici giornalieri: marzo–aprile, maggio–giugno, luglio–agosto, settembre–ottobre.
            "giorno" => 1 + 2 * n <= fino.Month,
            _ => true,
        };
    }

    public static byte[] Crea(string azienda, DatiMetaStagione dati, LinguaReport lingua, DateTimeOffset adesso, ScelteApplicate scelte)
    {
        string T(string chiave, params object[] valori) => TestiReport.Testo(lingua, chiave, valori);
        IReadOnlyList<string> Righe(string? testo) => (testo ?? "").Split('\n').Where(r => r.Trim().Length > 0).ToList();
        var annata = dati.Annata;
        var visibili = CatalogoMetaStagione.Sezioni.Where(s => scelte.Visibile(s.Id)).ToList();
        var inFondo = new List<(string, IReadOnlyList<string>)>();
        foreach (var s in visibili.Where(s => s.SezioneFineStagione is null))
        {
            var titolo = $"{s.Numero}. {CatalogoMetaStagione.Titolo(s, lingua, annata)}";
            if (s.Id == CatalogoMetaStagione.IndiceBigot)
                inFondo.Add((titolo, [T("meta.indice.testo"), .. Righe(scelte.Commento(s.Id))]));
            else if (scelte.Commento(s.Id) is { } testo)
                inFondo.Add((titolo, Righe(testo)));
        }
        inFondo.Add((T("meta.note.titolo"), Righe(T("meta.note.testo"))));

        var variante = new PowerPointFineStagione.VarianteReport(
            T("meta.titolo", annata),
            [RigaTaglio(dati.Taglio, lingua), T("meta.confronto", dati.Taglio.Fino.ToString("d", TestiReport.Cultura(lingua)))],
            slide => SlideAmmessa(slide, dati.Taglio.Fino),
            visibili.Select(s => $"{s.Numero}. {CatalogoMetaStagione.Titolo(s, lingua, annata)}").ToList(),
            inFondo);
        return PowerPointFineStagione.Crea(new DatiFineStagione(azienda, annata, dati.Meteo, dati.Campo, null), lingua, adesso,
            CatalogoMetaStagione.ComeFineStagione(scelte), variante);
    }
}
