using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Amministrazione del Portale (tappa 5a, task #284): 23 Utenti e 26 Impostazioni sull'Api vera con 4Grapes finto
/// (<see cref="DbFinto"/>): inserimento, modifica, cancellazione, controlli del portale, permessi, LogOp senza password.
/// </summary>
public class PortaleAmministrazioneTests
{
    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db) AvviaAmministrazione()
    {
        var db = new DbFinto();
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiFinti>();
                s.AddSingleton<ILettureAmministrazionePortale>(new LettureAmministrazioneFinte(db));
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
            }));
        return (api, db);
    }

    private static UtenteSalvaDto Mario(string? password = "segreta123") =>
        new("mario.rossi@esempio.it", password, "Mario", "Rossi", "IT", "3331234567", true, false, false, false);

    [Fact]
    public async Task Utenti_InserisciModificaElimina_ConLogOpSenzaPassword()
    {
        var (api, db) = AvviaAmministrazione();
        await using var _ = api;
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var nuovo = await client.PostAsJsonAsync("api/portale/utenti", Mario() with { IsMasterUser = true, Attivo = true });
        Assert.Equal(HttpStatusCode.OK, nuovo.StatusCode);
        var riga = db.Tabella("Utente")[1];
        Assert.Equal("mario.rossi@esempio.it", riga["LoginName"]);
        Assert.Equal(PortaleAmministrazioneController.HashPassword("segreta123"), riga["ShaPassword"]);
        Assert.Equal(64, ((string)riga["ShaPassword"]!).Length);
        Assert.Equal(true, riga["IsMasterUser"]);
        Assert.Equal(true, riga["IsAppEnabled"]);
        Assert.Equal(false, riga["IsSysAdmin"]);
        Assert.Equal((byte)1, riga["StatoEntita"]);
        Assert.IsType<DateTime>(riga["DataCreazione"]);

        // Modifica senza password: quella di prima resta; disattivato.
        var modifica = await client.PutAsJsonAsync("api/portale/utenti/1", Mario(password: null) with { Nome = "Marco", Attivo = false });
        Assert.Equal(HttpStatusCode.OK, modifica.StatusCode);
        Assert.Equal("Marco", db.Tabella("Utente")[1]["Nome"]);
        Assert.Equal(PortaleAmministrazioneController.HashPassword("segreta123"), db.Tabella("Utente")[1]["ShaPassword"]);
        Assert.Equal((byte)0, db.Tabella("Utente")[1]["StatoEntita"]);

        // Nuova password (il «reset» del portale: MODIFICA PASSWORD).
        await client.PutAsJsonAsync("api/portale/utenti/1", Mario(password: "nuovapassword"));
        Assert.Equal(PortaleAmministrazioneController.HashPassword("nuovapassword"), db.Tabella("Utente")[1]["ShaPassword"]);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("api/portale/utenti/1")).StatusCode);
        Assert.Empty(db.Tabella("Utente"));
        Assert.Equal(["nuovo", "modificato", "modificato", "cancellato"], db.LogOp.Select(l => l.Operazione));
        Assert.All(db.LogOp, l =>
        {
            Assert.Equal("Utente", l.Tabella);
            Assert.DoesNotContain("ShaPassword", l.Json);
            Assert.DoesNotContain(PortaleAmministrazioneController.HashPassword("segreta123"), l.Json);
            Assert.Contains("mario.rossi@esempio.it", l.Json);
        });
    }

    [Fact]
    public async Task Utenti_ControlliDelPortale()
    {
        var (api, db) = AvviaAmministrazione();
        await using var _ = api;
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var senzaPassword = await client.PostAsJsonAsync("api/portale/utenti", Mario(password: ""));
        Assert.Equal(HttpStatusCode.BadRequest, senzaPassword.StatusCode);
        Assert.Equal(["Password mancante"], (await ErroriAsync(senzaPassword))["Password"]);

        var vuoto = await client.PostAsJsonAsync("api/portale/utenti",
            new UtenteSalvaDto("non-una-mail", "abc", "", null, "XX", "", false, false, false, false));
        var errori = await ErroriAsync(vuoto);
        Assert.Equal(["Inserisci un indirizzo email valido."], errori["LoginName"]);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Nome"]);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Cognome"]);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["Cellulare"]);
        Assert.Equal(["Scegli una lingua dell'elenco."], errori["Lingua"]);
        Assert.Equal(["La password deve avere almeno 8 caratteri."], errori["Password"]);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/portale/utenti", Mario())).StatusCode);
        var doppio = await client.PostAsJsonAsync("api/portale/utenti", Mario() with { Nome = "Altro" });
        Assert.Equal(HttpStatusCode.BadRequest, doppio.StatusCode);
        Assert.Equal(["Email già esistente"], (await ErroriAsync(doppio))["LoginName"]);
        Assert.Single(db.Tabella("Utente"));

        // La stessa email sulla riga stessa va bene (il portale esclude l'utente che si modifica).
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/portale/utenti/1", Mario(password: null))).StatusCode);

        // Sé stessi: né eliminare né disattivare.
        db.Metti("Utente", PermessiFinti.AmministratoreCompleto, new() { ["idUtente"] = PermessiFinti.AmministratoreCompleto });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.DeleteAsync($"api/portale/utenti/{PermessiFinti.AmministratoreCompleto}")).StatusCode);
        var disattivaMe = await client.PutAsJsonAsync($"api/portale/utenti/{PermessiFinti.AmministratoreCompleto}",
            Mario(password: null) with { LoginName = "io@esempio.it", Attivo = false });
        Assert.Equal(["Non puoi disattivare te stesso."], (await ErroriAsync(disattivaMe))["Attivo"]);

        // Un utente che non c'è: 409.
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync("api/portale/utenti/999")).StatusCode);
    }

    [Fact]
    public async Task Utenti_SenzaAbilitazioni_RuoliEStatoNonCambiano()
    {
        var (api, db) = AvviaAmministrazione();
        await using var _ = api;
        var client = Client(api, PermessiFinti.SoloUtenti);

        var lista = await client.GetFromJsonAsync<UtentiDto>("api/portale/utenti");
        Assert.False(lista!.PuoAbilitazioni);

        // Come il portale: senza ABILITAZIONI i ruoli di un utente nuovo restano spenti, anche se il modulo li manda.
        await client.PostAsJsonAsync("api/portale/utenti", Mario() with { IsMasterUser = true, IsAppEnabled = true, Attivo = false });
        Assert.Equal(false, db.Tabella("Utente")[1]["IsMasterUser"]);
        // Come il portale nasce disattivo; «Attivo» alla creazione vale anche senza ABILITAZIONI.
        Assert.Equal((byte)0, db.Tabella("Utente")[1]["StatoEntita"]);
        await client.PostAsJsonAsync("api/portale/utenti", Mario() with { LoginName = "anna@esempio.it", Attivo = true });
        Assert.Equal((byte)1, db.Tabella("Utente")[2]["StatoEntita"]);
        db.Tabella("Utente")[1]["StatoEntita"] = (byte)1;
        Assert.Equal(false, db.Tabella("Utente")[1]["IsAppEnabled"]);

        db.Tabella("Utente")[1]["IsAppEnabled"] = true;
        await client.PutAsJsonAsync("api/portale/utenti/1", Mario(password: null) with { IsAppEnabled = false, Attivo = false });
        Assert.Equal(true, db.Tabella("Utente")[1]["IsAppEnabled"]);
        Assert.Equal((byte)1, db.Tabella("Utente")[1]["StatoEntita"]);
    }

    [Fact]
    public async Task Amministrazione_PermessoNegato()
    {
        var (api, _) = AvviaAmministrazione();
        await using var __ = api;
        var client = Client(api, PermessiFinti.SenzaPermessi);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/utenti")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/portale/utenti", Mario())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("api/portale/utenti/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/impostazioni")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("api/portale/impostazioni", new ImpostazioniSalvaDto(1.2m, 0.5m, "OLD"))).StatusCode);

        // Solo UTENTI: le impostazioni no.
        var soloUtenti = Client(api, PermessiFinti.SoloUtenti);
        Assert.Equal(HttpStatusCode.Forbidden, (await soloUtenti.GetAsync("api/portale/impostazioni")).StatusCode);
    }

    [Fact]
    public async Task Impostazioni_Salva_SoloITreCampi_ConLogOp()
    {
        var (api, db) = AvviaAmministrazione();
        await using var _ = api;
        db.Metti("Impostazioni", 0, new()
        {
            ["Id"] = 0, ["CoefficenteCanopy"] = "OLD", ["LarghezzaMaxRifChioma"] = 0.5m, ["AltezzaMaxRifChioma"] = 1.2m,
            ["ElaborazioneModelliProtezioneInCorso"] = false,
        });
        var client = Client(api, PermessiFinti.AmministratoreCompleto);

        var letto = await client.GetFromJsonAsync<ImpostazioniDto>("api/portale/impostazioni");
        Assert.Equal("OLD", letto!.CoefficenteCanopy);
        Assert.Equal(["OLD", "LWA", "TRV"], letto.Coefficienti);

        var salva = await client.PutAsJsonAsync("api/portale/impostazioni", new ImpostazioniSalvaDto(1.3m, 0.6m, "TRV"));
        Assert.Equal(HttpStatusCode.OK, salva.StatusCode);
        var riga = db.Tabella("Impostazioni")[0];
        Assert.Equal(1.3m, riga["AltezzaMaxRifChioma"]);
        Assert.Equal(0.6m, riga["LarghezzaMaxRifChioma"]);
        Assert.Equal("TRV", riga["CoefficenteCanopy"]);
        Assert.Equal(false, riga["ElaborazioneModelliProtezioneInCorso"]);
        var log = Assert.Single(db.LogOp);
        Assert.Equal(("modificato", "Impostazioni", 0), (log.Operazione, log.Tabella, log.IdRiga));

        var errata = await client.PutAsJsonAsync("api/portale/impostazioni", new ImpostazioniSalvaDto(null, 10000m, "XYZ"));
        var errori = await ErroriAsync(errata);
        Assert.Equal([PortaleAmministrazioneController.Obbligatorio], errori["AltezzaMaxRifChioma"]);
        Assert.Contains("9999,9999", errori["LarghezzaMaxRifChioma"][0]);
        Assert.Equal(["Scegli OLD, LWA o TRV."], errori["CoefficenteCanopy"]);
        Assert.Single(db.LogOp);
    }

    [Fact]
    public void FuoriDalLog_TogliePasswordECodici()
    {
        var riga = new List<KeyValuePair<string, object?>>
        {
            new("idUtente", 5), new("LoginName", "a@b.it"), new("ShaPassword", "ABC"), new("CodiceSicurezza", Guid.Empty),
            new("NuovaPassword", "x"), new("CodiceCambioPassword", "y"),
        };
        Assert.Equal(["idUtente", "LoginName"], TabelleScrivibili.Utente.PerLog(riga).Select(c => c.Key));
        Assert.Equal(6, TabelleScrivibili.Irroratrice.PerLog(riga).Count);
    }
}

