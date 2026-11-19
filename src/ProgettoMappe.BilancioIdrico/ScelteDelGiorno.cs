namespace ProgettoMappe.BilancioIdrico;

/// <summary>
/// REGOLE.md § 3: quale dato vale in un giorno (meteo, fenologia con riporto e previsione, suolo, gestione, stile,
/// configurazione del settore, irrigazione ripartita). Indicizza gli ingressi una volta sola.
/// </summary>
public sealed class ScelteDelGiorno
{
    private readonly Parametri _p;
    private readonly Dictionary<DateOnly, MeteoGiorno> _meteo;
    private readonly Dictionary<int, FenologiaVigneto> _fenologia;
    private readonly ILookup<int, AnalisiSuolo> _analisi;
    private readonly ILookup<int, RilievoRadici> _radici;
    private readonly ILookup<int, RilievoGestioneSuolo> _gestioni;
    private readonly ILookup<int, StileVigneto> _stili;
    private readonly ILookup<int, ConfigurazioneSettore> _configurazioni;
    private readonly Dictionary<int, Vigneto> _vigneti;
    private readonly ILookup<int, SettoreVigneto> _settoriDelVigneto;
    private readonly Dictionary<(int Vigneto, DateOnly Data), double> _irrigazione = [];

    public ScelteDelGiorno(IngressiAzienda ingressi, Parametri p)
    {
        _p = p;
        _meteo = ingressi.Meteo.GroupBy(m => m.Data).ToDictionary(g => g.Key, g => g.Last());
        _fenologia = ingressi.Fenologia.GroupBy(f => f.IdVigneto).ToDictionary(g => g.Key, g => new FenologiaVigneto(g));
        _analisi = ingressi.AnalisiSuolo.OrderBy(a => a.Data).ToLookup(a => a.IdVigneto);
        _radici = ingressi.Radici.OrderBy(r => r.Data).ToLookup(r => r.IdVigneto);
        _gestioni = ingressi.GestioniSuolo.OrderBy(g => g.Data).ToLookup(g => g.IdVigneto);
        _stili = ingressi.Stili.OrderBy(s => s.ValidoDal).ToLookup(s => s.IdVigneto);
        _configurazioni = ingressi.Configurazioni.OrderBy(c => c.ValidoDal).ToLookup(c => c.IdSettore);
        _vigneti = ingressi.Vigneti.GroupBy(v => v.IdVigneto).ToDictionary(g => g.Key, g => g.Last());
        _settoriDelVigneto = ingressi.SettoriVigneti.OrderBy(sv => sv.IdSettore).ToLookup(sv => sv.IdVigneto);
        RipartisciIrrigazioni(ingressi);
        if (p.PioggiaPerEvento)
            _eventiSottoSoglia = EventiSottoSoglia(_meteo, p.PioggiaMinimaUtileMm);
        if (p.SuoloMedioDelSettore)
            _suoloMedio = SuoliMedi(ingressi);
    }

    private readonly HashSet<DateOnly> _eventiSottoSoglia = [];
    private readonly Dictionary<int, SuoloScelto> _suoloMedio = [];

    /// <summary>§ 3.1: la riga del meteo aziendale; <c>Presente = false</c> è «meteo mancante».</summary>
    public (bool Presente, double? Et0Mm, double? PioggiaMm) Meteo(DateOnly data)
    {
        if (!_meteo.TryGetValue(data, out var m))
            return (false, null, null);
        if (_p.MeteoVuotoComeZero)
            return (true, m.Et0Mm ?? 0, m.PioggiaMm ?? 0);
        return (m.Et0Mm is not null && m.PioggiaMm is not null, m.Et0Mm, m.PioggiaMm);
    }

