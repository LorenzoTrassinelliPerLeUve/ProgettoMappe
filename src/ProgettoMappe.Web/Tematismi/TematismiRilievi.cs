using System.Globalization;
using System.Text.RegularExpressions;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Tematismo «Ultimo rilievo» (task #45): per vigneto i giorni dall'ultimo rilievo in campo di
/// qualunque tipo, contati da oggi (ora italiana). Classi fisse chieste dal proprietario: meno di 7
/// giorni, meno di 30, oltre 30 (grigio, «scoperto») e mai rilevato. Senza anno né confronto.
/// </summary>
public static partial class TematismiRilievi
{
    public const string Id = "ultimo-rilievo";

    public const int ClasseRecente = 0;
    public const int ClasseMese = 1;
    public const int ClasseScoperto = 2;
    public const int ClasseMaiRilevato = 3;

    /// <summary>Giorni: fino a <see cref="GiorniRecente"/> esclusi è «recente», fino a <see cref="GiorniScoperto"/> esclusi è «nel mese».</summary>
    public const int GiorniRecente = 7;
    public const int GiorniScoperto = 30;

    /// <summary>Chiave d'ordine dell'elenco dei mai rilevati: in cima, prima dei più vecchi.</summary>
    private const decimal ChiaveMaiRilevato = int.MaxValue;

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <param name="oggi">Il giorno da cui contare (per i test); null = oggi in Italia, letto a ogni uso.</param>
    public static Tematismo Crea(Func<DateOnly>? oggi = null)
    {
        oggi ??= () => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TematismiBigot.FusoItalia).DateTime);
        return new Tematismo
        {
            Id = Id,
            Nome = "Ultimo rilievo",
            Fonte = FonteTematismo.Rilievi,
            Nota = "Giorni dall'ultimo rilievo in campo di qualunque tipo, contati da oggi. "
                + "In grigio i vigneti scoperti da oltre 30 giorni.",
            Classi =
            [
                new("Meno di 7 giorni", "#1a9850"),
                new("Da 7 a 30 giorni", "#fee08b"),
                new("Scoperto: oltre 30 giorni", "#a6a6a6"),
                new("Mai rilevato", "#4d4d4d"),
            ],
            Classifica = TematismiBigot.Su<UltimoRilievoDto, int?>(r => Classe(r, oggi())),
            ValoreConfronto = TematismiBigot.Su<UltimoRilievoDto, decimal?>(r => Giorni(r, oggi()) ?? ChiaveMaiRilevato),
            TestoValore = TematismiBigot.Su<UltimoRilievoDto, string?>(r => TestoElenco(r, oggi())),
            EtichettaFuoriPerimetro = "Dato non disponibile",
        };
    }

    /// <summary>Giorni dal rilievo a <paramref name="oggi"/> (un rilievo con data futura conta 0); null se mai rilevato.</summary>
    public static int? Giorni(UltimoRilievoDto rilievo, DateOnly oggi) =>
        rilievo.Data is { } data ? Math.Max(0, oggi.DayNumber - DateOnly.FromDateTime(data).DayNumber) : null;

    public static int Classe(UltimoRilievoDto rilievo, DateOnly oggi) => Giorni(rilievo, oggi) switch
    {
        null => ClasseMaiRilevato,
        < GiorniRecente => ClasseRecente,
        < GiorniScoperto => ClasseMese,
        _ => ClasseScoperto,
    };

    /// <summary>Nell'elenco dei vigneti: «10/10 · Peronospora», «Mai rilevato».</summary>
    public static string TestoElenco(UltimoRilievoDto rilievo, DateOnly oggi) =>
        rilievo.Data is { } data
            ? string.Join(" · ", new[] { Giorno(data, oggi), TipoBreve(rilievo.Tipo) }.Where(t => t is not null))
            : "Mai rilevato";

    /// <summary>
    /// Nella scheda del vigneto: «Peronospora, 10/10 da M. Broggi, con foto»; tipo (corto, vedi
    /// <see cref="TipoBreve"/>), autore e foto solo se ci sono. «Mai rilevato» senza data.
    /// </summary>
    public static string TestoScheda(UltimoRilievoDto rilievo, DateOnly oggi)
    {
        if (rilievo.Data is not { } data)
        {
            return "Mai rilevato";
        }

        var testo = TipoBreve(rilievo.Tipo) is { } tipo ? $"{tipo}, {Giorno(data, oggi)}" : Giorno(data, oggi);
        if (rilievo.Autore is { } autore)
        {
            testo += $" da {autore}";
        }

        return rilievo.ConFoto ? testo + ", con foto" : testo;
    }

    /// <summary>
    /// Nome corto del tipo di rilievo, che in 4Grapes è maiuscolo e lungo (158 tipi, letti dal dba il
    /// 2026-10-13): senza le parti fra parentesi (unità, precisazioni), senza il nome latino (dopo
    /// « - », o subito dopo il nome maiuscolo come in «OIDIO Erysiphe Necator - …») e senza «della
    /// pianta», in minuscolo con l'iniziale maiuscola. Una sigla dopo il trattino (virus: «GLRaV»)
    /// resta, perché distingue i tipi. «PERONOSPORA - Plasmopara viticola» → «Peronospora», «STATO
    /// IDRICO DELLA PIANTA (classi)» → «Stato idrico». Il nome intero resta come title nella scheda.
    /// </summary>
    public static string? TipoBreve(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return null;
        }

        var pulito = Spazi().Replace(Parentesi().Replace(tipo, " ").Replace('_', ' '), " ").Trim();
        var parti = pulito.Split([" - ", " – "], 2, StringSplitOptions.None);
        var dopo = parti.Length > 1 ? parti[1].Trim() : "";
        var sigla = Sigla().IsMatch(dopo) ? dopo : null;

        // Il nome latino senza trattino comincia con una parola «Maiuscola» dopo quelle tutte maiuscole.
        var parole = parti[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var fine = parole.Length;
        for (var i = 1; i < parole.Length && parole[i - 1] == parole[i - 1].ToUpper(Italiano); i++)
        {
            if (Capitalizzata().IsMatch(parole[i]))
            {
                fine = i;
                break;
            }
        }

        var breve = DellaPianta().Replace(string.Join(' ', parole[..fine]), "").Trim();
        if (breve.Length == 0)
        {
            return tipo.Trim();
        }

        breve = breve.ToLower(Italiano);
        breve = char.ToUpper(breve[0], Italiano) + breve[1..];
        breve = NomiCorti.GetValueOrDefault(breve, breve);
        return sigla is null ? breve : $"{breve} {sigla}";
    }

    /// <summary>Tipi frequenti che restano lunghi anche puliti: il nome con cui li chiama chi lavora in vigna (task #91).</summary>
    private static readonly Dictionary<string, string> NomiCorti = new(StringComparer.Ordinal)
    {
        ["Fase fenologica"] = "Fenologia",
    };

    [GeneratedRegex(@"\([^)]*\)?")]
    private static partial Regex Parentesi();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spazi();

    [GeneratedRegex(@"\s+della pianta\b", RegexOptions.IgnoreCase)]
    private static partial Regex DellaPianta();

    /// <summary>Una parola sola, fino a 6 caratteri, con almeno due maiuscole: «GPGV», «GLRaV».</summary>
    [GeneratedRegex(@"^(?=(?:\S*\p{Lu}){2})\S{2,6}$")]
    private static partial Regex Sigla();

    [GeneratedRegex(@"^\p{Lu}\p{Ll}+$")]
    private static partial Regex Capitalizzata();

    /// <summary>«10/10», con l'anno se non è quello di <paramref name="oggi"/> («10/10/2024»).</summary>
    public static string Giorno(DateTime data, DateOnly oggi) =>
        data.ToString(data.Year == oggi.Year ? "dd/MM" : "dd/MM/yyyy", Italiano);
}
