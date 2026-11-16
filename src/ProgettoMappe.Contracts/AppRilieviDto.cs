namespace ProgettoMappe.Contracts;

// Rilievi delle app di Jan nel 4Grapes (task #265, docs/api-app.md): un rilievo (dbo.Rilievo) più la riga della tabella del
// tipo. Il corpo del POST è anche il campo «dati» delle letture. I nomi dei campi specifici sono quelli delle colonne del dba.

/// <summary>
/// Corpo di <c>POST api/app/prove-irroratrice</c>: il riassunto di una prova chiusa di 4Sprayers (tipo 169,
/// <c>dbo.RilievoProvaIrroratrice</c>). Obbligatori: codice, azienda, vigneto, data. Stesso <c>CodiceProva</c> = stessa prova
/// (si aggiorna, non si doppia).
/// </summary>
public record ProvaIrroratriceAppDto(
    string? CodiceProva,
    int? IdAzienda,
    int? IdVigneto,
    DateTime? Data,
    string? Note = null,
    string? TipoProva = null,
    int? IdIrroratrice = null,
    int? StadioBBCH = null,
    decimal? VelocitaKmh = null,
    decimal? PressioneBar = null,
    decimal? VolumeLHa = null,
    int? NumeroUgelli = null,
    decimal? PortataLMin = null,
    decimal? CoperturaMediaPerc = null,
    string? ClasseCopertura = null,
    decimal? CoperturaBassaPerc = null,
    decimal? CoperturaAltaPerc = null,
    decimal? CoperturaSopraPerc = null,
    decimal? CoperturaSottoPerc = null,
    int? CartineAnalizzate = null,
    bool? Definitiva = null,
    decimal? DensitaGocceCm2 = null,
    decimal? VmdMicron = null,
    decimal? UniformitaCvPerc = null,
    string? Esito = null,
    string? Consiglio = null);

/// <summary>
/// Corpo di <c>POST api/app/riassunti-potatura</c>: il riassunto di un conteggio dell'App potatura (tipo 170,
/// <c>dbo.RilievoRiassuntoPotatura</c>). Obbligatori: codice, azienda, vigneto, data, tipo di conteggio. Infiorescenze e
/// grappoli sono due rilievi (due codici).
/// </summary>
public record RiassuntoPotaturaAppDto(
    string? CodiceOrigine,
    int? IdAzienda,
    int? IdVigneto,
    DateTime? Data,
    string? Note = null,
    string? TipoConteggio = null,
    string? Struttura = null,
    int? PianteRilevate = null,
    decimal? GemmeLasciatePerPianta = null,
    decimal? GermogliPerPianta = null,
    decimal? GermogliamentoPerc = null,
    decimal? FertilitaPerGemma = null,
    decimal? FertilitaPerGermoglio = null,
    decimal? StimaPerPianta = null,
    decimal? KgPerCeppo = null,
    int? PesoGrappoloG = null,
    decimal? LunghezzaMediaGermogliCm = null,
    decimal? UniformitaCvPerc = null);

/// <summary>
/// Un rilievo letto: chiave del rilievo in 4Grapes, chi l'ha creato, ultima modifica, se chi chiama può cambiarlo, e i dati
/// nella stessa forma del POST.
/// </summary>
public record RilievoAppDto<T>(DateTime IdRilievo, int IdUtente, DateTime? DataModifica, bool Modificabile, T Dati);

/// <summary>
/// Esito di POST e DELETE. <paramref name="Esito"/>: «creato», «aggiornato», «invariato» (nessun cambiamento, niente scritto),
/// «cancellato». <paramref name="Prova"/> = Mappe in modo prova: controllato e poi annullato, niente scritto.
/// </summary>
public record EsitoRilievoAppDto(string Codice, DateTime IdRilievo, string Esito, bool Prova);