    /// <summary>
    /// La pioggia che entra nel bilancio: con <see cref="Parametri.PioggiaPerEvento"/> 0 nei giorni di un evento sotto
    /// <see cref="Parametri.PioggiaMinimaUtileMm"/>, altrimenti la soglia vale sul giorno.
    /// </summary>
    public double? PioggiaUtile(DateOnly data, double? pioggia)
    {
        if (pioggia is not { } mm)
            return null;
        if (_p.PioggiaPerEvento)
            return _eventiSottoSoglia.Contains(data) ? 0 : mm;
        return mm < _p.PioggiaMinimaUtileMm ? 0 : mm;
    }

    /// <summary>I giorni degli eventi piovosi (giorni consecutivi con pioggia &gt; 0) che in tutto non arrivano alla soglia.</summary>
    private static HashSet<DateOnly> EventiSottoSoglia(Dictionary<DateOnly, MeteoGiorno> meteo, double soglia)
    {
        var sotto = new HashSet<DateOnly>();
        var evento = new List<DateOnly>();
        var somma = 0.0;
        void Chiudi()
        {
            if (somma < soglia)
                sotto.UnionWith(evento);
            evento.Clear();
            somma = 0;
        }
        foreach (var m in meteo.Values.OrderBy(m => m.Data))
        {
            if (m.PioggiaMm is not > 0)
            {
                Chiudi();
                continue;
            }
            if (evento.Count > 0 && evento[^1].AddDays(1) != m.Data)
                Chiudi();
            evento.Add(m.Data);
            somma += m.PioggiaMm.Value;
        }
        Chiudi();
        return sotto;
    }

    /// <summary>
    /// Il suolo del vigneto nel giorno, con le fonti: l'analisi scelta (§ 3.3), oppure con <see cref="Parametri.SuoloMedioDelSettore"/>
    /// la media del settore. Lo scheletro della calibrazione (<see cref="Vigneto.ScheletroManualePerc"/>) vince su tutti.
    /// </summary>
    public SuoloScelto Suolo(int idVigneto, DateOnly data)
    {
        var suolo = _p.SuoloMedioDelSettore
            ? _suoloMedio.GetValueOrDefault(idVigneto) ?? SuoloScelto.Nessuno
            : Analisi(idVigneto, data) is { } a
                ? new SuoloScelto(a, a.AcquaDisponibileMmM is null ? null : FontiValore.Analisi,
                    a.ProfonditaSuolo is { } ps && ps != 0 ? FontiValore.Analisi : FontiValore.Ripiego,
                    a.Pietrosita is null ? null : FontiValore.Analisi, 1)
                : SuoloScelto.Nessuno;
        if (Vigneto(idVigneto).ScheletroManualePerc is { } manuale && _p.PietrositaScala > 0)
        {
            var analisi = (suolo.Analisi ?? new AnalisiSuolo(idVigneto, data, null)) with
            {
                Pietrosita = manuale / _p.PietrositaScala,
                ScheletroPerc = manuale,
            };
            suolo = suolo with { Analisi = analisi, FonteScheletro = FontiValore.Calibrazione };
        }
        return suolo;
    }

