using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Una classe della legenda. <see cref="Speciale"/>: classe fuori scala (es. "Mancano dati per il punteggio", "Dato anomalo").
/// <see cref="Soglia"/>: l'intervallo di valori che la classe rappresenta, scritto dopo l'etichetta
/// (solo per le classi a parole: in quelle numeriche l'etichetta è già l'intervallo).
/// </summary>
public sealed record ClasseTematismo(string Etichetta, string Colore, bool Speciale = false, string? Soglia = null)
{
    public string Testo => Soglia is null ? Etichetta : $"{Etichetta}: {Soglia}";
}

/// <summary>Da dove vengono le righe di un tematismo: ciascuna fonte ha i suoi anni e la sua copertura.</summary>
public enum FonteTematismo
{
    /// <summary>VistaIndiceBIGOT (<see cref="IndiceBigotDto"/>).</summary>
    Bigot,

    /// <summary>VistaStatisticheMappaVigneti (<see cref="RischioVignetoDto"/>).</summary>
    Rischi,

    /// <summary>VistaStatisticheVigoriaMappaVigneti (<see cref="VigoriaVignetoDto"/>).</summary>
    Vigoria,

    /// <summary>VistaStatistichePotenzialeIdricoMappaVigneti (<see cref="PotenzialeIdricoVignetoDto"/>): senza anno.</summary>
    PotenzialeIdrico,
}

/// <summary>
/// Tematismo su un parametro (BIGOT o statistiche 4Grapes): come assegnare una classe (indice in
/// <see cref="Classi"/>) a ogni riga della sua <see cref="Fonte"/>. Null = nessun dato per quel
/// vigneto/anno (mostrato come "n.d."); le righe di un'altra fonte danno null.
/// </summary>
public sealed class Tematismo
{
    public required string Id { get; init; }

    public FonteTematismo Fonte { get; init; } = FonteTematismo.Bigot;

    /// <summary>Un valore per vigneto, senza anno (potenziale idrico): niente cursore né confronto.</summary>
    public bool SenzaAnno => Fonte == FonteTematismo.PotenzialeIdrico;

    /// <summary>Titolo del gruppo nel menu «Colora per».</summary>
    public string Gruppo => Fonte == FonteTematismo.Bigot ? "Indice BIGOT" : "Rischi, vigoria e suolo";

    /// <summary>
    /// Voce di legenda dei vigneti senza riga nella fonte (nell'anno, se la fonte ha anni);
    /// <c>{anno}</c> diventa l'anno mostrato (vedi <see cref="EtichettaSenzaRiga"/>).
    /// </summary>
    public string EtichettaFuoriPerimetro { get; init; } = TematismiBigot.EtichettaFuoriPerimetro;

    /// <summary>«Non valutato nel 2025»: l'etichetta dei vigneti senza riga, con l'anno mostrato.</summary>
    public string EtichettaSenzaRiga(int? anno) =>
        EtichettaFuoriPerimetro.Replace("{anno}", anno?.ToString(CultureInfo.InvariantCulture) ?? "periodo", StringComparison.Ordinal);
    public required string Nome { get; init; }
    public string? Unita { get; init; }
    public string? Nota { get; init; }
    public required IReadOnlyList<ClasseTematismo> Classi { get; init; }
    public required Func<IRigaVigneto, int?> Classifica { get; init; }

    /// <summary>
    /// Solo per i tematismi a categorie: il valore della riga se non è fra quelli previsti (finisce
    /// nella classe "Altro"), altrimenti null. Serve a segnalarlo nel log.
    /// </summary>
    public Func<IRigaVigneto, string?>? ValoreNonPrevisto { get; init; }

    /// <summary>
    /// Valore da confrontare fra due anni (numero, o posizione della classe nei tematismi a parole);
    /// null = non confrontabile (vuoto, incompleto, anomalo, "Altro").
    /// </summary>
    public required Func<IRigaVigneto, decimal?> ValoreConfronto { get; init; }

    /// <summary>Il valore come si legge nella scheda del vigneto (null = vuoto).</summary>
    public required Func<IRigaVigneto, string?> TestoValore { get; init; }
}

