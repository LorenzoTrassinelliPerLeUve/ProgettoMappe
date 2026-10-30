using System.Globalization;
using System.Text.Json.Nodes;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Api.Filari;

/// <summary>
/// Proposta di filari dai dati di 4Grapes (task #146): primo filare per il centro del vigneto con
/// l'orientamento di Vigneto.Orientamento e interfila da Vigneto.TraFila. Il resto (filari paralleli,
/// ritagli) lo fa <see cref="GeneratoreFilari"/>; l'utente poi corregge nell'editor.
/// </summary>
public sealed record PropostaSesto(
    (double Lon, double Lat) Da,
    (double Lon, double Lat) A,
    double InterfilaM,
    double AzimutGradi,
    bool OrientamentoStimato,
    int? FilariIn4Grapes,
    double? SullaFilaM,
    IReadOnlyList<string> Note);

public static class SestoImpianto
{
    /// <summary>
    /// Interfila in metri da Vigneto.TraFila (dba, task #96: centimetri, ma 125 valori sotto 10 sono in metri,
    /// 58 fra 10 e 100 e 9 sopra 500 sono sbagliati). Valori in cm fra 100 e 500 → / 100; fra 0,5 e 10 → già in
    /// metri; il resto (null, ≤ 0, 10–100, oltre 500) → null con il motivo.
    /// </summary>
    public static (double? Metri, string? Nota) Interfila(decimal? traFila)
    {
        if (traFila is not { } valore || valore <= 0)
        {
            return (null, "In 4Grapes manca l'interfila (TraFila).");
        }

        if (valore is >= 100 and <= 500)
        {
            return ((double)valore / 100, null);
        }

        if (valore is >= 0.5m and < 10)
        {
            return ((double)valore, $"Interfila scritta in metri in 4Grapes ({Numero(valore)}).");
        }

        return (null, $"L'interfila di 4Grapes ({Numero(valore)}) non è credibile.");
    }

    /// <summary>Distanza fra le piante sulla fila in metri (Vigneto.SullaFila, cm, plausibile 30–400); altrimenti null.</summary>
    public static double? SullaFila(decimal? sullaFila) =>
        sullaFila is >= 30 and <= 400 ? (double)sullaFila.Value / 100 : null;

    /// <summary>Orientamento di Vigneto.Orientamento (gradi da nord, 0–180; 180 = 0); null se manca o è fuori intervallo.</summary>
    public static double? Orientamento(int? gradi) => gradi is >= 0 and <= 180 ? gradi.Value % 180 : null;

    /// <summary>
    /// La proposta per il vigneto. Senza orientamento in 4Grapes si usa il lato lungo del rettangolo più stretto
    /// che contiene il vigneto (i filari corrono quasi sempre lungo il vigneto), segnalato come stimato.
    /// </summary>
    /// <exception cref="ArgumentException">Senza poligono o senza un'interfila credibile, con il motivo.</exception>
    public static PropostaSesto Proponi(Vigneto vigneto)
    {
        if (vigneto.SoloPunto || ShapefileVigneti.Poligoni(Nodo(vigneto.GeometriaGeoJson)) is not { } poligoni)
        {
            throw new ArgumentException("Il vigneto non ha un poligono.");
        }

        var (interfila, notaInterfila) = Interfila(vigneto.TraFilaCm);
        if (interfila is not { } interfilaM)
        {
            throw new ArgumentException(notaInterfila + " Traccia tu il primo filare.");
        }

        var note = new List<string>();
        if (notaInterfila is not null)
        {
            note.Add(notaInterfila);
        }

        var centro = poligoni.EnvelopeInternal.Centre;
        var utm = ProiezioneUtm.Per(centro.X, centro.Y);
        var area = GeneratoreFilari.Valido(GeneratoreFilari.Proietta(poligoni, utm));

        var azimut = Orientamento(vigneto.OrientamentoGradi);
        var stimato = azimut is null;
        if (azimut is null)
        {
            azimut = LatoLungo(area);
            note.Add("Orientamento non indicato in 4Grapes: stimato dalla forma del vigneto.");
        }

        var radianti = azimut.Value * Math.PI / 180;
        var (dx, dy) = (Math.Sin(radianti), Math.Cos(radianti));
        var origine = area.Centroid.Coordinate;
        double sMin = double.MaxValue, sMax = double.MinValue;
        foreach (var c in area.Coordinates)
        {
            var s = (c.X - origine.X) * dx + (c.Y - origine.Y) * dy;
            (sMin, sMax) = (Math.Min(sMin, s), Math.Max(sMax, s));
        }

        if (sMax - sMin < 1)
        {
            throw new ArgumentException("Il vigneto è troppo piccolo per tracciare filari.");
        }

        var da = utm.Indietro(origine.X + dx * sMin, origine.Y + dy * sMin);
        var a = utm.Indietro(origine.X + dx * sMax, origine.Y + dy * sMax);
        return new PropostaSesto(da, a, Math.Round(interfilaM, 2), Math.Round(azimut.Value, 1), stimato,
            vigneto.FilariDichiarati is > 0 ? vigneto.FilariDichiarati : null, SullaFila(vigneto.SullaFilaCm), note);
    }

    /// <summary>Direzione (gradi da nord, 0–180) del lato lungo del rettangolo minimo che contiene l'area (in metri).</summary>
    public static double LatoLungo(Geometry area)
    {
        var rettangolo = MinimumDiameter.GetMinimumRectangle(area);
        var c = rettangolo.Coordinates;
        if (c.Length < 3)
        {
            return 0;
        }

        var (p, q) = c[0].Distance(c[1]) >= c[1].Distance(c[2]) ? (c[0], c[1]) : (c[1], c[2]);
        var l = p.Distance(q);
        return l > 0 ? GeneratoreFilari.Azimut((q.X - p.X) / l, (q.Y - p.Y) / l) : 0;
    }

    private static JsonNode? Nodo(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(geoJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string Numero(decimal valore) => valore.ToString("0.##", CultureInfo.GetCultureInfo("it-IT"));
}
