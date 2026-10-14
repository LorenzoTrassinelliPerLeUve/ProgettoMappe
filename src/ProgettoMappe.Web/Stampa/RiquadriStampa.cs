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

/// <summary>Posto di un riquadro nell'area dei riquadri, in mm dall'angolo in alto a sinistra.</summary>
public sealed record CellaStampa(double X, double Y, double Larghezza, double Altezza);

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

    /// <summary>Oltre questi riquadri la ricerca della disposizione diventa lenta: griglia uniforme (<see cref="Griglia"/>).</summary>
    public const int RiquadriDisposizioneProporzionale = 12;

    /// <summary>Quota minima dell'area per un riquadro piccolo (meno, se i riquadri sono tanti).</summary>
    public const double QuotaMinima = 0.06;

    /// <summary>Lato sotto il quale un riquadro non si legge più (numeri, lettera, nord e scala si coprono).</summary>
    public const double LatoMinimo = 25;

    /// <summary>Oltre questi vigneti un riquadro vuole almeno mezza pagina (task #94, richiesta del proprietario).</summary>
    public const int VignetiMezzaPagina = 40;

    /// <summary>Riquadri al massimo in una pagina: oltre, ognuno avrebbe meno di un sedicesimo del foglio.</summary>
    public const int RiquadriPerPagina = 16;

    /// <summary>
    /// La parte di pagina che chiedono i vigneti di un riquadro (task #94): oltre <see cref="VignetiMezzaPagina"/>
    /// almeno mezza pagina, e una pagina intera dal doppio; fino a lì un quarto di pagina ogni 40 vigneti
    /// (così un'azienda come Monsupello, 45 + 20 + 10 + 4 + 2 + 2 vigneti, resta su un foglio).
    /// </summary>
    private static double QuotaVigneti(int vigneti) => vigneti > VignetiMezzaPagina
        ? Math.Min(1, vigneti / (2.0 * VignetiMezzaPagina))
        : vigneti / (4.0 * VignetiMezzaPagina);

    /// <summary>
    /// La parte più piccola della pagina in cui un riquadro si legge ancora (task #94): quella dei suoi
    /// vigneti, mai meno di 1 / <see cref="RiquadriPerPagina"/>.
    /// </summary>
    public static double QuotaMinimaPagina(RiquadroStampa riquadro) =>
        Math.Max(1.0 / RiquadriPerPagina, QuotaVigneti(riquadro.Vigneti.Count));

    /// <summary>
    /// La pagina di ogni riquadro (da 1, task #94), nell'ordine dei riquadri: un riquadro sta nella pagina
    /// in corso se le parti minime (<see cref="QuotaMinimaPagina"/>) ci stanno tutte, altrimenti apre la
    /// pagina dopo. L'ordine non cambia, così le lettere si leggono di seguito pagina dopo pagina.
    /// </summary>
    public static IReadOnlyList<int> Pagine(IReadOnlyList<RiquadroStampa> riquadri)
    {
        var pagine = new List<int>();
        var (pagina, occupata) = (1, 0.0);
        foreach (var riquadro in riquadri)
        {
            var quota = QuotaMinimaPagina(riquadro);
            if (occupata > 0 && occupata + quota > 1 + 1e-9)
            {
                (pagina, occupata) = (pagina + 1, 0);
            }
            occupata += quota;
            pagine.Add(pagina);
        }
        return pagine;
    }

    /// <summary>
    /// Quanta area merita ogni riquadro (somma 1, task #91): per il 70% il numero di vigneti (i numeri
    /// devono starci senza coprirsi), per il 30% l'estensione del gruppo sul terreno; mai meno di
    /// <see cref="QuotaMinima"/>, così un riquadro con due vigneti resta leggibile, né meno della parte
    /// che chiedono i suoi vigneti (oltre <see cref="VignetiMezzaPagina"/> mezza pagina, task #94).
    /// </summary>
    public static IReadOnlyList<double> Pesi(IReadOnlyList<RiquadroStampa> riquadri)
    {
        var n = riquadri.Count;
        if (n == 0)
        {
            return [];
        }

        var vigneti = (double)Math.Max(1, riquadri.Sum(r => r.Vigneti.Count));
        var estensioni = riquadri.Select(r => Math.Sqrt(Math.Max(r.Ingombro.LarghezzaMetri, 50) * Math.Max(r.Ingombro.AltezzaMetri, 50))).ToList();
        var estensione = estensioni.Sum();
        var minimi = riquadri.Select(r => Math.Max(Math.Min(QuotaMinima, 0.5 / n), QuotaVigneti(r.Vigneti.Count))).ToArray();
        var sommaMinimi = minimi.Sum();
        if (sommaMinimi > 0.95)
        {
            // Troppi vigneti per una pagina (non succede con Pagine): i minimi si riducono insieme.
            minimi = minimi.Select(m => m / sommaMinimi * 0.95).ToArray();
        }
        var pesi = riquadri.Select((r, i) => 0.7 * r.Vigneti.Count / vigneti + 0.3 * estensioni[i] / estensione).ToArray();
        // I piccoli salgono al minimo e gli altri si riducono in proporzione; chi scende sotto il minimo si ferma lì.
        var fermi = new bool[n];
        while (true)
        {
            var liberi = Enumerable.Range(0, n).Where(i => !fermi[i]).ToList();
            var resto = 1 - Enumerable.Range(0, n).Where(i => fermi[i]).Sum(i => minimi[i]);
            var somma = liberi.Sum(i => pesi[i]);
            liberi.ForEach(i => pesi[i] = pesi[i] / somma * resto);
            var sotto = liberi.Where(i => pesi[i] < minimi[i]).ToList();
            if (sotto.Count == 0)
            {
                return pesi;
            }
            sotto.ForEach(i => (pesi[i], fermi[i]) = (minimi[i], true));
        }
    }

    /// <summary>
    /// Dove va ogni riquadro (task #91), nello stesso ordine di <paramref name="riquadri"/>: l'area si
    /// taglia in due, poi ogni parte ancora in due (in orizzontale o in verticale), dando a ogni parte
    /// l'area dei suoi pesi (<see cref="Pesi"/>). Fra tutti i tagli possibili vince quello in cui ogni
    /// gruppo, ingrandito finché sta nella sua cella col suo rapporto larghezza/altezza, occupa l'area
    /// più vicina a quella che merita; i riquadri con un lato sotto <see cref="LatoMinimo"/> perdono.
    /// Così un gruppo grande prende due colonne o due righe e i piccoli stanno in un angolo.
    /// <paramref name="spazio"/> è lo stacco fra due riquadri.
    /// </summary>
    public static IReadOnlyList<CellaStampa> Disponi(IReadOnlyList<RiquadroStampa> riquadri, double larghezza, double altezza, double spazio)
    {
        var n = riquadri.Count;
        if (n == 0 || larghezza <= 0 || altezza <= 0)
        {
            return riquadri.Select(_ => new CellaStampa(0, 0, Math.Max(0, larghezza), Math.Max(0, altezza))).ToList();
        }

        if (n > RiquadriDisposizioneProporzionale)
        {
            var griglia = Griglia(riquadri.Select(r => r.Ingombro.Aspetto).ToList(), larghezza, altezza);
            var (lc, ac) = ((larghezza + spazio) / griglia.Colonne, (altezza + spazio) / griglia.Righe);
            return riquadri.Select((_, i) => new CellaStampa(i % griglia.Colonne * lc, i / griglia.Colonne * ac, lc - spazio, ac - spazio)).ToList();
        }

        var pesi = Pesi(riquadri);
        // Dal più pesante: i tagli dividono la fila in due pezzi contigui.
        var ordine = Enumerable.Range(0, n).OrderByDescending(i => pesi[i]).ThenBy(i => i).ToArray();
        var ricerca = new RicercaDisposizione(
            ordine.Select(i => pesi[i]).ToArray(), ordine.Select(i => riquadri[i].Ingombro.Aspetto).ToArray(), larghezza * altezza, spazio);
        // Ogni cella perde lo stacco a destra e in basso: si dispone su un'area più grande di uno stacco.
        var risultato = new CellaStampa[n];
        ricerca.Costruisci(0, n, 0, 0, larghezza + spazio, altezza + spazio, (k, cella) => risultato[ordine[k]] = cella);
        return risultato;
    }

    /// <summary>
    /// Le celle dei riquadri pagina per pagina (task #94): in ogni pagina la stessa disposizione
    /// proporzionale di <see cref="Disponi"/>, sull'area dei riquadri di quella pagina
    /// (<paramref name="altezza"/> per numero di pagina). <paramref name="pagine"/> come da <see cref="Pagine"/>.
    /// </summary>
    public static IReadOnlyList<CellaStampa> DisponiPagine(IReadOnlyList<RiquadroStampa> riquadri, IReadOnlyList<int> pagine,
        double larghezza, Func<int, double> altezza, double spazio)
    {
        var celle = new CellaStampa[riquadri.Count];
        foreach (var pagina in Enumerable.Range(0, riquadri.Count).GroupBy(i => pagine[i]))
        {
            var indici = pagina.ToList();
            var disposte = Disponi(indici.Select(i => riquadri[i]).ToList(), larghezza, altezza(pagina.Key), spazio);
            for (var k = 0; k < indici.Count; k++)
            {
                celle[indici[k]] = disposte[k];
            }
        }
        return celle;
    }

    private sealed class RicercaDisposizione(double[] pesi, double[] aspetti, double areaTotale, double spazio)
    {
        // Lo stesso pezzo di fila nella stessa misura torna spesso (i tagli si compongono): si calcola una
        // volta, tenendo solo il punteggio e il taglio migliore; le celle si costruiscono alla fine.
        private readonly Dictionary<(int, int, long, long), (double Punteggio, bool Verticale, int K)> _giaVisti = [];

        public void Costruisci(int da, int a, double x, double y, double l, double h, Action<int, CellaStampa> cella)
        {
            if (a - da == 1)
            {
                cella(da, Cella(x, y, l, h));
                return;
            }

            var (_, verticale, k) = Migliore(da, a, l, h);
            var quota = Quota(da, k, a);
            if (verticale)
            {
                Costruisci(da, k, x, y, l * quota, h, cella);
                Costruisci(k, a, x + l * quota, y, l * (1 - quota), h, cella);
            }
            else
            {
                Costruisci(da, k, x, y, l, h * quota, cella);
                Costruisci(k, a, x, y + h * quota, l, h * (1 - quota), cella);
            }
        }

        private (double Punteggio, bool Verticale, int K) Migliore(int da, int a, double l, double h)
        {
            if (a - da == 1)
            {
                return (Punteggio(da, Cella(0, 0, l, h)), false, a);
            }

            // Al decimo di millimetro: misure più vicine danno lo stesso punteggio a tutti gli effetti.
            var chiave = (da, a, (long)Math.Round(l * 10), (long)Math.Round(h * 10));
            if (_giaVisti.TryGetValue(chiave, out var trovato))
            {
                return trovato;
            }

            var migliore = (Punteggio: double.NegativeInfinity, Verticale: true, K: da + 1);
            foreach (var verticale in new[] { true, false })
            {
                for (var k = da + 1; k < a; k++)
                {
                    var quota = Quota(da, k, a);
                    var punteggio = verticale
                        ? Migliore(da, k, l * quota, h).Punteggio + Migliore(k, a, l * (1 - quota), h).Punteggio
                        : Migliore(da, k, l, h * quota).Punteggio + Migliore(k, a, l, h * (1 - quota)).Punteggio;
                    if (punteggio > migliore.Punteggio + 1e-9)
                    {
                        migliore = (punteggio, verticale, k);
                    }
                }
            }

            _giaVisti[chiave] = migliore;
            return migliore;
        }

        // Somme dei pesi dall'inizio della fila: cumulati[i] = pesi[0] + … + pesi[i - 1].
        private readonly double[] _cumulati = pesi.Aggregate(new List<double> { 0 }, (s, p) => { s.Add(s[^1] + p); return s; }).ToArray();

        /// <summary>Parte dell'area di [da, a) che va a [da, k).</summary>
        private double Quota(int da, int k, int a) => (_cumulati[k] - _cumulati[da]) / (_cumulati[a] - _cumulati[da]);

        private CellaStampa Cella(double x, double y, double l, double h) =>
            new(x, y, Math.Max(0, l - spazio), Math.Max(0, h - spazio));

        /// <summary>
        /// Peso × logaritmo di (area che il gruppo occupa davvero nella cella / area che merita): massimo
        /// quando ogni gruppo ha la sua parte, e un riquadro sacrificato costa più di quanto rende agli altri.
        /// </summary>
        private double Punteggio(int i, CellaStampa cella)
        {
            if (cella.Larghezza <= 0 || cella.Altezza <= 0)
            {
                return -1e6;
            }

            var aspettoCella = cella.Larghezza / cella.Altezza;
            var occupata = cella.Larghezza * cella.Altezza * Math.Min(aspetti[i] / aspettoCella, aspettoCella / aspetti[i]);
            var punteggio = pesi[i] * Math.Log(occupata / (pesi[i] * areaTotale));
            var lato = Math.Min(cella.Larghezza, cella.Altezza);
            return lato < LatoMinimo ? punteggio - (LatoMinimo - lato) / LatoMinimo : punteggio;
        }
    }
}
