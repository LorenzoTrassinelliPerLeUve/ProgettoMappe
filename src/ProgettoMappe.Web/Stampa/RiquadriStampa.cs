using System.Text.Json;

namespace ProgettoMappe.Web.Stampa;

/// <summary>Rettangolo in gradi WGS84 [W, S, E, N], come il bbox delle Feature dell'Api.</summary>
public sealed record Ingombro(double Ovest, double Sud, double Est, double Nord)
{
    // Gli stessi valori del programma NDVI (ndvi/ndvilib/calcolo.py): i gruppi vengono uguali.
    public const double MetriPerGradoLat = 110_540.0;
    public const double MetriPerGradoLonEquatore = 111_320.0;

    public Ingombro Unione(Ingombro altro) =>
        new(Math.Min(Ovest, altro.Ovest), Math.Min(Sud, altro.Sud), Math.Max(Est, altro.Est), Math.Max(Nord, altro.Nord));

    /// <summary>Distanza in metri fra due rettangoli (0 se si toccano o si sovrappongono), come <c>distanza_m</c> dell'NDVI.</summary>
    public double DistanzaMetri(Ingombro altro)
    {
        var dx = Math.Max(0, Math.Max(altro.Ovest - Est, Ovest - altro.Est));
        var dy = Math.Max(0, Math.Max(altro.Sud - Nord, Sud - altro.Nord));
        var lat = (Sud + Nord + altro.Sud + altro.Nord) / 4 * Math.PI / 180;
        return Math.Sqrt(Math.Pow(dx * MetriPerGradoLonEquatore * Math.Cos(lat), 2) + Math.Pow(dy * MetriPerGradoLat, 2));
    }

    public double LarghezzaMetri => (Est - Ovest) * MetriPerGradoLonEquatore * Math.Cos((Sud + Nord) / 2 * Math.PI / 180);

    public double AltezzaMetri => (Nord - Sud) * MetriPerGradoLat;

    /// <summary>Larghezza / altezza sul terreno; un vigneto solo punto o una striscia non danno rapporti estremi.</summary>
    public double Aspetto => Math.Clamp(Math.Max(LarghezzaMetri, 50) / Math.Max(AltezzaMetri, 50), 0.2, 5);

    public double[] ComeArray() => [Ovest, Sud, Est, Nord];
}

/// <summary>Un vigneto sulla mappa (con geometria) e il suo ingombro.</summary>
public sealed record VignetoInStampa(int Id, Ingombro Ingombro);

/// <summary>
/// Un riquadro del foglio: i vigneti di un gruppo (o di più gruppi uniti), con l'ingombro complessivo.
/// <see cref="Chiave"/> è il primo gruppo che lo compone: resta la stessa finché non cambia la distanza.
/// </summary>
public sealed record RiquadroStampa(int Chiave, string Lettera, IReadOnlyList<int> Vigneti, Ingombro Ingombro);

/// <summary>Righe e colonne dei riquadri sul foglio.</summary>
public sealed record GrigliaStampa(int Colonne, int Righe);

/// <summary>
/// Layout di stampa a riquadri (task #90): i vigneti vicini stanno nello stesso riquadro, così gli
/// spazi vuoti fra i gruppi lontani non finiscono sul foglio.
/// </summary>
public static class RiquadriStampa
{
    public const double DistanzaPredefinitaKm = 2;
    public const double DistanzaMinimaKm = 0.5;
    public const double DistanzaMassimaKm = 20;

    /// <summary>Riquadri oltre i quali il foglio diventa illeggibile: la pagina lo dice.</summary>
    public const int RiquadriConsigliati = 12;

    /// <summary>
    /// IdVigneto e bbox delle Feature della FeatureCollection dell'Api (Feature senza id o bbox
    /// valido si saltano: non sarebbero comunque sulla mappa).
    /// </summary>
    public static IReadOnlyList<VignetoInStampa> DaGeoJson(JsonElement featureCollection)
    {
        var risultato = new List<VignetoInStampa>();
        if (featureCollection.ValueKind != JsonValueKind.Object
            || !featureCollection.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
        {
            return risultato;
        }

        foreach (var f in features.EnumerateArray())
        {
            if (f.ValueKind == JsonValueKind.Object
                && f.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var idVigneto)
                && f.TryGetProperty("bbox", out var bbox) && bbox.ValueKind == JsonValueKind.Array && bbox.GetArrayLength() == 4
                && bbox.EnumerateArray().All(n => n.ValueKind == JsonValueKind.Number))
            {
                var n = bbox.EnumerateArray().Select(v => v.GetDouble()).ToArray();
                if (n.All(double.IsFinite) && n[0] <= n[2] && n[1] <= n[3])
                {
                    risultato.Add(new VignetoInStampa(idVigneto, new Ingombro(n[0], n[1], n[2], n[3])));
                }
            }
        }

        return risultato;
    }

