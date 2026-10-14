using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgettoMappe.Web.Stampa;

/// <summary>Come nascono i riquadri del foglio: raggruppati da soli (task #90) o disegnati dall'utente (task #92).</summary>
public enum ModoStampa
{
    Automatico,
    Manuale,
}

/// <summary>
/// Un riquadro disegnato sulla mappa d'insieme (task #92): la zona in gradi WGS84 (il rettangolo
/// disegnato, nord in alto), la larghezza in colonne della griglia del foglio e l'altezza in righe.
/// </summary>
public sealed record RiquadroManuale(Ingombro Zona, int Colonne, int Righe);

/// <summary>Il posto di un riquadro nella griglia del foglio: colonna e riga (da 0) e misure in celle.</summary>
public sealed record PostoGriglia(int Colonna, int Riga, int Colonne, int Righe);

/// <summary>
/// I riquadri sul foglio: posti nella griglia, celle in mm (come <see cref="RiquadriStampa.Disponi"/>) e
/// righe usate. Oltre <see cref="GrigliaManuale.Righe"/> righe le righe si accorciano per stare nel foglio.
/// </summary>
public sealed record DisposizioneManuale(IReadOnlyList<PostoGriglia> Posti, IReadOnlyList<CellaStampa> Celle, int RigheUsate)
{
    public bool Compressa => RigheUsate > GrigliaManuale.Righe;
}

/// <summary>
/// Griglia del layout manuale (task #92): 12 colonne e 12 righe nell'area dei riquadri. I riquadri si
/// mettono in ordine, ognuno nel primo posto libero dopo il precedente (da sinistra a destra, poi giù,
/// come si legge), senza tornare indietro a riempire i buchi: l'ordine scelto resta l'ordine sul foglio.
/// </summary>
public static class GrigliaManuale
{
    public const int Colonne = 12;
    public const int Righe = 12;

    /// <summary>Larghezze possibili: un quarto, metà, tutta la larghezza.</summary>
    public static readonly IReadOnlyList<int> Taglie = [3, 6, 12];

    /// <summary>Riquadri al massimo in un layout (oltre, il foglio non si legge comunque).</summary>
    public const int MassimoRiquadri = 40;

    /// <summary>La taglia più vicina a <paramref name="colonne"/> (a pari distanza la più grande).</summary>
    public static int Taglia(int colonne) => Taglie.OrderBy(t => Math.Abs(t - colonne)).ThenByDescending(t => t).First();

    public static string NomeTaglia(int colonne) => colonne switch
    {
        3 => "¼",
        6 => "½",
        12 => "intero",
        _ => $"{colonne}/12",
    };

