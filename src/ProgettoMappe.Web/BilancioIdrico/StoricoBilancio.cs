using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>
/// Task #386: il bilancio del vigneto giorno per giorno per tutta la stagione. Il giorno scelto (selettore, clic sul grafico,
/// riga della tabella) sostituisce «oggi» in cima alla sezione; la tabella dell'anno si sfoglia per mese.
/// </summary>
public static class StoricoBilancio
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>La riga di oggi: l'ultima fino a oggi, altrimenti la prima.</summary>
    public static GiornoBilancioDto? Oggi(BilancioVignetoDto b) =>
        b.Giorni.Count == 0 ? null : b.Giorni.LastOrDefault(g => g.Data <= b.Oggi) ?? b.Giorni[0];

    /// <summary>La riga del giorno scelto; senza scelta o fuori dalla stagione calcolata, quella di oggi.</summary>
    public static GiornoBilancioDto? DelGiorno(BilancioVignetoDto b, DateOnly? scelto) =>
        scelto is { } d && b.Giorni.FirstOrDefault(g => g.Data == d) is { } g ? g : Oggi(b);

    /// <summary>I mesi della stagione calcolata, in ordine (primo giorno del mese).</summary>
    public static IReadOnlyList<DateOnly> Mesi(BilancioVignetoDto b) =>
        b.Giorni.Select(g => new DateOnly(g.Data.Year, g.Data.Month, 1)).Distinct().Order().ToList();

    /// <summary>I giorni di un mese (primo giorno del mese in <paramref name="mese"/>).</summary>
    public static IReadOnlyList<GiornoBilancioDto> DelMese(BilancioVignetoDto b, DateOnly mese) =>
        b.Giorni.Where(g => g.Data.Year == mese.Year && g.Data.Month == mese.Month).ToList();

    public static DateOnly Mese(DateOnly d) => new(d.Year, d.Month, 1);

    /// <summary>«luglio 2026» per le pillole dei mesi; corto «lug» sul telefono.</summary>
    public static string NomeMese(DateOnly mese) => mese.ToString("MMMM yyyy", It);

    public static string MeseBreve(DateOnly mese) => mese.ToString("MMM", It);

    /// <summary>«venerdì 3 luglio 2026»: il giorno dello stato in cima.</summary>
    public static string GiornoLungo(DateOnly d) => d.ToString("dddd d MMMM yyyy", It);

    /// <summary>Il giorno dal parametro dell'indirizzo (AAAA-MM-GG); null se manca o non è una data.</summary>
    public static DateOnly? LeggiGiorno(string? testo) =>
        DateOnly.TryParseExact(testo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>La pagina «giorno per giorno» del vigneto (dalla pagina dell'azienda).</summary>
    public static string Indirizzo(int azienda, int vigneto, DateOnly? giorno = null) =>
        $"bilancio-idrico/vigneto?azienda={azienda}&vigneto={vigneto}" + (giorno is { } g ? $"&giorno={Iso(g)}" : "");

    /// <summary>«Scarica Excel» del vigneto.</summary>
    public static string IndirizzoExcel(int azienda, int vigneto) => $"bilancio-idrico/excel?azienda={azienda}&vigneto={vigneto}";

    /// <summary>I dati mancanti e le criticità di un giorno, in parole.</summary>
    public static string Segnale(string codice) => codice switch
    {
        "meteo" => "meteo mancante",
        "fenologia" => "fenologia mancante",
        "suolo" => "suolo mancante",
        "configurazione" => "configurazione del settore mancante",
        "kc" => "Kc mancante",
        "criticita-pdlwp" => "stress oltre la soglia",
        "criticita-deficit" => "deficit alto",
        _ => codice,
    };

    public static string Segnali(GiornoBilancioDto g) => g.Segnali is { Count: > 0 } s ? string.Join(", ", s.Select(Segnale)) : "";

    /// <summary>Priorità del motore in parole (3 manca un dato, 2 criticità Pdlwp, 1 criticità deficit).</summary>
    public static string Priorita(int p) => p switch
    {
        3 => "manca un dato",
        2 => "stress oltre la soglia",
        1 => "deficit alto",
        _ => "nessuna",
    };

    /// <summary>Pioggia e irrigazione del giorno: «4,0 mm di pioggia · 15,0 mm irrigati», «nessuna».</summary>
    public static string Acque(GiornoBilancioDto g)
    {
        var parti = new List<string>();
        if (g.PioggiaMm is > 0 and var p)
            parti.Add($"{TestiBilancio.Mm(p)} di pioggia");
        if (g.IrrigazioneMm > 0)
            parti.Add($"{TestiBilancio.Mm(g.IrrigazioneMm)} irrigati");
        return parti.Count > 0 ? string.Join(" · ", parti) : g.PioggiaMm is null ? "pioggia non nota" : "nessuna";
    }

    /// <summary>Da dove viene il meteo del giorno.</summary>
    public static string Meteo(GiornoBilancioDto g, Func<int, string>? nome = null) =>
        g.MeteoDaVigneto is { } v
            ? $"dal vigneto {(nome is null ? v.ToString(It) : nome(v))}{(g.MeteoDistanzaKm is { } km ? $", a {km.ToString("0.#", It)} km" : "")}"
              + (g.Previsione ? " (previsto)" : "")
            : g.Segnali?.Contains("meteo") == true ? "mancante" : g.Previsione ? "dell'azienda (previsto)" : "dell'azienda";

    /// <summary>I settori del giorno con le ore suggerite: «Pozzo 100 % · 2,5 h».</summary>
    public static string Settori(GiornoBilancioDto g, IReadOnlyDictionary<int, string>? nomi) =>
        g.Settori is { Count: > 0 } s
            ? string.Join("; ", s.Select(x => $"{(nomi?.TryGetValue(x.IdSettore, out var n) == true ? n : $"Settore {x.IdSettore}")} "
                + $"{x.Percentuale.ToString("0.#", It)} %"
                + (x.IrrigazioneSuggeritaOre is { } o ? $" · {TestiBilancio.Ore(o)}" : "")
                + (x.ConfigMancante ? " · configurazione mancante" : "")
                + (x.OltreTempoMassimo ? " · oltre il tempo massimo" : "")))
            : "senza settore";
}