    /// <summary>
    /// Vigneti raggruppati per vicinanza, con lo stesso criterio dei gruppi NDVI: due vigneti a non più
    /// di <paramref name="distanzaMetri"/> (fra i bbox) stanno nello stesso gruppo, anche attraverso una
    /// catena di vicini. Gruppi dal più numeroso; a parità, da nord-ovest. 0 o meno = un gruppo solo.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<VignetoInStampa>> Raggruppa(IReadOnlyList<VignetoInStampa> vigneti, double distanzaMetri)
    {
        if (vigneti.Count == 0)
        {
            return [];
        }

        var capo = Enumerable.Range(0, vigneti.Count).ToArray();
        int Radice(int i)
        {
            while (capo[i] != i)
            {
                capo[i] = capo[capo[i]];
                i = capo[i];
            }
            return i;
        }

        for (var i = 0; i < vigneti.Count; i++)
        {
            for (var j = i + 1; j < vigneti.Count; j++)
            {
                if ((distanzaMetri <= 0 || vigneti[i].Ingombro.DistanzaMetri(vigneti[j].Ingombro) <= distanzaMetri)
                    && Radice(i) != Radice(j))
                {
                    capo[Radice(i)] = Radice(j);
                }
            }
        }

        return Enumerable.Range(0, vigneti.Count)
            .GroupBy(Radice)
            .Select(g => (IReadOnlyList<VignetoInStampa>)g.Select(i => vigneti[i]).ToList())
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.Max(v => v.Ingombro.Nord))
            .ThenBy(g => g.Min(v => v.Ingombro.Ovest))
            .ToList();
    }

    /// <summary>
    /// I riquadri del foglio: i gruppi con le unioni scelte dall'utente (coppie di chiavi di gruppo,
    /// anche a catena). Lettere A, B, C… nell'ordine dei gruppi; quelli da nascondere li toglie la
    /// pagina, così le lettere degli altri non cambiano.
    /// </summary>
    public static IReadOnlyList<RiquadroStampa> Riquadri(IReadOnlyList<IReadOnlyList<VignetoInStampa>> gruppi,
        IEnumerable<(int A, int B)> unioni)
    {
        // Chiave = indice del gruppo; ogni gruppo punta al primo gruppo del suo riquadro.
        var capo = Enumerable.Range(0, gruppi.Count).ToArray();
        int Radice(int i) => capo[i] == i ? i : capo[i] = Radice(capo[i]);
        foreach (var (a, b) in unioni)
        {
            if (a >= 0 && b >= 0 && a < gruppi.Count && b < gruppi.Count)
            {
                var (ra, rb) = (Radice(a), Radice(b));
                capo[Math.Max(ra, rb)] = Math.Min(ra, rb);
            }
        }

        var riquadri = new List<RiquadroStampa>();
        foreach (var unione in Enumerable.Range(0, gruppi.Count).GroupBy(Radice).OrderBy(g => g.Key))
        {
            var vigneti = unione.SelectMany(i => gruppi[i]).ToList();
            var ingombro = vigneti.Select(v => v.Ingombro).Aggregate((x, y) => x.Unione(y));
            riquadri.Add(new RiquadroStampa(unione.Key, Lettera(riquadri.Count), vigneti.Select(v => v.Id).ToList(), ingombro));
        }

        return riquadri;
    }

    /// <summary>
    /// Numero di ogni vigneto sul foglio (1, 2, 3…): riquadro per riquadro, dentro il riquadro da nord a
    /// sud a fasce di circa un decimo dell'altezza, e in ogni fascia da ovest a est, come si legge.
    /// </summary>
    public static IReadOnlyDictionary<int, int> Numera(IReadOnlyList<RiquadroStampa> riquadri, IReadOnlyList<VignetoInStampa> vigneti)
    {
        var ingombri = vigneti.DistinctBy(v => v.Id).ToDictionary(v => v.Id, v => v.Ingombro);
        var numeri = new Dictionary<int, int>();
        foreach (var riquadro in riquadri)
        {
            var fascia = Math.Max(riquadro.Ingombro.Nord - riquadro.Ingombro.Sud, 1e-9) / 10;
            var ordinati = riquadro.Vigneti.Where(ingombri.ContainsKey)
                .Select(id => (Id: id, X: (ingombri[id].Ovest + ingombri[id].Est) / 2, Y: (ingombri[id].Sud + ingombri[id].Nord) / 2))
                .OrderBy(v => Math.Floor((riquadro.Ingombro.Nord - v.Y) / fascia))
                .ThenBy(v => v.X)
                .ThenBy(v => v.Id);
            foreach (var v in ordinati)
            {
                numeri.TryAdd(v.Id, numeri.Count + 1);
            }
        }

        return numeri;
    }

    /// <summary>A … Z, poi AA, AB…</summary>
    public static string Lettera(int indice) =>
        indice < 26 ? ((char)('A' + indice)).ToString() : Lettera(indice / 26 - 1) + Lettera(indice % 26);

    /// <summary>
    /// Righe e colonne che riempiono meglio l'area dei riquadri: per ogni griglia si misura quanta parte
    /// dell'area coprono i gruppi, ciascuno ingrandito finché sta nella sua cella (col suo rapporto
    /// larghezza/altezza); a parità vince la griglia con meno celle vuote.
    /// </summary>
    public static GrigliaStampa Griglia(IReadOnlyList<double> aspetti, double larghezza, double altezza)
    {
        var n = aspetti.Count;
        if (n <= 1 || larghezza <= 0 || altezza <= 0)
        {
            return new GrigliaStampa(1, 1);
        }

        GrigliaStampa? migliore = null;
        var punteggioMigliore = double.MinValue;
        for (var colonne = 1; colonne <= n; colonne++)
        {
            var righe = (n + colonne - 1) / colonne;
            if ((colonne - 1) * righe >= n)
            {
                continue; // una colonna intera vuota: c'è la stessa griglia con una colonna in meno
            }

            var aspettoCella = larghezza / colonne / (altezza / righe);
            var copertura = aspetti.Sum(a => Math.Min(a / aspettoCella, aspettoCella / a)) / (colonne * righe);
            // Arrotondato: differenze minime non devono far preferire una griglia con più buchi.
            var punteggio = Math.Round(copertura, 3) - 0.001 * (colonne * righe - n);
            if (punteggio > punteggioMigliore)
            {
                punteggioMigliore = punteggio;
                migliore = new GrigliaStampa(colonne, righe);
            }
        }

        return migliore!;
    }
}