    /// <summary>Posti dei riquadri nella griglia, nell'ordine dato (vedi la classe).</summary>
    public static IReadOnlyList<PostoGriglia> Posiziona(IReadOnlyList<(int Colonne, int Righe)> taglie)
    {
        var occupate = new List<bool[]>();
        bool Libero(int riga, int colonna, int larghe, int alte)
        {
            for (var r = riga; r < riga + alte; r++)
            {
                for (var c = colonna; c < colonna + larghe; c++)
                {
                    if (r < occupate.Count && occupate[r][c])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        var posti = new List<PostoGriglia>();
        var (rigaCursore, colonnaCursore) = (0, 0);
        foreach (var (colonne, righe) in taglie)
        {
            var larghe = Math.Clamp(colonne, 1, Colonne);
            var alte = Math.Max(1, righe);
            var (riga, colonna) = (rigaCursore, colonnaCursore);
            while (colonna + larghe > Colonne || !Libero(riga, colonna, larghe, alte))
            {
                if (++colonna + larghe > Colonne)
                {
                    (riga, colonna) = (riga + 1, 0);
                }
            }

            while (occupate.Count < riga + alte)
            {
                occupate.Add(new bool[Colonne]);
            }
            for (var r = riga; r < riga + alte; r++)
            {
                for (var c = colonna; c < colonna + larghe; c++)
                {
                    occupate[r][c] = true;
                }
            }
            posti.Add(new PostoGriglia(colonna, riga, larghe, alte));
            (rigaCursore, colonnaCursore) = (riga, colonna + larghe);
        }
        return posti;
    }

    /// <summary>Celle in mm nell'area dei riquadri (<paramref name="spazio"/> = stacco fra due riquadri).</summary>
    public static DisposizioneManuale Disponi(IReadOnlyList<RiquadroManuale> riquadri, double larghezza, double altezza, double spazio)
    {
        var posti = Posiziona(riquadri.Select(r => (r.Colonne, r.Righe)).ToList());
        var righeUsate = posti.Count == 0 ? 0 : posti.Max(p => p.Riga + p.Righe);
        var (lc, ac) = ((larghezza + spazio) / Colonne, (altezza + spazio) / Math.Max(Righe, righeUsate));
        var celle = posti.Select(p => new CellaStampa(
            p.Colonna * lc, p.Riga * ac, Math.Max(0, p.Colonne * lc - spazio), Math.Max(0, p.Righe * ac - spazio))).ToList();
        return new DisposizioneManuale(posti, celle, righeUsate);
    }

    /// <summary>
    /// Le righe che danno a un riquadro largo <paramref name="colonne"/> le proporzioni della sua zona
    /// (così attorno resta poco margine), fra 1 e <see cref="Righe"/>.
    /// </summary>
    public static int RigheAdatte(Ingombro zona, int colonne, double larghezza, double altezza, double spazio)
    {
        var (lc, ac) = ((larghezza + spazio) / Colonne, (altezza + spazio) / Righe);
        var desiderata = (colonne * lc - spazio) / AspettoMercatore(zona);
        return Math.Clamp((int)Math.Round((desiderata + spazio) / ac), 1, Righe);
    }

    /// <summary>
    /// Larghezza / altezza della zona come la mostra la mappa (proiezione di Mercatore, quella di MapLibre):
    /// il riquadro sul foglio la mostra con queste proporzioni, mai deformata.
    /// </summary>
    public static double AspettoMercatore(Ingombro zona)
    {
        static double Y(double lat) => Math.Log(Math.Tan(Math.PI / 4 + Math.Clamp(lat, -85, 85) * Math.PI / 360)) * 180 / Math.PI;
        var alta = Y(zona.Nord) - Y(zona.Sud);
        return alta > 0 && zona.Est > zona.Ovest ? Math.Clamp((zona.Est - zona.Ovest) / alta, 0.05, 20) : 1;
    }

    /// <summary>I vigneti di una zona: quelli con il centro del loro ingombro dentro il rettangolo.</summary>
    public static IReadOnlyList<int> Contenuti(Ingombro zona, IReadOnlyList<VignetoInStampa> vigneti) =>
        vigneti.Where(v =>
        {
            var (x, y) = ((v.Ingombro.Ovest + v.Ingombro.Est) / 2, (v.Ingombro.Sud + v.Ingombro.Nord) / 2);
            return x >= zona.Ovest && x <= zona.Est && y >= zona.Sud && y <= zona.Nord;
        }).Select(v => v.Id).Distinct().ToList();

    /// <summary>
    /// L'ingombro allargato di <paramref name="quota"/> del lato più lungo per parte (almeno
    /// <paramref name="minimoMetri"/>): i vigneti non toccano il bordo del riquadro.
    /// </summary>
    public static Ingombro ConMargine(Ingombro ingombro, double quota = 0.08, double minimoMetri = 100)
    {
        var metri = Math.Max(minimoMetri, quota * Math.Max(ingombro.LarghezzaMetri, ingombro.AltezzaMetri));
        var dLat = metri / Ingombro.MetriPerGradoLat;
        var dLon = metri / (Ingombro.MetriPerGradoLonEquatore * Math.Max(0.01, Math.Cos((ingombro.Sud + ingombro.Nord) / 2 * Math.PI / 180)));
        return new Ingombro(ingombro.Ovest - dLon, ingombro.Sud - dLat, ingombro.Est + dLon, ingombro.Nord + dLat);
    }

    /// <summary>
    /// «Parti dall'automatico»: i riquadri automatici (con le loro celle in mm) come riquadri manuali,
    /// nell'ordine in cui si leggono sul foglio. Zona = i vigneti con un po' di margine; larghezza = la
    /// taglia più vicina a quella della cella; altezza = le righe più vicine a quella della cella.
    /// </summary>
    public static IReadOnlyList<RiquadroManuale> DaAutomatico(IReadOnlyList<RiquadroStampa> riquadri, IReadOnlyList<CellaStampa> celle,
        double larghezza, double altezza, double spazio)
    {
        var (lc, ac) = ((larghezza + spazio) / Colonne, (altezza + spazio) / Righe);
        return riquadri.Zip(celle)
            .OrderBy(c => Math.Round(c.Second.Y, 1)).ThenBy(c => c.Second.X)
            .Take(MassimoRiquadri)
            .Select(c => new RiquadroManuale(
                ConMargine(c.First.Ingombro),
                Taglia((int)Math.Round((c.Second.Larghezza + spazio) / lc)),
                Math.Clamp((int)Math.Round((c.Second.Altezza + spazio) / ac), 1, Righe)))
            .ToList();
    }

    /// <summary>Il riquadro all'indice <paramref name="da"/> spostato all'indice <paramref name="a"/> (gli altri scorrono).</summary>
    public static IReadOnlyList<T> Sposta<T>(IReadOnlyList<T> elementi, int da, int a)
    {
        var lista = elementi.ToList();
        if (da < 0 || da >= lista.Count || a < 0 || a >= lista.Count || da == a)
        {
            return lista;
        }
        var elemento = lista[da];
        lista.RemoveAt(da);
        lista.Insert(a, elemento);
        return lista;
    }
}

/// <summary>Il layout manuale com'è salvato: riquadri e scelte del foglio.</summary>
public sealed record LayoutManuale(
    IReadOnlyList<RiquadroManuale> Riquadri,
    FoglioStampa Foglio,
    PosizioneIndice? Indice = null,
    string Titolo = "",
    bool Satellite = false);

/// <summary>Un layout letto da un file o dal browser, con l'azienda e il tematismo per cui era stato fatto.</summary>
public sealed record LayoutLetto(LayoutManuale Layout, int? Azienda, string? Tematismo, int RiquadriScartati);

/// <summary>
/// Il layout manuale in JSON (task #92): lo stesso testo per il browser (localStorage) e per il file
/// «Salva layout» / «Carica layout». La lettura non si fida: un file rotto o di un altro programma dà
/// un messaggio, un riquadro non valido si scarta, taglie e righe fuori misura si riportano nei limiti.
/// </summary>
public static class FileLayout
{
    public const string Tipo = "mappe-layout-stampa";
    public const int Versione = 1;

    /// <summary>Oltre questa misura non è un layout (40 riquadri stanno in pochi kB).</summary>
    public const int DimensioneMassima = 256 * 1024;

    private static readonly JsonSerializerOptions Opzioni = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string Scrivi(LayoutManuale layout, int azienda, string? tematismo, DateTimeOffset salvato) =>
        JsonSerializer.Serialize(new Contenuto(
            Tipo, Versione, azienda, tematismo, salvato,
            new Foglio(layout.Foglio.Formato.ToString(), layout.Foglio.Orizzontale),
            layout.Indice?.ToString(),
            string.IsNullOrWhiteSpace(layout.Titolo) ? null : layout.Titolo.Trim(),
            layout.Satellite,
            layout.Riquadri.Select(r => (Riquadro?)new Riquadro(
                r.Zona.ComeArray().Select(v => Math.Round(v, 7)).ToArray(), r.Colonne, r.Righe)).ToList()), Opzioni);

    /// <summary>Il layout, o null con il motivo in <paramref name="errore"/> (in parole semplici).</summary>
    public static LayoutLetto? Leggi(string? testo, out string? errore)
    {
        errore = null;
        if (string.IsNullOrWhiteSpace(testo) || testo.Length > DimensioneMassima)
        {
            errore = "Il file è vuoto o troppo grande per essere un layout.";
            return null;
        }

        Contenuto? contenuto;
        try
        {
            contenuto = JsonSerializer.Deserialize<Contenuto>(testo, Opzioni);
        }
        catch (JsonException)
        {
            contenuto = null;
        }
        if (contenuto is null || contenuto.Tipo != Tipo)
        {
            errore = "Non è un layout di stampa delle Mappe.";
            return null;
        }
        if (contenuto.Versione != Versione)
        {
            errore = "Il layout è di una versione che questa pagina non conosce.";
            return null;
        }

        var riquadri = new List<RiquadroManuale>();
        var scartati = 0;
        foreach (var r in contenuto.Riquadri ?? [])
        {
            if (riquadri.Count < GrigliaManuale.MassimoRiquadri && r?.Zona is [var o, var s, var e, var n]
                && new[] { o, s, e, n }.All(double.IsFinite)
                && o >= -180 && e <= 180 && s >= -85 && n <= 85 && o < e && s < n)
            {
                riquadri.Add(new RiquadroManuale(new Ingombro(o, s, e, n),
                    GrigliaManuale.Taglia(r.Colonne ?? 6), Math.Clamp(r.Righe ?? 4, 1, GrigliaManuale.Righe)));
            }
            else
            {
                scartati++;
            }
        }

        var foglio = new FoglioStampa(
            Enum.TryParse<FormatoFoglio>(contenuto.Foglio?.Formato, true, out var formato) && Enum.IsDefined(formato) ? formato : FormatoFoglio.A4,
            contenuto.Foglio?.Orizzontale ?? true);
        PosizioneIndice? indice = Enum.TryParse<PosizioneIndice>(contenuto.Indice, true, out var p) && Enum.IsDefined(p) ? p : null;
        var titolo = (contenuto.Titolo ?? "").Trim();
        var layout = new LayoutManuale(riquadri, foglio, indice, titolo.Length > 120 ? titolo[..120] : titolo, contenuto.Satellite ?? false);
        return new LayoutLetto(layout, contenuto.Azienda, string.IsNullOrWhiteSpace(contenuto.Tematismo) ? null : contenuto.Tematismo, scartati);
    }

    /// <summary>Nome del file: «layout-casa-e-mirafiore.json».</summary>
    public static string NomeFile(string azienda)
    {
        var parole = new string(azienda.ToLower(CultureInfo.GetCultureInfo("it-IT")).Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return $"layout-{(parole.Length == 0 ? "azienda" : string.Join('-', parole))}.json";
    }

    private sealed record Contenuto(string? Tipo, int Versione, int? Azienda, string? Tematismo, DateTimeOffset? Salvato,
        Foglio? Foglio, string? Indice, string? Titolo, bool? Satellite, List<Riquadro?>? Riquadri);

    private sealed record Foglio(string? Formato, bool? Orizzontale);

    private sealed record Riquadro(double[]? Zona, int? Colonne, int? Righe);
}
