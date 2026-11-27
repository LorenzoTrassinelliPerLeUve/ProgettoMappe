using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.BilancioIdrico;

/// <summary>
/// Un vigneto nella pagina della configurazione (task #384): cosa ha e cosa gli manca, dall'ultimo calcolo del bilancio e dalla
/// configurazione. <see cref="NelCalcolo"/> false = vigneto non attivo in 4Grapes (estirpato, non a carico, vigna nascosta):
/// non entra nel bilancio, quindi non gli manca niente.
/// </summary>
public sealed record VignetoConfigurazione(
    int Id,
    string Nome,
    bool NelCalcolo,
    int? StileConfigurato,
    bool StileDalTarget,
    bool SenzaStile,
    bool SenzaAnalisiSuolo,
    bool SenzaRadici,
    bool SenzaSettore,
    double? AwcMm);

/// <summary>
/// Le sezioni della pagina della configurazione e i vigneti con quello che manca (task #384): la pagina mostra una sezione per
/// volta (<c>?sezione=</c>), «solo da completare» (<c>?solo=mancanti</c>) e un vigneto solo (<c>?vigneto=</c>). Pura, testata.
/// </summary>
public sealed class VignetiConfigurazione
{
    public const string Stile = "stile", Suolo = "suolo", Irrigazione = "irrigazione", Fatte = "fatte", Esperti = "esperti";

    /// <summary>Le sezioni nell'ordine delle linguette, con il nome per chi le legge.</summary>
    public static readonly IReadOnlyList<(string Chiave, string Nome)> Sezioni =
    [
        (Stile, "Stile del vino"), (Suolo, "Suolo e radici"), (Irrigazione, "Settori e impianti"), (Fatte, "Irrigazioni fatte"),
        (Esperti, "Per esperti"),
    ];

    public IReadOnlyList<VignetoConfigurazione> Vigneti { get; }

    private VignetiConfigurazione(IReadOnlyList<VignetoConfigurazione> vigneti) => Vigneti = vigneti;

    public int SenzaStile => Vigneti.Count(v => v.SenzaStile);
    public int SenzaAnalisiSuolo => Vigneti.Count(v => v.SenzaAnalisiSuolo);
    public int SenzaSettore => Vigneti.Count(v => v.SenzaSettore);
    public int FuoriCalcolo => Vigneti.Count(v => !v.NelCalcolo);

    /// <summary>
    /// Dai vigneti dell'azienda, dalla configurazione e dall'ultimo calcolo (<paramref name="stato"/>; null = mai calcolata: tutti
    /// i vigneti contano come nel calcolo e lo stile manca a chi non l'ha nella configurazione). Una calibrazione con l'AWC copre
    /// l'analisi del suolo mancante, una con la profondità delle radici il rilievo delle radici, anche prima del ricalcolo.
    /// </summary>
    public static VignetiConfigurazione Da(IEnumerable<VignetoDto> vigneti, ConfigurazioneWeb conf, BilancioAziendaDto? stato, DateOnly oggi)
    {
        var calcolati = stato?.Vigneti.ToDictionary(v => v.IdVigneto);
        HashSet<int> Anomalia(string codice) =>
            stato?.Anomalie.Where(a => a.Codice == codice).SelectMany(a => a.Vigneti).ToHashSet() ?? [];
        var senzaSuolo = Anomalia("suolo");
        var senzaRadici = Anomalia("radici");
        var lista = vigneti.Select(v =>
        {
            var nelCalcolo = calcolati is null || calcolati.ContainsKey(v.Id);
            var calcolato = calcolati?.GetValueOrDefault(v.Id);
            var stile = conf.StileDi(v.Id, oggi)?.Dati.IdStileVino;
            var calibrazione = conf.CalibrazioneDi(v.Id)?.Dati;
            // Lo stile appena salvato vale anche prima del ricalcolo; senza stile nella configurazione decide l'ultimo calcolo
            // (che lo prende dal target di concimazione, se c'è).
            var haStile = stile is not null || calcolato is { HaStile: true };
            return new VignetoConfigurazione(
                v.Id, v.Nome, nelCalcolo, stile,
                StileDalTarget: stile is null && calcolato is { HaStile: true },
                SenzaStile: nelCalcolo && !haStile,
                SenzaAnalisiSuolo: nelCalcolo && senzaSuolo.Contains(v.Id) && calibrazione?.AwcMm is null,
                SenzaRadici: nelCalcolo && senzaRadici.Contains(v.Id) && calibrazione?.ProfonditaRadiciM is null,
                SenzaSettore: nelCalcolo && conf.SommaQuote(v.Id) <= 0,
                calcolato?.AwcMm);
        });
        // Prima quelli nel calcolo, per nome; i non attivi in fondo.
        return new(lista.OrderBy(v => !v.NelCalcolo).ThenBy(v => v.Nome, StringComparer.CurrentCultureIgnoreCase).ToList());
    }

