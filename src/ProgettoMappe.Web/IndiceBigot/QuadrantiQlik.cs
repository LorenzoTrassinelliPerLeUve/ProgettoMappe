namespace ProgettoMappe.Web.IndiceBigot;

/// <summary>Una fascia colorata del quadrante, da <see cref="Da"/> ad <see cref="A"/> nelle unità del valore.</summary>
public sealed record FasciaQuadrante(double Da, double A, string Colore);

/// <summary>Scala di un quadrante: minimo, massimo e fasce, dal basso verso l'alto.</summary>
public sealed record ScalaQuadrante(double Minimo, double Massimo, IReadOnlyList<FasciaQuadrante> Fasce)
{
    /// <summary>Posizione del valore sulla scala, fra 0 e 1 (fuori scala = all'estremo).</summary>
    public double Frazione(double valore) => Math.Clamp((valore - Minimo) / (Massimo - Minimo), 0, 1);
}

/// <summary>
/// Scale e fasce dei nove gauge dell'app Qlik «INDICE BIGOT - REPORT» (fogli 5–7), lette dal boss con
/// GetProperties il 12/11/2026 (task #195, <c>quadranti-qlik.json</c> in
/// <c>~/progetti/lavori/2026-11-12-pilota-indice-bigot</c>): <c>measureAxis.min/max</c> e
/// <c>segmentInfo</c> (limiti ordinati, colori nell'ordine salvato). Copiate qui perché Qlik non si legge
/// a ogni richiesta. Unica differenza: la sanità, che Qlik tiene in 0–1, qui è in percentuale come nella pagina.
/// </summary>
public static class QuadrantiQlik
{
    private static ScalaQuadrante Scala(double minimo, double massimo, params (double A, string Colore)[] fasce)
    {
        var elenco = new List<FasciaQuadrante>();
        var da = minimo;
        foreach (var (a, colore) in fasce)
        {
            elenco.Add(new FasciaQuadrante(da, a, colore));
            da = a;
        }
        return new ScalaQuadrante(minimo, massimo, elenco);
    }

    public static readonly IReadOnlyDictionary<string, ScalaQuadrante> Scale =
        new Dictionary<string, ScalaQuadrante>(StringComparer.OrdinalIgnoreCase)
        {
            ["SFE"] = Scala(0, 5, (0.945, "#ee3700"), (1.905, "#cfc804"), (2.445, "#00ba20"), (3.075, "#cfc804"), (3.65, "#cf7500"), (5, "#f00b00")),
            ["SFEKg"] = Scala(0, 5, (0.64, "#ee3700"), (1.415, "#cfc804"), (1.995, "#00ba20"), (2.75, "#cfc804"), (3.65, "#cf7500"), (5, "#f00b00")),
            ["PRODUZIONE"] = Scala(0, 5, (0.645, "#f21d00"), (1.155, "#ffd300"), (1.695, "#00ea24"), (2.6, "#ffd300"), (3.35, "#ff8200"), (5, "#f21d00")),
            ["AMPELOPATIE"] = Scala(50, 100, (78.05, "#e50009"), (91, "#f15e00"), (96, "#fedf00"), (100, "#44e300")),
            ["VIGORIA"] = Scala(1, 7, (2.176, "#e50009"), (3.652, "#f15e00"), (5, "#fedf00"), (6.154, "#44e300"), (6.58, "#ffac00"), (7, "#ff1803")),
            ["REGIME IDRICO"] = Scala(0, 5, (0.5, "#e50009"), (1.095, "#ffac00"), (2.49, "#01fe00"), (4, "#ffac00"), (5, "#ff1803")),
            ["ETA VIGNETO"] = Scala(3, 100, (7.656, "#e50009"), (19.684, "#ffac00"), (36.659, "#ffea00"), (62.267, "#44e300"), (100, "#298800")),
            ["MORFOLOGIA"] = Scala(1, 6, (1.93, "#e50009"), (2.94, "#f15e00"), (4.215, "#fedf00"), (5.135, "#44e300"), (6, "#298800")),
            ["BIODIVERSITA"] = Scala(0, 9, (1.17, "#e50009"), (3.033, "#f15e00"), (5.238, "#fedf00"), (7.533, "#44e300"), (9, "#298800")),
        };
}
