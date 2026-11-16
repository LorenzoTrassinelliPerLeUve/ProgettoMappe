using System.Net;
using System.Net.Http.Json;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Components.Portale.Sezioni;
using ProgettoMappe.Web.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Menù «Decision 4Grapes Nutrizione» (task #293): le voci del sotto-portale Nutrizione del vecchio portale nello stesso ordine,
/// la Dashboard nutrizione (sezione 2) e la lista Vigneti (sezione 4), sull'Api vera con 4Grapes finto.
/// </summary>
public class PortaleMenuNutrizioneTests
{
    private static async Task<PortaleUtenteDto> UtenteAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<PortaleUtenteDto>("api/portale/utente"))!;

    [Fact]
    public void Menu_StessoOrdineDelPortale_EVociCollegateASezioniVere()
    {
        // PortalNutrizione.cshtml, righe 78-122: Dashboard (spenta nel portale, voluta dal proprietario), le quattro pagine delle
        // concimazioni, obiettivo produttivo, asportazioni, il sotto-menù Rilievi, Vigneti.
        Assert.Equal(
            ["Dashboard", "Target Enologico", "Fattori Concimazioni", "Unità Concimazioni", "Resoconto concimazioni",
                "Obiettivo produttivo", "Asportazioni", "Analisi dei terreni", "Analisi uve", "Fallanze", "Dati produzione", "Vigneti"],
            MenuNutrizione.Voci.Select(v => v.Titolo));
        Assert.All(MenuNutrizione.Voci, v => Assert.NotNull(SezioniPortale.Trova(v.Sezione)));
        Assert.Equal(MenuNutrizione.Voci.Count, MenuNutrizione.Voci.Select(v => v.Chiave).Distinct().Count());
        // Le viste delle concimazioni sono i nomi delle pagine della sezione.
        Assert.All(MenuNutrizione.Voci.Where(v => v.Sezione == "concimazioni"),
            v => Assert.True(Enum.TryParse<SezioneConcimazioni.Pagina>(v.Vista, out _)));
    }

    [Fact]
    public void Menu_RegoleComeIlLayoutDelPortale()
    {
        Assert.Equal("DASHBOARD", SezioniPortale.Trova("dashboard-nutrizione")!.Regola.Abilitazione);
        Assert.Equal("VIGNETI", SezioniPortale.Trova("vigneti")!.Regola.Abilitazione);
        Assert.Equal("PRODUZIONE", SezioniPortale.Trova("produzione")!.Regola.Abilitazione);
        Assert.Equal("CONCIMAZIONI", SezioniPortale.Trova("concimazioni")!.Regola.Abilitazione);
    }

    [Fact]
    public async Task Menu_SoloLeVociCheLUtentePuoAprire_ABlocchiComeIlPortale()
    {
        var (api, _) = PortaleDifesaTests.Avvia();
        await using var _a = api;

        var master = MenuNutrizione.Visibili(await UtenteAsync(PortaleDifesaTests.Client(api, PermessiFinti.Master)));
        Assert.Equal(MenuNutrizione.Voci, master);
        var blocchi = MenuNutrizione.Blocchi(master);
        Assert.Equal([null, MenuNutrizione.Rilievi, null], blocchi.Select(b => b.Gruppo));
        Assert.Equal(["analisi-dei-terreni", "analisi-uve", "fallanze", "dati-produzione"], blocchi[1].Voci.Select(v => v.Chiave));
        Assert.Equal("vigneti", Assert.Single(blocchi[2].Voci).Chiave);

        // Il consulente non ha DASHBOARD né ANALISI_UVE.
        var consulente = MenuNutrizione.Visibili(await UtenteAsync(PortaleDifesaTests.Client(api, PermessiFinti.Consulente)));
        Assert.DoesNotContain(consulente, v => v.Chiave is "dashboard" or "analisi-uve");
        Assert.Contains(consulente, v => v.Chiave == "vigneti");

        Assert.Empty(MenuNutrizione.Visibili(await UtenteAsync(PortaleDifesaTests.Client(api, PermessiFinti.SenzaPermessi))));
    }

    [Fact]
    public async Task Dashboard_QuattroNumeri_CollezioneNullSenzaPermesso()
    {
        var (api, _) = PortaleDifesaTests.Avvia();
        await using var _a = api;

        var dati = await PortaleDifesaTests.Client(api, PermessiFinti.Master)
            .GetFromJsonAsync<DashboardNutrizioneDto>("api/portale/dashboard-nutrizione");
        Assert.Equal(new DashboardNutrizioneDto(5432, 20885, 1190667, null), dati);
        Assert.Equal(["UTENTI", "VIGNETI", "RILIEVI", "COLLEZIONI"], SezioneDashboardNutrizione.Riquadri(dati!).Select(r => r.Titolo));

        Assert.Equal(HttpStatusCode.Forbidden, (await PortaleDifesaTests.Client(api, PermessiFinti.Consulente)
            .GetAsync("api/portale/dashboard-nutrizione")).StatusCode);
    }

    [Fact]
    public async Task Vigneti_MasterTutti_AltriSoloAbbinate()
    {
        var (api, _) = PortaleDifesaTests.Avvia();
        await using var _a = api;
        var master = PortaleDifesaTests.Client(api, PermessiFinti.Master);
        var consulente = PortaleDifesaTests.Client(api, PermessiFinti.Consulente);

        Assert.Equal(3, (await master.GetFromJsonAsync<List<AziendaPortaleDto>>("api/portale/vigneti/aziende"))!.Count);
        Assert.Equal([425, 426], (await consulente.GetFromJsonAsync<List<AziendaPortaleDto>>("api/portale/vigneti/aziende"))!.Select(a => a.Id));

        var tutti = await master.GetFromJsonAsync<List<VignetoPortaleDto>>("api/portale/vigneti");
        Assert.Equal(6, tutti!.Count);
        var azienda500 = await master.GetFromJsonAsync<List<VignetoPortaleDto>>("api/portale/vigneti?azienda=500");
        Assert.All(azienda500!, v => Assert.Equal(500, v.IdEnte));

        var suoi = await consulente.GetFromJsonAsync<List<VignetoPortaleDto>>("api/portale/vigneti");
        Assert.All(suoi!, v => Assert.Contains(v.IdEnte, new[] { 425, 426 }));
        var vietata = await consulente.GetAsync("api/portale/vigneti?azienda=500");
        Assert.Equal(HttpStatusCode.Forbidden, vietata.StatusCode);
        Assert.Equal("Quest'azienda non è abbinata al tuo utente.", await PortaleDifesaTests.TitoloAsync(vietata));

        Assert.Equal(HttpStatusCode.Forbidden, (await PortaleDifesaTests.Client(api, PermessiFinti.SenzaPermessi)
            .GetAsync("api/portale/vigneti")).StatusCode);
    }

    [Fact]
    public async Task ExportVigneti_FiltriDelleAziendePermesse_AnchePerUnAziendaScelta()
    {
        var (api, _) = PortaleDifesaTests.Avvia();
        await using var _a = api;
        var letture = (LettureDifesaFinte)api.Services.GetService(typeof(ProgettoMappe.Infrastructure.Repositories.ILetturePortale))!;

        var master = await PortaleDifesaTests.Client(api, PermessiFinti.Master)
            .GetFromJsonAsync<EsportazioneRilieviDto>("api/portale/vigneti/esporta?azienda=500");
        Assert.Equal("Azienda C", master!.Azienda);
        Assert.Equal(("IT", (IReadOnlyList<int>?)null, (int?)500), letture.UltimoExportVigneti);

        await PortaleDifesaTests.Client(api, PermessiFinti.Consulente).GetFromJsonAsync<EsportazioneRilieviDto>("api/portale/vigneti/esporta");
        Assert.Equal([425, 426], letture.UltimoExportVigneti!.Value.SoloEnti!);
        Assert.Null(letture.UltimoExportVigneti!.Value.IdAzienda);

        // Nel portale l'azienda scelta faceva perdere il filtro delle abbinate: qui no.
        Assert.Equal(HttpStatusCode.Forbidden, (await PortaleDifesaTests.Client(api, PermessiFinti.Consulente)
            .GetAsync("api/portale/vigneti/esporta?azienda=500")).StatusCode);

        var file = ExcelRilievi.CreaVigneti(master.Tabella);
        using var libro = new ClosedXML.Excel.XLWorkbook(new MemoryStream(file));
        Assert.Equal("Vigneti", libro.Worksheets.Single().Name);
        Assert.Equal("Export Vigneti.xlsx", ExcelRilievi.NomeFileVigneti);
    }
}
