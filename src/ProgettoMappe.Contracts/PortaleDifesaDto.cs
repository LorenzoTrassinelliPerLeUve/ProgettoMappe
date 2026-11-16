namespace ProgettoMappe.Contracts;

// Tabelle della difesa del Portale (tappa 2, task #246): sezioni 13–17. Campi e nomi come le entità del portale.

/// <summary>Una voce di un menù a tendina (Id e testo).</summary>
public record VocePortaleDto(int Id, string Testo);

/// <summary>Sezione 15: una riga di <c>AvversitaDaTrattare</c> (il portale mostra e salva solo la descrizione, in sola lettura).</summary>
public record AvversitaDaTrattareDto(int Id, string? Descrizione);

/// <summary>Sezione 16: una riga di <c>Irroratrice</c> con il nome del tipo.</summary>
public record IrroratriceDto(int Id, string? Descrizione, int? IdTipoIrroratrice, string? TipoIrroratrice);

/// <summary>Sezione 16: la lista e i tipi per il menù della scheda.</summary>
public record IrroratriciDto(IReadOnlyList<IrroratriceDto> Righe, IReadOnlyList<VocePortaleDto> Tipi);

/// <summary>Sezione 16: i campi della scheda del portale (Descrizione, Tipo irroratrice).</summary>
public record IrroratriceSalvaDto(string? Descrizione, int? IdTipoIrroratrice);

/// <summary>
/// Sezione 17: una riga di <c>Irroratrice_Ente</c>. <paramref name="Modificabile"/> = il portale mostra il pulsante di modifica
/// (MasterUser o azienda con <c>Utente_Ente.AggiornamentiAbilitati</c>).
/// </summary>
public record IrroratriceAziendaDto(
    int Id,
    int IdEnte,
    string? Ente,
    int IdIrroratrice,
    string? Irroratrice,
    string? TipoIrroratrice,
    int? BBCH,
    decimal? VolumeAcqua,
    decimal? VelocitaAvanzamento,
    int? NumeroUgelli,
    string? TipologiaUgelli,
    bool Modificabile);

/// <summary>Sezione 17: lista, aziende (filtro e scheda) e irroratrici (scheda).</summary>
public record IrroratriciAziendaDto(
    IReadOnlyList<IrroratriceAziendaDto> Righe,
    IReadOnlyList<AziendaPortaleDto> Aziende,
    IReadOnlyList<VocePortaleDto> Irroratrici);

/// <summary>Sezione 17: i campi della scheda del portale (il numero di ugelli non c'è).</summary>
public record IrroratriceAziendaSalvaDto(
    int? IdIrroratrice,
    int? IdEnte,
    int? BBCH,
    decimal? VolumeAcqua,
    decimal? VelocitaAvanzamento,
    string? TipologiaUgelli);
