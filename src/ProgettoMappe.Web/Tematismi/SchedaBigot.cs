using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>Un valore della scheda: il testo (mai vuoto: "–" se manca) e il colore della sua voce di legenda.</summary>
public sealed record ValoreScheda(string Testo, string? Colore);

/// <summary>Una riga della tabella BIGOT della scheda: il parametro, il valore nell'anno scelto e negli anni precedenti.</summary>
public sealed record RigaSchedaBigot(Tematismo Tema, string Etichetta, ValoreScheda Valore, IReadOnlyList<ValoreScheda> Precedenti);

/// <summary>
/// Tabella BIGOT nella scheda del vigneto: una riga per tematismo (punteggio e 9 parametri), con lo
/// stesso testo e lo stesso colore che il parametro ha in legenda e sulla mappa; sotto, gli anni
/// precedenti disponibili per l'azienda.
/// </summary>
public static class SchedaBigot
{
    public const int AnniPrecedenti = 3;

    public const string Vuoto = "–";

    /// <summary>Gli ultimi <see cref="AnniPrecedenti"/> anni disponibili prima di <paramref name="anno"/>, dal più recente.</summary>
    public static IReadOnlyList<int> Precedenti(IEnumerable<int> anniDisponibili, int anno) =>
        anniDisponibili.Where(a => a < anno).Distinct().OrderByDescending(a => a).Take(AnniPrecedenti).ToList();

    public static ValoreScheda Valore(Tematismo tema, IRigaVigneto? riga)
    {
        if (riga is null || tema.TestoValore(riga) is not { } testo)
        {
            return new ValoreScheda(Vuoto, null);
        }

        var classe = tema.Classifica(riga);
        return new ValoreScheda(testo, classe is { } c && c >= 0 && c < tema.Classi.Count ? tema.Classi[c].Colore : null);
    }

    public static IReadOnlyList<RigaSchedaBigot> Righe(IEnumerable<IndiceBigotDto> indici, int idVigneto, int anno, IReadOnlyList<int> precedenti)
    {
        var perAnno = indici.Where(r => r.IdVigneto == idVigneto)
            .GroupBy(r => r.Anno)
            .ToDictionary(g => g.Key, g => g.First());
        var riga = perAnno.GetValueOrDefault(anno);

        return TematismiBigot.Tutti
            .Select(t => new RigaSchedaBigot(t, Etichetta(t), Valore(t, riga),
                precedenti.Select(a => Valore(t, perAnno.GetValueOrDefault(a))).ToList()))
            .ToList();
    }

    /// <summary>
    /// Righe «Rischi, vigoria e suolo» della scheda: un valore per tematismo sulle statistiche, con
    /// lo stesso testo e colore della legenda. Rischi e vigoria dell'anno scelto, potenziale idrico
    /// senza anno. Vuoto se il vigneto non ha nessuno di questi dati.
    /// </summary>
    public static IReadOnlyList<(string Etichetta, ValoreScheda Valore)> RigheStatistiche(
        IEnumerable<Tematismo> tematismi, StatisticheAziendaDto statistiche, int idVigneto, int? anno)
    {
        IRigaVigneto? Riga(FonteTematismo fonte) => fonte switch
        {
            FonteTematismo.Rischi => statistiche.Rischi.FirstOrDefault(r => r.IdVigneto == idVigneto && r.Anno == anno),
            FonteTematismo.Vigoria => statistiche.Vigoria.FirstOrDefault(r => r.IdVigneto == idVigneto && r.Anno == anno),
            FonteTematismo.PotenzialeIdrico => statistiche.PotenzialeIdrico.FirstOrDefault(r => r.IdVigneto == idVigneto),
            _ => null,
        };

        var righe = tematismi.Select(t => (t.Nome, Valore(t, Riga(t.Fonte)))).ToList();
        return righe.All(r => r.Item2.Colore is null && r.Item2.Testo == Vuoto) ? [] : righe;
    }

    /// <summary>Nella scheda basta "Punteggio": il titolo della tabella dice già "Indice BIGOT".</summary>
    private static string Etichetta(Tematismo tema) => tema.Id == "punteggio" ? "Punteggio" : tema.Nome;
}
