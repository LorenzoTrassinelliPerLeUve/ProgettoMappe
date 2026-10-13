using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Tematismo «Ultimo rilievo» (task #45): per vigneto i giorni dall'ultimo rilievo in campo di
/// qualunque tipo, contati da oggi (ora italiana). Classi fisse chieste dal proprietario: meno di 7
/// giorni, meno di 30, oltre 30 (grigio, «scoperto») e mai rilevato. Senza anno né confronto.
/// </summary>
public static class TematismiRilievi
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

    /// <summary>Nell'elenco dei vigneti: «10/10 · Fenologia», «Mai rilevato».</summary>
    public static string TestoElenco(UltimoRilievoDto rilievo, DateOnly oggi) =>
        rilievo.Data is { } data
            ? string.Join(" · ", new[] { Giorno(data, oggi), rilievo.Tipo }.Where(t => t is not null))
            : "Mai rilevato";

    /// <summary>
    /// Nella scheda del vigneto: «Fenologia, 10/10 da M. Broggi, con foto»; tipo, autore e foto
    /// solo se ci sono. «Mai rilevato» senza data.
    /// </summary>
    public static string TestoScheda(UltimoRilievoDto rilievo, DateOnly oggi)
    {
        if (rilievo.Data is not { } data)
        {
            return "Mai rilevato";
        }

        var testo = rilievo.Tipo is { } tipo ? $"{tipo}, {Giorno(data, oggi)}" : Giorno(data, oggi);
        if (rilievo.Autore is { } autore)
        {
            testo += $" da {autore}";
        }

        return rilievo.ConFoto ? testo + ", con foto" : testo;
    }

    /// <summary>«10/10», con l'anno se non è quello di <paramref name="oggi"/> («10/10/2024»).</summary>
    public static string Giorno(DateTime data, DateOnly oggi) =>
        data.ToString(data.Year == oggi.Year ? "dd/MM" : "dd/MM/yyyy", Italiano);
}
