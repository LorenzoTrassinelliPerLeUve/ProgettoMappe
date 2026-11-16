namespace ProgettoMappe.Contracts;

/// <summary>
/// «Dashboard» del portale Nutrizione (DashboardNutrizioneController, task #293): le righe di Utente, Vigneto, Rilievo e
/// Collezione lette da <c>sys.partitions</c> come <c>CountFast</c>. null = Mappe non vede quella tabella (Collezione senza GRANT).
/// </summary>
public record DashboardNutrizioneDto(long? Utenti, long? Vigneti, long? Rilievi, long? Collezioni);

/// <summary>
/// Una riga della lista «Vigneti» del portale (VignetiController.ListaAjax), con le stesse 7 colonne. <paramref name="Modificabile"/>
/// = il pulsante «modifica» del portale (MasterUser o azienda con aggiornamenti abilitati, task #296).
/// </summary>
public record VignetoPortaleDto(int IdVigneto, int IdEnte, string Ente, string? Vigna, string Vigneto, string? Regione,
    string? Provincia, string? Comune, bool Modificabile = false);

/// <summary>
/// I campi modificabili della scheda del vigneto (<c>VignetiController.Form</c> POST, task #296), coi nomi delle colonne di
/// <c>dbo.Vigneto</c> (i nomi vanno anche negli errori per campo). Le coordinate non ci sono: in Mappe restano da leggere.
/// </summary>
public record VignetoSalvaDto(
    string? NomeVigneto,
    int? Vitigno_idVitigno,
    int? AnnoImpianto,
    decimal? SullaFila,
    decimal? TraFila,
    bool Estirpo,
    DateTime? DataEstirpo,
    bool Carico,
    int? IdTipologiaLinea,
    int? IdRischio,
    bool ElaboraMeteo,
    int? PesoMedioGrappolo,
    string? Note,
    int? Irrigazione_idIrrigazione,
    int? Giacitura_idGiacitura,
    int? FormaAllevamento_idFormaAllevamento,
    string? Portainnesto_idPortainnesto,
    string? Clone_IdClone,
    int? Conduzione_idConduzione,
    string? Esposizione,
    int? QuotaSLM,
    int? Pendenza,
    int? Orientamento,
    int? NumeroPianteContate,
    decimal? SuperficieDichiarata,
    decimal? SuperficieMisurata);

/// <summary>Una voce di una tendina della scheda vigneto: Id come testo (Portainnesto e Clone hanno chiavi di testo).</summary>
public record VoceVignetoDto(string Id, string Testo, int? IdVitigno = null);

/// <summary>
/// Le tendine della scheda vigneto. <paramref name="NonLeggibili"/> = le tabelle che Mappe non può ancora leggere (manca il
/// GRANT della parte A dello script del dba): la loro tendina mostra solo il valore attuale.
/// </summary>
public record MenuVignetoDto(
    IReadOnlyList<VoceVignetoDto> Vitigni,
    IReadOnlyList<VoceVignetoDto> Irrigazioni,
    IReadOnlyList<VoceVignetoDto> Giaciture,
    IReadOnlyList<VoceVignetoDto> FormeAllevamento,
    IReadOnlyList<VoceVignetoDto> Portainnesti,
    IReadOnlyList<VoceVignetoDto> Cloni,
    IReadOnlyList<VoceVignetoDto> Conduzioni,
    IReadOnlyList<VoceVignetoDto> TipologieLinea,
    IReadOnlyList<VoceVignetoDto> Rischi,
    IReadOnlyList<string> NonLeggibili);

/// <summary>
/// La scheda del vigneto: i campi modificabili, quelli solo da leggere (azienda, vigna, coordinate, comune di
/// <c>VignetoMaps</c>) e cosa può cambiare chi la apre (<paramref name="Modificabile"/>, linea e rischio, «Elabora meteo»).
/// </summary>
public record VignetoSchedaDto(
    int IdVigneto,
    int IdEnte,
    string Ente,
    string? Vigna,
    VignetoSalvaDto Dati,
    decimal? Latitudine,
    decimal? Longitudine,
    string? Regione,
    string? Provincia,
    string? Comune,
    int? AnnoEstirpo,
    DateTime? DataAggiornamentoStato,
    bool Modificabile,
    bool PuoLineaRischio,
    bool PuoElaboraMeteo,
    MenuVignetoDto Menu);
