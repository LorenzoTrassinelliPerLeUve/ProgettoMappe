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
    bool Attivo = false);

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

// ---- Tappa 5b: 24 Abilitazioni (per categoria e per persona) e 25 Gruppi di utenti ----

/// <summary>Sezione 24: una riga di <c>Abilitazione</c> (codice e i sei ruoli che la danno), come la lista del portale.</summary>
public record AbilitazioneCategoriaDto(
    string Id,
    bool SysAdmin,
    bool MasterUser,
    bool UtentePerLeUve,
    bool DSSUser,
    bool SoilUser,
    bool AppEnabled);

/// <summary>Sezione 24: i sei campi della scheda del portale (il codice non cambia).</summary>
public record AbilitazioneCategoriaSalvaDto(bool SysAdmin, bool MasterUser, bool UtentePerLeUve, bool DSSUser, bool SoilUser, bool AppEnabled);

/// <summary>
/// Un'abilitazione personale (<c>AbilitazioneUtente</c>): un codice dato a una persona, per un'azienda o per tutte
/// (<paramref name="IdEnte"/> null). <paramref name="ValeOggi"/> = attiva e dentro le date.
/// </summary>
public record AbilitazionePersonaDto(
    int Id,
    string Codice,
    int? IdEnte,
    string? Azienda,
    bool Attiva,
    DateOnly? DataInizio,
    DateOnly? DataFine,
    string? Note,
    bool ValeOggi);

/// <summary>
/// Le abilitazioni personali di un utente nella sua scheda: le righe, i codici di <c>Abilitazione</c> e le aziende abbinate
/// all'utente (le sole a cui si dà un codice per azienda).
/// </summary>
public record AbilitazioniPersonaDto(
    IReadOnlyList<AbilitazionePersonaDto> Righe,
    IReadOnlyList<string> Codici,
    IReadOnlyList<AziendaPortaleDto> Aziende);

/// <summary>Dare un codice a una persona: <paramref name="IdEnte"/> null = per tutte le aziende.</summary>
public record AbilitazionePersonaSalvaDto(string? Codice, int? IdEnte, DateOnly? DataFine, string? Note);

/// <summary>Sezione 25: un gruppo di utenti nella lista (il portale mostra solo il nome).</summary>
public record GruppoUtentiDto(int Id, string? Descrizione, int Partecipanti);

/// <summary>Sezione 25: un partecipante (Nome, E-Mail come la tabella del portale).</summary>
public record PartecipanteGruppoDto(int IdUtente, string NomeCompleto, string? LoginName);

/// <summary>Sezione 25: la scheda del gruppo con i partecipanti.</summary>
public record GruppoUtentiSchedaDto(int Id, string? Descrizione, IReadOnlyList<PartecipanteGruppoDto> Partecipanti);

/// <summary>Sezione 25: il campo «Nome» della scheda.</summary>
public record GruppoUtentiSalvaDto(string? Descrizione);

/// <summary>Sezione 25: gli utenti da aggiungere («Aggiungi partecipanti»).</summary>
public record PartecipantiSalvaDto(IReadOnlyList<int> IdsUtenti);

// ---- Tappa 5c: 21 Notifiche ----

/// <summary>
/// Sezione 21: il modulo «Invia» — tipi (menù «Icona»), gruppi con il numero di partecipanti, e se push ed email partono davvero
/// (<paramref name="PushVera"/>/<paramref name="PostaVera"/> false = simulate e registrate).
/// </summary>
public record NotificheDto(IReadOnlyList<VocePortaleDto> Tipi, IReadOnlyList<GruppoUtentiDto> Gruppi, bool PushVera, bool PostaVera);

/// <summary>
/// Sezione 21: Titolo (Testata), Icona (IdTipoNotifica), Corpo, gruppi e utenti singoli. Senza gruppi né utenti = tutti gli utenti
/// con l'app abilitata, come il portale. <paramref name="AncheEmail"/> = manda anche la posta (il portale manda solo la push).
/// </summary>
public record NotificaInviaDto(
    string? Testata,
    int? IdTipoNotifica,
    string? Corpo,
    IReadOnlyList<int>? IdsGruppi,
    IReadOnlyList<int>? IdsUtenti,
    bool AncheEmail = true);

/// <summary>Sezione 21: com'è andato l'invio. Gli errori di push o posta non annullano le notifiche registrate in 4Grapes.</summary>
public record EsitoNotificaDto(
    int Destinatari,
    bool Prova,
    bool PushSimulate,
    int PushInviate,
    int PushNonPartite,
    bool PostaSimulata,
    int EmailInviate,
    string? Errore);
