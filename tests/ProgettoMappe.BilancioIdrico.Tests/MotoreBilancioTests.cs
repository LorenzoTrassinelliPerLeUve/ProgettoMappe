using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>Le regole di REGOLE.md §§ 1–3 una per una, su un'azienda finta piccola, e l'effetto dei parametri.</summary>
public class MotoreBilancioTests
{
    private static readonly DateOnly G1 = new(2026, 6, 1);
    private static readonly Parametri P = Parametri.ComeOggi;

    /// <summary>Un settore irriguo (tasso 2 mm/ora, goccia fuori suolo 0,90) con un vigneto al 100 %, AWC 100 mm, stile 3.</summary>
    private static IngressiAzienda Azienda(int giorni = 5, double et0 = 5, int bbch = 75, double? awcMmM = 100) => new()
    {
        IdEnte = 1,
        Da = G1,
        A = G1.AddDays(giorni - 1),
        Settori = [new Settore(10)],
        SettoriVigneti = [new SettoreVigneto(10, 100, 100)],
        Configurazioni = [new ConfigurazioneSettore(10, new DateOnly(2026, 1, 1), null, 1, 2, null)],
        Stili = [new StileVigneto(100, 3, new DateOnly(2020, 1, 1), null)],
        Meteo = Enumerable.Range(0, giorni).Select(i => new MeteoGiorno(G1.AddDays(i), et0, 0)).ToList(),
        Fenologia = Enumerable.Range(0, giorni).Select(i => new FenologiaGiorno(100, G1.AddDays(i), bbch)).ToList(),
        AnalisiSuolo = awcMmM is null ? [] : [new AnalisiSuolo(100, new DateOnly(2025, 3, 1), awcMmM)],
    };

    private static readonly Parametri ComeIlVecchio = P with { PartenzaComeIlVecchio = true };

    [Theory]
    [InlineData(1.0, -0.010)]
    [InlineData(0.5, -0.140)]
    [InlineData(0.3, -0.236)]
    [InlineData(0.1, -0.441)]
    public void Pdlwp_EsempiDelleRegole(double faw, double atteso) => Assert.Equal(atteso, Formule.Pdlwp(faw, P)!.Value, 0.0006);

    [Fact]
    public void Pdlwp_SuoloVuoto_Vuoto() => Assert.Null(Formule.Pdlwp(0, P));

    [Theory]
    [InlineData(61, 2, 0.624545)] // fra 10 (0,30) e 65 (0,65)
    [InlineData(65, 2, 0.65)]
    [InlineData(93, 1, 0.50)] // fra 89 (0,70) e 97 (0,30)
    [InlineData(99, 3, 0.25)] // dopo l'ultimo punto
    [InlineData(0, 1, 0.30)]
    public void Kc_Interpolato(int bbch, int gestione, double atteso) => Assert.Equal(atteso, Formule.Kc(bbch, gestione, P)!.Value, 0.000001);

    [Fact]
    public void Kc_SenzaBbchOGestione_Vuoto()
    {
        Assert.Null(Formule.Kc(null, 2, P));
        Assert.Null(Formule.Kc(65, null, P));
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(10, 1)]
    [InlineData(54, 1)]
    [InlineData(55, 2)]
    [InlineData(61, 3)]
    [InlineData(71, 3)]
    [InlineData(72, 4)]
    [InlineData(79, 5)]
    [InlineData(85, 6)]
    [InlineData(90, 6)]
    [InlineData(91, 7)]
    public void Fase_DaBbch(int bbch, int fase) => Assert.Equal(fase, Formule.Fase(bbch, P));

    [Fact]
    public void Awc_ConRadiciProfonditaEPietrosita()
    {
        var analisi = new AnalisiSuolo(1, G1, 150, ProfonditaSuolo: 0.8, Pietrosita: 20);
        var radici = new RilievoRadici(1, G1, 0.6, 0.5);
        // 150 × 0,8 × 80 % × (0,6 / 2,4 × 0,5)
        Assert.Equal(150 * 0.8 * 0.8 * 0.125, Formule.Awc(analisi, radici, 2.4, P)!.Value, 1e-9);
        // Senza distanza fra le file il fattore radici è 1; profondità 0 = 1 metro.
        Assert.Equal(150 * 0.8, Formule.Awc(analisi with { ProfonditaSuolo = 0 }, radici, null, P)!.Value, 1e-9);
        Assert.Null(Formule.Awc(null, radici, 2.4, P));
    }

