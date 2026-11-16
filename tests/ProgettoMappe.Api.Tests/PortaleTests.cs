using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using ProgettoMappe.Web.Portale;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Portale tappa 0 (task #225): permessi come il portale, LogOp, comandi di scrittura, lista comune.</summary>
public class PortaleTests
{
    private static PermessiUtente Utente(bool app = true, bool master = false, int? idApp = null, params string[] abilitazioni) =>
        new(10, app, false, master, false, false, idApp, abilitazioni.Select(a => a.ToUpperInvariant()).ToHashSet());

    // ── Sezioni e regole ──

    [Fact]
    public void Sezioni_SonoLe23SceltedalProprietario()
    {
        int[] scelte = [3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23, 24, 25, 26, 27];
        Assert.Equal(scelte, SezioniPortale.Tutte.Select(s => s.Numero).Order());
        Assert.Equal(23, SezioniPortale.Tutte.Select(s => s.Chiave).Distinct().Count());
        Assert.All(SezioniPortale.Tutte, s => Assert.Matches("^[a-z-]+$", s.Chiave));
        Assert.All(SezioniPortale.Tutte, s => Assert.Contains(s.Gruppo, SezioniPortale.Gruppi));
    }

    [Fact]
    public void Regola_AppNonAbilitata_NonEntraMai()
    {
        var utente = Utente(app: false, master: true, abilitazioni: "UTENTI");
        Assert.DoesNotContain(SezioniPortale.Tutte, s => s.Regola.Consente(utente));
    }

    [Fact]
    public void Regola_SoloLogin_BastaLAppAbilitata()
    {
        Assert.True(SezioniPortale.Trova("home")!.Regola.Consente(Utente()));
        Assert.False(SezioniPortale.Trova("utenti")!.Regola.Consente(Utente()));
    }

    [Fact]
    public void Regola_Abilitazione_IgnoraLeMaiuscoleComeIlPortale()
    {
        var regola = new RegolaAccesso(Abilitazione: "DECISION4GRAPES");
        Assert.True(regola.Consente(Utente(abilitazioni: "Decision4Grapes")));
    }

    [Fact]
    public void Regola_Irroratrici_VuoleMasterUserEAbilitazione()
    {
        var regola = SezioniPortale.Trova("irroratrici")!.Regola;
        Assert.False(regola.Consente(Utente(abilitazioni: "IRRORATRICI")));
        Assert.False(regola.Consente(Utente(master: true)));
        Assert.True(regola.Consente(Utente(master: true, abilitazioni: "IRRORATRICI")));
    }

    [Fact]
    public void Regola_Trattamenti_UnaDelleTreOppureAppSardegna()
    {
        var regola = SezioniPortale.Trova("trattamenti")!.Regola;
        Assert.True(regola.Consente(Utente(abilitazioni: "TRATTAMENTI_AUTOMATICI")));
        Assert.True(regola.Consente(Utente(idApp: SezioniPortale.AppSardegna)));
        Assert.False(regola.Consente(Utente(idApp: 1, abilitazioni: "BOLLETTINI_AZIENDALI")));
    }

    [Fact]
    public void Regola_AvversitaEGruppiUtenti_SoloMasterUser()
    {
        Assert.False(SezioniPortale.Trova("avversita")!.Regola.Consente(Utente(abilitazioni: "AVVERSITA_DA_TRATTARE")));
        Assert.True(SezioniPortale.Trova("avversita")!.Regola.Consente(Utente(master: true)));
        Assert.True(SezioniPortale.Trova("gruppi-utenti")!.Regola.Consente(Utente(master: true)));
    }

    [Fact]
    public void Componi_PermessiNonLetti_TutteLeVociSpente()
    {
        var dto = PortaleController.Componi(10, null, "manca il GRANT");
        Assert.False(dto.PermessiLetti);
        Assert.Equal(23, dto.Sezioni.Count);
        Assert.DoesNotContain(dto.Sezioni, s => s.Accessibile);
    }

    [Fact]
    public void Componi_SezioniNellOrdineDeiGruppi_ConLeSoleAccessibili()
    {
        var dto = PortaleController.Componi(10, Utente(abilitazioni: ["FALLANZE", "RILIEVI"]), null);
        Assert.Equal(["account", "fallanze", "home", "rilievi"], dto.Sezioni.Where(s => s.Accessibile).Select(s => s.Chiave).Order());
        Assert.Equal(SezioniPortale.Generale, dto.Sezioni[0].Gruppo);
        Assert.Equal(SezioniPortale.Amministrazione, dto.Sezioni[^1].Gruppo);
        Assert.Equal(["FALLANZE", "RILIEVI"], dto.Abilitazioni);
    }

    [Fact]
    public void PermessiRidotti_SoloLeSezioniDiChiHaLApp()
    {
        var ridotti = new PermessiUtente(10, true, false, false, false, false, null, PermessiUtente.AbilitazioniAppEnabled, Ridotti: true);
        var dto = PortaleController.Componi(10, ridotti, "ridotti");
        Assert.Equal(
            ["account", "analisi-terreni", "analisi-uve", "asportazioni", "fallanze", "galleria", "home", "produzione", "rilievi"],
            dto.Sezioni.Where(s => s.Accessibile).Select(s => s.Chiave).Order());
    }

    [Fact]
    public void SezioniPronte_TappaUnoEDue()
    {
        Assert.Equal(["analisi-terreni", "analisi-uve", "asportazioni", "avversita", "fallanze", "galleria", "home", "indicatori", "irroratrici", "irroratrici-azienda", "linee-difesa", "produzione", "rilievi", "soglie-trattamenti"],
            SezioniPortale.Tutte.Where(s => s.Pronta).Select(s => s.Chiave).Order());
        Assert.Equal(DateTime.Today.Year - 3, PortaleLettureController.AnniExport(DateTime.Today.Year)[^1]);
    }

    [Fact]
    public async Task Letture_SenzaPermesso403_ConPermessiNonLetti()
    {
        await using var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>();
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add(PortaleControllerBase.IntestazioneUtente, "12");
        // In memoria l'utente non esiste: nessuna sezione.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/asportazioni?azienda=1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/rilievi/esporta?azienda=1&anno=2026&tipo=miei")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("api/portale/trattamenti/aziende")).StatusCode);
    }

    [Fact]
    public void ExcelRilievi_FoglioDateENomi()
    {
        var export = new EsportazioneRilieviDto("Az. Agricola: Rossi/Bianchi [Collio] di nome lungo",
            new TabellaPortaleDto(["Vigneto", "_InizioRilievo", "Valore"], ["testo", "data", "numero"],
                [["Ronco", "2026-05-01T08:30:00", 12.5m], [null, null, null]]));
        var file = ExcelRilievi.Crea(export);
        using var libro = new ClosedXML.Excel.XLWorkbook(new MemoryStream(file));
        var foglio = libro.Worksheets.Single();
        Assert.True(foglio.Name.Length <= 31);
        Assert.StartsWith("Rilievi azienda Az. Agricola", foglio.Name);
        Assert.Equal(new DateTime(2026, 5, 1, 8, 30, 0), foglio.Cell(2, 2).GetDateTime());
        Assert.Equal("dd/MM/yyyy HH:mm:ss", foglio.Cell(2, 2).Style.DateFormat.Format);
        Assert.Equal(12.5, foglio.Cell(2, 3).GetDouble());
        Assert.True(foglio.Cell(3, 1).IsEmpty());
        Assert.Equal("Export Rilievi azienda Az. Agricola  Rossi Bianchi  Collio  di nome lungo.xlsx",
            ExcelRilievi.NomeFile(export.Azienda));
    }

    // ── LogOp ──

    [Fact]
    public void JsonLogOp_ComeNewtonsoft()
    {
        var json = JsonLogOp.Scrivi(
        [
            new("Id", 42),
            new("Nome", "Più \"vino\"\n\\ € "),
            new("Data", new DateTime(2026, 11, 11, 19, 3, 18, 495).AddTicks(2540)),
            new("Giorno", new DateTime(2026, 11, 11)),
            new("Kg", 12m),
            new("Ha", 1.50m),
            new("Media", 3d),
            new("Effettivo", true),
            new("Note", null),
            new("Ctrl", "a" + (char)1 + "b" + (char)0x2028),
        ]);
        Assert.Equal(
            "{\"Id\":42,\"Nome\":\"Più \\\"vino\\\"\\n\\\\ € \",\"Data\":\"2026-11-11T19:03:18.495254\"," +
            "\"Giorno\":\"2026-11-11T00:00:00\",\"Kg\":12.0,\"Ha\":1.50,\"Media\":3.0,\"Effettivo\":true,\"Note\":null," +
            "\"Ctrl\":\"a\\u0001b\\u2028\"}",
            json);
    }

    [Fact]
    public void RigaLogOp_IdRigaSoloDallaColonnaIdIntera()
    {
        var adesso = new DateTimeOffset(2026, 11, 14, 14, 0, 0, TimeSpan.Zero);
        Assert.Equal(7, RigaLogOp.Crea(1, adesso, RigaLogOp.Nuovo, "Trattamento", [new("Id", 7)]).IdRiga);
        // Vigneto (IdVigneto) e Rilievo (IdRilievo datetime2): il portale scrive 0 (dba, 2026-11-14).
        Assert.Equal(0, RigaLogOp.Crea(1, adesso, RigaLogOp.Modificato, "Vigneto", [new("IdVigneto", 100622)]).IdRiga);
        Assert.Equal(0, RigaLogOp.Crea(1, adesso, RigaLogOp.Nuovo, "Rilievo", [new("IdRilievo", DateTime.Now)]).IdRiga);
    }

    [Fact]
    public void RigaLogOp_DataInOraItaliana()
    {
        var inverno = RigaLogOp.Crea(1, new DateTimeOffset(2026, 11, 14, 14, 0, 0, TimeSpan.Zero), "nuovo", "T", []);
        var estate = RigaLogOp.Crea(1, new DateTimeOffset(2026, 7, 14, 14, 0, 0, TimeSpan.Zero), "nuovo", "T", []);
        Assert.Equal(new DateTime(2026, 11, 14, 15, 0, 0), inverno.Data);
        Assert.Equal(new DateTime(2026, 7, 14, 16, 0, 0), estate.Data);
        Assert.Equal(DateTimeKind.Unspecified, inverno.Data.Kind);
        Assert.Equal("{}", inverno.Json);
    }

    // ── Comandi di scrittura ──

    private static readonly TabellaScrivibile Trattamento = new("Trattamento", "Id", ChiaveIdentity: true);

    [Fact]
    public void Inserisci_ConIdentity_LeggeLaChiaveGenerata()
    {
        var comando = ComandiScrittura.Inserisci(Trattamento,
            new Dictionary<string, object?> { ["IdVigneto"] = 5, ["Data"] = new DateTime(2026, 5, 1, 8, 30, 0) });
        Assert.Equal("INSERT INTO dbo.[Trattamento] ([IdVigneto], [Data]) VALUES (@v0, @v1); SELECT CAST(SCOPE_IDENTITY() AS int);",
            comando.Testo);
        Assert.Equal(SqlDbType.DateTime2, comando.Parametri[1].SqlDbType);
    }

    [Fact]
    public void Aggiorna_ConValoriAttesi_ControllaCheNessunoLiAbbiaCambiati()
    {
        var comando = ComandiScrittura.Aggiorna(Trattamento, 9,
            new Dictionary<string, object?> { ["Note"] = "ok" },
            new Dictionary<string, object?> { ["Note"] = null, ["Effettivo"] = false });
        Assert.Equal(
            "UPDATE dbo.[Trattamento] SET [Note] = @v0 WHERE [Id] = @chiave AND [Note] IS NULL AND [Effettivo] = @a0; SELECT @@ROWCOUNT;",
            comando.Testo);
        Assert.Equal(3, comando.Parametri.Count);
    }

    [Fact]
    public void Comandi_RifiutanoNomiStraniEChiaveModificata()
    {
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Aggiorna(Trattamento, 1, new Dictionary<string, object?> { ["Id"] = 2 }));
        Assert.Throws<ArgumentException>(() =>
            ComandiScrittura.Inserisci(Trattamento, new Dictionary<string, object?> { ["Note]; DROP TABLE x;--"] = 1 }));
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Elimina(new TabellaScrivibile("Log Op", "Id", true), 1));
    }

    [Fact]
    public void InserisciLogOp_TuttiICampiDelPortale()
    {
        var riga = new RigaLogOp(3, new DateTime(2026, 11, 14, 15, 0, 0), "nuovo", "Trattamento", 7, "{}");
        var comando = ComandiScrittura.InserisciLogOp(riga);
        Assert.StartsWith("INSERT INTO dbo.LogOp (IdUtente, Data, Operazione, Tabella, IdRiga, Json)", comando.Testo);
        Assert.Equal(SqlDbType.DateTime2, comando.Parametri.Single(p => p.ParameterName == "@Data").SqlDbType);
    }

    [Fact]
    public async Task Scritture_SenzaStringa_SonoSpente()
    {
        var scrittura = new ScritturaFourGrapes("", ModoScritture.Vere, TimeProvider.System);
        Assert.Equal(ModoScritture.Spente, scrittura.Modo);
        await Assert.ThrowsAsync<ScrittureSpenteException>(() => scrittura.EseguiAsync<object?>(1, _ => Task.FromResult<object?>(null)));
    }

    [Theory]
    [InlineData("prova", ModoScritture.Prova)]
    [InlineData("VERE", ModoScritture.Vere)]
    [InlineData("", ModoScritture.Spente)]
    [InlineData("si", ModoScritture.Spente)]
    [InlineData("7", ModoScritture.Spente)]
    public void LeggiModo_AltroValore_Spente(string testo, ModoScritture atteso) =>
        Assert.Equal(atteso, ScritturaFourGrapes.LeggiModo(testo));

    [Fact]
    public void TabelleScrivibili_AccessoUnicoEDifesa() =>
        Assert.Equal(["Utente", "Utente_Ente", "AbilitazioneUtente", "Irroratrice", "Irroratrice_Ente", "LineaDifesa", "SogliaTrattamento", "Rilievo", "TargetProduttivo",
            "UnitaBaseConcimazione", "ConcimazioneReale"],
            TabelleScrivibili.Tutte.Select(t => t.Nome));

    // ── Api ──

    [Fact]
    public async Task Api_SenzaUtente_401_ConUtenteSenza4Grapes_VociSpente()
    {
        await using var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>();
        var client = api.CreateClient();

        var senza = await client.GetAsync("api/portale/utente");
        Assert.Equal(HttpStatusCode.Unauthorized, senza.StatusCode);
        var errore = await PortaleApiClient.ErroreAsync<PortaleUtenteDto>(senza, default);
        Assert.Equal("Accedi per usare il Portale.", errore.Errore);

        client.DefaultRequestHeaders.Add(PortaleControllerBase.IntestazioneUtente, "12");
        var dto = await client.GetFromJsonAsync<PortaleUtenteDto>("api/portale/utente");
        Assert.False(dto!.PermessiLetti);
        Assert.DoesNotContain(dto.Sezioni, s => s.Accessibile);

        var scritture = await client.GetFromJsonAsync<StatoScrittureDto>("api/portale/scritture");
        Assert.Equal("spente", scritture!.Modo);
    }

    // ── Web ──

    [Fact]
    public void IdUtente_DalClaim_CookieVecchioSenzaClaim_Null()
    {
        var nuovo = new ClaimsPrincipal(new ClaimsIdentity([new Claim(UtentePortale.ClaimIdUtente, "345")], "c"));
        var vecchio = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Mario")], "c"));
        Assert.Equal(345, UtentePortale.IdUtente(nuovo));
        Assert.Null(UtentePortale.IdUtente(vecchio));
        Assert.Null(UtentePortale.IdUtente(null));
    }

    private sealed record Riga(string Nome, decimal? Kg, DateTime Data);

    private static readonly IReadOnlyList<ColonnaLista<Riga>> Colonne =
        [new("Nome", r => r.Nome), new("Kg", r => r.Kg, Numerica: true), new("Data", r => r.Data)];

    private static readonly Riga[] Righe =
    [
        new("Città", 1200.5m, new DateTime(2026, 5, 1)),
        new("bosco", null, new DateTime(2026, 5, 3, 8, 15, 0)),
        new("Arsura", 3m, new DateTime(2026, 4, 1)),
    ];

    [Fact]
    public void Lista_RicercaSenzaMaiuscoleNeAccenti()
    {
        var stato = new StatoLista<Riga> { Ricerca = "citta" };
        Assert.Equal("Città", Assert.Single(stato.Applica(Righe, Colonne).Righe).Nome);
        stato.Ricerca = "1.200,5";
        Assert.Single(stato.Applica(Righe, Colonne).Righe);
    }

    [Fact]
    public void Lista_OrdineConIVuotiInFondo()
    {
        var stato = new StatoLista<Riga>();
        stato.OrdinaPer(1);
        Assert.Equal(["Arsura", "Città", "bosco"], stato.Applica(Righe, Colonne).Righe.Select(r => r.Nome));
        stato.OrdinaPer(1);
        Assert.True(stato.Discendente);
        Assert.Equal(["Città", "Arsura", "bosco"], stato.Applica(Righe, Colonne).Righe.Select(r => r.Nome));
        stato.OrdinaPer(0);
        Assert.Equal(["Arsura", "bosco", "Città"], stato.Applica(Righe, Colonne).Righe.Select(r => r.Nome));
    }

    [Fact]
    public void Lista_PagineEPaginaFuoriLimite()
    {
        var stato = new StatoLista<Riga> { PerPagina = 2, Pagina = 9 };
        var pagina = stato.Applica(Righe, Colonne);
        Assert.Equal((2, 1, 3), (pagina.Pagine, pagina.Pagina, pagina.Totale));
        Assert.Single(pagina.Righe);
    }

    [Fact]
    public void Lista_FormatoItaliano()
    {
        Assert.Equal("1.200,5", Colonne[1].TestoDi(Righe[0]));
        Assert.Equal("–", Colonne[1].TestoDi(Righe[1]));
        Assert.Equal("01/05/2026", Colonne[2].TestoDi(Righe[0]));
        Assert.Equal("03/05/2026 08:15", Colonne[2].TestoDi(Righe[1]));
        Assert.Equal("Sì", StatoLista<Riga>.Formatta(true));
    }

    [Fact]
    public async Task ErroreApi_ProblemDetailsConCampi()
    {
        using var risposta = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"title":"Controlla i campi segnati.","errors":{"Data":["Manca la data."]}}"""),
        };
        var esito = await PortaleApiClient.ErroreAsync<object>(risposta, default);
        Assert.Equal("Controlla i campi segnati.", esito.Errore);
        Assert.Equal(["Manca la data."], esito.ErroriCampi["data"]);

        using var vuota = new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("") };
        Assert.StartsWith("Qualcun altro", (await PortaleApiClient.ErroreAsync<object>(vuota, default)).Errore);
    }
}