    /// <summary>I vigneti da mostrare nella sezione: tutti, solo quelli a cui manca il dato della sezione, o uno solo.</summary>
    public IReadOnlyList<VignetoConfigurazione> DaMostrare(string sezione, bool soloMancanti, int? vigneto) =>
        vigneto is { } id ? Vigneti.Where(v => v.Id == id).ToList()
        : soloMancanti ? Vigneti.Where(v => Manca(v, sezione)).ToList()
        : Vigneti;

    /// <summary>Al vigneto manca il dato che si compila nella sezione?</summary>
    public static bool Manca(VignetoConfigurazione v, string sezione) => sezione switch
    {
        Stile => v.SenzaStile,
        Suolo => v.SenzaAnalisiSuolo,
        Irrigazione => v.SenzaSettore,
        _ => false,
    };

    /// <summary>Quanti vigneti da completare nella sezione (0 per quelle senza vigneti).</summary>
    public int Mancanti(string sezione) => Vigneti.Count(v => Manca(v, sezione));

    /// <summary>
    /// La sezione da aprire: quella chiesta se esiste, altrimenti la prima fra stile e suolo con vigneti da completare, altrimenti
    /// lo stile.
    /// </summary>
    public string SezioneDaAprire(string? chiesta) =>
        Sezioni.Any(s => s.Chiave == chiesta) ? chiesta!
        : SenzaStile > 0 ? Stile
        : SenzaAnalisiSuolo > 0 ? Suolo
        : Stile;

    /// <summary>
    /// Dove si sistema un dato mancante della pagina del bilancio (codice dell'anomalia dell'Api), per il collegamento diretto;
    /// null se non si sistema nella configurazione di Mappe.
    /// </summary>
    public static string? SezioneDi(string codiceAnomalia) => codiceAnomalia switch
    {
        "stile" => Stile,
        "suolo" or "radici" => Suolo,
        "senza-settore" or "quote" or "non-attivi" => Irrigazione,
        _ => null,
    };

    /// <summary>L'indirizzo della configurazione aperta su una sezione, su un vigneto o solo sui vigneti da completare.</summary>
    public static string Indirizzo(int azienda, string? sezione = null, bool soloMancanti = false, int? vigneto = null) =>
        $"bilancio-idrico/configurazione?azienda={azienda}"
        + (sezione is null ? "" : $"&sezione={sezione}")
        + (soloMancanti ? "&solo=mancanti" : "")
        + (vigneto is { } v ? $"&vigneto={v}" : "")
        // Task #405: la configurazione è una pagina sola; irrigazione e irrigazioni fatte sono più in basso.
        + (sezione switch { Irrigazione => "#irrigazione", Fatte => "#irrigato", _ => "" });

    /// <summary>
    /// Task #405: la pagina di prima, divisa per tipo di dato (parametri del modello, correzioni fini del suolo, settori con
    /// quote parziali e turni), resta per gli interni Perleuve.
    /// </summary>
    public static string IndirizzoEsperti(int azienda, string? sezione = null, bool soloMancanti = false, int? vigneto = null) =>
        $"bilancio-idrico/configurazione/esperti?azienda={azienda}"
        + (sezione is null ? "" : $"&sezione={sezione}")
        + (soloMancanti ? "&solo=mancanti" : "")
        + (vigneto is { } v ? $"&vigneto={v}" : "");
}
