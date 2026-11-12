namespace ProgettoMappe.Web.Report;

/// <summary>
/// Nome dei file dei report: «{Azienda} - {titolo} - {AAAA-MM-GG HHmm}.pptx», con data e ora italiane. Con l'ora nel
/// nome due report non hanno mai lo stesso nome (in Qlik il nome fisso dava «il file esiste già» su SharePoint).
/// </summary>
public static class NomeFileReport
{
    private static readonly TimeZoneInfo FusoItalia =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Rome", out var fuso) ? fuso : TimeZoneInfo.Local;

    public static string Crea(string azienda, string titolo, DateTimeOffset adesso, string estensione = "pptx")
    {
        var quando = TimeZoneInfo.ConvertTime(adesso, FusoItalia);
        return $"{Pulisci(azienda)} - {Pulisci(titolo)} - {quando:yyyy-MM-dd HHmm}.{estensione}";
    }

    /// <summary>Toglie i caratteri che Windows e SharePoint non accettano nei nomi dei file e gli spazi doppi.</summary>
    public static string Pulisci(string testo)
    {
        var pulito = new string(testo.Select(c => "\\/:*?\"<>|#%".Contains(c) || char.IsControl(c) ? ' ' : c).ToArray());
        pulito = string.Join(' ', pulito.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('.', ' ');
        return pulito.Length > 0 ? pulito : "Report";
    }
}
