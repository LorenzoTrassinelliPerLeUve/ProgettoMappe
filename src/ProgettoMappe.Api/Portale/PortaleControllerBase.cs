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

    /// <summary>Il titolo del 503 quando 4Grapes nega la scrittura (errore SQL 229).</summary>
    public const string SenzaPermessoScrittura =
        "Mappe non ha ancora il permesso di salvare questi dati in 4Grapes: lo deve dare il proprietario. Niente è stato cambiato.";

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
    /// Il lavoro restituisce la chiave della riga principale (o null). <paramref name="seUsata"/> = il titolo del 409 quando
    /// una chiave esterna di 4Grapes blocca il salvataggio (altrimenti un testo generico).
    /// </summary>
    protected async Task<ActionResult<EsitoScritturaDto>> ScriviAsync(PermessiUtente utente,
        Func<IOperazioniScrittura, Task<object?>> lavoro, CancellationToken ct, string? seUsata = null)
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
        catch (RifiutoPortaleException ex)
        {
            return ex.Campi.Count == 0
                ? Errore(StatusCodes.Status400BadRequest, ex.Message)
                : NonValido(ex.Campi.ToDictionary(c => c, _ => new[] { ex.Message }));
        }
        catch (RifiutoFourGrapesException ex)
        {
            // Il messaggio della procedura di 4Grapes, così com'è (task #257).
            logger.LogWarning("Procedura di 4Grapes rifiutata (utente {IdUtente}): {Messaggio}", utente.IdUtente, ex.Message);
            return Errore(StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 229)
        {
            // Permesso negato: a ProgettoMappe_Scrittura manca un GRANT (lo dà il proprietario con lo script del dba).
            logger.LogError(ex, "Scrittura del Portale senza permesso in 4Grapes (utente {IdUtente})", utente.IdUtente);
            return Errore(StatusCodes.Status503ServiceUnavailable, SenzaPermessoScrittura);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            // Vincolo di chiave esterna: la riga è usata altrove (es. una linea di difesa in un trattamento).
            logger.LogWarning(ex, "Scrittura del Portale rifiutata da un vincolo (utente {IdUtente})", utente.IdUtente);
            return Errore(StatusCodes.Status409Conflict, seUsata ?? "4Grapes non lo permette: questi dati sono usati altrove. Niente è stato cambiato.");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Scrittura del Portale non riuscita (errore SQL {Numero}, utente {IdUtente})", ex.Number, utente.IdUtente);
            return Errore(StatusCodes.Status503ServiceUnavailable, "4Grapes non ha accettato il salvataggio: niente è stato cambiato.");
        }
    }

    /// <summary>
    /// Una lettura da 4Grapes: 200 con il risultato, 503 se 4Grapes non risponde o se a Mappe manca il permesso di leggere
    /// quell'oggetto (errore SQL 229: il titolo lo dice, il nome dell'oggetto va nel log).
    /// </summary>
    protected async Task<ActionResult<T>> LetturaAsync<T>(string cosa, Func<Task<T>> lettura)
    {
        try
        {
            return Ok(await lettura());
        }
        catch (SqlException ex)
        {
            return ErroreLettura(cosa, ex);
        }
    }

    protected ObjectResult ErroreLettura(string cosa, SqlException ex)
    {
        HttpContext.RequestServices.GetRequiredService<ILogger<PortaleControllerBase>>()
            .LogError(ex, "Portale: lettura di {Cosa} non riuscita (errore SQL {Numero})", cosa, ex.Number);
        return Errore(StatusCodes.Status503ServiceUnavailable, ex.Number == 229
            ? $"Mappe non ha ancora il permesso di leggere {cosa} in 4Grapes."
            : $"4Grapes non risponde: non riesco a leggere {cosa}. Riprova tra poco.");
    }

    /// <summary>400 con gli errori per campo (chiave = nome del campo del modulo).</summary>
    protected ActionResult NonValido(IDictionary<string, string[]> errori) =>
        ValidationProblem(new ValidationProblemDetails(errori) { Title = "Controlla i campi segnati." });

    protected ObjectResult Errore(int stato, string titolo) =>
        Problem(title: titolo, statusCode: stato);
}
