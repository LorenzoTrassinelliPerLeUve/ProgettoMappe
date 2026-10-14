using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Filtri;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Un tematismo calcolato sui vigneti: classe di ogni vigneto, colori delle classi (l'indice è la
/// classe), conteggi della legenda, valore per vigneto come nell'elenco e, nel confronto, il riepilogo.
/// </summary>
public sealed record TematismoCalcolato(
    ClassificazioneTematismo Classificazione,
    IReadOnlyList<string> Colori,
    Dictionary<int, int> Conteggi,
    Dictionary<int, ValoreElenco> Valori,
    RiepilogoConfronto? Riepilogo);

/// <summary>Lo stesso calcolo per la mappa e per il layout di stampa (task #90).</summary>
public static class CalcoloTematismo
{
    /// <param name="confronto">Regola del confronto fra <paramref name="annoDa"/> e <paramref name="anno"/>; null = un anno solo.</param>
    /// <param name="visibili">Vigneti di Vigna/Vitigno sulla mappa (con geometria): su questi si contano le voci della legenda.</param>
    public static TematismoCalcolato Calcola(Tematismo tema, RegolaConfronto? confronto, IReadOnlyList<IRigaVigneto> righe,
        int? anno, int? annoDa, IEnumerable<int> visibili)
    {
        ClassificazioneTematismo classificazione;
        List<string> colori;
        Dictionary<int, ValoreElenco> valori = [];
        RiepilogoConfronto? riepilogo = null;
        var idVisibili = visibili.ToList();

        if (confronto is not null && annoDa is { } da && anno is { } a)
        {
            var (variazioni, conRiga) = ConfrontoBigot.Variazioni(tema, confronto, righe, da, a);
            classificazione = new ClassificazioneTematismo(variazioni.ToDictionary(v => v.Key, v => v.Value.Classe), [], conRiga);
            colori = ConfrontoBigot.Classi(tema, confronto).Select(c => c.Colore).ToList();
            riepilogo = ConfrontoBigot.Riepilogo(variazioni, idVisibili);
            // Nell'elenco la variazione, ordinabile dal più migliorato.
            valori = variazioni.ToDictionary(v => v.Key,
                v => new ValoreElenco(ConfrontoBigot.TestoDelta(tema, v.Value.Delta), colori[v.Value.Classe], v.Value.Miglioramento));
        }
        else
        {
            classificazione = TematismiBigot.ClassiPerVigneto(tema, righe, anno);
            colori = tema.Classi.Select(c => c.Colore).ToList();
            // Il valore come nella scheda (stesso testo e colore), ordinabile per valore.
            foreach (var riga in righe.Where(r => tema.SenzaAnno || r is IRigaAnnuale ra && ra.Anno == anno))
            {
                if (SchedaBigot.Valore(tema, riga) is { Testo: not SchedaBigot.Vuoto } valore)
                {
                    valori[riga.IdVigneto] = new ValoreElenco(valore.Testo, valore.Colore, tema.ValoreConfronto(riga));
                }
            }
        }

        return new TematismoCalcolato(classificazione, colori, TematismiBigot.Conteggi(classificazione, idVisibili), valori, riepilogo);
    }
}
