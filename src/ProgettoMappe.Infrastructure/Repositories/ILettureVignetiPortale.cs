using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Letture della scheda del vigneto del menù Nutrizione (<c>VignetiController.Form</c>, task #296). Colonne e tabelle di
/// decodifica dal dba (16/11/2026, sotto-task #297).
/// </summary>
public interface ILettureVignetiPortale
{
    /// <summary>
    /// La scheda del vigneto (Id &gt; 0) con azienda, vigna e comune di <c>VignetoMaps</c>; null se non c'è. Permessi e
    /// tendine li mette il controller (<c>Modificabile</c>, <c>PuoLineaRischio</c>, <c>PuoElaboraMeteo</c> false, menù vuoto).
    /// </summary>
    Task<VignetoSchedaDto?> VignetoAsync(int idVigneto, CancellationToken ct = default);

    /// <summary>
    /// Le tendine: vitigni, le sei tabelle di decodifica (Irrigazione, Giacitura, FormaAllevamento, Portainnesto, Clone,
    /// Conduzione), tipologie di linea e rischi. Una tabella senza permesso di lettura resta vuota ed entra in
    /// <c>NonLeggibili</c>: la scheda si apre lo stesso.
    /// </summary>
    Task<MenuVignetoDto> MenuAsync(CancellationToken ct = default);
}
