namespace ProgettoMappe.Contracts;

/// <summary>
/// L'utente entrato visto dal Portale: ruoli, abilitazioni e le 22 sezioni con <c>Accessibile</c>.
/// <c>PermessiLetti = false</c> se 4Grapes non risponde o mancano i permessi di lettura (<c>Problema</c> dice quale):
/// in quel caso nessuna sezione è accessibile.
/// </summary>
public record PortaleUtenteDto(
    int IdUtente,
    bool PermessiLetti,
    string? Problema,
    IReadOnlyList<string> Ruoli,
    IReadOnlyList<string> Abilitazioni,
    IReadOnlyList<SezionePortaleDto> Sezioni);

/// <param name="Pronta">false = la voce c'è ma la pagina è «in arrivo».</param>
public record SezionePortaleDto(
    string Chiave,
    int Numero,
    string Titolo,
    string Gruppo,
    string Descrizione,
    int Tappa,
    bool Pronta,
    bool Accessibile);

/// <summary>Stato delle scritture su 4Grapes: «spente», «prova» (tutto annullato alla fine) o «vere».</summary>
public record StatoScrittureDto(string Modo, string Descrizione);

/// <summary>
/// Risposta di ogni scrittura del Portale. <c>Prova = true</c>: eseguita e poi annullata, sul DB non è rimasto niente.
/// <c>Log</c> = le righe di LogOp scritte (o che sarebbero state scritte), operazione e tabella.
/// </summary>
public record EsitoScritturaDto(string? Chiave, bool Prova, IReadOnlyList<string> Log);