/// <summary>
/// Esito di un tematismo su un anno: IdVigneto → classe, i vigneti che hanno una riga BIGOT
/// nell'anno (anche senza valore per questo parametro) e i valori non previsti trovati.
/// </summary>
public sealed record ClassificazioneTematismo(
    IReadOnlyDictionary<int, int> Classi,
    IReadOnlyCollection<string> ValoriNonPrevisti,
    IReadOnlySet<int> ConRiga)
{
    /// <summary>
    /// Classe del vigneto: indice in <see cref="Tematismo.Classi"/>, oppure
    /// <see cref="TematismiBigot.ClasseNessunDato"/> (valutato nell'anno, parametro vuoto) o
    /// <see cref="TematismiBigot.ClasseFuoriPerimetro"/> (nessuna riga BIGOT nell'anno).
    /// </summary>
    public int ClasseDi(int idVigneto) =>
        Classi.TryGetValue(idVigneto, out var classe) ? classe
        : ConRiga.Contains(idVigneto) ? TematismiBigot.ClasseNessunDato
        : TematismiBigot.ClasseFuoriPerimetro;
}

/// <summary>
/// Definizione dei tematismi sulla vista 4Grapes VistaIndiceBIGOT (prima versione).
/// Regole concordate con l'utente:
/// <list type="bullet">
/// <item>Punteggio e Ampelopatie (scala 0–100): 100 è positivo → scala rosso → verde.</item>
/// <item>Punteggio solo per vigneti completi (9 parametri su 9): con completezza diversa i
/// punteggi non sono confrontabili; gli altri cadono nella classe "Mancano dati per il punteggio".</item>
/// <item>Gli altri parametri non hanno una direzione nota: scala neutra sequenziale (nessun
/// rosso/verde). Produzione in kg/ceppo; le altre unità non sono note.</item>
/// </list>
/// Le soglie numeriche sono provvisorie, scelte sulla distribuzione osservata nei dati reali
/// (2019–2026); sono fisse, quindi i colori sono confrontabili tra aziende e anni.
/// </summary>
public static class TematismiBigot
{
    public const int ParametriTotali = 9;

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    // Scala "positiva" (valore alto = buono): rosso → verde (ColorBrewer RdYlGn).
    private static readonly string[] Positiva5 = ["#d73027", "#fc8d59", "#fee08b", "#91cf60", "#1a9850"];

    // Scale neutre sequenziali (viridis), chiaro → scuro: nessuna connotazione buono/cattivo.
    private static readonly string[] Neutra4 = ["#fde725", "#35b779", "#31688e", "#440154"];
    private static readonly string[] Neutra5 = ["#fde725", "#5ec962", "#21918c", "#3b528b", "#440154"];
    private static readonly string[] Neutra6 = ["#fde725", "#7ad151", "#22a884", "#2a788e", "#414487", "#440154"];
    private static readonly string[] Neutra8 = ["#fde725", "#a0da39", "#4ac16d", "#1fa187", "#277f8e", "#365c8d", "#46327e", "#440154"];

    private const string ColoreSpeciale = "#bdbdbd";

    // Classe "Altro" dei tematismi a categorie: fuori dalle scale neutre (viridis) e dal grigio.
    public const string EtichettaAltro = "Altro (valore non previsto)";
    private const string ColoreAltro = "#e7298a";

    /// <summary>Vigneto con una riga BIGOT nell'anno, ma senza valore per il parametro scelto.</summary>
    public const int ClasseNessunDato = -1;

    /// <summary>Vigneto senza nessuna riga BIGOT nell'anno: fuori dal perimetro del monitoraggio.</summary>
    public const int ClasseFuoriPerimetro = -2;

    public const string EtichettaNessunDato = "Nessun dato per questo parametro";

    /// <summary>Punteggio di un vigneto con meno di <see cref="ParametriTotali"/> parametri.</summary>
    public const string EtichettaIncompleto = "Mancano dati per il punteggio";
    public const string EtichettaFuoriPerimetro = "Non valutato nel {anno}";

