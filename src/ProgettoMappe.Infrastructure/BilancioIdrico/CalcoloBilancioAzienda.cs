using ProgettoMappe.BilancioIdrico;

namespace ProgettoMappe.Infrastructure.BilancioIdrico;

/// <summary>
/// Mette insieme dati di campo (4Grapes) e configurazione (Mappe) negli ingressi del motore. Calcolo puro, testato.
/// </summary>
public static class ComposizioneIngressi
{
    /// <summary>
    /// Vigneti calcolati = quelli attivi dell'azienda più quelli che stanno in un settore della configurazione (anche se in
    /// 4Grapes non risultano attivi: lo dice l'anomalia). I settori non attivi non entrano.
    /// </summary>
    public static IngressiAzienda Componi(DatiCampoBilancio campo, ConfigurazioneAzienda conf, DateOnly da, DateOnly a)
    {
        var settori = conf.Settori.Where(s => s.Attivo).Select(s => s.IdSettore).ToHashSet();
        var quote = conf.Quote.Where(q => settori.Contains(q.IdSettore)).ToList();
        var campoPerId = campo.Vigneti.ToDictionary(v => v.IdVigneto);
        var idVigneti = campo.Vigneti.Where(v => v.Attivo).Select(v => v.IdVigneto).Concat(quote.Select(q => q.IdVigneto)).Distinct().Order();
        var calibrazioni = conf.Calibrazioni.GroupBy(c => c.IdVigneto).ToDictionary(g => g.Key, g => g.Last());
        var target = TargetVigneto.PerVigneto(campo.Target, a.Year);
        Vigneto Vigneto(int id)
        {
            var v = campoPerId.GetValueOrDefault(id);
            var c = calibrazioni.GetValueOrDefault(id);
            return new Vigneto(id, v?.TraFila, v?.SuperficieM2, c?.AwcMm, c?.ScheletroPerc, c?.ProfonditaRadiciM, c?.LarghezzaRadiciM,
                target.TryGetValue(id, out var t) ? t : null);
        }
        return new IngressiAzienda
        {
            IdEnte = campo.IdEnte,
            Da = da,
            A = a,
            Vigneti = idVigneti.Select(Vigneto).ToList(),
            Settori = settori.Order().Select(id => new Settore(id)).ToList(),
            SettoriVigneti = quote.Select(q => new SettoreVigneto(q.IdSettore, q.IdVigneto, q.Percentuale)).ToList(),
            Configurazioni = conf.Impianti.Where(i => settori.Contains(i.IdSettore))
                .Select(i => new ConfigurazioneSettore(i.IdSettore, i.ValidoDal, i.ValidoAl, i.IdTipologiaImpianto, i.TassoMmOra, i.EfficienzaReale))
                .ToList(),
            ShiftSettori = conf.SettoriShift.Where(s => settori.Contains(s.IdSettore) && conf.Shift.Any(x => x.IdShift == s.IdShift && x.Attivo))
                .Select(s => new ShiftSettore(s.IdShift, s.IdSettore, s.ValidoDal, s.ValidoAl)).ToList(),
            Stili = conf.Stili.Select(s => new StileVigneto(s.IdVigneto, s.IdStileVino, s.ValidoDal, s.ValidoAl)).ToList(),
            Meteo = campo.Meteo,
            Fenologia = campo.Fenologia,
            AnalisiSuolo = campo.AnalisiSuolo,
            Radici = campo.Radici,
            GestioniSuolo = campo.GestioniSuolo,
            Irrigazioni = conf.Irrigazioni.Where(i => i.IdEnte == campo.IdEnte)
                .Select(i => new EventoIrrigazione(i.Data, i.IdSettore, i.IdShift, i.DurataOre, i.VolumeMm, i.Efficienza, i.VolumeUtileMm))
                .ToList(),
        };
    }

    /// <summary>Il preset (predefinito <see cref="Parametri.ComeOggi"/>) con le tabelle del modello della configurazione.</summary>
    public static Parametri Parametri(ConfigurazioneAzienda conf, Parametri? preset = null)
    {
        var p = preset ?? ProgettoMappe.BilancioIdrico.Parametri.ComeOggi;
        return conf.Modello?.Applica(p) ?? p;
    }
}

/// <summary>Totali e anomalie dei dati di un calcolo (task #333, punto D; base del ricalcolo notturno della fase 3).</summary>
public sealed record RiepilogoCalcolo
{
    public required int IdEnte { get; init; }
    public required DateOnly Da { get; init; }
    public required DateOnly A { get; init; }
    public int Vigneti { get; init; }
    public int VignetiInSettore { get; init; }
    public int Settori { get; init; }
    public int GiorniVigneto { get; init; }
    public double Et0Mm { get; init; }
    public double PioggiaMm { get; init; }
    public double EtaMediaMm { get; init; }
    public double IrrigazioneMediaMm { get; init; }

