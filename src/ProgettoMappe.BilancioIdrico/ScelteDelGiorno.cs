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
    private readonly Dictionary<int, double?> _distanzaFile;
    private readonly Dictionary<(int Settore, int Vigneto, DateOnly Data), double> _irrigazione = [];

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
        _distanzaFile = ingressi.Vigneti.GroupBy(v => v.IdVigneto).ToDictionary(g => g.Key, g => g.Last().DistanzaFile);
        RipartisciIrrigazioni(ingressi);
    }

    /// <summary>§ 3.1: la riga del meteo aziendale; <c>Presente = false</c> è «meteo mancante».</summary>
    public (bool Presente, double? Et0Mm, double? PioggiaMm) Meteo(DateOnly data)
    {
        if (!_meteo.TryGetValue(data, out var m))
            return (false, null, null);
        if (_p.MeteoVuotoComeZero)
            return (true, m.Et0Mm ?? 0, m.PioggiaMm ?? 0);
        return (m.Et0Mm is not null && m.PioggiaMm is not null, m.Et0Mm, m.PioggiaMm);
    }

    /// <summary>§ 3.2: storico del giorno, altrimenti riporto fra il primo e l'ultimo storico, poi la previsione.</summary>
    public int? Bbch(int idVigneto, DateOnly data)
    {
        if (!_fenologia.TryGetValue(idVigneto, out var f))
            return null;
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

    public double? DistanzaFile(int idVigneto) => _distanzaFile.GetValueOrDefault(idVigneto);

    /// <summary>§ 1.2: gestione del suolo dall'ultimo rilievo 140, altrimenti il ripiego.</summary>
    public int? Gestione(int idVigneto, DateOnly data) =>
        Rilievo(_gestioni[idVigneto], g => g.Data, data, _p.RilieviVignetoPiuRecenti)?.IdGestioneSuolo ?? _p.GestioneSuoloRipiego;

    /// <summary>§ 1.8: lo stile valido nel giorno con la data d'inizio più recente, altrimenti il ripiego.</summary>
    public int? Stile(int idVigneto, DateOnly data) =>
        Valido(_stili[idVigneto], s => s.ValidoDal, s => s.ValidoAl, data)?.IdStileVino ?? _p.StileVinoRipiego;

    /// <summary>§ 3.5: la configurazione valida nel giorno con la data d'inizio più recente.</summary>
    public ConfigurazioneSettore? Configurazione(int idSettore, DateOnly data) =>
        Valido(_configurazioni[idSettore], c => c.ValidoDal, c => c.ValidoAl, data);

    /// <summary>Efficienza del settore: reale, poi tipica dell'impianto, poi il ripiego (§ 1.11).</summary>
    public double Efficienza(ConfigurazioneSettore? config) =>
        config?.EfficienzaReale
        ?? (config?.IdTipologiaImpianto is { } t && _p.EfficienzaImpianto.TryGetValue(t, out var e) ? e : _p.EfficienzaRipiego);

    /// <summary>§ 3.4: mm d'irrigazione del vigneto nel settore in quel giorno (0 senza eventi).</summary>
    public double Irrigazione(int idSettore, int idVigneto, DateOnly data) => _irrigazione.GetValueOrDefault((idSettore, idVigneto, data));

    private void RipartisciIrrigazioni(IngressiAzienda ingressi)
    {
        foreach (var r in ingressi.IrrigazioniRipartite)
            Aggiungi(r.IdSettore, r.IdVigneto, r.Data, r.Mm);

        var vignetiDelSettore = ingressi.SettoriVigneti.ToLookup(sv => sv.IdSettore);
        void Ripartisci(int idSettore, DateOnly data, double mmSettore)
        {
            foreach (var sv in vignetiDelSettore[idSettore])
                Aggiungi(idSettore, sv.IdVigneto, data, mmSettore * sv.Percentuale / 100);
        }

        foreach (var e in ingressi.Irrigazioni)
        {
            if (e.IdSettore is { } idSettore)
            {
                var mm = e.VolumeUtileMm
                    ?? e.VolumeMm * (e.Efficienza ?? 1)
                    ?? e.DurataOre * Configurazione(idSettore, e.Data)?.TassoMmOra;
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

    private void Aggiungi(int idSettore, int idVigneto, DateOnly data, double mm) =>
        _irrigazione[(idSettore, idVigneto, data)] = _irrigazione.GetValueOrDefault((idSettore, idVigneto, data)) + mm;

    public static bool ValidoIl(DateOnly dal, DateOnly? al, DateOnly data) => dal <= data && (al is null || al >= data);

    /// <summary>Elenco ordinato per data: l'ultimo in assoluto, oppure l'ultimo fatto entro il giorno.</summary>
    private static T? Rilievo<T>(IEnumerable<T> ordinati, Func<T, DateOnly> dataDi, DateOnly data, bool piuRecente) where T : class =>
        piuRecente ? ordinati.LastOrDefault() : ordinati.LastOrDefault(r => dataDi(r) <= data);

    private static T? Valido<T>(IEnumerable<T> ordinatiPerInizio, Func<T, DateOnly> dal, Func<T, DateOnly?> al, DateOnly data)
        where T : class => ordinatiPerInizio.LastOrDefault(x => ValidoIl(dal(x), al(x), data));

    private sealed class FenologiaVigneto
    {
        public Dictionary<DateOnly, int> Storico { get; } = [];
        public Dictionary<DateOnly, int> Previsione { get; } = [];
        private readonly DateOnly[] _dateStorico;

        public FenologiaVigneto(IEnumerable<FenologiaGiorno> righe)
        {
            foreach (var r in righe)
                (r.Fonte == FonteFenologia.Storico ? Storico : Previsione)[r.Data] = r.Bbch;
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
