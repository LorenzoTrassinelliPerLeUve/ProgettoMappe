using System.Globalization;

namespace ProgettoMappe.Web.Services;

/// <summary>Un formato offerto dal pulsante «Scarica»: Id = valore di <c>formato</c> per l'Api.</summary>
public sealed record FormatoEsportazione(string Id, string Nome);

/// <summary>File preparato dall'Api, inoltrato così com'è al browser.</summary>
public sealed record FileEsportato(byte[] Contenuto, string TipoContenuto, string NomeFile);

/// <summary>
/// Scaricamento dei vigneti: il browser chiede <c>esporta/{azienda}</c> al Web, che controlla i
/// parametri e li inoltra all'Api con la chiave condivisa (l'Api non è mai esposta).
/// </summary>
public static class EsportazioneVigneti
{
    public static readonly IReadOnlyList<FormatoEsportazione> Formati =
    [
        new("geojson", "GeoJSON"),
        new("kml", "KML (Google Earth)"),
        new("shp", "Shapefile (zip)"),
    ];

    /// <summary>Link relativo per il browser; <paramref name="ids"/> null = tutti i vigneti dell'azienda.</summary>
    public static string Link(int aziendaId, string formato, int? anno, IEnumerable<int>? ids)
    {
        var link = $"esporta/{aziendaId}?formato={Uri.EscapeDataString(formato)}";
        if (anno is not null)
        {
            link += $"&anno={anno.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        if (ids is not null)
        {
            link += "&ids=" + string.Join(",", ids.Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }
        return link;
    }

    /// <summary>
    /// Indirizzo dell'Api ricostruito solo da valori controllati (formato noto, anno e id numerici),
    /// mai copiando la query del browser; null se un parametro non va.
    /// </summary>
    public static string? IndirizzoApi(int aziendaId, string? formato, string? anno, string? ids)
    {
        if (Formati.All(f => f.Id != formato))
        {
            return null;
        }

        var indirizzo = $"api/aziende/{aziendaId}/vigneti/esporta?formato={formato}";
        if (!string.IsNullOrEmpty(anno))
        {
            if (!int.TryParse(anno, NumberStyles.None, CultureInfo.InvariantCulture, out var a))
            {
                return null;
            }
            indirizzo += $"&anno={a.ToString(CultureInfo.InvariantCulture)}";
        }

        if (ids is not null)
        {
            var numeri = new List<int>();
            foreach (var parte in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(parte, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var id))
                {
                    return null;
                }
                numeri.Add(id);
            }
            // ids presente ma vuoto = nessun vigneto filtrato: si inoltra così (file vuoto), non "tutti".
            indirizzo += "&ids=" + (numeri.Count == 0 ? "," : string.Join(",", numeri.Select(i => i.ToString(CultureInfo.InvariantCulture))));
        }

        return indirizzo;
    }
}