    public static readonly IReadOnlyList<Tematismo> Tutti =
    [
        Punteggio(),
        // Soglie delle classi a parole: quelle con cui VistaIndiceBIGOT le calcola dal valore del
        // parametro (definizione della vista letta dal dba, 2026-10-13). La vista non espone il valore.
        Categorico("vigoria", "Vigoria", r => r.Vigoria, Neutra8,
            ["MOLTO BASSA", "BASSA", "MEDIO BASSA", "MEDIA", "MEDIO ALTA", "ALTA", "MOLTO ALTA", "ECCESSIVA"],
            ["≤ 1", "1 – 2,5", "2,5 – 4", "4 – 5,5", "5,5 – 7", "7 – 8,5", "8,5 – 10", "≥ 10"]),
        Numerico("sfe", "SFE", null, r => r.Sfe, [1.5m, 2.0m, 2.5m, 3.0m], Neutra5),
        Numerico("produzione", "Produzione", "kg/ceppo", r => r.Produzione, [1.0m, 1.5m, 2.0m, 3.0m], Neutra5),
        Numerico("sfekg", "SFE/kg", null, r => r.SfeKg, [0.8m, 1.2m, 1.6m, 2.2m], Neutra5),
        EtaVigneto(),
        Categorico("morfologia", "Morfologia", r => r.Morfologia, Neutra6,
            ["MOLTO SPARGOLO", "SPARGOLO", "MEDIO SPARGOLO", "MEDIO COMPATTO", "COMPATTO", "MOLTO COMPATTO"],
            ["6 – 7", "5 – 6", "4 – 5", "3 – 4", "2 – 3", "1 – 2"]),
        Numerico("ampelopatie", "Ampelopatie", null, r => r.Ampelopatie, [90m, 95m, 98m, 99.5m], Positiva5,
            "Scala da 0 a 100."),
        Categorico("regimeidrico", "Regime idrico", r => r.RegimeIdrico, Neutra5,
            ["ASSENTE", "LIEVE", "MEDIO", "SEVERO", "MOLTO SEVERO"],
            ["0 – 1", "1 – 2", "2 – 3", "3 – 4", "4 – 5"]),
        Numerico("biodiversita", "Biodiversità", null, r => r.Biodiversita, [0.5m, 1.5m, 2.5m], Neutra4),
    ];

    public static Tematismo? Trova(string? id) => Tutti.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Anno proposto all'apertura: l'ultimo anno CONCLUSO (precedente a quello corrente) tra quelli
    /// disponibili; se non ce ne sono, l'ultimo disponibile (anche se in corso).
    /// </summary>
    public static int? AnnoPredefinito(IEnumerable<int> anniDisponibili, int annoCorrente)
    {
        var anni = anniDisponibili.Distinct().ToList();
        return anni.Where(a => a < annoCorrente).Cast<int?>().Max() ?? anni.Cast<int?>().Max();
    }

    /// <summary>Ora italiana, qualunque sia il fuso del server (in produzione è UTC).</summary>
    public static readonly TimeZoneInfo FusoItalia =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Rome", out var fuso) ? fuso : TimeZoneInfo.Local;

    /// <summary>
    /// "BIGOT aggiornato alle HH:MM" (ora dell'ultimo caricamento della cache sull'Api); se non è di
    /// oggi si aggiunge il giorno, perché un'ora sola farebbe sembrare recenti dati di ieri.
    /// </summary>
    public static string EtichettaAggiornamento(DateTimeOffset aggiornamento, DateTimeOffset adesso, TimeZoneInfo fuso,
        string titolo = "BIGOT aggiornato")
    {
        var quando = TimeZoneInfo.ConvertTime(aggiornamento, fuso);
        var oggi = TimeZoneInfo.ConvertTime(adesso, fuso);
        return quando.Date == oggi.Date
            ? $"{titolo} alle {quando:HH:mm}"
            : $"{titolo} alle {quando:HH:mm} del {quando:dd/MM}";
    }

    /// <summary>
    /// IdVigneto → indice di classe, per le righe dell'anno indicato (vigneti senza dato esclusi),
    /// con i valori di categoria non previsti (finiti in "Altro") da segnalare. Le righe sono quelle
    /// della fonte del tematismo; per un tematismo senza anno l'anno non conta.
    /// </summary>
    public static ClassificazioneTematismo ClassiPerVigneto(Tematismo tematismo, IEnumerable<IRigaVigneto> righe, int? anno)
    {
        var risultato = new Dictionary<int, int>();
        var nonPrevisti = new SortedSet<string>(StringComparer.Ordinal);
        var conRiga = new HashSet<int>();
        foreach (var riga in righe.Where(r => tematismo.SenzaAnno || r is IRigaAnnuale a && a.Anno == anno))
        {
            conRiga.Add(riga.IdVigneto);
            if (tematismo.Classifica(riga) is { } classe)
            {
                risultato[riga.IdVigneto] = classe;
            }

            if (tematismo.ValoreNonPrevisto?.Invoke(riga) is { } valore)
            {
                nonPrevisti.Add(valore);
            }
        }

        return new ClassificazioneTematismo(risultato, nonPrevisti, conRiga);
    }

