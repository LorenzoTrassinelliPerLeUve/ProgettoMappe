using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Tematismi del gruppo «Difesa» (task #68), senza anno né confronto:
/// <list type="bullet">
/// <item>«Ultimo trattamento»: giorni dall'ultimo trattamento fatto (Effettivo in 4Grapes, non i
/// consigliati), contati da oggi in ora italiana. Classi fisse chieste dal proprietario: meno di 7
/// giorni, 7–14, 15–30, oltre 30 o mai trattato (grigio).</item>
/// <item>«Linea di difesa»: un colore per linea (<c>Vigneto.IdTipologiaLinea</c>), nomi dai dati; i
/// vigneti senza linea in grigio, «Nessuna linea assegnata».</item>
/// </list>
/// </summary>
public static class TematismiDifesa
{
    public const string IdUltimoTrattamento = "ultimo-trattamento";
    public const string IdLinea = "linea-difesa";

    public const int ClasseRecente = 0;
    public const int ClasseDueSettimane = 1;
    public const int ClasseMese = 2;
    public const int ClasseScoperto = 3;

    /// <summary>Giorni: sotto <see cref="GiorniRecente"/> «recente», fino a <see cref="GiorniDueSettimane"/> e <see cref="GiorniMese"/> compresi le classi di mezzo.</summary>
    public const int GiorniRecente = 7;
    public const int GiorniDueSettimane = 14;
    public const int GiorniMese = 30;

    /// <summary>Linea: la classe è l'Id della linea (1–<see cref="LineaMassima"/>); 0 = nessuna linea assegnata.</summary>
    public const int ClasseNessunaLinea = 0;
    public const int LineaMassima = 199;

    public const string EtichettaNessunaLinea = "Nessuna linea assegnata";

    private const string Grigio = "#a6a6a6";

    /// <summary>Chiave d'ordine dei mai trattati e dei senza linea: in fondo all'ordine per valore.</summary>
    private const decimal ChiaveMancante = int.MaxValue;

    /// <summary>
    /// Colori delle linee usate dai vigneti VTS (dba, 2026-10-13: 9 su 38), scelti ben distinti; le
    /// altre linee prendono un colore da <see cref="AltriColori"/> secondo l'Id.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, string> ColoriLinee = new Dictionary<int, string>
    {
        [1] = "#d62728",  // DPI
        [2] = "#ff7f0e",  // SAFE
        [3] = "#1f77b4",  // SOFT
        [4] = "#2ca02c",  // BIO 4 KG
        [5] = "#98df8a",  // BIO 2 KG
        [20] = "#e377c2", // GENTLE
        [25] = "#8c564b", // TIKVES ORGANIC
        [27] = "#9467bd", // TIKVES CONVENTIONAL
        [36] = "#ffbb78", // SAFE - DECISION 4GRAPES
        [38] = "#17becf", // TESTUGGINE
    };

    private static readonly string[] AltriColori =
        ["#bcbd22", "#c5b0d5", "#c49c94", "#f7b6d2", "#9edae5", "#dbdb8d", "#aec7e8", "#ff9896", "#393b79", "#637939", "#8c6d31", "#843c39"];

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <param name="oggi">Il giorno da cui contare (per i test); null = oggi in Italia, letto a ogni uso.</param>
    public static Tematismo CreaUltimoTrattamento(Func<DateOnly>? oggi = null)
    {
        oggi ??= () => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TematismiBigot.FusoItalia).DateTime);
        return new Tematismo
        {
            Id = IdUltimoTrattamento,
            Nome = "Ultimo trattamento",
            Fonte = FonteTematismo.Difesa,
            Nota = "Giorni dall'ultimo trattamento fatto (non i consigliati), contati da oggi. "
                + "In grigio i vigneti non trattati da oltre 30 giorni o senza trattamenti registrati.",
            Classi =
            [
                new("Meno di 7 giorni", "#1a9850"),
                new("Da 7 a 14 giorni", "#a6d96a"),
                new("Da 15 a 30 giorni", "#fee08b"),
                new("Oltre 30 giorni o mai", Grigio),
            ],
            Classifica = TematismiBigot.Su<DifesaVignetoDto, int?>(d => Classe(d, oggi())),
            ValoreConfronto = TematismiBigot.Su<DifesaVignetoDto, decimal?>(d => Giorni(d, oggi()) ?? ChiaveMancante),
            TestoValore = TematismiBigot.Su<DifesaVignetoDto, string?>(d => TestoElenco(d, oggi())),
            EtichettaFuoriPerimetro = "Dato non disponibile",
        };
    }

    public static Tematismo CreaLinea() => new()
    {
        Id = IdLinea,
        Nome = "Linea di difesa",
        Fonte = FonteTematismo.Difesa,
        NomiDaiDati = true,
        Nota = "La linea di difesa assegnata al vigneto in 4Grapes, un colore per linea.",
        Classi = Enumerable.Range(0, LineaMassima + 1)
            .Select(id => id == ClasseNessunaLinea
                ? new ClasseTematismo(EtichettaNessunaLinea, Grigio, SoloSePresente: true)
                : new ClasseTematismo($"Linea {id.ToString(CultureInfo.InvariantCulture)}", ColoreLinea(id), SoloSePresente: true))
            .ToList(),
        Classifica = TematismiBigot.Su<DifesaVignetoDto, int?>(ClasseLinea),
        ValoreConfronto = TematismiBigot.Su<DifesaVignetoDto, decimal?>(d => d.IdLinea ?? ChiaveMancante),
        TestoValore = TematismiBigot.Su<DifesaVignetoDto, string?>(d => d.IdLinea is null ? EtichettaNessunaLinea : NomeLinea(d)),
        EtichettaFuoriPerimetro = "Dato non disponibile",
    };

    /// <summary>Giorni dal trattamento a <paramref name="oggi"/> (una data futura conta 0); null se mai trattato.</summary>
    public static int? Giorni(DifesaVignetoDto difesa, DateOnly oggi) =>
        difesa.Data is { } data ? Math.Max(0, oggi.DayNumber - DateOnly.FromDateTime(data).DayNumber) : null;

    public static int Classe(DifesaVignetoDto difesa, DateOnly oggi) => Giorni(difesa, oggi) switch
    {
        < GiorniRecente => ClasseRecente,
        <= GiorniDueSettimane => ClasseDueSettimane,
        <= GiorniMese => ClasseMese,
        _ => ClasseScoperto, // anche mai trattato
    };

    /// <summary>La classe della linea: il suo Id, <see cref="ClasseNessunaLinea"/> senza linea, null con un Id fuori scala.</summary>
    public static int? ClasseLinea(DifesaVignetoDto difesa) => difesa.IdLinea switch
    {
        null => ClasseNessunaLinea,
        >= 1 and <= LineaMassima and var id => id,
        _ => null,
    };

    public static string ColoreLinea(int id) =>
        ColoriLinee.TryGetValue(id, out var colore) ? colore : AltriColori[id % AltriColori.Length];

    /// <summary>Il nome della linea come in 4Grapes («BIO 4 KG»), o «Linea 12» se 4Grapes non lo ha.</summary>
    public static string NomeLinea(DifesaVignetoDto difesa) =>
        difesa.Linea ?? $"Linea {difesa.IdLinea?.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Nomi delle linee presenti nelle righe, per la legenda: Id → «BIO 4 KG».</summary>
    public static IReadOnlyDictionary<int, string> NomiLinee(IEnumerable<DifesaVignetoDto> righe) =>
        righe.Where(d => d.IdLinea is not null && d.Linea is not null)
            .GroupBy(d => d.IdLinea!.Value)
            .ToDictionary(g => g.Key, g => g.First().Linea!);

    /// <summary>Nell'elenco dei vigneti, corto: «03/07 · rame», «Mai trattato».</summary>
    public static string TestoElenco(DifesaVignetoDto difesa, DateOnly oggi) =>
        difesa.Data is { } data
            ? string.Join(" · ", new[] { TematismiRilievi.Giorno(data, oggi), Cosa(difesa, 1) }.Where(t => t is not null))
            : "Mai trattato";

    /// <summary>
    /// Nella scheda del vigneto: «03/07, rame (peronospora) · linea BIO 4 KG»; principi attivi (o
    /// prodotti, se 4Grapes non ha i principi), avversità e linea solo se ci sono. Senza trattamenti:
    /// «Nessun trattamento registrato».
    /// </summary>
    public static string TestoScheda(DifesaVignetoDto difesa, DateOnly oggi)
    {
        var testo = "Nessun trattamento registrato";
        if (difesa.Data is { } data)
        {
            testo = TematismiRilievi.Giorno(data, oggi);
            if (Cosa(difesa, 3) is { } cosa)
            {
                testo += $", {cosa}";
            }
            if (Avversita(difesa) is { } avversita)
            {
                testo += $" ({avversita})";
            }
        }

        return difesa.IdLinea is null ? testo : $"{testo} · linea {NomeLinea(difesa)}";
    }

    /// <summary>La classe di rischio di 4Grapes in parole: «ALTO / BASSO» → «peronospora alto, oidio basso»; null se manca.</summary>
    public static string? TestoRischio(string? rischio)
    {
        if (string.IsNullOrWhiteSpace(rischio))
        {
            return null;
        }

        var parti = rischio.Split('/', StringSplitOptions.TrimEntries);
        return parti.Length == 2 && parti.All(p => p.Length > 0)
            ? $"peronospora {parti[0].ToLower(Italiano)}, oidio {parti[1].ToLower(Italiano)}"
            : rischio.Trim().ToLower(Italiano);
    }

    /// <summary>Per il title della scheda: «Prodotti: CYPRUS 25 WG BLU, THIOVIT JET»; null se non ce ne sono.</summary>
    public static string? TestoProdotti(DifesaVignetoDto difesa) =>
        difesa.Prodotti.Count == 0 ? null : "Prodotti: " + string.Join(", ", difesa.Prodotti);

    /// <summary>Principi attivi in minuscolo («rame, zolfo e altri 2»), o i prodotti come in 4Grapes; null se nessuno dei due.</summary>
    private static string? Cosa(DifesaVignetoDto difesa, int quanti)
    {
        var elenco = difesa.PrincipiAttivi.Count > 0
            ? difesa.PrincipiAttivi.Select(p => p.ToLower(Italiano)).ToList()
            : difesa.Prodotti.ToList();
        if (elenco.Count == 0)
        {
            return null;
        }

        var primi = string.Join(", ", elenco.Take(quanti));
        return elenco.Count > quanti ? $"{primi} e altri {elenco.Count - quanti}" : primi;
    }

    /// <summary>Avversità corte e in minuscolo («peronospora, muffa grigia o botrite»); null se nessuna.</summary>
    private static string? Avversita(DifesaVignetoDto difesa)
    {
        var nomi = difesa.Avversita.Select(TematismiRilievi.TipoBreve).OfType<string>().Select(a => a.ToLower(Italiano)).Distinct().ToList();
        return nomi.Count == 0 ? null : string.Join(", ", nomi);
    }
}
