using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportAnalisiSuolo;

/// <summary>Che cosa mostra un foglio dell'app Qlik.</summary>
public enum TipoFoglioSuolo
{
    /// <summary>Barre per Vigna × punto/vigneto, KPI, linee di riferimento.</summary>
    Parametro,
    /// <summary>Combo per una sola dimensione: valore e limiti min/max (fogli «(1)»).</summary>
    ConLimiti,
    Tessitura,
    TriangoloUsda,
    Idropedologici,
    ComposizioneCsc,
    Considerazioni,
}

/// <summary>Seconda dimensione dei grafici (la prima è la vigna): il codice del punto o il vigneto.</summary>
public enum DimensioneSuolo
{
    CodicePunto,
    Vigneto,
}

/// <summary>Limiti dei fogli «(1)»: dalle soglie della terna di tessitura (MinSO…) o costanti con la CSC (Bray Kurz).</summary>
public enum LimitiSuolo
{
    Nessuno,
    SostanzaOrganica,
    Azoto,
    Fosforo,
    BrayKurz,
}

/// <summary>
/// Un foglio/slide (<c>docs/specifiche/report-analisi-suolo/struttura.md</c>): titolo come nell'app, campo e fattore
/// (‰ = % × 10), seconda dimensione, linee di riferimento, scala dell'asse e formato dei numeri.
/// </summary>
public sealed record FoglioSuolo(string Titolo, TipoFoglioSuolo Tipo, string? Campo = null, double Fattore = 1,
    DimensioneSuolo Dimensione = DimensioneSuolo.CodicePunto, double[]? Riferimenti = null, double? Minimo = null, double? Massimo = null,
    string Formato = "0.0", LimitiSuolo Limiti = LimitiSuolo.Nessuno)
{
    public IReadOnlyList<double> Linee => Riferimenti ?? [];
}

/// <summary>Le selezioni del foglio «0 - FILITRI» (una per filtro, null = tutte).</summary>
public sealed record SelezioneSuolo(int? Anno = null, string? Vigna = null, string? Vitigno = null, string? SubVigneto = null,
    DateOnly? DataPrelievo = null, int? Profondita = null, string? Vigneto = null, string? CodicePunto = null);

/// <summary>Un rilievo con i campi calcolati e la sua terna di tessitura.</summary>
public sealed record RigaSuolo(AnalisiSuoloDto Analisi, IReadOnlyDictionary<string, double> Campi, TessituraSuoloDto? Tessitura)
{
    public double? this[string campo] => Campi.TryGetValue(campo, out var v) ? v : null;
}

/// <summary>Una barra: vigna, seconda dimensione, valore e (fogli «(1)») limiti.</summary>
public sealed record BarraSuolo(string Vigna, string Voce, double Valore, double? Minimo = null, double? Massimo = null)
{
    public string Etichetta => $"{Voce} · {Vigna}";
}

/// <summary>
/// I numeri del report analisi del suolo (task #317) come le misure dell'app Qlik: medie semplici (<c>Avg</c>) delle analisi
/// della selezione per Vigna × punto/vigneto; i campi della tessitura una volta per terna distinta nel gruppo.
/// Gli stessi numeri vanno nella pagina e nel PowerPoint.
/// </summary>
public static class CalcoloAnalisiSuolo
{
    /// <summary>Etichetta di Qlik per una dimensione vuota.</summary>
    public const string Vuoto = "-";

    private static FoglioSuolo P(string titolo, string campo, DimensioneSuolo d, double[]? rif, double? min, double? max,
        double fattore = 1, string formato = "0.0") => new(titolo, TipoFoglioSuolo.Parametro, campo, fattore, d, rif, min, max, formato);

    private const DimensioneSuolo Punto = DimensioneSuolo.CodicePunto, Vigneto = DimensioneSuolo.Vigneto;