    /// <summary>
    /// Conteggi della legenda: classe (anche <see cref="ClasseNessunDato"/> e
    /// <see cref="ClasseFuoriPerimetro"/>) → numero di vigneti fra quelli indicati.
    /// </summary>
    public static Dictionary<int, int> Conteggi(ClassificazioneTematismo classificazione, IEnumerable<int> idVigneti) =>
        idVigneti.GroupBy(classificazione.ClasseDi).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Filtro dalla legenda: solo i vigneti della classe scelta (null = nessun filtro, tutti).
    /// Senza classificazione (tematismo spento o indici non pronti) il filtro non si applica.
    /// </summary>
    public static IReadOnlyList<T> FiltraPerClasse<T>(IReadOnlyList<T> vigneti, Func<T, int> id,
        ClassificazioneTematismo? classificazione, int? classe) =>
        classificazione is null || classe is not { } c
            ? vigneti
            : vigneti.Where(v => classificazione.ClasseDi(id(v)) == c).ToList();

    private static Tematismo Punteggio()
    {
        var soglie = new[] { 40m, 55m, 70m, 85m };
        var classi = ClassiNumeriche(soglie, Positiva5, null).ToList();
        var incompleto = classi.Count;
        classi.Add(new ClasseTematismo(EtichettaIncompleto, ColoreSpeciale, Speciale: true));

        return new Tematismo
        {
            Id = "punteggio",
            Nome = "Indice BIGOT (punteggio)",
            // Con completezza diversa i punteggi non sono confrontabili: per questo solo i vigneti 9/9.
            Nota = $"Punteggio solo per i vigneti con tutti i {ParametriTotali} parametri",
            Classi = classi,
            Classifica = Su<IndiceBigotDto, int?>(r => r.Punteggio is not { } p ? null
                : r.NumParametri == ParametriTotali ? ClasseNumerica(p, soglie) : incompleto),
            ValoreConfronto = Su<IndiceBigotDto, decimal?>(r => r.NumParametri == ParametriTotali ? r.Punteggio : null),
            TestoValore = Su<IndiceBigotDto, string?>(r => r.Punteggio is not { } p ? null
                : r.NumParametri == ParametriTotali ? Numero(p) : $"{Numero(p)} (incompleto)"),
        };
    }

    private static Tematismo EtaVigneto()
    {
        var soglie = new[] { 5m, 15m, 30m, 50m };
        var classi = ClassiNumeriche(soglie, Neutra5, "anni").ToList();
        var anomalo = classi.Count;
        classi.Add(new ClasseTematismo("Dato anomalo", ColoreSpeciale, Speciale: true));

        return new Tematismo
        {
            Id = "eta",
            Nome = "Età vigneto",
            Unita = "anni",
            Nota = "Età negative o superiori a 150 (es. un anno al posto dell'età) sono dati anomali di 4Grapes, mostrati come tali.",
            Classi = classi,
            Classifica = Su<IndiceBigotDto, int?>(r => r.EtaVigneto is not { } e ? null
                : e is < 0 or > 150 ? anomalo : ClasseNumerica(e, soglie)),
            ValoreConfronto = Su<IndiceBigotDto, decimal?>(r => r.EtaVigneto is { } e and >= 0 and <= 150 ? e : null),
            TestoValore = Su<IndiceBigotDto, string?>(r => r.EtaVigneto is not { } e ? null
                : e is < 0 or > 150 ? $"{Numero(e)} (dato anomalo)" : $"{Numero(e)} anni"),
        };
    }

    private static Tematismo Numerico(string id, string nome, string? unita, Func<IndiceBigotDto, decimal?> valoreBigot,
        decimal[] soglie, string[] colori, string? nota = null)
    {
        var valore = Su(valoreBigot);
        return new Tematismo
        {
            Id = id,
            Nome = nome,
            Unita = unita,
            Nota = nota,
            Classi = ClassiNumeriche(soglie, colori, unita).ToList(),
            Classifica = r => valore(r) is { } v ? ClasseNumerica(v, soglie) : null,
            ValoreConfronto = valore,
            TestoValore = r => valore(r) is { } v ? Numero(v) + (unita is null ? "" : $" {unita}") : null,
        };
    }

