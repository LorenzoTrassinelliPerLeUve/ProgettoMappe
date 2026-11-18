namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>
/// Un run salvato del vecchio motore, rimesso come ingressi del motore nuovo. Gli input di ogni giorno (meteo, BBCH, AWC,
/// gestione, stile, irrigazione) vengono dalle righe del risultato, perché le tabelle BI_Input* sono riscritte a ogni run
/// (LEGGIMI.md); settori, configurazioni e tabelle del modello vengono dai csv di configurazione (quelli di oggi).
/// </summary>
public sealed class RunDiRiferimento
{
    public required int IdRun { get; init; }
    public required IngressiAzienda Ingressi { get; init; }
    public required Parametri Parametri { get; init; }

    /// <summary>Le righe di BI_ResultSettoreVignetoGiornaliero del run.</summary>
    public required IReadOnlyList<Riga> Righe { get; init; }

    /// <summary>Le righe di BI_ResultSettoreGiornaliero del run.</summary>
    public required IReadOnlyList<Riga> RigheSettore { get; init; }

    public static IReadOnlyList<int> Run(TabelleCsv t) =>
        t["BI_Run"].Where(r => r.Testo("Stato") is null or "COMPLETATO").Select(r => r.IntObbl("IdRun")).Order().ToList();

    public static RunDiRiferimento Carica(TabelleCsv t, int idRun) =>
        Carica(t, idRun, t["BI_ResultSettoreVignetoGiornaliero"].Where(r => r.Intero("IdRun") == idRun).ToList());

    public static RunDiRiferimento Carica(TabelleCsv t, int idRun, IReadOnlyList<Riga> righe)
    {
        var run = t["BI_Run"].Single(r => r.Intero("IdRun") == idRun);
        var idEnte = run.IntObbl("IdEnte");
        var perVigneto = righe.GroupBy(r => r.IntObbl("IdVigneto"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Data("Data")).ThenBy(r => r.IntObbl("IdSettore")).ToList());

        var meteo = righe.Where(r => !r.Vero("FlagMeteoMancante"))
            .GroupBy(r => r.Data("Data")!.Value)
            .Select(g => new MeteoGiorno(g.Key, g.First().Numero("Et0Mm"), g.First().Numero("PioggiaMm")))
            .ToList();

        var fenologia = new List<FenologiaGiorno>();
        var analisi = new List<AnalisiSuolo>();
        var gestioni = new List<RilievoGestioneSuolo>();
        var stili = new List<StileVigneto>();
        foreach (var (idVigneto, rv) in perVigneto)
        {
            var perGiorno = rv.GroupBy(r => r.Data("Data")!.Value).Select(g => (Data: g.Key, Riga: g.First())).ToList();
            foreach (var (data, r) in perGiorno)
                if (r.Intero("BBCH") is { } bbch)
                    fenologia.Add(new FenologiaGiorno(idVigneto, data, bbch, FonteFenologia.Risolta));
            // Senza «suolo mancante» c'è un'analisi anche quando l'AWC è vuota (per esempio senza tessitura).
            foreach (var (data, suolo) in Cambi(perGiorno, r => (Awc: r.Numero("AWCmm"), Mancante: r.Vero("FlagSuoloMancante"))))
                if (!suolo.Mancante)
                    analisi.Add(new AnalisiSuolo(idVigneto, data, suolo.Awc));
            foreach (var (data, gestione) in Cambi(perGiorno, r => r.Intero("IdGestioneSuolo")))
                if (gestione is { } gs)
                    gestioni.Add(new RilievoGestioneSuolo(idVigneto, data, gs));
            var tratti = Cambi(perGiorno, r => r.Intero("IdStileVino"));
            for (var i = 0; i < tratti.Count; i++)
                if (tratti[i].Valore is { } stile)
                    stili.Add(new StileVigneto(idVigneto, stile, tratti[i].Data,
                        i + 1 < tratti.Count ? tratti[i + 1].Data.AddDays(-1) : null));
        }

        var coppie = t["BI_RunScope"].Where(r => r.Intero("IdRun") == idRun)
            .Select(r => (Settore: r.IntObbl("IdSettore"), Vigneto: r.IntObbl("IdVigneto")))
            .Concat(righe.Select(r => (Settore: r.IntObbl("IdSettore"), Vigneto: r.IntObbl("IdVigneto"))))
            .Distinct().ToList();
        var percentuali = t["BI_SettoreVigneto"]
            .ToDictionary(r => (r.IntObbl("IdSettore"), r.IntObbl("IdVigneto")), r => r.Numero("PercentualeNelSettore") ?? 100);
        var idSettori = coppie.Select(c => c.Settore).ToHashSet();

        var ingressi = new IngressiAzienda
        {
            IdEnte = idEnte,
            Da = run.Data("DataDa")!.Value,
            A = run.Data("DataA")!.Value,
            // Il settore «Regime in Asciutta» del vecchio resta un settore come gli altri, con impianto 0 e senza tasso.
            Settori = idSettori.Order().Select(id => new Settore(id)).ToList(),
            Vigneti = perVigneto.Keys.Order().Select(id => new Vigneto(id)).ToList(),
            SettoriVigneti = coppie.Select(c => new SettoreVigneto(c.Settore, c.Vigneto, percentuali.GetValueOrDefault(c, 100))).ToList(),
            Configurazioni = t["BI_SettoreConfig"].Where(r => idSettori.Contains(r.IntObbl("IdSettore")))
                .Select(r => new ConfigurazioneSettore(r.IntObbl("IdSettore"), r.Data("ValidoDal")!.Value, r.Data("ValidoAl"),
                    r.Intero("IdTipologiaImpianto"), r.Numero("TassoIrrigazioneMmOra"), r.Numero("EfficienzaReale")))
                .ToList(),
            ShiftSettori = t["BI_ShiftSettore"].Where(r => idSettori.Contains(r.IntObbl("IdSettore")))
                .Select(r => new ShiftSettore(r.IntObbl("IdShift"), r.IntObbl("IdSettore"), r.Data("ValidoDal")!.Value, r.Data("ValidoAl")))
                .ToList(),
            Meteo = meteo,
            Fenologia = fenologia,
            AnalisiSuolo = analisi,
            GestioniSuolo = gestioni,
            Stili = stili,
            IrrigazioniVigneto = righe.Where(r => r.Numero("IrrigazioneMm") is > 0)
                .Select(r => new IrrigazioneVigneto(r.IntObbl("IdSettore"), r.IntObbl("IdVigneto"), r.Data("Data")!.Value,
                    r.Numero("IrrigazioneMm")!.Value))
                .ToList(),
        };

        return new RunDiRiferimento
        {
            IdRun = idRun,
            Ingressi = ingressi,
            Parametri = ParametriDelDataset(t) with
            {
                PartenzaComeIlVecchio = true,
                DecimaliComeSql = true,
                // Le percentuali dei run vecchi sono quote del settore (REGOLE.md § 9): totali del settore come allora.
                QuoteDelSettore = true,
                // Gli input sono quelli salvati giorno per giorno: ogni valore vale dal giorno in cui compare.
                AnalisiSuoloPiuRecente = false,
                RilieviVignetoPiuRecenti = false,
                GestioneSuoloRipiego = null,
            },
            Righe = righe,
            RigheSettore = t["BI_ResultSettoreGiornaliero"].Where(r => r.Intero("IdRun") == idRun).ToList(),
        };
    }

