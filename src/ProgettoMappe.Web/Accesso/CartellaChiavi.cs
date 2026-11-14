namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Cartella delle chiavi che cifrano cookie di accesso e antiforgery (<c>DataProtection:Cartella</c>, task #210 e #222).
/// Le chiavi devono sopravvivere ai riavvii, altrimenti dopo ogni rilascio i cookie dei browser non valgono più.
/// Produzione: <c>/var/lib/mappe/dati/chiavi-web</c>, creata qui se manca (solo il proprietario la legge).
/// Se non si può scrivere (l'anteprima e i test girano come un altro utente) si torna alle chiavi predefinite.
/// </summary>
public static class CartellaChiavi
{
    /// <summary>La cartella pronta, oppure null con il motivo in <paramref name="problema"/>.</summary>
    public static string? Prepara(string? configurata, out string? problema)
    {
        problema = null;
        if (string.IsNullOrWhiteSpace(configurata))
            return null;
        try
        {
            if (!Directory.Exists(configurata))
            {
                if (OperatingSystem.IsWindows())
                    Directory.CreateDirectory(configurata);
                else
                    Directory.CreateDirectory(configurata, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            var prova = Path.Combine(configurata, $".prova-{Guid.NewGuid():N}");
            File.WriteAllBytes(prova, []);
            File.Delete(prova);
            return configurata;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problema = ex.Message;
            return null;
        }
    }
}
