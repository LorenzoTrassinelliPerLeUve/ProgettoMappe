using System.Text.Json;

namespace ProgettoMappe.Contracts;

/// <summary>
/// Una modifica della configurazione del DSS Bilancio Idrico (task #333): il record del tipo (<c>settore</c>, <c>quota</c>,
/// <c>impianto</c>, <c>shift</c>, <c>settore-shift</c>, <c>stile</c>, <c>irrigazione</c>, <c>modello</c>) in <see cref="Dati"/>.
/// <see cref="Elimina"/> = toglie la voce (resta nello storico). <see cref="Autore"/> lo mette il Web dall'utente entrato.
/// </summary>
public sealed record SalvaVoceBilancioDto(string? Autore, JsonElement Dati, bool Elimina = false, string? Nota = null);

/// <summary>Una versione di una voce della configurazione, con chi e quando.</summary>
public sealed record VoceBilancioDto(string Tipo, string Chiave, int Versione, JsonElement? Dati, string Autore, DateTime CreatoIlUtc,
    string? Nota);