    /// <summary>
    /// Per ogni vigneto: media per vigneto di tutte le sue analisi (qualunque data), poi media semplice sui vigneti dei suoi
    /// settori (lui compreso; senza settore lui solo). La profondità del suolo è quella del vigneto se ce l'ha.
    /// </summary>
    private Dictionary<int, SuoloScelto> SuoliMedi(IngressiAzienda ingressi)
    {
        double? Media(IEnumerable<double?> valori) => valori.OfType<double>().ToList() is { Count: > 0 } l ? l.Average() : null;
        var perVigneto = ingressi.AnalisiSuolo.GroupBy(a => a.IdVigneto).ToDictionary(g => g.Key, g => (
            Acqua: Media(g.Select(a => a.AcquaDisponibileMmM)),
            Profondita: Media(g.Select(a => a.ProfonditaSuolo is { } ps && ps != 0 ? ps : (double?)null)),
            Pietrosita: Media(g.Select(a => a.Pietrosita)),
            Tessitura: g.Where(a => a.IdTessitura is not null).GroupBy(a => a.IdTessitura).OrderByDescending(t => t.Count())
                .ThenBy(t => t.Key).Select(t => t.Key).FirstOrDefault()));
        var settoriDi = ingressi.SettoriVigneti.ToLookup(sv => sv.IdVigneto, sv => sv.IdSettore);
        var vignetiDi = ingressi.SettoriVigneti.ToLookup(sv => sv.IdSettore, sv => sv.IdVigneto);
        var tutti = ingressi.Vigneti.Select(v => v.IdVigneto).Concat(ingressi.SettoriVigneti.Select(sv => sv.IdVigneto)).Distinct();

        var suoli = new Dictionary<int, SuoloScelto>();
        foreach (var id in tutti)
        {
            var inSettore = settoriDi[id].Any();
            var gruppo = settoriDi[id].SelectMany(s => vignetiDi[s]).Append(id).Distinct()
                .Where(perVigneto.ContainsKey).Select(v => perVigneto[v]).ToList();
            if (gruppo.Count == 0)
            {
                suoli[id] = SuoloScelto.Nessuno;
                continue;
            }
            var fonte = inSettore ? FontiValore.Settore : FontiValore.Analisi;
            var proprio = perVigneto.TryGetValue(id, out var v) ? v.Profondita : null;
            var profondita = proprio ?? Media(gruppo.Select(x => x.Profondita));
            var acqua = Media(gruppo.Select(x => x.Acqua));
            var pietrosita = Media(gruppo.Select(x => x.Pietrosita));
            var tessitura = gruppo.Select(x => x.Tessitura).Where(t => t is not null).GroupBy(t => t).OrderByDescending(t => t.Count())
                .ThenBy(t => t.Key).Select(t => t.Key).FirstOrDefault();
            suoli[id] = new SuoloScelto(
                new AnalisiSuolo(id, DateOnly.MinValue, acqua, profondita, pietrosita, tessitura, pietrosita * _p.PietrositaScala),
                acqua is null ? null : fonte,
                proprio is not null ? FontiValore.Analisi : profondita is not null ? fonte : FontiValore.Ripiego,
                pietrosita is null ? null : fonte,
                gruppo.Count);
        }
        return suoli;
    }

    /// <summary>
    /// Le radici del vigneto nel giorno: il rilievo 145 scelto, con profondità e larghezza della calibrazione al posto delle sue
    /// (ognuna da sola). Fonte: calibrazione se ce n'è almeno una, rilievo, altrimenti null.
    /// </summary>
    public (RilievoRadici? Radici, string? Fonte) RadiciUsate(int idVigneto, DateOnly data)
    {
        var rilievo = Radici(idVigneto, data);
        var v = Vigneto(idVigneto);
        if (v.ProfonditaRadiciManualeM is null && v.LarghezzaRadiciManualeM is null)
            return (rilievo, rilievo is null ? null : FontiValore.Rilievo);
        return (new RilievoRadici(idVigneto, data, v.LarghezzaRadiciManualeM ?? rilievo?.LarghezzaM, v.ProfonditaRadiciManualeM ?? rilievo?.ProfonditaM),
            FontiValore.Calibrazione);
    }

    /// <summary>§ 3.2: storico del giorno, altrimenti riporto fra il primo e l'ultimo storico, poi la previsione.</summary>
    public int? Bbch(int idVigneto, DateOnly data)
    {
        if (!_fenologia.TryGetValue(idVigneto, out var f))
            return null;
        if (f.Risolta.TryGetValue(data, out var risolta))
            return risolta;
        if (f.Storico.TryGetValue(data, out var oggi))
            return oggi;
        if (f.PrimoStorico is { } primo && f.UltimoStorico is { } ultimo && data > primo && data < ultimo)
            return _meteo.ContainsKey(data) ? f.StoricoPrecedente(data) : null;
        if (f.UltimoStorico is { } u && data > u && f.Previsione.TryGetValue(data, out var previsto))
            return previsto;
        return null;
    }