    /// <summary>
    /// Le 52 slide nell'ordine dell'automazione PPT_REPORT_ANALISI_DEL_TERRENO, con la correzione decisa il 17/11: Rame EDTA
    /// una volta sola e Ferro EDTA al suo posto (in Qlik il Rame EDTA era doppio e il Ferro EDTA mancava).
    /// Le scale strette salvate ma non attive in Qlik (struttura.md, «(*)») qui sono automatiche.
    /// </summary>
    public static readonly IReadOnlyList<FoglioSuolo> Fogli =
    [
        new("Tessitura (USDA)", TipoFoglioSuolo.Tessitura, Minimo: 0, Massimo: 1000, Formato: "0"),
        new("Triangolo USDA", TipoFoglioSuolo.TriangoloUsda, Formato: "0"),
        new("Parametri Idropedologici", TipoFoglioSuolo.Idropedologici),
        P("pH in H₂O", CampiSuolo.PhH2o, Vigneto, [6.8, 7.2], 6, 9),
        P("pH in KCL", CampiSuolo.PhKcl, Punto, [6.8, 7.2], 6, 9),
        P("Carbonati Totali - CaCO₃ (‰)", CampiSuolo.Carbonati, Vigneto, null, null, null, 10),
        P("Calcare Attivo - CaCO₃ (‰)", CampiSuolo.Calcare, Punto, null, null, null, 10),
        P("Conducibilità (mS/cm)", CampiSuolo.Conducibilita, Punto, null, 0, 5, formato: "0.00"),
        P("Sostanza Organica (‰)", CampiSuolo.So, Punto, null, 0, 400, 10),
        new("Sostanza Organica (‰)", TipoFoglioSuolo.ConLimiti, CampiSuolo.So, 10, Punto, null, 0, 400, Limiti: LimitiSuolo.SostanzaOrganica),
        P("Sostanza Organica su Argilla (%)", CampiSuolo.SoArgilla, Vigneto, null, 0, 10),
        P("Azoto N-Totale (‰)", CampiSuolo.Azoto, Vigneto, null, 0, 4, formato: "0.00"),
        new("Azoto N-Totale (‰)", TipoFoglioSuolo.ConLimiti, CampiSuolo.Azoto, 1, Punto, null, 0, 4, "0.00", LimitiSuolo.Azoto),
        P("C/N", CampiSuolo.CN, Punto, [10], 0, 15),
        P("Coefficiente K2", CampiSuolo.K2, Punto, [1, 3], 0, 4, formato: "0.00"),
        P("CSC (T) cmol(₊)/kg", CampiSuolo.Csc, Punto, [5, 15, 25], 0, 30, formato: "0.00"),
        new("Composizione CSC (T) cmol(₊)/kg", TipoFoglioSuolo.ComposizioneCsc, Minimo: 0, Formato: "0.00"),
        P("Saturazione Basica (%)", CampiSuolo.Saturazione, Punto, [85], 0, 100),
        P("Ca Scambiabile [% CSC (T)]", CampiSuolo.CaCsc, Punto, [70, 85], 0, 100),
        P("Ca Scambiabile (ppm)", CampiSuolo.CaPpm, Punto, null, null, null),
        P("K Scambiabile [% CSC (T)]", CampiSuolo.KCsc, Punto, [2, 5], 0, 9),
        P("K Scambiabile (ppm)", CampiSuolo.KPpm, Punto, null, null, null),
        P("Mg Scambiabile [% CSC (T)]", CampiSuolo.MgCsc, Punto, [7, 10], null, null),
        P("Mg Scambiabile (ppm)", CampiSuolo.MgPpm, Punto, null, null, null),
        P("Na Scambiabile [% CSC (T)]", CampiSuolo.NaCsc, Punto, [2, 15], 0, 18),
        P("Na Scambiabile (ppm)", CampiSuolo.NaPpm, Punto, null, null, null),
        P("Acidità di Scambio % CSC", CampiSuolo.AciditaCsc, Vigneto, [2, 15], 0, 15),
        P("Rapporto Mg/K", CampiSuolo.MgK, Punto, [2, 3], 0, 7),
        P("Rapporto Ca/Mg", CampiSuolo.CaMg, Punto, [8.5, 10], 0, 15),
        P("Fosforo (P) assimilabile (Olsen) ppm", CampiSuolo.POlsen, Punto, null, 0, 35),
        new("Fosforo (P) assimilabile (Olsen) ppm", TipoFoglioSuolo.ConLimiti, CampiSuolo.POlsen, 1, Punto, null, 0, 35, Limiti: LimitiSuolo.Fosforo),
        P("Fosforo (P) assimilabile (Bray Kurz) ppm", CampiSuolo.PBray, Vigneto, null, 0, 35),
        new("Fosforo (P) assimilabile (Bray Kurz) ppm", TipoFoglioSuolo.ConLimiti, CampiSuolo.PBray, 1, Vigneto, null, 0, 35, Limiti: LimitiSuolo.BrayKurz),
        P("Rame (Cu) assimilabile (DTPA) ppm", CampiSuolo.CuDtpa, Punto, [0.2, 1], 0, 5),
        P("Ferro (Fe) assimilabile (DTPA) ppm", CampiSuolo.FeDtpa, Punto, [2.5, 4.5], 0, 10),
        P("Manganese (Mn) assimilabile (DTPA) ppm", CampiSuolo.MnDtpa, Punto, [1, 1.5], 0, 5),
        P("Zinco (Zn) assimilabile (DTPA) ppm", CampiSuolo.ZnDtpa, Punto, [0.5, 1], 0, 2),
        P("Rame (Cu) assimilabile (EDTA) ppm", CampiSuolo.CuEdta, Vigneto, [1.5, 6, 12], 0, 15),
        P("Ferro (Fe) assimilabile (EDTA) ppm", CampiSuolo.FeEdta, Vigneto, [45, 166, 350], null, null),
        P("Manganese (Mn) assimilabile (EDTA) ppm", CampiSuolo.MnEdta, Vigneto, [100, 260, 350], null, null),
        P("Zinco (Zn) assimilabile (EDTA) ppm", CampiSuolo.ZnEdta, Vigneto, [0.6, 3.8, 8], null, null),
        P("Boro (B) assimilabile (ppm)", CampiSuolo.Boro, Punto, [1, 20], null, null),
        P("Zolfo (S) assimilabile (ppm)", CampiSuolo.Zolfo, Vigneto, [13, 20], null, null),
        P("Molibdeno (Mo) assimilabile (ppm)", CampiSuolo.Molibdeno, Vigneto, [0.2], 0, 2.5),
        P("Calcio (Ca) idrosolubile (ppm)", CampiSuolo.CaIdro, Punto, [50, 100], null, null),
        P("Magnesio (Mg) idrosolubile (ppm)", CampiSuolo.MgIdro, Punto, [4, 7], 0, 10),
        P("Potassio (K) idrosolubile (ppm)", CampiSuolo.KIdro, Punto, [8, 15], 0, 20),
        P("Sodio (Na) idrosolubile (ppm)", CampiSuolo.NaIdro, Punto, [15, 20], 0, 22),
        P("Cloro (Cl) idrosolubile (ppm)", CampiSuolo.ClIdro, Punto, [30, 50], null, null),
        new("Considerazioni", TipoFoglioSuolo.Considerazioni),
        new("Considerazioni", TipoFoglioSuolo.Considerazioni),
        new("Considerazioni", TipoFoglioSuolo.Considerazioni),
    ];

