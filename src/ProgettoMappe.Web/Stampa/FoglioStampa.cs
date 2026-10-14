using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ProgettoMappe.Web.Stampa;

/// <summary>Testi del foglio che non dipendono dai dati.</summary>
public static partial class TestiStampa
{
    /// <summary>Millimetri per lo stile CSS, sempre col punto decimale (la cultura italiana scriverebbe la virgola).</summary>
    public static string Mm(double mm) => mm.ToString("0.##", CultureInfo.InvariantCulture) + "mm";

    /// <summary>
    /// Il credito della base cartografica come testo: l'attribuzione delle sorgenti è HTML (link),
    /// sul foglio restano le parole. Null o vuoto = nessun credito.
    /// </summary>
    public static string? Crediti(string? attribuzione)
    {
        var testo = SpaziMultipli().Replace(WebUtility.HtmlDecode(Tag().Replace(attribuzione ?? "", " ")), " ").Trim();
        return testo == "" ? null : testo;
    }

    /// <summary>Caratteri del valore del tematismo nell'indice: oltre, la colonna lo taglierebbe a metà.</summary>
    public const int CaratteriValore = 19;

    /// <summary>
    /// Il testo accorciato a <paramref name="massimo"/> caratteri con «…» (senza un separatore appeso
    /// alla fine); il testo intero resta nel title, a video. I testi dei tematismi sono già quelli
    /// corti dell'elenco (task #60): questo è solo il paracadute per i nomi lunghi dei dati.
    /// </summary>
    public static string Accorcia(string testo, int massimo)
    {
        testo = testo.Trim();
        return testo.Length <= massimo || massimo < 2 ? testo : testo[..(massimo - 1)].TrimEnd(' ', '·', ',', '-', '–') + "…";
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaziMultipli();
}

public enum FormatoFoglio
{
    A4,
    A3,
}

/// <summary>
/// Misure del foglio in millimetri (task #90): le stesse per l'anteprima a video, la stampa
/// (<c>@page</c>) e il PNG. Il layout è fatto di blocchi ad altezza nota: intestazione in alto, sotto
/// la fascia con legenda e indice, in mezzo i riquadri.
/// </summary>
public sealed record FoglioStampa(FormatoFoglio Formato, bool Orizzontale)
{
    public const double Margine = 10;
    public const double AltezzaIntestazione = 17;
    public const double AltezzaPiede = 5;
    public const double Spazio = 3;
    public const double LarghezzaLegenda = 58;

    /// <summary>Stacco fra due riquadri.</summary>
    public const double SpazioRiquadri = 2;

    /// <summary>Altezza di una riga dell'indice e della legenda (carattere da 7,5 pt).</summary>
    public const double AltezzaRiga = 3.6;

    /// <summary>Larghezza minima di una colonna dell'indice: numero, nome e vigna, ettari, valore.</summary>
    public const double LarghezzaColonnaIndice = 82;

    public double Larghezza => Orizzontale ? LatoLungo : LatoCorto;

    public double Altezza => Orizzontale ? LatoCorto : LatoLungo;

    private double LatoCorto => Formato == FormatoFoglio.A3 ? 297 : 210;

    private double LatoLungo => Formato == FormatoFoglio.A3 ? 420 : 297;

    public double LarghezzaUtile => Larghezza - 2 * Margine;

    /// <summary>Nome per <c>@page { size: … }</c>.</summary>
    public string PerPagina => $"{Formato} {(Orizzontale ? "landscape" : "portrait")}";

    /// <summary>Parte minima dell'altezza per i riquadri nella prima pagina: il resto dell'indice va nelle pagine dopo (task #91).</summary>
    public const double QuotaMinimaRiquadri = 0.6;

    /// <summary>Colonne dell'indice accanto alla legenda (almeno una).</summary>
    public int ColonneIndice => Math.Max(1, (int)((LarghezzaUtile - LarghezzaLegenda - Spazio) / LarghezzaColonnaIndice));

