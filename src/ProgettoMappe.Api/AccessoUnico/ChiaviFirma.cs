using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// La chiave RSA (2048 bit) che firma i token RS256. Le app la verificano con la parte pubblica
/// (<c>GET api/accesso/chiavi</c>, formato JWKS): non possiedono niente che permetta di creare token.
/// </summary>
public sealed class ChiaviFirma
{
    public ChiaviFirma(IOptions<AccessoUnicoOptions> opzioni, ILogger<ChiaviFirma> logger)
        : this(Carica(opzioni.Value.ChiavePrivata, logger))
    {
    }

    public ChiaviFirma(RSA rsa)
    {
        var pubblica = rsa.ExportParameters(false);
        // kid = impronta della chiave pubblica: cambia da sola quando si cambia la chiave.
        Kid = Base64UrlEncoder.Encode(SHA256.HashData([.. pubblica.Modulus!, .. pubblica.Exponent!]))[..16];
        Chiave = new RsaSecurityKey(rsa) { KeyId = Kid };
        Pubblica = new RsaSecurityKey(RSA.Create(pubblica)) { KeyId = Kid };
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

    /// <summary>La chiave pubblica come voce JWKS (RFC 7517).</summary>
    public IReadOnlyDictionary<string, string> Jwk { get; }

    private static RSA Carica(string? percorso, ILogger logger)
    {
        var rsa = RSA.Create(2048);
        if (string.IsNullOrWhiteSpace(percorso))
        {
            logger.LogWarning("AccessoUnico:ChiavePrivata non configurata: chiave in memoria, i token scadono al riavvio");
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

            logger.LogInformation("Chiave dell'accesso unico creata in {Percorso}", percorso);
            return rsa;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or ArgumentException)
        {
            logger.LogWarning(ex, "Chiave dell'accesso unico non leggibile né creabile in {Percorso}: chiave in memoria, i token scadono al riavvio", percorso);
            return RSA.Create(2048);
        }
    }
}