    [Fact]
    public void Ks_SottoLaSogliaDiStress()
    {
        Assert.Equal(1, Formule.Ks(60, 100, P));
        Assert.Equal(50 / 55.0, Formule.Ks(50, 100, P)!.Value, 1e-12);
        Assert.Null(Formule.Ks(50, 0, P));
    }

    [Fact]
    public void Deficit_FinoAllaFawDellaSoglia()
    {
        var obiettivo = Formule.FawObiettivo(-0.2, P); // 0,363
        Assert.Equal(-0.2, Formule.Pdlwp(obiettivo, P)!.Value, 1e-12);
        Assert.Equal(0, Formule.Deficit(obiettivo + 0.01, 100, -0.2, P));
        Assert.Equal((obiettivo - 0.2) * 100, Formule.Deficit(0.2, 100, -0.2, P)!.Value, 1e-9);
        Assert.Equal(0, Formule.Deficit(0.2, 100, -0.4, P)); // con soglia −0,4 la FAW 0,2 basta
        Assert.Null(Formule.Deficit(0.2, 100, null, P));
    }

    [Fact]
    public void Bilancio_AwConIFlussiDiIeriEEtaFuturaDiDueGiorni()
    {
        var r = MotoreBilancio.Calcola(Azienda(), ComeIlVecchio).Vigneti;

        // Kc BBCH 75 lavorato = 0,65; ETa = 5 × 0,65 × 1 = 3,25.
        Assert.Equal(100, r[0].AwMm);
        Assert.Equal(3.25, r[0].EtaMm!.Value, 1e-9);
        Assert.Equal(100 - 3.25, r[1].AwMm, 1e-9);
        Assert.Equal(6.5, r[0].EtaFuturaMm, 1e-9);
        Assert.Equal(3.25, r[3].EtaFuturaMm, 1e-9); // penultimo giorno: un giorno solo dopo
        Assert.Equal(0, r[4].EtaFuturaMm);
        // Fase 4 stile 3 = −0,2 MPa; con FAW alta niente deficit, NON_IRRIGARE, ma il volume c'è lo stesso.
        Assert.Equal(-0.2, r[0].SogliaMpa);
        Assert.Equal(Suggerimento.NonIrrigare, r[0].Suggerimento);
        Assert.Equal(3.25 + 6.5, r[0].IrrigazioneSuggeritaMm, 1e-9);
        Assert.Equal((3.25 + 6.5) / (2 * 0.90), r[0].IrrigazioneSuggeritaOre!.Value, 1e-9);
        Assert.Equal(0, r[0].Priorita);
    }

    [Fact]
    public void Bilancio_AcquaFraZeroEAwc()
    {
        var ingressi = Azienda(giorni: 3, awcMmM: 10) with
        {
            Meteo = [new(G1, 5, 50), new(G1.AddDays(1), 5, 0), new(G1.AddDays(2), 5, 0)],
        };
        var r = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti;
        Assert.Equal(10, r[1].AwMm); // 10 + 50 − 3,25 limitata all'AWC
        Assert.True(r[2].AwMm < 10);
    }

    [Fact]
    public void Suggerimento_IrrigareSottoLaSogliaConCriticita()
    {
        // Senza pioggia per 40 giorni con ET0 alta il suolo si svuota.
        var r = MotoreBilancio.Calcola(Azienda(giorni: 40, et0: 8), ComeIlVecchio).Vigneti;
        var critico = r.First(x => x.Suggerimento == Suggerimento.Irrigare);
        Assert.True(critico.PdlwpMpa <= -0.2);
        Assert.True(critico.CriticitaPdlwp);
        Assert.True(critico.CriticitaDeficit);
        Assert.Equal(2, critico.Priorita);
        Assert.True(critico.DeficitMm > 0);
        Assert.True(r.First(x => x.CriticitaPdlwp).Data < critico.Data, "la criticità arriva 0,04 MPa prima della soglia");
    }