    /// <summary>Vigneti con suggerimento IRRIGARE nell'ultimo giorno.</summary>
    public int DaIrrigareUltimoGiorno { get; init; }

    public int GiorniSenzaMeteo { get; init; }
    public IReadOnlyList<int> SenzaSuolo { get; init; } = [];
    public IReadOnlyList<int> SenzaStile { get; init; } = [];
    public IReadOnlyList<int> SenzaFenologia { get; init; } = [];
    public IReadOnlyList<int> SenzaRadici { get; init; } = [];
    public IReadOnlyList<int> InSettoreMaNonAttivi { get; init; } = [];
    public IReadOnlyList<string> Controlli { get; init; } = [];
    public IReadOnlyList<string> FontiMancanti { get; init; } = [];

    /// <summary>Il preset dei parametri usato (<see cref="Parametri.Nome"/>).</summary>
    public string Preset { get; init; } = ProgettoMappe.BilancioIdrico.Parametri.NomeComeOggi;

    /// <summary>
    /// Task #349: vigneti con il suggerimento di <see cref="GiornoConfronto"/> diverso da quello che darebbe
    /// <see cref="Parametri.ComeOggi"/> (stesse tabelle del modello); null se il preset è già ComeOggi.
    /// </summary>
    public IReadOnlyList<CambioSuggerimento>? CambiRispettoAComeOggi { get; init; }

    public DateOnly? GiornoConfronto { get; init; }

    /// <summary>I valori di suolo, radici e stile usati per ogni vigneto, con le fonti (scheda del vigneto).</summary>
    public IReadOnlyList<SuoloVigneto> Suoli { get; init; } = [];

    public static RiepilogoCalcolo Calcola(DatiCampoBilancio campo, IngressiAzienda ingressi, RisultatoBilancio risultato, Parametri p)
    {
        var righe = risultato.Vigneti;
        var perVigneto = righe.GroupBy(r => r.IdVigneto).ToList();
        var giorni = righe.Select(r => r.Data).Distinct().ToList();
        var ultimo = giorni.Count > 0 ? giorni.Max() : ingressi.A;
        var attivi = campo.Vigneti.Where(v => v.Attivo).Select(v => v.IdVigneto).ToHashSet();
        var conRadici = campo.Radici.Select(r => r.IdVigneto).ToHashSet();
        var meteoPerGiorno = righe.GroupBy(r => r.Data).ToDictionary(g => g.Key, g => g.First());
        return new RiepilogoCalcolo
        {
            IdEnte = ingressi.IdEnte,
            Da = ingressi.Da,
            A = ingressi.A,
            Vigneti = perVigneto.Count,
            VignetiInSettore = ingressi.SettoriVigneti.Select(s => s.IdVigneto).Distinct().Count(),
            Settori = ingressi.Settori.Count,
            GiorniVigneto = righe.Count,
            Et0Mm = meteoPerGiorno.Values.Sum(r => r.Et0Mm ?? 0),
            PioggiaMm = meteoPerGiorno.Values.Sum(r => r.PioggiaMm ?? 0),
            EtaMediaMm = perVigneto.Count == 0 ? 0 : perVigneto.Average(g => g.Sum(r => r.EtaMm ?? 0)),
            IrrigazioneMediaMm = perVigneto.Count == 0 ? 0 : perVigneto.Average(g => g.Sum(r => r.IrrigazioneMm)),
            DaIrrigareUltimoGiorno = righe.Count(r => r.Data == ultimo && r.Suggerimento == Suggerimento.Irrigare),
            GiorniSenzaMeteo = meteoPerGiorno.Values.Count(r => r.MeteoMancante),
            SenzaSuolo = perVigneto.Where(g => g.All(r => r.SuoloMancante)).Select(g => g.Key).Order().ToList(),
            SenzaStile = perVigneto.Where(g => g.All(r => r.IdStileVino is null)).Select(g => g.Key).Order().ToList(),
            SenzaFenologia = perVigneto.Where(g => g.All(r => r.FenologiaMancante)).Select(g => g.Key).Order().ToList(),
            SenzaRadici = perVigneto.Select(g => g.Key).Where(id => !conRadici.Contains(id)).Order().ToList(),
            InSettoreMaNonAttivi = ingressi.SettoriVigneti.Select(s => s.IdVigneto).Distinct().Where(id => !attivi.Contains(id)).Order().ToList(),
            Controlli = MotoreBilancio.Controlla(ingressi, p),
            FontiMancanti = campo.FontiMancanti,
            Preset = p.Nome,
            Suoli = risultato.Suoli,
        };
    }

