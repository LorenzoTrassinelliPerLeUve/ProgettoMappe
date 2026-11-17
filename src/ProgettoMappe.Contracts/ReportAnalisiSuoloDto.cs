namespace ProgettoMappe.Contracts;

/// <summary>Report analisi del suolo (task #317): un'azienda con analisi di laboratorio nei rilievi 144.</summary>
public sealed record AziendaAnalisiSuoloDto(int IdAzienda, string RagioneSociale, int Analisi);

/// <summary>
/// Un rilievo 144 con analisi di laboratorio. <see cref="Valori"/>: le colonne numeriche di <c>dbo.Rilievo</c> col loro nome
/// del 4g (es. <c>Suolo_ph_h20</c>), solo quelle piene; i campi del report di Qlik li ricava il Web
/// (<c>ReportAnalisiSuolo/CampiSuolo</c>).
/// </summary>
public sealed record AnalisiSuoloDto(string IdRilievo, int IdVigneto, string NomeVigneto, string? Vigna, string? Vitigno,
    string? SubVigneto, int Anno, DateOnly? DataPrelievo, int? ProfonditaCm, string? CodicePunto, string? CodiceAnalisi,
    string? Laboratorio, int? IdTessitura, IReadOnlyDictionary<string, double> Valori);

/// <summary>Una riga di <c>dbo.Tessitura</c> (una per terna sabbia/limo/argilla): classe, parametri idropedologici, soglie.</summary>
public sealed record TessituraSuoloDto(int IdTessitura, double? Sabbia, double? Limo, double? Argilla, string? Descrizione,
    double? PuntoDiAppassimento, double? CapacitaDiCampo, double? AcquaDisponibileMmM, double? PesoSpecifico,
    double? ConducibilitaIdraulica, double? MinSO, double? MaxSO, double? MinN, double? MaxN, double? MinP, double? MaxP);

/// <summary>Tutte le analisi di un'azienda e le terne di tessitura che usano.</summary>
public sealed record AnalisiSuoloAziendaDto(int IdAzienda, string RagioneSociale, IReadOnlyList<AnalisiSuoloDto> Analisi,
    IReadOnlyList<TessituraSuoloDto> Tessiture);
