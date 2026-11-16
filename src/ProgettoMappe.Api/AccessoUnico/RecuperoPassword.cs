using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.DataProtection;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>
/// «Password dimenticata» della sezione 22 (AccountController del portale, task #289). Come il portale: un codice nuovo in
/// <c>Utente.CodiceSicurezza</c> (uniqueidentifier, NULL per tutti: dba, 16/11) e una mail con il collegamento a «Reimposta
/// password»; la nuova password almeno 8 caratteri. In più del portale: il collegamento vale <see cref="Validita"/> (il codice va in
/// un testo firmato con la scadenza, dalle chiavi di ASP.NET Core) e si usa una volta sola (dopo, <c>CodiceSicurezza</c> torna NULL).
/// </summary>
public sealed class RecuperoPassword(IDataProtectionProvider protezione)
{
    public static readonly TimeSpan Validita = TimeSpan.FromHours(1);

    public const int MinimoCaratteri = 8;

    private readonly ITimeLimitedDataProtector _protettore =
        protezione.CreateProtector("ProgettoMappe.RecuperoPassword.v1").ToTimeLimitedDataProtector();

    /// <summary>Il testo del collegamento: utente e codice, firmati, con la scadenza.</summary>
    public string Codice(int idUtente, Guid codice, DateTimeOffset adesso) =>
        _protettore.Protect(string.Create(CultureInfo.InvariantCulture, $"{idUtente}:{codice:N}"), adesso + Validita);

    /// <summary>Utente e codice dal collegamento; null se è stato toccato, è scaduto o non è nostro.</summary>
    public (int IdUtente, Guid Codice)? Leggi(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo) || testo.Length > 2000) return null;
        try
        {
            var parti = _protettore.Unprotect(testo).Split(':');
            return parti.Length == 2 && int.TryParse(parti[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                   && Guid.TryParseExact(parti[1], "N", out var codice)
                ? (id, codice)
                : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Controlli della nuova password: come «Reimposta password» del portale (almeno 8 caratteri, contati senza spazi ai bordi).</summary>
    public static string? ErrorePassword(string? nuova, string? conferma)
    {
        if (string.IsNullOrEmpty(nuova) || nuova.Trim().Length < MinimoCaratteri) return "La password deve avere almeno 8 caratteri.";
        if (nuova.Length > 256) return "Al massimo 256 caratteri.";
        return nuova == conferma ? null : "Le due password non sono uguali.";
    }

    /// <summary>La mail, in italiano e in inglese (il portale la manda solo in inglese: «Reset Password»).</summary>
    public static string HtmlEmail(string nome, string collegamento)
    {
        var n = WebUtility.HtmlEncode(nome);
        var c = WebUtility.HtmlEncode(collegamento);
        return $"<p>Ciao {n},</p><p>per scegliere una nuova password di 4Grapes e delle app Perleuve apri questo collegamento entro un'ora: "
               + $"<a href=\"{c}\">reimposta la password</a>.</p><p>Se non l'hai chiesto tu, ignora questa mail: la password resta quella di prima.</p>"
               + $"<hr><p>Hello {n},</p><p>to choose a new password for 4Grapes and the Perleuve apps open this link within one hour: "
               + $"<a href=\"{c}\">reset your password</a>.</p><p>If you did not ask for it, ignore this email.</p>"
               + "<p style=\"color:#5f727f\">4Grapes · Perleuve</p>";
    }

    public const string Oggetto = "4Grapes: nuova password / Reset Password";
}
