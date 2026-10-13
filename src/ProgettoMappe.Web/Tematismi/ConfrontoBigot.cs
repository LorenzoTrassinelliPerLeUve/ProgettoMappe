using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Confronto di un parametro BIGOT fra due anni ("Dal" → "Al"): ogni vigneto va in una di tre
/// classi (sceso, uguale, salito). Rosso/verde solo per i parametri con un verso noto
/// (<see cref="Tematismo.AltoPositivo"/>); per gli altri colori neutri, come nella legenda normale.
/// Valori confrontati arrotondati a 2 decimali, come si leggono nella scheda.
/// </summary>
public static class ConfrontoBigot
{
    public const int ClasseCalo = 0;
    public const int ClasseUguale = 1;
    public const int ClasseAumento = 2;

    public const string EtichettaNessunDato = "Senza dato in uno dei due anni";
    public const string EtichettaFuoriPerimetro = "Fuori perimetro BIGOT in entrambi gli anni";

    /// <summary>Per i tematismi non BIGOT: nessuna riga della loro fonte nei due anni.</summary>
    public const string EtichettaNonValutato = "Non valutato in nessuno dei due anni";

    public static string EtichettaSenzaRiga(Tematismo tema) =>
        tema.Fonte == FonteTematismo.Bigot ? EtichettaFuoriPerimetro : EtichettaNonValutato;

    private const string ColoreUguale = "#757575";

    public static IReadOnlyList<ClasseTematismo> Classi(Tematismo tema) =>
        tema.AltoPositivo ? [new("Peggiorato", "#d73027"), new("Uguale", ColoreUguale), new("Migliorato", "#1a9850")]
        : tema.AltoNegativo ? [new("Migliorato (sceso)", "#1a9850"), new("Uguale", ColoreUguale), new("Peggiorato (salito)", "#d73027")]
        : [new(tema.EtichettaCalo, "#5e3c99"), new("Uguale", ColoreUguale), new(tema.EtichettaAumento, "#e66101")];

    public static string Nota(Tematismo tema) =>
        (tema.AltoPositivo || tema.AltoNegativo
            ? "Differenza fra i due anni: verde migliorato, rosso peggiorato."
            : "Differenza fra i due anni. Questo parametro non ha un verso buono o cattivo: i colori dicono solo da che parte si è mosso.")
        + (tema.Id == "punteggio" ? $" Solo vigneti con tutti i {TematismiBigot.ParametriTotali} parametri in entrambi gli anni." : "");

    /// <summary>
    /// Classe di confronto per ogni vigneto con un valore in entrambi gli anni. Gli altri: "senza
    /// dato" se hanno una riga BIGOT in almeno uno dei due anni, altrimenti "fuori perimetro".
    /// </summary>
    public static ClassificazioneTematismo Classifica(Tematismo tema, IEnumerable<IRigaVigneto> righe, int annoDa, int annoA)
    {
        var lista = righe.OfType<IRigaAnnuale>().ToList();
        var da = PerVigneto(lista, annoDa);
        var a = PerVigneto(lista, annoA);
        var conRiga = new HashSet<int>(da.Keys);
        conRiga.UnionWith(a.Keys);

        var classi = new Dictionary<int, int>();
        foreach (var (id, rigaA) in a)
        {
            if (da.TryGetValue(id, out var rigaDa)
                && tema.ValoreConfronto(rigaDa) is { } prima && tema.ValoreConfronto(rigaA) is { } dopo)
            {
                classi[id] = Math.Sign(Math.Round(dopo, 2) - Math.Round(prima, 2)) switch
                {
                    < 0 => ClasseCalo,
                    0 => ClasseUguale,
                    _ => ClasseAumento,
                };
            }
        }

        return new ClassificazioneTematismo(classi, [], conRiga);
    }

    /// <summary>Riga della scheda del vigneto: «2024 → 2025: 3,2 → 2,8» («–» dove manca il dato).</summary>
    public static string RigaScheda(Tematismo tema, IEnumerable<IRigaVigneto> righe, int idVigneto, int annoDa, int annoA)
    {
        string Valore(int anno) =>
            righe.OfType<IRigaAnnuale>().FirstOrDefault(r => r.IdVigneto == idVigneto && r.Anno == anno) is { } riga
                ? tema.TestoValore(riga) ?? "–"
                : "–";

        return $"{annoDa} → {annoA}: {Valore(annoDa)} → {Valore(annoA)}";
    }

    /// <summary>Anno "Dal" proposto attivando il confronto: il precedente disponibile, altrimenti il successivo.</summary>
    public static int? AnnoDaPredefinito(IEnumerable<int> anniDisponibili, int annoA)
    {
        var anni = anniDisponibili.Distinct().ToList();
        return anni.Where(x => x < annoA).Cast<int?>().Max() ?? anni.Where(x => x > annoA).Cast<int?>().Min();
    }

    /// <summary>Tasti ◀ ▶ del cursore: l'anno disponibile prima (passo &lt; 0) o dopo; null = già all'estremo.</summary>
    public static int? AnnoVicino(IEnumerable<int> anniDisponibili, int anno, int passo)
    {
        var anni = anniDisponibili.Distinct().ToList();
        return passo < 0
            ? anni.Where(x => x < anno).Cast<int?>().Max()
            : anni.Where(x => x > anno).Cast<int?>().Min();
    }

    private static Dictionary<int, IRigaAnnuale> PerVigneto(IEnumerable<IRigaAnnuale> righe, int anno) =>
        righe.Where(r => r.Anno == anno).GroupBy(r => r.IdVigneto).ToDictionary(g => g.Key, g => g.First());
}
