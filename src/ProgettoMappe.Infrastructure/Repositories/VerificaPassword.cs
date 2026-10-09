using System.Security.Cryptography;
using System.Text;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Confronto della password con <c>Utente.ShaPassword</c> di 4Grapes. Dall'analisi del dba
/// (2026-10-09): 5401 valori su 5405 sono 64 caratteri esadecimali (SHA-256), in maiuscolo o
/// minuscolo, senza sale né prefissi. Assunto da confermare: la password è codificata in UTF-8
/// prima dell'hash (dal database non si vede). I 4 valori con formato anomalo non corrispondono
/// mai: quegli utenti non entrano finché il formato non è chiarito.
/// </summary>
public static class VerificaPassword
{
    private const int LunghezzaSha256Esadecimale = 64;

    public static bool Corrisponde(string password, string? shaSalvato)
    {
        var salvato = shaSalvato?.Trim();
        if (string.IsNullOrEmpty(password) || salvato is null
            || salvato.Length != LunghezzaSha256Esadecimale || !salvato.All(char.IsAsciiHexDigit))
            return false;

        var calcolato = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(calcolato),
            Encoding.ASCII.GetBytes(salvato.ToUpperInvariant()));
    }

    /// <summary>
    /// L'utente che entra: deve esserci <b>esattamente una</b> riga con la password giusta. Con
    /// LoginName ripetuti (6 casi nei dati reali) e la stessa password su più righe non si sa
    /// quale utente sia: meglio negare l'accesso che sceglierne uno a caso.
    /// </summary>
    public static CredenzialiUtente? Scegli(IEnumerable<CredenzialiUtente> candidati, string password)
    {
        var validi = candidati.Where(c => Corrisponde(password, c.ShaPassword)).Take(2).ToList();
        return validi.Count == 1 ? validi[0] : null;
    }

    /// <summary>"Nome Cognome" ripulito; se entrambi vuoti, il LoginName.</summary>
    public static string NomeVisualizzato(CredenzialiUtente utente, string loginName)
    {
        var nome = $"{utente.Nome?.Trim()} {utente.Cognome?.Trim()}".Trim();
        return nome.Length > 0 ? nome : loginName;
    }
}
