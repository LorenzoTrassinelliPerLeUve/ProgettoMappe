namespace ProgettoMappe.Web.IndiceBigot;

public static class Percorsi
{
    /// <summary><c>~</c> all'inizio = cartella dell'utente che fa girare il Web.</summary>
    public static string Espandi(string percorso) =>
        percorso == "~" || percorso.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), percorso.Length > 2 ? percorso[2..] : "")
            : percorso;
}
