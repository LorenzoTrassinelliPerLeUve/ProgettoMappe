using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Meteo;

/// <summary>Una barretta della pioggia: <see cref="Altezza"/> in percentuale, 0 per un giorno senza dato.</summary>
public sealed record BarraPioggia(DateOnly Data, string Giorno, decimal? Pioggia, int Altezza, string Titolo);

/// <summary>
/// Testi e barrette del meteo per azienda (task #67): cartellino della pagina iniziale e pannello
/// «Meteo» della mappa. Un'azienda senza dati dice «nessun dato meteo», mai uno zero.
/// </summary>
public static class TestiMeteo
{
    /// <summary>Le barrette arrivano in cima almeno a questa pioggia: una settimana quasi asciutta resta bassa.</summary>
    public const decimal ScalaMinimaMm = 10m;

    public const string NessunDato = "nessun dato meteo";

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>«7 gg: 42 mm · peronospora alta»; senza meteo «nessun dato meteo» (più le infezioni, se ci sono).</summary>
    public static string Cartellino(MeteoAziendaDto meteo)
    {
        var parti = new List<string>
        {
            meteo.Pioggia is { } pioggia
                ? $"7 gg: {Millimetri(pioggia)}{(meteo.GiorniConPioggia < meteo.Giorni.Count ? "*" : "")}"
                : NessunDato,
        };
        parti.AddRange(meteo.Infezioni.Where(i => i.Alta == true).Select(i => $"{i.Nome.ToLower(Italiano)} {Alta(i.Nome)}"));
        return string.Join(" · ", parti);
    }

    /// <summary>Il title del cartellino: quanti giorni hanno il dato, e da dove vengono pioggia e infezioni.</summary>
    public static string TitoloCartellino(MeteoAziendaDto meteo)
    {
        var righe = new List<string>();
        if (meteo.Pioggia is not null)
        {
            righe.Add(meteo.GiorniConPioggia < meteo.Giorni.Count
                ? $"* Pioggia di {meteo.GiorniConPioggia} giorni su {meteo.Giorni.Count}: per gli altri 4Grapes non ha il dato"
                : "Pioggia degli ultimi 7 giorni (oggi escluso), media dei punti meteo dell'azienda");
        }
        else
        {
            righe.Add("4Grapes non ha il meteo dei vigneti di questa azienda negli ultimi 7 giorni");
        }

        righe.AddRange(meteo.Infezioni.Select(Infezione));
        return string.Join("\n", righe);
    }

    /// <summary>
    /// «Peronospora: alta, 34% in 2 vigneti su 40 (soglia 30%)» / «Oidio: bassa, al massimo 12% (soglia 30%)».
    /// Senza soglia, solo il massimo.
    /// </summary>
    public static string Infezione(InfezioneAziendaDto infezione)
    {
        var su = infezione.Vigneti == 1 ? "1 vigneto" : $"{infezione.Vigneti} vigneti";
        var massima = Percentuale(infezione.Massima);
        if (infezione.Soglia is not { } soglia)
        {
            return $"{infezione.Nome}: al massimo {massima} su {su}";
        }

        return infezione.Alta == true
            ? $"{infezione.Nome}: {Alta(infezione.Nome)}, sopra soglia in {infezione.VignetiSopraSoglia} {(infezione.VignetiSopraSoglia == 1 ? "vigneto" : "vigneti")} su {infezione.Vigneti}, al massimo {massima} (soglia {Percentuale(soglia)})"
            : $"{infezione.Nome}: {Bassa(infezione.Nome)}, al massimo {massima} su {su} (soglia {Percentuale(soglia)})";
    }

    /// <summary>
    /// Sette barrette, dal giorno più vecchio a ieri. Altezza in proporzione alla pioggia più alta della
    /// settimana (almeno <see cref="ScalaMinimaMm"/>); un giorno con poca pioggia ha comunque una barretta
    /// visibile, uno senza dato resta vuoto (e lo dice il titolo).
    /// </summary>
    public static IReadOnlyList<BarraPioggia> Barre(MeteoAziendaDto meteo)
    {
        var scala = Math.Max(ScalaMinimaMm, meteo.Giorni.Max(g => g.Pioggia) ?? 0m);
        return meteo.Giorni.Select(g =>
        {
            var giorno = g.Data.ToString("ddd d", Italiano);
            var altezza = g.Pioggia switch
            {
                null or 0m => 0,
                var p => Math.Max(4, (int)Math.Round(p.Value / scala * 100m)),
            };
            var titolo = g.Pioggia is { } p2 ? $"{g.Data.ToString("dddd d MMMM", Italiano)}: {Millimetri(p2)}" : $"{g.Data.ToString("dddd d MMMM", Italiano)}: nessun dato";
            return new BarraPioggia(g.Data, giorno, g.Pioggia, altezza, titolo);
        }).ToList();
    }

    /// <summary>«Ieri: min 8 °C, max 19 °C»; null se ieri non ha temperature.</summary>
    public static string? Temperature(MeteoAziendaDto meteo) => meteo.Ieri switch
    {
        { TemperaturaMin: { } min, TemperaturaMax: { } max } => $"Ieri: min {Gradi(min)}, max {Gradi(max)}",
        { TemperaturaMin: { } min } => $"Ieri: min {Gradi(min)}",
        { TemperaturaMax: { } max } => $"Ieri: max {Gradi(max)}",
        _ => null,
    };

    /// <summary>42 → «42 mm», 3,46 → «3,5 mm», 0,04 → «0 mm».</summary>
    public static string Millimetri(decimal mm) => mm.ToString(mm >= 10 ? "0" : "0.#", Italiano) + " mm";

    public static string Gradi(decimal gradi) => gradi.ToString("0.#", Italiano) + " °C";

    public static string Percentuale(decimal percentuale) => percentuale.ToString("0", Italiano) + "%";

    // «oidio alto», «peronospora alta»: l'aggettivo segue il genere della malattia.
    private static string Alta(string nome) => Maschile(nome) ? "alto" : "alta";

    private static string Bassa(string nome) => Maschile(nome) ? "basso" : "bassa";

    private static bool Maschile(string nome) => nome.EndsWith('o') || nome.EndsWith('O');
}