    /// <summary>Le tabelle del modello come sono nel dataset (BI_ParametroModello, BI_KcCurva, BI_SogliaPdlwp, BI_TipologiaImpianto).</summary>
    public static Parametri ParametriDelDataset(TabelleCsv t)
    {
        var p = Parametri.ComeOggi;
        // Gli altri tre parametri della tabella il codice SQL non li legge (REGOLE.md § 4.1).
        if (t["BI_ParametroModello"].FirstOrDefault(r => r.Testo("NomeParametro") == "FRAZIONE_RAW_AWC")?.Numero("ValoreNumerico")
            is { } frazione)
            p = p with { FrazioneRawAwc = frazione };
        if (t["BI_KcCurva"].Count > 0)
            p = p with
            {
                CurveKc = t["BI_KcCurva"].GroupBy(r => r.IntObbl("IdGestioneSuolo")).ToDictionary(g => g.Key,
                    g => (IReadOnlyList<PuntoKc>)g.OrderBy(r => r.IntObbl("BBCH"))
                        .Select(r => new PuntoKc(r.IntObbl("BBCH"), r.Numero("ValoreKc")!.Value)).ToList()),
            };
        if (t["BI_SogliaPdlwp"].Count > 0)
            p = p with
            {
                Soglie = t["BI_SogliaPdlwp"].ToDictionary(r => (r.IntObbl("IdStileVino"), r.IntObbl("IdFase")), r => r.Numero("SogliaMpa")!.Value),
            };
        if (t["BI_TipologiaImpianto"].Count > 0)
            p = p with
            {
                EfficienzaImpianto = t["BI_TipologiaImpianto"].Where(r => r.Numero("CoefficienteEfficienza") is not null)
                    .ToDictionary(r => r.IntObbl("IdTipologiaImpianto"), r => r.Numero("CoefficienteEfficienza")!.Value),
            };
        return p;
    }

    /// <summary>I giorni in cui il valore cambia (il primo giorno compreso), con il valore nuovo.</summary>
    private static List<(DateOnly Data, T? Valore)> Cambi<T>(List<(DateOnly Data, Riga Riga)> giorni, Func<Riga, T?> valore)
    {
        var cambi = new List<(DateOnly, T?)>();
        for (var i = 0; i < giorni.Count; i++)
        {
            var v = valore(giorni[i].Riga);
            if (i == 0 || !EqualityComparer<T?>.Default.Equals(v, valore(giorni[i - 1].Riga)))
                cambi.Add((giorni[i].Data, v));
        }
        return cambi;
    }
}
