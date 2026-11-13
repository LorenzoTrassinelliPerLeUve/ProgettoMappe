namespace ProgettoMappe.Domain.Entities;

/// <summary>Un rilievo di un vigneto dell'azienda nella settimana del report settimanale (task #213).</summary>
public sealed record RilievoSettimana(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch);

/// <summary>
/// I rilievi di un periodo per il report settimanale, con i nomi dei tipi. <c>Fonte</c> = «snapshot» (file notturno del
/// dba, <c>Snapshot</c> = quando è stato costruito) o «4grapes» (lettura diretta, solo tipo e data).
/// </summary>
public sealed record RilieviSettimanaLetti(
    IReadOnlyList<RilievoSettimana> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot);
