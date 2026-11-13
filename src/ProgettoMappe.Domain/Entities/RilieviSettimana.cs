namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un rilievo di un vigneto dell'azienda per il report settimanale (task #213), con le misure delle slide. Da 4Grapes
/// arrivano solo vigneto, data, tipo e BBCH. <c>PresenzaAvversita</c>, <c>PercDanno</c> e <c>LunghezzaGermogli</c> non
/// sono ancora nello snapshot (arrivano col lavoro del dba, task #212): oggi restano null.
/// </summary>
public sealed record RilievoSettimana(int IdVigneto, DateTime Data, int TipoRilievo, int? Bbch,
    int? Organo = null, int? EtaEpoca = null, double? NrOrgani = null, double? PercDiffusione = null, double? PercIntensita = null,
    double? NrFormeMobiliNidi = null, double? CatturePerTrappola = null, int? StatoIdrico = null,
    int? PresenzaAvversita = null, double? PercDanno = null, double? LunghezzaGermogli = null);

/// <summary>
/// I rilievi di un periodo per il report settimanale, con i nomi dei tipi. <c>Fonte</c> = «snapshot» (file notturno del
/// dba, <c>Snapshot</c> = quando è stato costruito) o «4grapes» (lettura diretta, solo tipo e data).
/// </summary>
public sealed record RilieviSettimanaLetti(
    IReadOnlyList<RilievoSettimana> Rilievi,
    IReadOnlyDictionary<int, string> NomiTipi,
    string Fonte,
    DateTimeOffset? Snapshot);
