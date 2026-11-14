using System.Globalization;

namespace ProgettoMappe.Web.Portale;

/// <summary>
/// Una colonna della lista del Portale. <c>Valore</c> serve per ordinare (numeri e date in ordine vero),
/// <c>Testo</c> per mostrare e cercare; senza <c>Testo</c> il valore si scrive all'italiana (<see cref="StatoLista{T}.Formatta"/>).
/// </summary>
public sealed record ColonnaLista<T>(string Titolo, Func<T, object?> Valore, Func<T, string>? Testo = null, bool Numerica = false)
{
    public string TestoDi(T elemento) => Testo?.Invoke(elemento) ?? StatoLista<T>.Formatta(Valore(elemento));
}

/// <summary>Una pagina della lista: righe, totale dopo la ricerca, pagina corrente (da 0) e numero di pagine.</summary>
public sealed record PaginaLista<T>(IReadOnlyList<T> Righe, int Totale, int Pagina, int Pagine);

/// <summary>
/// Ricerca, ordine e pagine della lista comune del Portale (<c>Components/Portale/ListaPortale</c>), tenuti qui fuori dal
/// componente per poterli provare. La ricerca guarda il testo di tutte le colonne e ignora maiuscole e accenti.
/// </summary>
public sealed class StatoLista<T>
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    public string Ricerca { get; set; } = "";

    /// <summary>Indice della colonna di ordinamento, null = ordine dei dati.</summary>
    public int? Ordine { get; private set; }

    public bool Discendente { get; private set; }

    public int Pagina { get; set; }

    public int PerPagina { get; init; } = 50;

    /// <summary>Clic sul titolo di una colonna: prima crescente, poi decrescente, poi di nuovo crescente.</summary>
    public void OrdinaPer(int colonna)
    {
        Discendente = Ordine == colonna && !Discendente;
        Ordine = colonna;
        Pagina = 0;
    }

    public PaginaLista<T> Applica(IEnumerable<T> elementi, IReadOnlyList<ColonnaLista<T>> colonne)
    {
        IEnumerable<T> righe = elementi;
        var ricerca = Ricerca.Trim();
        if (ricerca.Length > 0)
        {
            righe = righe.Where(e => colonne.Any(c => Contiene(c.TestoDi(e), ricerca)));
        }

        if (Ordine is { } i && i >= 0 && i < colonne.Count)
        {
            var colonna = colonne[i];
            var confronto = Comparer<T>.Create((a, b) => Confronta(colonna.Valore(a), colonna.Valore(b)));
            // I vuoti restano in fondo anche in ordine decrescente.
            righe = Discendente
                ? righe.OrderBy(e => colonna.Valore(e) is null).ThenByDescending(e => e, confronto)
                : righe.OrderBy(e => colonna.Valore(e) is null).ThenBy(e => e, confronto);
        }

        var tutte = righe.ToList();
        var pagine = Math.Max(1, (tutte.Count + PerPagina - 1) / PerPagina);
        Pagina = Math.Clamp(Pagina, 0, pagine - 1);
        return new PaginaLista<T>(tutte.Skip(Pagina * PerPagina).Take(PerPagina).ToList(), tutte.Count, Pagina, pagine);
    }

    /// <summary>Valore all'italiana: 1.234,5; date gg/mm/aaaa (con l'ora se c'è); Sì/No; vuoto = «–».</summary>
    public static string Formatta(object? valore) => valore switch
    {
        null => "–",
        string s => s,
        bool b => b ? "Sì" : "No",
        DateTime d when d.TimeOfDay == TimeSpan.Zero => d.ToString("dd/MM/yyyy", Italiano),
        DateTime d => d.ToString("dd/MM/yyyy HH:mm", Italiano),
        DateOnly d => d.ToString("dd/MM/yyyy", Italiano),
        int or long or short => Convert.ToString(valore, Italiano) ?? "",
        decimal d => d.ToString("#,##0.##", Italiano),
        double d => d.ToString("#,##0.##", Italiano),
        float f => f.ToString("#,##0.##", Italiano),
        _ => Convert.ToString(valore, Italiano) ?? "",
    };

    private static bool Contiene(string testo, string cercato) =>
        Italiano.CompareInfo.IndexOf(testo, cercato, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static int Confronta(object? a, object? b) => (a, b) switch
    {
        (null, null) => 0,
        (null, _) => 1,
        (_, null) => -1,
        (string x, string y) => Italiano.CompareInfo.Compare(x, y, CompareOptions.IgnoreCase),
        (IComparable x, _) when a.GetType() == b.GetType() => x.CompareTo(b),
        _ => string.Compare(Formatta(a), Formatta(b), StringComparison.CurrentCultureIgnoreCase),
    };
}