    [Fact]
    public void Suggerimento_FaseZeroNonIrrigareSenzaStileNA()
    {
        Assert.All(MotoreBilancio.Calcola(Azienda(bbch: 5), ComeIlVecchio).Vigneti,
            x => Assert.Equal(Suggerimento.NonIrrigare, x.Suggerimento));
        Assert.All(MotoreBilancio.Calcola(Azienda() with { Stili = [] }, ComeIlVecchio).Vigneti,
            x => Assert.Equal(Suggerimento.NonApplicabile, x.Suggerimento));
        Assert.All(MotoreBilancio.Calcola(Azienda() with { Stili = [] }, ComeIlVecchio with { StileVinoRipiego = 3 }).Vigneti,
            x => Assert.Equal(Suggerimento.NonIrrigare, x.Suggerimento));
    }

    [Fact]
    public void Suggerimento_SuoloAZero()
    {
        var senzaAcqua = Azienda(awcMmM: 0);
        var r = MotoreBilancio.Calcola(senzaAcqua, ComeIlVecchio).Vigneti;
        Assert.All(r, x => Assert.Equal(Suggerimento.Irrigare, x.Suggerimento));
        Assert.All(r, x => Assert.Equal(0, x.EtaMm)); // Ks vuoto conta 0
        r = MotoreBilancio.Calcola(senzaAcqua, ComeIlVecchio with { SuoloVuotoIrrigare = false }).Vigneti;
        Assert.All(r, x => Assert.Equal(Suggerimento.DatoNonValido, x.Suggerimento));
    }

    [Fact]
    public void SuoloMancante_SoloSenzaAnalisi()
    {
        var senza = MotoreBilancio.Calcola(Azienda(awcMmM: null), ComeIlVecchio).Vigneti;
        Assert.All(senza, x => Assert.True(x.SuoloMancante && x.Priorita == 3 && x.AwcMm is null));

        var vuota = Azienda() with { AnalisiSuolo = [new AnalisiSuolo(100, G1, null)] };
        Assert.All(MotoreBilancio.Calcola(vuota, ComeIlVecchio).Vigneti, x => Assert.False(x.SuoloMancante));
        Assert.All(MotoreBilancio.Calcola(vuota, ComeIlVecchio with { AwcVuotaComeSuoloMancante = true }).Vigneti,
            x => Assert.True(x.SuoloMancante));
    }

