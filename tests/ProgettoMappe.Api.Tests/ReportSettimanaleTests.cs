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

/// <summary>Report settimanale (task #213): settimane di Qlik, slide, calcoli degli schemi, endpoint.</summary>
public class ReportSettimanaleTests
{
    [Theory]
    [InlineData("2026-01-01", 53)] // giorno 1 → 53 (DOY_WEEK.csv)
    [InlineData("2026-01-02", 1)]
    [InlineData("2026-01-08", 2)]
    [InlineData("2026-11-13", 46)] // giorno di Qlik 318 (un giorno in più dal 1/3 negli anni non bisestili)
    [InlineData("2026-12-31", 53)]
    public void Settimana_ComeLaTabellaDiQlik(string giorno, int settimana) =>
        Assert.Equal(settimana, SettimanaQlik.Di(DateOnly.Parse(giorno)));

    [Fact]
    public void Etichette_DaWeekRef()
    {
        Assert.Equal("01 - 04 Gen", SettimanaQlik.Etichetta(1));
        Assert.Equal("53 - 31 Dic", SettimanaQlik.Etichetta(53));
        Assert.Equal("Settimana 01 - 04 Gen 2026", new SettimanaReport(2026, 1).Etichetta(LinguaReport.It));
    }

    [Fact]
    public void Settimana_ContieneLAnnoFinoAQuellaSettimana()
    {
        var s = new SettimanaReport(2026, 46);
        Assert.True(s.Contiene(new DateTime(2026, 3, 10)));
        Assert.True(s.Contiene(new DateTime(2026, 11, 13)));
        Assert.False(s.Contiene(new DateTime(2026, 11, 20)));
        Assert.False(s.Contiene(new DateTime(2026, 1, 1))); // settimana 53, come Qlik
        Assert.False(s.Contiene(new DateTime(2025, 6, 1)));
        Assert.Equal(46, SettimanaQlik.Di(s.A));
        Assert.Equal(47, SettimanaQlik.Di(s.A.AddDays(1)));
    }

    [Fact]
    public void Codice_AndataERitorno()
    {
        Assert.Equal(new SettimanaReport(2026, 46), SettimanaReport.DaCodice("2026-46"));
        Assert.Null(SettimanaReport.DaCodice("2026-54"));
        Assert.Null(SettimanaReport.DaCodice("x"));
        Assert.Null(SettimanaReport.DaCodice(null));
    }

    [Fact]
    public void Elenco_DaOggiIndietro()
    {
        var elenco = SettimanaReport.Elenco(new DateOnly(2026, 11, 13));
        Assert.Equal("2026-46", elenco[0].Codice);
        Assert.Equal("2025-53", elenco[46].Codice);
        Assert.Equal("2025-01", elenco[^1].Codice);
    }

    [Fact]
    public void Slide_NellOrdineDellAutomazioneConTesti()
    {
        // 18 e 12 fogli dell'automazione più il meteo del portale (task #214).
        Assert.Equal(19, SezioniSettimanale.Della(LinguaReport.It).Count());
        Assert.Equal(13, SezioniSettimanale.Della(LinguaReport.En).Count());
        Assert.Equal(18, SezioniSettimanale.Tutte.Count(s => !s.FoglioQlik.StartsWith("(portale)", StringComparison.Ordinal)));
        foreach (var chiave in SezioniSettimanale.Tutte.Select(s => s.Chiave)
                     .Concat(CalcoloSettimanale.Grappolo.Select(a => a.Chiave)).Concat(CalcoloSettimanale.Generazioni.Select(g => g.Chiave)))
        {
            Assert.Contains(chiave, TestiSettimanale.Chiavi);
        }
        foreach (var v in CalcoloSettimanale.ValoriEsca)
        {
            Assert.Contains($"esca.{v}", TestiSettimanale.Chiavi);
        }
    }

    private static readonly Dictionary<int, double?> Superfici = new() { [1] = 10000, [2] = 30000 };

    private static RilievoSettimanaDto R(int vigneto, int tipo, double? intensita = null, double? diffusione = null, int? organo = 2,
        int? eta = null, int? stato = null, int mese = 6) =>
        new(vigneto, new DateTime(2026, mese, 1), tipo, null, organo, eta, 10, diffusione, intensita, null, null, stato);

    [Fact]
    public void Pesata_MassimoPerVignetoPerSuperficieSulTotale()
    {
        // Vigneto 1: max 20 × 1 ha; vigneto 2: 10 × 3 ha; totale 4 ha → (20 + 30) / 4 / 100 = 0,125.
        List<RilievoSettimanaDto> sel = [R(1, 12, 5), R(1, 12, 20), R(2, 12, 10), R(2, 12, 50, organo: 1)];
        Assert.Equal(0.125, CalcoloSettimanale.Pesata(sel, Superfici, [12], CalcoloSettimanale.OrganiGrappolo, r => r.PercIntensita), 6);
        Assert.Equal(0.25, CalcoloSettimanale.Copertura(sel.Where(r => r.IdVigneto == 1).ToList(), Superfici, r => r.TipoRilievo == 12), 6);
        Assert.Null(CalcoloSettimanale.SenzaDanno(sel, 12, CalcoloSettimanale.OrganiGrappolo));
    }

    [Fact]
    public void StatoIdricoEGenerazioni()
    {
        List<RilievoSettimanaDto> sel = [R(1, 146, stato: 1), R(1, 146, stato: 3), R(2, 146, stato: 2), R(2, 146, stato: 4, mese: 10),
            R(1, 13, diffusione: 8, eta: 1310), R(2, 13, diffusione: 2, eta: 410)];
        Assert.Equal(2.5, CalcoloSettimanale.StatoIdrico(sel, 2026));
        Assert.Null(CalcoloSettimanale.StatoIdrico(sel, 2025));
        var (valore, copertura) = CalcoloSettimanale.Generazione(sel, Superfici, [1310, 410]);
        Assert.Equal(5, valore);
        Assert.Equal(1, copertura);
        Assert.Equal("vigoria.alta", CalcoloSettimanale.ClasseVigoria(7.5));
        Assert.Equal("vigoria.eccessiva", CalcoloSettimanale.ClasseVigoria(10));
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
        Assert.IsType<BadRequestResult>((await controller.GetRilievi(1, new DateOnly(2020, 1, 1), new DateOnly(2026, 11, 8), default)).Result);
    }
}
