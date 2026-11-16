using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Accesso unico per le app esterne (task #234, docs/accesso-unico.md): un'utenza sola, quella di 4Grapes. Queste rotte
/// non usano la chiave del Web (<c>X-Mappe-Chiave</c>) ma la chiave dell'app (<c>X-Mappe-App</c> + segreto), tranne
/// <c>chiavi</c>, che è pubblica. Errori come ProblemDetails con il titolo in italiano.
/// </summary>
[ApiController]
public class AccessoUnicoController : ControllerBase
{
    /// <summary>Le rotte che <see cref="Servizi.ControlloChiaveApi"/> lascia passare senza la chiave del Web.</summary>
    public static readonly IReadOnlySet<string> PercorsiApp = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/api/accesso/token", "/api/accesso/me", "/api/accesso/chiavi",
    };

    private readonly IUtentiRepository _utenti;
    private readonly IPermessiRepository _permessi;
    private readonly IAccessoUnicoRepository _accesso;
    private readonly TokenAccesso _token;
    private readonly ChiaviFirma _chiavi;
    private readonly LimitiAccessoUnico _limiti;
    private readonly AccessoUnicoOptions _opzioni;
    private readonly ILogger<AccessoUnicoController> _logger;

    public AccessoUnicoController(IUtentiRepository utenti, IPermessiRepository permessi, IAccessoUnicoRepository accesso,
        TokenAccesso token, ChiaviFirma chiavi, LimitiAccessoUnico limiti, IOptions<AccessoUnicoOptions> opzioni,
        ILogger<AccessoUnicoController> logger)
    {
        _utenti = utenti;
        _permessi = permessi;
        _accesso = accesso;
        _token = token;
        _chiavi = chiavi;
        _limiti = limiti;
        _opzioni = opzioni.Value;
        _logger = logger;
    }

    /// <summary>Email (LoginName di 4Grapes) e password → token firmato con profilo, ruoli, abilitazioni e aziende.</summary>
    [HttpPost("api/accesso/token")]
    public async Task<ActionResult<TokenAccessoDto>> Token(RichiestaTokenDto richiesta, CancellationToken ct)
    {
        var (erroreApp, nomeApp, app) = RiconosciApp();
        if (erroreApp is not null) return erroreApp;

        var email = richiesta.Email?.Trim() ?? "";
        var password = richiesta.Password ?? "";
        if (email.Length is 0 or > 50 || password.Length is 0 or > 256)
            return CredenzialiErrate();

        if (_limiti.ErroriEmail.Conteggio(email) >= _opzioni.MaxErrori)
            return Errore(StatusCodes.Status429TooManyRequests, "Troppi tentativi sbagliati: riprova fra 15 minuti.",
                (int)LimitiAccessoUnico.FinestraErrori.TotalSeconds);

        try
        {
            var utente = VerificaPassword.Scegli(await _utenti.GetCredenzialiAsync(email, ct), password);
            if (utente is null)
            {
                _limiti.ErroriEmail.Conta(email);
                return CredenzialiErrate();
            }

            var profilo = await ProfiloAsync(utente.Id, ct);
            if (profilo is null)
                return CredenzialiErrate();
            if (NonAbilitato(profilo, nomeApp, app) is { } vietato)
                return vietato;

            _limiti.ErroriEmail.Azzera(email);
            _logger.LogInformation("Token dell'accesso unico rilasciato all'app {App} per l'utente {IdUtente}", nomeApp, profilo.IdUtente);
            return Ok(new TokenAccessoDto(_token.Crea(profilo, nomeApp), "Bearer", (int)_token.Durata.TotalSeconds, profilo));
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    /// <summary>Il profilo riletto adesso da 4Grapes per il titolare del token (Authorization: Bearer).</summary>
    [HttpGet("api/accesso/me")]
    public async Task<ActionResult<ProfiloAccessoDto>> Me(CancellationToken ct)
    {
        var (erroreApp, nomeApp, app) = RiconosciApp();
        if (erroreApp is not null) return erroreApp;

        var autorizzazione = Request.Headers.Authorization.ToString();
        const string schema = "Bearer ";
        if (!autorizzazione.StartsWith(schema, StringComparison.OrdinalIgnoreCase)
            || await _token.VerificaAsync(autorizzazione[schema.Length..].Trim(), nomeApp) is not { } idUtente)
            return Errore(StatusCodes.Status401Unauthorized, "Token mancante, scaduto o non valido: accedi di nuovo.");

        try
        {
            var profilo = await ProfiloAsync(idUtente, ct);
            if (profilo is null)
                return Errore(StatusCodes.Status401Unauthorized, "L'utenza non è più attiva in 4Grapes.");
            return NonAbilitato(profilo, nomeApp, app) ?? (ActionResult<ProfiloAccessoDto>)Ok(profilo);
        }
        catch (SqlException ex)
        {
            return NonRisponde(ex);
        }
    }

    /// <summary>La chiave pubblica che verifica i token (JWKS, RFC 7517). Pubblica: non serve nessuna chiave.</summary>
    [HttpGet("api/accesso/chiavi")]
    public IActionResult Chiavi() => Ok(new { keys = new[] { _chiavi.Jwk } });

    /// <summary>
    /// Il profilo come lo dà 4Grapes adesso; null se l'utente non è attivo o non ha più l'app abilitata (stessa regola
    /// del login: <c>StatoEntita = 1</c> e <c>IsAppEnabled = 1</c>).
    /// </summary>
    private async Task<ProfiloAccessoDto?> ProfiloAsync(int idUtente, CancellationToken ct)
    {
        var anagrafica = await _accesso.GetAnagraficaAsync(idUtente, ct);
        if (anagrafica is not { IsAppEnabled: true })
            return null;
        var permessi = await _permessi.GetPermessiAsync(idUtente, ct);
        if (permessi is not { IsAppEnabled: true })
            return null;
        var aziende = await _accesso.GetAziendeAsync(idUtente, ct);
        return Profilo(anagrafica, permessi, aziende);
    }

    public static ProfiloAccessoDto Profilo(AnagraficaUtente anagrafica, PermessiUtente permessi, IReadOnlyList<AziendaUtente> aziende)
    {
        var ruoli = new List<string>();
        if (permessi.IsMasterUser) ruoli.Add("MasterUser");
        if (permessi.IsUtentePerleuve) ruoli.Add("UtentePerleuve");
        if (permessi.IsSysAdmin) ruoli.Add("SysAdmin");
        var nome = $"{anagrafica.Nome.Trim()} {anagrafica.Cognome.Trim()}".Trim();
        return new ProfiloAccessoDto(
            anagrafica.IdUtente,
            anagrafica.LoginName,
            nome.Length > 0 ? nome : anagrafica.LoginName,
            ruoli,
            permessi.Abilitazioni.Order(StringComparer.Ordinal).ToList(),
            aziende.Select(a => new AziendaAccessoDto(a.IdEnte, a.Nome, a.Gestione)).ToList(),
            permessi.Ridotti);
    }

    private ActionResult? NonAbilitato(ProfiloAccessoDto profilo, string nomeApp, AppEsternaOptions app)
    {
        var richiesta = app.Abilitazione?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(richiesta) || profilo.Abilitazioni.Contains(richiesta))
            return null;
        _logger.LogInformation("Utente {IdUtente} senza l'abilitazione {Abilitazione} per l'app {App}", profilo.IdUtente, richiesta, nomeApp);
        return Errore(StatusCodes.Status403Forbidden, $"La tua utenza 4Grapes non è abilitata a {nomeApp}.");
    }

    /// <summary>L'app chiamante riconosciuta e dentro il suo limite, oppure la risposta d'errore.</summary>
    private (ActionResult? Errore, string Nome, AppEsternaOptions App) RiconosciApp()
    {
        var (stato, nome, app) = AppChiamante.Riconosci(Request.Headers, _opzioni);
        if (stato == AppChiamante.Esito.Sconosciuta)
            return (Errore(StatusCodes.Status403Forbidden, "App non riconosciuta: servono X-Mappe-App e X-Mappe-App-Segreto validi."), "", null!);
        if (stato == AppChiamante.Esito.Disabilitata)
            return (Errore(StatusCodes.Status403Forbidden, $"L'app {nome} è disabilitata nell'accesso unico."), "", null!);
        if (_limiti.RichiesteApp.Conta(nome!) > Math.Max(1, app!.LimiteAlMinuto))
            return (Errore(StatusCodes.Status429TooManyRequests, "Troppe richieste da questa app: riprova fra un minuto.", 60), "", null!);
        return (null, nome!, app!);
    }

    private ObjectResult CredenzialiErrate()
    {
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Email o password sbagliate. La password è quella di 4Grapes.",
        };
        if (!string.IsNullOrWhiteSpace(_opzioni.PasswordDimenticata))
            problema.Extensions["passwordDimenticata"] = _opzioni.PasswordDimenticata;
        return new ObjectResult(problema) { StatusCode = problema.Status };
    }

    private ObjectResult NonRisponde(SqlException ex)
    {
        // Es. GRANT mancante o 4Grapes spento: si logga il motivo, mai le credenziali.
        _logger.LogError(ex, "Accesso unico: lettura da 4Grapes non riuscita (errore SQL {Numero})", ex.Number);
        return Errore(StatusCodes.Status503ServiceUnavailable, "4Grapes non risponde: riprova fra poco.");
    }

    private ObjectResult Errore(int stato, string titolo, int? riprovaFraSecondi = null)
    {
        if (riprovaFraSecondi is { } secondi)
            Response.Headers.RetryAfter = secondi.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new ObjectResult(new ProblemDetails { Status = stato, Title = titolo }) { StatusCode = stato };
    }
}
