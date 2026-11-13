namespace ProgettoMappe.Contracts;

/// <summary>
/// I rilievi di una settimana per il report settimanale (task #213): dati grezzi, settimane e calcoli li fa il Web in
/// <c>ReportSettimanale/</c>. <c>Fonte</c> = «snapshot» o «4grapes» (solo tipo, data e BBCH, tipo 0 = fase da
/// <c>VistaRilievoBBCH</c>).
/// </summary>
public sealed record RilieviSettimanaDto(
    IReadOnlyList<RilievoSettimanaDto> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot);

public sealed record RilievoSettimanaDto(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch);
