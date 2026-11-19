using Xunit;

namespace ProgettoMappe.BilancioIdrico.Tests;

/// <summary>Task #349: il preset delle risposte di Matteo (RISPOSTE-MATTEO-2026-11-18.md), la calibrazione e lo stile dal target.</summary>
public class ComeMatteoTests
{
    private static readonly DateOnly G1 = new(2026, 6, 1);
    private static readonly Parametri M = Parametri.ComeMatteo with { PartenzaComeIlVecchio = true };

    /// <summary>Un vigneto (100) in un settore con impianto, AWC 100 mm, stile 3, BBCH 75.</summary>
    private static IngressiAzienda Azienda(int giorni = 5, double et0 = 5, IReadOnlyList<MeteoGiorno>? meteo = null) => new()
    {
        IdEnte = 1,
        Da = G1,
        A = G1.AddDays(giorni - 1),
        Vigneti = [new Vigneto(100, DistanzaFile: 2.5)],
        Settori = [new Settore(10)],
        SettoriVigneti = [new SettoreVigneto(10, 100, 100)],
        Configurazioni = [new ConfigurazioneSettore(10, new DateOnly(2026, 1, 1), null, 1, 2, null)],
        Stili = [new StileVigneto(100, 3, new DateOnly(2020, 1, 1), null)],
        Meteo = meteo ?? Enumerable.Range(0, giorni).Select(i => new MeteoGiorno(G1.AddDays(i), et0, 0)).ToList(),
        Fenologia = Enumerable.Range(0, giorni).Select(i => new FenologiaGiorno(100, G1.AddDays(i), 75)).ToList(),
        AnalisiSuolo = [new AnalisiSuolo(100, new DateOnly(2025, 3, 1), 100)],
    };

    [Fact]
    public void Preset_ValoriDelleRisposte()
    {
        var m = Parametri.ComeMatteo;
        Assert.Equal("ComeMatteo", m.Nome);
        Assert.Equal(0.5, m.FrazioneRawAwc);
        Assert.Equal(0.5, m.InizioStress);
        Assert.Equal(1, m.GestioneSuoloRipiego);
        Assert.False(m.MeteoVuotoComeZero);
        Assert.Equal(7, m.GiorniEtaFutura);
        Assert.True(m.SuggerimentoConPdlwpDiIeri);
        Assert.False(m.AvvisoAnticipatoNelSuggerimento);
        Assert.Equal(-0.5, m.Soglie[(3, 6)]);
        Assert.Equal(-0.45, Parametri.ComeOggi.Soglie[(3, 6)]);
        Assert.Null(m.KcPerFase);
        Assert.False(m.KcConBbchDelGiornoDopo);
        Assert.Same(Parametri.ComeMatteo, Parametri.Preset("comematteo"));
        Assert.Same(Parametri.ComeOggi, Parametri.Preset("altro"));
    }

    [Fact]
    public void PioggiaPerEvento_DueGiorniSommati_SoloSopraCinqueMm()
    {
        // 4 mm il 2/6 + 10 mm il 3/6 = un evento da 14 mm: contano tutti e due i giorni. 3 mm isolati il 5/6 non contano.
        double[] pioggia = [0, 4, 10, 0, 3, 0];
        var meteo = pioggia.Select((mm, i) => new MeteoGiorno(G1.AddDays(i), 5, mm)).ToList();
        var r = MotoreBilancio.Calcola(Azienda(6, meteo: meteo), M).Vigneti;
        Assert.Equal([0, 4, 10, 0, 0, 0], r.Select(x => x.PioggiaMm ?? -1).ToArray());

        // Col vecchio modo (soglia sul giorno) i 4 mm si perdono.
        var giorno = MotoreBilancio.Calcola(Azienda(6, meteo: meteo), M with { PioggiaPerEvento = false }).Vigneti;
        Assert.Equal(0, giorno[1].PioggiaMm);
    }

    [Fact]
    public void MeteoVuoto_AnomaliaNonZero()
    {
        var meteo = new List<MeteoGiorno> { new(G1, 5, 0), new(G1.AddDays(1), null, 0), new(G1.AddDays(2), 5, 0) };
        var r = MotoreBilancio.Calcola(Azienda(3, meteo: meteo), M).Vigneti;
        Assert.True(r[1].MeteoMancante);
        Assert.Equal(3, r[1].Priorita);
        Assert.Null(r[1].EtaMm);
        Assert.False(MotoreBilancio.Calcola(Azienda(3, meteo: meteo), Parametri.ComeOggi with { PartenzaComeIlVecchio = true }).Vigneti[1].MeteoMancante);
    }

