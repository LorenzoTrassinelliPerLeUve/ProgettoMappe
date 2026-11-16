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

/// <summary>Sezione 13: una riga della lista (<c>VistaLineeDifesa</c>), colonne della lista del portale.</summary>
public record LineaDifesaRigaDto(
    int Id,
    int Anno,
    int? IdTipologiaLinea,
    string? TipologiaLinea,
    int? IdRischio,
    string? Rischio,
    string? Pressione,
    int? IdAvversita,
    string? Avversita,
    int? IdBBCH,
    string? BBCH,
    string? NumRegProdotto,
    string? Prodotto,
    decimal? DosaggioRifPerleuve,
    decimal? DosaggioTrv,
    int? H20RifPerleuve,
    int? H20Trv);

/// <summary>Sezione 13: lista dell'anno, con le aziende per il filtro e se l'utente può modificare (solo MasterUser).</summary>
public record LineeDifesaDto(IReadOnlyList<LineaDifesaRigaDto> Righe, IReadOnlyList<AziendaPortaleDto> Aziende, bool Modificabili);

/// <summary>Sezione 13: le tipologie di linea dei vigneti di un'azienda (filtro azienda del portale).</summary>
public record TipologieAziendaDto(int IdAzienda, IReadOnlyList<int> Tipologie);

/// <summary>Sezione 13: i menù della scheda e dei filtri.</summary>
public record MenuLineeDifesaDto(
    IReadOnlyList<VocePortaleDto> Tipologie,
    IReadOnlyList<VocePortaleDto> Rischi,
    IReadOnlyList<VocePortaleDto> Avversita,
    IReadOnlyList<VocePortaleDto> Bbch,
    IReadOnlyList<ProdottoPortaleDto> Prodotti,
    IReadOnlyList<VocePortaleDto> UnitaMisura);

/// <summary>Un prodotto di <c>ProdottoVite</c> (numero di registrazione e nome commerciale).</summary>
public record ProdottoPortaleDto(string NumReg, string Nome);

/// <summary>Sezione 13: la riga intera di <c>LineaDifesa</c> (scheda) o i campi da salvare (Id e Concentrazione ignorati).</summary>
public record LineaDifesaDto(
    int Id,
    int? Anno,
    int? IdTipologiaLinea,
    int? IdRischio,
    int? IdAvversita,
    int? IdBBCH,
    string? Pressione,
    string? NumRegProdotto,
    decimal? DosaggioRifPerleuve,
    int? UM,
    int? H20RifPerleuve,
    int? UMH20,
    decimal? Concentrazione,
    string? Note);

/// <summary>Sezione 17: i campi della scheda del portale (il numero di ugelli non c'è).</summary>
public record IrroratriceAziendaSalvaDto(
    int? IdIrroratrice,
    int? IdEnte,
    int? BBCH,
    decimal? VolumeAcqua,
    decimal? VelocitaAvanzamento,
    string? TipologiaUgelli);
