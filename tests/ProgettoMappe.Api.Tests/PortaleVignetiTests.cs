using System.Net;
using System.Net.Http.Json;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Web.Components.Portale.Sezioni;
using ProgettoMappe.Web.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Letture della scheda vigneto sul <see cref="DbFinto"/>: tabella «Vigneto» con le colonne di 4Grapes, l'azienda è
/// <c>Vigna_IdVigna</c> (vigna = azienda, per semplicità), il comune in «VignetoMaps».
/// </summary>
public sealed class LettureVignetiFinte(DbFinto db) : ILettureVignetiPortale
{
    public static readonly MenuVignetoDto Menu = new(
        [new("7", "Glera"), new("8", "Merlot")], [new("1", "Goccia")], [new("2", "Collina")], [new("3", "Guyot")],
        [new("SO4", "SO4")], [new("VCR1", "VCR 1", 7), new("R3", "R 3", 8)], [new("1", "Biologica")],
        [new("1", "Linea 1")], [new("1", "ALTO / ALTO")], []);

    public Task<VignetoSchedaDto?> VignetoAsync(int idVigneto, CancellationToken ct = default)
    {
        if (!db.Tabella("Vigneto").TryGetValue(idVigneto, out var r)) return Task.FromResult<VignetoSchedaDto?>(null);
        T? V<T>(string c) => r.GetValueOrDefault(c) is T t ? t : default;
        var ente = (int)r["Vigna_IdVigna"]!;
        db.Tabella("VignetoMaps").TryGetValue(idVigneto, out var m);
        var dati = new VignetoSalvaDto(V<string>("NomeVigneto"), V<int?>("Vitigno_idVitigno"), V<int?>("AnnoImpianto"),
            V<decimal?>("SullaFila"), V<decimal?>("TraFila"), r.GetValueOrDefault("Estirpo") is true, V<DateTime?>("DataEstirpo"),
            r.GetValueOrDefault("Carico") is true, V<int?>("IdTipologiaLinea"), V<int?>("IdRischio"), r.GetValueOrDefault("ElaboraMeteo") is true,
            V<int?>("PesoMedioGrappolo"), V<string>("Note"), V<int?>("Irrigazione_idIrrigazione"), V<int?>("Giacitura_idGiacitura"),
            V<int?>("FormaAllevamento_idFormaAllevamento"), V<string>("Portainnesto_idPortainnesto"), V<string>("Clone_IdClone"),
            V<int?>("Conduzione_idConduzione"), V<string>("Esposizione"), V<int?>("QuotaSLM"), V<int?>("Pendenza"), V<int?>("Orientamento"),
            V<int?>("NumeroPianteContate"), V<decimal?>("SuperficieDichiarata"), V<decimal?>("SuperficieMisurata"));
        return Task.FromResult<VignetoSchedaDto?>(new VignetoSchedaDto(idVigneto, ente, $"Azienda {ente}", "Vigna", dati,
            V<decimal?>("Latitudine"), V<decimal?>("Longitudine"), m?["Regione"] as string, null, m?["Comune"] as string,
            V<int?>("AnnoEstirpo"), null, false, false, false, Menu with { Vitigni = [] }));
    }

    public Task<MenuVignetoDto> MenuAsync(CancellationToken ct = default) => Task.FromResult(Menu);
}

/// <summary>
/// Scheda del vigneto del menù Nutrizione (task #296): modifica come <c>VignetiController.Form</c> del portale con le coordinate
/// solo da leggere, eliminazione con <c>VignetoMaps</c>, LogOp, permessi; sull'Api vera con 4Grapes finto.
/// </summary>
public class PortaleVignetiTests
{
    private static Dictionary<string, object?> Riga(int id, int ente, string nome) => new()
    {
        ["IdVigneto"] = id, ["NomeVigneto"] = nome, ["Vigna_IdVigna"] = ente, ["Vitigno_idVitigno"] = 7, ["Estirpo"] = false,
        ["Carico"] = true, ["ElaboraMeteo"] = false, ["IdTipologiaLinea"] = 1, ["IdRischio"] = null,
        ["Latitudine"] = 45.912345678901m, ["Longitudine"] = 13.412345678901m, ["Maps"] = true,
    };

