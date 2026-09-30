using System.Globalization;
using ProgettoMappe.Web.Services;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>Una classe della legenda. <see cref="Speciale"/>: classe fuori scala (es. "Incompleto", "Dato anomalo").</summary>
public sealed record ClasseTematismo(string Etichetta, string Colore, bool Speciale = false);

/// <summary>
/// Tematismo su un parametro BIGOT: come assegnare una classe (indice in <see cref="Classi"/>) a
/// ogni riga. Null = nessun dato per quel vigneto/anno (mostrato come "n.d.").
/// </summary>
public sealed class Tematismo
{
    public required string Id { get; init; }
    public required string Nome { get; init; }
    public string? Unita { get; init; }
    public string? Nota { get; init; }
    public required IReadOnlyList<ClasseTematismo> Classi { get; init; }
    public required Func<IndiceBigotDto, int?> Classifica { get; init; }
}

/// <summary>
/// Definizione dei tematismi sulla vista 4Grapes VistaIndiceBIGOT (prima versione).
/// Regole concordate con l'utente:
/// <list type="bullet">
/// <item>Punteggio e Ampelopatie (scala 0–100): 100 è positivo → scala rosso → verde.</item>
/// <item>Punteggio solo per vigneti completi (9 parametri su 9): con completezza diversa i
/// punteggi non sono confrontabili; gli altri cadono nella classe "Incompleto".</item>
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

    public static readonly IReadOnlyList<Tematismo> Tutti =
    [
        Punteggio(),
        Categorico("vigoria", "Vigoria", r => r.Vigoria, Neutra8,
            ["MOLTO BASSA", "BASSA", "MEDIO BASSA", "MEDIA", "MEDIO ALTA", "ALTA", "MOLTO ALTA", "ECCESSIVA"]),
        Numerico("sfe", "SFE", null, r => r.Sfe, [1.5m, 2.0m, 2.5m, 3.0m], Neutra5),
        Numerico("produzione", "Produzione", "kg/ceppo", r => r.Produzione, [1.0m, 1.5m, 2.0m, 3.0m], Neutra5),
        Numerico("sfekg", "SFE/kg", null, r => r.SfeKg, [0.8m, 1.2m, 1.6m, 2.2m], Neutra5),
        EtaVigneto(),
        Categorico("morfologia", "Morfologia", r => r.Morfologia, Neutra6,
            ["MOLTO SPARGOLO", "SPARGOLO", "MEDIO SPARGOLO", "MEDIO COMPATTO", "COMPATTO", "MOLTO COMPATTO"]),
        Numerico("ampelopatie", "Ampelopatie", null, r => r.Ampelopatie, [90m, 95m, 98m, 99.5m], Positiva5,
            "Scala 0–100: 100 è il valore positivo."),
        Categorico("regimeidrico", "Regime idrico", r => r.RegimeIdrico, Neutra5,
            ["ASSENTE", "LIEVE", "MEDIO", "SEVERO", "MOLTO SEVERO"]),
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

    /// <summary>IdVigneto → indice di classe, per le righe dell'anno indicato (vigneti senza dato esclusi).</summary>
    public static IReadOnlyDictionary<int, int> ClassiPerVigneto(Tematismo tematismo, IEnumerable<IndiceBigotDto> righe, int anno)
    {
        var risultato = new Dictionary<int, int>();
        foreach (var riga in righe.Where(r => r.Anno == anno))
        {
            if (tematismo.Classifica(riga) is { } classe)
            {
                risultato[riga.IdVigneto] = classe;
            }
        }

        return risultato;
    }

    private static Tematismo Punteggio()
    {
        var soglie = new[] { 40m, 55m, 70m, 85m };
        var classi = ClassiNumeriche(soglie, Positiva5, null).ToList();
        var incompleto = classi.Count;
        classi.Add(new ClasseTematismo($"Incompleto (< {ParametriTotali} parametri)", ColoreSpeciale, Speciale: true));

        return new Tematismo
        {
            Id = "punteggio",
            Nome = "Indice BIGOT (punteggio)",
            Nota = $"Solo vigneti con tutti i {ParametriTotali} parametri: con completezza diversa i punteggi non sono confrontabili. 100 è il valore positivo.",
            Classi = classi,
            Classifica = r => r.Punteggio is not { } p ? null
                : r.NumParametri == ParametriTotali ? ClasseNumerica(p, soglie) : incompleto,
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
            Classifica = r => r.EtaVigneto is not { } e ? null
                : e is < 0 or > 150 ? anomalo : ClasseNumerica(e, soglie),
        };
    }

    private static Tematismo Numerico(string id, string nome, string? unita, Func<IndiceBigotDto, decimal?> valore,
        decimal[] soglie, string[] colori, string? nota = null) => new()
    {
        Id = id,
        Nome = nome,
        Unita = unita,
        Nota = nota,
        Classi = ClassiNumeriche(soglie, colori, unita).ToList(),
        Classifica = r => valore(r) is { } v ? ClasseNumerica(v, soglie) : null,
    };

    private static Tematismo Categorico(string id, string nome, Func<IndiceBigotDto, string?> valore,
        string[] colori, string[] categorieOrdinate) => new()
    {
        Id = id,
        Nome = nome,
        Classi = categorieOrdinate.Select((c, i) => new ClasseTematismo(Titolo(c), colori[i])).ToList(),
        Classifica = r =>
        {
            var v = valore(r)?.Trim();
            if (string.IsNullOrEmpty(v))
            {
                return null;
            }

            var indice = Array.FindIndex(categorieOrdinate, c => string.Equals(c, v, StringComparison.OrdinalIgnoreCase));
            return indice >= 0 ? indice : null;
        },
    };

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

    private static IEnumerable<ClasseTematismo> ClassiNumeriche(decimal[] soglie, string[] colori, string? unita)
    {
        if (colori.Length != soglie.Length + 1)
        {
            throw new ArgumentException("Servono tanti colori quante sono le soglie + 1.");
        }

        string N(decimal v) => v.ToString("0.##", Italiano);
        var suffisso = unita is null ? "" : $" {unita}";

        yield return new ClasseTematismo($"< {N(soglie[0])}{suffisso}", colori[0]);
        for (var i = 1; i < soglie.Length; i++)
        {
            yield return new ClasseTematismo($"{N(soglie[i - 1])} – {N(soglie[i])}{suffisso}", colori[i]);
        }

        yield return new ClasseTematismo($"≥ {N(soglie[^1])}{suffisso}", colori[^1]);
    }

    private static string Titolo(string categoria) =>
        categoria.Length == 0 ? categoria : char.ToUpper(categoria[0], Italiano) + categoria[1..].ToLower(Italiano);
}
