using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.AppRilievi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;
using Persona = ProgettoMappe.Api.Tests.AppIrroratriciTests.Persona;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Rilievi delle app di Jan (task #265): l'Api vera, persona e 4Grapes finti. La persona è quella di
/// <see cref="AppIrroratriciTests"/> (425 con aggiornamenti abilitati, 426 in sola lettura, 500 non sua); vigneti finti di
/// <see cref="LettureNutrizioneFinte"/>: 4250 di 425, 4260 di 426, 5000 di 500.
/// </summary>
public class AppRilieviTests
{
    private const string Segreto = AppIrroratriciTests.Segreto;
    private const int IdUtente = AppIrroratriciTests.IdUtente;

    /// <summary>Rilievi letti dalle tabelle finte come la query vera: tabella del tipo + Rilievo.</summary>
    private sealed class LettureRilieviFinte(DbFinto db) : ILettureRilieviApp
    {
        private IEnumerable<RilievoApp> Tutti(TipoRilievoApp tipo) =>
            from t in db.Tabella(tipo.Tabella.Nome).Values
            let r = db.Tabella("Rilievo")[t["IdRilievo"]!]
            where (int?)r["TipoRilievo_idTipoRilievo"] == tipo.Tipo
            orderby (DateTime)r["InizioRilievo"]! descending
            select new RilievoApp((DateTime)t["IdRilievo"]!, r["IdAziendaDaApp"] as int?, r["Vigneto_IdVigneto"] as int?, (int)r["IdUtente"]!,
                (DateTime)r["InizioRilievo"]!, r.GetValueOrDefault("NoteRilievo") as string,
                t.Where(c => c.Key != "IdRilievo").ToDictionary(c => c.Key, c => c.Value));

        public Task<IReadOnlyList<RilievoApp>> RilieviAsync(TipoRilievoApp tipo, int idEnte, int? idVigneto, DateOnly? da, DateOnly? a,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RilievoApp>>(Tutti(tipo)
                .Where(r => r.IdEnte == idEnte && (idVigneto is null || r.IdVigneto == idVigneto)
                            && (da is null || DateOnly.FromDateTime(r.InizioRilievo) >= da)
                            && (a is null || DateOnly.FromDateTime(r.InizioRilievo) <= a))
                .ToList());

        public Task<RilievoApp?> PerCodiceAsync(TipoRilievoApp tipo, string codice, CancellationToken ct = default) =>
            Task.FromResult(Tutti(tipo).FirstOrDefault(r => Equals(r.Valori[tipo.ColonnaCodice], codice)));
    }

    private static (WebApplicationFactory<AccessoUnicoOptions> Api, Persona Persona, DbFinto Db) Avvia()
    {
        var persona = new Persona();
        persona.Abilitazioni.Clear();
        persona.Abilitazioni.UnionWith(["4SPRAYERS", "POTATURA"]);
        var db = new DbFinto();
        var letture = new LettureDifesaFinte(db);
        var api = new WebApplicationFactory<AccessoUnicoOptions>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ChiaveApi:Valore", "chiave-del-web");
            b.UseSetting("AccessoUnico:ChiavePrivata", "");
            b.UseSetting("AccessoUnico:App:4sprayers:SegretoSha256", AppIrroratriciTests.Impronta(Segreto));
            b.UseSetting("AccessoUnico:App:4sprayers:Abilitazione", "4SPRAYERS");
            b.UseSetting("AccessoUnico:App:potatura:SegretoSha256", AppIrroratriciTests.Impronta(Segreto));
            b.UseSetting("AccessoUnico:App:potatura:Abilitazione", "POTATURA");
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IUtentiRepository>(persona);
                s.AddSingleton<IPermessiRepository>(persona);
                s.AddSingleton<IAccessoUnicoRepository>(persona);
                s.AddSingleton<ILettureDifesaPortale>(letture);
                s.AddSingleton<ILetturePortale>(letture);
                s.AddSingleton<ILettureNutrizionePortale>(new LettureNutrizioneFinte(db));
                s.AddSingleton<ILettureRilieviApp>(new LettureRilieviFinte(db));
                s.AddSingleton<ScritturaFourGrapes>(new ScritturaFinta(db));
            });
        });
        return (api, persona, db);
    }

    private static HttpRequestMessage Richiesta(HttpMethod metodo, string url, string? token, object? corpo = null, string? app = "4sprayers")
    {
        var r = new HttpRequestMessage(metodo, url);
        if (app is not null)
        {
            r.Headers.Add(AppChiamante.IntestazioneNome, app);
            r.Headers.Add(AppChiamante.IntestazioneSegreto, Segreto);
        }

        if (token is not null) r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null) r.Content = JsonContent.Create(corpo);
        return r;
    }

    private static async Task<(HttpClient Client, string Token)> EntraAsync(WebApplicationFactory<AccessoUnicoOptions> api, string app = "4sprayers")
    {
        var client = api.CreateClient();
        var risposta = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/accesso/token", null,
            new RichiestaTokenDto(AppIrroratriciTests.Email, AppIrroratriciTests.Password), app));
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        return (client, (await risposta.Content.ReadFromJsonAsync<TokenAccessoDto>())!.AccessToken);
    }

    private static async Task<string?> TitoloAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ProblemDetails>())?.Title;

    /// <summary>Tre giorni fa alle 9:30, senza fuso (ora italiana).</summary>
    private static readonly DateTime Giorno = DateTime.SpecifyKind(DateTime.Today.AddDays(-3).AddHours(9.5), DateTimeKind.Unspecified);

    private static ProvaIrroratriceAppDto Prova(string codice = "RS-0001", int azienda = 425, int vigneto = 4250, decimal copertura = 41.237m,
        string? classe = "ottimale") =>
        new(codice, azienda, vigneto, Giorno, " Prova con vento debole ", "Semplice", 3, 65, 6.5m, 9m, 300m, 12, 1.2m, copertura, classe,
            38m, 44m, 52m, 30m, 8, true);

    private static RiassuntoPotaturaAppDto Potatura(string? tipo = "grappoli") =>
        new("6f1c2a9e-0000-4000-8000-000000000001", 425, 4250, Giorno, null, tipo, "Capo", 20, 12.5m, 11m, 88m, 1.234m, 1.402m, 15.4m,
            2.31m, 150, 42.5m, 18m);

    [Fact]
    public async Task Prova_creata_aggiornata_per_codice_e_cancellata_con_EliminaRilievoCompleto()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);

        var nuova = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova()));
        Assert.Equal(HttpStatusCode.OK, nuova.StatusCode);
        var esito = (await nuova.Content.ReadFromJsonAsync<EsitoRilievoAppDto>())!;
        Assert.Equal(("RS-0001", "creato", false), (esito.Codice, esito.Esito, esito.Prova));
        var rilievo = Assert.Single(db.Tabella("Rilievo")).Value;
        Assert.Equal(169, rilievo["TipoRilievo_idTipoRilievo"]);
        Assert.Equal((IdUtente, 425, 4250, Giorno, "Prova con vento debole"),
            (rilievo["IdUtente"], rilievo["IdAziendaDaApp"], rilievo["Vigneto_IdVigneto"], rilievo["InizioRilievo"], rilievo["NoteRilievo"]));
        var prova = Assert.Single(db.Tabella("RilievoProvaIrroratrice")).Value;
        Assert.Equal(esito.IdRilievo, prova["IdRilievo"]);
        Assert.Equal("OTTIMALE", prova["ClasseCopertura"]);
        Assert.Equal(41.24m, prova["CoperturaMediaPerc"]); // decimal(5,2)
        Assert.Equal(true, prova["Definitiva"]);
        Assert.Equal([("nuovo", "Rilievo"), ("nuovo", "RilievoProvaIrroratrice")], db.LogOp.Select(l => (l.Operazione, l.Tabella)));

        // Stesso codice: si aggiorna lo stesso rilievo, l'autore resta.
        var aggiornata = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(copertura: 55m)));
        Assert.Equal("aggiornato", (await aggiornata.Content.ReadFromJsonAsync<EsitoRilievoAppDto>())!.Esito);
        Assert.Single(db.Tabella("Rilievo"));
        Assert.Equal(55m, Assert.Single(db.Tabella("RilievoProvaIrroratrice")).Value["CoperturaMediaPerc"]);
        Assert.Equal(4, db.LogOp.Count);

        // Rimandata uguale: niente di scritto.
        var uguale = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(copertura: 55m)));
        Assert.Equal("invariato", (await uguale.Content.ReadFromJsonAsync<EsitoRilievoAppDto>())!.Esito);
        Assert.Equal(4, db.LogOp.Count);

        var lista = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice?azienda=425&vigneto=4250", token));
        var riga = Assert.Single((await lista.Content.ReadFromJsonAsync<List<RilievoAppDto<ProvaIrroratriceAppDto>>>())!);
        Assert.True(riga.Modificabile);
        Assert.Equal((IdUtente, "RS-0001", 425, 55m, "OTTIMALE"),
            (riga.IdUtente, riga.Dati.CodiceProva, riga.Dati.IdAzienda, riga.Dati.CoperturaMediaPerc, riga.Dati.ClasseCopertura));
        var altroPeriodo = await client.SendAsync(Richiesta(HttpMethod.Get,
            $"/api/app/prove-irroratrice?azienda=425&da={DateTime.Today.AddDays(-1):yyyy-MM-dd}", token));
        Assert.Empty((await altroPeriodo.Content.ReadFromJsonAsync<List<RilievoAppDto<ProvaIrroratriceAppDto>>>())!);
        var una = await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice/RS-0001", token));
        Assert.Equal(Giorno, (await una.Content.ReadFromJsonAsync<RilievoAppDto<ProvaIrroratriceAppDto>>())!.Dati.Data);

        var via = await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/prove-irroratrice/RS-0001", token));
        Assert.Equal("cancellato", (await via.Content.ReadFromJsonAsync<EsitoRilievoAppDto>())!.Esito);
        Assert.Empty(db.Tabella("Rilievo"));
        Assert.Empty(db.Tabella("RilievoProvaIrroratrice"));
        // Il LogOp della cancellazione lo scrive la procedura, con l'utente della persona.
        Assert.Equal(4, db.LogOp.Count);
        Assert.Equal(IdUtente, Assert.Single(db.Tabella("LogOpProcedura")).Value["IdUtente"]);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/prove-irroratrice/RS-0001", token))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice/RS-0001", token))).StatusCode);
    }

    [Fact]
    public async Task Riassunto_potatura_e_validazioni_dei_CHECK_in_italiano()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api, "potatura");

        var ok = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/riassunti-potatura", token, Potatura(), "potatura"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var riga = Assert.Single(db.Tabella("RilievoRiassuntoPotatura")).Value;
        Assert.Equal(("Grappoli", 2.31m, 1.234m), (riga["TipoConteggio"], riga["KgPerCeppo"], riga["FertilitaPerGemma"]));
        Assert.Equal(170, Assert.Single(db.Tabella("Rilievo")).Value["TipoRilievo_idTipoRilievo"]);

        var senzaTipo = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/riassunti-potatura", token, Potatura(tipo: null), "potatura"));
        Assert.Equal(HttpStatusCode.BadRequest, senzaTipo.StatusCode);
        Assert.Contains("Tipo di conteggio (tipoConteggio): campo obbligatorio.", await TitoloAsync(senzaTipo));
        var tipoSbagliato = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/riassunti-potatura", token, Potatura(tipo: "Foglie"), "potatura"));
        Assert.Contains("uno fra Infiorescenze, Grappoli", await TitoloAsync(tipoSbagliato));

        var (_, tokenSpray) = await EntraAsync(api);
        var classe = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", tokenSpray, Prova(classe: "BUONO")));
        Assert.Equal(HttpStatusCode.BadRequest, classe.StatusCode);
        Assert.Contains("Classe di copertura (classeCopertura): uno fra SCARSO, DISCRETO, OTTIMALE, ECCESSIVO.", await TitoloAsync(classe));
        var fuori = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", tokenSpray, Prova(copertura: 140m)));
        Assert.Contains("Copertura media (%) (coperturaMediaPerc): fra 0 e 100.", await TitoloAsync(fuori));
        var vuota = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", tokenSpray,
            new ProvaIrroratriceAppDto(" ", null, 4250, null)));
        var titolo = await TitoloAsync(vuota);
        Assert.Contains("Codice della prova (codiceProva): campo obbligatorio.", titolo);
        Assert.Contains("Azienda (idAzienda): campo obbligatorio.", titolo);
        Assert.Contains("Data (data): campo obbligatorio.", titolo);
        Assert.Empty(db.Tabella("RilievoProvaIrroratrice"));
    }

    [Fact]
    public async Task Vigneto_di_un_altra_azienda_o_inesistente_400()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);

        var altra = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(vigneto: 4260)));
        Assert.Equal(HttpStatusCode.BadRequest, altra.StatusCode);
        Assert.Equal("Il vigneto 4260 non è dell'azienda 425 in 4Grapes.", await TitoloAsync(altra));
        var manca = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(vigneto: 9999)));
        Assert.Equal("In 4Grapes non c'è il vigneto 9999.", await TitoloAsync(manca));
        Assert.Empty(db.Tabella("Rilievo"));
        Assert.Empty(db.LogOp);
    }

    [Fact]
    public async Task Permesso_negato_fuori_dalle_proprie_aziende_senza_aggiornamenti_o_senza_abilitazione()
    {
        var (api, persona, db) = Avvia();
        await using var _a = api;
        var (client, token) = await EntraAsync(api);

        // 426: abbinata ma senza aggiornamenti abilitati → si legge, non si scrive. 500: non è sua.
        var sola = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(azienda: 426, vigneto: 4260)));
        Assert.Equal(HttpStatusCode.Forbidden, sola.StatusCode);
        Assert.Contains("aggiornamenti abilitati", await TitoloAsync(sola));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice?azienda=426", token))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova(azienda: 500, vigneto: 5000)))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice?azienda=500", token))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice", token))).StatusCode);

        // Perleuve scrive anche su 500; il consulente poi non la vede né la cancella.
        persona.Perleuve = true;
        Assert.Equal(HttpStatusCode.OK,
            (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova("RS-500", 500, 5000)))).StatusCode);
        persona.Perleuve = false;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice/RS-500", token))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Delete, "/api/app/prove-irroratrice/RS-500", token))).StatusCode);
        // Lo stesso codice da un'azienda sua non «ruba» la prova di 500.
        var ruba = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova("RS-500")));
        Assert.Equal(HttpStatusCode.Forbidden, ruba.StatusCode);
        Assert.Equal(500, Assert.Single(db.Tabella("Rilievo")).Value["IdAziendaDaApp"]);

        // Senza l'abilitazione dell'app: niente. Con IRRORATRICI del portale: le prove sì, la potatura no.
        persona.Abilitazioni.Clear();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/prove-irroratrice?azienda=425", token))).StatusCode);
        persona.Abilitazioni.Add("IRRORATRICI");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", token, Prova()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/riassunti-potatura?azienda=425", token))).StatusCode);
    }

    [Fact]
    public async Task Token_mancante_401_e_app_browser_con_lo_scopo_portale_rilievi()
    {
        var (api, _, db) = Avvia();
        await using var _a = api;
        var client = api.CreateClient();

        var senza = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", null, Prova()));
        Assert.Equal(HttpStatusCode.Unauthorized, senza.StatusCode);
        Assert.Contains("Token mancante", await TitoloAsync(senza));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Richiesta(HttpMethod.Get, "/api/app/riassunti-potatura?azienda=425", null, app: null))).StatusCode);

        var chiavi = api.Services.GetRequiredService<ChiaviFirma>();
        string Token(string scopi) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "http://localhost/",
            Audience = "4sprayers",
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = IdUtente.ToString(), ["scope"] = scopi },
            SigningCredentials = new SigningCredentials(chiavi.Chiave, SecurityAlgorithms.RsaSha256),
        });

        // Lo scopo delle irroratrici non basta per i rilievi.
        var irroratrici = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", Token("openid portale.irroratrici"), Prova(), app: null));
        Assert.Equal(HttpStatusCode.Forbidden, irroratrici.StatusCode);
        Assert.Contains("portale.rilievi", await TitoloAsync(irroratrici));
        var ok = await client.SendAsync(Richiesta(HttpMethod.Post, "/api/app/prove-irroratrice", Token("openid portale.rilievi"), Prova(), app: null));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Single(db.Tabella("RilievoProvaIrroratrice"));
    }

    [Fact]
    public void Ogni_campo_dei_DTO_e_una_colonna_della_tabella_del_dba()
    {
        Assert.Empty(RilieviApp.ProveIrroratrice.ProprietaSenzaColonna());
        Assert.Empty(RilieviApp.RiassuntiPotatura.ProprietaSenzaColonna());
        Assert.Equal(999.99m, CampoRilievoApp.MassimoDecimale(5, 2));
        Assert.Equal(999_999.9m, CampoRilievoApp.MassimoDecimale(7, 1));
        Assert.Equal(999.999m, CampoRilievoApp.MassimoDecimale(6, 3));
        Assert.True(Controllers.AccessoUnicoController.DellApp("/api/app/prove-irroratrice/RS-1"));
    }
}
