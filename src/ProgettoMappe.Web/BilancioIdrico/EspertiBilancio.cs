using System.Security.Claims;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Web.Portale;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>
/// Task #405 (decisione del proprietario, 27/11): la configurazione «per esperti» del bilancio idrico (parametri del modello,
/// correzioni fini, quote parziali) solo a chi in 4Grapes è MasterUser o SysAdmin, dai ruoli che dà già l'Api del Portale.
/// </summary>
public static class EspertiBilancio
{
    public static readonly IReadOnlyList<string> Ruoli = ["MasterUser", "SysAdmin"];

    public static bool Esperto(IEnumerable<string>? ruoli) => ruoli?.Any(r => Ruoli.Contains(r)) == true;

    /// <summary>Vero se l'utente entrato è un esperto; se i permessi non si leggono, no.</summary>
    public static async Task<bool> EspertoAsync(ClaimsPrincipal utente, PortaleApiClient api, ILogger logger)
    {
        if (UtentePortale.IdUtente(utente) is not { } id)
            return false;
        try
        {
            return Esperto((await api.GetUtenteAsync(id)).Dati?.Ruoli);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Bilancio idrico: ruoli dell'utente {Utente} non letti ({Messaggio})", id, ex.Message);
            return false;
        }
    }
}
