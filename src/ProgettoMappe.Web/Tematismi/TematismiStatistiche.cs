using System.Text.RegularExpressions;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Tematismi;

/// <summary>
/// Soglie e colori di una scala (sezione <c>Tematismi</c> di appsettings.json): soglie crescenti
/// s1..sk → k+1 classi (v &lt; s1, s1 ≤ v &lt; s2, …, v ≥ sk), un colore #rrggbb per classe e, se
/// si vuole, un nome per classe (scritto prima dell'intervallo in legenda).
/// </summary>
public sealed class ScalaTematismoOptions
{
    public decimal[] Soglie { get; set; } = [];

    public string[] Colori { get; set; } = [];

    public string[] Etichette { get; set; } = [];
}

/// <summary>
/// Scale dei tematismi sulle statistiche 4Grapes. In 4Grapes non ci sono né soglie né colori
/// (la tabella ScaleColori non esiste): li decide l'app, e stanno in configurazione per poterli
/// cambiare senza toccare il codice. Una scala assente o non valida usa quella predefinita.
/// </summary>
public sealed class TematismiOptions
{
    public const string SectionName = "Tematismi";

    /// <summary>Oidio e peronospora (scala 1–9, più alto = più rischio).</summary>
    public ScalaTematismoOptions? Rischio { get; set; }

    /// <summary>Punteggio di vigoria (1–10, equilibrio al centro).</summary>
    public ScalaTematismoOptions? Vigoria { get; set; }

    /// <summary>Potenziale idrico in mm.</summary>
    public ScalaTematismoOptions? PotenzialeIdrico { get; set; }

    /// <summary>Colori delle fasi BBCH (vedi <see cref="TematismiFenologia"/>).</summary>
    public FenologiaOptions? Fenologia { get; set; }

    /// <summary>Confronto fra due anni, per Id del tematismo: verso buono e soglie (vedi <see cref="RegolaConfronto"/>).</summary>
    public Dictionary<string, RegolaConfrontoOptions> Confronto { get; set; } = [];
}

/// <summary>
/// Tutti i tematismi del menu «Colora per»: BIGOT (fissi), statistiche e fenologia (scale da configurazione), con
/// le regole del confronto fra due anni.
/// </summary>
public sealed class CatalogoTematismi
{
    private readonly IReadOnlyDictionary<string, RegolaConfronto> _regole;

    public CatalogoTematismi(IReadOnlyList<Tematismo> tutti, IReadOnlyDictionary<string, RegolaConfronto>? regole = null)
    {
        Tutti = tutti;
        _regole = regole ?? ConfrontoBigot.Predefinite;
    }

    public IReadOnlyList<Tematismo> Tutti { get; }

    public Tematismo? Trova(string? id) => Tutti.FirstOrDefault(t => t.Id == id);

    /// <summary>I tematismi sulle statistiche 4Grapes (né BIGOT né fenologia), nell'ordine del menu.</summary>
    public IEnumerable<Tematismo> Statistiche => Tutti.Where(t => t.Fonte is not (FonteTematismo.Bigot or FonteTematismo.Fenologia));

    public RegolaConfronto Regola(Tematismo tema) => _regole.GetValueOrDefault(tema.Id, ConfrontoBigot.Generica);

    public static CatalogoTematismi Crea(TematismiOptions opzioni, ILogger logger) =>
        new([.. TematismiBigot.Tutti, .. TematismiStatistiche.Crea(opzioni, logger), TematismiFenologia.Crea(opzioni.Fenologia, logger)],
            ConfrontoBigot.Regole(opzioni.Confronto, logger));
}

/// <summary>
/// Tematismi sulle viste statistiche di 4Grapes (dba, 2026-10-13): rischio oidio e peronospora
/// (1–9, più alto = più rischio; in 4Grapes «alto» è sopra 5), sempre separati: niente rischio
/// «combinato» (decisione del proprietario, 2026-10-13); punteggio di vigoria (1–10, equilibrio al
/// centro: scala divergente) e potenziale idrico (mm, senza anno).
/// </summary>
public static partial class TematismiStatistiche
{
    public static readonly ScalaTematismoOptions RischioPredefinito = new()
    {
        Soglie = [4m, 6m, 8m],
        Colori = ["#1a9850", "#fee08b", "#fc8d59", "#d73027"],
        Etichette = ["Basso", "Medio", "Alto", "Molto alto"],
    };

    /// <summary>Le classi di vigoria di 4Grapes (VistaIndiceBIGOT): ≤ 1, &lt; 2,5, … ≥ 10; sui valori interi ≤ 1 equivale a &lt; 2.</summary>
    public static readonly ScalaTematismoOptions VigoriaPredefinita = new()
    {
        Soglie = [2m, 2.5m, 4m, 5.5m, 7m, 8.5m, 10m],
        Colori = ["#b35806", "#e08214", "#fdb863", "#fee0b6", "#d8daeb", "#b2abd2", "#8073ac", "#542788"],
        Etichette = ["Molto bassa", "Bassa", "Medio bassa", "Media", "Medio alta", "Alta", "Molto alta", "Eccessiva"],
    };

