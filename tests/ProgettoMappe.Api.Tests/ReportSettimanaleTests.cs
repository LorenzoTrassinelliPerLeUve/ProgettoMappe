using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Report settimanale, tappa 1 (task #213): settimane come Qlik, sezioni, testi, endpoint.</summary>
public class ReportSettimanaleTests
{
    [Theory]
    [InlineData("2026-11-13", 2026, 46, "2026-11-08", "2026-11-14")] // venerdì
    [InlineData("2026-11-08", 2026, 46, "2026-11-08", "2026-11-14")] // la domenica apre la settimana
    [InlineData("2026-11-07", 2026, 45, "2026-11-01", "2026-11-07")] // il sabato la chiude
    [InlineData("2026-01-03", 2026, 1, "2025-12-28", "2026-01-03")] // a cavallo d'anno: settimana 1 del 2026
    public void Settimana_DomenicaSabato(string giorno, int anno, int numero, string da, string a)
    {
        var s = SettimanaReport.Di(DateOnly.Parse(giorno));
        Assert.Equal((anno, numero), (s.Anno, s.Numero));
        Assert.Equal(DateOnly.Parse(da), s.Da);
        Assert.Equal(DateOnly.Parse(a), s.A);
        Assert.Equal(DayOfWeek.Sunday, s.Da.DayOfWeek);
    }

    [Fact]
    public void Codice_AndataERitorno()
    {
        var s = SettimanaReport.Di(new DateOnly(2026, 11, 13));
        Assert.Equal("2026-46", s.Codice);
        Assert.Equal(s, SettimanaReport.DaCodice("2026-46"));
        Assert.Null(SettimanaReport.DaCodice("2026-54"));
        Assert.Null(SettimanaReport.DaCodice("x"));
        Assert.Null(SettimanaReport.DaCodice(null));
    }

    [Fact]
    public void Elenco_DaOggiIndietroSenzaBuchi()
    {
        var elenco = SettimanaReport.Elenco(new DateOnly(2026, 11, 13));
        Assert.Equal("2026-46", elenco[0].Codice);
        Assert.Equal(2025, elenco[^1].Anno);
        Assert.Equal(1, elenco[^1].Numero);
        for (var i = 1; i < elenco.Count; i++)
        {
            Assert.Equal(elenco[i - 1].Da.AddDays(-7), elenco[i].Da);
        }
    }

    [Fact]
    public void Sezioni_NellOrdineDeiFogliEConTesti()
    {
        Assert.Equal("Dashboard", SezioniSettimanale.Tutte[0].FoglioQlik);
        Assert.Equal("Foto", SezioniSettimanale.Tutte[^1].FoglioQlik);
        Assert.Equal(SezioniSettimanale.Tutte.Count, SezioniSettimanale.Tutte.Select(s => s.Chiave).Distinct().Count());
        foreach (var s in SezioniSettimanale.Tutte)
        {
            Assert.Contains(s.Chiave, TestiSettimanale.Chiavi);
            Assert.NotEqual(TestiSettimanale.Testo(LinguaReport.It, s.Chiave), s.Chiave);
        }
    }

    [Fact]
    public void Sezioni_ContanoISoliRilieviDeiLoroTipi()
    {
        var t0 = new DateTime(2026, 6, 1);
        List<RilievoSettimanaDto> rilievi = [new(1, t0, 146, null), new(1, t0, 13, null), new(2, t0, 0, 65), new(2, t0, 9, null)];
        int? Conta(string chiave) => SezioniSettimanale.Rilievi(SezioniSettimanale.Tutte.Single(s => s.Chiave == chiave), rilievi);
        Assert.Equal(1, Conta("sezione.deficit-idrico"));
        Assert.Equal(1, Conta("sezione.tignole"));
        Assert.Equal(1, Conta("sezione.esca"));
        Assert.Equal(1, Conta("sezione.fenologia"));
        Assert.Null(Conta("sezione.sanita-foglia"));
    }

    [Fact]
    public void Testi_DueLingue()
    {
        Assert.Equal("Settimana 46 del 2026 (08/11–14/11)", SettimanaReport.Di(new DateOnly(2026, 11, 13)).Etichetta(LinguaReport.It));
        Assert.Equal("Week 46 of 2026 (08/11–14/11)", SettimanaReport.Di(new DateOnly(2026, 11, 13)).Etichetta(LinguaReport.En));
    }

    [Fact]
    public async Task Endpoint_SenzaSnapshotLeggeLaFonteEControllaIlPeriodo()
    {
        var snapshot = new SnapshotReportFineStagione(Options.Create(new ReportFineStagioneOptions { Snapshot = "/non/esiste.sqlite" }),
            NullLogger<SnapshotReportFineStagione>.Instance);
        var controller = new ReportSettimanaleController(new InMemoryFonteRilieviSettimana(), snapshot,
            PerimetroAziendeTests.Perimetro(), NullLogger<ReportSettimanaleController>.Instance);

        var risposta = await controller.GetRilievi(1, new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 14), default);
        var dto = Assert.IsType<RilieviSettimanaDto>(Assert.IsType<OkObjectResult>(risposta.Result).Value);
        Assert.Equal("4grapes", dto.Fonte);
        Assert.Equal(3, dto.Rilievi.Count);

        Assert.IsType<BadRequestResult>((await controller.GetRilievi(1, new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 8), default)).Result);
        Assert.IsType<BadRequestResult>((await controller.GetRilievi(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 8), default)).Result);
    }
}
