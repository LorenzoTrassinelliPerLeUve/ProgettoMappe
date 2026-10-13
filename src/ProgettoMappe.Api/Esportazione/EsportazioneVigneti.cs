using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Esportazione;

public enum FormatoEsportazione
{
    GeoJson,
    Kml,
    Shapefile,
}

/// <summary>
/// Un vigneto da esportare, con il nome dell'azienda e, per l'anno scelto (se c'è), indice BIGOT,
/// rischi, vigoria e fenologia; il potenziale idrico e l'ultimo rilievo non hanno anno e ci sono sempre.
/// <see cref="GiorniDalRilievo"/>: giorni fra l'ultimo rilievo e il giorno dell'esportazione; così
/// <see cref="GiorniDalTrattamento"/> per l'ultimo trattamento (anche lui, con la linea di difesa, senza anno).
/// <see cref="Ndvi"/>: l'ultimo passaggio pulito di Sentinel-2 con un valore (task #84), senza anno.
/// </summary>
public sealed record VignetoEsportato(Vigneto Vigneto, string Azienda, int? Anno, IndiceBigotVigneto? Indice,
    RischioVigneto? Rischio = null, VigoriaVigneto? Vigoria = null, PotenzialeIdricoVigneto? PotenzialeIdrico = null,
    ConfrontoEsportatoDto? Confronto = null, VariazioneEsportataDto? Variazione = null, FenologiaVigneto? Fenologia = null,
    UltimoRilievo? Rilievo = null, int? GiorniDalRilievo = null,
    UltimoTrattamento? Trattamento = null, int? GiorniDalTrattamento = null, LineaDifesaVigneto? Linea = null,
    PuntoNdvi? Ndvi = null);

public enum TipoCampo
{
    Intero,
    Decimale,
    Testo,
}

/// <summary>Un sottovigneto da esportare (task #66), con i nomi di azienda e vigneto.</summary>
public sealed record SottovignetoEsportato(Sottovigneto Sottovigneto, string Azienda, string? Vigneto);

/// <summary>Attributo esportato: stesso nome in tutti i formati (max 10 caratteri, il limite del .dbf).</summary>
public sealed record CampoEsportato<T>(string Nome, TipoCampo Tipo, Func<T, object?> Valore, int Decimali = 0);

