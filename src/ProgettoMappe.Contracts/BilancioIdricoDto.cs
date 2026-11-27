using System.Text.Json;

namespace ProgettoMappe.Contracts;

/// <summary>
/// Una modifica della configurazione del DSS Bilancio Idrico (task #333): il record del tipo (<c>settore</c>, <c>quota</c>,
/// <c>impianto</c>, <c>shift</c>, <c>settore-shift</c>, <c>stile</c>, <c>irrigazione</c>, <c>modello</c>) in <see cref="Dati"/>.
/// <see cref="Elimina"/> = toglie la voce (resta nello storico). <see cref="Autore"/> lo mette il Web dall'utente entrato.
/// </summary>
public sealed record SalvaVoceBilancioDto(string? Autore, JsonElement Dati, bool Elimina = false, string? Nota = null);

/// <summary>
/// Task #384: più voci per vigneto dello stesso tipo (<c>stile</c> o <c>calibrazione</c>) in un salvataggio solo, tutto o niente:
/// lo stesso stile a 37 vigneti con un clic.
/// </summary>
public sealed record SalvaVociBilancioDto(string? Autore, IReadOnlyList<JsonElement> Dati, bool Elimina = false, string? Nota = null);

/// <summary>Esito del salvataggio in blocco: quante voci scritte.</summary>
public sealed record EsitoBloccoBilancioDto(int Salvate);

/// <summary>Una versione di una voce della configurazione, con chi e quando.</summary>
public sealed record VoceBilancioDto(string Tipo, string Chiave, int Versione, JsonElement? Dati, string Autore, DateTime CreatoIlUtc,
    string? Nota);

// Fase 3 (task #335, punto D): i record della configurazione come li legge e scrive la pagina. Stessi nomi dei campi dei
// record dell'archivio (Infrastructure/BilancioIdrico/ConfigurazioneBilancio.cs): il JSON passa così com'è in
// VoceBilancioDto.Dati e SalvaVoceBilancioDto.Dati. Numero 0 = nuovo (lo dà l'Api).

public sealed record SettoreBilancioDto(int IdSettore, int IdEnte, string Nome, double? SuperficieM2 = null, bool Attivo = true);

public sealed record QuotaBilancioDto(int IdSettore, int IdVigneto, double Percentuale);

public sealed record ImpiantoBilancioDto(int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl, int? IdTipologiaImpianto,
    double? DistanzaGocciolatoriM = null, double? DistanzaAliM = null, double? PortataLh = null, double? TassoMmOra = null,
    double? EfficienzaReale = null);

public sealed record ShiftBilancioDto(int IdShift, int IdEnte, string Nome, bool Attivo = true);

public sealed record SettoreShiftBilancioDto(int IdShift, int IdSettore, DateOnly ValidoDal, DateOnly? ValidoAl);

public sealed record StileBilancioDto(int IdVigneto, int IdStileVino, DateOnly ValidoDal, DateOnly? ValidoAl);

public sealed record IrrigazioneBilancioDto(int IdEvento, int IdEnte, DateOnly Data, int? IdSettore, int? IdShift,
    double? DurataOre = null, double? VolumeMm = null, double? Efficienza = null, double? VolumeUtileMm = null);

// Fase 3 (task #335): risultati del ricalcolo notturno per le pagine.

/// <summary>
/// Il calcolo da cui vengono i numeri: quando, stagione, motore e parametri. <see cref="ErroreDopo"/> = l'ultimo tentativo è
/// fallito dopo questo calcolo (i numeri sono quelli di prima); <see cref="RicalcoloInAttesa"/> = una modifica della
/// configurazione aspetta il ricalcolo.
/// </summary>
public sealed record CalcoloBilancioDto(DateTime CalcolatoIlUtc, int Anno, DateOnly Da, DateOnly A, string VersioneMotore,
    string VersioneParametri, int DurataMs, string? ErroreDopo, DateTime? ErroreIlUtc, bool RicalcoloInAttesa);

/// <summary>Un'anomalia dei dati, spiegata in italiano, con i vigneti che riguarda (vuoto = tutta l'azienda).</summary>
public sealed record AnomaliaBilancioDto(string Codice, string Testo, IReadOnlyList<int> Vigneti);

/// <summary>Il suggerimento di un giorno (oggi e i prossimi): codice del motore (IRRIGARE, NON_IRRIGARE, N_A, DATO_NON_VALIDO).</summary>
public sealed record GiornoSuggerimentoDto(DateOnly Data, string Suggerimento, double? PdlwpMpa, double IrrigazioneSuggeritaMm);

/// <summary>Lo stato idrico di oggi di un vigneto, per la pagina dell'azienda.</summary>
public sealed record StatoVignetoBilancioDto(
    int IdVigneto,
    double? PdlwpMpa,
    double? SogliaMpa,
    string Suggerimento,
    double? DeficitMm,
    double AwMm,
    double? AwcMm,
    double? Ks,
    double IrrigazioneSuggeritaMm,
    int Priorita,
    bool CriticitaPdlwp,
    bool CriticitaDeficit,
    IReadOnlyList<int> Settori,
    bool HaStile,
    IReadOnlyList<string> Anomalie,
    IReadOnlyList<GiornoSuggerimentoDto> Prossimi);

