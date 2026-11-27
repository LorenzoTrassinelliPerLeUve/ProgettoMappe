using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// Regole della sessione dell'accesso unico (task #403): il cookie «perleuve.accesso» porta l'Id della sessione
/// (<c>sid</c>, lo stesso che va negli id_token e nell'uscita unica) e l'ora dell'accesso (<c>inizio</c>). Scade dopo
/// <see cref="AccessoUnicoOptions.Inattivita"/> senza uso (scorrevole, nel cookie) e comunque dopo
/// <see cref="AccessoUnicoOptions.SessioneMassima"/> dall'accesso (controllato qui a ogni richiesta).
/// </summary>
public static class SessioneAccesso
{
    public const string ClaimSessione = "sid";
    public const string ClaimInizio = "inizio";

    /// <summary>Vero se la sessione iniziata a <paramref name="inizio"/> vale ancora (senza ora d'inizio = cookie vecchio: no).</summary>
    public static bool Valida(ClaimsPrincipal? principal, DateTimeOffset adesso, AccessoUnicoOptions opzioni) =>
        Inizio(principal) is { } inizio && inizio <= adesso.AddMinutes(1) && adesso - inizio < opzioni.SessioneMassima()
        && !string.IsNullOrEmpty(principal!.FindFirst(ClaimSessione)?.Value);

    public static DateTimeOffset? Inizio(ClaimsPrincipal? principal) =>
        long.TryParse(principal?.FindFirst(ClaimInizio)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var s)
            ? DateTimeOffset.FromUnixTimeSeconds(s)
            : null;

    public static string? Sessione(ClaimsPrincipal? principal) => principal?.FindFirst(ClaimSessione)?.Value;

    public static Claim ClaimDiInizio(DateTimeOffset adesso) =>
        new(ClaimInizio, adesso.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64);

    /// <summary>Id di sessione nuovo: 128 bit casuali.</summary>
    public static string NuovaSessione() => Base64UrlEncoder.Encode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
}

/// <summary>
/// Registro degli accessi dell'accesso unico (task #403): tabella <c>registro_accessi</c> nel database SQLite di OpenID
/// Connect (<c>AccessoUnico:Database</c>). Eventi: <c>accesso</c>, <c>accesso-negato</c>, <c>accesso-bloccato</c>,
/// <c>app</c> (entrato in un'app), <c>app-negata</c>, <c>uscita</c>, <c>scaduta</c>. Mai password; l'email solo per gli
/// accessi negati (per capire attacchi e blocchi). Un errore del registro non ferma mai l'accesso.
/// </summary>
public sealed class RegistroAccessi
{
    private readonly string _connessione;
    private readonly ILogger<RegistroAccessi> _logger;
    private bool _pronto;

    public RegistroAccessi(string database, ILogger<RegistroAccessi> logger)
    {
        _connessione = new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString();
        _logger = logger;
    }

    public record Riga(DateTimeOffset Quando, string Evento, int? IdUtente, string? App, string? Sessione, string? Ip, string? Dettaglio);

    public async Task AnnotaAsync(string evento, int? idUtente, string? app = null, string? sessione = null, IPAddress? ip = null,
        string? dettaglio = null)
    {
        try
        {
            await using var c = await ApriAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO registro_accessi (quando, evento, id_utente, app, sessione, ip, dettaglio) VALUES ($q, $e, $u, $a, $s, $i, $d)";
            cmd.Parameters.AddWithValue("$q", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$e", evento);
            cmd.Parameters.AddWithValue("$u", (object?)idUtente ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$a", (object?)app ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$s", (object?)sessione ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$i", (object?)ip?.ToString() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$d", dettaglio is null ? DBNull.Value : dettaglio.Length > 200 ? dettaglio[..200] : dettaglio);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning(ex, "Registro degli accessi non scrivibile: evento {Evento} dell'utente {IdUtente} perso", evento, idUtente);
        }
    }

    /// <summary>Gli ultimi eventi, dal più recente.</summary>
    public async Task<IReadOnlyList<Riga>> UltimiAsync(int quanti = 100)
    {
        await using var c = await ApriAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT quando, evento, id_utente, app, sessione, ip, dettaglio FROM registro_accessi ORDER BY id DESC LIMIT $n";
        cmd.Parameters.AddWithValue("$n", quanti);
        var righe = new List<Riga>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            righe.Add(new Riga(DateTimeOffset.Parse(r.GetString(0), CultureInfo.InvariantCulture), r.GetString(1),
                r.IsDBNull(2) ? null : r.GetInt32(2), r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6)));
        return righe;
    }