/// <summary>
/// Esportazione dei vigneti in GeoJSON, KML e shapefile (tutti WGS84, lon/lat in gradi), al posto
/// dei file preparati a mano in QGIS. Si esportano solo i vigneti con una geometria leggibile.
/// Gli attributi (<see cref="Campi"/>) sono gli stessi nei tre formati. I vigneti senza poligono
/// (<see cref="Vigneto.SoloPunto"/>) sono uno strato «punti» a parte: cartella in KML, secondo
/// shapefile nello zip; in GeoJSON stanno nella stessa collezione (QGIS la apre in due strati per
/// tipo di geometria) e si riconoscono dal campo «geometria». Così anche i sottovigneti (task #66),
/// con i loro attributi (<see cref="CampiSottovigneti"/>): cartella, shapefile «-sottovigneti».
/// </summary>
public static class EsportazioneVigneti
{
    public static readonly IReadOnlyList<CampoEsportato<VignetoEsportato>> Campi =
    [
        new("id", TipoCampo.Intero, v => v.Vigneto.Id),
        new("azienda", TipoCampo.Testo, v => v.Azienda),
        new("vigna", TipoCampo.Testo, v => v.Vigneto.NomeVigna),
        new("vigneto", TipoCampo.Testo, v => v.Vigneto.Nome),
        new("vitigno", TipoCampo.Testo, v => v.Vigneto.Varieta),
        new("sup_ha", TipoCampo.Decimale, v => v.Vigneto.SuperficieEttari, 4),
        // «poligono», o «punto» se in 4Grapes c'è solo Latitudine/Longitudine (strato a parte in KML e shapefile).
        new("geometria", TipoCampo.Testo, v => v.Vigneto.SoloPunto ? "punto" : "poligono"),
        new("anno_bigot", TipoCampo.Intero, v => v.Anno),
        new("num_param", TipoCampo.Intero, v => v.Indice?.NumParametri),
        new("punteggio", TipoCampo.Intero, v => v.Indice?.Punteggio),
        new("peso", TipoCampo.Intero, v => v.Indice?.Peso),
        new("vigoria", TipoCampo.Testo, v => v.Indice?.Vigoria),
        new("sfe", TipoCampo.Decimale, v => v.Indice?.Sfe, 2),
        new("produzione", TipoCampo.Decimale, v => v.Indice?.Produzione, 2),
        new("sfe_kg", TipoCampo.Decimale, v => v.Indice?.SfeKg, 2),
        new("eta", TipoCampo.Intero, v => v.Indice?.EtaVigneto),
        new("morfologia", TipoCampo.Testo, v => v.Indice?.Morfologia),
        new("ampelopat", TipoCampo.Decimale, v => v.Indice?.Ampelopatie, 2),
        new("regime_idr", TipoCampo.Testo, v => v.Indice?.RegimeIdrico),
        new("biodivers", TipoCampo.Decimale, v => v.Indice?.Biodiversita, 2),
        // Statistiche 4Grapes dello stesso anno (anno_bigot), il potenziale idrico senza anno.
        new("ris_oidio", TipoCampo.Decimale, v => v.Rischio?.Oidio, 2),
        new("ris_perono", TipoCampo.Decimale, v => v.Rischio?.Peronospora, 2),
        new("vigoria_pt", TipoCampo.Decimale, v => v.Vigoria?.Punteggio, 2),
        new("pot_idrico", TipoCampo.Decimale, v => v.PotenzialeIdrico?.PotenzialeIdrico, 2),
        // Fenologia dello stesso anno: fase BBCH dell'ultimo rilievo, il suo nome e il giorno (AAAA-MM-GG).
        new("bbch", TipoCampo.Intero, v => v.Fenologia?.Bbch),
        new("fase_bbch", TipoCampo.Testo, v => v.Fenologia?.Descrizione),
        new("data_bbch", TipoCampo.Testo, v => v.Fenologia?.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        // Ultimo rilievo di qualunque tipo (senza anno): giorno, giorni fino all'esportazione, tipo,
        // autore («M. Broggi») e foto (1/0); tutti vuoti se il vigneto non è mai stato rilevato.
        new("ril_data", TipoCampo.Testo, v => v.Rilievo?.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        new("ril_giorni", TipoCampo.Intero, v => v.Rilievo is null ? null : v.GiorniDalRilievo),
        new("ril_tipo", TipoCampo.Testo, v => v.Rilievo?.Tipo),
        new("ril_autore", TipoCampo.Testo, v => v.Rilievo?.Autore),
        new("ril_foto", TipoCampo.Intero, v => v.Rilievo is null ? null : v.Rilievo.ConFoto ? 1 : 0),
        // Ultimo trattamento fatto (senza anno): giorno, giorni fino all'esportazione, principi attivi,
        // prodotti e avversità (separati da «; », come in 4Grapes); linea di difesa e classe di rischio
        // («ALTO / BASSO» = peronospora, oidio). Vuoti se il vigneto non ne ha.
        new("tratt_data", TipoCampo.Testo, v => v.Trattamento?.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        new("tratt_gg", TipoCampo.Intero, v => v.Trattamento is null ? null : v.GiorniDalTrattamento),
        new("tratt_pa", TipoCampo.Testo, v => Elenco(v.Trattamento?.PrincipiAttivi)),
        new("tratt_prod", TipoCampo.Testo, v => Elenco(v.Trattamento?.Prodotti)),
        new("tratt_avv", TipoCampo.Testo, v => Elenco(v.Trattamento?.Avversita)),
        new("tratt_lin", TipoCampo.Testo, v => v.Linea?.IdLinea is null ? null : v.Linea.Linea),
        new("tratt_risc", TipoCampo.Testo, v => v.Linea?.Rischio),
        // NDVI da satellite (senza anno): media dell'ultimo passaggio pulito e il suo giorno.
        new("ndvi", TipoCampo.Decimale, v => v.Ndvi?.Media, 3),
        new("ndvi_data", TipoCampo.Testo, v => v.Ndvi?.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        // Confronto fra due anni (se acceso nella mappa): parametro, anno di confronto, variazione e classe.
        new("conf_param", TipoCampo.Testo, v => v.Confronto?.Parametro),
        new("anno_da", TipoCampo.Intero, v => v.Confronto?.AnnoDa),
        new("variaz", TipoCampo.Decimale, v => v.Variazione?.Variazione, 2),
        new("var_classe", TipoCampo.Testo, v => v.Variazione?.Classe),
    ];

    /// <summary>
    /// Attributi dello strato dei sottovigneti (task #66): niente tematismi, i dati di 4Grapes sono
    /// per vigneto. «geometria» = «sottovigneto» li distingue nel GeoJSON, dove stanno insieme ai vigneti.
    /// </summary>
    public static readonly IReadOnlyList<CampoEsportato<SottovignetoEsportato>> CampiSottovigneti =
    [
        new("id", TipoCampo.Intero, s => s.Sottovigneto.Id),
        new("id_vigneto", TipoCampo.Intero, s => s.Sottovigneto.IdVigneto),
        new("azienda", TipoCampo.Testo, s => s.Azienda),
        new("vigneto", TipoCampo.Testo, s => s.Vigneto),
        new("sottovign", TipoCampo.Testo, s => s.Sottovigneto.Nome),
        new("sup_ha", TipoCampo.Decimale, s => s.Sottovigneto.SuperficieEttari, 4),
        new("geometria", TipoCampo.Testo, _ => "sottovigneto"),
    ];

    /// <summary>Più valori in un campo solo: «RAME; ZOLFO»; null se non ce ne sono.</summary>
    private static string? Elenco(IReadOnlyList<string>? valori) => valori is { Count: > 0 } ? string.Join("; ", valori) : null;

    /// <summary>"geojson", "kml", "shp" (o "shapefile"), senza distinzione di maiuscole.</summary>
    public static bool TryFormato(string? testo, out FormatoEsportazione formato)
    {
        switch (testo?.Trim().ToLowerInvariant())
        {
            case "geojson":
                formato = FormatoEsportazione.GeoJson;
                return true;
            case "kml":
                formato = FormatoEsportazione.Kml;
                return true;
            case "shp" or "shapefile":
                formato = FormatoEsportazione.Shapefile;
                return true;
            default:
                formato = default;
                return false;
        }
    }

    public static string TipoContenuto(FormatoEsportazione formato) => formato switch
    {
        FormatoEsportazione.GeoJson => "application/geo+json",
        FormatoEsportazione.Kml => "application/vnd.google-earth.kml+xml",
        _ => "application/zip",
    };

    /// <summary>Nome del file senza estensione: azienda-AAAAMMGG (azienda in minuscolo, senza accenti né spazi).</summary>
    public static string NomeBase(string azienda, int aziendaId, DateOnly data)
    {
        var testo = new StringBuilder();
        foreach (var c in azienda.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                testo.Append(char.ToLowerInvariant(c));
            }
            else if (testo.Length > 0 && testo[^1] != '-')
            {
                testo.Append('-');
            }
        }

        var nome = testo.ToString().Trim('-');
        if (nome.Length > 60)
        {
            nome = nome[..60].TrimEnd('-');
        }

        if (nome.Length == 0)
        {
            nome = $"azienda-{aziendaId}";
        }

        return $"{nome}-{data:yyyyMMdd}";
    }

    public static string NomeFile(string nomeBase, FormatoEsportazione formato) => formato switch
    {
        FormatoEsportazione.GeoJson => nomeBase + ".geojson",
        FormatoEsportazione.Kml => nomeBase + ".kml",
        _ => nomeBase + ".zip",
    };

    public static byte[] Scrivi(FormatoEsportazione formato, string nomeBase, IReadOnlyList<VignetoEsportato> vigneti,
        IReadOnlyList<SottovignetoEsportato>? sottovigneti = null) => formato switch
    {
        FormatoEsportazione.GeoJson => GeoJson(vigneti, sottovigneti),
        FormatoEsportazione.Kml => KmlVigneti.Scrivi(nomeBase, vigneti, sottovigneti),
        _ => ShapefileVigneti.Scrivi(nomeBase, vigneti, sottovigneti),
    };

    /// <summary>
    /// FeatureCollection (RFC 7946, WGS84): una Feature per vigneto con geometria (id = IdVigneto),
    /// poi una per sottovigneto (senza id, per non confonderlo con un vigneto; campo «geometria» = «sottovigneto»).
    /// </summary>
    public static byte[] GeoJson(IReadOnlyList<VignetoEsportato> vigneti, IReadOnlyList<SottovignetoEsportato>? sottovigneti = null)
    {
        var features = new JsonArray();
        foreach (var v in vigneti)
        {
            if (Geometria(v.Vigneto) is not { } geometria)
            {
                continue;
            }

            features.Add(new JsonObject
            {
                ["type"] = "Feature",
                ["id"] = v.Vigneto.Id,
                ["properties"] = Proprieta(Campi, v),
                ["geometry"] = geometria,
            });
        }

        foreach (var s in sottovigneti ?? [])
        {
            if (Geometria(s.Sottovigneto) is not { } geometria)
            {
                continue;
            }

            features.Add(new JsonObject
            {
                ["type"] = "Feature",
                ["properties"] = Proprieta(CampiSottovigneti, s),
                ["geometry"] = geometria,
            });
        }

        var collezione = new JsonObject { ["type"] = "FeatureCollection", ["features"] = features };
        return JsonSerializer.SerializeToUtf8Bytes(collezione);
    }

    private static JsonObject Proprieta<T>(IReadOnlyList<CampoEsportato<T>> campi, T elemento)
    {
        var proprieta = new JsonObject();
        foreach (var campo in campi)
        {
            proprieta[campo.Nome] = campo.Valore(elemento) switch
            {
                null => null,
                string s => JsonValue.Create(s),
                int i => JsonValue.Create(i),
                decimal d => JsonValue.Create(d),
                var altro => JsonValue.Create(Convert.ToString(altro, CultureInfo.InvariantCulture)),
            };
        }
        return proprieta;
    }

    /// <summary>La geometria GeoJSON del vigneto, o null se manca o non si legge.</summary>
    public static JsonNode? Geometria(Vigneto vigneto) => Geometria(vigneto.GeometriaGeoJson);

    /// <summary>La geometria GeoJSON del sottovigneto, o null se manca o non si legge.</summary>
    public static JsonNode? Geometria(Sottovigneto sottovigneto) => Geometria(sottovigneto.GeometriaGeoJson);

    private static JsonNode? Geometria(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(geoJson) is JsonObject { } oggetto && oggetto["type"] is not null ? oggetto : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Valore come testo per KML e .dbf: numeri con il punto, null = vuoto.</summary>
    public static string? Testo(object? valore) => valore switch
    {
        null => null,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valore.ToString(),
    };
}
