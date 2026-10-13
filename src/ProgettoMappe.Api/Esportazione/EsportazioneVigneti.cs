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
/// rischi e vigoria; il potenziale idrico non ha anno e c'è sempre.
/// </summary>
public sealed record VignetoEsportato(Vigneto Vigneto, string Azienda, int? Anno, IndiceBigotVigneto? Indice,
    RischioVigneto? Rischio = null, VigoriaVigneto? Vigoria = null, PotenzialeIdricoVigneto? PotenzialeIdrico = null,
    ConfrontoEsportatoDto? Confronto = null, VariazioneEsportataDto? Variazione = null);

public enum TipoCampo
{
    Intero,
    Decimale,
    Testo,
}

/// <summary>Attributo esportato: stesso nome in tutti i formati (max 10 caratteri, il limite del .dbf).</summary>
public sealed record CampoEsportato(string Nome, TipoCampo Tipo, Func<VignetoEsportato, object?> Valore, int Decimali = 0);

/// <summary>
/// Esportazione dei vigneti in GeoJSON, KML e shapefile (tutti WGS84, lon/lat in gradi), al posto
/// dei file preparati a mano in QGIS. Si esportano solo i vigneti con una geometria leggibile.
/// Gli attributi (<see cref="Campi"/>) sono gli stessi nei tre formati.
/// </summary>
public static class EsportazioneVigneti
{
    public static readonly IReadOnlyList<CampoEsportato> Campi =
    [
        new("id", TipoCampo.Intero, v => v.Vigneto.Id),
        new("azienda", TipoCampo.Testo, v => v.Azienda),
        new("vigna", TipoCampo.Testo, v => v.Vigneto.NomeVigna),
        new("vigneto", TipoCampo.Testo, v => v.Vigneto.Nome),
        new("vitigno", TipoCampo.Testo, v => v.Vigneto.Varieta),
        new("sup_ha", TipoCampo.Decimale, v => v.Vigneto.SuperficieEttari, 4),
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
        // Confronto fra due anni (se acceso nella mappa): parametro, anno di confronto, variazione e classe.
        new("conf_param", TipoCampo.Testo, v => v.Confronto?.Parametro),
        new("anno_da", TipoCampo.Intero, v => v.Confronto?.AnnoDa),
        new("variaz", TipoCampo.Decimale, v => v.Variazione?.Variazione, 2),
        new("var_classe", TipoCampo.Testo, v => v.Variazione?.Classe),
    ];

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

    public static byte[] Scrivi(FormatoEsportazione formato, string nomeBase, IReadOnlyList<VignetoEsportato> vigneti) => formato switch
    {
        FormatoEsportazione.GeoJson => GeoJson(vigneti),
        FormatoEsportazione.Kml => KmlVigneti.Scrivi(nomeBase, vigneti),
        _ => ShapefileVigneti.Scrivi(nomeBase, vigneti),
    };

    /// <summary>FeatureCollection (RFC 7946, WGS84): una Feature per vigneto con geometria, id = IdVigneto.</summary>
    public static byte[] GeoJson(IReadOnlyList<VignetoEsportato> vigneti)
    {
        var features = new JsonArray();
        foreach (var v in vigneti)
        {
            if (Geometria(v.Vigneto) is not { } geometria)
            {
                continue;
            }

            var proprieta = new JsonObject();
            foreach (var campo in Campi)
            {
                proprieta[campo.Nome] = campo.Valore(v) switch
                {
                    null => null,
                    string s => JsonValue.Create(s),
                    int i => JsonValue.Create(i),
                    decimal d => JsonValue.Create(d),
                    var altro => JsonValue.Create(Convert.ToString(altro, CultureInfo.InvariantCulture)),
                };
            }

            features.Add(new JsonObject
            {
                ["type"] = "Feature",
                ["id"] = v.Vigneto.Id,
                ["properties"] = proprieta,
                ["geometry"] = geometria,
            });
        }

        var collezione = new JsonObject { ["type"] = "FeatureCollection", ["features"] = features };
        return JsonSerializer.SerializeToUtf8Bytes(collezione);
    }

    /// <summary>La geometria GeoJSON del vigneto, o null se manca o non si legge.</summary>
    public static JsonNode? Geometria(Vigneto vigneto)
    {
        if (string.IsNullOrWhiteSpace(vigneto.GeometriaGeoJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(vigneto.GeometriaGeoJson) is JsonObject { } oggetto && oggetto["type"] is not null ? oggetto : null;
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
