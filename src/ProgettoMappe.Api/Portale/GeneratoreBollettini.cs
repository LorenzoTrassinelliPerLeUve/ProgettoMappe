using System.Globalization;
using System.Net;
using System.Reflection;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un prodotto di un trattamento nel bollettino: avversità, nome commerciale, dosaggio (<c>TrattamentoAvversita.Dosaggio</c>).</summary>
public sealed record ProdottoBollettino(int IdAvversita, string Nome, decimal Dosaggio);

/// <summary>
/// Un trattamento di gruppo del giorno, con il suo vigneto: <c>Superficie</c> è la colonna grezza di <c>Vigneto</c> (m²), come
/// il portale (che la divide per 10.000).
/// </summary>
public sealed record TrattamentoBollettino(
    int Id,
    int IdVigneto,
    string? Vigna,
    string? Vigneto,
    decimal? Superficie,
    int? GruppoIdRischio,
    bool? GruppoEsclusi,
    int IdBBCH,
    string? Note,
    string? Transito,
    IReadOnlyList<ProdottoBollettino> Prodotti);

/// <summary>Il meteo di un comune (<c>GetDatiMeteoComuni</c>): ultimi 7 giorni e prossimi 6 (dal giorno del bollettino).</summary>
public sealed record MeteoComuneBollettino(
    string Nome,
    int UltimiGiorniPioggia, decimal UltimiMm, decimal UltimiTMin, decimal UltimiTMed, decimal UltimiTMax,
    int ProssimiGiorniPioggia, decimal ProssimiMm, decimal ProssimiTMin, decimal ProssimiTMed, decimal ProssimiTMax);

/// <summary>I livelli già tradotti in testo (pressione, probabilità, protezione) per peronospora e oidio.</summary>
public sealed record LivelliBollettino(string PressionePeronospora, string PressioneOidio, string ProbabilitaPeronospora,
    string ProbabilitaOidio, string ProtezionePeronospora, string ProtezioneOidio);

/// <summary>Tutto quello che serve per scrivere un bollettino (un tipo, un'azienda, un giorno, una lingua).</summary>
/// <param name="Avversita">Per le altre avversità: Id → nome, nell'ordine di <c>AvversitaDaTrattare</c>.</param>
public sealed record DatiBollettino(
    int Numero,
    string Azienda,
    string? Linea,
    DateTime Giorno,
    DateTime Oggi,
    IReadOnlyList<TrattamentoBollettino> Trattamenti,
    IReadOnlyList<MeteoComuneBollettino> Meteo,
    LivelliBollettino? Livelli = null,
    IReadOnlyList<(int Id, string Nome)>? Avversita = null);

/// <summary>I modelli HTML del portale (cartella <c>Email</c> di 4GrapesAPI), con i segnaposto <c>{NOME}</c>.</summary>
public interface IModelliBollettino
{
    /// <summary>Il testo del modello (es. <c>TrattamentoOidioPeronospora_AA.IT.html</c>), null se manca.</summary>
    string? Leggi(string nome);
}

/// <summary>
/// I modelli dentro l'assembly dell'Api (<c>Portale/ModelliBollettino/*.html</c>, risorse incorporate): copie dei file del
/// portale, nessuna modifica al testo.
/// </summary>
public sealed class ModelliBollettinoIncorporati : IModelliBollettino
{
    private static readonly Assembly Assembly = typeof(ModelliBollettinoIncorporati).Assembly;

    public string? Leggi(string nome)
    {
        var risorsa = Assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("." + nome, StringComparison.OrdinalIgnoreCase));
        if (risorsa is null) return null;
        using var flusso = Assembly.GetManifestResourceStream(risorsa)!;
        using var lettore = new StreamReader(flusso, detectEncodingFromByteOrderMarks: true);
        return lettore.ReadToEnd();
    }
}

public class ModelloBollettinoMancanteException(string nome)
    : InvalidOperationException($"Manca il modello del bollettino {nome}: va copiato dal portale (cartella Email).");

/// <summary>
/// I bollettini di <c>BollettiniTrattamentiController</c> del portale (sezione 12): <c>GeneraBollettino</c> (peronospora/oidio, blocchi
/// AA, AB, BA, BB per rischio del gruppo e EE per gli esclusi) e <c>GeneraBollettino_AltreAvversita</c> (un blocco per avversità).
/// Stessi segnaposto, stesse regole (primo trattamento del gruppo per prodotti e note, superficie grezza / 10.000, «0.###» per le
/// dosi, «0.000» per i totali, moda della fase BBCH). Differenza voluta: i testi che vengono da 4Grapes (nomi, note) si scrivono
/// con l'escape HTML (il portale li inserisce crudi).
/// </summary>
public static class GeneratoreBollettini
{
    public const int TipoPeronosporaOidio = 100;
    public const int TipoAltreAvversita = 200;