    public static readonly ScalaTematismoOptions PotenzialeIdricoPredefinito = new()
    {
        Soglie = [50m, 100m, 150m, 200m],
        Colori = ["#c6dbef", "#9ecae1", "#6baed6", "#3182bd", "#08519c"],
    };

    public const string NotaRischio = "Scala di 4Grapes da 1 a 9: più alto = più rischio. In 4Grapes «alto» vuol dire sopra 5.";

    public const string FuoriRischi = "Non valutato nel {anno}";

    public static IReadOnlyList<Tematismo> Crea(TematismiOptions opzioni, ILogger logger)
    {
        var rischio = Valida(opzioni.Rischio, RischioPredefinito, "Rischio", logger);
        var vigoria = Valida(opzioni.Vigoria, VigoriaPredefinita, "Vigoria", logger);
        var potenziale = Valida(opzioni.PotenzialeIdrico, PotenzialeIdricoPredefinito, "PotenzialeIdrico", logger);

        return
        [
            Scala<RischioVignetoDto>("rischio-oidio", "Rischio oidio", FonteTematismo.Rischi, r => r.Oidio, rischio, null,
                NotaRischio, FuoriRischi),
            Scala<RischioVignetoDto>("rischio-peronospora", "Rischio peronospora", FonteTematismo.Rischi, r => r.Peronospora, rischio, null,
                NotaRischio, FuoriRischi),
            Scala<VigoriaVignetoDto>("vigoria-punteggio", "Vigoria (punteggio)", FonteTematismo.Vigoria, r => r.Punteggio, vigoria, null,
                "Scala da 1 a 10: l'equilibrio è al centro (circa 5), entrambi gli estremi sono un problema. Classi come in 4Grapes.",
                "Nessun rilievo di vigoria nel {anno}"),
            Scala<PotenzialeIdricoVignetoDto>("potenziale-idrico", "Potenziale idrico", FonteTematismo.PotenzialeIdrico,
                r => r.PotenzialeIdrico, potenziale, "mm",
                "Acqua disponibile nel suolo, in mm: più alto = più riserva. Un valore per vigneto, senza anno "
                + "(dai rilievi del suolo; se il vigneto non li ha, la media della sua vigna).",
                "Nessun rilievo del suolo"),
        ];
    }

    /// <summary>Errori di una scala (vuoto = valida): soglie crescenti, un colore #rrggbb per classe, nomi facoltativi.</summary>
    public static IReadOnlyList<string> Errori(ScalaTematismoOptions scala)
    {
        var errori = new List<string>();
        if (scala.Soglie.Length == 0)
        {
            errori.Add("nessuna soglia");
        }
        for (var i = 1; i < scala.Soglie.Length; i++)
        {
            if (scala.Soglie[i] <= scala.Soglie[i - 1])
            {
                errori.Add("le soglie devono essere crescenti");
                break;
            }
        }
        if (scala.Colori.Length != scala.Soglie.Length + 1)
        {
            errori.Add($"servono {scala.Soglie.Length + 1} colori (uno in più delle soglie), ce ne sono {scala.Colori.Length}");
        }
        if (scala.Colori.Any(c => !Colore().IsMatch(c ?? "")))
        {
            errori.Add("i colori devono essere nella forma #rrggbb");
        }
        if (scala.Etichette.Length > 0 && scala.Etichette.Length != scala.Colori.Length)
        {
            errori.Add("i nomi delle classi, se ci sono, devono essere tanti quanti i colori");
        }

        return errori;
    }

    private static ScalaTematismoOptions Valida(ScalaTematismoOptions? scala, ScalaTematismoOptions predefinita, string nome, ILogger logger)
    {
        if (scala is null)
        {
            return predefinita;
        }

        var errori = Errori(scala);
        if (errori.Count == 0)
        {
            return scala;
        }

        logger.LogWarning("Tematismi:{Nome} non valido ({Errori}): uso la scala predefinita", nome, string.Join("; ", errori));
        return predefinita;
    }

    private static Tematismo Scala<T>(string id, string nome, FonteTematismo fonte, Func<T, decimal?> valoreRiga,
        ScalaTematismoOptions scala, string? unita, string nota, string fuori) where T : IRigaVigneto
    {
        var valore = TematismiBigot.Su<T, decimal?>(valoreRiga);
        var soglie = scala.Soglie;
        var classi = TematismiBigot.ClassiNumeriche(soglie, scala.Colori, unita)
            .Select((c, i) => scala.Etichette.Length == 0 ? c : new ClasseTematismo(scala.Etichette[i], c.Colore, Soglia: c.Etichetta))
            .ToList();

        return new Tematismo
        {
            Id = id,
            Nome = nome,
            Fonte = fonte,
            Unita = unita,
            Nota = nota,
            Classi = classi,
            Classifica = r => valore(r) is { } v ? TematismiBigot.ClasseNumerica(v, soglie) : null,
            ValoreConfronto = valore,
            TestoValore = r => valore(r) is { } v ? TematismiBigot.Numero(v) + (unita is null ? "" : $" {unita}") : null,
            EtichettaFuoriPerimetro = fuori,
        };
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Colore();
}