    /// <summary>I vigneti con un suggerimento diverso fra due calcoli nel giorno <paramref name="giorno"/>.</summary>
    public static List<CambioSuggerimento> Cambi(RisultatoBilancio prima, RisultatoBilancio dopo, DateOnly giorno)
    {
        var vecchi = prima.Vigneti.Where(r => r.Data == giorno).ToDictionary(r => r.IdVigneto, r => r.Suggerimento);
        return dopo.Vigneti.Where(r => r.Data == giorno && vecchi.TryGetValue(r.IdVigneto, out var s) && s != r.Suggerimento)
            .Select(r => new CambioSuggerimento(r.IdVigneto, vecchi[r.IdVigneto].Codice(), r.Suggerimento.Codice()))
            .OrderBy(c => c.IdVigneto).ToList();
    }
}

/// <summary>Un vigneto che cambia suggerimento passando da ComeOggi al preset in uso.</summary>
public sealed record CambioSuggerimento(int IdVigneto, string ComeOggi, string Preset);

/// <summary>
/// «Calcola azienda X stagione Y» (task #333, punto D): legge i dati di campo da 4Grapes e la configurazione da Mappe e
/// lancia il motore sulla stagione intera (REGOLE.md: niente ricalcoli a pezzi).
/// </summary>
public sealed class CalcoloBilancioAzienda(IFonteBilancio fonte, ArchivioConfigurazioneBilancio archivio, Parametri? preset = null)
{
    /// <summary>Il preset dei parametri (<c>BilancioIdrico:Preset</c> dell'Api): ComeOggi se non indicato.</summary>
    public Parametri Preset { get; } = preset ?? ProgettoMappe.BilancioIdrico.Parametri.ComeOggi;

    /// <summary>Giorni di previsione oltre oggi, nella stagione in corso.</summary>
    public const int GiorniPrevisione = 7;

    public sealed record Esito(IngressiAzienda Ingressi, Parametri Parametri, RisultatoBilancio Risultato, RiepilogoCalcolo Riepilogo,
        DatiCampoBilancio Campo);

    /// <summary>Stagione <paramref name="anno"/>: dal 1/1 al 31/12, o fino a oggi + <see cref="GiorniPrevisione"/> se è in corso.</summary>
    public static (DateOnly Da, DateOnly A) Stagione(int anno, DateOnly oggi)
    {
        var fine = new DateOnly(anno, 12, 31);
        var a = oggi.Year == anno ? oggi.AddDays(GiorniPrevisione) : fine;
        return (new DateOnly(anno, 1, 1), a < fine ? a : fine);
    }

    public Task<Esito> CalcolaAsync(int idEnte, int anno, CancellationToken ct = default)
    {
        var (da, a) = Stagione(anno, DateOnly.FromDateTime(DateTime.Today));
        return CalcolaAsync(idEnte, da, a, ct);
    }

    public async Task<Esito> CalcolaAsync(int idEnte, DateOnly da, DateOnly a, CancellationToken ct = default)
    {
        var campo = await fonte.LeggiAsync(idEnte, new DateOnly(da.Year, 1, 1), a, ct);
        var conf = await archivio.LeggiAsync(idEnte, ct);
        var ingressi = ComposizioneIngressi.Componi(campo, conf, da, a);
        var p = ComposizioneIngressi.Parametri(conf, Preset);
        var risultato = MotoreBilancio.Calcola(ingressi, p);
        var riepilogo = RiepilogoCalcolo.Calcola(campo, ingressi, risultato, p);
        if (p.Nome != ProgettoMappe.BilancioIdrico.Parametri.NomeComeOggi)
        {
            // Task #349: quanti vigneti cambiano suggerimento rispetto a ComeOggi (il motore impiega pochi millisecondi).
            var giorno = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Rome"));
            giorno = giorno > a ? a : giorno < da ? da : giorno;
            var comeOggi = MotoreBilancio.Calcola(ingressi, ComposizioneIngressi.Parametri(conf));
            riepilogo = riepilogo with { CambiRispettoAComeOggi = RiepilogoCalcolo.Cambi(comeOggi, risultato, giorno), GiornoConfronto = giorno };
        }
        return new Esito(ingressi, p, risultato, riepilogo, campo);
    }
}
