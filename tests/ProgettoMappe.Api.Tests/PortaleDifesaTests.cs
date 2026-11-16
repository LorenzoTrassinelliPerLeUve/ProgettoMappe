using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Tabelle della difesa del Portale (tappa 2, task #246): per ogni API di scrittura inserimento, modifica, cancellazione e
/// permesso negato, sull'Api vera con 4Grapes finto (<see cref="DbFinto"/>).
/// </summary>
public class PortaleDifesaTests
{
    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db) Avvia()
    {
        var db = new DbFinto();
        var letture = new LettureDifesaFinte(db);
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiFinti>();
                s.AddSingleton<ILettureDifesaPortale>(letture);
                s.AddSingleton<ILetturePortale>(letture);
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
            }));
        return (api, db);
    }

    private static HttpClient Client(WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> api, int utente)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add(PortaleControllerBase.IntestazioneUtente, utente.ToString());
        return client;
    }

    private static async Task<string?> TitoloAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<ProblemDetails>())?.Title;

    private static async Task<IDictionary<string, string[]>> ErroriAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors;

    // ---- 16. Irroratrici ----

    [Fact]
    public async Task Irroratrici_InserisciModificaElimina_ConLogOp()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);

        var nuova = await client.PostAsJsonAsync("api/portale/irroratrici", new IrroratriceSalvaDto("Atomizzatore 1", 1));
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var esito = (await nuova.Content.ReadFromJsonAsync<EsitoScritturaDto>())!;
        Assert.Equal("1", esito.Chiave);
        Assert.Equal(["nuovo Irroratrice"], esito.Log);
        Assert.Equal("Atomizzatore 1", db.Tabella("Irroratrice")[1]["Descrizione"]);

        var modifica = await client.PutAsJsonAsync("api/portale/irroratrici/1", new IrroratriceSalvaDto("Atomizzatore 2", 2));
        Assert.Equal(HttpStatusCode.OK, modifica.StatusCode);
        Assert.Equal(2, db.Tabella("Irroratrice")[1]["IdTipoIrroratrice"]);

        var lista = await client.GetFromJsonAsync<IrroratriciDto>("api/portale/irroratrici");
        Assert.Equal("Atomizzatore 2", Assert.Single(lista!.Righe).Descrizione);
        Assert.Equal(2, lista.Tipi.Count);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/irroratrici/1")).StatusCode);
        Assert.Empty(db.Tabella("Irroratrice"));
        Assert.Equal(["nuovo", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
        Assert.All(db.LogOp, l => Assert.Equal(PermessiFinti.Master, l.IdUtente));
        // IdRiga = colonna «Id» intera, come SaveAndLog: Irroratrice ha IdIrroratrice, quindi 0.
        Assert.All(db.LogOp, l => Assert.Equal(0, l.IdRiga));
    }

    [Fact]
    public async Task Irroratrici_ControlliEMessaggiDelPortale()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);
        db.Metti("Irroratrice", 1, new() { ["IdIrroratrice"] = 1, ["Descrizione"] = "Tunnel", ["IdTipoIrroratrice"] = 2 });
        db.Metti("Irroratrice", 2, new() { ["IdIrroratrice"] = 2, ["Descrizione"] = "Libera", ["IdTipoIrroratrice"] = 1 });
        db.Metti("Irroratrice_Ente", 7, new() { ["Id"] = 7, ["IdEnte"] = 425, ["IdIrroratrice"] = 1, ["BBCH"] = 60,
            ["VolumeAcqua"] = 300m, ["VelocitaAvanzamento"] = 6m, ["TipologiaUgelli"] = "antideriva" });

        var vuota = await client.PostAsJsonAsync("api/portale/irroratrici", new IrroratriceSalvaDto(" ", null));
        Assert.Equal(HttpStatusCode.BadRequest, vuota.StatusCode);
        var errori = await ErroriAsync(vuota);
        Assert.Equal([PortaleDifesaController.Obbligatorio], errori["Descrizione"]);
        Assert.Equal([PortaleDifesaController.Obbligatorio], errori["IdTipoIrroratrice"]);

        var doppia = await client.PutAsJsonAsync("api/portale/irroratrici/2", new IrroratriceSalvaDto("Tunnel", 1));
        Assert.Equal(HttpStatusCode.BadRequest, doppia.StatusCode);
        Assert.Equal(["Nome già esistente"], (await ErroriAsync(doppia))["Descrizione"]);
        // La stessa riga con la sua descrizione va bene.
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("api/portale/irroratrici/1", new IrroratriceSalvaDto("Tunnel", 1))).StatusCode);

        var usata = await client.DeleteAsync("api/portale/irroratrici/1");
        Assert.Equal(HttpStatusCode.BadRequest, usata.StatusCode);
        Assert.Equal("Non puoi eliminare l'irroratrice in quanto è utilizzata in 1 riferimenti di aziende", await TitoloAsync(usata));
        Assert.True(db.Tabella("Irroratrice").ContainsKey(1));

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("api/portale/irroratrici/99",
            new IrroratriceSalvaDto("Nuova", 1))).StatusCode);
        Assert.Single(db.LogOp);
    }

    [Fact]
    public async Task Irroratrici_PermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        db.Metti("Irroratrice", 1, new() { ["IdIrroratrice"] = 1, ["Descrizione"] = "Tunnel", ["IdTipoIrroratrice"] = 2 });

        // Il consulente ha IRRORATRICI ma non è MasterUser: la sezione 16 non è sua.
        foreach (var utente in new[] { PermessiFinti.Consulente, PermessiFinti.SenzaPermessi })
        {
            var client = Client(api, utente);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/irroratrici")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PostAsJsonAsync("api/portale/irroratrici", new IrroratriceSalvaDto("X", 1))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PutAsJsonAsync("api/portale/irroratrici/1", new IrroratriceSalvaDto("X", 1))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("api/portale/irroratrici/1")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().DeleteAsync("api/portale/irroratrici/1")).StatusCode);
        Assert.Empty(db.LogOp);
        Assert.Equal("Tunnel", db.Tabella("Irroratrice")[1]["Descrizione"]);
    }

    // ---- 17. Irroratrici dell'azienda ----

    private static IrroratriceAziendaSalvaDto Regolazione(int ente = 425, string ugelli = "antideriva") =>
        new(1, ente, 61, 350.5m, 6.2m, ugelli);

    [Fact]
    public async Task IrroratriciAzienda_InserisciModificaElimina_ConsulenteSullaSuaAzienda()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        db.Metti("Irroratrice", 1, new() { ["IdIrroratrice"] = 1, ["Descrizione"] = "Tunnel", ["IdTipoIrroratrice"] = 2 });
        var client = Client(api, PermessiFinti.Consulente);

        var nuova = await client.PostAsJsonAsync("api/portale/irroratrici-azienda", Regolazione());
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var riga = db.Tabella("Irroratrice_Ente")[1];
        Assert.Equal(350.5m, riga["VolumeAcqua"]);
        Assert.False(riga.ContainsKey("NumeroUgelli"));

        riga["NumeroUgelli"] = 12;
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("api/portale/irroratrici-azienda/1", Regolazione(ugelli: "a ventaglio"))).StatusCode);
        Assert.Equal("a ventaglio", db.Tabella("Irroratrice_Ente")[1]["TipologiaUgelli"]);
        // Il numero di ugelli non è nella scheda: resta com'era (il portale lo azzerava).
        Assert.Equal(12, db.Tabella("Irroratrice_Ente")[1]["NumeroUgelli"]);

        var lista = await client.GetFromJsonAsync<IrroratriciAziendaDto>("api/portale/irroratrici-azienda");
        Assert.True(Assert.Single(lista!.Righe).Modificabile);
        Assert.Equal([425, 426], lista.Aziende.Select(a => a.Id));

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/irroratrici-azienda/1")).StatusCode);
        Assert.Empty(db.Tabella("Irroratrice_Ente"));
        Assert.Equal(["nuovo Irroratrice_Ente", "modificato Irroratrice_Ente", "cancellato Irroratrice_Ente"],
            db.LogOp.Select(l => $"{l.Operazione} {l.Tabella}"));
    }

    [Fact]
    public async Task IrroratriciAzienda_ValidazioneEPermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        db.Metti("Irroratrice", 1, new() { ["IdIrroratrice"] = 1, ["Descrizione"] = "Tunnel", ["IdTipoIrroratrice"] = 2 });
        db.Metti("Irroratrice_Ente", 5, new() { ["Id"] = 5, ["IdEnte"] = 426, ["IdIrroratrice"] = 1, ["BBCH"] = 60,
            ["VolumeAcqua"] = 300m, ["VelocitaAvanzamento"] = 6m, ["TipologiaUgelli"] = "x" });
        var consulente = Client(api, PermessiFinti.Consulente);

        var vuota = await consulente.PostAsJsonAsync("api/portale/irroratrici-azienda",
            new IrroratriceAziendaSalvaDto(null, null, null, null, null, ""));
        Assert.Equal(HttpStatusCode.BadRequest, vuota.StatusCode);
        Assert.Equal(["BBCH", "IdEnte", "IdIrroratrice", "TipologiaUgelli", "VelocitaAvanzamento", "VolumeAcqua"],
            (await ErroriAsync(vuota)).Keys.Order());

        // 426 è abbinata ma senza aggiornamenti abilitati, 500 non è abbinata: niente modifiche (né spostarci una riga).
        Assert.Equal(HttpStatusCode.Forbidden,
            (await consulente.PostAsJsonAsync("api/portale/irroratrici-azienda", Regolazione(500))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await consulente.PutAsJsonAsync("api/portale/irroratrici-azienda/5", Regolazione(425))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync("api/portale/irroratrici-azienda/5")).StatusCode);
        var lista = await consulente.GetFromJsonAsync<IrroratriciAziendaDto>("api/portale/irroratrici-azienda");
        Assert.False(Assert.Single(lista!.Righe).Modificabile);

        var senza = Client(api, PermessiFinti.SenzaPermessi);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.GetAsync("api/portale/irroratrici-azienda")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await senza.PostAsJsonAsync("api/portale/irroratrici-azienda", Regolazione())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.DeleteAsync("api/portale/irroratrici-azienda/5")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await Client(api, PermessiFinti.Master).DeleteAsync("api/portale/irroratrici-azienda/99")).StatusCode);
        // Il MasterUser modifica anche 426.
        Assert.Equal(HttpStatusCode.OK,
            (await Client(api, PermessiFinti.Master).PutAsJsonAsync("api/portale/irroratrici-azienda/5", Regolazione(426))).StatusCode);
        Assert.Single(db.LogOp);
    }

    // ---- 13. Linee di difesa ----

    private static LineaDifesaDto Linea(string pressione = "ALTA", int bbch = 65, string? note = null) =>
        new(0, 2026, 1, 1, 12, bbch, pressione, "12345", 1.5m, 20, 1000, 20, 99m, note);

    [Fact]
    public async Task LineeDifesa_InserisciModificaElimina_ConLogOp()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);

        var nuova = await client.PostAsJsonAsync("api/portale/linee-difesa", Linea());
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var riga = db.Tabella("LineaDifesa")[1];
        Assert.Equal(1.5m, riga["Dosaggio_Rif_Perleuve"]);
        Assert.Equal(20, riga["UM_H20"]);
        Assert.False(riga.ContainsKey("Concentrazione"));   // il portale non la assegna

        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("api/portale/linee-difesa/1", Linea(note: "seconda passata"))).StatusCode);
        Assert.Equal("seconda passata", db.Tabella("LineaDifesa")[1]["Note"]);
        var letta = await client.GetFromJsonAsync<LineaDifesaDto>("api/portale/linee-difesa/1");
        Assert.Equal("12345", letta!.NumRegProdotto);
        var lista = await client.GetFromJsonAsync<LineeDifesaDto>("api/portale/linee-difesa?anno=2026");
        Assert.True(lista!.Modificabili);
        Assert.Single(lista.Righe);
        Assert.Empty((await client.GetFromJsonAsync<LineeDifesaDto>("api/portale/linee-difesa?anno=2025"))!.Righe);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/linee-difesa/1")).StatusCode);
        Assert.Empty(db.Tabella("LineaDifesa"));
        Assert.Equal(["nuovo", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
        Assert.All(db.LogOp, l => Assert.Equal(1, l.IdRiga));
    }

    [Fact]
    public async Task LineeDifesa_ControlliEMessaggiDelPortale()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/portale/linee-difesa", Linea())).StatusCode);

        var doppia = await client.PostAsJsonAsync("api/portale/linee-difesa", Linea(note: "diversa"));
        Assert.Equal(HttpStatusCode.BadRequest, doppia.StatusCode);
        var errori = await ErroriAsync(doppia);
        Assert.Equal(7, errori.Count);
        Assert.All(errori.Values, e => Assert.Equal(["Linea difesa già esistente"], e));
        // Stessa riga: va bene; un'altra pressione non è un doppione.
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/linee-difesa/1", Linea())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/portale/linee-difesa", Linea("BASSA"))).StatusCode);

        var vuota = await client.PostAsJsonAsync("api/portale/linee-difesa",
            new LineaDifesaDto(0, null, null, null, null, null, "ALTO", null, null, null, null, null, null, null));
        var campi = await ErroriAsync(vuota);
        Assert.Equal(11, campi.Count);
        Assert.Equal(["Scegli ALTA o BASSA."], campi["Pressione"]);
        Assert.Equal([PortaleDifesaController.Obbligatorio], campi["H20RifPerleuve"]);
        Assert.Equal(2, db.Tabella("LineaDifesa").Count);
    }

    [Fact]
    public async Task LineeDifesa_PermessoNegato_ESoloLeLineeDelleSueAziende()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var master = Client(api, PermessiFinti.Master);
        Assert.Equal(HttpStatusCode.OK, (await master.PostAsJsonAsync("api/portale/linee-difesa", Linea())).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await master.PostAsJsonAsync("api/portale/linee-difesa", Linea() with { IdTipologiaLinea = 3 })).StatusCode);

        // Il consulente ha LINEE_DI_DIFESA: legge le linee delle sue aziende (425 → 1, 426 → 2), non modifica.
        var consulente = Client(api, PermessiFinti.Consulente);
        var lista = await consulente.GetFromJsonAsync<LineeDifesaDto>("api/portale/linee-difesa?anno=2026");
        Assert.False(lista!.Modificabili);
        Assert.Equal([1], lista.Righe.Select(r => r.IdTipologiaLinea));
        Assert.Equal([1], (await consulente.GetFromJsonAsync<TipologieAziendaDto>("api/portale/linee-difesa/tipologie?azienda=425"))!.Tipologie);
        Assert.Empty((await consulente.GetFromJsonAsync<TipologieAziendaDto>("api/portale/linee-difesa/tipologie?azienda=500"))!.Tipologie);
        var vietata = await consulente.PostAsJsonAsync("api/portale/linee-difesa", Linea("BASSA"));
        Assert.Equal(HttpStatusCode.Forbidden, vietata.StatusCode);
        Assert.Equal("Solo i MasterUser possono modificare le linee di difesa.", await TitoloAsync(vietata));
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PutAsJsonAsync("api/portale/linee-difesa/1", Linea())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync("api/portale/linee-difesa/1")).StatusCode);

        var senza = Client(api, PermessiFinti.SenzaPermessi);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.GetAsync("api/portale/linee-difesa")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.PostAsJsonAsync("api/portale/linee-difesa", Linea())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.DeleteAsync("api/portale/linee-difesa/1")).StatusCode);
        Assert.Equal(2, db.LogOp.Count);
    }

    [Fact]
    public void LineeDifesa_AnniDelFiltro() =>
        Assert.Equal([2026, 2025, 2024, 2023, 2022, 2021, 2020], PortaleDifesaController.AnniLinee(2026));

    // ---- 14. Soglie trattamenti ----

    private static DbFinto ConSoglieGlobali(DbFinto db)
    {
        db.Metti("AvversitaDaTrattare", 11, new() { ["Id"] = 11, ["Descrizione"] = "OIDIO" });
        db.Metti("AvversitaDaTrattare", 12, new() { ["Id"] = 12, ["Descrizione"] = "PERONOSPORA" });
        db.Metti("SogliaTrattamento", 24, new() { ["Id"] = 24, ["IdAvversitaDaTrattare"] = 11, ["ValoriGlobali"] = true,
            ["SogliaProbabilitaInfezione"] = 30m, ["SogliaPressione"] = 1m, ["SogliaProtezione"] = 30m });
        return db;
    }

    [Fact]
    public async Task Soglie_InserisciModificaRipristina_PerAzienda()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        ConSoglieGlobali(db);
        var client = Client(api, PermessiFinti.Consulente);

        var livello = await client.GetFromJsonAsync<SoglieTrattamentiDto>("api/portale/soglie-trattamenti?azienda=425");
        Assert.Equal("Soglie Ente: Azienda A", livello!.Titolo);
        Assert.True(livello.Modificabile);
        var oidio = livello.Righe.Single(r => r.IdAvversita == 11);
        Assert.Null(oidio.IdSoglia);
        Assert.Equal(30m, Assert.Single(oidio.Ereditate).Probabilita);

        var nuova = await client.PutAsJsonAsync("api/portale/soglie-trattamenti/11?azienda=425", new SoglieSalvaDto(40, 2, 35));
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var riga = db.Tabella("SogliaTrattamento")[25];
        Assert.Equal(425, riga["IdEnte"]);
        Assert.Equal(false, riga["ValoriGlobali"]);
        Assert.Equal(40m, riga["SogliaProbabilitaInfezione"]);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("api/portale/soglie-trattamenti/11?azienda=425", new SoglieSalvaDto(45, 2, 35))).StatusCode);
        Assert.Equal(45m, db.Tabella("SogliaTrattamento")[25]["SogliaProbabilitaInfezione"]);
        Assert.Equal(2, db.Tabella("SogliaTrattamento").Count);

        // Il rischio eredita da ente e globali; il vigneto 4250 (rischio 1) da tutti e tre.
        var vigneto = await client.GetFromJsonAsync<SoglieTrattamentiDto>("api/portale/soglie-trattamenti?azienda=425&vigneto=4250");
        Assert.Equal(["Soglia globale", "Soglia Ente", "Soglia ALTO / ALTO"],
            vigneto!.Righe.Single(r => r.IdAvversita == 11).Ereditate.Select(e => e.Etichetta));
        Assert.Equal(45m, vigneto.Righe.Single(r => r.IdAvversita == 11).Ereditate[1].Probabilita);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/soglie-trattamenti/11?azienda=425")).StatusCode);
        Assert.False(db.Tabella("SogliaTrattamento").ContainsKey(25));
        Assert.Equal(["nuovo", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("api/portale/soglie-trattamenti/11?azienda=425")).StatusCode);
    }

    [Fact]
    public async Task Soglie_PermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        ConSoglieGlobali(db);
        var consulente = Client(api, PermessiFinti.Consulente);

        // Globali: il consulente le vede ma non le salva; le globali non si ripristinano.
        var globali = await consulente.GetFromJsonAsync<SoglieTrattamentiDto>("api/portale/soglie-trattamenti");
        Assert.False(globali!.Modificabile);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await consulente.PutAsJsonAsync("api/portale/soglie-trattamenti/11", new SoglieSalvaDto(1, 1, 1))).StatusCode);
        // Azienda non abbinata e vigneto non suo.
        Assert.Equal(HttpStatusCode.NotFound,
            (await consulente.PutAsJsonAsync("api/portale/soglie-trattamenti/11?azienda=500", new SoglieSalvaDto(1, 1, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PutAsJsonAsync("api/portale/soglie-trattamenti/11?azienda=426&vigneto=4260",
            new SoglieSalvaDto(1, 1, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.SenzaPermessi)
            .PutAsJsonAsync("api/portale/soglie-trattamenti/11?azienda=425", new SoglieSalvaDto(1, 1, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client(api, PermessiFinti.SenzaPermessi).GetAsync("api/portale/soglie-trattamenti")).StatusCode);

        var master = Client(api, PermessiFinti.Master);
        Assert.Equal(HttpStatusCode.BadRequest, (await master.DeleteAsync("api/portale/soglie-trattamenti/11")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await master.PutAsJsonAsync("api/portale/soglie-trattamenti/11", new SoglieSalvaDto(25, 1, 30))).StatusCode);
        Assert.Equal(25m, db.Tabella("SogliaTrattamento")[24]["SogliaProbabilitaInfezione"]);
        Assert.Single(db.LogOp);
    }

    [Fact]
    public void Soglie_LivelliComeIlPortale()
    {
        var soglie = new List<SogliaTrattamentoDto>
        {
            new(1, 11, null, null, null, 30, 1, 30, true),
            new(2, 11, 425, null, null, 40, null, null, false),
            new(3, 11, 425, 1, null, 50, null, null, false),
            new(4, 11, 999, 1, null, 99, null, null, false),   // stesso rischio, altra azienda: non conta
            new(5, 11, null, null, 4250, 60, null, null, false),
        };
        Assert.Equal(1, CalcoloSoglie.Trova(soglie, new LivelloSoglie(), 11)!.Id);
        Assert.Equal(2, CalcoloSoglie.Trova(soglie, new LivelloSoglie(425), 11)!.Id);
        Assert.Equal(3, CalcoloSoglie.Trova(soglie, new LivelloSoglie(425, 1), 11)!.Id);
        Assert.Equal(5, CalcoloSoglie.Trova(soglie, new LivelloSoglie(425, 1, 4250), 11)!.Id);
        Assert.Null(CalcoloSoglie.Trova(soglie, new LivelloSoglie(426), 11));
        // Un vigneto senza rischio: niente livello del rischio fra quelli sopra.
        var riga = Assert.Single(CalcoloSoglie.Righe([new(11, "OIDIO")], soglie, new LivelloSoglie(425, null, 4250), null));
        Assert.Equal([30m, 40m], riga.Ereditate.Select(e => e.Probabilita));
        Assert.Equal(true, CalcoloSoglie.ChiaveNuova(new LivelloSoglie(), 11)["ValoriGlobali"]);
        Assert.Equal(4250, CalcoloSoglie.ChiaveNuova(new LivelloSoglie(425, 1, 4250), 11)["IdVigneto"]);
    }

    // ---- 15. Avversità da trattare ----

    [Fact]
    public async Task Avversita_SoloMasterUser_SenzaScritture()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        db.Metti("AvversitaDaTrattare", 12, new() { ["Id"] = 12, ["Descrizione"] = "PERONOSPORA" });

        var righe = await Client(api, PermessiFinti.Master).GetFromJsonAsync<List<AvversitaDaTrattareDto>>("api/portale/avversita");
        Assert.Equal("PERONOSPORA", Assert.Single(righe!).Descrizione);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.Consulente).GetAsync("api/portale/avversita")).StatusCode);
        // Il portale non salva niente: nessuna API di scrittura.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await Client(api, PermessiFinti.Master)
            .PutAsJsonAsync("api/portale/avversita", new { Descrizione = "X" })).StatusCode);
    }

    [Fact]
    public void ComandoConta_ParametriENull()
    {
        var comando = ComandiScrittura.Conta(TabelleScrivibili.Irroratrice,
            new Dictionary<string, object?> { ["Descrizione"] = "Tunnel", ["IdTipoIrroratrice"] = null }, 3);
        Assert.Equal("SELECT COUNT(*) FROM dbo.[Irroratrice] WHERE [Descrizione] = @u0 AND [IdTipoIrroratrice] IS NULL " +
                     "AND [IdIrroratrice] <> @esclusa;", comando.Testo);
        Assert.Equal(["@u0", "@esclusa"], comando.Parametri.Select(p => p.ParameterName));
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Conta(TabelleScrivibili.Irroratrice,
            new Dictionary<string, object?> { ["Descrizione; DROP"] = "x" }));
    }
}