    private async Task<SqliteConnection> ApriAsync()
    {
        var c = new SqliteConnection(_connessione);
        await c.OpenAsync();
        if (!_pronto)
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS registro_accessi (id INTEGER PRIMARY KEY AUTOINCREMENT, quando TEXT NOT NULL,
                  evento TEXT NOT NULL, id_utente INTEGER, app TEXT, sessione TEXT, ip TEXT, dettaglio TEXT);
                CREATE INDEX IF NOT EXISTS ix_registro_utente ON registro_accessi (id_utente, quando);
                """;
            await cmd.ExecuteNonQueryAsync();
            _pronto = true;
        }
        return c;
    }
}

/// <summary>
/// «Esci» = uscita unica (task #403). Due canali di OpenID Connect, per le app che li dichiarano in
/// <see cref="AppEsternaOptions"/>: Back-Channel (POST di un <c>logout_token</c> firmato con le chiavi dei token, sub e
/// sid, mandato da qui a ogni app; le app che non conoscono la sessione lo ignorano) e Front-Channel (riquadri nascosti
/// nella pagina «Sei uscito», con <c>iss</c> e <c>sid</c>). Mappe Web li riceve tutti e due.
/// </summary>
public sealed class UscitaUnica(IHttpClientFactory http, ChiaviFirma chiavi, Microsoft.Extensions.Options.IOptions<AccessoUnicoOptions> opzioni,
    ILogger<UscitaUnica> logger)
{
    public const string ClientHttp = "uscita-unica";
    public const string EventoLogout = "http://schemas.openid.net/event/backchannel-logout";

    /// <summary>Il logout_token per un'app (JWT RS256, tipo «logout+jwt», 2 minuti).</summary>
    public string LogoutToken(string emittente, string clientId, string sub, string sid) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = emittente,
            Audience = clientId,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(2),
            TokenType = "logout+jwt",
            Claims = new Dictionary<string, object>
            {
                ["sub"] = sub, ["sid"] = sid, ["jti"] = Guid.NewGuid().ToString("N"),
                ["events"] = new Dictionary<string, object> { [EventoLogout] = new Dictionary<string, object>() },
            },
            SigningCredentials = new SigningCredentials(chiavi.Chiave, SecurityAlgorithms.RsaSha256),
        });

    /// <summary>
    /// Indirizzo d'uscita di un'app: assoluto com'è; un percorso («/accesso/…») vale sullo stesso sito dell'accesso
    /// (l'issuer), così Mappe, che sta sullo stesso dominio, funziona con ogni suo indirizzo (sslip, 4grapes.it).
    /// </summary>
    public static Uri? Indirizzo(string? configurato, string emittente) =>
        string.IsNullOrWhiteSpace(configurato) ? null
        : configurato.StartsWith('/') && !configurato.StartsWith("//") && Uri.TryCreate(emittente, UriKind.Absolute, out var base_)
            ? new Uri(base_, configurato)
        : Uri.TryCreate(configurato.Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp) ? u
        : null;

    /// <summary>Manda il logout_token a tutte le app con <see cref="AppEsternaOptions.BackChannelLogoutUri"/>, in parallelo, 5 s al massimo.</summary>
    public async Task<int> CanaleServerAsync(string emittente, string sub, string sid)
    {
        var app = opzioni.Value.App.Where(a => a.Value.Abilitata).Select(a => (a.Key, Uri: Indirizzo(a.Value.BackChannelLogoutUri, emittente)))
            .Where(a => a.Uri is not null).ToList();
        var client = http.CreateClient(ClientHttp);
        client.Timeout = TimeSpan.FromSeconds(5);
        var esiti = await Task.WhenAll(app.Select(async a =>
        {
            try
            {
                using var risposta = await client.PostAsync(a.Uri, new FormUrlEncodedContent(
                    new Dictionary<string, string> { ["logout_token"] = LogoutToken(emittente, a.Key, sub, sid) }));
                if (!risposta.IsSuccessStatusCode)
                    logger.LogWarning("Uscita unica: l'app {App} ha risposto {Stato}", a.Key, (int)risposta.StatusCode);
                else
                    logger.LogInformation("Uscita unica: avvisata l'app {App}", a.Key);
                return risposta.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning("Uscita unica: l'app {App} non risponde ({Errore})", a.Key, ex.Message);
                return false;
            }
        }));
        return esiti.Count(e => e);
    }

    /// <summary>Gli indirizzi Front-Channel da aprire nella pagina «Sei uscito», con iss e sid.</summary>
    public IEnumerable<string> CanaleBrowser(string emittente, string sid) =>
        opzioni.Value.App.Values
            .Where(a => a.Abilitata)
            .Select(a => Indirizzo(a.FrontChannelLogoutUri, emittente)?.AbsoluteUri)
            .OfType<string>()
            .Select(u => u + (u.Contains('?') ? "&" : "?") + "iss=" + Uri.EscapeDataString(emittente) + "&sid=" + Uri.EscapeDataString(sid))
            .Distinct();

    /// <summary>Corpo della pagina «Sei uscito»: riquadri nascosti e il collegamento per proseguire (dopo 2 s da solo).</summary>
    public static string PaginaUscita(IEnumerable<string> riquadri, string prosegui, string testo, string continua)
    {
        var h = HtmlEncoder.Default;
        // Il meta nel corpo i browser lo seguono comunque: il tempo di caricare i riquadri, poi si prosegue.
        var corpo = new StringBuilder($"<meta http-equiv=\"refresh\" content=\"2;url={h.Encode(prosegui)}\">"
            + $"<p class=\"avviso\" role=\"status\">{h.Encode(testo)}</p>");
        foreach (var r in riquadri)
            corpo.Append($"<iframe src=\"{h.Encode(r)}\" hidden title=\"uscita\" style=\"display:none\"></iframe>");
        corpo.Append($"<p><a href=\"{h.Encode(prosegui)}\" id=\"prosegui\">{h.Encode(continua)}</a></p>");
        return corpo.ToString();
    }
}
