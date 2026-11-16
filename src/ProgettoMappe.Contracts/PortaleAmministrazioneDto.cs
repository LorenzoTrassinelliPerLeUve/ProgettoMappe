namespace ProgettoMappe.Contracts;

// Amministrazione del Portale (tappa 5, task #284): 23 Utenti, 26 Impostazioni. Campi e nomi come le entità del portale.

/// <summary>Sezione 23: una riga della lista (il portale mostra Email e «Cognome Nome»).</summary>
public record UtenteListaDto(int Id, string? LoginName, string NomeCompleto, bool Attivo);

/// <summary>
/// Sezione 23: la lista e cosa può fare chi guarda. <paramref name="PuoAbilitazioni"/> = ha ABILITAZIONI: vede e cambia i
/// ruoli (app abilitata, MasterUser, utente Perleuve, guest) e lo stato, come la scheda del portale.
/// </summary>
public record UtentiDto(IReadOnlyList<UtenteListaDto> Righe, bool PuoAbilitazioni, IReadOnlyList<string> Lingue);

/// <summary>Sezione 23: la scheda di un utente (senza password: si scrive solo quella nuova).</summary>
public record UtenteSchedaDto(
    int Id,
    string? LoginName,
    string? Nome,
    string? Cognome,
    string? Lingua,
    string? Cellulare,
    bool IsAppEnabled,
    bool IsMasterUser,
    bool IsUtentePerleuve,
    bool IsGuest,
    bool Attivo,
    DateTime? DataCreazione,
    DateTime? DataUltimoAggiornamento);

/// <summary>
/// Sezione 23: i campi della scheda del portale. <paramref name="Password"/> = «MODIFICA PASSWORD» (obbligatoria solo per un
/// utente nuovo; vuota = invariata). Ruoli e <paramref name="Attivo"/> contano solo per chi ha ABILITAZIONI.
/// </summary>
public record UtenteSalvaDto(
    string? LoginName,
    string? Password,
    string? Nome,
    string? Cognome,
    string? Lingua,
    string? Cellulare,
    bool IsAppEnabled,
    bool IsMasterUser,
    bool IsUtentePerleuve,
    bool IsGuest,
    bool Attivo = true);

/// <summary>
/// Sezione 26: la riga 0 di <c>Impostazioni</c>. Le date delle elaborazioni e il segnale «in corso» sono solo da leggere
/// (li scrive il programma dei modelli sul server di 4Grapes).
/// </summary>
public record ImpostazioniDto(
    decimal? AltezzaMaxRifChioma,
    decimal? LarghezzaMaxRifChioma,
    string? CoefficenteCanopy,
    IReadOnlyList<string> Coefficienti,
    DateTime? DataUltimaElaborazioneModelliBBCH,
    DateTime? DataUltimaElaborazioneModelliInfezione,
    DateTime? DataUltimaElaborazioneModelliProtezione,
    DateTime? DataUltimaGenerazioneTrattamentiAutomatici,
    bool ElaborazioneModelliProtezioneInCorso);

/// <summary>Sezione 26: i tre campi della scheda del portale.</summary>
public record ImpostazioniSalvaDto(decimal? AltezzaMaxRifChioma, decimal? LarghezzaMaxRifChioma, string? CoefficenteCanopy);
