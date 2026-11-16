namespace ProgettoMappe.Contracts;

/// <summary>
/// Corpo di <c>POST/PUT api/app/aziende/{idAzienda}/irroratrici</c> (task #259, docs/api-app.md): una regolazione
/// dell'irroratrice per l'azienda e la fase BBCH (<c>dbo.Irroratrice_Ente</c>). L'azienda viene dall'indirizzo.
/// Obbligatori come nella scheda del portale: irroratrice, BBCH, volume d'acqua, velocità, tipologia degli ugelli.
/// Facoltativi (la scheda del portale non li ha): numero di ugelli, portata (l/min), pressione (bar); in modifica un
/// facoltativo assente (null) resta com'è.
/// </summary>
public record IrroratriceAppSalvaDto(
    int? IdIrroratrice,
    int? BBCH,
    decimal? VolumeAcqua,
    decimal? VelocitaAvanzamento,
    string? TipologiaUgelli,
    int? NumeroUgelli = null,
    decimal? PortataLminuto = null,
    decimal? Pressionebar = null);
