using System.Net;
using System.Net.Http.Json;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Rilievi della nutrizione del Portale (tappa 2, task #246): per ogni sezione inserimento, modifica, cancellazione e permesso
/// negato, sull'Api vera con 4Grapes finto (<see cref="DbFinto"/>).
/// </summary>
public class PortaleNutrizioneTests
{
    private static RilievoSalvaDto Fallanza(int vigneto = 4250, int giorno = 10, string fallanze = "3.5") =>
        new(vigneto, new DateTime(2026, 6, giorno), new Dictionary<string, string?>
        {
            ["PercInAllevamento"] = "1.5", ["PercCapitozzata"] = "0.5", ["PercPresenteMorta"] = "2", ["PercFallanze"] = fallanze,
            ["NrOrganiControllati"] = "100",
        });

    private static async Task<long> ChiaveAsync(HttpResponseMessage risposta)
    {
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        return long.Parse((await risposta.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Chiave!);
    }

    // ---- 7. Fallanze ----

    [Fact]
    public async Task Fallanze_InserisciModificaElimina_ComeIlPortale()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Consulente);

        var ticks = await ChiaveAsync(await client.PostAsJsonAsync("api/portale/fallanze", Fallanza()));
        var riga = db.Tabella("Rilievo")[new DateTime(ticks)];
        Assert.Equal(109, riga["TipoRilievo_idTipoRilievo"]);
        Assert.Equal(PermessiFinti.Consulente, riga["IdUtente"]);
        Assert.Equal(425, riga["IdAziendaDaApp"]);
        Assert.Equal(7, riga["IdVitignoDaApp"]);
        Assert.Equal(45.9m, riga["Latitudine"]);
        Assert.Equal(false, riga["RichiestoCampione"]);
        Assert.Equal(3.5m, riga["PercFallanze"]);
        Assert.Equal(100, riga["NrOrganiControllati"]);
        Assert.Equal(new DateTime(2026, 6, 10), riga["FineRilievo"]);

        // Stesso giorno con un'ora: la data non cambia (il portale la cambia solo se cambia il giorno).
        riga["InizioRilievo"] = new DateTime(2026, 6, 10, 9, 30, 0);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"api/portale/fallanze/{ticks}", Fallanza(fallanze: "4"))).StatusCode);
        riga = db.Tabella("Rilievo")[new DateTime(ticks)];
        Assert.Equal(new DateTime(2026, 6, 10, 9, 30, 0), riga["InizioRilievo"]);
        Assert.Equal(4m, riga["PercFallanze"]);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"api/portale/fallanze/{ticks}", Fallanza(giorno: 12))).StatusCode);
        Assert.Equal(new DateTime(2026, 6, 12), db.Tabella("Rilievo")[new DateTime(ticks)]["InizioRilievo"]);

        var lista = await client.GetFromJsonAsync<RilieviDto>("api/portale/fallanze");
        Assert.True(lista!.ConData);
        Assert.Equal(5, lista.Campi.Count);
        var letta = Assert.Single(lista.Righe);
        Assert.True(letta.Modificabile);
        Assert.Equal(ticks, letta.Id);
        Assert.Equal("3.5", letta.Valori["PercFallanze"]);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"api/portale/fallanze/{ticks}")).StatusCode);
        Assert.Empty(db.Tabella("Rilievo"));
        Assert.Equal(["nuovo Rilievo", "modificato Rilievo", "modificato Rilievo", "cancellato Rilievo"],
            db.LogOp.Select(l => $"{l.Operazione} {l.Tabella}"));
        Assert.All(db.LogOp, l => Assert.Equal(0, l.IdRiga));
    }

    [Fact]
    public async Task Fallanze_ValidazioneEPermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var master = Client(api, PermessiFinti.Master);
        var ticks426 = await ChiaveAsync(await master.PostAsJsonAsync("api/portale/fallanze", Fallanza(4260)));

        var vuota = await master.PostAsJsonAsync("api/portale/fallanze",
            new RilievoSalvaDto(null, null, new Dictionary<string, string?> { ["PercFallanze"] = "tanto", ["NrOrganiControllati"] = "1.5" }));
        var errori = await ErroriAsync(vuota);
        Assert.Equal(["Data", "IdVigneto", "NrOrganiControllati", "PercFallanze"], errori.Keys.Order());
        Assert.Equal(["Numero non valido."], errori["PercFallanze"]);
        Assert.Equal(["Vigneto non trovato"],
            (await ErroriAsync(await master.PostAsJsonAsync("api/portale/fallanze", Fallanza(9999))))["IdVigneto"]);
        Assert.Equal(["Valore troppo grande."], (await ErroriAsync(await master.PostAsJsonAsync("api/portale/fallanze",
            Fallanza(fallanze: "1000000"))))["PercFallanze"]);

        // Il consulente: 500 non abbinata (niente inserimento), 426 abbinata ma senza aggiornamenti (niente modifica né eliminazione).
        var consulente = Client(api, PermessiFinti.Consulente);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PostAsJsonAsync("api/portale/fallanze", Fallanza(5000))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await consulente.PutAsJsonAsync($"api/portale/fallanze/{ticks426}", Fallanza(4260))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync($"api/portale/fallanze/{ticks426}")).StatusCode);
        Assert.False(Assert.Single((await consulente.GetFromJsonAsync<RilieviDto>("api/portale/fallanze"))!.Righe).Modificabile);
        Assert.Empty((await consulente.GetFromJsonAsync<List<VignetoMenuDto>>("api/portale/fallanze/vigneti?azienda=500"))!);
        Assert.Single((await consulente.GetFromJsonAsync<List<VignetoMenuDto>>("api/portale/fallanze/vigneti?azienda=425"))!);

        var senza = Client(api, PermessiFinti.SenzaPermessi);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.GetAsync("api/portale/fallanze")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.PostAsJsonAsync("api/portale/fallanze", Fallanza())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await senza.DeleteAsync($"api/portale/fallanze/{ticks426}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await master.DeleteAsync("api/portale/fallanze/123")).StatusCode);
        Assert.Single(db.LogOp);
    }

    // ---- 8. Analisi uve ----

    private static RilievoSalvaDto Analisi(int vigneto = 4250, string zuccheri = "21.4") =>
        new(vigneto, new DateTime(2026, 9, 1), new Dictionary<string, string?>
        {
            ["Zuccheri"] = zuccheri, ["AciditaTotale"] = "6.8", ["PH"] = "3.25", ["AcMalico"] = null, ["Tinta"] = "",
        });

    [Fact]
    public async Task AnalisiUve_InserisciModificaElimina()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);

        var ticks = await ChiaveAsync(await client.PostAsJsonAsync("api/portale/analisi-uve", Analisi()));
        var riga = db.Tabella("Rilievo")[new DateTime(ticks)];
        Assert.Equal(110, riga["TipoRilievo_idTipoRilievo"]);
        Assert.Equal(21.4m, riga["Zuccheri"]);
        Assert.Null(riga["Tinta"]);
        Assert.False(riga.ContainsKey("PercFallanze"));

        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"api/portale/analisi-uve/{ticks}", Analisi(zuccheri: "22"))).StatusCode);
        Assert.Equal(22m, db.Tabella("Rilievo")[new DateTime(ticks)]["Zuccheri"]);
        // Un rilievo di un altro tipo non si apre da questa sezione.
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"api/portale/fallanze/{ticks}", Fallanza())).StatusCode);

        var lista = await client.GetFromJsonAsync<RilieviDto>("api/portale/analisi-uve");
        Assert.Equal(["Zuccheri", "AciditaTotale", "PH", "AcMalico", "Tinta"], lista!.Campi.Select(c => c.Nome));
        Assert.Equal("22", Assert.Single(lista.Righe).Valori["Zuccheri"]);
        Assert.Empty((await client.GetFromJsonAsync<RilieviDto>("api/portale/fallanze"))!.Righe);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"api/portale/analisi-uve/{ticks}")).StatusCode);
        Assert.Equal(["nuovo", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
    }

    [Fact]
    public async Task AnalisiUve_PermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var ticks = await ChiaveAsync(await Client(api, PermessiFinti.Master).PostAsJsonAsync("api/portale/analisi-uve", Analisi()));

        // Il consulente non ha ANALISI_UVE: la sezione non è sua, anche se l'azienda 425 è modificabile.
        var consulente = Client(api, PermessiFinti.Consulente);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.GetAsync("api/portale/analisi-uve")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PostAsJsonAsync("api/portale/analisi-uve", Analisi())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await consulente.PutAsJsonAsync($"api/portale/analisi-uve/{ticks}", Analisi())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync($"api/portale/analisi-uve/{ticks}")).StatusCode);
        Assert.Single(db.LogOp);
    }

    [Fact]
    public void Rilievi_ConversioneDeiCampi()
    {
        var (valori, errori) = RilieviNutrizione.Converti(RilieviNutrizione.Fallanze,
            new Dictionary<string, string?> { ["PercFallanze"] = " 2.25 ", ["Sconosciuta; DROP"] = "1" });
        Assert.Empty(errori);
        Assert.Equal(2.25m, valori["PercFallanze"]);
        Assert.Null(valori["NrOrganiControllati"]);
        Assert.Equal(RilieviNutrizione.Fallanze.Campi.Select(c => c.Colonna), valori.Keys);
        Assert.Matches(RilieviNutrizione.RegexSezioni, "analisi-uve");
        Assert.All(RilieviNutrizione.Tutte, d => Assert.Matches(RilieviNutrizione.RegexSezioni, d.Sezione));

        var nuovo = PortaleNutrizioneController.NuovoRilievo(RilieviNutrizione.AnalisiUve, 5,
            new Infrastructure.Repositories.VignetoRilievo(4250, 42, 425, null, null, null, 3), new DateTime(2026, 9, 1, 10, 0, 0),
            new DateTime(2026, 9, 1));
        Assert.Equal(0, nuovo["IdVitignoDaApp"]);
        Assert.Equal(new DateTime(2026, 9, 1), nuovo["InizioRilievo"]);
        Assert.Equal(new DateTime(2026, 9, 1, 10, 0, 0), nuovo["IdRilievo"]);
    }
}
