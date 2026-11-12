using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class MeteoPerFaseTests
{
    [Fact]
    public void Flag_SoglieDelloScript_NullDaZero()
    {
        var r = Riga(1, 2026, 7, 1, pioggia: 10m, tmin: -2m, tmax: 30m);
        Assert.Equal(1m, MeteoPerFase.Valore(r, CampoMeteo.Pioggia2));
        Assert.Equal(1m, MeteoPerFase.Valore(r, CampoMeteo.Pioggia10));   // ≥ 10
        Assert.Equal(0m, MeteoPerFase.Valore(r, CampoMeteo.Pioggia20));
        Assert.Equal(1m, MeteoPerFase.Valore(r, CampoMeteo.Gelo0));
        Assert.Equal(0m, MeteoPerFase.Valore(r, CampoMeteo.Gelo2));       // < −2 stretto
        Assert.Equal(0m, MeteoPerFase.Valore(r, CampoMeteo.Caldo30));     // > 30 stretto
        var vuota = Riga(1, 2026, 7, 1);
        Assert.Equal(0m, MeteoPerFase.Valore(vuota, CampoMeteo.Gelo0));
        Assert.Null(MeteoPerFase.Valore(vuota, CampoMeteo.TMin));
    }

    [Fact]
    public void Aggr_TemperatureMedia_GiorniSomma_PerLivello()
    {
        List<RigaMeteo> righe =
        [
            Riga(1, 2026, 7, 1, tmax: 31m), Riga(1, 2026, 7, 2, tmax: 33m), Riga(1, 2026, 7, 3, tmax: 20m),
            Riga(2, 2026, 7, 1, tmax: 36m),
        ];
        // Per vigneto e annata: medie 28 e 36 → 32; per giorno: (31+33+20+36)/4 = 30.
        Assert.Equal(32m, MeteoPerFase.Media(MeteoPerFase.Aggr(righe, 3, 2026, 2026, CampoMeteo.TMax, LivelloAggr.VignetoAnnata)));
        Assert.Equal(30m, MeteoPerFase.Media(MeteoPerFase.Aggr(righe, 3, 2026, 2026, CampoMeteo.TMax, LivelloAggr.Giorno)));
        // Giorni > 30: vigneto 1 → 2, vigneto 2 → 1.
        Assert.Equal(1.5m, MeteoPerFase.Media(MeteoPerFase.Aggr(righe, 3, 2026, 2026, CampoMeteo.Caldo30, LivelloAggr.VignetoAnnata)));
        Assert.Empty(MeteoPerFase.Aggr(righe, 1, 2026, 2026, CampoMeteo.TMax, LivelloAggr.VignetoAnnata));
    }

    [Fact]
    public void DevSt_Campionaria()
    {
        Assert.Null(MeteoPerFase.DevSt([5m]));
        Assert.Equal(1.0, (double)MeteoPerFase.DevSt([1m, 2m, 3m])!.Value, 6);
    }

    [Fact]
    public void GiorniCaldi_ConfrontoDivisoDieci_AncheConAnnateMancanti()
    {
        // Annata 2026: 4 giorni > 30. Storico: solo 2025 con 2 giorni (le altre 9 annate mancano).
        var righe = new List<RigaMeteo>();
        for (var g = 1; g <= 4; g++)
        {
            righe.Add(Riga(1, 2026, 7, g, tmax: 32m));
        }
        righe.Add(Riga(1, 2025, 7, 1, tmax: 32m));
        righe.Add(Riga(1, 2025, 7, 2, tmax: 32m));

        var caldi = MeteoPerFase.CalcolaFase(righe, 3, 2026).GiorniTemperatura.Single(r => r.Campo == CampoMeteo.Caldo30).Valori;

        Assert.Equal(4m, caldi.Annata);
        Assert.Equal(2m, caldi.Media);                 // media delle annate presenti
        Assert.Equal(4m / (2m / 10) - 1, caldi.ScartoPct); // Qlik: somma dei 10 anni / 10
    }

    [Fact]
    public void Inverno_GiorniFreddi_ConfrontoSullaMedia()
    {
        List<RigaMeteo> righe = [Riga(1, 2026, 1, 10, tmin: -1m), Riga(1, 2026, 1, 11, tmin: -3m), Riga(1, 2025, 1, 10, tmin: -1m)];
        var fase = MeteoPerFase.CalcolaFase(righe, 1, 2026);
        var gelo0 = fase.GiorniTemperatura.Single(r => r.Campo == CampoMeteo.Gelo0).Valori;
        Assert.Equal(2m, gelo0.Annata);
        Assert.Equal(1m, gelo0.Media);
        Assert.Equal(1m, gelo0.ScartoPct);              // 2 / 1 − 1
        Assert.Equal([CampoMeteo.Gelo0, CampoMeteo.Gelo2, CampoMeteo.Gelo5], fase.GiorniTemperatura.Select(r => r.Campo));
        Assert.Empty(MeteoPerFase.CalcolaFase(righe, 2, 2026).GiorniTemperatura);
    }

    [Fact]
    public void Inverno_TmaxDellAnnataPerGiorno()
    {
        // Novembre 2025 = annata 2026, fase 1. Vigneto 1: 10 e 20 (media 15); vigneto 2: 30 → per vigneto 22,5, per giorno 20.
        List<RigaMeteo> righe = [Riga(1, 2025, 11, 1, tmax: 10m), Riga(1, 2025, 11, 2, tmax: 20m), Riga(2, 2025, 11, 1, tmax: 30m)];
        var termico = MeteoPerFase.CalcolaFase(righe, 1, 2026).Termico;
        Assert.Equal(20m, termico.Single(r => r.Campo == CampoMeteo.TMax).Valori.Annata);
        Assert.Equal(22.5m, MeteoPerFase.CalcolaFase(righe.Select(r => r with { TMed = r.TMax }).ToList(), 1, 2026)
            .Termico.Single(r => r.Campo == CampoMeteo.TMed).Valori.Annata);
    }

    [Fact]
    public void TermoPluviometrico_PioggiaInPercentuale_TemperaturaInGradi()
    {
        List<RigaMeteo> righe =
        [
            Riga(1, 2026, 6, 1, pioggia: 30m, tmed: 22m),
            Riga(1, 2025, 6, 1, pioggia: 20m, tmed: 20m),
        ];
        var tp = MeteoPerFase.CalcolaFase(righe, 3, 2026).TermoPluviometrico;
        Assert.Equal(2m, tp.TMed30.Scarto);
        Assert.Null(tp.TMed30.ScartoPct);
        Assert.Equal(0.5m, tp.Pioggia5.ScartoPct);
        Assert.Equal(30m, tp.Pioggia30.Annata);
    }

    [Fact]
    public void Giornalieri_QuattroPeriodi_PrevisioniSoloNellUltimo()
    {
        var meteo = new MeteoReportFineStagioneDto([new(1, "A", null, null, true), new(2, "B", null, null, false)],
        [
            new(1, new DateOnly(2026, 3, 1), false, 1m, 2m, 4m, 6m),
            new(2, new DateOnly(2026, 3, 1), false, 99m, 99m, 99m, 99m),   // senza meteo storico: fuori
            new(1, new DateOnly(2026, 4, 30), true, 5m, 5m, 5m, 5m),       // previsione: fuori dal primo
            new(1, new DateOnly(2026, 10, 31), true, 7m, 7m, 7m, 7m),      // previsione: dentro il quarto
            new(1, new DateOnly(2025, 3, 2), false, 1m, 1m, 1m, 1m),       // altro anno
        ], true);

        var periodi = MeteoPerFase.Giornalieri(meteo, 2026);

        Assert.Equal(4, periodi.Count);
        var primo = Assert.Single(periodi[0]);
        Assert.Equal(new PuntoGiorno(new DateOnly(2026, 3, 1), 6m, 4m, 2m, 1m), primo);
        Assert.Empty(periodi[1]);
        Assert.Single(periodi[3]);
    }

    private static RigaMeteo Riga(int vigneto, int anno, int mese, int giorno, decimal? pioggia = null,
        decimal? tmin = null, decimal? tmed = null, decimal? tmax = null)
    {
        var data = new DateOnly(anno, mese, giorno);
        return new RigaMeteo(vigneto, data, CalcoloMeteoReport.AnnataAgraria(data), CalcoloMeteoReport.Fase(data), false,
            pioggia, tmin, tmed, tmax);
    }
}
