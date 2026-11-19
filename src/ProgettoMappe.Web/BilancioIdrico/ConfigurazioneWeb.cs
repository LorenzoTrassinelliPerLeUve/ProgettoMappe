using System.Globalization;
using System.Text.Json;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>Una riga della corrispondenza target di concimazione → stile del vino (null = da decidere con Matteo).</summary>
public sealed record StileTargetWeb(int IdTarget, string Descrizione, int? IdStileVino);

/// <summary>Una voce letta, con chi l'ha cambiata per ultimo e quando.</summary>
public sealed record VoceLetta<T>(T Dati, string Chiave, int Versione, string Autore, DateTime CreatoIlUtc);

/// <summary>
/// La configurazione del bilancio idrico di un'azienda come la mostra la pagina (task #335, punto D), letta dalle voci
/// dell'Api (ultima versione di ogni voce, con autore e data). Pura, testata.
/// </summary>
public sealed class ConfigurazioneWeb
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Gli stili del vino (BI_StileVino del vecchio DSS: soglie di Pdlwp in Parametri.Soglie).</summary>
    public static readonly IReadOnlyDictionary<int, string> Stili = new Dictionary<int, string>
    {
        [1] = "1 · Vegetale, bassa concentrazione, alta acidità o vigneti giovani",
        [2] = "2 · Fruttato, bassa concentrazione, equilibrato",
        [3] = "3 · Fruttato, media concentrazione, equilibrato",
        [4] = "4 · Fruttato, concentrazione da media a forte, equilibrato",
        [5] = "5 · Equilibrato/disequilibrato, forte concentrazione, alcolico",
        [6] = "6 · Astringente, disequilibrato, troppo alcolico",
    };

    /// <summary>Le tipologie d'impianto (BI_TipologiaImpianto) con l'efficienza predefinita.</summary>
    public static readonly IReadOnlyDictionary<int, string> Tipologie = new Dictionary<int, string>
    {
        [0] = "Nessun impianto (asciutta)",
        [1] = "Goccia fuori suolo (0,90)",
        [2] = "Goccia interrata SDI (0,93)",
        [3] = "Microsprinkler sottochioma (0,88)",
        [4] = "Microsprinkler soprachioma (0,80)",
        [5] = "Sprinkler soprachioma (0,70)",
    };

    public List<VoceLetta<SettoreBilancioDto>> Settori { get; } = [];
    public List<VoceLetta<QuotaBilancioDto>> Quote { get; } = [];
    public List<VoceLetta<ImpiantoBilancioDto>> Impianti { get; } = [];
    public List<VoceLetta<ShiftBilancioDto>> Shift { get; } = [];
    public List<VoceLetta<SettoreShiftBilancioDto>> SettoriShift { get; } = [];
    public List<VoceLetta<StileBilancioDto>> StiliVigneti { get; } = [];
    public List<VoceLetta<IrrigazioneBilancioDto>> Irrigazioni { get; } = [];
    public List<VoceLetta<CalibrazioneBilancioDto>> Calibrazioni { get; } = [];

    /// <summary>
    /// La voce «modello» (comune a tutte le aziende) com'è, in JSON: la pagina cambia solo la corrispondenza target → stile
    /// (task #349) e rimanda il resto invariato. Null = mai salvata.
    /// </summary>
    public VoceLetta<System.Text.Json.Nodes.JsonObject>? Modello { get; private set; }

    /// <summary>
    /// I target di concimazione di 4Grapes (dba, 19/11/2026), quando il modello non ha ancora la corrispondenza: stessi valori
    /// di <c>ModelloBilancio.TargetDiPartenza</c> dell'Api.
    /// </summary>
    public static readonly IReadOnlyList<StileTargetWeb> TargetDiPartenza =
    [
        new(1, "Metodo Charmat da uve bianche", null), new(2, "Metodo Champenoise da uve bianche", null), new(3, "Vino bianco pronta beva", null),
        new(4, "Vino bianco strutturato", null), new(5, "Vino rosso pronta beva", null), new(6, "Vino rosso strutturato", null),
        new(7, "Metodo Charmat da uve rosse", null), new(8, "Metodo Champenoise da uve rosse", null), new(9, "Vino rosé", null),
        new(10, "Non definito", null),
    ];

    /// <summary>La corrispondenza target → stile del modello, completata con i target che mancano.</summary>
    public List<StileTargetWeb> StiliDaTarget()
    {
        var salvati = Modello?.Dati["stiliDaTarget"] is System.Text.Json.Nodes.JsonArray a
            ? a.Deserialize<List<StileTargetWeb>>(Json) ?? []
            : [];
        return salvati.Concat(TargetDiPartenza.Where(t => salvati.All(s => s.IdTarget != t.IdTarget))).OrderBy(t => t.IdTarget).ToList();
    }

    /// <summary>Il modello da salvare: quello di adesso (o vuoto) con la corrispondenza nuova.</summary>
    public System.Text.Json.Nodes.JsonObject ModelloConStili(IEnumerable<StileTargetWeb> stili)
    {
        var modello = Modello?.Dati.DeepClone().AsObject() ?? [];
        modello["stiliDaTarget"] = JsonSerializer.SerializeToNode(stili.ToList(), Json);
        return modello;
    }

    /// <summary>La calibrazione del vigneto, se c'è.</summary>
    public VoceLetta<CalibrazioneBilancioDto>? CalibrazioneDi(int idVigneto) => Calibrazioni.FirstOrDefault(c => c.Dati.IdVigneto == idVigneto);

    /// <summary>Dalle voci dell'Api: solo quelle valide (non tolte) dell'azienda e il modello comune.</summary>
    public static ConfigurazioneWeb Da(IEnumerable<VoceBilancioDto> voci)
    {
        var c = new ConfigurazioneWeb();
        foreach (var v in voci)
        {
            if (v.Dati is not { } dati)
                continue;
            VoceLetta<T> L<T>() => new(dati.Deserialize<T>(Json)!, v.Chiave, v.Versione, v.Autore, v.CreatoIlUtc);
            switch (v.Tipo)
            {
                case "settore": c.Settori.Add(L<SettoreBilancioDto>()); break;
                case "quota": c.Quote.Add(L<QuotaBilancioDto>()); break;
                case "impianto": c.Impianti.Add(L<ImpiantoBilancioDto>()); break;
                case "shift": c.Shift.Add(L<ShiftBilancioDto>()); break;
                case "settore-shift": c.SettoriShift.Add(L<SettoreShiftBilancioDto>()); break;
                case "stile": c.StiliVigneti.Add(L<StileBilancioDto>()); break;
                case "irrigazione": c.Irrigazioni.Add(L<IrrigazioneBilancioDto>()); break;
                case "calibrazione": c.Calibrazioni.Add(L<CalibrazioneBilancioDto>()); break;
                case "modello": c.Modello = L<System.Text.Json.Nodes.JsonObject>(); break;
            }
        }
        c.Settori.Sort((a, b) => a.Dati.IdSettore.CompareTo(b.Dati.IdSettore));
        c.Impianti.Sort((a, b) => (a.Dati.IdSettore, a.Dati.ValidoDal).CompareTo((b.Dati.IdSettore, b.Dati.ValidoDal)));
        c.Irrigazioni.Sort((a, b) => b.Dati.Data.CompareTo(a.Dati.Data));
        return c;
    }

    public string NomeSettore(int id) => Settori.FirstOrDefault(s => s.Dati.IdSettore == id)?.Dati.Nome ?? $"Settore {id}";

    public string NomeShift(int id) => Shift.FirstOrDefault(s => s.Dati.IdShift == id)?.Dati.Nome ?? $"Shift {id}";

    /// <summary>Lo stile valido oggi del vigneto (quello con l'inizio più recente, non scaduto).</summary>
    public VoceLetta<StileBilancioDto>? StileDi(int idVigneto, DateOnly oggi) =>
        StiliVigneti.Where(s => s.Dati.IdVigneto == idVigneto && s.Dati.ValidoDal <= oggi && (s.Dati.ValidoAl is null || s.Dati.ValidoAl >= oggi))
            .MaxBy(s => s.Dati.ValidoDal);

    /// <summary>Somma delle quote del vigneto (deve restare ≤ 100).</summary>
    public double SommaQuote(int idVigneto) => Quote.Where(q => q.Dati.IdVigneto == idVigneto).Sum(q => q.Dati.Percentuale);

    /// <summary>
    /// Tasso dell'impianto in mm/h: portata del gocciolatore (L/h) sull'area che bagna (distanza fra i gocciolatori × fra le
    /// ali, m²): 1 L/m² = 1 mm. Null se manca un numero.
    /// </summary>
    public static double? TassoDaGocciolatori(double? portataLh, double? distanzaGocciolatoriM, double? distanzaAliM) =>
        portataLh is > 0 && distanzaGocciolatoriM is > 0 && distanzaAliM is > 0
            ? Math.Round(portataLh.Value / (distanzaGocciolatoriM.Value * distanzaAliM.Value), 3)
            : null;

    /// <summary>«v3 · mrossi · 18/11/2026 17:40» in ora italiana.</summary>
    public static string Modifica<T>(VoceLetta<T> v)
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        var quando = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(v.CreatoIlUtc, DateTimeKind.Utc), fuso);
        return $"v{v.Versione} · {v.Autore} · {quando.ToString("d/M/yyyy HH:mm", CultureInfo.GetCultureInfo("it-IT"))}";
    }

    /// <summary>Numero scritto all'italiana (virgola o punto); null se vuoto o non valido.</summary>
    public static double? Numero(string? testo) =>
        double.TryParse(testo?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