    [Fact]
    public void Asciutta_SuggerimentoComeGliAltriOppureNA()
    {
        var ingressi = Azienda(giorni: 40, et0: 8) with
        {
            Settori = [new Settore(10, Asciutta: true)],
            Configurazioni = [new ConfigurazioneSettore(10, new DateOnly(2000, 1, 1), null, 0, null, 1)],
        };
        var r = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti;
        Assert.Contains(r, x => x.Suggerimento == Suggerimento.Irrigare);
        Assert.All(r, x => Assert.Null(x.IrrigazioneSuggeritaOre));
        Assert.All(MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { SuggerimentoInAsciutta = false }).Vigneti,
            x => Assert.Equal(Suggerimento.NonApplicabile, x.Suggerimento));
    }

    [Fact]
    public void Meteo_VuotoZeroOppureMancante()
    {
        var ingressi = Azienda(giorni: 2) with { Meteo = [new(G1, null, null)] };
        var r = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti;
        Assert.Equal(0, r[0].Et0Mm);
        Assert.False(r[0].MeteoMancante);
        Assert.True(r[1].MeteoMancante); // nessuna riga
        Assert.Null(r[1].EtaMm);
        Assert.Equal(3, r[1].Priorita);

        r = MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { MeteoVuotoComeZero = false }).Vigneti;
        Assert.True(r[0].MeteoMancante);
        Assert.Null(r[0].EtaMm);
    }

    [Fact]
    public void Fenologia_StoricoRiportoPrevisione()
    {
        var ingressi = Azienda(giorni: 6) with
        {
            Fenologia =
            [
                new(100, G1.AddDays(1), 60), new(100, G1.AddDays(3), 65),
                new(100, G1.AddDays(2), 99, FonteFenologia.Previsione), // ignorata: c'è il riporto
                new(100, G1.AddDays(5), 68, FonteFenologia.Previsione),
            ],
            Meteo = Azienda(giorni: 6).Meteo.Where(m => m.Data != G1.AddDays(2)).ToList(),
        };
        var bbch = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti.Select(x => x.Bbch).ToArray();
        // giorno 0 prima dello storico; 2 senza meteo: niente riporto; 4 dopo l'ultimo storico senza previsione.
        Assert.Equal(new int?[] { null, 60, null, 65, null, 68 }, bbch);

        ingressi = ingressi with { Meteo = Azienda(giorni: 6).Meteo };
        Assert.Equal(60, MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti[2].Bbch);
    }

    [Fact]
    public void Gestione_RipiegoLavorato()
    {
        var r = MotoreBilancio.Calcola(Azienda(), ComeIlVecchio).Vigneti[0];
        Assert.Equal(2, r.IdGestioneSuolo);
        var inerbito = Azienda() with { GestioniSuolo = [new RilievoGestioneSuolo(100, new DateOnly(2027, 1, 1), 1)] };
        Assert.Equal(1, MotoreBilancio.Calcola(inerbito, ComeIlVecchio).Vigneti[0].IdGestioneSuolo); // anche se futura
        Assert.Equal(2, MotoreBilancio.Calcola(inerbito, ComeIlVecchio with { RilieviVignetoPiuRecenti = false }).Vigneti[0].IdGestioneSuolo);
        Assert.True(MotoreBilancio.Calcola(Azienda(), ComeIlVecchio with { GestioneSuoloRipiego = null }).Vigneti[0].KcMancante);
    }

    [Fact]
    public void Irrigazione_SettoreEShift()
    {
        var ingressi = Azienda(giorni: 3) with
        {
            SettoriVigneti = [new SettoreVigneto(10, 100, 40), new SettoreVigneto(10, 101, 60)],
            ShiftSettori = [new ShiftSettore(7, 10, G1, null)],
            Irrigazioni =
            [
                new EventoIrrigazione(G1, 10, null, VolumeUtileMm: 10, VolumeMm: 99),
                new EventoIrrigazione(G1, 10, null, VolumeMm: 10, Efficienza: 0.5),
                new EventoIrrigazione(G1.AddDays(1), 10, null, DurataOre: 3),
                new EventoIrrigazione(G1.AddDays(2), null, 7, DurataOre: 5),
            ],
        };
        var r = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti.Where(x => x.IdVigneto == 100).ToList();
        Assert.Equal((10 + 5) * 0.4, r[0].IrrigazioneMm, 1e-9);
        Assert.Equal(3 * 2 * 0.4, r[1].IrrigazioneMm, 1e-9); // ore × tasso
        Assert.Equal(5 * 2 * 0.4, r[2].IrrigazioneMm, 1e-9); // shift: senza efficienza
        var conEfficienza = MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { EfficienzaSuShift = true }).Vigneti;
        Assert.Equal(5 * 2 * 0.9 * 0.4, conEfficienza.Single(x => x.IdVigneto == 100 && x.Data == G1.AddDays(2)).IrrigazioneMm, 1e-9);
    }

    [Fact]
    public void SettoreEShift_Aggregati()
    {
        var ingressi = Azienda(giorni: 2) with
        {
            Settori = [new Settore(10), new Settore(11)],
            SettoriVigneti = [new SettoreVigneto(10, 100, 50), new SettoreVigneto(10, 101, 50), new SettoreVigneto(11, 100, 100)],
            Configurazioni = [new(10, G1, null, 1, 2, null), new(11, G1, null, 1, 4, 1)],
            ShiftSettori = [new ShiftSettore(7, 10, G1, null), new ShiftSettore(7, 11, G1, G1)],
            AnalisiSuolo = [new AnalisiSuolo(100, G1, 100)],
        };
        var risultato = MotoreBilancio.Calcola(ingressi, ComeIlVecchio);
        var v = risultato.Vigneti.Where(x => x.Data == G1 && x.IdSettore == 10).ToList();
        var s10 = risultato.Settori.Single(s => s.IdSettore == 10 && s.Data == G1);
        Assert.Equal(v.Sum(x => x.IrrigazioneSuggeritaMm) / 2, s10.IrrigazioneSuggeritaMm, 1e-9);
        Assert.Equal(3, s10.Priorita); // il vigneto 101 non ha il suolo

        var turni = risultato.Shift;
        Assert.Equal(2, turni.Single(t => t.Data == G1).SettoriCoinvolti);
        Assert.Equal(1, turni.Single(t => t.Data == G1.AddDays(1)).SettoriCoinvolti);
        var s11 = risultato.Settori.Single(s => s.IdSettore == 11 && s.Data == G1);
        Assert.Equal(Math.Max(s10.IrrigazioneSuggeritaOre!.Value, s11.IrrigazioneSuggeritaOre!.Value),
            turni.Single(t => t.Data == G1).OreComando!.Value, 1e-9);
    }

    [Fact]
    public void Configurazione_Mancante_OreVuotePriorita3()
    {
        var r = MotoreBilancio.Calcola(Azienda() with { Configurazioni = [] }, ComeIlVecchio).Vigneti[0];
        Assert.True(r.ConfigMancante);
        Assert.Null(r.IrrigazioneSuggeritaOre);
        Assert.Equal(3, r.Priorita);
    }

    [Fact]
    public void Stagione_ContinuaDalPrimoGennaio()
    {
        // Da giugno, ma con meteo e fenologia da gennaio: il nuovo parte dal 1/1 e a giugno il suolo non è più pieno.
        var gennaio = new DateOnly(2026, 1, 1);
        var giorni = G1.DayNumber - gennaio.DayNumber + 5;
        var ingressi = Azienda() with
        {
            Meteo = Enumerable.Range(0, giorni).Select(i => new MeteoGiorno(gennaio.AddDays(i), 3, 0)).ToList(),
            Fenologia = Enumerable.Range(0, giorni).Select(i => new FenologiaGiorno(100, gennaio.AddDays(i), 75)).ToList(),
        };
        var nuovo = MotoreBilancio.Calcola(ingressi).Vigneti;
        var vecchio = MotoreBilancio.Calcola(ingressi, ComeIlVecchio).Vigneti;
        Assert.Equal(G1, nuovo[0].Data);
        Assert.Equal(5, nuovo.Count);
        Assert.True(nuovo[0].AwMm < 10);
        Assert.Equal(100, vecchio[0].AwMm);
    }

    [Fact]
    public void Parametri_DomandeAllAgronomo()
    {
        var ingressi = Azienda(giorni: 6);
        // Domanda 3: ETa futura su 3 giorni.
        Assert.Equal(3 * 3.25, MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { GiorniEtaFutura = 3 }).Vigneti[0].EtaFuturaMm, 1e-9);
        // Domanda 2: AW a fine giornata.
        Assert.Equal(100 - 3.25, MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { AwConFlussiDiIeri = false }).Vigneti[0].AwMm, 1e-9);
        // Domanda 11: volume a zero quando non si irriga.
        Assert.All(MotoreBilancio.Calcola(ingressi, ComeIlVecchio with { AzzeraVolumeSeNonIrrigare = true }).Vigneti,
            x => Assert.Equal(0, x.IrrigazioneSuggeritaMm));
        // Domanda 1: suolo pieno fino al germogliamento.
        var germoglia = ingressi with
        {
            Fenologia = Enumerable.Range(0, 6).Select(i => new FenologiaGiorno(100, G1.AddDays(i), i < 3 ? 5 : 12)).ToList(),
        };
        var r = MotoreBilancio.Calcola(germoglia, ComeIlVecchio with { SuoloPienoFinoAlBbch = 10 }).Vigneti;
        Assert.Equal(new double[] { 100, 100, 100 }, r.Take(3).Select(x => x.AwMm));
        Assert.True(r[4].AwMm < 100);
        // Pioggia sotto 5 mm non utile.
        var pioggia = ingressi with { Meteo = [.. ingressi.Meteo.Select(m => m with { PioggiaMm = 3 })] };
        Assert.Equal(0, MotoreBilancio.Calcola(pioggia, ComeIlVecchio with { PioggiaMinimaUtileMm = 5 }).Vigneti[0].PioggiaMm);
        // Domanda 4 e § 7.6: TraFila in centimetri.
        var radici = ingressi with
        {
            Radici = [new RilievoRadici(100, G1, 1, 1)],
            Vigneti = [new VignetoImpianto(100, 250)],
        };
        Assert.Equal(100 * (1 / 2.5), MotoreBilancio.Calcola(radici, ComeIlVecchio with { DistanzaFileScala = 0.01 }).Vigneti[0].AwcMm!.Value, 1e-9);
    }
}