    public AnalisiSuolo? Analisi(int idVigneto, DateOnly data) => Rilievo(_analisi[idVigneto], a => a.Data, data, _p.AnalisiSuoloPiuRecente);

    public RilievoRadici? Radici(int idVigneto, DateOnly data) => Rilievo(_radici[idVigneto], r => r.Data, data, _p.RilieviVignetoPiuRecenti);

    public Vigneto Vigneto(int idVigneto) => _vigneti.GetValueOrDefault(idVigneto) ?? new Vigneto(idVigneto);

    /// <summary>I settori in cui sta il vigneto (nessuno = vigneto senza settore).</summary>
    public IEnumerable<SettoreVigneto> SettoriDelVigneto(int idVigneto) => _settoriDelVigneto[idVigneto];

    /// <summary>§ 1.2: gestione del suolo dall'ultimo rilievo 140, altrimenti il ripiego.</summary>
    public int? Gestione(int idVigneto, DateOnly data) =>
        Rilievo(_gestioni[idVigneto], g => g.Data, data, _p.RilieviVignetoPiuRecenti)?.IdGestioneSuolo ?? _p.GestioneSuoloRipiego;

    /// <summary>§ 1.8: lo stile valido nel giorno con la data d'inizio più recente, altrimenti il ripiego.</summary>
    public int? Stile(int idVigneto, DateOnly data) => StileConFonte(idVigneto, data).Stile;

    /// <summary>Lo stile con la sua fonte: configurazione, target di concimazione (<see cref="Parametri.StileDaTarget"/>), ripiego.</summary>
    public (int? Stile, string? Fonte) StileConFonte(int idVigneto, DateOnly data)
    {
        if (Valido(_stili[idVigneto], s => s.ValidoDal, s => s.ValidoAl, data) is { } s)
            return (s.IdStileVino, FontiValore.Configurazione);
        if (Vigneto(idVigneto).IdTargetConcimazione is { } t && _p.StileDaTarget.TryGetValue(t, out var daTarget))
            return (daTarget, FontiValore.Target);
        return _p.StileVinoRipiego is { } r ? (r, FontiValore.Ripiego) : (null, null);
    }

    /// <summary>§ 3.5: la configurazione valida nel giorno con la data d'inizio più recente.</summary>
    public ConfigurazioneSettore? Configurazione(int idSettore, DateOnly data) =>
        Valido(_configurazioni[idSettore], c => c.ValidoDal, c => c.ValidoAl, data);

    /// <summary>Efficienza del settore: reale, poi tipica dell'impianto, poi il ripiego (§ 1.11).</summary>
    public double Efficienza(ConfigurazioneSettore? config) =>
        config?.EfficienzaReale
        ?? (config?.IdTipologiaImpianto is { } t && _p.EfficienzaImpianto.TryGetValue(t, out var e) ? e : _p.EfficienzaRipiego);

    /// <summary>
    /// § 3.4 e § 9: mm d'irrigazione del vigneto in quel giorno = Σ sui suoi settori (mm del settore × quota del vigneto / 100)
    /// più quelle registrate sul vigneto (0 senza eventi).
    /// </summary>
    public double Irrigazione(int idVigneto, DateOnly data) => _irrigazione.GetValueOrDefault((idVigneto, data));

