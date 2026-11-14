using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Base dei controller del Portale (convenzioni in docs/portale/LEGGIMI.md):
/// <list type="bullet">
/// <item>rotta <c>api/portale/{sezione}/…</c>;</item>
/// <item>l'utente arriva nell'intestazione <c>X-Mappe-Utente</c> (IdUtente 4Grapes), messa dal Web dal cookie di accesso,
///   mai dal browser; senza → 401;</item>
/// <item>ogni azione chiama <see cref="UtenteAsync"/> con la sua sezione: regola di <see cref="SezioniPortale"/> → 403;</item>
/// <item>errori sempre come ProblemDetails con un titolo in italiano da mostrare: 400 (con gli errori per campo),
///   401, 403, 404, 409 (cambiata da altri), 503 (4Grapes non risponde o scritture spente);</item>
/// <item>le scritture passano solo da <see cref="ScriviAsync"/>: transazione, LogOp, modo prova.</item>
/// </list>
/// </summary>
[ApiController]
public abstract class PortaleControllerBase : ControllerBase
{
    public const string IntestazioneUtente = "X-Mappe-Utente";

    /// <summary>
    /// L'utente e i suoi permessi, se può usare la sezione; altrimenti la risposta d'errore da restituire.
    /// </summary>
    protected async Task<(PermessiUtente? Utente, ActionResult? Errore)> UtenteAsync(string chiaveSezione, CancellationToken ct)
    {
        var sezione = SezioniPortale.Trova(chiaveSezione)
                      ?? throw new ArgumentException($"Sezione sconosciuta: {chiaveSezione}");
        if (IdUtente() is not { } id)
        {
            return (null, Errore(StatusCodes.Status401Unauthorized, "Accedi per usare il Portale."));
        }

        PermessiUtente? permessi;
        try
        {
            permessi = await HttpContext.RequestServices.GetRequiredService<IPermessiRepository>().GetPermessiAsync(id, ct);
        }
        catch (SqlException ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<PortaleControllerBase>>()
                .LogError(ex, "Permessi dell'utente {IdUtente} non leggibili (errore SQL {Numero})", id, ex.Number);
            return (null, Errore(StatusCodes.Status503ServiceUnavailable, "Non riesco a leggere i tuoi permessi in 4Grapes."));
        }

        if (permessi is null || !sezione.Regola.Consente(permessi))
        {
            return (null, Errore(StatusCodes.Status403Forbidden, $"Non hai il permesso per «{sezione.Titolo}»."));
        }

        return (permessi, null);
    }

    /// <summary>L'IdUtente dell'intestazione, null se manca o non è un intero positivo.</summary>
    protected int? IdUtente() =>
        Request.Headers.TryGetValue(IntestazioneUtente, out var valore) && int.TryParse(valore.ToString(), out var id) && id > 0
            ? id
            : null;

    /// <summary>
    /// Esegue un salvataggio nella transazione di <see cref="ScritturaFourGrapes"/> e lo traduce nella risposta:
    /// 200 con <see cref="EsitoScritturaDto"/>, 409 per un conflitto, 503 se le scritture sono spente o 4Grapes non risponde.
    /// Il lavoro restituisce la chiave della riga principale (o null).
    /// </summary>
    protected async Task<ActionResult<EsitoScritturaDto>> ScriviAsync(PermessiUtente utente,
        Func<OperazioniScrittura, Task<object?>> lavoro, CancellationToken ct)
    {
        var scrittura = HttpContext.RequestServices.GetRequiredService<ScritturaFourGrapes>();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<PortaleControllerBase>>();
        try
        {
            var esito = await scrittura.EseguiAsync(utente.IdUtente, lavoro, ct);
            logger.LogInformation("Portale: {Operazioni} dell'utente {IdUtente}{Prova}",
                string.Join(", ", esito.Log.Select(l => $"{l.Operazione} {l.Tabella}")), utente.IdUtente,
                esito.Prova ? " (prova, annullato)" : "");
            return Ok(new EsitoScritturaDto(esito.Risultato?.ToString(), esito.Prova,
                esito.Log.Select(l => $"{l.Operazione} {l.Tabella}").ToList()));
        }
        catch (ScrittureSpenteException)
        {
            return Errore(StatusCodes.Status503ServiceUnavailable, "Il salvataggio su 4Grapes non è ancora attivo.");
        }
        catch (ConflittoScritturaException ex)
        {
            return Errore(StatusCodes.Status409Conflict, ex.Message + ". Ricarica e riprova.");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Scrittura del Portale non riuscita (errore SQL {Numero}, utente {IdUtente})", ex.Number, utente.IdUtente);
            return Errore(StatusCodes.Status503ServiceUnavailable, "4Grapes non ha accettato il salvataggio: niente è stato cambiato.");
        }
    }

    /// <summary>400 con gli errori per campo (chiave = nome del campo del modulo).</summary>
    protected ActionResult NonValido(IDictionary<string, string[]> errori) =>
        ValidationProblem(new ValidationProblemDetails(errori) { Title = "Controlla i campi segnati." });

    protected ObjectResult Errore(int stato, string titolo) =>
        Problem(title: titolo, statusCode: stato);
}
