using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Il valore NDVI di un vigneto in un anno, a un passaggio scelto (vedi <see cref="TematismiNdvi.Righe"/>):
/// <see cref="Data"/> e <see cref="Media"/> null = nessun passaggio pulito nei giorni prima.
/// </summary>
public sealed record RigaNdvi(int IdVigneto, int Anno, DateOnly? Data, decimal? Media) : IRigaAnnuale;

/// <summary>
/// Tematismo «NDVI (vigore da satellite)», gruppo «Satellite» (task #84): media dell'NDVI del vigneto
/// all'ultimo passaggio pulito di Sentinel-2, in 5 classi fisse dal beige al verde scuro; in grigio
/// i vigneti senza un passaggio pulito nei <see cref="GiorniValidita"/> giorni prima del passaggio
/// scelto. Si può tornare indietro di <see cref="PassaggiSelettore"/> passaggi e confrontare con lo
/// stesso giorno dell'anno prima (verso «alto è meglio»).
/// </summary>
public static class TematismiNdvi
{
    public const string Id = "ndvi";

    public static readonly decimal[] Soglie = [0.2m, 0.35m, 0.5m, 0.65m];
    public static readonly string[] Colori = ["#e8dcb8", "#c9d98f", "#93c25f", "#4f9a3a", "#1d6427"];

    /// <summary>Classe dei vigneti senza un passaggio pulito: dopo le 5 della scala.</summary>
    public const int ClasseNessunPassaggio = 5;
    public const string EtichettaNessunPassaggio = "Nessun passaggio pulito";

    /// <summary>Un valore più vecchio di così rispetto al passaggio scelto non vale: il vigneto va in grigio.</summary>
    public const int GiorniValidita = 30;

    /// <summary>Quanti passaggi puliti offre il selettore della data (dal più recente).</summary>
    public const int PassaggiSelettore = 8;

    private const string Grigio = "#a6a6a6";

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    public static Tematismo Crea() => new()
    {
        Id = Id,
        Nome = "NDVI (vigore da satellite)",
        Fonte = FonteTematismo.Ndvi,
        Nota = "Media dell'NDVI dei pixel del vigneto (Sentinel-2, 10 m): più alto = più vegetazione verde. "
            + $"In grigio i vigneti senza un passaggio pulito (poche nuvole) nei {GiorniValidita} giorni prima.",
        Classi = [.. TematismiBigot.ClassiNumeriche(Soglie, Colori, null), new ClasseTematismo(EtichettaNessunPassaggio, Grigio, Speciale: true)],
        Classifica = TematismiBigot.Su<RigaNdvi, int?>(r => Classe(r)),
        ValoreConfronto = TematismiBigot.Su<RigaNdvi, decimal?>(r => r.Media),
        TestoValore = TematismiBigot.Su<RigaNdvi, string?>(Testo),
        EtichettaFuoriPerimetro = "Nessun dato NDVI nel {anno}",
    };

    public static int Classe(RigaNdvi riga) =>
        riga.Media is { } media ? TematismiBigot.ClasseNumerica(media, Soglie) : ClasseNessunPassaggio;

    /// <summary>«0,62 (11/10)», o «Nessun passaggio pulito».</summary>
    public static string Testo(RigaNdvi riga) =>
        riga is { Media: { } media, Data: { } data } ? $"{Numero(media)} ({data:dd'/'MM})" : EtichettaNessunPassaggio;

    public static string Numero(decimal media) => media.ToString("0.00", Italiano);

    /// <summary>
    /// Una riga per vigneto per ciascuno dei due anni della risposta (solo per gli anni con almeno un
    /// punto): il valore dell'ultimo passaggio pulito fino al giorno <paramref name="passaggio"/>
    /// portato in quell'anno (l'anno prima: lo stesso giorno), non più vecchio di <see cref="GiorniValidita"/>.
    /// </summary>
    public static IReadOnlyList<RigaNdvi> Righe(NdviAziendaDto ndvi, DateOnly passaggio)
    {
        var righe = new List<RigaNdvi>();
        foreach (var (anno, serie) in new (int, Func<NdviVignetoDto, IReadOnlyList<PuntoNdviDto>>)[]
                     { (ndvi.Anno, v => v.Serie), (ndvi.Anno - 1, v => v.SeriePrecedente) })
        {
            if (!ndvi.Vigneti.Any(v => serie(v).Count > 0))
            {
                continue;
            }

            var limite = StessoGiorno(passaggio, anno);
            foreach (var vigneto in ndvi.Vigneti)
            {
                var punto = PuntoAl(serie(vigneto), limite);
                righe.Add(new RigaNdvi(vigneto.IdVigneto, anno, punto?.Data, punto?.Media));
            }
        }

        return righe;
    }

