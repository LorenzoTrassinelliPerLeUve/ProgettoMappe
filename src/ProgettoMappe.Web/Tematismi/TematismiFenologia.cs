using System.Globalization;
using System.Text.RegularExpressions;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>Un punto della scala dei colori della fenologia: il colore che ha la fase <see cref="Bbch"/>.</summary>
public sealed class TappaFenologiaOptions
{
    public int Bbch { get; set; }

    public string Colore { get; set; } = "";
}

/// <summary>
/// Colori della fenologia (sezione <c>Tematismi:Fenologia</c>): tappe con codice BBCH crescente;
/// le fasi fra due tappe hanno un colore intermedio, quelle prima della prima o dopo l'ultima il
/// colore della tappa più vicina.
/// </summary>
public sealed class FenologiaOptions
{
    public TappaFenologiaOptions[] Tappe { get; set; } = [];
}

/// <summary>
/// Tematismo «Fenologia» (task #44): per vigneto la fase BBCH dell'ultimo rilievo dell'anno scelto
/// (rilievi di 4Grapes, riassunti dall'Api). Una classe per codice BBCH (0–99, l'indice è il codice),
/// in legenda solo le fasi presenti con il loro nome; colore su una scala ordinata dal germogliamento
/// alla vendemmia, in configurazione. Non si confronta fra anni (la fase dipende dal giorno dell'ultimo
/// rilievo, non dall'annata) e si apre sull'anno più recente.
/// </summary>
public static partial class TematismiFenologia
{
    public const string Id = "fenologia";

    public const int CodiceMassimo = 99;

    /// <summary>Dormienza (marrone), germogliamento e foglie (verdi), fioritura (giallo), acini (arancio), invaiatura e vendemmia (viola), caduta delle foglie.</summary>
    public static readonly FenologiaOptions Predefinita = new()
    {
        Tappe =
        [
            new() { Bbch = 0, Colore = "#8c6d4f" },
            new() { Bbch = 7, Colore = "#d9f0a3" },
            new() { Bbch = 19, Colore = "#78c679" },
            new() { Bbch = 57, Colore = "#238443" },
            new() { Bbch = 65, Colore = "#fed976" },
            new() { Bbch = 77, Colore = "#fd8d3c" },
            new() { Bbch = 81, Colore = "#9e9ac8" },
            new() { Bbch = 89, Colore = "#54278f" },
            new() { Bbch = 97, Colore = "#7f2704" },
        ],
    };

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    public static Tematismo Crea(FenologiaOptions? opzioni, ILogger logger)
    {
        var tappe = Valida(opzioni, logger);
        return new Tematismo
        {
            Id = Id,
            Nome = "Fenologia (fase BBCH)",
            Fonte = FonteTematismo.Fenologia,
            Nota = "Fase dell'ultimo rilievo dell'anno: se quel giorno ci sono più rilievi, la più avanzata. "
                + "Colori dal germogliamento alla vendemmia.",
            Classi = Enumerable.Range(0, CodiceMassimo + 1)
                .Select(b => new ClasseTematismo(b.ToString(CultureInfo.InvariantCulture), Colore(b, tappe), SoloSePresente: true))
                .ToList(),
            Classifica = TematismiBigot.Su<FenologiaVignetoDto, int?>(f => f.Bbch is >= 0 and <= CodiceMassimo ? f.Bbch : null),
            ValoreConfronto = TematismiBigot.Su<FenologiaVignetoDto, decimal?>(f => f.Bbch),
            TestoValore = TematismiBigot.Su<FenologiaVignetoDto, string?>(TestoElenco),
            EtichettaFuoriPerimetro = "Nessun rilievo nel {anno}",
        };
    }

    /// <summary>Errori della scala (vuoto = valida): almeno una tappa, codici crescenti fra 0 e 99, colori #rrggbb.</summary>
    public static IReadOnlyList<string> Errori(FenologiaOptions opzioni)
    {
        var errori = new List<string>();
        var tappe = opzioni.Tappe;
        if (tappe.Length == 0)
        {
            errori.Add("nessuna tappa");
        }
        if (tappe.Any(t => t.Bbch is < 0 or > CodiceMassimo))
        {
            errori.Add($"i codici BBCH vanno da 0 a {CodiceMassimo}");
        }
        for (var i = 1; i < tappe.Length; i++)
        {
            if (tappe[i].Bbch <= tappe[i - 1].Bbch)
            {
                errori.Add("i codici BBCH delle tappe devono essere crescenti");
                break;
            }
        }
        if (tappe.Any(t => !ColoreValido().IsMatch(t.Colore ?? "")))
        {
            errori.Add("i colori devono essere nella forma #rrggbb");
        }

        return errori;
    }

