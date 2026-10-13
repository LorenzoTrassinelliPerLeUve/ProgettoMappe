namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Rischio di oidio e peronospora di un vigneto in un anno (vista 4Grapes
/// <c>VistaStatisticheMappaVigneti</c>): interi 1–9 arrotondati da 4Grapes, più alto = più rischio.
/// </summary>
public sealed record RischioVigneto(int Anno, int IdVigneto, decimal? Oidio, decimal? Peronospora);

/// <summary>
/// Punteggio di vigoria di un vigneto in un anno (vista <c>VistaStatisticheVigoriaMappaVigneti</c>):
/// 1–10, l'equilibrio è al centro (circa 5), entrambi gli estremi sono un problema.
/// </summary>
public sealed record VigoriaVigneto(int Anno, int IdVigneto, decimal? Punteggio);

/// <summary>
/// Potenziale idrico di un vigneto, senza anno (vista <c>VistaStatistichePotenzialeIdricoMappaVigneti</c>):
/// acqua disponibile nel suolo in mm, più alto = più riserva.
/// </summary>
public sealed record PotenzialeIdricoVigneto(int IdVigneto, decimal? PotenzialeIdrico);

/// <summary>Le tre statistiche di 4Grapes per un insieme di vigneti.</summary>
public sealed record StatisticheVigneti(
    IReadOnlyList<RischioVigneto> Rischi,
    IReadOnlyList<VigoriaVigneto> Vigoria,
    IReadOnlyList<PotenzialeIdricoVigneto> PotenzialeIdrico)
{
    public static readonly StatisticheVigneti Vuote = new([], [], []);
}
