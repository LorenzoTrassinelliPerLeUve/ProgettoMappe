namespace ProgettoMappe.BilancioIdrico;

/// <summary>Il centro di un vigneto attivo (gradi WGS84) e la sua azienda.</summary>
public sealed record PosizioneVigneto(int IdVigneto, int IdEnte, double Latitudine, double Longitudine);

/// <summary>
/// Task #383 (proprietario, 27/11/2026: «quelli senza meteo devono usare i dati meteo del vigneto più vicino»). Nel bilancio
/// il meteo di un vigneto è quello della sua azienda (media dei suoi vigneti con meteo): un vigneto è senza meteo nei giorni
/// in cui la sua azienda non ne ha. In quei giorni prende il meteo del vigneto attivo più vicino, di un'altra azienda, che
/// quel giorno ce l'ha (cioè il meteo dell'azienda di quel vigneto), entro <see cref="Parametri.RaggioMeteoVicinoKm"/>.
/// Si sceglie giorno per giorno: un buco di pochi giorni si riempie da solo. Calcolo puro.
/// </summary>
public static class RipiegoMeteo
{
    private const double RaggioTerraKm = 6371.0088;

    /// <summary>Distanza sulla sfera (formula dell'emisenoverso), in km.</summary>
    public static double DistanzaKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * RaggioTerraKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>
    /// Il meteo dell'azienda c'è nel giorno, con la stessa regola del motore (<see cref="ScelteDelGiorno.Meteo(DateOnly)"/>).
    /// </summary>
    public static bool Presente(MeteoGiorno? m, Parametri p) =>
        m is not null && (p.MeteoVuotoComeZero || (m.Et0Mm is not null && m.PioggiaMm is not null));

    /// <summary>Per ogni vigneto le posizioni dei vigneti di altre aziende entro il raggio, dal più vicino.</summary>
    public static Dictionary<int, List<(PosizioneVigneto Vicino, double Km)>> Vicini(IEnumerable<int> vigneti,
        IReadOnlyDictionary<int, PosizioneVigneto> posizioni, double raggioKm)
    {
        var tutti = posizioni.Values.ToList();
        var risultato = new Dictionary<int, List<(PosizioneVigneto, double)>>();
        foreach (var id in vigneti.Distinct())
        {
            if (!posizioni.TryGetValue(id, out var v))
                continue;
            // Un grado di latitudine ≈ 111 km: un primo taglio veloce prima della distanza vera.
            var gradi = raggioKm / 111.0 * 1.5;
            var gradiLon = gradi / Math.Max(0.2, Math.Cos(v.Latitudine * Math.PI / 180));
            risultato[id] = tutti
                .Where(x => x.IdEnte != v.IdEnte && Math.Abs(x.Latitudine - v.Latitudine) <= gradi && Math.Abs(x.Longitudine - v.Longitudine) <= gradiLon)
                .Select(x => (x, DistanzaKm(v.Latitudine, v.Longitudine, x.Latitudine, x.Longitudine)))
                .Where(x => x.Item2 <= raggioKm)
                .OrderBy(x => x.Item2).ThenBy(x => x.Item1.IdVigneto)
                .ToList();
        }
        return risultato;
    }

    /// <summary>I giorni da <paramref name="da"/> ad <paramref name="a"/> senza meteo dell'azienda.</summary>
    public static List<DateOnly> GiorniSenzaMeteo(IReadOnlyList<MeteoGiorno> meteoAzienda, DateOnly da, DateOnly a, Parametri p)
    {
        var perGiorno = meteoAzienda.GroupBy(m => m.Data).ToDictionary(g => g.Key, g => g.Last());
        return Enumerable.Range(0, Math.Max(0, a.DayNumber - da.DayNumber + 1)).Select(da.AddDays)
            .Where(d => !Presente(perGiorno.GetValueOrDefault(d), p)).ToList();
    }

    /// <summary>
    /// Il meteo di ripiego dei <paramref name="vigneti"/> nei giorni senza meteo dell'azienda: per ogni giorno il vicino più
    /// vicino la cui azienda quel giorno ha il meteo (<paramref name="meteoPerEnte"/>). Vigneti senza posizione o senza
    /// vicini con meteo restano senza.
    /// </summary>
    public static List<MeteoVicinoGiorno> Calcola(IEnumerable<int> vigneti, IReadOnlyList<MeteoGiorno> meteoAzienda, DateOnly da, DateOnly a,
        IReadOnlyDictionary<int, PosizioneVigneto> posizioni, IReadOnlyDictionary<int, IReadOnlyList<MeteoGiorno>> meteoPerEnte, Parametri p)
    {
        if (p.RaggioMeteoVicinoKm is not > 0)
            return [];
        var giorni = GiorniSenzaMeteo(meteoAzienda, da, a, p);
        if (giorni.Count == 0)
            return [];
        var meteoEnti = meteoPerEnte.ToDictionary(kv => kv.Key, kv => kv.Value.GroupBy(m => m.Data).ToDictionary(g => g.Key, g => g.Last()));
        var righe = new List<MeteoVicinoGiorno>();
        foreach (var (id, vicini) in Vicini(vigneti, posizioni, p.RaggioMeteoVicinoKm.Value).OrderBy(kv => kv.Key))
            foreach (var giorno in giorni)
                foreach (var (vicino, km) in vicini)
                    if (meteoEnti.TryGetValue(vicino.IdEnte, out var m) && m.GetValueOrDefault(giorno) is { } meteo && Presente(meteo, p))
                    {
                        righe.Add(new MeteoVicinoGiorno(id, meteo, vicino.IdVigneto, Math.Round(km, 2)));
                        break;
                    }
        return righe;
    }

    /// <summary>Le aziende dei vicini entro il raggio: quelle di cui leggere il meteo.</summary>
    public static HashSet<int> AziendeVicine(IEnumerable<int> vigneti, IReadOnlyDictionary<int, PosizioneVigneto> posizioni, double raggioKm) =>
        Vicini(vigneti, posizioni, raggioKm).Values.SelectMany(l => l.Select(x => x.Vicino.IdEnte)).ToHashSet();
}