    /// <summary>Colore della fase: quello della tappa, o la media pesata fra le due tappe vicine.</summary>
    public static string Colore(int bbch, IReadOnlyList<TappaFenologiaOptions> tappe)
    {
        if (bbch <= tappe[0].Bbch)
        {
            return tappe[0].Colore.ToLowerInvariant();
        }

        for (var i = 1; i < tappe.Count; i++)
        {
            if (bbch <= tappe[i].Bbch)
            {
                var (da, a) = (tappe[i - 1], tappe[i]);
                var t = (double)(bbch - da.Bbch) / (a.Bbch - da.Bbch);
                return Mescola(da.Colore, a.Colore, t);
            }
        }

        return tappe[^1].Colore.ToLowerInvariant();
    }

    /// <summary>«65 – Piena fioritura»: il codice e il nome breve (fino ai due punti); solo il codice se il nome manca.</summary>
    public static string NomeFase(int bbch, string? descrizione)
    {
        var breve = descrizione?.Split([':', ';'], 2)[0].Trim();
        return string.IsNullOrEmpty(breve)
            ? bbch.ToString(CultureInfo.InvariantCulture)
            : $"{bbch} – {char.ToUpper(breve[0], Italiano)}{breve[1..]}";
    }

    /// <summary>Nomi delle fasi presenti nelle righe (ultima fase e precedenti), per la legenda: codice → «65 – Piena fioritura».</summary>
    public static IReadOnlyDictionary<int, string> NomiFasi(IEnumerable<FenologiaVignetoDto> righe)
    {
        var nomi = new Dictionary<int, string>();
        foreach (var riga in righe)
        {
            foreach (var (bbch, descrizione) in riga.Precedenti.Select(p => (p.Bbch, p.Descrizione)).Prepend((riga.Bbch, riga.Descrizione)))
            {
                if (descrizione is not null || !nomi.ContainsKey(bbch))
                {
                    nomi[bbch] = NomeFase(bbch, descrizione);
                }
            }
        }

        return nomi;
    }

    /// <summary>Nell'elenco dei vigneti, corto per stare su una riga: «BBCH 65 · 12/06» (il nome della fase è in legenda e nella scheda).</summary>
    public static string TestoElenco(FenologiaVignetoDto riga) =>
        $"BBCH {riga.Bbch.ToString(CultureInfo.InvariantCulture)} · {Giorno(riga.Data)}";

    /// <summary>Riga della scheda del vigneto: «65 – Piena fioritura, rilevata il 12/06».</summary>
    public static string TestoScheda(FenologiaVignetoDto riga) => $"{NomeFase(riga.Bbch, riga.Descrizione)}, rilevata il {Giorno(riga.Data)}";

    /// <summary>Le fasi precedenti dell'anno, in piccolo sotto la riga: «57 il 28/05 · 61 il 03/06»; null se non ce ne sono.</summary>
    public static string? TestoPrecedenti(FenologiaVignetoDto riga) =>
        riga.Precedenti.Count == 0
            ? null
            : string.Join(" · ", riga.Precedenti.Select(p => $"{p.Bbch} il {Giorno(p.Data)}"));

    public static string Giorno(DateOnly data) => data.ToString("dd/MM", CultureInfo.InvariantCulture);

    private static IReadOnlyList<TappaFenologiaOptions> Valida(FenologiaOptions? opzioni, ILogger logger)
    {
        if (opzioni is null)
        {
            return Predefinita.Tappe;
        }

        var errori = Errori(opzioni);
        if (errori.Count == 0)
        {
            return opzioni.Tappe;
        }

        logger.LogWarning("Tematismi:Fenologia non valido ({Errori}): uso la scala predefinita", string.Join("; ", errori));
        return Predefinita.Tappe;
    }

    private static string Mescola(string da, string a, double t)
    {
        int Canale(string colore, int i) => Convert.ToInt32(colore.Substring(1 + 2 * i, 2), 16);
        var canali = Enumerable.Range(0, 3)
            .Select(i => (int)Math.Round(Canale(da, i) + (Canale(a, i) - Canale(da, i)) * t))
            .Select(c => c.ToString("x2", CultureInfo.InvariantCulture));
        return "#" + string.Concat(canali);
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex ColoreValido();
}