/// <summary>Letture dell'amministrazione sul <see cref="DbFinto"/> (tabelle «Utente» e «Impostazioni»).</summary>
public sealed class LettureAmministrazioneFinte(DbFinto db) : ILettureAmministrazionePortale
{
    public Task<IReadOnlyList<UtenteListaDto>> UtentiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UtenteListaDto>>(db.Tabella("Utente").Values
            .Select(r => new UtenteListaDto((int)r["idUtente"]!, r.GetValueOrDefault("LoginName") as string,
                FourGrapesLettureAmministrazionePortale.NomeCompleto(r.GetValueOrDefault("Cognome") as string, r.GetValueOrDefault("Nome") as string),
                Equals(r.GetValueOrDefault("StatoEntita"), (byte)1)))
            .ToList());

    public Task<UtenteSchedaDto?> UtenteAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("Utente").TryGetValue(id, out var r)
            ? new UtenteSchedaDto(id, r.GetValueOrDefault("LoginName") as string, r.GetValueOrDefault("Nome") as string,
                r.GetValueOrDefault("Cognome") as string, r.GetValueOrDefault("Lingua") as string, r.GetValueOrDefault("Cellulare") as string,
                Equals(r.GetValueOrDefault("IsAppEnabled"), true), Equals(r.GetValueOrDefault("IsMasterUser"), true),
                Equals(r.GetValueOrDefault("IsUtentePerleuve"), true), Equals(r.GetValueOrDefault("IsGuest"), true),
                Equals(r.GetValueOrDefault("StatoEntita"), (byte)1), null, null)
            : null);

    public Task<ImpostazioniDto?> ImpostazioniAsync(CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("Impostazioni").TryGetValue(0, out var r)
            ? new ImpostazioniDto(r["AltezzaMaxRifChioma"] as decimal?, r["LarghezzaMaxRifChioma"] as decimal?, r["CoefficenteCanopy"] as string,
                FourGrapesLettureAmministrazionePortale.CoefficientiCanopy, null, null, null, null,
                Equals(r.GetValueOrDefault("ElaborazioneModelliProtezioneInCorso"), true))
            : null);

    /// <summary>Le aziende abbinate per utente (in 4Grapes da Utente_Ente).</summary>
    public Dictionary<int, List<AziendaPortaleDto>> Aziende { get; } = new();

    private static bool B(Dictionary<string, object?> r, string c) => Equals(r.GetValueOrDefault(c), true);

    public Task<IReadOnlyList<AbilitazioneCategoriaDto>> AbilitazioniCategoriaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AbilitazioneCategoriaDto>>(db.Tabella("Abilitazione").Values
            .Select(r => new AbilitazioneCategoriaDto((string)r["Id"]!, B(r, "SysAdmin"), B(r, "MasterUser"), B(r, "UtentePerLeUve"),
                B(r, "DSSUser"), B(r, "SoilUser"), B(r, "AppEnabled")))
            .OrderBy(a => a.Id).ToList());

    public Task<IReadOnlyList<AbilitazionePersonaDto>> AbilitazioniPersonaAsync(int idUtente, DateOnly oggi, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AbilitazionePersonaDto>>(db.Tabella("AbilitazioneUtente").Values
            .Where(r => Equals(r["IdUtente"], idUtente))
            .Select(r =>
            {
                var attiva = Equals(r["StatoEntita"], (byte)1);
                var fine = r.GetValueOrDefault("DataFine") as DateOnly?;
                return new AbilitazionePersonaDto((int)r["Id"]!, (string)r["IdAbilitazione"]!, r["IdEnte"] as int?, null, attiva,
                    r.GetValueOrDefault("DataInizio") as DateOnly?, fine, r.GetValueOrDefault("Note") as string,
                    attiva && (fine is null || fine >= oggi));
            }).ToList());

    public Task<IReadOnlyList<AziendaPortaleDto>> AziendeUtenteAsync(int idUtente, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AziendaPortaleDto>>(Aziende.GetValueOrDefault(idUtente) ?? []);

    public Task<IReadOnlyList<GruppoUtentiDto>> GruppiUtentiAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<GruppoUtentiDto>>(db.Tabella("GruppoUtenti").Values
            .Select(r => new GruppoUtentiDto((int)r["Id"]!, r["Descrizione"] as string,
                db.Tabella("ComponenteGruppoUtenti").Values.Count(c => Equals(c["IdGruppo"], r["Id"]))))
            .ToList());

    public Task<GruppoUtentiSchedaDto?> GruppoUtentiAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("GruppoUtenti").TryGetValue(id, out var r)
            ? new GruppoUtentiSchedaDto(id, r["Descrizione"] as string, db.Tabella("ComponenteGruppoUtenti").Values
                .Where(c => Equals(c["IdGruppo"], id))
                .Select(c => new PartecipanteGruppoDto((int)c["IdUtente"]!, $"Utente {c["IdUtente"]}", null)).ToList())
            : null);

    public Task<IReadOnlyList<PartecipanteGruppoDto>> CercaUtentiAsync(string testo, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PartecipanteGruppoDto>>(db.Tabella("Utente").Values
            .Where(r => $"{r.GetValueOrDefault("Nome")} {r.GetValueOrDefault("Cognome")} ({r.GetValueOrDefault("LoginName")})".Contains(testo))
            .Select(r => new PartecipanteGruppoDto((int)r["idUtente"]!, "", r.GetValueOrDefault("LoginName") as string)).Take(50).ToList());
}
