namespace ProgettoMappe.Contracts;

/// <summary>
/// I rilievi di un periodo per il report settimanale (task #213): dati grezzi, settimane e calcoli li fa il Web in
/// <c>ReportSettimanale/</c>. <c>Fonte</c> = «snapshot» o «4grapes» (solo tipo, data e BBCH, tipo 0 = fase da
/// <c>VistaRilievoBBCH</c>).
/// </summary>
public sealed record RilieviSettimanaDto(
    IReadOnlyList<RilievoSettimanaDto> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot);

/// <summary>Un rilievo con le misure delle slide; le ultime tre non sono ancora nello snapshot (null).</summary>
public sealed record RilievoSettimanaDto(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch,
    int? Organo = null, int? EtaEpoca = null, double? NrOrgani = null, double? PercDiffusione = null, double? PercIntensita = null,
    double? NrFormeMobiliNidi = null, double? CatturePerTrappola = null, int? StatoIdrico = null,
    int? PresenzaAvversita = null, double? PercDanno = null, double? LunghezzaGermogli = null);
