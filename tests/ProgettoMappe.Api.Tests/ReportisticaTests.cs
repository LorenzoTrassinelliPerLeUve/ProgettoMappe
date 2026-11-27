using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Reportistica;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Reportistica R2 (task #416): id stabili del catalogo, scelte risolte (edizione → predefinita → visibile), archivio SQLite
/// versionato, stati Bozza → Pronto → Pubblicato, pubblicazione congelata e ritiro, filtro sul PowerPoint.
/// </summary>
public sealed class ReportisticaTests : IDisposable
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 27, 18, 0, 0, TimeSpan.Zero);

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "reportistica-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_cartella))
            Directory.Delete(_cartella, true);
    }

    private ArchivioReportistica Archivio() =>
        new(Options.Create(new ReportisticaOptions { Database = Path.Combine(_cartella, "sotto", "reportistica.db"), DatabaseRipiego = "" }));

    private ReportisticaController Controller(ArchivioReportistica? archivio = null) =>
        new(archivio ?? Archivio(), new PerimetroAziende(new InMemoryGruppiRepository(), Options.Create(new PerimetroOptions())));

    /// <summary>
    /// Gli id si scrivono una volta e non cambiano più: le scelte e i commenti salvati li usano. Se questo test fallisce
    /// perché un id è cambiato, rimettere quello vecchio (si cambia il titolo, non l'id).
    /// </summary>
    [Fact]
    public void Catalogo_IdStabiliUniciEValidi()
    {
        string[] attesi =
        [
            "rs.copertina", "rs.dashboard", "rs.meteo", "rs.sanita-grappoli", "rs.grafico-grappoli", "rs.sanita-foglia",
            "rs.grafico-foglia", "rs.fenologia", "rs.bbch-vitigno-foglie", "rs.bbch-vitigno-grappoli", "rs.bbch-storico-foglie",
            "rs.bbch-storico-grappoli", "rs.vigoria", "rs.germogli", "rs.deficit-idrico", "rs.tignole", "rs.tignole-larve", "rs.esca",
            "rs.considerazioni",
        ];
        var ids = CatalogoSettimanale.Elementi.Select(e => e.Id).ToList();
        Assert.Equal(attesi, ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.True(ReportisticaController.IdValido("rs", id), id));
        Assert.False(CatalogoSettimanale.Elementi.Single(e => e.Id == CatalogoSettimanale.Copertina).Nascondibile);
    }

    private static SceltaReportDto Scelta(string id, bool visibile, bool predefinita) => new(id, visibile, predefinita, 1, "prova", Adesso);

    [Fact]
    public void Scelte_EdizioneVinceSullaPredefinita_DefaultVisibile()
    {
        var edizione = new EdizioneReportDto("rs", 1, "2026-47", StatiReport.Bozza, null, null,
        [
            Scelta("rs.meteo", false, true), // nascosta per l'azienda…
            Scelta("rs.meteo", true, false), // …ma mostrata in questa settimana
            Scelta("rs.esca", false, true),
            Scelta("rs.copertina", false, false), // non nascondibile: si ignora
            Scelta("rs.non-esiste", false, false),
        ],
        [
            new("rs.copertina", "it", "Settimana tranquilla.", 1, "prova", Adesso),
            new("rs.copertina", "en", "Quiet week.", 1, "prova", Adesso),
            new("rs.non-esiste", "it", "Perso", 1, "prova", Adesso),
        ], [], false);

        var it = ScelteApplicate.Da(CatalogoSettimanale.Elementi, edizione, "it");
        Assert.True(it.Visibile("rs.meteo"));
        Assert.False(it.Visibile("rs.esca"));
        Assert.True(it.Visibile("rs.copertina"));
        Assert.True(it.Visibile("rs.dashboard"));
        Assert.Equal(["rs.esca"], it.Nascosti);
        Assert.Equal("Settimana tranquilla.", it.Commento("rs.copertina"));
        Assert.Null(it.Commento("rs.non-esiste"));
        Assert.Equal("Quiet week.", ScelteApplicate.Da(CatalogoSettimanale.Elementi, edizione, "en").Commento("rs.copertina"));
        Assert.Same(ScelteApplicate.Tutto, ScelteApplicate.Da(CatalogoSettimanale.Elementi, null, "it"));
    }

    [Fact]
    public void Scelte_SezioneNascostaPortaViaISuoiElementi()
    {
        List<ElementoReport> catalogo =
        [
            new("x.a", TipoElementoReport.Sezione, null, "A"),
            new("x.a.kpi", TipoElementoReport.Kpi, "x.a", "KPI di A"),
            new("x.b", TipoElementoReport.Sezione, null, "B"),
        ];
        var ed = new EdizioneReportDto("x", 1, "2026", StatiReport.Bozza, null, null, [Scelta("x.a", false, false)], [], [], false);
        var s = ScelteApplicate.Da(catalogo, ed, "it");
        Assert.False(s.Visibile("x.a.kpi"));
        Assert.True(s.Visibile("x.b"));
    }

    [Fact]
    public async Task Archivio_SenzaFile_EdizioneInBozzaVuota()
    {
        var e = await Archivio().EdizioneAsync("rs", 1, "2026-47", default);
        Assert.Equal(StatiReport.Bozza, e.Stato);
        Assert.Empty(e.Scelte);
        Assert.Empty(e.Pubblicazioni);
    }

    [Fact]
    public async Task Archivio_OgniModificaUnaVersione_LeggeLUltima()
    {
        var a = Archivio();
        await a.SalvaSceltaAsync("rs", 1, "2026-47", "rs.meteo", false, "mario", Adesso, default);
        await a.SalvaSceltaAsync("rs", 1, "2026-47", "rs.meteo", true, "anna", Adesso.AddMinutes(1), default);
        await a.SalvaSceltaAsync("rs", 1, "", "rs.esca", false, "anna", Adesso, default);
        await a.SalvaSceltaAsync("rs", 2, "2026-47", "rs.esca", true, "anna", Adesso, default); // altra azienda
        await a.SalvaCommentoAsync("rs", 1, "2026-47", "rs.copertina", "it", "Prima", "mario", Adesso, default);
        await a.SalvaCommentoAsync("rs", 1, "2026-47", "rs.copertina", "it", "Seconda", "anna", Adesso.AddMinutes(1), default);
        await a.SalvaCommentoAsync("rs", 1, "2026-47", "rs.meteo", "it", "Tolto", "anna", Adesso, default);
        await a.SalvaCommentoAsync("rs", 1, "2026-47", "rs.meteo", "it", "", "anna", Adesso.AddMinutes(1), default);

        var e = await a.EdizioneAsync("rs", 1, "2026-47", default);
        var meteo = Assert.Single(e.Scelte, s => s.IdElemento == "rs.meteo");
        Assert.True(meteo.Visibile);
        Assert.Equal(2, meteo.Versione);
        Assert.Equal("anna", meteo.Autore);
        Assert.True(Assert.Single(e.Scelte, s => s.IdElemento == "rs.esca").Predefinita);
        var commento = Assert.Single(e.Commenti);
        Assert.Equal(("rs.copertina", "Seconda", 2), (commento.IdSezione, commento.Testo, commento.Versione));

        // La predefinita vale anche per un'altra settimana della stessa azienda.
        var altra = await a.EdizioneAsync("rs", 1, "2026-48", default);
        Assert.Equal("rs.esca", Assert.Single(altra.Scelte).IdElemento);
    }

    [Fact]
    public async Task Archivio_PubblicaVersioniRitiraTornaInBozza()
    {
        var a = Archivio();
        await a.SalvaStatoAsync("rs", 1, "2026-47", StatiReport.Pronto, "mario", Adesso, default);
        var v1 = await a.PubblicaAsync("rs", 1, "2026-47", "it", Adesso.AddHours(-10), null, "r.pptx", "{\"v\":1}", [1, 2, 3], "mario", Adesso, default);
        var v2 = await a.PubblicaAsync("rs", 1, "2026-47", "it", null, "Corretto il commento", "r.pptx", "{\"v\":2}", [4, 5], "anna",
            Adesso.AddMinutes(5), default);
        Assert.Equal((1, 2), (v1.Versione, v2.Versione));
        Assert.Equal(64, v1.Impronta.Length);

        var e = await a.EdizioneAsync("rs", 1, "2026-47", default);
        Assert.Equal(StatiReport.Pubblicato, e.Stato);
        Assert.False(e.ModificheNonPubblicate);
        Assert.Equal([2, 1], e.Pubblicazioni.Select(p => p.Versione));
        Assert.Equal("{\"v\":2}", (await a.ContenutoAsync("rs", 1, "2026-47", "it", null, default))!.Contenuto);
        Assert.Equal("{\"v\":1}", (await a.ContenutoAsync("rs", 1, "2026-47", "it", 1, default))!.Contenuto);
        Assert.Null(await a.ContenutoAsync("rs", 1, "2026-47", "en", null, default));
        Assert.Equal(new byte[] { 4, 5 }, (await a.PptxAsync("rs", 1, "2026-47", "it", 2, default))!.Value.Pptx);

        // Una modifica dopo la pubblicazione si vede; il cliente resta sulla versione 2.
        await a.SalvaCommentoAsync("rs", 1, "2026-47", "rs.meteo", "it", "Nuovo", "anna", Adesso.AddMinutes(10), default);
        Assert.True((await a.EdizioneAsync("rs", 1, "2026-47", default)).ModificheNonPubblicate);

        // Ritirata la 2 il cliente vede la 1; ritirate tutte, l'edizione torna in bozza.
        Assert.True(await a.RitiraAsync("rs", 1, "2026-47", "it", 2, "anna", Adesso.AddMinutes(11), default));
        Assert.False(await a.RitiraAsync("rs", 1, "2026-47", "it", 2, "anna", Adesso.AddMinutes(11), default));
        Assert.Equal(1, (await a.ContenutoAsync("rs", 1, "2026-47", "it", null, default))!.Pubblicazione.Versione);
        Assert.Equal(StatiReport.Pubblicato, (await a.EdizioneAsync("rs", 1, "2026-47", default)).Stato);
        Assert.True(await a.RitiraAsync("rs", 1, "2026-47", "it", 1, "anna", Adesso.AddMinutes(12), default));
        Assert.Null(await a.ContenutoAsync("rs", 1, "2026-47", "it", null, default));
        Assert.Equal(StatiReport.Bozza, (await a.EdizioneAsync("rs", 1, "2026-47", default)).Stato);
    }

    [Fact]
    public async Task Controller_FormaPerimetroAutoreEStati()
    {
        var c = Controller();
        Assert.IsType<NotFoundResult>((await c.Get("xx", 1, "2026-47", default)).Result);
        Assert.IsType<NotFoundResult>((await c.Get("rs", 1, "2026-4", default)).Result);
        Assert.Equal(StatiReport.Bozza, (await c.Get("rs", 1, "2026", default)).Value!.Stato);

        var senzaAutore = await c.Scelta("rs", 1, "2026-47", new SalvaSceltaReportDto("rs.meteo", false, false, " "), default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(senzaAutore.Result).StatusCode);
        Assert.IsType<BadRequestObjectResult>((await c.Scelta("rs", 1, "2026-47", new SalvaSceltaReportDto("ib.meteo", false, false, "m"), default)).Result);
        Assert.IsType<BadRequestObjectResult>((await c.Commento("rs", 1, "2026-47", new SalvaCommentoReportDto("rs.meteo", "fr", "x", "m"), default)).Result);
        Assert.IsType<BadRequestObjectResult>((await c.Commento("rs", 1, "2026-47",
            new SalvaCommentoReportDto("rs.meteo", "it", new string('a', ReportisticaController.LunghezzaMassimaCommento + 1), "m"), default)).Result);

        var pubblica = new PubblicaReportDto("it", null, null, "../r.pptx", "{}", Convert.ToBase64String([1, 2]), "m");
        Assert.IsType<ConflictObjectResult>((await c.Pubblica("rs", 1, "2026-47", pubblica, default)).Result);
        Assert.IsType<ConflictObjectResult>((await c.Stato("rs", 1, "2026-47", new CambiaStatoReportDto(StatiReport.Pubblicato, "m"), default)).Result);
        Assert.Equal(StatiReport.Pronto, (await c.Stato("rs", 1, "2026-47", new CambiaStatoReportDto(StatiReport.Pronto, "m"), default)).Value!.Stato);
        Assert.IsType<BadRequestObjectResult>((await c.Pubblica("rs", 1, "2026-47", pubblica with { PptxBase64 = "non base64!" }, default)).Result);
        var fatta = (await c.Pubblica("rs", 1, "2026-47", pubblica, default)).Value!;
        Assert.Equal("r.pptx", fatta.NomeFile);
        Assert.Equal(StatiReport.Bozza, (await c.Stato("rs", 1, "2026-47", new CambiaStatoReportDto(StatiReport.Bozza, "m"), default)).Value!.Stato);
    }

    [Fact]
    public void PowerPoint_SezioniNascosteSpariscono_CommentiSonoSlide()
    {
        var dati = PowerPointSettimanaleTests.Dati(true);
        var intero = PowerPointSettimanaleTests.Slide(PowerPointSettimanale.Crea(dati, LinguaReport.It, Adesso));
        var scelte = new ScelteApplicate(new HashSet<string> { "rs.meteo", "rs.esca" },
            new Dictionary<string, string> { ["rs.copertina"] = "Riga 1\nRiga 2", ["rs.vigoria"] = "Vigoria alta" });
        Assert.Equal(intero - 2 + 2, PowerPointSettimanaleTests.Slide(PowerPointSettimanale.Crea(dati, LinguaReport.It, Adesso, scelte)));
        Assert.Equal(intero - 2, PowerPointSettimanaleTests.Slide(PowerPointSettimanale.Crea(dati, LinguaReport.It, Adesso,
            scelte with { Commenti = new Dictionary<string, string>() })));
    }

    [Fact]
    public void Contenuto_JsonAndataERitorno()
    {
        var dati = PowerPointSettimanaleTests.Dati(true);
        var scelte = new ScelteApplicate(new HashSet<string> { "rs.meteo" }, new Dictionary<string, string> { ["rs.copertina"] = "Ciao" });
        var json = ContenutoSettimanale.Di(dati, LinguaReport.En, scelte, Adesso).Json();
        var letto = ContenutoSettimanale.Leggi(json)!;
        Assert.Equal(("Azienda / prova", "en", Adesso), (letto.Azienda, letto.Lingua, letto.Generato));
        Assert.False(letto.Scelte.Visibile("rs.meteo"));
        Assert.Equal("Ciao", letto.Scelte.Commento("rs.copertina"));
        Assert.Equal(dati.Rilievi.Rilievi.Count, letto.Rilievi.Rilievi.Count);
        Assert.Equal(dati.Settimana, letto.Dati()!.Settimana);
        // Stesso PowerPoint dai dati congelati.
        Assert.Equal(PowerPointSettimanaleTests.Slide(PowerPointSettimanale.Crea(dati, LinguaReport.En, Adesso, scelte)),
            PowerPointSettimanaleTests.Slide(PowerPointSettimanale.Crea(letto.Dati()!, LinguaReport.En, Adesso, letto.Scelte)));
        Assert.Null(ContenutoSettimanale.Leggi("{\"formato\":99}"));
        Assert.Null(ContenutoSettimanale.Leggi("non json"));
    }

    [Fact]
    public void Stati_PassaggiAmmessi()
    {
        Assert.True(StatiReport.Ammesso(StatiReport.Bozza, StatiReport.Pronto));
        Assert.True(StatiReport.Ammesso(StatiReport.Pubblicato, StatiReport.Bozza));
        Assert.False(StatiReport.Ammesso(StatiReport.Bozza, StatiReport.Pubblicato));
        Assert.False(StatiReport.Ammesso(StatiReport.Pronto, StatiReport.Pronto));
        Assert.False(StatiReport.SiPuoPubblicare(StatiReport.Bozza));
        Assert.True(StatiReport.SiPuoPubblicare(StatiReport.Pubblicato));
    }
}
