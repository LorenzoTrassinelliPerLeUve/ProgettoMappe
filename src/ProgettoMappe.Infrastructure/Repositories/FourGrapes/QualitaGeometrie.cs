namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Sezione "QualitaGeometrie" della configurazione dell'Api.</summary>
public sealed class QualitaGeometrieOptions
{
    public const string Sezione = "QualitaGeometrie";

    public const double SogliaPredefinitaEttari = 500;

    /// <summary>
    /// Area massima (ha) di un poligono attendibile. Oltre, il vigneto resta in elenco ma senza
    /// forma sulla mappa (richiesta del proprietario, 2026-10-13: vigneti 16792/16793 da ~30.000
    /// ha). ≤ 0 = nessun controllo.
    /// </summary>
    public double SogliaEttari { get; set; } = SogliaPredefinitaEttari;
}

/// <summary>
/// Controllo esplicito sulla qualità dei poligoni di 4Grapes: un'area oltre la soglia non è un
/// vigneto ma un errore di dati (poligono sbagliato, coordinate errate). Non si corregge: si
/// esclude dalla mappa e si segnala.
/// </summary>
public static class QualitaGeometrie
{
    /// <summary>True se l'area (m²) supera la soglia (ha); area mancante o soglia ≤ 0 → false.</summary>
    public static bool Enorme(double? areaPoligonoMq, double sogliaEttari)
    {
        return sogliaEttari > 0
               && areaPoligonoMq is { } area
               && double.IsFinite(area)
               && area / (double)SuperficieFourGrapes.MetriQuadratiPerEttaro > sogliaEttari;
    }
}
