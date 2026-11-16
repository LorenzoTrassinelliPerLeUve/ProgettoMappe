using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// Crea e verifica i JWT dell'accesso unico (RS256). Contenuto in docs/accesso-unico.md: <c>iss</c> = Emittente,
/// <c>aud</c> = l'app che l'ha chiesto, <c>sub</c> = IdUtente, <c>email</c>, <c>name</c>, <c>ruoli</c>,
/// <c>abilitazioni</c>, <c>aziende</c> (id, nome, gestione), <c>permessi_ridotti</c>, <c>iat</c>/<c>exp</c>, <c>jti</c>.
/// </summary>
public sealed class TokenAccesso
{
    /// <summary>Tolleranza sugli orologi fra Mappe e le app.</summary>
    public static readonly TimeSpan Tolleranza = TimeSpan.FromSeconds(30);

    private readonly ChiaviFirma _chiavi;
    private readonly AccessoUnicoOptions _opzioni;
    private readonly TimeProvider _orologio;
    private readonly JsonWebTokenHandler _gestore = new() { SetDefaultTimesOnTokenCreation = false };

    public TokenAccesso(ChiaviFirma chiavi, IOptions<AccessoUnicoOptions> opzioni, TimeProvider orologio)
    {
        _chiavi = chiavi;
        _opzioni = opzioni.Value;
        _orologio = orologio;
    }

    public TimeSpan Durata => _opzioni.Durata();

    public string Crea(ProfiloAccessoDto profilo, string app)
    {
        var adesso = _orologio.GetUtcNow().UtcDateTime;
        var descrittore = new SecurityTokenDescriptor
        {
            Issuer = _opzioni.Emittente,
            Audience = app,
            IssuedAt = adesso,
            NotBefore = adesso,
            Expires = adesso + Durata,
            SigningCredentials = new SigningCredentials(_chiavi.Chiave, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = profilo.IdUtente.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [JwtRegisteredClaimNames.Email] = profilo.Email,
                [JwtRegisteredClaimNames.Name] = profilo.Nome,
                ["ruoli"] = profilo.Ruoli.ToList(),
                ["abilitazioni"] = profilo.Abilitazioni.ToList(),
                ["aziende"] = Aziende(profilo),
                ["tutte_le_aziende"] = profilo.TutteLeAziende,
                ["abilitazioni_azienda"] = AbilitazioniAzienda(profilo),
                ["permessi_ridotti"] = profilo.PermessiRidotti,
            },
        };
        return _gestore.CreateToken(descrittore);
    }

    /// <summary>Le aziende come nel token: id, nome, gestione e ruolo (solo se valorizzato).</summary>
    public static List<Dictionary<string, object>> Aziende(ProfiloAccessoDto profilo) =>
        profilo.Aziende.Select(a =>
        {
            var azienda = new Dictionary<string, object> { ["id"] = a.Id, ["nome"] = a.Nome, ["gestione"] = a.Gestione };
            if (!string.IsNullOrEmpty(a.Ruolo))
                azienda["ruolo"] = a.Ruolo;
            return azienda;
        }).ToList();

    /// <summary>Le abilitazioni per una sola azienda come nel token: codice e id dell'azienda.</summary>
    public static List<Dictionary<string, object>> AbilitazioniAzienda(ProfiloAccessoDto profilo) =>
        (profilo.AbilitazioniAzienda ?? [])
        .Select(a => new Dictionary<string, object> { ["codice"] = a.Codice, ["azienda"] = a.IdAzienda })
        .ToList();

    /// <summary>L'IdUtente se il token è nostro, intatto, non scaduto e rilasciato a questa app; altrimenti null.</summary>
    public async Task<int?> VerificaAsync(string token, string app, IEnumerable<string>? altriEmittenti = null)
    {
        var parametri = new TokenValidationParameters
        {
            // Anche gli access token di OpenID Connect: stessa chiave, emittente = l'indirizzo pubblico.
            ValidIssuers = [_opzioni.Emittente, .. altriEmittenti ?? []],
            ValidAudience = app,
            IssuerSigningKey = _chiavi.Pubblica,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            LifetimeValidator = (inizio, fine, _, _) =>
            {
                var adesso = _orologio.GetUtcNow().UtcDateTime;
                return fine is { } f && adesso < f + Tolleranza && (inizio is not { } i || adesso + Tolleranza >= i);
            },
        };
        var esito = await _gestore.ValidateTokenAsync(token, parametri);
        return esito.IsValid
               && esito.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub)
               && int.TryParse(sub?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }
}
