using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// La chiave RSA (2048 bit) che firma i token RS256, del token semplice e di OpenID Connect. Le app la verificano con la
/// parte pubblica (<c>GET api/accesso/chiavi</c> o <c>/.well-known/jwks</c>): non possiedono niente che permetta di creare
/// token. Una seconda chiave (<see cref="Cifratura"/>) cifra codici e refresh token di OpenID Connect, che solo Mappe legge.
/// </summary>
public sealed class ChiaviFirma
{
    public ChiaviFirma(IOptions<AccessoUnicoOptions> opzioni, ILogger<ChiaviFirma> logger)
        : this(Carica(PercorsoDati.Scegli(opzioni.Value.ChiavePrivata), logger, "firma"),
            Carica(PercorsoCifratura(PercorsoDati.Scegli(opzioni.Value.ChiavePrivata)), logger, "cifratura"))
    {
    }

    public ChiaviFirma(RSA rsa, RSA? cifratura = null)
    {
        var pubblica = rsa.ExportParameters(false);
        // kid = impronta della chiave pubblica: cambia da sola quando si cambia la chiave.
        Kid = Base64UrlEncoder.Encode(SHA256.HashData([.. pubblica.Modulus!, .. pubblica.Exponent!]))[..16];
        Chiave = new RsaSecurityKey(rsa) { KeyId = Kid };
        Pubblica = new RsaSecurityKey(RSA.Create(pubblica)) { KeyId = Kid };
        Cifratura = new RsaSecurityKey(cifratura ?? RSA.Create(2048)) { KeyId = Kid + "-c" };
        Jwk = new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["alg"] = SecurityAlgorithms.RsaSha256,
            ["kid"] = Kid,
            ["n"] = Base64UrlEncoder.Encode(pubblica.Modulus),
            ["e"] = Base64UrlEncoder.Encode(pubblica.Exponent),
        };
    }

    public string Kid { get; }

    /// <summary>Chiave privata: solo per firmare.</summary>
    public RsaSecurityKey Chiave { get; }

    /// <summary>Solo la parte pubblica: per verificare (come fanno le app).</summary>
    public RsaSecurityKey Pubblica { get; }

    /// <summary>Chiave privata di cifratura di OpenID Connect (codici, refresh token).</summary>
    public RsaSecurityKey Cifratura { get; }

    /// <summary>La chiave pubblica come voce JWKS (RFC 7517).</summary>
    public IReadOnlyDictionary<string, string> Jwk { get; }

    private static string? PercorsoCifratura(string? percorso) =>
        percorso is null ? null : Path.ChangeExtension(percorso, null) + ".cifratura.pem";

    private static RSA Carica(string? percorso, ILogger logger, string uso)
    {
        var rsa = RSA.Create(2048);
        if (string.IsNullOrWhiteSpace(percorso))
        {
            logger.LogWarning("AccessoUnico:ChiavePrivata senza un percorso usabile: chiave di {Uso} in memoria, i token scadono al riavvio", uso);
            return rsa;
        }

        try
        {
            if (File.Exists(percorso))
            {
                rsa.ImportFromPem(File.ReadAllText(percorso));
                return rsa;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(percorso))!);
            var opzioni = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                opzioni.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var scrittore = new StreamWriter(percorso, opzioni))
            {
                scrittore.Write(rsa.ExportPkcs8PrivateKeyPem());
            }

            logger.LogInformation("Chiave di {Uso} dell'accesso unico creata in {Percorso}", uso, percorso);
            return rsa;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException)
        {
            logger.LogWarning(ex, "Chiave di {Uso} dell'accesso unico non leggibile né creabile in {Percorso}: chiave in memoria, i token scadono al riavvio", uso, percorso);
            return RSA.Create(2048);
        }
    }
}
