using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Tappa 5b (task #284): 24 Abilitazioni per categoria e per persona/azienda, 25 Gruppi di utenti, sull'Api vera con 4Grapes
/// finto: scritture, LogOp, controlli, permessi.
/// </summary>
public class PortaleAbilitazioniGruppiTests
{
    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db, LettureAmministrazioneFinte Letture) Avvia()
    {
        var db = new DbFinto();
        var letture = new LettureAmministrazioneFinte(db);
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiFinti>();
                s.AddSingleton<ILettureAmministrazionePortale>(letture);
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
            }));
        db.Metti("Abilitazione", "TRATTAMENTI", new()
        {
            ["Id"] = "TRATTAMENTI", ["SysAdmin"] = true, ["MasterUser"] = true, ["UtentePerLeUve"] = true, ["DSSUser"] = true,
            ["AppEnabled"] = false, ["SoilUser"] = false,
        });
        db.Metti("Abilitazione", "SUITE_TRATTORISTA", new()
        {
            ["Id"] = "SUITE_TRATTORISTA", ["SysAdmin"] = true, ["MasterUser"] = false, ["UtentePerLeUve"] = false, ["DSSUser"] = false,
            ["AppEnabled"] = false, ["SoilUser"] = false,
        });
        db.Metti("Utente", 50, new() { ["idUtente"] = 50, ["Nome"] = "Luca", ["Cognome"] = "Bianchi", ["LoginName"] = "luca@esempio.it" });
        db.Metti("Utente", 51, new() { ["idUtente"] = 51, ["Nome"] = "Sara", ["Cognome"] = "Verdi", ["LoginName"] = "sara@esempio.it" });
        letture.Aziende[50] = [new AziendaPortaleDto(425, "Cantina Uno")];
        return (api, db, letture);
    }

    [Fact]
    public async Task AbilitazioniCategoria_ListaESalva_ConLogOp()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var lista = await client.GetFromJsonAsync<List<AbilitazioneCategoriaDto>>("api/portale/abilitazioni");
        Assert.Equal(["SUITE_TRATTORISTA", "TRATTAMENTI"], lista!.Select(a => a.Id));

        var salva = await client.PutAsJsonAsync("api/portale/abilitazioni/TRATTAMENTI",
            new AbilitazioneCategoriaSalvaDto(true, true, false, true, false, true));
        Assert.Equal(HttpStatusCode.OK, salva.StatusCode);
        var riga = db.Tabella("Abilitazione")["TRATTAMENTI"];
        Assert.Equal(false, riga["UtentePerLeUve"]);
        Assert.Equal(true, riga["AppEnabled"]);
        var log = Assert.Single(db.LogOp);
        Assert.Equal(("modificato", "Abilitazione", 0), (log.Operazione, log.Tabella, log.IdRiga));

        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsJsonAsync("api/portale/abilitazioni/NON_ESISTE", new AbilitazioneCategoriaSalvaDto(true, true, true, true, true, true))).StatusCode);
        // Solo UTENTI (senza ABILITAZIONI): niente.
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.SoloUtenti).GetAsync("api/portale/abilitazioni")).StatusCode);
    }

    [Fact]
    public async Task AbilitazioniPersona_DaiRiaccendiRevoca()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var scheda = await client.GetFromJsonAsync<AbilitazioniPersonaDto>("api/portale/abilitazioni/persona/50");
        Assert.Empty(scheda!.Righe);
        Assert.Contains("SUITE_TRATTORISTA", scheda.Codici);
        Assert.Equal(425, Assert.Single(scheda.Aziende).Id);

        var dai = await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50",
            new AbilitazionePersonaSalvaDto("suite_trattorista", 425, null, "per la vendemmia"));
        Assert.Equal(HttpStatusCode.OK, dai.StatusCode);
        var riga = db.Tabella("AbilitazioneUtente")[1];
        Assert.Equal(("SUITE_TRATTORISTA", 425, (byte)1, PermessiFinti.AmministratoreCompleto),
            ((string)riga["IdAbilitazione"]!, (int)riga["IdEnte"]!, (byte)riga["StatoEntita"]!, (int)riga["IdUtenteModifica"]!));

        // Di nuovo uguale: c'è già.
        var doppia = await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50", new AbilitazionePersonaSalvaDto("SUITE_TRATTORISTA", 425, null, null));
        Assert.Equal(["Questa abilitazione la persona ce l'ha già."], (await ErroriAsync(doppia))["Codice"]);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("api/portale/abilitazioni/persona/riga/1/revoca", null)).StatusCode);
        Assert.Equal((byte)0, db.Tabella("AbilitazioneUtente")[1]["StatoEntita"]);

        // Ridarla riaccende la stessa riga (indice unico IdUtente, IdAbilitazione, IdEnte).
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50",
            new AbilitazionePersonaSalvaDto("SUITE_TRATTORISTA", 425, null, null))).StatusCode);
        Assert.Single(db.Tabella("AbilitazioneUtente"));
        Assert.Equal((byte)1, db.Tabella("AbilitazioneUtente")[1]["StatoEntita"]);
        Assert.Equal(["nuovo", "modificato", "modificato"], db.LogOp.Select(l => l.Operazione));
        Assert.All(db.LogOp, l => Assert.Equal("AbilitazioneUtente", l.Tabella));

        // Controlli: codice sconosciuto, azienda non abbinata, scadenza passata, utente che non c'è.
        var errata = await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50", new AbilitazionePersonaSalvaDto("NON_ESISTE", null, null, null));
        Assert.Equal(["In 4Grapes non c'è l'abilitazione NON_ESISTE."], (await ErroriAsync(errata))["Codice"]);
        var azienda = await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50", new AbilitazionePersonaSalvaDto("TRATTAMENTI", 999, null, null));
        Assert.Equal(["L'utente non è abbinato a questa azienda."], (await ErroriAsync(azienda))["IdEnte"]);
        var passata = await client.PostAsJsonAsync("api/portale/abilitazioni/persona/50",
            new AbilitazionePersonaSalvaDto("TRATTAMENTI", null, new DateOnly(2020, 1, 1), null));
        Assert.Equal(["La scadenza è già passata."], (await ErroriAsync(passata))["DataFine"]);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("api/portale/abilitazioni/persona/777",
            new AbilitazionePersonaSalvaDto("TRATTAMENTI", null, null, null))).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.SoloUtenti)
            .PostAsJsonAsync("api/portale/abilitazioni/persona/50", new AbilitazionePersonaSalvaDto("TRATTAMENTI", null, null, null))).StatusCode);
    }

    [Fact]
    public async Task Gruppi_CreaPartecipantiElimina_ConLogOp()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var client = Client(api, PermessiFinti.Master);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/portale/gruppi-utenti", new GruppoUtentiSalvaDto("Tecnici FVG"))).StatusCode);
        var doppio = await client.PostAsJsonAsync("api/portale/gruppi-utenti", new GruppoUtentiSalvaDto("Tecnici FVG"));
        Assert.Equal(["Esiste già un gruppo utenti con questo nome"], (await ErroriAsync(doppio))["Descrizione"]);
        var vuoto = await client.PostAsJsonAsync("api/portale/gruppi-utenti", new GruppoUtentiSalvaDto(" "));
        Assert.Equal(["Campo obbligatorio"], (await ErroriAsync(vuoto))["Descrizione"]);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/gruppi-utenti/1", new GruppoUtentiSalvaDto("Tecnici"))).StatusCode);
        Assert.Equal("Tecnici", db.Tabella("GruppoUtenti")[1]["Descrizione"]);

        var trovati = await client.GetFromJsonAsync<List<PartecipanteGruppoDto>>("api/portale/gruppi-utenti/utenti?testo=Luca");
        Assert.Equal(50, Assert.Single(trovati!).IdUtente);
        Assert.Empty((await client.GetFromJsonAsync<List<PartecipanteGruppoDto>>("api/portale/gruppi-utenti/utenti?testo=L"))!);

        var aggiungi = await client.PostAsJsonAsync("api/portale/gruppi-utenti/1/partecipanti", new PartecipantiSalvaDto([50, 51, 50]));
        Assert.Equal("2", (await aggiungi.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Chiave);
        // Chi c'è già si salta.
        var ancora = await client.PostAsJsonAsync("api/portale/gruppi-utenti/1/partecipanti", new PartecipantiSalvaDto([50]));
        Assert.Equal("0", (await ancora.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Chiave);
        var inesistente = await client.PostAsJsonAsync("api/portale/gruppi-utenti/1/partecipanti", new PartecipantiSalvaDto([999]));
        Assert.Equal(HttpStatusCode.BadRequest, inesistente.StatusCode);

        var scheda = await client.GetFromJsonAsync<GruppoUtentiSchedaDto>("api/portale/gruppi-utenti/1");
        Assert.Equal(2, scheda!.Partecipanti.Count);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/gruppi-utenti/1/partecipanti/51")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync("api/portale/gruppi-utenti/1/partecipanti/51")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/gruppi-utenti/1")).StatusCode);
        Assert.Empty(db.Tabella("GruppoUtenti"));
        Assert.Empty(db.Tabella("ComponenteGruppoUtenti"));
        Assert.Equal(
            ["nuovo GruppoUtenti", "modificato GruppoUtenti", "nuovo ComponenteGruppoUtenti", "nuovo ComponenteGruppoUtenti",
                "cancellato ComponenteGruppoUtenti", "cancellato ComponenteGruppoUtenti", "cancellato GruppoUtenti"],
            db.LogOp.Select(l => $"{l.Operazione} {l.Tabella}"));

        // Solo MasterUser, come il portale.
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.Consulente).GetAsync("api/portale/gruppi-utenti")).StatusCode);
    }

    [Fact]
    public void Escape_DelLike() =>
        Assert.Equal(@"50\% \_a\[b\\", FourGrapesLettureAmministrazionePortale.Escape(@"50% _a[b\"));
}
