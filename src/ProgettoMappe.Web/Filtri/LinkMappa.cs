using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace ProgettoMappe.Web.Filtri;

/// <summary>Centro (gradi WGS84) e zoom della mappa.</summary>
public sealed record VistaMappa(double Lon, double Lat, double Zoom);

/// <summary>
/// Stato della mappa scritto nell'indirizzo della pagina, così un link (o un ricaricamento)
/// riapre la stessa vista: <c>/mappa/{azienda}?gruppo=1&amp;vigna=2&amp;vitigno=3&amp;tema=punteggio&amp;anno=2025&amp;centro=13.1,45.9&amp;zoom=14.5</c>.
/// L'azienda resta nel percorso. Valori mancanti o non validi = scelta predefinita (mai un errore):
/// i valori inesistenti per l'azienda (vigna, vitigno, anno, gruppo) li scarta la pagina.
/// <see cref="Da"/>: anno di partenza del confronto fra anni (<c>da=2024</c>; assente = nessun confronto).
/// <see cref="Vigneto"/>: vigneto con la scheda aperta (<c>vigneto=16792</c>, «Copia link» della scheda).
/// <see cref="Ordine"/>: ordine dell'elenco (<c>ordine=valore</c>; assente = per nome).
/// <see cref="FiltriChiusi"/>, <see cref="TemiChiusi"/>: riquadri della colonna chiusi (<c>chiusi=filtri,temi</c>).
/// </summary>
public sealed record LinkMappa(int? Gruppo, int? Vigna, int? Vitigno, string? Tema, int? Anno, VistaMappa? Vista, int? Da = null, int? Vigneto = null,
    OrdineElenco Ordine = OrdineElenco.Nome, bool FiltriChiusi = false, bool TemiChiusi = false)
{
    public static readonly LinkMappa Vuoto = new(null, null, null, null, null, null);

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Legge lo stato dalla query di un indirizzo (assoluto o relativo).</summary>
    public static LinkMappa Leggi(string uri)
    {
        var inizio = uri.IndexOf('?');
        if (inizio < 0)
        {
            return Vuoto;
        }

        var fine = uri.IndexOf('#', inizio);
        var query = QueryHelpers.ParseQuery(fine < 0 ? uri[inizio..] : uri[inizio..fine]);
        string? Valore(string nome) => query.TryGetValue(nome, out var v) ? v.ToString() : null;
        var chiusi = (Valore("chiusi") ?? "").Split(',', StringSplitOptions.TrimEntries);

        return new LinkMappa(
            Intero(Valore("gruppo")),
            Intero(Valore("vigna")),
            Intero(Valore("vitigno")),
            string.IsNullOrWhiteSpace(Valore("tema")) ? null : Valore("tema")!.Trim(),
            Intero(Valore("anno")),
            LeggiVista(Valore("centro"), Valore("zoom")),
            Intero(Valore("da")),
            Intero(Valore("vigneto")),
            ElencoVigneti.Leggi(Valore("ordine")),
            chiusi.Contains("filtri"),
            chiusi.Contains("temi"));
    }

    /// <summary>Percorso relativo della pagina (senza "/" iniziale, come per NavigateTo) con la query dei soli valori presenti.</summary>
    public string Indirizzo(int aziendaId) => Indirizzo("mappa", aziendaId);

    /// <summary>
    /// Il layout di stampa (task #90) con lo stesso tematismo, anni e filtri della mappa; inquadratura,
    /// scheda aperta, ordine dell'elenco e riquadri chiusi non servono al foglio.
    /// </summary>
    public string IndirizzoStampa(int aziendaId) =>
        (this with { Vista = null, Vigneto = null, Ordine = OrdineElenco.Nome, FiltriChiusi = false, TemiChiusi = false })
        .Indirizzo("stampa", aziendaId);

    private string Indirizzo(string pagina, int aziendaId)
    {
        var parametri = new List<string>();
        void Aggiungi(string nome, string? valore)
        {
            if (!string.IsNullOrEmpty(valore))
            {
                parametri.Add($"{nome}={Uri.EscapeDataString(valore)}");
            }
        }

        Aggiungi("gruppo", Gruppo?.ToString(Inv));
        Aggiungi("vigna", Vigna?.ToString(Inv));
        Aggiungi("vitigno", Vitigno?.ToString(Inv));
        Aggiungi("vigneto", Vigneto?.ToString(Inv));
        Aggiungi("tema", Tema);
        Aggiungi("da", Da?.ToString(Inv));
        Aggiungi("anno", Anno?.ToString(Inv));
        Aggiungi("ordine", ElencoVigneti.PerLink(Ordine));
        if (FiltriChiusi || TemiChiusi)
        {
            // Valori fissi, senza caratteri da codificare: la virgola resta leggibile (come in centro=).
            parametri.Add("chiusi=" + string.Join(',', new[] { FiltriChiusi ? "filtri" : null, TemiChiusi ? "temi" : null }.OfType<string>()));
        }
        if (Vista is { } vista)
        {
            // 5 decimali ≈ 1 m: basta per riaprire la stessa inquadratura, il link resta corto.
            // La virgola è lecita in una query: niente %2C, il link resta leggibile.
            parametri.Add($"centro={Math.Round(vista.Lon, 5).ToString(Inv)},{Math.Round(vista.Lat, 5).ToString(Inv)}");
            Aggiungi("zoom", Math.Round(vista.Zoom, 2).ToString(Inv));
        }

        var percorso = $"{pagina}/{aziendaId.ToString(Inv)}";
        return parametri.Count == 0 ? percorso : $"{percorso}?{string.Join('&', parametri)}";
    }

    private static int? Intero(string? valore) =>
        int.TryParse(valore, NumberStyles.AllowLeadingSign, Inv, out var n) ? n : null;

    private static VistaMappa? LeggiVista(string? centro, string? zoom)
    {
        var parti = centro?.Split(',');
        if (parti is not { Length: 2 }
            || !Numero(parti[0], out var lon) || !Numero(parti[1], out var lat) || !Numero(zoom, out var z)
            || lon is < -180 or > 180 || lat is < -90 or > 90 || z is < 0 or > 24)
        {
            return null; // inquadratura assente o non valida: si inquadrano i vigneti dell'azienda
        }

        return new VistaMappa(lon, lat, z);
    }

    private static bool Numero(string? valore, out double numero) =>
        double.TryParse(valore, NumberStyles.Float, Inv, out numero) && double.IsFinite(numero);
}
