namespace ProgettoMappe.Domain.Entities;

/// <summary>
/// Un vigneto attivo dell'azienda per il report fine stagione (task #201): come la tabella Vigneto dello
/// script Qlik (Estirpo = 0, Carico = 1). <see cref="DatiMeteoStorico"/> è il flag di 4Grapes che tutte le
/// misure meteo di Qlik usano come filtro (dba, 2026-11-12: significato non documentato e poco affidabile,
/// ma per la parità si usa com'è).
/// </summary>
public sealed record VignetoMeteoReport(int IdVigneto, string Nome, int? IdVigna, string? Vigna, bool DatiMeteoStorico);

/// <summary>Un giorno di <c>MeteoGiornaliero</c> (o dello storico) per un vigneto, valori grezzi di 4Grapes.</summary>
public sealed record GiornoMeteoReport(
    int IdVigneto,
    DateOnly Data,
    bool Previsione,
    decimal? Precipitazione,
    decimal? TemperaturaMin,
    decimal? TemperaturaMed,
    decimal? TemperaturaMax);

/// <summary>
/// Il meteo letto per un'azienda. <see cref="StoricoDisponibile"/> è falso finché Mappe non legge
/// <c>4g_Storico.dbo.MeteoGiornaliero_Storico</c> (1984–2025): senza, i confronti con gli ultimi 10 e 30 anni
/// restano vuoti.
/// </summary>
public sealed record MeteoReportLetto(
    IReadOnlyList<VignetoMeteoReport> Vigneti,
    IReadOnlyList<GiornoMeteoReport> Giorni,
    bool StoricoDisponibile,
    DateTimeOffset? Snapshot = null);

/// <summary>Suolo e riserva idrica di un vigneto (<c>VistaStatistichePotenzialeIdricoVigneti</c>, foglio 42 di Qlik).</summary>
public sealed record SuoloVigneto(int IdVigneto, string Nome, string? Vigna, double? CapacitaDiCampo, double? AcquaDisponibile,
    double? PuntoDiAppassimento, double? Profondita, double? Pietrosita, double? AwcRealeVigneto, double? AwcRealeVigna,
    double? Conducibilita);

/// <summary>Una riga di scottature: da <c>VistaScottature</c> (fonte «vista») o da un rilievo di tipo 32 (organo nel campo).</summary>
public sealed record ScottaturaVigneto(int IdVigneto, int Anno, int? Organo, double? Intensita, double? Diffusione);

/// <summary>Dati di campo del report fine stagione dallo snapshot (tappa 3): suolo e scottature, con la superficie grezza.</summary>
public sealed record CampoReportLetto(
    IReadOnlyDictionary<int, double?> Superfici,
    IReadOnlyList<SuoloVigneto> Suolo,
    IReadOnlyList<ScottaturaVigneto> ScottatureVista,
    IReadOnlyList<ScottaturaVigneto> ScottatureRilievi);

/// <summary>
/// Un controllo sui dati del dba (tabella <c>_controlli</c> dello snapshot, <c>controlli.md</c>): solo le righe
/// del dettaglio dell'azienda (colonna «Azienda» = ragione sociale), oppure tutte se il controllo non ha quella colonna.
/// </summary>
public sealed record ControlloReport(
    string Codice,
    string Titolo,
    string? Perche,
    string Gravita,
    int? Annata,
    IReadOnlyList<string> Colonne,
    IReadOnlyList<IReadOnlyList<string>> Righe);