    /// <summary>Le analisi dell'azienda con i campi calcolati (tutte, prima delle selezioni).</summary>
    public static IReadOnlyList<RigaSuolo> Righe(AnalisiSuoloAziendaDto dati)
    {
        var terne = dati.Tessiture.ToDictionary(t => t.IdTessitura);
        return dati.Analisi.Select(a =>
        {
            var t = a.IdTessitura is { } id && terne.TryGetValue(id, out var x) ? x : null;
            return new RigaSuolo(a, CampiSuolo.Calcola(a, t), t);
        }).ToList();
    }

    public static IReadOnlyList<RigaSuolo> Filtra(IEnumerable<RigaSuolo> righe, SelezioneSuolo s) =>
        righe.Where(r =>
        {
            var a = r.Analisi;
            return (s.Anno is null || a.Anno == s.Anno) && (s.Vigna is null || a.Vigna == s.Vigna)
                   && (s.Vitigno is null || a.Vitigno == s.Vitigno) && (s.SubVigneto is null || a.SubVigneto == s.SubVigneto)
                   && (s.DataPrelievo is null || a.DataPrelievo == s.DataPrelievo) && (s.Profondita is null || a.ProfonditaCm == s.Profondita)
                   && (s.Vigneto is null || a.NomeVigneto == s.Vigneto) && (s.CodicePunto is null || a.CodicePunto == s.CodicePunto);
        }).ToList();