    /// <summary>
    /// Adatta una funzione su righe di tipo <typeparamref name="T"/> a righe qualsiasi: le righe di
    /// un'altra fonte danno il valore predefinito (null).
    /// </summary>
    public static Func<IRigaVigneto, TValore?> Su<T, TValore>(Func<T, TValore?> funzione) where T : IRigaVigneto =>
        r => r is T riga ? funzione(riga) : default;

    private static Func<IRigaVigneto, decimal?> Su(Func<IndiceBigotDto, decimal?> funzione) => Su<IndiceBigotDto, decimal?>(funzione);

    private static Tematismo Categorico(string id, string nome, Func<IndiceBigotDto, string?> valoreBigot,
        string[] colori, string[] categorieOrdinate, string[] soglie)
    {
        var valore = Su<IndiceBigotDto, string?>(valoreBigot);
        if (soglie.Length != categorieOrdinate.Length)
        {
            throw new ArgumentException("Serve una soglia per ogni categoria.");
        }

        // Un valore che 4Grapes scrive e la mappa non conosce (es. una classe nuova nella vista)
        // non deve sembrare "Nessun dato": va in "Altro", con un colore suo, e si segnala nel log.
        var altro = categorieOrdinate.Length;
        int? Indice(string v) =>
            Array.FindIndex(categorieOrdinate, c => string.Equals(c, v, StringComparison.OrdinalIgnoreCase)) is var i and >= 0 ? i : null;

        return new Tematismo
        {
            Id = id,
            Nome = nome,
            Nota = "Classi calcolate da 4Grapes sul valore del parametro: a fianco l'intervallo di ciascuna.",
            Classi = categorieOrdinate.Select((c, i) => new ClasseTematismo(Titolo(c), colori[i], Soglia: soglie[i]))
                .Append(new ClasseTematismo(EtichettaAltro, ColoreAltro, Speciale: true))
                .ToList(),
            Classifica = r => valore(r)?.Trim() is { Length: > 0 } v ? Indice(v) ?? altro : null,
            ValoreNonPrevisto = r => valore(r)?.Trim() is { Length: > 0 } v && Indice(v) is null ? v : null,
            // Confronto sulla posizione nella scala: un passo verso una delle due estremità.
            ValoreConfronto = r => valore(r)?.Trim() is { Length: > 0 } v ? Indice(v) : null,
            TestoValore = r => valore(r)?.Trim() is { Length: > 0 } v ? (Indice(v) is { } i ? Titolo(categorieOrdinate[i]) : v) : null,
        };
    }

    /// <summary>Soglie crescenti s1..sk → k+1 classi: v &lt; s1, s1 ≤ v &lt; s2, …, v ≥ sk.</summary>
    public static int ClasseNumerica(decimal valore, IReadOnlyList<decimal> soglie)
    {
        var classe = 0;
        while (classe < soglie.Count && valore >= soglie[classe])
        {
            classe++;
        }

        return classe;
    }

    public static IEnumerable<ClasseTematismo> ClassiNumeriche(IReadOnlyList<decimal> soglie, IReadOnlyList<string> colori, string? unita)
    {
        if (colori.Count != soglie.Count + 1)
        {
            throw new ArgumentException("Servono tanti colori quante sono le soglie + 1.");
        }

        string N(decimal v) => v.ToString("0.##", Italiano);
        var suffisso = unita is null ? "" : $" {unita}";

        yield return new ClasseTematismo($"< {N(soglie[0])}{suffisso}", colori[0]);
        for (var i = 1; i < soglie.Count; i++)
        {
            yield return new ClasseTematismo($"{N(soglie[i - 1])} – {N(soglie[i])}{suffisso}", colori[i]);
        }

        yield return new ClasseTematismo($"≥ {N(soglie[^1])}{suffisso}", colori[^1]);
    }

    public static string Numero(decimal valore) => valore.ToString("0.##", Italiano);

    private static string Titolo(string categoria) =>
        categoria.Length == 0 ? categoria : char.ToUpper(categoria[0], Italiano) + categoria[1..].ToLower(Italiano);
}