    private void RipartisciIrrigazioni(IngressiAzienda ingressi)
    {
        foreach (var r in ingressi.IrrigazioniVigneto)
            Aggiungi(r.IdVigneto, r.Data, r.Mm);

        var vignetiDelSettore = ingressi.SettoriVigneti.ToLookup(sv => sv.IdSettore);
        void Ripartisci(int idSettore, DateOnly data, double mmSettore)
        {
            foreach (var sv in vignetiDelSettore[idSettore])
                Aggiungi(sv.IdVigneto, data, mmSettore * sv.Percentuale / 100);
        }

        foreach (var e in ingressi.Irrigazioni)
        {
            if (e.IdSettore is { } idSettore)
            {
                var config = Configurazione(idSettore, e.Data);
                var efficienza = _p.EfficienzaSulleIrrigazioni ? Efficienza(config) : 1;
                var mm = e.VolumeUtileMm
                    ?? e.VolumeMm * (e.Efficienza ?? efficienza)
                    ?? e.DurataOre * config?.TassoMmOra * efficienza;
                if (mm is { } v)
                    Ripartisci(idSettore, e.Data, v);
            }
            else if (e.IdShift is { } idShift && e.DurataOre is { } ore)
            {
                foreach (var ss in ingressi.ShiftSettori.Where(s => s.IdShift == idShift && ValidoIl(s.ValidoDal, s.ValidoAl, e.Data)))
                {
                    var config = Configurazione(ss.IdSettore, e.Data);
                    if (config?.TassoMmOra is not { } tasso)
                        continue;
                    var efficienza = _p.EfficienzaSuShift ? Efficienza(config) : 1;
                    Ripartisci(ss.IdSettore, e.Data, ore * tasso * efficienza);
                }
            }
        }
    }

    private void Aggiungi(int idVigneto, DateOnly data, double mm) =>
        _irrigazione[(idVigneto, data)] = _irrigazione.GetValueOrDefault((idVigneto, data)) + mm;

    public static bool ValidoIl(DateOnly dal, DateOnly? al, DateOnly data) => dal <= data && (al is null || al >= data);

    /// <summary>Elenco ordinato per data: l'ultimo in assoluto, oppure l'ultimo fatto entro il giorno.</summary>
    private static T? Rilievo<T>(IEnumerable<T> ordinati, Func<T, DateOnly> dataDi, DateOnly data, bool piuRecente) where T : class =>
        piuRecente ? ordinati.LastOrDefault() : ordinati.LastOrDefault(r => dataDi(r) <= data);

    private static T? Valido<T>(IEnumerable<T> ordinatiPerInizio, Func<T, DateOnly> dal, Func<T, DateOnly?> al, DateOnly data)
        where T : class => ordinatiPerInizio.LastOrDefault(x => ValidoIl(dal(x), al(x), data));

    /// <summary>Il suolo usato per l'AWC, con la fonte di acqua, profondità e scheletro e quanti vigneti con analisi fanno la media.</summary>
    public sealed record SuoloScelto(AnalisiSuolo? Analisi, string? FonteAcqua, string? FonteProfondita, string? FonteScheletro,
        int VignetiConAnalisi)
    {
        public static SuoloScelto Nessuno { get; } = new(null, null, null, null, 0);
    }

    private sealed class FenologiaVigneto
    {
        public Dictionary<DateOnly, int> Storico { get; } = [];
        public Dictionary<DateOnly, int> Previsione { get; } = [];
        public Dictionary<DateOnly, int> Risolta { get; } = [];
        private readonly DateOnly[] _dateStorico;

        public FenologiaVigneto(IEnumerable<FenologiaGiorno> righe)
        {
            foreach (var r in righe)
                (r.Fonte switch { FonteFenologia.Storico => Storico, FonteFenologia.Previsione => Previsione, _ => Risolta })[r.Data] = r.Bbch;
            _dateStorico = [.. Storico.Keys.Order()];
        }

        public DateOnly? PrimoStorico => _dateStorico.Length > 0 ? _dateStorico[0] : null;
        public DateOnly? UltimoStorico => _dateStorico.Length > 0 ? _dateStorico[^1] : null;

        public int StoricoPrecedente(DateOnly data)
        {
            var i = Array.BinarySearch(_dateStorico, data);
            return Storico[_dateStorico[(i >= 0 ? i : ~i) - 1]];
        }
    }
}
