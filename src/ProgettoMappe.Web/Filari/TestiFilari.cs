using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.Filari;

/// <summary>
/// Testi e misure dei filari (task #95) per la scheda e l'editor. Le misure qui servono solo a mostrare
/// numero e lunghezza mentre si ritocca: quelle che si salvano le ricalcola l'Api in UTM.
/// </summary>
public static class TestiFilari
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    private const double RaggioTerraM = 6371008.8;

    /// <summary>Lunghezza di un filare [[lon, lat], [lon, lat]] in metri (approssimazione locale, &lt; 0,1 % su un vigneto).</summary>
    public static double Lunghezza(double[][] filare)
    {
        if (filare is not [{ Length: >= 2 } a, { Length: >= 2 } b])
        {
            return 0;
        }

        var latMedia = (a[1] + b[1]) / 2 * Math.PI / 180;
        var dx = (b[0] - a[0]) * Math.PI / 180 * Math.Cos(latMedia) * RaggioTerraM;
        var dy = (b[1] - a[1]) * Math.PI / 180 * RaggioTerraM;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double LunghezzaTotale(IEnumerable<double[][]> filari) => filari.Sum(Lunghezza);

    /// <summary>«850 m», «3,1 km».</summary>
    public static string Metri(double metri) =>
        metri < 1000
            ? $"{Math.Round(metri).ToString("N0", Italiano)} m"
            : $"{(metri / 1000).ToString("0.0", Italiano)} km";

    /// <summary>«2,5 m».</summary>
    public static string Interfila(double metri) => $"{metri.ToString("0.0#", Italiano)} m";

    /// <summary>«42 filari» / «1 filare».</summary>
    public static string Numero(int n) => n == 1 ? "1 filare" : $"{n.ToString("N0", Italiano)} filari";

    /// <summary>Riga della scheda: «42, interfila 2,5 m, 3,1 km, disegnati da L. Trassinelli il 14/10».</summary>
    public static string Riassunto(FilariVignetoDto filari, TimeZoneInfo fuso) =>
        $"{filari.NumFilari.ToString("N0", Italiano)}, interfila {Interfila(filari.InterfilaM)}, {Metri(filari.LunghezzaM)}, " +
        $"disegnati da {filari.AutoreBreve} il {Data(filari.CreatoIl, fuso)}";

    /// <summary>Riga dello storico: «v3 · 14/10/2026 15:20 · L. Trassinelli · 42 filari, 3,1 km».</summary>
    public static string Versione(FilariVignetoDto filari, TimeZoneInfo fuso) =>
        $"v{filari.Versione} · {DataOra(filari.CreatoIl, fuso)} · {filari.AutoreBreve} · " +
        (filari.NumFilari == 0 ? "nessun filare" : $"{Numero(filari.NumFilari)}, {Metri(filari.LunghezzaM)}");

    /// <summary>«14/10», con l'anno se non è quello corrente.</summary>
    public static string Data(DateTime utc, TimeZoneInfo fuso)
    {
        var locale = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), fuso);
        var oggi = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso);
        return locale.ToString(locale.Year == oggi.Year ? "dd/MM" : "dd/MM/yyyy", Italiano);
    }

    private static string DataOra(DateTime utc, TimeZoneInfo fuso) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), fuso).ToString("dd/MM/yyyy HH:mm", Italiano);
}

/// <summary>
/// Annulla/ripristina per l'editor dei filari: una pila di stati (copie). <see cref="Fai"/> con
/// <c>unisci</c> sostituisce lo stato in cima se anche lui era «unibile» (i cursori che si muovono fanno
/// un solo passo da annullare, non uno per ogni posizione).
/// </summary>
public sealed class StoriaModifiche<T>
{
    private readonly List<(T Stato, bool Unibile)> _passi = [];
    private int _attuale = -1;

    public StoriaModifiche(T iniziale) => Fai(iniziale);

    public T Attuale => _passi[_attuale].Stato;

    public bool PuoAnnullare => _attuale > 0;

    public bool PuoRipristinare => _attuale < _passi.Count - 1;

    public void Fai(T stato, bool unisci = false)
    {
        if (_attuale >= 0 && _attuale < _passi.Count - 1)
        {
            _passi.RemoveRange(_attuale + 1, _passi.Count - _attuale - 1);
        }

        if (unisci && _attuale > 0 && _passi[_attuale].Unibile)
        {
            _passi[_attuale] = (stato, true);
            return;
        }

        _passi.Add((stato, unisci));
        _attuale = _passi.Count - 1;
    }

    public T Annulla()
    {
        if (PuoAnnullare)
        {
            _attuale--;
        }
        return Attuale;
    }

    public T Ripristina()
    {
        if (PuoRipristinare)
        {
            _attuale++;
        }
        return Attuale;
    }
}