    /// <summary>Media dei valori presenti; null senza valori (come <c>Avg</c> di Qlik).</summary>
    public static double? Media(IEnumerable<double?> valori)
    {
        var presenti = valori.OfType<double>().ToList();
        return presenti.Count == 0 ? null : presenti.Average();
    }

    /// <summary>Il KPI: media del campo su tutta la selezione (× il fattore del foglio).</summary>
    public static double? Kpi(IReadOnlyList<RigaSuolo> righe, string campo, double fattore = 1) =>
        Media(righe.Select(r => r[campo])) * fattore;

    /// <summary>Media di un campo della tessitura, una volta per terna distinta (in Qlik sta nella tabella Tessitura).</summary>
    public static double? MediaTessitura(IEnumerable<RigaSuolo> righe, Func<TessituraSuoloDto, double?> campo) =>
        Media(righe.Select(r => r.Tessitura).OfType<TessituraSuoloDto>().DistinctBy(t => t.IdTessitura).Select(campo));

    public static string Voce(AnalisiSuoloDto a, DimensioneSuolo d) =>
        (d == DimensioneSuolo.Vigneto ? a.NomeVigneto : a.CodicePunto) ?? Vuoto;

    /// <summary>
    /// Le barre di un foglio parametro: per Vigna × punto/vigneto (o solo punto/vigneto nei fogli con limiti), ordinate come Qlik
    /// (numerico, poi alfabetico); senza valore la barra non c'è.
    /// </summary>
    public static IReadOnlyList<BarraSuolo> Barre(IReadOnlyList<RigaSuolo> righe, FoglioSuolo f)
    {
        var perVigna = f.Tipo != TipoFoglioSuolo.ConLimiti;
        return righe.GroupBy(r => (Vigna: perVigna ? r.Analisi.Vigna ?? Vuoto : "", Voce: Voce(r.Analisi, f.Dimensione)))
            .Select(g =>
            {
                var valore = Media(g.Select(r => r[f.Campo!])) * f.Fattore;
                var (min, max) = Limiti(g.ToList(), f.Limiti);
                return valore is { } v ? new BarraSuolo(g.Key.Vigna, g.Key.Voce, v, min, max) : null;
            })
            .OfType<BarraSuolo>()
            .OrderBy(b => b.Vigna, OrdineQlik.Istanza).ThenBy(b => b.Voce, OrdineQlik.Istanza)
            .ToList();
    }

    /// <summary>Limiti dei fogli «(1)»: soglie della tessitura (g/kg, ‰, ppm, confermate il 17/11) o 25/37 con una CSC.</summary>
    public static (double? Minimo, double? Massimo) Limiti(IReadOnlyList<RigaSuolo> gruppo, LimitiSuolo limiti) => limiti switch
    {
        LimitiSuolo.SostanzaOrganica => (MediaTessitura(gruppo, t => t.MinSO), MediaTessitura(gruppo, t => t.MaxSO)),
        LimitiSuolo.Azoto => (MediaTessitura(gruppo, t => t.MinN), MediaTessitura(gruppo, t => t.MaxN)),
        LimitiSuolo.Fosforo => (MediaTessitura(gruppo, t => t.MinP), MediaTessitura(gruppo, t => t.MaxP)),
        LimitiSuolo.BrayKurz => Media(gruppo.Select(r => r[CampiSuolo.Csc])) > 0 ? (25, 37) : (null, null),
        _ => (null, null),
    };

