namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Una voce della legenda: <see cref="Classe"/> è l'indice della classe (o
/// <see cref="TematismiBigot.ClasseNessunDato"/> / <see cref="TematismiBigot.ClasseFuoriPerimetro"/>),
/// <see cref="Css"/> la forma del campione senza colore ("nd", "fp"). <see cref="Vuota"/>: classe della
/// scala senza vigneti, mostrata in grigio chiaro.
/// </summary>
public sealed record VoceLegenda(int Classe, string Testo, string? Colore, string Css, int Conteggio, bool Vuota);

/// <summary>
/// Voci della legenda (decisione del proprietario, task #50): le classi della scala restano sempre,
/// in grigio se nessun vigneto ci cade, così la scala si legge intera; le altre voci (classi
/// speciali, «Nessun dato», «Non valutato») compaiono solo se contano almeno un vigneto, o se sono
/// la voce scelta come filtro (per poterla togliere).
/// </summary>
public static class LegendaTematismo
{
    /// <param name="confronto">Regola del confronto fra due anni; null = parametro di un anno solo.</param>
    /// <param name="anno">Anno mostrato, per «Non valutato nel …».</param>
    public static IReadOnlyList<VoceLegenda> Voci(Tematismo tema, RegolaConfronto? confronto, int? anno,
        IReadOnlyDictionary<int, int> conteggi, int? classeScelta)
    {
        var classi = confronto is null ? tema.Classi : ConfrontoBigot.Classi(tema, confronto);
        var voci = new List<VoceLegenda>();
        void Aggiungi(int classe, string testo, string? colore, string css, bool scala)
        {
            var conteggio = conteggi.GetValueOrDefault(classe);
            if (scala || conteggio > 0 || classe == classeScelta)
            {
                voci.Add(new VoceLegenda(classe, testo, colore, css, conteggio, conteggio == 0));
            }
        }

        for (var i = 0; i < classi.Count; i++)
        {
            Aggiungi(i, classi[i].Testo, classi[i].Colore, "", !classi[i].Speciale);
        }

        Aggiungi(TematismiBigot.ClasseNessunDato,
            confronto is null ? TematismiBigot.EtichettaNessunDato : ConfrontoBigot.EtichettaNessunDato, null, "nd", false);
        Aggiungi(TematismiBigot.ClasseFuoriPerimetro,
            confronto is null ? tema.EtichettaSenzaRiga(anno) : ConfrontoBigot.EtichettaNonValutato, null, "fp", false);
        return voci;
    }

    /// <summary>Titolo della legenda e della sua linguetta: «Vigoria — 2025», «Vigoria — 2024 → 2025».</summary>
    public static string Titolo(Tematismo tema, int? anno, int? annoDa) =>
        tema.SenzaAnno || anno is null ? tema.Nome
        : annoDa is { } da ? $"{tema.Nome} — {da} → {anno}"
        : $"{tema.Nome} — {anno}";
}
