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
    public const int CaratteriValore = 17;

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

/// <summary>Dove va l'indice dei vigneti (scelta nella barra, task #91).</summary>
public enum PosizioneIndice
{
    Sotto,
    PaginaAParte,
    Nessuno,
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

    /// <summary>Altezza di una riga dell'indice e della legenda (carattere da 7–7,5 pt).</summary>
    public const double AltezzaRiga = 3.3;

    /// <summary>
    /// Larghezza minima di una colonna dell'indice: numero, nome e vigna, ettari, valore corto. Compatta
    /// (task #91, nota del proprietario): 4 colonne su un A4 orizzontale.
    /// </summary>
    public const double LarghezzaColonnaIndice = 64;

    /// <summary>Larghezza di una voce della legenda compatta (in fila sotto i riquadri, senza indice accanto).</summary>
    public const double LarghezzaVoceLegenda = 48;

    /// <summary>Oltre questi vigneti l'indice va per intero su una pagina a parte, se l'utente non sceglie altro.</summary>
    public const int VignetiIndiceSotto = 30;

    public double Larghezza => Orizzontale ? LatoLungo : LatoCorto;

    public double Altezza => Orizzontale ? LatoCorto : LatoLungo;

    private double LatoCorto => Formato == FormatoFoglio.A3 ? 297 : 210;

    private double LatoLungo => Formato == FormatoFoglio.A3 ? 420 : 297;

    public double LarghezzaUtile => Larghezza - 2 * Margine;

    /// <summary>Nome per <c>@page { size: … }</c>.</summary>
    public string PerPagina => $"{Formato} {(Orizzontale ? "landscape" : "portrait")}";

    /// <summary>
    /// Parte minima dell'altezza per i riquadri nella prima pagina (task #91: le mappe sono il contenuto
    /// principale, almeno 2/3): il resto dell'indice va nelle pagine dopo.
    /// </summary>
    public const double QuotaMinimaRiquadri = 2.0 / 3;

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

    /// <summary>Dove va l'indice se l'utente non sceglie: sotto i riquadri fino a 30 vigneti, oltre su una pagina a parte.</summary>
    public static PosizioneIndice PosizionePredefinita(int vigneti) =>
        vigneti > VignetiIndiceSotto ? PosizioneIndice.PaginaAParte : PosizioneIndice.Sotto;

    /// <summary>
    /// Quanti vigneti dell'indice in ogni pagina (la prima è quella dei riquadri). Sotto: nella prima
    /// finché i riquadri tengono almeno <see cref="QuotaMinimaRiquadri"/> dell'altezza, poi pagine di
    /// solo indice a tutta larghezza finché serve. Pagina a parte: 0 nella prima, tutto nelle altre.
    /// Nessun vigneto o nessun indice = nessuna voce (un foglio solo).
    /// </summary>
    public IReadOnlyList<int> PagineIndice(int vigneti, PosizioneIndice posizione = PosizioneIndice.Sotto)
    {
        var pagine = new List<int>();
        if (vigneti <= 0 || posizione == PosizioneIndice.Nessuno)
        {
            return pagine;
        }

        var primaPagina = posizione == PosizioneIndice.Sotto ? Math.Min(vigneti, RigheIndicePrimaPagina * ColonneIndice) : 0;

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
    /// Pagine stampate in tutto (task #94): quelle dei riquadri, poi quelle che l'indice aggiunge. La prima
    /// voce di <see cref="PagineIndice"/> sta sotto l'ultima pagina di riquadri, le altre sono pagine nuove.
    /// </summary>
    public static int TotalePagine(int pagineRiquadri, IReadOnlyList<int> pagineIndice) =>
        Math.Max(1, pagineRiquadri) + Math.Max(0, pagineIndice.Count - 1);

    /// <summary>Numero della pagina della voce <paramref name="voce"/> di <see cref="PagineIndice"/> (da 0).</summary>
    public static int PaginaIndice(int pagineRiquadri, int voce) => Math.Max(1, pagineRiquadri) + voce;

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

    /// <summary>Colonne della legenda compatta (senza indice accanto: le voci in fila).</summary>
    public int ColonneLegenda => Math.Max(1, (int)(LarghezzaUtile / LarghezzaVoceLegenda));

    /// <summary>
    /// Altezza della fascia in basso nella prima pagina: la più alta fra legenda (titolo + voci) e
    /// indice (righe divise nelle colonne, più il titolo). Senza indice la legenda è compatta, a
    /// colonne (<see cref="ColonneLegenda"/>). Senza legenda né indice, niente fascia.
    /// </summary>
    public double AltezzaFascia(int vociLegenda, int vignetiIndice)
    {
        var righeLegenda = vignetiIndice > 0 ? vociLegenda : (vociLegenda + ColonneLegenda - 1) / ColonneLegenda;
        var legenda = vociLegenda > 0 ? (righeLegenda + 1.5) * AltezzaRiga : 0;
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
