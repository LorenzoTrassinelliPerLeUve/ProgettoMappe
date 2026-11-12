using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.ReportFineStagione;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class ReportFineStagioneTests
{
    [Fact]
    public void OgniTesto_HaLeDueLingue()
    {
        foreach (var chiave in TestiReport.Chiavi)
        {
            Assert.False(string.IsNullOrWhiteSpace(TestiReport.Testo(LinguaReport.It, chiave, 2026)), chiave);
            Assert.False(string.IsNullOrWhiteSpace(TestiReport.Testo(LinguaReport.En, chiave, 2026)), chiave);
        }
        foreach (var s in SezioniReport.Tutte)
        {
            Assert.Contains(s.Chiave, TestiReport.Chiavi);
        }
    }

    [Fact]
    public void Sommario_NellOrdineDiQlik()
    {
        Assert.Equal(14, SezioniReport.Tutte.Count);
        Assert.Equal("Caratterizzazione dell’annata 2026", SezioniReport.Titolo(SezioniReport.Tutte[0], LinguaReport.It, 2026));
        Assert.Equal("Season characterization 2026", SezioniReport.Titolo(SezioniReport.Tutte[0], LinguaReport.En, 2026));
        Assert.Equal("Indice Bigot", SezioniReport.Titolo(SezioniReport.Tutte[10], LinguaReport.It, 2026));
        Assert.Equal("Degustazione Vini 2025", SezioniReport.Titolo(SezioniReport.Tutte[13], LinguaReport.It, 2026));
    }

    [Fact]
    public void Lingua_DalLink()
    {
        Assert.Equal(LinguaReport.En, TestiReport.DaCodice("EN"));
        Assert.Equal(LinguaReport.It, TestiReport.DaCodice(null));
        Assert.Equal(LinguaReport.It, TestiReport.DaCodice("de"));
    }

    [Theory]
    [InlineData(2025, 10, 31, 2025)]
    [InlineData(2025, 11, 1, 2026)]
    [InlineData(2026, 1, 15, 2026)]
    public void AnnataAgraria_ParteIlPrimoNovembre(int anno, int mese, int giorno, int attesa) =>
        Assert.Equal(attesa, CalcoloMeteoReport.AnnataAgraria(new DateOnly(anno, mese, giorno)));

    [Theory]
    [InlineData(11, 1, 1)]
    [InlineData(3, 14, 1)]
    [InlineData(3, 15, 2)]
    [InlineData(5, 31, 2)]
    [InlineData(6, 1, 3)]
    [InlineData(7, 31, 3)]
    [InlineData(8, 1, 4)]
    [InlineData(10, 31, 4)]
    public void Fase_ComeLoScriptQlik(int mese, int giorno, int attesa) =>
        Assert.Equal(attesa, CalcoloMeteoReport.Fase(new DateOnly(2026, mese, giorno)));

    [Fact]
    public void PioggiaPerFase_MedieDiQlik()
    {
        // Vigneto 1 e 2 con meteo storico, 3 senza (non conta). Annata 2026, fase 3 (giugno-luglio).
        var meteo = new MeteoReportFineStagioneDto(
            [new(1, "A", null, null, true), new(2, "B", null, null, true), new(3, "C", null, null, false)],
            [
                Giorno(1, 2026, 6, 1, 10), Giorno(1, 2026, 7, 1, 20), // vigneto 1: 30
                Giorno(2, 2026, 6, 2, 50),                            // vigneto 2: 50 → annata = 40
                Giorno(3, 2026, 6, 3, 999),
                Giorno(1, 2025, 6, 1, 20),                            // 2025: 20 e 40
                Giorno(2, 2025, 6, 1, 40),
                Giorno(1, 2016, 6, 1, 60),                            // 2016: dentro i 10 anni (2016–2025)
                Giorno(1, 2015, 6, 1, 100),                           // 2015: solo nei 30 anni
                Giorno(2, 2025, 6, 2, null),                          // senza pioggia: non cambia la somma
            ],
            true);

        var fase3 = CalcoloMeteoReport.PioggiaPerFase(meteo, 2026).Single(r => r.Fase == 3);

        Assert.Equal(40m, fase3.Annata);
        Assert.Equal(40m, fase3.Media10);            // (20 + 40 + 60) / 3
        Assert.Equal(55m, fase3.Media30);            // (20 + 40 + 60 + 100) / 4
        Assert.Equal(0m, fase3.Variazione10);
        Assert.Equal(0m, fase3.VariazionePct10);
        Assert.Equal(-15m, fase3.Variazione30);
        Assert.Equal(40m / 55m - 1, fase3.VariazionePct30);

        var fase1 = CalcoloMeteoReport.PioggiaPerFase(meteo, 2026).Single(r => r.Fase == 1);
        Assert.Null(fase1.Annata);
        Assert.Null(fase1.VariazionePct10);
    }

    [Fact]
    public void PioggiaPerFase_SenzaStorico_SoloLAnnata()
    {
        var meteo = new MeteoReportFineStagioneDto([new(1, "A", null, null, true)], [Giorno(1, 2026, 6, 1, 12)], false);
        var fase3 = CalcoloMeteoReport.PioggiaPerFase(meteo, 2026).Single(r => r.Fase == 3);
        Assert.Equal(12m, fase3.Annata);
        Assert.Null(fase3.Media10);
        Assert.Null(fase3.Variazione30);
    }

    [Fact]
    public async Task Esempio_AnnateDalPiuRecente()
    {
        var letto = await new InMemoryFonteMeteoReport().LeggiAsync(1);
        var dto = new MeteoReportFineStagioneDto(
            letto.Vigneti.Select(v => new VignetoMeteoReportDto(v.IdVigneto, v.Nome, v.IdVigna, v.Vigna, v.DatiMeteoStorico)).ToList(),
            letto.Giorni.Select(g => new GiornoMeteoReportDto(g.IdVigneto, g.Data, g.Previsione, g.Precipitazione, g.TemperaturaMin, g.TemperaturaMed, g.TemperaturaMax)).ToList(),
            letto.StoricoDisponibile);
        var annate = CalcoloMeteoReport.Annate(dto);
        Assert.Equal(2026, annate[0]);
        Assert.Equal(1996, annate[^1]);
        Assert.All(CalcoloMeteoReport.PioggiaPerFase(dto, 2026), r => Assert.NotNull(r.Media30));
    }

    private static GiornoMeteoReportDto Giorno(int vigneto, int anno, int mese, int giorno, decimal? pioggia) =>
        new(vigneto, new DateOnly(anno, mese, giorno), false, pioggia, null, null, null);
}