    /// <summary>Colonne dell'indice nelle pagine che lo continuano: tutta la larghezza, niente legenda.</summary>
    public int ColonneIndiceSeguito => Math.Max(1, (int)(LarghezzaUtile / LarghezzaColonnaIndice));

    /// <summary>Altezza fra intestazione e piede: riquadri e fascia nella prima pagina, l'indice nelle altre.</summary>
    public double AltezzaCorpo => Altezza - 2 * Margine - AltezzaIntestazione - Spazio - AltezzaPiede;

    /// <summary>Righe dell'indice per colonna nella prima pagina, se i riquadri tengono la loro quota minima.</summary>
    public int RigheIndicePrimaPagina =>
        Math.Max(0, (int)Math.Floor(((1 - QuotaMinimaRiquadri) * AltezzaCorpo - Spazio) / AltezzaRiga - 1.5 + 1e-9));

    /// <summary>Righe dell'indice per colonna nelle pagine dopo la prima (sotto il titolo «Indice dei vigneti»).</summary>
    public int RigheIndiceSeguito => Math.Max(1, (int)Math.Floor(AltezzaCorpo / AltezzaRiga - 1.5 + 1e-9));

    /// <summary>
    /// Quanti vigneti dell'indice in ogni pagina: nella prima finché i riquadri tengono almeno
    /// <see cref="QuotaMinimaRiquadri"/> dell'altezza, poi pagine di solo indice (a tutta larghezza)
    /// finché serve. Nessun vigneto = nessuna voce (un foglio solo, senza indice).
    /// </summary>
    public IReadOnlyList<int> PagineIndice(int vigneti)
    {
        var pagine = new List<int>();
        var primaPagina = Math.Min(vigneti, RigheIndicePrimaPagina * ColonneIndice);
        if (vigneti <= 0)
        {
            return pagine;
        }

        pagine.Add(primaPagina);
        var restanti = vigneti - primaPagina;
        var perPagina = RigheIndiceSeguito * ColonneIndiceSeguito;
        while (restanti > 0)
        {
            pagine.Add(Math.Min(restanti, perPagina));
            restanti -= perPagina;
        }
        return pagine;
    }

    /// <summary>
    /// Le righe divise in <paramref name="colonne"/> colonne da riempire dall'alto, tutte lunghe uguali
    /// tranne l'ultima (si legge per colonne, come un elenco del telefono).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<T>> InColonne<T>(IReadOnlyList<T> righe, int colonne)
    {
        if (righe.Count == 0)
        {
            return [];
        }
        var perColonna = (righe.Count + Math.Max(1, colonne) - 1) / Math.Max(1, colonne);
        return righe.Chunk(perColonna).Select(c => (IReadOnlyList<T>)c).ToList();
    }

    /// <summary>
    /// Altezza della fascia in basso nella prima pagina: la più alta fra legenda (titolo + voci) e
    /// indice (righe divise nelle colonne, più il titolo). Senza legenda né indice, niente fascia.
    /// </summary>
    public double AltezzaFascia(int vociLegenda, int vignetiIndice)
    {
        var legenda = vociLegenda > 0 ? (vociLegenda + 1.5) * AltezzaRiga : 0;
        var righeIndice = (vignetiIndice + ColonneIndice - 1) / ColonneIndice;
        var indice = vignetiIndice > 0 ? (righeIndice + 1.5) * AltezzaRiga : 0;
        var fascia = Math.Max(legenda, indice);
        return fascia > 0 ? fascia + Spazio : 0;
    }

    /// <summary>
    /// Altezza rimasta ai riquadri nella prima pagina, con i vigneti dell'indice che ci stanno (vedi
    /// <see cref="PagineIndice"/>); mai meno della quota minima (una legenda lunghissima si taglia).
    /// </summary>
    public double AltezzaRiquadri(int vociLegenda, int vignetiPrimaPagina) =>
        Math.Max(QuotaMinimaRiquadri * AltezzaCorpo, AltezzaCorpo - AltezzaFascia(vociLegenda, vignetiPrimaPagina));
}
