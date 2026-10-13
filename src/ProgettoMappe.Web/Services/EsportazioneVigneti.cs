using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Tematismi;

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

    /// <summary>
    /// Link relativo per il browser; <paramref name="ids"/> null = tutti i vigneti dell'azienda.
    /// Con <paramref name="da"/> e <paramref name="tema"/> il file ha anche la variazione del confronto.
    /// </summary>
    public static string Link(int aziendaId, string formato, int? anno, IEnumerable<int>? ids, int? da = null, string? tema = null)
    {
        var link = $"esporta/{aziendaId}?formato={Uri.EscapeDataString(formato)}";
        if (anno is not null)
        {
            link += $"&anno={anno.Value.ToString(CultureInfo.InvariantCulture)}";
        }
        if (anno is not null && da is not null && tema is not null)
        {
            link += $"&da={da.Value.ToString(CultureInfo.InvariantCulture)}&tema={Uri.EscapeDataString(tema)}";
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

    /// <summary>Confronto chiesto dal link: parametro (con anni), anno di confronto e anno mostrato.</summary>
    public sealed record RichiestaConfronto(Tematismo Tema, int AnnoDa, int Anno);

    /// <summary>
    /// Legge da= e tema= del link. Nessuno dei due → nessun confronto (true, null); un parametro
    /// sconosciuto o senza anni, anni non numerici, uguali o senza anno= → false (richiesta non valida).
    /// </summary>
    public static bool TryRichiestaConfronto(CatalogoTematismi catalogo, string? tema, string? anno, string? da, out RichiestaConfronto? richiesta)
    {
        richiesta = null;
        if (string.IsNullOrEmpty(da) && string.IsNullOrEmpty(tema))
        {
            return true;
        }

        if (catalogo.Trova(tema) is not { SenzaAnno: false } t
            || !int.TryParse(anno, NumberStyles.None, CultureInfo.InvariantCulture, out var a)
            || !int.TryParse(da, NumberStyles.None, CultureInfo.InvariantCulture, out var d)
            || a == d)
        {
            return false;
        }

        richiesta = new RichiestaConfronto(t, d, a);
        return true;
    }

    /// <summary>Le righe della fonte del parametro, se il server le ha già pronte (null altrimenti).</summary>
    public static IReadOnlyList<IRigaVigneto>? Righe(FonteTematismo fonte, IndiciBigotAziendaDto? bigot, StatisticheAziendaDto? statistiche) => fonte switch
    {
        FonteTematismo.Bigot => bigot is { Pronto: true } ? bigot.Indici : null,
        FonteTematismo.Rischi => statistiche is { Pronto: true } ? statistiche.Rischi : null,
        FonteTematismo.Vigoria => statistiche is { Pronto: true } ? statistiche.Vigoria : null,
        _ => null,
    };

    /// <summary>La variazione di ogni vigneto confrontabile, con il nome della sua classe in legenda.</summary>
    public static ConfrontoEsportatoDto Confronto(Tematismo tema, RegolaConfronto regola, IEnumerable<IRigaVigneto> righe, int annoDa, int anno)
    {
        var classi = ConfrontoBigot.Classi(tema, regola);
        var (variazioni, _) = ConfrontoBigot.Variazioni(tema, regola, righe, annoDa, anno);
        return new ConfrontoEsportatoDto(tema.Nome, annoDa, variazioni.Values.OrderBy(v => v.IdVigneto)
            .Select(v => new VariazioneEsportataDto(v.IdVigneto, v.Delta, classi[v.Classe].Etichetta)).ToList());
    }
}
