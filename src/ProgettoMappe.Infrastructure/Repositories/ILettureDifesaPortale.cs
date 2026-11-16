using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Letture delle tabelle della difesa del Portale (tappa 2, sezioni 13–17), con le stesse condizioni dei controller del
/// portale (AvversitaDaTrattareController, IrroratriciController, IrroratriciEnteController). Schema dal dba, 16/11/2026.
/// </summary>
public interface ILettureDifesaPortale
{
    /// <summary><c>AvversitaDaTrattare</c>, per descrizione.</summary>
    Task<IReadOnlyList<AvversitaDaTrattareDto>> AvversitaAsync(CancellationToken ct = default);

    /// <summary><c>Irroratrice</c> con il nome del tipo, per descrizione.</summary>
    Task<IReadOnlyList<IrroratriceDto>> IrroratriciAsync(CancellationToken ct = default);

    /// <summary><c>TipoIrroratrice</c> (menù della scheda).</summary>
    Task<IReadOnlyList<VocePortaleDto>> TipiIrroratriceAsync(CancellationToken ct = default);

    /// <summary>
    /// <c>Irroratrice_Ente</c> con irroratrice, tipo e azienda; <paramref name="soloEnti"/> = solo quelle aziende (chi non è
    /// MasterUser vede le abbinate), null = tutte. <c>Modificabile</c> lo decide il controller.
    /// </summary>
    Task<IReadOnlyList<IrroratriceAziendaDto>> IrroratriciAziendaAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default);

    /// <summary>Le aziende abbinate con <c>AggiornamentiAbilitati = 1</c> (<c>GetIdsEntiConAggiornamentiAbilitati</c>).</summary>
    Task<IReadOnlyList<int>> AziendeModificabiliAsync(int idUtente, CancellationToken ct = default);

    /// <summary>L'azienda (<c>IdEnte</c>) di una riga di <c>Irroratrice_Ente</c>, null se non c'è.</summary>
    Task<int?> AziendaDellIrroratriceAsync(int id, CancellationToken ct = default);

    /// <summary><c>VistaLineeDifesa</c> dell'anno; <paramref name="tipologie"/> = solo quelle tipologie di linea, null = tutte.</summary>
    Task<IReadOnlyList<LineaDifesaRigaDto>> LineeDifesaAsync(int anno, IReadOnlyList<int>? tipologie, CancellationToken ct = default);

    /// <summary>La riga intera di <c>LineaDifesa</c>, null se non c'è.</summary>
    Task<LineaDifesaDto?> LineaDifesaAsync(int id, CancellationToken ct = default);

    /// <summary>Le tipologie di linea (<c>Vigneto.IdTipologiaLinea</c>) dei vigneti di quelle aziende.</summary>
    Task<IReadOnlyList<int>> TipologieDeiVignetiAsync(IReadOnlyList<int> aziende, CancellationToken ct = default);

    Task<MenuLineeDifesaDto> MenuLineeDifesaAsync(CancellationToken ct = default);

    /// <summary>Tutte le righe di <c>SogliaTrattamento</c> (14 righe, dba 16/11).</summary>
    Task<IReadOnlyList<SogliaTrattamentoDto>> SoglieAsync(CancellationToken ct = default);

    /// <summary>Le aziende con almeno un vigneto con la linea di difesa (<c>SoglieTrattamentiModel.GetEnti</c>).</summary>
    Task<IReadOnlyList<AziendaPortaleDto>> AziendeConLineaAsync(CancellationToken ct = default);

    /// <summary>
    /// I vigneti dell'azienda con la linea di difesa (<c>SoglieTrattamentiModel.GetVigneti</c>); <paramref name="idUtente"/> =
    /// solo quelli di <c>VistaUtentiVigneti</c> per quell'utente.
    /// </summary>
    Task<IReadOnlyList<VignetoConLinea>> VignetiConLineaAsync(int azienda, int? idUtente, CancellationToken ct = default);
}

/// <summary>Un vigneto con la linea di difesa, con il suo rischio (<c>Vigneto.IdRischio</c>).</summary>
public record VignetoConLinea(int IdVigneto, string Nome, int? IdRischio);
