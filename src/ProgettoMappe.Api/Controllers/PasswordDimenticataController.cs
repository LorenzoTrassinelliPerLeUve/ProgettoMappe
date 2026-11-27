using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Api.Controllers;

/// <summary>
/// Sezione 22 «Accesso e password» (task #289): «Password dimenticata» e «Reimposta password» del portale (<c>ForgotPassword</c>,
/// <c>ResetPassword</c> di AccountController), come pagine Perleuve accanto all'accesso (<c>/perleuve/*</c>, dal proxy del Web).
/// Regole del portale: risposta uguale che l'email esista o no, codice in <c>Utente.CodiceSicurezza</c>, password almeno 8
/// caratteri, hash SHA-256 come <c>Utils.ComputeHash</c>. In più: collegamento valido un'ora e una volta sola, solo utenti che
/// possono entrare (attivi, app abilitata, una riga sola con quell'email), limiti di richieste, LogOp senza password né codice
/// (<c>TabelleScrivibili.Utente.FuoriDalLog</c>). La mail parte davvero solo con scritture «vere» e posta configurata.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class PasswordDimenticataController : Controller
{
    private const string Titolo = "Password dimenticata";
    private const string TitoloReimposta = "Nuova password";

    private readonly AccessoUnicoOptions _opzioni;
    private readonly LimitiAccessoUnico _limiti;
    private readonly RecuperoPassword _recupero;
    private readonly ScritturaFourGrapes _scrittura;
    private readonly IUtentiRepository _utenti;
    private readonly IPosta _posta;
    private readonly TimeProvider _tempo;
    private readonly ILogger<PasswordDimenticataController> _logger;

    public PasswordDimenticataController(IOptions<AccessoUnicoOptions> opzioni, LimitiAccessoUnico limiti, RecuperoPassword recupero,
        ScritturaFourGrapes scrittura, IUtentiRepository utenti, IPosta posta, TimeProvider tempo, ILogger<PasswordDimenticataController> logger)
    {
        _opzioni = opzioni.Value;
        _limiti = limiti;
        _recupero = recupero;
        _scrittura = scrittura;
        _utenti = utenti;
        _posta = posta;
        _tempo = tempo;
        _logger = logger;
    }

    public const string Inviata =
        "Se l'email è di un'utenza attiva ti abbiamo mandato un collegamento per scegliere la nuova password. Vale un'ora. "
        + "Non arriva? Guarda nella posta indesiderata o chiedi all'amministratore.";

    [HttpGet("~/perleuve/password-dimenticata")]
    public IActionResult Richiesta(string? email) =>
        _opzioni.RecuperoAttivo() ? Pagina(Titolo, ModuloRichiesta(email, null)) : Spento();

    [HttpPost("~/perleuve/password-dimenticata")]
    public async Task<IActionResult> RichiestaInvio([FromForm] string? email, CancellationToken ct)
    {
        if (!_opzioni.RecuperoAttivo()) return Spento();
        email = email?.Trim() ?? "";
        if (email.Length is 0 or > 50 || !email.Contains('@'))
            return Pagina(Titolo, ModuloRichiesta(email, "Scrivi l'email con cui entri."), StatusCodes.Status400BadRequest);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        if (_limiti.RichiesteRecupero.Conta("ip:" + ip) > LimitiAccessoUnico.MaxRecuperoPerIp
            || _limiti.RichiesteRecupero.Conta("e:" + email) > LimitiAccessoUnico.MaxRecuperoPerEmail)
            return Pagina(Titolo, ModuloRichiesta(email, "Troppe richieste: riprova fra un'ora."), StatusCodes.Status429TooManyRequests);

        try
        {
            var candidati = await _utenti.GetCredenzialiAsync(email, ct);
            if (candidati.Count != 1)
            {
                // Come il portale: chi chiede non sa se l'email esiste. Nel log solo quante righe (LoginName non è unico).
                _logger.LogInformation("Password dimenticata: {Righe} utenze attive con quell'email, nessuna mail", candidati.Count);
                return Pagina(Titolo, Avviso(Inviata));
            }

            var utente = candidati[0];
            var codice = Guid.NewGuid();
            var esito = await _scrittura.EseguiAsync(utente.Id, async op =>
            {
                await op.AggiornaAsync(TabelleScrivibili.Utente, utente.Id, new Dictionary<string, object?> { ["CodiceSicurezza"] = codice });
                var collegamento = _opzioni.IndirizzoPubblico() + "/perleuve/reimposta?codice="
                                   + Uri.EscapeDataString(_recupero.Codice(utente.Id, codice, _tempo.GetUtcNow()));
                var nome = $"{utente.Nome.Trim()} {utente.Cognome.Trim()}".Trim();
                var posta = PostaVera
                    ? await _posta.InviaAsync(new MessaggioPosta([email], RecuperoPassword.Oggetto, RecuperoPassword.HtmlEmail(nome, collegamento)), ct)
                    : new EsitoPosta(true, [email], null);
                // La riga «inviato» di LogOp dice a chi e come, mai il collegamento.
                await op.AnnotaAsync(TabelleScrivibili.Utente, RigaLogOp.Inviato,
                [
                    new("idUtente", utente.Id), new("Oggetto", RecuperoPassword.Oggetto),
                    new("Email", posta.Simulato ? "simulata" : posta.Riuscito ? "inviata" : "non partita"),
                ]);
                if (!posta.Riuscito) throw new InvalidOperationException(posta.Errore);
                return posta.Simulato;
            }, ct);
            _logger.LogInformation("Password dimenticata: codice per l'utente {IdUtente}{Prova}{Simulata}", utente.Id,
                esito.Prova ? " (prova, annullato)" : "", esito.Risultato ? ", mail simulata" : "");
            return Pagina(Titolo, Avviso(Inviata) + (esito.Prova || esito.Risultato
                ? "<p class=\"piccolo\">Anteprima: le scritture o la posta sono in prova, la mail non è partita.</p>"
                : ""));
        }
        catch (ScrittureSpenteException)
        {
            return Spento();
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or HttpRequestException)
        {
            _logger.LogError(ex, "Password dimenticata: richiesta non riuscita");
            return Pagina(Titolo, ModuloRichiesta(email, "Adesso non riesco a mandare la mail: riprova fra poco."),
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("~/perleuve/reimposta")]
    public IActionResult Reimposta(string? codice) =>
        !_opzioni.RecuperoAttivo() ? Spento()
        : _recupero.Leggi(codice) is null ? Scaduto()
        : Pagina(TitoloReimposta, ModuloReimposta(codice!, null));

    [HttpPost("~/perleuve/reimposta")]
    public async Task<IActionResult> ReimpostaInvio([FromForm] string? codice, [FromForm] string? password, [FromForm] string? conferma,
        CancellationToken ct)
    {
        if (!_opzioni.RecuperoAttivo()) return Spento();
        if (_recupero.Leggi(codice) is not { } dati) return Scaduto();
        if (RecuperoPassword.ErrorePassword(password, conferma) is { } errore)
            return Pagina(TitoloReimposta, ModuloReimposta(codice!, errore), StatusCodes.Status400BadRequest);

        try
        {
            var esito = await _scrittura.EseguiAsync(dati.IdUtente, async op =>
            {
                var riga = await op.LeggiAsync(TabelleScrivibili.Utente, dati.IdUtente);
                var valori = riga?.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
                if (valori is null || !Equals(valori.GetValueOrDefault("CodiceSicurezza"), dati.Codice)
                    || !Vero(valori.GetValueOrDefault("StatoEntita")) || !Vero(valori.GetValueOrDefault("IsAppEnabled")))
                    return false;
                await op.AggiornaAsync(TabelleScrivibili.Utente, dati.IdUtente, new Dictionary<string, object?>
                {
                    ["ShaPassword"] = PortaleAmministrazioneController.HashPassword(password!),
                    ["CodiceSicurezza"] = null,
                }, attesi: new Dictionary<string, object?> { ["CodiceSicurezza"] = dati.Codice });
                return true;
            }, ct);
            if (!esito.Risultato) return Scaduto();
            _logger.LogInformation("Password reimpostata per l'utente {IdUtente}{Prova}", dati.IdUtente, esito.Prova ? " (prova, annullato)" : "");
            return Pagina(TitoloReimposta, Avviso(esito.Prova
                    ? "Anteprima: le scritture sono in prova, la password non è cambiata."
                    : "Fatto: la password è cambiata. Vale per 4Grapes, Mappe e le app Perleuve.")
                + "<p><a href=\"/perleuve/accesso\">Vai all'accesso</a></p>");
        }
        catch (ScrittureSpenteException)
        {
            return Spento();
        }
        catch (ConflittoScritturaException)
        {
            return Scaduto();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Reimposta password: 4Grapes non ha accettato (errore SQL {Numero})", ex.Number);
            return Pagina(TitoloReimposta, ModuloReimposta(codice!, "4Grapes non risponde: riprova fra poco."), StatusCodes.Status503ServiceUnavailable);
        }
    }

    private bool PostaVera => _posta.Vera && _scrittura.Modo == ModoScritture.Vere;

    private static bool Vero(object? valore) => valore switch
    {
        bool b => b,
        null => false,
        _ => Convert.ToInt32(valore, System.Globalization.CultureInfo.InvariantCulture) == 1,
    };

    private IActionResult Spento() => Pagina(Titolo,
        Avviso("Il recupero della password da qui non è attivo.")
        + (string.IsNullOrWhiteSpace(_opzioni.PasswordDimenticata) ? ""
            : $"<p><a href=\"{HtmlEncoder.Default.Encode(_opzioni.PasswordDimenticata)}\">Recuperala dal portale 4Grapes</a></p>"),
        StatusCodes.Status404NotFound);

    private IActionResult Scaduto() => Pagina(TitoloReimposta,
        "<p class=\"errore\" role=\"alert\">Il collegamento non vale più (è scaduto, è già stato usato o ne hai chiesto uno più nuovo).</p>"
        + "<p><a href=\"/perleuve/password-dimenticata\">Chiedine uno nuovo</a></p>", StatusCodes.Status400BadRequest);

    private static string Avviso(string testo) => $"<p class=\"avviso\" role=\"status\">{HtmlEncoder.Default.Encode(testo)}</p>";

    private static string ModuloRichiesta(string? email, string? errore)
    {
        var h = HtmlEncoder.Default;
        var testo = new StringBuilder();
        if (errore is not null) testo.Append($"<p class=\"errore\" role=\"alert\">{h.Encode(errore)}</p>");
        testo.Append($"""
            <p>Scrivi l'email con cui entri in 4Grapes: ti mandiamo un collegamento per scegliere una password nuova.</p>
            <form method="post" action="/perleuve/password-dimenticata">
              <label>Email<input name="email" type="email" autocomplete="username" maxlength="50" required autofocus value="{h.Encode(email ?? "")}"></label>
              <button>Mandami il collegamento</button>
            </form>
            <p class="piccolo"><a href="/perleuve/accesso">Torna all'accesso</a></p>
            """);
        return testo.ToString();
    }

    private static string ModuloReimposta(string codice, string? errore)
    {
        var h = HtmlEncoder.Default;
        var testo = new StringBuilder();
        if (errore is not null) testo.Append($"<p class=\"errore\" role=\"alert\">{h.Encode(errore)}</p>");
        testo.Append($"""
            <form method="post" action="/perleuve/reimposta">
              <input type="hidden" name="codice" value="{h.Encode(codice)}">
              {OidcController.CampoPassword("password", "new-password", "it", autofocus: true, etichetta: "Nuova password", attributi: " minlength=\"8\"")}
              {OidcController.CampoPassword("conferma", "new-password", "it", etichetta: "Scrivila di nuovo", attributi: " minlength=\"8\"")}
              <button>Salva la password</button>
            </form>
            <p class="piccolo">Almeno 8 caratteri. Vale per 4Grapes, Mappe e le app Perleuve.</p>
            """);
        return testo.ToString();
    }

    private ContentResult Pagina(string titolo, string corpo, int stato = StatusCodes.Status200OK) =>
        OidcController.Pagina(Response, titolo, corpo, stato);
}
