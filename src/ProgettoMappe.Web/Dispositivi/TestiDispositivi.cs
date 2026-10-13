using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Dispositivi;

/// <summary>
/// Un segnaposto per mappa.js: <see cref="Tipo"/> sceglie l'icona («stazione», «sensore», «trappole»),
/// <see cref="Colore"/> lo sfondo («verde», «giallo», «rosso», «grigio»); <see cref="Righe"/> sono le
/// righe del popup sotto il <see cref="Titolo"/>, già scritte (il JS le mette come testo).
/// </summary>
public sealed record SegnapostoDispositivo(string Id, string Tipo, double Lon, double Lat, string Colore, string Titolo, IReadOnlyList<string> Righe);

/// <summary>
/// Testi dei dispositivi (task #69): popup dei segnaposti, legenda nei Filtri e puntino della pagina
/// iniziale. Il colore lo decide l'Api (<see cref="StatoDispositivo"/>), qui solo le parole.
/// </summary>
public static class TestiDispositivi
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Le voci della legenda nei Filtri, nell'ordine dei colori.</summary>
    public static readonly IReadOnlyList<(string Colore, string Testo)> Legenda =
    [
        ("verde", "dato di oggi o di ieri"),
        ("giallo", "fino a 10 giorni"),
        ("rosso", "oltre 10 giorni (stazione ferma)"),
        ("grigio", "nessun dato"),
    ];

    public static string Fermi(int quanti) =>
        quanti == 1 ? "1 stazione meteo ferma da oltre 10 giorni" : $"{quanti} stazioni meteo ferme da oltre 10 giorni";

    public static string Colore(StatoDispositivo stato) => stato switch
    {
        StatoDispositivo.Attivo => "verde",
        StatoDispositivo.InRitardo => "giallo",
        StatoDispositivo.Fermo => "rosso",
        _ => "grigio",
    };

    /// <summary>«Stazione meteo», «Sensore: Umidità suolo», «3 trappole».</summary>
    public static string Tipo(DispositivoDto d) => d.Tipo switch
    {
        TipoDispositivo.Stazione => "Stazione meteo",
        TipoDispositivo.Sensore => d.Sensore is { } sensore ? $"Sensore: {sensore}" : "Sensore",
        _ => d.Trappole == 1 ? "1 trappola" : $"{d.Trappole ?? 0} trappole",
    };

    /// <summary>«oggi», «ieri», «5 giorni fa».</summary>
    public static string DaQuanto(int giorni) => giorni switch
    {
        <= 0 => "oggi",
        1 => "ieri",
        _ => $"{giorni} giorni fa",
    };

    /// <summary>«12/10», con l'anno se non è quello di oggi.</summary>
    public static string Giorno(DateTime data, DateOnly oggi) =>
        data.ToString(data.Year == oggi.Year ? "dd/MM" : "dd/MM/yyyy", Italiano);

    /// <summary>
    /// La riga dell'ultimo dato: «Ultimo dato: 12/10 (ieri)» per le stazioni, con «ferma» oltre i 10
    /// giorni; «Ultimo controllo: …» per le trappole (ultimo rilievo di monitoraggio), oltre i 10 giorni
    /// «Nessun monitoraggio da 23 giorni (ultimo 20/09)»: fuori stagione è normale, non è un guasto.
    /// </summary>
    public static string UltimoDato(DispositivoDto d, DateOnly oggi)
    {
        if (d.Tipo == TipoDispositivo.Sensore || d.Stato == StatoDispositivo.NonDisponibile)
        {
            return "Ultimo dato non disponibile in 4Grapes";
        }

        var trappole = d.Tipo == TipoDispositivo.Trappole;
        if (d.UltimoDato is not { } data)
        {
            return trappole ? "Nessun controllo registrato" : "Nessun dato misurato";
        }

        if (trappole && d.Stato == StatoDispositivo.Fermo)
        {
            return $"Nessun monitoraggio da {d.Giorni} giorni (ultimo {Giorno(data, oggi)})";
        }

        var testo = $"{(trappole ? "Ultimo controllo" : "Ultimo dato")}: {Giorno(data, oggi)} ({DaQuanto(d.Giorni ?? 0)})";
        return d.Stato == StatoDispositivo.Fermo ? $"{testo} · ferma" : testo;
    }

    /// <summary>Le righe del popup: tipo, ultimo dato e, per i dispositivi, stato e registrazione di 4Grapes.</summary>
    public static IReadOnlyList<string> Righe(DispositivoDto d, DateOnly oggi)
    {
        var righe = new List<string> { Tipo(d), UltimoDato(d, oggi) };
        if (d.Tipo == TipoDispositivo.Sensore)
        {
            if (d.StatoFourGrapes is { } stato)
            {
                righe.Add($"Stato in 4Grapes: {stato}");
            }
            if (d.Registrato is { } registrato)
            {
                righe.Add($"Registrato il {registrato.ToString("dd/MM/yyyy", Italiano)}");
            }
        }

        return righe;
    }

    /// <summary>I segnaposti per la mappa: solo i dispositivi con una posizione.</summary>
    public static IReadOnlyList<SegnapostoDispositivo> Segnaposti(IEnumerable<DispositivoDto> dispositivi, DateOnly oggi) =>
        dispositivi
            .Where(d => d is { Longitudine: not null, Latitudine: not null })
            .Select(d => new SegnapostoDispositivo(d.Id, d.Tipo.ToString().ToLowerInvariant(), d.Longitudine!.Value, d.Latitudine!.Value,
                Colore(d.Stato), d.Nome, Righe(d, oggi)))
            .ToList();
}
