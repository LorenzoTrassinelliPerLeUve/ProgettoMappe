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

    /// <summary>Colonne dell'indice accanto alla legenda (almeno una).</summary>
    public int ColonneIndice => Math.Max(1, (int)((LarghezzaUtile - LarghezzaLegenda - Spazio) / LarghezzaColonnaIndice));

    /// <summary>
    /// Altezza della fascia in basso: la più alta fra legenda (titolo + voci) e indice (righe divise
    /// nelle colonne, più l'intestazione delle colonne). Senza legenda né indice, niente fascia.
    /// </summary>
    public double AltezzaFascia(int vociLegenda, int vignetiIndice)
    {
        var legenda = vociLegenda > 0 ? (vociLegenda + 1.5) * AltezzaRiga : 0;
        var righeIndice = (vignetiIndice + ColonneIndice - 1) / ColonneIndice;
        var indice = vignetiIndice > 0 ? (righeIndice + 1.5) * AltezzaRiga : 0;
        var fascia = Math.Max(legenda, indice);
        return fascia > 0 ? fascia + Spazio : 0;
    }

    /// <summary>Altezza rimasta ai riquadri (mai meno di un terzo del foglio: oltre, l'indice va su più fogli… o in A3).</summary>
    public double AltezzaRiquadri(int vociLegenda, int vignetiIndice) =>
        Math.Max(Altezza / 3,
            Altezza - 2 * Margine - AltezzaIntestazione - Spazio - AltezzaPiede - AltezzaFascia(vociLegenda, vignetiIndice));

    /// <summary>L'indice non ci sta sul foglio insieme ai riquadri: la pagina consiglia A3, orizzontale o di nasconderlo.</summary>
    public bool IndiceTroppoLungo(int vociLegenda, int vignetiIndice) =>
        Altezza - 2 * Margine - AltezzaIntestazione - Spazio - AltezzaPiede - AltezzaFascia(vociLegenda, vignetiIndice) < Altezza / 3;
}