    /// <summary>L'ultimo punto fino a <paramref name="limite"/> compreso, se non più vecchio di <see cref="GiorniValidita"/> giorni.</summary>
    public static PuntoNdviDto? PuntoAl(IEnumerable<PuntoNdviDto> serie, DateOnly limite) =>
        serie.Where(p => p.Data <= limite && limite.DayNumber - p.Data.DayNumber < GiorniValidita).MaxBy(p => p.Data);

    /// <summary>Lo stesso giorno e mese in un altro anno (il 29 febbraio diventa il 28).</summary>
    public static DateOnly StessoGiorno(DateOnly giorno, int anno) =>
        new(anno, giorno.Month, Math.Min(giorno.Day, DateTime.DaysInMonth(anno, giorno.Month)));

    /// <summary>Gli ultimi <see cref="PassaggiSelettore"/> passaggi puliti, dal più recente.</summary>
    public static IReadOnlyList<PassaggioNdviDto> UltimiPassaggi(IEnumerable<PassaggioNdviDto> passaggi) =>
        passaggi.Where(p => p.Valido).OrderByDescending(p => p.Data).DistinctBy(p => p.Data).Take(PassaggiSelettore).ToList();

    /// <summary>Il passaggio da mostrare all'inizio: l'ultimo pulito, altrimenti l'ultimo punto dei dati, altrimenti oggi.</summary>
    public static DateOnly PassaggioPredefinito(IEnumerable<PassaggioNdviDto> passaggi, NdviAziendaDto ndvi, DateOnly oggi) =>
        passaggi.Where(p => p.Valido).Select(p => (DateOnly?)p.Data).Max()
        ?? ndvi.Vigneti.SelectMany(v => v.Serie.Concat(v.SeriePrecedente)).Select(p => (DateOnly?)p.Data).Max()
        ?? oggi;

    /// <summary>
    /// Per la legenda: «Passaggio Sentinel-2 del 11/10, nuvole 5 %» (con l'anno se non è <paramref name="annoCorrente"/>):
    /// il giorno più frequente fra i vigneti dell'anno, con le nuvole del passaggio; null se nessun vigneto ha un valore.
    /// </summary>
    public static string? TestoPassaggio(IEnumerable<RigaNdvi> righe, int anno, IEnumerable<PassaggioNdviDto> passaggi, int annoCorrente)
    {
        var giorno = righe.Where(r => r.Anno == anno && r.Data is not null)
            .GroupBy(r => r.Data!.Value).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).Select(g => (DateOnly?)g.Key).FirstOrDefault();
        if (giorno is not { } data)
        {
            return null;
        }

        var testo = $"Passaggio Sentinel-2 del {data.ToString(anno == annoCorrente ? "dd'/'MM" : "dd'/'MM'/'yyyy", Italiano)}";
        return passaggi.FirstOrDefault(p => p.Data == data)?.NuvolePct is { } nuvole
            ? $"{testo}, nuvole {Math.Round(nuvole, 0).ToString("0", Italiano)} %"
            : testo;
    }

    /// <summary>Testo di un passaggio nel selettore: «11/10 · nuvole 5 %».</summary>
    public static string TestoOpzione(PassaggioNdviDto passaggio, int annoCorrente) =>
        passaggio.Data.ToString(passaggio.Data.Year == annoCorrente ? "dd'/'MM" : "dd'/'MM'/'yyyy", Italiano)
        + (passaggio.NuvolePct is { } nuvole ? $" · nuvole {Math.Round(nuvole, 0).ToString("0", Italiano)} %" : "");
}
