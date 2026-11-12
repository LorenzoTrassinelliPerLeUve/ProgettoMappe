namespace ProgettoMappe.Contracts;

/// <summary>
/// Una riga di <c>VistaIndicePerParametro</c> (vigneto) o <c>VistaIndicePerParametro_Sub</c> (sottovigneto,
/// <see cref="IdSubVigneto"/> valorizzato), così come nello snapshot notturno (task #189). I numeri
/// sono quelli di 4Grapes, senza arrotondamenti: le formule della pagina sono quelle di Qlik.
/// </summary>
public record RigaIndiceParametroDto(
    int Anno,
    int IdVigneto,
    int? IdSubVigneto,
    string IdParametro,
    double? Min,
    double? Max,
    double? Avg,
    double? Valore,
    double? Score,
    double? Punteggio);

public record VignaReportDto(int Id, string Nome);

public record VitignoReportDto(int Id, string Nome);

/// <summary>Vigneto dell'anagrafica 4Grapes. <see cref="Superficie"/>: <c>Vigneto.Superficie</c> grezza (di solito m²), come in Qlik.</summary>
public record VignetoReportDto(int Id, string Nome, int? IdVigna, int? IdVitigno, double? Superficie, int? AnnoImpianto);

public record SottovignetoReportDto(int Id, string Nome, int IdVigneto);

/// <summary>
/// Dati del report «Indice Bigot» di un'azienda, letti dallo snapshot notturno (mai da 4Grapes a ogni
/// richiesta). <see cref="Pronto"/> false: nessuno snapshot ancora disponibile. <see cref="Snapshot"/>:
/// quando sono stati letti da 4Grapes (UTC).
/// </summary>
public record IndiceBigotReportDto(
    bool Pronto,
    DateTimeOffset? Snapshot,
    int IdAzienda,
    string NomeAzienda,
    IReadOnlyList<VignaReportDto> Vigne,
    IReadOnlyList<VitignoReportDto> Vitigni,
    IReadOnlyList<VignetoReportDto> Vigneti,
    IReadOnlyList<SottovignetoReportDto> Sottovigneti,
    IReadOnlyList<RigaIndiceParametroDto> Righe);

/// <summary>
/// Stato dello snapshot dell'Indice Bigot: <see cref="Snapshot"/> = ora della lettura da 4Grapes in uso,
/// <see cref="UltimoErrore"/> = ultimo tentativo fallito (null se l'ultimo è riuscito), <see cref="Righe"/>
/// = righe per parametro (vigneti + sottovigneti).
/// </summary>
public record StatoSnapshotIndiceDto(bool Pronto, bool InCaricamento, DateTimeOffset? Snapshot, DateTimeOffset? UltimoErrore, int Righe);