    private static DbFinto Prepara(DbFinto db)
    {
        db.Metti("Vigneto", 4250, Riga(4250, 425, "Ronco"));
        db.Metti("Vigneto", 4251, Riga(4251, 425, "Bosco"));
        db.Metti("Vigneto", 4260, Riga(4260, 426, "Piano"));
        db.Metti("Vigneto", 5000, Riga(5000, 500, "Colle"));
        db.Metti("VignetoMaps", 4250, new() { ["IdVigneto"] = 4250, ["Regione"] = "Friuli", ["Comune"] = "Cormons" });
        return db;
    }

    private static VignetoSalvaDto Dati(string nome = "Ronco alto") => new(nome, 8, 2004, 0.9m, 2.5m, false, null, true,
        null, 1, true, 150, " note ", 1, 2, 3, "SO4", "R3", 1, "Sud", 120, 5, 90, 4100, 10000m, 9800.5m);

    [Fact]
    public async Task Scheda_Master_PermessiETendine()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);

        var scheda = await PortaleDifesaTests.Client(api, PermessiFinti.Master).GetFromJsonAsync<VignetoSchedaDto>("api/portale/vigneti/4250");
        Assert.Equal("Ronco", scheda!.Dati.NomeVigneto);
        Assert.Equal(425, scheda.IdEnte);
        Assert.Equal(45.912345678901m, scheda.Latitudine);
        Assert.Equal("Cormons", scheda.Comune);
        Assert.True(scheda.Modificabile && scheda.PuoLineaRischio && scheda.PuoElaboraMeteo);
        Assert.Equal(2, scheda.Menu.Vitigni.Count);

        Assert.Equal(HttpStatusCode.NotFound,
            (await PortaleDifesaTests.Client(api, PermessiFinti.Master).GetAsync("api/portale/vigneti/9999")).StatusCode);
    }

    [Fact]
    public async Task Salva_TuttiICampi_CoordinateIntatte_ConLogOp()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);

        var risposta = await PortaleDifesaTests.Client(api, PermessiFinti.Master).PutAsJsonAsync("api/portale/vigneti/4250", Dati());
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Equal(["modificato Vigneto"], (await risposta.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Log);

        var riga = db.Tabella("Vigneto")[4250];
        Assert.Equal("Ronco alto", riga["NomeVigneto"]);
        Assert.Equal(8, riga["Vitigno_idVitigno"]);
        Assert.Equal(2.5m, riga["TraFila"]);
        Assert.Equal("note", riga["Note"]);
        Assert.Equal("R3", riga["Clone_IdClone"]);
        Assert.Equal(9800.5m, riga["SuperficieMisurata"]);
        Assert.Null(riga["IdTipologiaLinea"]);
        Assert.Equal(true, riga["ElaboraMeteo"]);
        Assert.IsType<DateTime>(riga["DataAggiornamentoStato"]);
        // Coordinate, Maps e comune non si toccano (GeoRef gira solo sul server del portale).
        Assert.Equal(45.912345678901m, riga["Latitudine"]);
        Assert.Equal(13.412345678901m, riga["Longitudine"]);
        Assert.Equal(true, riga["Maps"]);
        Assert.True(db.Tabella("VignetoMaps").ContainsKey(4250));

        var log = Assert.Single(db.LogOp);
        Assert.Equal(("modificato", "Vigneto", 0), (log.Operazione, log.Tabella, log.IdRiga));
        Assert.Contains("\"NomeVigneto\":\"Ronco alto\"", log.Json);
    }

    [Fact]
    public async Task Salva_Estirpo_DataEAnno_ComeIlPortale()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);
        var client = PortaleDifesaTests.Client(api, PermessiFinti.Master);

        var senzaData = await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati() with { Estirpo = true });
        Assert.Equal(HttpStatusCode.BadRequest, senzaData.StatusCode);
        Assert.Contains("DataEstirpo", (await PortaleDifesaTests.ErroriAsync(senzaData)).Keys);

        var estirpo = await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati() with { Estirpo = true, DataEstirpo = new DateTime(2025, 11, 3) });
        Assert.Equal(HttpStatusCode.OK, estirpo.StatusCode);
        Assert.Equal(new DateTime(2025, 11, 3), db.Tabella("Vigneto")[4250]["DataEstirpo"]);
        Assert.Equal(2025, db.Tabella("Vigneto")[4250]["AnnoEstirpo"]);

        // Togliendo «Estirpo» data e anno restano quelli che c'erano, come nel portale.
        await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati());
        Assert.Equal(false, db.Tabella("Vigneto")[4250]["Estirpo"]);
        Assert.Equal(2025, db.Tabella("Vigneto")[4250]["AnnoEstirpo"]);
    }

    [Fact]
    public async Task Salva_NomeGiaEsistenteNellaStessaVigna()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);
        var client = PortaleDifesaTests.Client(api, PermessiFinti.Master);

        var doppio = await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati("Bosco"));
        Assert.Equal(HttpStatusCode.BadRequest, doppio.StatusCode);
        Assert.Equal([PortaleVignetiController.NomeGiaEsistente], (await PortaleDifesaTests.ErroriAsync(doppio))["NomeVigneto"]);
        Assert.Equal("Ronco", db.Tabella("Vigneto")[4250]["NomeVigneto"]);
        Assert.Empty(db.LogOp);

        // Lo stesso nome in un'altra vigna va bene, e anche tenere il proprio.
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati("Piano"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati("Piano"))).StatusCode);
    }

    [Fact]
    public void Valida_CampiObbligatoriELimitiDelleColonne()
    {
        var errori = PortaleVignetiController.Valida(Dati(" ") with
        {
            Vitigno_idVitigno = null, TraFila = 12345.5m, SullaFila = 1.234m, Note = new string('x', 2001), Clone_IdClone = new string('c', 51),
            AnnoImpianto = -1,
        });
        Assert.Equal(["NomeVigneto", "Vitigno_idVitigno", "Note", "Clone_IdClone", "SullaFila", "TraFila", "AnnoImpianto"], errori.Keys);
        Assert.Empty(PortaleVignetiController.Valida(Dati()));
    }

    [Fact]
    public async Task Consulente_SoloAziendeAbbinate_ModificaSoloConAggiornamentiAbilitati()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);
        var client = PortaleDifesaTests.Client(api, PermessiFinti.Consulente);

        // 425 abbinata e modificabile: salva, ma linea, rischio ed «Elabora meteo» restano quelli che c'erano.
        var scheda = await client.GetFromJsonAsync<VignetoSchedaDto>("api/portale/vigneti/4250");
        Assert.True(scheda!.Modificabile);
        Assert.False(scheda.PuoLineaRischio || scheda.PuoElaboraMeteo);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/vigneti/4250", Dati())).StatusCode);
        Assert.Equal(1, db.Tabella("Vigneto")[4250]["IdTipologiaLinea"]);
        Assert.Equal(false, db.Tabella("Vigneto")[4250]["ElaboraMeteo"]);

        // 426 abbinata in sola lettura: la scheda si apre, salvare ed eliminare no.
        Assert.False((await client.GetFromJsonAsync<VignetoSchedaDto>("api/portale/vigneti/4260"))!.Modificabile);
        var vietata = await client.PutAsJsonAsync("api/portale/vigneti/4260", Dati());
        Assert.Equal(HttpStatusCode.Forbidden, vietata.StatusCode);
        Assert.Equal(PortaleVignetiController.NonModificabile, await PortaleDifesaTests.TitoloAsync(vietata));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("api/portale/vigneti/4260")).StatusCode);

        // 500 non abbinata: niente.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/vigneti/5000")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("api/portale/vigneti/5000", Dati())).StatusCode);
        Assert.Equal("Colle", db.Tabella("Vigneto")[5000]["NomeVigneto"]);

        // Senza VIGNETI: 403.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PortaleDifesaTests.Client(api, PermessiFinti.SenzaPermessi).GetAsync("api/portale/vigneti/4250")).StatusCode);
    }

    [Fact]
    public async Task Lista_PulsanteModificaComeIlPortale()
    {
        var (api, _) = PortaleDifesaTests.Avvia();
        await using var _a = api;

        var suoi = await PortaleDifesaTests.Client(api, PermessiFinti.Consulente).GetFromJsonAsync<List<VignetoPortaleDto>>("api/portale/vigneti");
        Assert.All(suoi!, v => Assert.Equal(v.IdEnte == 425, v.Modificabile));
        var tutti = await PortaleDifesaTests.Client(api, PermessiFinti.Master).GetFromJsonAsync<List<VignetoPortaleDto>>("api/portale/vigneti");
        Assert.All(tutti!, v => Assert.True(v.Modificabile));
    }

    [Fact]
    public async Task Elimina_ConVignetoMaps_UnaSolaRigaDiLogOp()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        await using var _ = api;
        Prepara(db);
        var client = PortaleDifesaTests.Client(api, PermessiFinti.Master);

        var risposta = await client.DeleteAsync("api/portale/vigneti/4250");
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        Assert.Equal(["cancellato Vigneto"], (await risposta.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Log);
        Assert.False(db.Tabella("Vigneto").ContainsKey(4250));
        Assert.False(db.Tabella("VignetoMaps").ContainsKey(4250));
        Assert.Contains("\"NomeVigneto\":\"Ronco\"", Assert.Single(db.LogOp).Json);

        // Senza VignetoMaps si cancella solo il vigneto; uno che non c'è più → 404.
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/vigneti/4251")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("api/portale/vigneti/4251")).StatusCode);
    }

    [Theory]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint \"FK_Rilievo_Vigneto\". The conflict occurred in database \"4g\", table \"dbo.Rilievo\", column 'Vigneto_IdVigneto'.", "ha dei rilievi")]
    [InlineData("L'istruzione DELETE è in conflitto con il vincolo REFERENCE \"FK_MeteoGiornaliero_Vigneto\". Il conflitto si è verificato nella tabella \"dbo.MeteoGiornaliero\", colonna 'IdVigneto' del database \"4g\".", "ha dei dati meteo")]
    [InlineData("conflict in table \"dbo.MorfometriaVignetoPunto\"", "ha dei dati morfometrici")]
    [InlineData("conflict in table \"dbo.TabellaNuova\"", "(tabella TabellaNuova)")]
    [InlineData("errore senza tabella", "ha dei dati collegati")]
    public void Bloccato_DiceCosaImpedisceLEliminazione(string messaggioSql, string atteso)
    {
        var testo = PortaleVignetiController.Bloccato(messaggioSql);
        Assert.Contains(atteso, testo);
        Assert.Contains("Niente è stato cambiato", testo);
    }

    [Fact]
    public void Modulo_TendineConValoreAttuale_CloniDelVitigno_DataEstirpoProposta()
    {
        var scheda = new VignetoSchedaDto(4250, 425, "Azienda A", null, Dati() with { Clone_IdClone = "VCR1" }, null, null, null, null, null,
            null, null, true, true, true, LettureVignetiFinte.Menu);
        var modulo = ModuloVigneto.Da(scheda, new DateTime(2026, 11, 16, 10, 0, 0));
        Assert.Equal(new DateTime(2026, 11, 16), modulo.DataEstirpo);
        // Vitigno 8: solo il suo clone, più quello attuale (di un altro vitigno) per non perderlo salvando.
        Assert.Equal(["VCR1", "R3"], modulo.Cloni().Select(c => c.Id));
        Assert.Equal("99 (valore attuale)", ModuloVigneto.Voci([], 99).Single().Testo);
        Assert.Empty(ModuloVigneto.Voci([], null));
        Assert.Equal(modulo.Dto() with { DataEstirpo = null }, Dati() with { Clone_IdClone = "VCR1" });
        Assert.Contains("GeoRef", SezioneVigneti.NotaCoordinate);
        Assert.Equal("—", ModuloVigneto.Coordinata(null));
    }
}