    /// <summary>Tessitura per vigneto: argilla, limo e sabbia in ‰ (medie per terna distinta).</summary>
    public static IReadOnlyList<(string Vigneto, double? Argilla, double? Limo, double? Sabbia)> Tessitura(IReadOnlyList<RigaSuolo> righe) =>
        righe.Where(r => r.Tessitura is not null).GroupBy(r => r.Analisi.NomeVigneto)
            .Select(g => (g.Key, MediaTessitura(g, t => t.Argilla) * 10, MediaTessitura(g, t => t.Limo) * 10, MediaTessitura(g, t => t.Sabbia) * 10))
            .OrderBy(x => x.Key, OrdineQlik.Istanza).ToList();

    /// <summary>
    /// Le righe delle tabelle Vigna × Vigneto × Codice punto × Classe: una per combinazione con una terna, con le medie
    /// per terna distinta (in Qlik i campi stanno nella tabella Tessitura).
    /// </summary>
    public static IReadOnlyList<(string Vigna, string Vigneto, string Punto, string Classe, IReadOnlyList<TessituraSuoloDto> Terne)> Classi(
        IReadOnlyList<RigaSuolo> righe) =>
        righe.Where(r => r.Tessitura is not null)
            .GroupBy(r => (Vigna: r.Analisi.Vigna ?? Vuoto, Vigneto: r.Analisi.NomeVigneto, Punto: r.Analisi.CodicePunto ?? Vuoto,
                Classe: r.Tessitura!.Descrizione ?? Vuoto))
            .Select(g => (g.Key.Vigna, g.Key.Vigneto, g.Key.Punto, g.Key.Classe,
                (IReadOnlyList<TessituraSuoloDto>)g.Select(r => r.Tessitura!).DistinctBy(t => t.IdTessitura).ToList()))
            .OrderBy(x => x.Vigna, OrdineQlik.Istanza).ThenBy(x => x.Vigneto, OrdineQlik.Istanza).ThenBy(x => x.Punto, OrdineQlik.Istanza)
            .ToList();

    /// <summary>Composizione della CSC per punto: Ca, Mg, K, Na (ppm / divisore, come il grafico di Qlik) e acidità di scambio.</summary>
    public static readonly (string Nome, string Campo)[] ComponentiCsc =
    [
        ("Ca", CampiSuolo.CaMeq), ("Mg", CampiSuolo.MgMeq), ("K", CampiSuolo.KMeq), ("Na", CampiSuolo.NaMeq),
        ("Acidità di Scambio", CampiSuolo.AciditaMeq),
    ];

    public static IReadOnlyList<(string Punto, double?[] Valori)> ComposizioneCsc(IReadOnlyList<RigaSuolo> righe) =>
        righe.GroupBy(r => r.Analisi.CodicePunto ?? Vuoto)
            .Select(g => (g.Key, ComponentiCsc.Select(c => Media(g.Select(r => r[c.Campo]))).ToArray()))
            .Where(x => x.Item2.Any(v => v is not null))
            .OrderBy(x => x.Key, OrdineQlik.Istanza).ToList();

    /// <summary>Le scelte possibili di ogni filtro, dalle analisi dell'azienda.</summary>
    public static IReadOnlyList<string> Valori(IEnumerable<RigaSuolo> righe, Func<AnalisiSuoloDto, string?> campo) =>
        righe.Select(r => campo(r.Analisi)).OfType<string>().Distinct().Order(OrdineQlik.Istanza).ToList();

    public static string Numero(double? valore, string formato) =>
        valore is { } v ? v.ToString(formato.Replace("0.", "#,##0.", StringComparison.Ordinal), CultureInfo.GetCultureInfo("it-IT")) : "-";
}

/// <summary>Ordine delle dimensioni di Qlik: prima i valori numerici (per valore), poi i testi in ordine alfabetico.</summary>
public sealed class OrdineQlik : IComparer<string>
{
    public static OrdineQlik Istanza { get; } = new();

    public int Compare(string? x, string? y)
    {
        var nx = double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var a);
        var ny = double.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out var b);
        return (nx, ny) switch
        {
            (true, true) => a.CompareTo(b),
            (true, false) => -1,
            (false, true) => 1,
            _ => StringComparer.CurrentCultureIgnoreCase.Compare(x, y),
        };
    }
}
