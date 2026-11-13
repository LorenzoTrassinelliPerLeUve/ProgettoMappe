using System.Globalization;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>
/// La settimana di Qlik (regole-script.md §1): <c>Week(DayNumberOfYear(data))</c>, cioè il giorno dell'anno di Qlik
/// (sempre su 366 giorni: negli anni non bisestili dal 1/3 un giorno in più) letto nella tabella fissa
/// <c>DOY_WEEK.csv</c> esportata dall'app (giorno 1 → 53, 2–7 → 1, 8 → 2…). Le etichette («46 - 15 Nov») vengono da
/// <c>WEEK_REF.csv</c> (<c>Week_Ref.qvd</c>). Tutte e due sono in docs/specifiche/report-settimanale/dati.
/// </summary>
public static class SettimanaQlik
{
    private static readonly Lazy<(int[] Settimane, Dictionary<int, string> Etichette)> Tabelle = new(Carica);

    /// <summary><c>DayNumberOfYear</c> di Qlik: conta su 366 giorni.</summary>
    public static int GiornoQlik(DateOnly data) =>
        data.DayOfYear + (data.Month > 2 && !DateTime.IsLeapYear(data.Year) ? 1 : 0);

    public static int Di(DateOnly data) => Tabelle.Value.Settimane[GiornoQlik(data)];

    public static int Di(DateTime data) => Di(DateOnly.FromDateTime(data));

    /// <summary>L'etichetta di <c>WEEK_REF</c> («46 - 15 Nov»); il numero se manca.</summary>
    public static string Etichetta(int settimana) =>
        Tabelle.Value.Etichette.TryGetValue(settimana, out var e) ? e : settimana.ToString("00", CultureInfo.InvariantCulture);

    private static (int[], Dictionary<int, string>) Carica()
    {
        var settimane = new int[367];
        foreach (var campi in Righe("DOY_WEEK.csv"))
        {
            settimane[int.Parse(campi[0], CultureInfo.InvariantCulture)] = int.Parse(campi[1], CultureInfo.InvariantCulture);
        }
        var etichette = Righe("WEEK_REF.csv").ToDictionary(c => int.Parse(c[0], CultureInfo.InvariantCulture), c => c[2].Trim());
        return (settimane, etichette);
    }

    private static IEnumerable<string[]> Righe(string nome)
    {
        using var flusso = typeof(SettimanaQlik).Assembly.GetManifestResourceStream($"ProgettoMappe.Web.{nome}")
            ?? throw new InvalidOperationException($"Risorsa {nome} mancante");
        using var lettore = new StreamReader(flusso);
        var righe = lettore.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return righe.Skip(1).Select(r => r.Split(';')).ToList();
    }
}

/// <summary>
/// La settimana scelta nel report settimanale (task #213): anno e settimana di Qlik (<see cref="SettimanaQlik"/>),
/// «AAAA-SS» nell'indirizzo. Il report mostra l'anno fino a quella settimana compresa (<see cref="Contiene"/>): è la
/// lettura dei «RILIEVI FATTI al …» di Qlik e delle linee per settimana; da verificare con la parità (tappa 4).
/// </summary>
public sealed record SettimanaReport(int Anno, int Numero)
{
    public string Codice => $"{Anno}-{Numero:00}";

    /// <summary>L'ultimo giorno dell'anno che cade in questa settimana (o prima): fin qui si leggono i rilievi.</summary>
    public DateOnly A
    {
        get
        {
            var giorno = new DateOnly(Anno, 12, 31);
            while (giorno.Year == Anno && (giorno.DayOfYear == 1 || SettimanaQlik.Di(giorno) > Numero))
            {
                giorno = giorno.AddDays(-1);
            }
            return giorno.Year == Anno ? giorno : new DateOnly(Anno, 1, 1);
        }
    }

    /// <summary>Il rilievo è dell'anno e della settimana scelta o di una prima (il 1° gennaio è settimana 53, come Qlik).</summary>
    public bool Contiene(DateTime data) => data.Year == Anno && SettimanaQlik.Di(data) <= Numero;

    public static SettimanaReport Di(DateOnly giorno) => new(giorno.Year, SettimanaQlik.Di(giorno));

    /// <summary>«2026-46» → settimana; null se il testo non è una settimana 1–53.</summary>
    public static SettimanaReport? DaCodice(string? codice)
    {
        var parti = codice?.Split('-');
        return parti is [var a, var s]
            && int.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var anno) && anno is >= 2000 and <= 2100
            && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero is >= 1 and <= 53
                ? new SettimanaReport(anno, numero)
                : null;
    }

    /// <summary>Le settimane da scegliere: da quella di oggi (come Qlik) indietro fino alla prima dell'anno prima.</summary>
    public static IReadOnlyList<SettimanaReport> Elenco(DateOnly oggi)
    {
        var attuale = Di(oggi);
        var lista = new List<SettimanaReport>();
        for (var n = attuale.Numero; n >= 1; n--)
        {
            lista.Add(new SettimanaReport(oggi.Year, n));
        }
        for (var n = 53; n >= 1; n--)
        {
            lista.Add(new SettimanaReport(oggi.Year - 1, n));
        }
        return lista;
    }

    public string Etichetta(ProgettoMappe.Web.ReportFineStagione.LinguaReport lingua) =>
        TestiSettimanale.Testo(lingua, "settimana.etichetta", SettimanaQlik.Etichetta(Numero), Anno);
}