/// <summary>Un settore oggi: mm e ore suggerite (ore vuote senza impianto o senza tasso).</summary>
public sealed record SettoreOggiBilancioDto(int IdSettore, string Nome, double IrrigazioneSuggeritaMm, double? IrrigazioneSuggeritaOre,
    int Priorita, IReadOnlyList<int> Vigneti);

/// <summary>Uno shift oggi: ore di comando.</summary>
public sealed record ShiftOggiBilancioDto(int IdShift, string Nome, double? OreComando, int SettoriCoinvolti);

/// <summary>La pagina «Bilancio idrico» di un'azienda (task #335, punto C). <see cref="Calcolo"/> null = mai calcolata.</summary>
public sealed record BilancioAziendaDto(
    int IdEnte,
    DateOnly Oggi,
    CalcoloBilancioDto? Calcolo,
    IReadOnlyList<StatoVignetoBilancioDto> Vigneti,
    IReadOnlyList<SettoreOggiBilancioDto> Settori,
    IReadOnlyList<ShiftOggiBilancioDto> Shift,
    IReadOnlyList<AnomaliaBilancioDto> Anomalie,
    int? CambiRispettoAComeOggi = null);

/// <summary>Un giorno del bilancio di un vigneto: tutte le grandezze («non deve essere una scatola nera»).</summary>
public sealed record GiornoBilancioDto(
    DateOnly Data,
    bool Previsione,
    double? Et0Mm,
    double? PioggiaMm,
    double IrrigazioneMm,
    int? Bbch,
    int? Fase,
    double? Kc,
    double? Ks,
    double? EtaMm,
    double AwMm,
    double? AwcMm,
    double? Faw,
    double? PdlwpMpa,
    double? SogliaMpa,
    double? DeficitMm,
    double IrrigazioneSuggeritaMm,
    double PerditeMm,
    string Suggerimento);

/// <summary>Un potenziale misurato in campo (rilievi dello stato idrico), da sovrapporre alla curva prevista.</summary>
public sealed record PotenzialeMisuratoDto(DateOnly Data, double Mpa, string Tipo);

/// <summary>La sezione «Bilancio idrico» della scheda del vigneto (task #335, punto B).</summary>
public sealed record BilancioVignetoDto(
    int IdVigneto,
    DateOnly Oggi,
    CalcoloBilancioDto? Calcolo,
    IReadOnlyList<GiornoBilancioDto> Giorni,
    IReadOnlyList<string> Anomalie,
    IReadOnlyList<int> Settori,
    IReadOnlyList<PotenzialeMisuratoDto> Misurati,
    string? NotaMisurati,
    SuoloBilancioDto? Suolo = null,
    MeteoVicinoBilancioDto? MeteoVicino = null);

/// <summary>
/// Task #383: il vigneto ha preso il meteo da un vicino (di un'altra azienda) nei giorni in cui la sua azienda non l'aveva.
/// <c>IdVignetoFonte</c>/<c>DistanzaKm</c> = il vicino usato più spesso, <c>Fonti</c> = quanti vicini diversi.
/// </summary>
public sealed record MeteoVicinoBilancioDto(int IdVignetoFonte, double DistanzaKm, int Giorni, int Fonti, DateOnly Dal, DateOnly Al);

/// <summary>
/// Task #349 (Matteo, s8): i valori usati per l'AWC e lo stile del vigneto, con da dove vengono (<c>analisi</c>, <c>settore</c>,
/// <c>calibrazione</c>, <c>rilievo</c>, <c>ripiego</c>, <c>configurazione</c>, <c>target</c>; null = manca).
/// </summary>
public sealed record SuoloBilancioDto(
    double? AcquaDisponibileMmM,
    string? FonteAcqua,
    double? ProfonditaSuoloM,
    string? FonteProfondita,
    double? ScheletroPerc,
    string? FonteScheletro,
    double? ProfonditaRadiciM,
    double? LarghezzaRadiciM,
    string? FonteRadici,
    double? DistanzaFileM,
    double FattoreRadici,
    double? AwcMm,
    string? FonteAwc,
    int VignetiNelSettore,
    int? IdStileVino,
    string? FonteStile,
    int? IdTargetConcimazione);

/// <summary>La calibrazione di un vigneto come la scrive la pagina (stessi campi di CalibrazioneVigneto dell'archivio).</summary>
public sealed record CalibrazioneBilancioDto(int IdVigneto, double? ScheletroPerc = null, double? ProfonditaRadiciM = null,
    double? LarghezzaRadiciM = null, double? AwcMm = null);

/// <summary>Stato del giro notturno.</summary>
public sealed record StatoRicalcoloBilancioDto(bool InCorso, DateTime? InizioUtc, DateTime? FineUtc, int Aziende, int Fatte, int Fallite,
    int AziendeCalcolate, DateTime? UltimoCalcoloUtc);
