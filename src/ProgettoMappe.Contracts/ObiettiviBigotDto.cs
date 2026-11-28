namespace ProgettoMappe.Contracts;

/// <summary>
/// Obiettivi dei parametri dell'Indice Bigot (task #419, tappa R6 della Reportistica, PROGETTO.md §5.1): per parametro e annata,
/// a tre livelli (predefinito per tutte le aziende → azienda → vigneto; vince il più specifico). I valori sono nella scala che la
/// pagina dell'Indice Bigot mostra per il parametro (<c>CalcoloIndiceBigot.ValoreNumerico</c>: vigoria 1–10, sanità in %,
/// SFE in m²/vite…), l'indice in punti 0–100.
/// </summary>
public static class ObiettiviBigot
{
    /// <summary>L'obiettivo sull'indice totale (somma dei punteggi), accanto ai parametri.</summary>
    public const string Indice = "INDICE";

    /// <summary>
    /// I parametri con obiettivo, nell'ordine dei fogli di Qlik, più l'indice. L'età del vigneto non c'è: non si governa durante
    /// la stagione (scelta provvisoria della task #419).
    /// </summary>
    public static readonly IReadOnlyList<string> Parametri =
        ["VIGORIA", "MORFOLOGIA", "SFE", "PRODUZIONE", "SFEKg", "REGIME IDRICO", "AMPELOPATIE", "BIODIVERSITA", Indice];

    /// <summary>Id ammesso (stesse maiuscole di 4Grapes); null se non è un parametro con obiettivo.</summary>
    public static string? Normalizza(string? id) =>
        Parametri.FirstOrDefault(p => string.Equals(p, id?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>I limiti della scala del parametro: fuori da qui un obiettivo non ha senso.</summary>
    public static (double Da, double A) Scala(string parametro) => parametro.ToUpperInvariant() switch
    {
        "VIGORIA" => (1, 10),
        "MORFOLOGIA" => (1, 6),
        "REGIME IDRICO" => (0, 4),
        "AMPELOPATIE" => (0, 100),
        "SFE" => (0, 10),
        "PRODUZIONE" => (0, 20),
        "SFEKG" => (0, 20),
        "BIODIVERSITA" => (0, 20),
        _ => (0, 100),
    };

    /// <summary>Annate ammesse.</summary>
    public const int AnnoDa = 2000, AnnoA = 2100;

    /// <summary>Il motivo per cui un obiettivo non si può salvare; null se va bene. Tutti vuoti = «torna al livello sopra».</summary>
    public static string? Controlla(SalvaObiettivoBigotDto o)
    {
        if (Normalizza(o.Parametro) is not { } parametro)
            return "Parametro non valido.";
        if (o.Anno is < AnnoDa or > AnnoA)
            return "Annata non valida.";
        if (o.IdEnte < 0 || o.IdVigneto < 0 || (o.IdVigneto > 0 && o.IdEnte == 0))
            return "Un obiettivo per vigneto va dato dentro la sua azienda.";
        var (da, a) = Scala(parametro);
        foreach (var (nome, v) in new[] { ("minimo", o.Minimo), ("obiettivo", o.Obiettivo), ("massimo", o.Massimo) })
            if (v is { } x && (double.IsNaN(x) || x < da || x > a))
                return $"Il {nome} deve stare fra {da:0.##} e {a:0.##}.";
        if (o.Minimo is { } min && o.Massimo is { } max && min > max)
            return "Il minimo non può superare il massimo.";
        if (o.Obiettivo is { } obj && ((o.Minimo is { } m1 && obj < m1) || (o.Massimo is { } m2 && obj > m2)))
            return "L'obiettivo deve stare fra minimo e massimo.";
        return null;
    }
}

/// <summary>Un obiettivo salvato (l'ultima versione della sua chiave). <c>IdEnte</c> 0 = predefinito, <c>IdVigneto</c> 0 = tutta l'azienda.</summary>
public sealed record ObiettivoBigotDto(int IdEnte, int IdVigneto, int Anno, string Parametro, double? Minimo, double? Obiettivo, double? Massimo,
    int Versione, string Autore, DateTimeOffset CreatoIl)
{
    /// <summary>Tutti vuoti: l'obiettivo è stato tolto e vale quello del livello sopra.</summary>
    public bool Vuoto => Minimo is null && Obiettivo is null && Massimo is null;
}

/// <summary>Gli obiettivi di un'annata: i predefiniti e, con un'azienda, i suoi e quelli dei suoi vigneti.</summary>
public sealed record ObiettiviBigotDto(int Anno, int IdEnte, IReadOnlyList<ObiettivoBigotDto> Obiettivi, IReadOnlyList<int> AnniConObiettivi);

public sealed record SalvaObiettivoBigotDto(int IdEnte, int IdVigneto, int Anno, string Parametro, double? Minimo, double? Obiettivo, double? Massimo,
    string? Autore);

/// <summary>«Copia dall'annata scorsa»: gli obiettivi del livello (predefinito o azienda con i suoi vigneti) che mancano nell'annata nuova.</summary>
public sealed record CopiaObiettiviBigotDto(int IdEnte, int DaAnno, int AdAnno, string? Autore);
