namespace ProgettoMappe.Contracts;

// Accesso unico per le app esterne (task #234, docs/accesso-unico.md): nomi JSON in camelCase come il resto dell'Api.

/// <summary>Corpo di <c>POST api/accesso/token</c>: l'email è il <c>LoginName</c> di 4Grapes.</summary>
public record RichiestaTokenDto(string? Email, string? Password);

/// <summary>
/// Azienda abbinata all'utente (<c>Utente_Ente</c> attiva); <c>Gestione</c> e <c>Ruolo</c> = colonne di <c>Utente_Ente</c>
/// (Ruolo oggi sempre vuoto: dba 16/11).
/// </summary>
public record AziendaAccessoDto(int Id, string Nome, bool Gestione, string? Ruolo = null);

/// <summary>
/// Chi è l'utente e cosa può fare: lo stesso contenuto del token, riletto da 4Grapes da <c>GET api/accesso/me</c>.
/// <c>Ruoli</c>: «MasterUser», «UtentePerleuve», «SysAdmin» (solo quelli veri). <c>Abilitazioni</c>: codici di
/// <c>VistaAbilitazioniUtente</c> in maiuscolo. <c>PermessiRidotti</c>: Mappe non può leggere ruoli e abilitazioni
/// (GRANT mancante) e dà solo quelle di chi ha l'app. <c>TutteLeAziende</c>: MasterUser, vede tutte le aziende come nel
/// portale (decisione del proprietario, 16/11); <c>Aziende</c> resta l'elenco delle abbinate, per non fare token enormi.
/// </summary>
public record ProfiloAccessoDto(
    int IdUtente,
    string Email,
    string Nome,
    IReadOnlyList<string> Ruoli,
    IReadOnlyList<string> Abilitazioni,
    IReadOnlyList<AziendaAccessoDto> Aziende,
    bool PermessiRidotti,
    bool TutteLeAziende = false,
    IReadOnlyList<AbilitazioneAziendaDto>? AbilitazioniAzienda = null);

/// <summary>
/// Abilitazione data alla persona per una sola azienda (<c>dbo.AbilitazioneUtente</c> con IdEnte), es. SUITE_TRATTORISTA
/// per l'azienda 425. Quelle per tutte le aziende stanno in <see cref="ProfiloAccessoDto.Abilitazioni"/>.
/// </summary>
public record AbilitazioneAziendaDto(string Codice, int IdAzienda);

/// <summary>Corpo di <c>POST api/accesso/trattoristi</c>: utente 4Grapes nuovo per un'azienda, con SUITE_TRATTORISTA.</summary>
public record NuovoTrattoristaDto(int IdAzienda, string? Email, string? Nome, string? Cognome, string? Password, string? Cellulare = null);

/// <summary>Corpo di <c>POST api/accesso/abilitazioni</c>: dare un'abilitazione a una persona (IdAzienda null = tutte).</summary>
public record NuovaAbilitazioneDto(int IdUtente, string? Codice, int? IdAzienda, DateOnly? DataFine = null, string? Note = null);

/// <summary>Esito di una scrittura dell'accesso unico: Id creato e se era una prova (modo «prova»: niente salvato).</summary>
public record EsitoAccessoDto(int Id, bool Prova);

/// <summary>Risposta di <c>POST api/accesso/token</c>, nel formato OAuth2 per i primi tre campi.</summary>
public record TokenAccessoDto(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
    ProfiloAccessoDto Utente);
