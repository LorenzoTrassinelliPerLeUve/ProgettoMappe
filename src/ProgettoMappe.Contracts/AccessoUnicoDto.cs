namespace ProgettoMappe.Contracts;

// Accesso unico per le app esterne (task #234, docs/accesso-unico.md): nomi JSON in camelCase come il resto dell'Api.

/// <summary>Corpo di <c>POST api/accesso/token</c>: l'email è il <c>LoginName</c> di 4Grapes.</summary>
public record RichiestaTokenDto(string? Email, string? Password);

/// <summary>Azienda visibile all'utente (<c>Utente_Ente</c> attiva); <c>Gestione</c> = colonna <c>Utente_Ente.Gestione</c>.</summary>
public record AziendaAccessoDto(int Id, string Nome, bool Gestione);

/// <summary>
/// Chi è l'utente e cosa può fare: lo stesso contenuto del token, riletto da 4Grapes da <c>GET api/accesso/me</c>.
/// <c>Ruoli</c>: «MasterUser», «UtentePerleuve», «SysAdmin» (solo quelli veri). <c>Abilitazioni</c>: codici di
/// <c>VistaAbilitazioniUtente</c> in maiuscolo. <c>PermessiRidotti</c>: Mappe non può leggere ruoli e abilitazioni
/// (GRANT mancante) e dà solo quelle di chi ha l'app.
/// </summary>
public record ProfiloAccessoDto(
    int IdUtente,
    string Email,
    string Nome,
    IReadOnlyList<string> Ruoli,
    IReadOnlyList<string> Abilitazioni,
    IReadOnlyList<AziendaAccessoDto> Aziende,
    bool PermessiRidotti);

/// <summary>Risposta di <c>POST api/accesso/token</c>, nel formato OAuth2 per i primi tre campi.</summary>
public record TokenAccessoDto(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
    ProfiloAccessoDto Utente);