    [Fact]
    public void SuoloMedioDelSettore_MediaDeiVignetiEScheletro()
    {
        // Due vigneti nello stesso settore: acqua 100 e 200 mm/m, scheletro 10 % e 30 %, profondità 1 m. Media: 150 e 20 %.
        var az = Azienda() with
        {
            Vigneti = [new Vigneto(100), new Vigneto(200)],
            SettoriVigneti = [new SettoreVigneto(10, 100, 60), new SettoreVigneto(10, 200, 100)],
            AnalisiSuolo =
            [
                new AnalisiSuolo(100, new DateOnly(2020, 1, 1), 100, 1, 10),
                new AnalisiSuolo(200, new DateOnly(2026, 5, 1), 200, 1, 30),
            ],
        };
        var r = MotoreBilancio.Calcola(az, M);
        Assert.All(r.Vigneti, v => Assert.Equal(150 * 0.8, v.AwcMm!.Value, 1e-9));
        var suolo = r.Suoli.Single(s => s.IdVigneto == 100);
        Assert.Equal(FontiValore.Settore, suolo.FonteAcqua);
        Assert.Equal(FontiValore.Settore, suolo.FonteScheletro);
        Assert.Equal(FontiValore.Analisi, suolo.FonteProfondita);
        Assert.Equal(2, suolo.VignetiNelSettore);

        // Senza settore: le analisi del vigneto, tutte (non solo l'ultima).
        var solo = az with { SettoriVigneti = [], AnalisiSuolo = [new(100, new(2020, 1, 1), 100, 1, 10), new(100, new(2026, 1, 1), 200, 1, 30)] };
        var s = MotoreBilancio.Calcola(solo, M).Suoli.Single(x => x.IdVigneto == 100);
        Assert.Equal(150, s.AcquaDisponibileMmM);
        Assert.Equal(20, s.ScheletroPerc!.Value, 1e-9);
        Assert.Equal(FontiValore.Analisi, s.FonteScheletro);
    }

    [Fact]
    public void Calibrazione_ScheletroERadiciVinconoSuAnalisiERilievo()
    {
        var az = Azienda() with
        {
            Vigneti = [new Vigneto(100, DistanzaFile: 2.5, ScheletroManualePerc: 50, ProfonditaRadiciManualeM: 0.8)],
            Radici = [new RilievoRadici(100, new DateOnly(2025, 1, 1), 1.25, 0.6)],
        };
        var r = MotoreBilancio.Calcola(az, M);
        // 100 mm/m × 1 m × (100 − 50) % × (1,25 / 2,5 × 0,8) = 20 mm.
        Assert.Equal(20, r.Vigneti[0].AwcMm!.Value, 1e-9);
        var s = r.Suoli.Single();
        Assert.Equal(FontiValore.Calibrazione, s.FonteScheletro);
        Assert.Equal(FontiValore.Calibrazione, s.FonteRadici);
        Assert.Equal(1.25, s.LarghezzaRadiciM);
        Assert.Equal(0.8, s.ProfonditaRadiciM);
        Assert.Equal(20, s.AwcMm!.Value, 1e-9);
    }

    [Fact]
    public void StileDalTarget_SoloSenzaStileConfigurato()
    {
        var az = Azienda() with { Stili = [], Vigneti = [new Vigneto(100, IdTargetConcimazione: 6)] };
        Assert.Null(MotoreBilancio.Calcola(az, M).Vigneti[0].IdStileVino);
        var conTabella = M with { StileDaTarget = new Dictionary<int, int> { [6] = 5 } };
        var r = MotoreBilancio.Calcola(az, conTabella);
        Assert.Equal(5, r.Vigneti[0].IdStileVino);
        Assert.Equal(FontiValore.Target, r.Suoli[0].FonteStile);
        Assert.Equal(3, MotoreBilancio.Calcola(Azienda() with { Vigneti = [new Vigneto(100, IdTargetConcimazione: 6)] }, conTabella).Vigneti[0].IdStileVino);
    }

    [Fact]
    public void Asciutta_SenzaSuggerimento_SenzaSettoreSi()
    {
        var asciutta = Azienda() with { Configurazioni = [new ConfigurazioneSettore(10, new DateOnly(2026, 1, 1), null, 0, null, null)] };
        Assert.All(MotoreBilancio.Calcola(asciutta, M).Vigneti, v => Assert.Equal(Suggerimento.NonApplicabile, v.Suggerimento));
        var senzaSettore = Azienda() with { SettoriVigneti = [], Configurazioni = [] };
        Assert.All(MotoreBilancio.Calcola(senzaSettore, M).Vigneti, v => Assert.NotEqual(Suggerimento.NonApplicabile, v.Suggerimento));
    }

    [Fact]
    public void Efficienza_AncheSulleOreDelSettore()
    {
        // 5 ore × 2 mm/ora × 0,90 (goccia fuori suolo) = 9 mm.
        var az = Azienda() with { Irrigazioni = [new EventoIrrigazione(G1, 10, null, DurataOre: 5)] };
        Assert.Equal(9, MotoreBilancio.Calcola(az, M).Vigneti[0].IrrigazioneMm, 1e-9);
        Assert.Equal(10, MotoreBilancio.Calcola(az, Parametri.ComeOggi with { PartenzaComeIlVecchio = true }).Vigneti[0].IrrigazioneMm, 1e-9);
    }
}
