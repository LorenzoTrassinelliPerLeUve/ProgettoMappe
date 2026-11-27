using Microsoft.Data.SqlClient;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// La consultazione chiusa anche nell'Api (task #401), non solo nel menù: con l'utente in <c>X-Mappe-Utente</c> (lo mette il
/// Web per il Portale, il bilancio idrico e i filari) chi è in consultazione non scrive niente (POST/PUT/PATCH/DELETE → 403),
/// tranne il proprio profilo e la password (<c>api/portale/account</c>); chi ha l'account non attivo non legge nemmeno il
/// Portale (solo la sua esperienza). Senza intestazione la richiesta passa: i controller fanno i loro controlli come prima.
/// </summary>
public sealed class ControlloConsultazione(RequestDelegate avanti)
{
    public const string MessaggioConsultazione = "Il tuo accesso è di consultazione: qui puoi guardare ma non modificare.";
    public const string MessaggioFuori = "Il tuo account non è attivo: scrivi a " + TipiEsperienza.Supporto + ".";

    public async Task InvokeAsync(HttpContext contesto, EsperienzeUtenti esperienze, ILogger<ControlloConsultazione> logger)
    {
        var percorso = contesto.Request.Path;
        if (!percorso.StartsWithSegments("/api")
            || !contesto.Request.Headers.TryGetValue(PortaleControllerBase.IntestazioneUtente, out var valore)
            || !int.TryParse(valore, out var idUtente)
            || percorso.StartsWithSegments("/api/portale/esperienza")
            || percorso.StartsWithSegments("/api/portale/account"))
        {
            await avanti(contesto);
            return;
        }

        var scrittura = !(HttpMethods.IsGet(contesto.Request.Method) || HttpMethods.IsHead(contesto.Request.Method)
                          || HttpMethods.IsOptions(contesto.Request.Method));
        var portale = percorso.StartsWithSegments("/api/portale");
        if (!scrittura && !portale)
        {
            await avanti(contesto);
            return;
        }

        string? esperienza;
        try
        {
            esperienza = (await esperienze.GetAsync(idUtente, contesto.RequestAborted))?.Esperienza;
        }
        catch (SqlException ex)
        {
            // Se non si sa chi è, una scrittura non parte (le letture del Portale falliscono comunque con 4Grapes giù).
            logger.LogError(ex, "Esperienza dell'utente {IdUtente} non leggibile (errore SQL {Numero})", idUtente, ex.Number);
            if (scrittura)
            {
                await Rifiuta(contesto, portale, StatusCodes.Status503ServiceUnavailable,
                    "4Grapes non risponde: non riesco a controllare i tuoi permessi.");
                return;
            }

            esperienza = null;
        }

        if (esperienza == TipiEsperienza.Fuori || scrittura && TipiEsperienza.SoloConsultazione(esperienza))
        {
            logger.LogInformation("Rifiutata {Metodo} {Percorso} all'utente {IdUtente} ({Esperienza})",
                contesto.Request.Method, percorso, idUtente, esperienza);
            await Rifiuta(contesto, portale, StatusCodes.Status403Forbidden,
                esperienza == TipiEsperienza.Fuori ? MessaggioFuori : MessaggioConsultazione);
            return;
        }

        await avanti(contesto);
    }

    /// <summary>Il Portale legge il <c>title</c> di un ProblemDetails, bilancio idrico e filari il testo semplice.</summary>
    private static Task Rifiuta(HttpContext contesto, bool portale, int stato, string messaggio) =>
        portale
            ? Results.Problem(title: messaggio, statusCode: stato).ExecuteAsync(contesto)
            : Results.Text(messaggio, statusCode: stato).ExecuteAsync(contesto);
}
