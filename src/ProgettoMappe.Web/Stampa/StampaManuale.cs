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
/// disegnato, nord in alto), la larghezza in colonne della griglia del foglio, l'altezza in righe e la
/// pagina in cui sta (da 1, task #94).
/// </summary>
public sealed record RiquadroManuale(Ingombro Zona, int Colonne, int Righe, int Pagina = 1);

/// <summary>Un riquadro manuale con la chiave che lo segue finché la pagina del browser è aperta.</summary>
public sealed record RiquadroDisegnato(int Chiave, RiquadroManuale Riquadro);

/// <summary>Una pagina del layout manuale: i suoi riquadri (indici nell'elenco) e come stanno nella griglia.</summary>
public sealed record PaginaManuale(int Numero, IReadOnlyList<int> Riquadri, DisposizioneManuale Disposizione);

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
    /// «Parti dall'automatico»: i riquadri automatici (con le loro celle in mm e la loro pagina, da 1) come
    /// riquadri manuali, pagina per pagina nell'ordine in cui si leggono. Zona = i vigneti con un po' di
    /// margine; larghezza = la taglia più vicina a quella della cella; altezza = le righe più vicine a
    /// quella della cella, nell'area dei riquadri della sua pagina (<paramref name="altezza"/> per numero di pagina).
    /// </summary>
    public static IReadOnlyList<RiquadroManuale> DaAutomatico(IReadOnlyList<RiquadroStampa> riquadri, IReadOnlyList<CellaStampa> celle,
        IReadOnlyList<int> pagine, double larghezza, Func<int, double> altezza, double spazio)
    {
        var lc = (larghezza + spazio) / Colonne;
        return riquadri.Select((r, i) => (Riquadro: r, Cella: celle[i], Pagina: Math.Clamp(pagine[i], 1, PagineManuali.MassimoPagine)))
            .OrderBy(c => c.Pagina).ThenBy(c => Math.Round(c.Cella.Y, 1)).ThenBy(c => c.Cella.X)
            .Take(MassimoRiquadri)
            .Select(c => new RiquadroManuale(
                ConMargine(c.Riquadro.Ingombro),
                Taglia((int)Math.Round((c.Cella.Larghezza + spazio) / lc)),
                Math.Clamp((int)Math.Round((c.Cella.Altezza + spazio) / ((altezza(c.Pagina) + spazio) / Righe)), 1, Righe),
                c.Pagina))
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

/// <summary>
/// Le pagine del layout manuale (task #94): ogni riquadro sta in una pagina, con la sua griglia; le
/// pagine possono restare vuote finché l'utente non le toglie. L'elenco dei riquadri si tiene in ordine
/// di pagina (<see cref="InOrdine"/>): lettere e numeri dei vigneti seguono le pagine.
/// </summary>
public static class PagineManuali
{
    /// <summary>Pagine di riquadri al massimo (con 40 riquadri al massimo in tutto).</summary>
    public const int MassimoPagine = 10;

    /// <summary>Le pagine del layout: quelle dichiarate e almeno quelle che i riquadri usano, fra 1 e <see cref="MassimoPagine"/>.</summary>
    public static int Conta(int dichiarate, IEnumerable<RiquadroManuale> riquadri) =>
        Math.Clamp(Math.Max(dichiarate, riquadri.Select(r => r.Pagina).DefaultIfEmpty(1).Max()), 1, MassimoPagine);

    /// <summary>I riquadri in ordine di pagina; nella stessa pagina resta l'ordine che hanno.</summary>
    public static List<RiquadroDisegnato> InOrdine(IEnumerable<RiquadroDisegnato> riquadri) =>
        riquadri.OrderBy(r => r.Riquadro.Pagina).ToList();

    /// <summary>
    /// Trascinato nel foglio: il riquadro all'indice <paramref name="da"/> prende il posto e la pagina di
    /// quello all'indice <paramref name="a"/> (gli altri scorrono).
    /// </summary>
    public static List<RiquadroDisegnato> Sposta(IReadOnlyList<RiquadroDisegnato> riquadri, int da, int a)
    {
        if (da < 0 || da >= riquadri.Count || a < 0 || a >= riquadri.Count || da == a)
        {
            return riquadri.ToList();
        }
        var lista = GrigliaManuale.Sposta(riquadri, da, a).ToList();
        lista[a] = ConPagina(lista[a], riquadri[a].Riquadro.Pagina);
        return InOrdine(lista);
    }

    /// <summary>
    /// ↑ e ↓ della barra: scambia il riquadro col vicino; se il vicino è in un'altra pagina il riquadro
    /// passa solo di pagina (in fondo a quella prima, in cima a quella dopo).
    /// </summary>
    public static List<RiquadroDisegnato> SuOGiu(IReadOnlyList<RiquadroDisegnato> riquadri, int indice, bool su)
    {
        var vicino = su ? indice - 1 : indice + 1;
        if (indice < 0 || indice >= riquadri.Count || vicino < 0 || vicino >= riquadri.Count)
        {
            return riquadri.ToList();
        }
        if (riquadri[vicino].Riquadro.Pagina == riquadri[indice].Riquadro.Pagina)
        {
            return GrigliaManuale.Sposta(riquadri, indice, vicino).ToList();
        }
        var lista = riquadri.ToList();
        lista[indice] = ConPagina(lista[indice], riquadri[vicino].Riquadro.Pagina);
        return InOrdine(lista);
    }

    /// <summary>Il riquadro con quella chiave in fondo alla pagina <paramref name="pagina"/> (campo «Pagina», o trascinato in una parte vuota).</summary>
    public static List<RiquadroDisegnato> InPagina(IReadOnlyList<RiquadroDisegnato> riquadri, int chiave, int pagina)
    {
        var lista = riquadri.ToList();
        var i = lista.FindIndex(r => r.Chiave == chiave);
        if (i < 0 || pagina < 1 || pagina > MassimoPagine || lista[i].Riquadro.Pagina == pagina)
        {
            return lista;
        }
        var riquadro = ConPagina(lista[i], pagina);
        lista.RemoveAt(i);
        lista.Add(riquadro);
        return InOrdine(lista);
    }

    /// <summary>
    /// «×» su una pagina vuota: la pagina si toglie e quelle dopo scalano di uno. Una pagina con dei
    /// riquadri, o l'unica, resta.
    /// </summary>
    public static (List<RiquadroDisegnato> Riquadri, int Pagine) TogliPagina(IReadOnlyList<RiquadroDisegnato> riquadri, int pagine, int pagina)
    {
        if (pagine <= 1 || pagina < 1 || pagina > pagine || riquadri.Any(r => r.Riquadro.Pagina == pagina))
        {
            return (riquadri.ToList(), pagine);
        }
        return (riquadri.Select(r => r.Riquadro.Pagina > pagina ? ConPagina(r, r.Riquadro.Pagina - 1) : r).ToList(), pagine - 1);
    }

    /// <summary>
    /// Le pagine con i loro riquadri, ognuna con la sua griglia (<see cref="GrigliaManuale.Disponi"/>);
    /// <paramref name="altezza"/> dà l'altezza dell'area dei riquadri per numero di pagina (l'ultima può
    /// avere sotto anche l'indice). Un riquadro con una pagina oltre l'ultima va nell'ultima.
    /// </summary>
    public static IReadOnlyList<PaginaManuale> Disponi(IReadOnlyList<RiquadroManuale> riquadri, int pagine,
        double larghezza, Func<int, double> altezza, double spazio)
    {
        var n = Conta(pagine, riquadri);
        return Enumerable.Range(1, n).Select(p =>
        {
            var indici = Enumerable.Range(0, riquadri.Count).Where(i => Math.Clamp(riquadri[i].Pagina, 1, n) == p).ToList();
            return new PaginaManuale(p, indici, GrigliaManuale.Disponi(indici.Select(i => riquadri[i]).ToList(), larghezza, altezza(p), spazio));
        }).ToList();
    }

    private static RiquadroDisegnato ConPagina(RiquadroDisegnato r, int pagina) => r with { Riquadro = r.Riquadro with { Pagina = pagina } };
}

/// <summary>Il layout manuale com'è salvato: riquadri, pagine (task #94) e scelte del foglio.</summary>
public sealed record LayoutManuale(
    IReadOnlyList<RiquadroManuale> Riquadri,
    FoglioStampa Foglio,
    PosizioneIndice? Indice = null,
    string Titolo = "",
    bool Satellite = false,
    int Pagine = 1);

/// <summary>Un layout letto da un file o dal browser, con l'azienda e il tematismo per cui era stato fatto.</summary>
public sealed record LayoutLetto(LayoutManuale Layout, int? Azienda, string? Tematismo, int RiquadriScartati);

/// <summary>
/// Il layout manuale in JSON (task #92): lo stesso testo per il browser (localStorage) e per il file
/// «Salva layout» / «Carica layout». La lettura non si fida: un file rotto o di un altro programma dà
/// un messaggio, un riquadro non valido si scarta, taglie e righe fuori misura si riportano nei limiti.
/// Le pagine (task #94) sono campi in più della stessa versione: un layout di prima sta tutto a pagina 1.
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
            PagineManuali.Conta(layout.Pagine, layout.Riquadri),
            layout.Riquadri.Select(r => (Riquadro?)new Riquadro(
                r.Zona.ComeArray().Select(v => Math.Round(v, 7)).ToArray(), r.Colonne, r.Righe, r.Pagina)).ToList()), Opzioni);

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
                    GrigliaManuale.Taglia(r.Colonne ?? 6), Math.Clamp(r.Righe ?? 4, 1, GrigliaManuale.Righe),
                    Math.Clamp(r.Pagina ?? 1, 1, PagineManuali.MassimoPagine)));
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
        var layout = new LayoutManuale(riquadri, foglio, indice, titolo.Length > 120 ? titolo[..120] : titolo, contenuto.Satellite ?? false,
            PagineManuali.Conta(contenuto.Pagine ?? 1, riquadri));
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
        Foglio? Foglio, string? Indice, string? Titolo, bool? Satellite, int? Pagine, List<Riquadro?>? Riquadri);

    private sealed record Foglio(string? Formato, bool? Orizzontale);

    private sealed record Riquadro(double[]? Zona, int? Colonne, int? Righe, int? Pagina);
}
