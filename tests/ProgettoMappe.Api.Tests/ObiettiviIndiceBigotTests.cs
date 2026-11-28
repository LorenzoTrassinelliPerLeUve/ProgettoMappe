using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Reportistica;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.IndiceBigot;
using ProgettoMappe.Web.Report;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Reportistica R6 (task #419): obiettivi dei parametri dell'Indice Bigot (controlli, proposte dalle curve di 4Grapes, livello che
/// vale, archivio versionato, Api) e cruscotto «a che punto sono».
/// </summary>
public sealed class ObiettiviIndiceBigotTests : IDisposable
{
    private static readonly DateTimeOffset Adesso = new(2026, 11, 28, 18, 0, 0, TimeSpan.Zero);

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "obiettivi-bigot-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_cartella))
            Directory.Delete(_cartella, true);
    }

    private ArchivioReportistica Archivio() =>
        new(Options.Create(new ReportisticaOptions { Database = Path.Combine(_cartella, "reportistica.db"), DatabaseRipiego = "" }));

    private ObiettiviBigotController Controller(ArchivioReportistica archivio) =>
        new(archivio, new PerimetroAziende(new InMemoryGruppiRepository(), Options.Create(new PerimetroOptions())),
            new SnapshotIndiceBigot(Options.Create(new IndiceBigotOptions { Database = Path.Combine(_cartella, "manca.db") }),
                NullLogger<SnapshotIndiceBigot>.Instance));

    private static SalvaObiettivoBigotDto Salva(string p, double? min, double? obj, double? max, int ente = 1, int vigneto = 0, int anno = 2026) =>
        new(ente, vigneto, anno, p, min, obj, max, "prova");

    [Fact]
    public void Controlla_ScalaOrdineELivelli()
    {
        Assert.Null(ObiettiviBigot.Controlla(Salva("SFE", 2, 2.7, 3.5)));
        Assert.Null(ObiettiviBigot.Controlla(Salva("sfe", null, null, null))); // tutti vuoti = togli
        Assert.Null(ObiettiviBigot.Controlla(Salva("INDICE", 60, 70, null)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("ETA VIGNETO", 1, 2, 3))); // l'età non ha obiettivo
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("SFE", 3.5, null, 2)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("SFE", 2, 4, 3.5)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("VIGORIA", 0, null, null))); // vigoria 1–10
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("REGIME IDRICO", null, null, 5)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("SFE", double.NaN, null, null)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("SFE", 1, null, null, ente: 0, vigneto: 5)));
        Assert.NotNull(ObiettiviBigot.Controlla(Salva("SFE", 1, null, null, anno: 1990)));
        Assert.Equal("SFEKg", ObiettiviBigot.Normalizza("sfekg"));
    }

    [Fact]
    public void Proposte_DalleCurveDi4Grapes()
    {
        // Regime idrico: il punteggio migliore è con stato idrico «medio» (2), come dice il dba.
        var idrico = ObiettiviIndice.Proposta("REGIME IDRICO")!;
        // Sulle classi intere: 0 → 5, 1 → 10, 2 → 18, 3 → 15, 4 → 8; almeno l'80 % di 18 = 2 e 3.
        Assert.Equal((2.0, 2.0, 3.0), (idrico.Minimo!.Value, idrico.Obiettivo!.Value, idrico.Massimo!.Value));
        // Morfologia: meglio molto spargolo (6, fine scala); dal 3 in su almeno l'80 %.
        Assert.Equal((3.0, 6.0, 6.0), (ObiettiviIndice.Proposta("MORFOLOGIA")!.Minimo!.Value, ObiettiviIndice.Proposta("MORFOLOGIA")!.Obiettivo!.Value,
            ObiettiviIndice.Proposta("MORFOLOGIA")!.Massimo!.Value));
        // Sanità: obiettivo 100 % (nessun danno), fascia verso il basso.
        var sanita = ObiettiviIndice.Proposta("AMPELOPATIE")!;
        Assert.Equal(100, sanita.Obiettivo);
        Assert.Equal(100, sanita.Massimo);
        Assert.InRange(sanita.Minimo!.Value, 85, 99);
        // Vigoria e indice: nessuna proposta.
        Assert.Null(ObiettiviIndice.Proposta("VIGORIA"));
        Assert.Null(ObiettiviIndice.Proposta(ObiettiviBigot.Indice));

        foreach (var c in ObiettiviIndice.Curve)
        {
            var p = ObiettiviIndice.Proposta(c.Id)!;
            Assert.True(p.Minimo <= p.Obiettivo && p.Obiettivo <= p.Massimo, c.Id);
            // Ogni proposta passa i controlli dell'Api.
            Assert.Null(ObiettiviBigot.Controlla(Salva(c.Id, p.Minimo, p.Obiettivo, p.Massimo)));
        }
    }

    private static ObiettivoBigotDto Obiettivo(int ente, int vigneto, string p, double? min, double? max) =>
        new(ente, vigneto, 2026, p, min, null, max, 1, "prova", Adesso);

    [Fact]
    public void Risolvi_VignetoPoiAziendaPoiPredefinitoPoiProposta()
    {
        var obiettivi = new[]
        {
            Obiettivo(0, 0, "SFE", 1, 2),
            Obiettivo(7, 0, "SFE", 2, 3),
            Obiettivo(7, 11, "SFE", 3, 4),
            Obiettivo(7, 12, "SFE", null, null), // tolto: vale l'azienda
            Obiettivo(8, 0, "PRODUZIONE", null, null),
        };
        Assert.Equal(LivelloObiettivo.Vigneto, ObiettiviIndice.Risolvi(obiettivi, "SFE", 7, 11).Livello);
        Assert.Equal(3, ObiettiviIndice.Risolvi(obiettivi, "SFE", 7, 11).Minimo);
        Assert.Equal(LivelloObiettivo.Azienda, ObiettiviIndice.Risolvi(obiettivi, "SFE", 7, 12).Livello);
        Assert.Equal(LivelloObiettivo.Predefinito, ObiettiviIndice.Risolvi(obiettivi, "SFE", 8).Livello);
        Assert.Equal(LivelloObiettivo.Proposta, ObiettiviIndice.Risolvi(obiettivi, "PRODUZIONE", 8).Livello);
        Assert.Equal(LivelloObiettivo.Nessuno, ObiettiviIndice.Risolvi(obiettivi, "VIGORIA", 8).Livello);

        var fascia = new ObiettivoRisolto(2, 2.5, 3, LivelloObiettivo.Azienda);
        Assert.Equal(EsitoObiettivo.InLinea, ObiettiviIndice.Esito(2, fascia));
        Assert.Equal(EsitoObiettivo.InLinea, ObiettiviIndice.Esito(3, fascia));
        Assert.Equal(EsitoObiettivo.Sotto, ObiettiviIndice.Esito(1.9, fascia));
        Assert.Equal(EsitoObiettivo.Sopra, ObiettiviIndice.Esito(3.1, fascia));
        Assert.Equal(EsitoObiettivo.MancaDato, ObiettiviIndice.Esito(null, fascia));
        Assert.Equal(EsitoObiettivo.SenzaObiettivo, ObiettiviIndice.Esito(5, ObiettivoRisolto.Nessuno));
        Assert.Equal(EsitoObiettivo.InLinea, ObiettiviIndice.Esito(95, new ObiettivoRisolto(90, null, null, LivelloObiettivo.Azienda)));
        Assert.Equal("da 2 a 3, obiettivo 2,5", ObiettiviIndice.Fascia("SFE", fascia));
        Assert.Equal("almeno 90 %", ObiettiviIndice.Fascia("AMPELOPATIE", new ObiettivoRisolto(90, null, null, LivelloObiettivo.Azienda)));
    }

    [Fact]
    public async Task Archivio_VersioniUltimaValeECopiaSoloQuelliCheMancano()
    {
        var archivio = Archivio();
        Assert.Empty((await archivio.ObiettiviAsync(2026, 1, default)).Obiettivi); // senza file

        await archivio.SalvaObiettivoAsync(Salva("SFE", 1, null, 2, ente: 0), "a", Adesso, default);
        await archivio.SalvaObiettivoAsync(Salva("SFE", 2, null, 3), "a", Adesso, default);
        await archivio.SalvaObiettivoAsync(Salva("SFE", 2.2, 2.7, 3.2), "b", Adesso.AddMinutes(1), default);
        await archivio.SalvaObiettivoAsync(Salva("PRODUZIONE", 0.8, null, 1.4, vigneto: 11), "a", Adesso, default);
        await archivio.SalvaObiettivoAsync(Salva("SFE", 9, null, 9, ente: 2), "a", Adesso, default); // altra azienda

        var dati = await archivio.ObiettiviAsync(2026, 1, default);
        Assert.Equal(3, dati.Obiettivi.Count);
        var sfe = dati.Obiettivi.Single(o => o.IdEnte == 1 && o.Parametro == "SFE");
        Assert.Equal((2.2, 2.7, 3.2, 2, "b"), (sfe.Minimo!.Value, sfe.Obiettivo!.Value, sfe.Massimo!.Value, sfe.Versione, sfe.Autore));
        Assert.Equal([2026], dati.AnniConObiettivi);
        Assert.Equal(2, (await archivio.StoricoObiettivoAsync(1, 0, 2026, "SFE", default)).Count);

        // Nel 2027 l'azienda ha già la SFE: si copia solo la produzione del vigneto.
        await archivio.SalvaObiettivoAsync(Salva("SFE", 1, null, 1.5, anno: 2027), "a", Adesso, default);
        Assert.Equal(1, await archivio.CopiaObiettiviAsync(1, 2026, 2027, "c", Adesso, default));
        var nuovi = await archivio.ObiettiviAsync(2027, 1, default);
        Assert.Equal(1.5, nuovi.Obiettivi.Single(o => o.IdEnte == 1 && o.Parametro == "SFE").Massimo);
        Assert.Equal(11, nuovi.Obiettivi.Single(o => o.Parametro == "PRODUZIONE").IdVigneto);
        Assert.Equal(0, await archivio.CopiaObiettiviAsync(1, 2026, 2027, "c", Adesso, default));

        // Togliere = versione vuota: resta nello storico, non vale più.
        await archivio.SalvaObiettivoAsync(Salva("SFE", null, null, null), "a", Adesso, default);
        Assert.True((await archivio.ObiettiviAsync(2026, 1, default)).Obiettivi.Single(o => o.IdEnte == 1 && o.Parametro == "SFE").Vuoto);
    }

    [Fact]
    public async Task Api_ControllaAutoreValoriEVigneto()
    {
        var controller = Controller(Archivio());
        Assert.IsType<ObjectResult>((await controller.Salva(Salva("SFE", 1, null, 2) with { Autore = " " }, default)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.Salva(Salva("SFE", 3, null, 2), default)).Result);
        // Il vigneto si controlla sullo snapshot: senza snapshot «non pronto».
        Assert.IsType<ConflictObjectResult>((await controller.Salva(Salva("SFE", 1, null, 2, vigneto: 5), default)).Result);

        var ok = (await controller.Salva(Salva("sfe", 1, null, 2), default)).Value!;
        Assert.Equal("SFE", ok.Obiettivi.Single().Parametro);
        Assert.Single((await controller.Get(2026, 1, default)).Value!.Obiettivi);
        Assert.IsType<BadRequestObjectResult>((await controller.Get(1990, null, default)).Result);
    }

    private static RigaIndiceParametroDto Riga(int vigneto, int anno, string p, double? avg, double punteggio, double? max = null) =>
        new(anno, vigneto, null, p, null, max, avg, avg, null, punteggio);

    [Fact]
    public void Cruscotto_InLineaFuoriSenzaDatoEIndiceProvvisorio()
    {
        List<RigaIndiceParametroDto> righe =
        [
            Riga(1, 2026, "SFE", 2.5, 8),
            Riga(1, 2026, "PRODUZIONE", 3, 4),
            Riga(2, 2026, "SFE", 1.0, 2),
            Riga(3, 2026, "AMPELOPATIE", null, 30, max: 2), // sanità 98 %
            // Il 2025 completo per il vigneto 1: indice finale dell'anno prima.
            .. CalcoloIndiceBigot.Parametri.Select(p => Riga(1, 2025, p.Id, 1, 7)),
        ];
        var report = new IndiceBigotReportDto(true, Adesso, 7, "Prova", [], [],
            [new(1, "Alto", null, null, 1000, 2000), new(2, "Basso", null, null, 1000, 2000), new(3, "Centro", null, null, 1000, 2000)], [], righe);
        var obiettivi = new[]
        {
            Obiettivo(7, 0, "SFE", 2, 3),
            Obiettivo(7, 2, "SFE", 0.5, 1.5), // il vigneto 2 ha la sua fascia
            Obiettivo(0, 0, "PRODUZIONE", 0.8, 1.5),
        };

        var c = CruscottoIndice.Calcola(report, obiettivi, 2026);
        var sfe = c.Parametri.Single(p => p.Parametro == "SFE");
        Assert.Equal((2, 2, 0, 1), (sfe.ConDato, sfe.InLinea, sfe.Fuori, sfe.SenzaDato));
        Assert.Equal(1.75, sfe.Valore);
        Assert.Equal(EsitoObiettivo.Sotto, sfe.Esito); // la media 1,75 è sotto la fascia dell'azienda
        Assert.Equal(1, sfe.ValoreAnnoPrima);
        var produzione = c.Parametri.Single(p => p.Parametro == "PRODUZIONE");
        Assert.Equal((1, 0, 1, 2), (produzione.ConDato, produzione.InLinea, produzione.Fuori, produzione.SenzaDato));
        Assert.Equal(LivelloObiettivo.Predefinito, produzione.Obiettivo.Livello);
        Assert.NotNull(produzione.RilievoCheServe);

        var alto = c.Vigneti.Single(v => v.Id == 1);
        Assert.Equal((2, 12.0), (alto.Parametri, alto.IndiceProvvisorio!.Value));
        Assert.Equal(CalcoloIndiceBigot.Parametro("SFE")!.Massimo + CalcoloIndiceBigot.Parametro("PRODUZIONE")!.Massimo, alto.MassimoPossibile, 6);
        Assert.Equal(63, alto.IndiceAnnoPrima);
        Assert.Equal(7, alto.Mancanti.Count());
        Assert.Equal(0, c.Completi);
        Assert.Equal(98, c.Vigneti.Single(v => v.Id == 3).Dettaglio.Single(d => d.Parametro == "AMPELOPATIE").Valore);

        // Un vigneto solo: la fascia è la sua.
        var solo = CruscottoIndice.Calcola(report, obiettivi, 2026, soloVigneto: 2);
        Assert.Equal(EsitoObiettivo.InLinea, solo.Parametri.Single(p => p.Parametro == "SFE").Esito);
        Assert.Single(solo.Vigneti);
    }

    [Fact]
    public void MenuReport_ObiettiviECruscottoAccesi()
    {
        var voci = MenuReport.Gruppi.Single(g => g.Titolo == "Indice Bigot").Voci;
        Assert.Equal("/indice-bigot/obiettivi", voci.Single(v => v.Titolo == "Obiettivi dei parametri").Indirizzo);
        Assert.Equal("/indice-bigot/cruscotto", voci.Single(v => v.Titolo == "A che punto sono").Indirizzo);
    }
}
