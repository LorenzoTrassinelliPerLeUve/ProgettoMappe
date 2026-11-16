using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Sezione 6, scheda dell'obiettivo produttivo: come lo script della pagina del portale, cambiare uno dei tre valori ricalcola
/// gli altri due con le piante e la superficie del vigneto (m²; kg/ha = kg ÷ (superficie ÷ 10.000)), arrotondati a 2 decimali
/// (<c>toFixed(2)</c>). Senza piante il valore per pianta non si tocca; senza superficie (il portale scriverebbe «Infinity»)
/// nemmeno quello per ettaro.
/// </summary>
public sealed class ModuloObiettivo
{
    public int IdVigneto { get; init; }
    public int Anno { get; init; }
    public decimal? NumeroPiante { get; init; }
    public decimal? Superficie { get; init; }
    public decimal? ObiettivoVigneto { get; set; }
    public decimal? ObiettivoPerPianta { get; set; }
    public decimal? ObiettivoPerEttaro { get; set; }

    public static ModuloObiettivo Da(ObiettivoDto o) => new()
    {
        IdVigneto = o.IdVigneto, Anno = o.Anno, NumeroPiante = o.NumeroPiante, Superficie = o.Superficie,
        ObiettivoVigneto = o.ObiettivoVigneto, ObiettivoPerPianta = o.ObiettivoPerPianta, ObiettivoPerEttaro = o.ObiettivoPerEttaro,
    };

    public ObiettivoSalvaDto Dto() => new(ObiettivoVigneto, ObiettivoPerPianta, ObiettivoPerEttaro);

    private decimal? Ettari => Superficie is > 0 ? Superficie / 10_000m : null;

    public void DaVigneto()
    {
        if (ObiettivoVigneto is not { } kg) return;
        if (NumeroPiante is > 0) ObiettivoPerPianta = Tondo(kg / NumeroPiante.Value);
        if (Ettari is { } ha) ObiettivoPerEttaro = Tondo(kg / ha);
    }

    public void DaPianta()
    {
        if (ObiettivoPerPianta is not { } kgPianta) return;
        var kg = kgPianta * (NumeroPiante ?? 0);
        ObiettivoVigneto = Tondo(kg);
        if (Ettari is { } ha) ObiettivoPerEttaro = Tondo(kg / ha);
    }

    public void DaEttaro()
    {
        if (ObiettivoPerEttaro is not { } kgEttaro || Ettari is not { } ha) return;
        var kg = kgEttaro * ha;
        ObiettivoVigneto = Tondo(kg);
        if (NumeroPiante is > 0) ObiettivoPerPianta = Tondo(kg / NumeroPiante.Value);
    }

    private static decimal Tondo(decimal valore) => Math.Round(valore, 2, MidpointRounding.AwayFromZero);
}