    /// <summary>Gli Id di <c>Rischio</c> per blocco (dba 16/11: 1 ALTO/ALTO, 2 ALTO/BASSO, 4 BASSO/ALTO, 5 BASSO/BASSO).</summary>
    public static readonly IReadOnlyList<(string Blocco, int IdRischio)> Blocchi = [("AA", 1), ("AB", 2), ("BA", 4), ("BB", 5)];

    /// <summary>I numeri come li scrive il portale (cultura del server in Italia, anche per la versione inglese).</summary>
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>L'oggetto della mail del portale: «Avviso Trattamento» in italiano, «Tratment Advice» (sic) per gli altri.</summary>
    public static string Oggetto(string lingua) => lingua == "IT" ? "Avviso Trattamento" : "Tratment Advice";

    /// <summary>Le lingue del bollettino: IT e EN (il portale genera solo queste due; chi non ha IT riceve l'inglese).</summary>
    public static string LinguaDi(string? linguaUtente) => linguaUtente == "IT" ? "IT" : "EN";

    /// <summary><c>Utils.Moda</c>: il valore più frequente, a parità il più alto.</summary>
    public static int Moda(IEnumerable<int> valori) =>
        valori.GroupBy(v => v).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).Select(g => g.Key).FirstOrDefault();

    /// <summary>Il bollettino peronospora/oidio (<c>GeneraBollettino</c>).</summary>
    public static string PeronosporaOidio(DatiBollettino d, string lingua, IModelliBollettino modelli)
    {
        var inclusi = d.Trattamenti.Where(t => t.GruppoEsclusi != true).ToList();
        var esclusi = d.Trattamenti.Where(t => t.GruppoEsclusi == true).ToList();
        var campi = CampiComuni(d, Moda(inclusi.Select(t => t.IdBBCH)));
        var l = d.Livelli ?? new LivelliBollettino("", "", "", "", "", "");
        campi["PROTEZIONE_OIDIO"] = Testo(l.ProtezioneOidio);
        campi["PROTEZIONE_PERONOSPORA"] = Testo(l.ProtezionePeronospora);
        campi["PRESSIONE_OIDIO"] = Testo(l.PressioneOidio);
        campi["PRESSIONE_PERONOSPORA"] = Testo(l.PressionePeronospora);
        campi["PROBABILITA_OIDIO"] = Testo(l.ProbabilitaOidio);
        campi["PROBABILITA_PERONOSPORA"] = Testo(l.ProbabilitaPeronospora);
        foreach (var (blocco, rischio) in Blocchi)
        {
            campi["BLOCCO_" + blocco] = BloccoOidioPeronospora(inclusi.Where(t => t.GruppoIdRischio == rischio).ToList(), false, blocco,
                lingua, modelli);
        }

        campi["BLOCCO_EE"] = BloccoOidioPeronospora(esclusi, true, "EE", lingua, modelli);
        return Unisci(Modello(modelli, "TrattamentoOidioPeronospora", lingua), campi);
    }

    /// <summary>Il bollettino delle altre avversità (<c>GeneraBollettino_AltreAvversita</c>).</summary>
    public static string AltreAvversita(DatiBollettino d, string lingua, IModelliBollettino modelli)
    {
        var presenti = d.Trattamenti.SelectMany(t => t.Prodotti).Select(p => p.IdAvversita).ToHashSet();
        var blocchi = string.Concat((d.Avversita ?? []).Where(a => presenti.Contains(a.Id))
            .Select(a => BloccoAvversita(d.Trattamenti, a.Id, a.Nome, lingua, modelli)));
        var campi = CampiComuni(d, Moda(d.Trattamenti.Select(t => t.IdBBCH)));
        campi["BLOCCO_AVVERSITA"] = blocchi;
        return Unisci(Modello(modelli, "TrattamentoAltreAmpelopatie", lingua), campi);
    }

    /// <summary><c>EmailUtils.GeneraEmail</c>: sostituisce ogni <c>{CAMPO}</c>; un valore null toglie il segnaposto.</summary>
    public static string Unisci(string html, IReadOnlyDictionary<string, string?> campi)
    {
        foreach (var (chiave, valore) in campi)
        {
            html = html.Replace("{" + chiave + "}", valore ?? "", StringComparison.Ordinal);
        }

        return html;
    }

    private static string Testo(string? valore) => WebUtility.HtmlEncode(valore ?? "");

    private static string Numero(decimal valore, string formato) => valore.ToString(formato, Cultura);

    private static Dictionary<string, string?> CampiComuni(DatiBollettino d, int bbch)
    {
        var campi = new Dictionary<string, string?>
        {
            ["PROGRESSIVO"] = d.Numero.ToString(CultureInfo.InvariantCulture),
            ["AZIENDA"] = Testo(d.Azienda),
            ["BBCH"] = bbch.ToString(CultureInfo.InvariantCulture),
            ["TIPOLOGIA_LINEA"] = Testo(d.Linea),
            ["DATA_COMUNICAZIONE"] = d.Oggi.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            ["DATA_CONSIGLIO_TRATTAMENTO"] = d.Giorno.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        };
        for (var i = 0; i < 5; i++)
        {
            var m = d.Meteo.ElementAtOrDefault(i);
            var n = (i + 1).ToString(CultureInfo.InvariantCulture);
            campi["METEO_COMUNE_" + n] = m is null ? null : Testo(m.Nome);
            campi["METEO_ULTIMI_GG_PIOGGIA1MM_" + n] = m?.UltimiGiorniPioggia.ToString(CultureInfo.InvariantCulture);
            campi["METEO_ULTIMI_MM_PIOGGIA_" + n] = m is null ? null : Colonna(m.UltimiMm);
            campi["METEO_ULTIMI_T_MIN_" + n] = m is null ? null : Colonna(m.UltimiTMin);
            campi["METEO_ULTIMI_T_MED_" + n] = m is null ? null : Colonna(m.UltimiTMed);
            campi["METEO_ULTIMI_T_MAX_" + n] = m is null ? null : Colonna(m.UltimiTMax);
            campi["METEO_PROSSIMI_GG_PIOGGIA1MM_" + n] = m?.ProssimiGiorniPioggia.ToString(CultureInfo.InvariantCulture);
            campi["METEO_PROSSIMI_MM_PIOGGIA_" + n] = m is null ? null : Colonna(m.ProssimiMm);
            campi["METEO_PROSSIMI_T_MIN_" + n] = m is null ? null : Colonna(m.ProssimiTMin);
            campi["METEO_PROSSIMI_T_MED_" + n] = m is null ? null : Colonna(m.ProssimiTMed);
            campi["METEO_PROSSIMI_T_MAX_" + n] = m is null ? null : Colonna(m.ProssimiTMax);
        }

        return campi;
    }

    /// <summary><c>ToColumn(1)</c> del portale: un decimale.</summary>
    public static string Colonna(decimal valore) => valore.ToString("0.0", Cultura);

    /// <summary><c>GeneraBollettino_BloccoRischio</c> / <c>_BloccoEsclusi</c> (gli esclusi tolgono i prodotti doppi).</summary>
    private static string? BloccoOidioPeronospora(IReadOnlyList<TrattamentoBollettino> trattamenti, bool esclusi, string blocco,
        string lingua, IModelliBollettino modelli)
    {
        var primo = trattamenti.FirstOrDefault();
        IReadOnlyList<ProdottoBollettino> Di(int avversita)
        {
            var p = (primo?.Prodotti ?? []).Where(x => x.IdAvversita == avversita).Select(x => x with { IdAvversita = 0 });
            if (esclusi) p = p.Distinct();
            return p.OrderBy(x => x.Nome, StringComparer.Ordinal).ToList();
        }

        var oidio = Di(ScrittureTrattamenti.AvversitaOidio);
        var peronospora = Di(ScrittureTrattamenti.AvversitaPeronospora);
        if (oidio.Count == 0 && peronospora.Count == 0) return null;

        var vigneti = trattamenti.OrderBy(t => t.Vigna, StringComparer.Ordinal).ToList();
        var superficie = vigneti.Sum(t => t.Superficie ?? 0) / 10000;
        var riga = modelli.Leggi("TrattamentoOidioPeronospora_RIGAPRODOTTI.html")
                   ?? throw new ModelloBollettinoMancanteException("TrattamentoOidioPeronospora_RIGAPRODOTTI.html");
        var righe = string.Concat(Enumerable.Range(0, Math.Max(oidio.Count, peronospora.Count)).Select(i =>
            Unisci(riga, new Dictionary<string, string?>
            {
                ["NOME_PRODOTTO_OIDIO"] = oidio.ElementAtOrDefault(i) is { } o ? Testo(o.Nome) : null,
                ["DOSE_PRODOTTO_OIDIO"] = oidio.ElementAtOrDefault(i) is { } o2 ? Numero(o2.Dosaggio, "0.###") : null,
                ["NOME_PRODOTTO_PERONOSPORA"] = peronospora.ElementAtOrDefault(i) is { } p ? Testo(p.Nome) : null,
                ["DOSE_PRODOTTO_PERONOSPORA"] = peronospora.ElementAtOrDefault(i) is { } p2 ? Numero(p2.Dosaggio, "0.###") : null,
            }) + "\r\n"));
        var campi = new Dictionary<string, string?>
        {
            ["RIGHE_PRODOTTI"] = righe,
            ["HIDDEN_NOTE"] = string.IsNullOrEmpty(primo!.Note) ? "hidden" : null,
            ["NOTE"] = Testo(primo.Note),
            ["TRANSITO_NEL_VIGNETO"] = Testo(primo.Transito),
            ["VIGNETI"] = Vigneti(vigneti),
            ["SUPERFICIE_TOTALE"] = $"{Numero(superficie, "0.00")} ha",
            ["TOTALE_PRODOTTI"] = Totali(peronospora.Union(oidio), superficie),
        };
        return Unisci(Modello(modelli, $"TrattamentoOidioPeronospora_{blocco}", lingua), campi);
    }

    /// <summary><c>GeneraBollettino_AltreAvversita_BloccoAvversita</c>: prodotti del primo trattamento del giorno (non del primo con
    /// quell'avversità, come il portale), solo con dosaggio &gt; 0; vigneti che hanno l'avversità con dosaggio &gt; 0.</summary>
    private static string? BloccoAvversita(IReadOnlyList<TrattamentoBollettino> trattamenti, int avversita, string nome, string lingua,
        IModelliBollettino modelli)
    {
        var primo = trattamenti.FirstOrDefault();
        var prodotti = (primo?.Prodotti ?? []).Where(p => p.IdAvversita == avversita && p.Dosaggio > 0)
            .Select(p => p with { IdAvversita = 0 }).OrderBy(p => p.Nome, StringComparer.Ordinal).Distinct().ToList();
        if (prodotti.Count == 0) return null;

        var vigneti = trattamenti.Where(t => t.Prodotti.Any(p => p.IdAvversita == avversita && p.Dosaggio > 0))
            .OrderBy(t => t.Vigna, StringComparer.Ordinal).ToList();
        var superficie = vigneti.Sum(t => t.Superficie ?? 0) / 10000;
        var riga = modelli.Leggi("TrattamentoAltreAmpelopatie_RIGAPRODOTTI.html")
                   ?? throw new ModelloBollettinoMancanteException("TrattamentoAltreAmpelopatie_RIGAPRODOTTI.html");
        var righe = string.Concat(prodotti.Select(p => Unisci(riga, new Dictionary<string, string?>
        {
            ["NOME_PRODOTTO"] = Testo(p.Nome),
            ["DOSE_PRODOTTO"] = Numero(p.Dosaggio, "0.###"),
        }) + "\r\n"));
        var campi = new Dictionary<string, string?>
        {
            ["AVVERSITA"] = Testo(nome),
            ["RIGHE_PRODOTTI"] = righe,
            ["HIDDEN_NOTE"] = string.IsNullOrEmpty(primo!.Note) ? "hidden" : null,
            ["NOTE"] = Testo(primo.Note),
            ["VIGNETI"] = Vigneti(vigneti),
            ["SUPERFICIE_TOTALE"] = $"{Numero(superficie, "0.00")} ha",
            ["TOTALE_PRODOTTI"] = Totali(prodotti, superficie),
        };
        return Unisci(Modello(modelli, "TrattamentoAltreAmpelopatie_AVVERSITA", lingua), campi);
    }

    private static string Vigneti(IEnumerable<TrattamentoBollettino> vigneti) => string.Join("<br>", vigneti.Select(t =>
        $"{Testo(t.Vigna)} - {Testo(t.Vigneto)} ({(t.Superficie is { } s ? Numero(s / 10000, "0.00") : "")} ha)"));

    private static string Totali(IEnumerable<ProdottoBollettino> prodotti, decimal superficie) =>
        string.Join("<br>", prodotti.Select(p => $"{Testo(p.Nome)} - {Numero(p.Dosaggio * superficie, "0.000")}"));

    /// <summary>Il modello nella lingua, altrimenti quello inglese (come il portale).</summary>
    private static string Modello(IModelliBollettino modelli, string nome, string lingua) =>
        modelli.Leggi($"{nome}.{lingua}.html") ?? modelli.Leggi($"{nome}.EN.html")
        ?? throw new ModelloBollettinoMancanteException($"{nome}.{lingua}.html");
}
