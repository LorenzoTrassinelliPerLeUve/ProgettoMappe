using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Scritture dei Trattamenti (tappa 4b, task #269) sull'Api vera con 4Grapes finto: nuovo per vigneto e di gruppo, modifica,
/// effettivo, elimina, validazioni, permessi, LogOp solo per la testata.
/// </summary>
public class PortaleScrittureTrattamentiTests
{
    private const int Master = 1, Consulente = 2, SenzaPermessi = 3;
    private static readonly DateTime Giorno = new(2026, 6, 10, 8, 0, 0);

    /// <summary>Vigneti: 4251 e 4252 dell'azienda 425 (rischio 1, linea 1, interfila 250 cm e nessuna), 4261 della 426.</summary>
    private static readonly Dictionary<int, (int Azienda, decimal? TraFila, int Rischio, int Linea)> Vigneti = new()
    {
        [4251] = (425, 250m, 1, 1), [4252] = (425, null, 1, 1), [4261] = (426, 250m, 2, 1),
    };

    private sealed class Permessi : IPermessiRepository
    {
        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(idUtente switch
            {
                Master => new(Master, true, false, true, false, true, null, new HashSet<string> { "TRATTAMENTI" }),
                Consulente => new(Consulente, true, false, false, false, false, null, new HashSet<string> { "TRATTAMENTI_AZIENDALI" }),
                SenzaPermessi => new(SenzaPermessi, true, false, false, false, false, null, new HashSet<string>()),
                _ => null,
            });
    }

    /// <summary>Letture sul <see cref="DbFinto"/> (tabelle «Trattamento» e «TrattamentoAvversita»).</summary>
    private sealed class Letture(DbFinto db) : ILettureScrittureTrattamenti
    {
        public bool InCorso { get; set; }

        private IEnumerable<TrattamentoEsistente> Tutti()
        {
            var prodotti = db.Tabella("TrattamentoAvversita").Values.ToList();
            return db.Tabella("Trattamento").Values.Select(r =>
            {
                var id = (int)r["Id"]!;
                var v = (int)r["IdVigneto"]!;
                return new TrattamentoEsistente(id, v, Vigneti[v].Azienda, Vigneti[v].TraFila, (DateTime)r["Data"]!, (bool)r["Automatico"]!,
                    (bool)r["Suggerito"]!, (bool)r["Effettivo"]!, (bool)r["Visibile"]!, (int)r["IdTipoTrattamento"]!, (int)r["IdBBCH"]!,
                    (int)r["IdCanopy"]!, (int)r["VolumeAcqua"]!, (int)r["VolumeAcquaRif"]!, r["CoefficienteCanopy"] as string,
                    r.GetValueOrDefault("IdTransitoNelVigneto") as int?, r.GetValueOrDefault("Note") as string, r.GetValueOrDefault("Gruppo_IdEnte") as int?,
                    r.GetValueOrDefault("Gruppo_IdRischio") as int?, r.GetValueOrDefault("Gruppo_IdTipologiaLinea") as int?,
                    r.GetValueOrDefault("Gruppo_Esclusi") as bool?, null,
                    prodotti.Where(p => Equals(p["IdTrattamento"], id)).Select(p => new ProdottoEsistente((int)p["Id"]!,
                        (int)p["IdAvversitaDaTrattare"]!, (string)p["NumRegProdottoVite"]!, p["IdLineaDifesa"] as int?,
                        (decimal)p["Dosaggio_Rif_Perleuve"]!, (decimal)p["Dosaggio"]!, (decimal)p["Dosaggio_OLD"]!, (decimal)p["Dosaggio_LWA"]!,
                        (decimal)p["Dosaggio_TRV"]!)).ToList());
            });
        }

        public Task<TrattamentoEsistente?> TrattamentoAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Tutti().FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<TrattamentoEsistente>> GruppoAsync(int ente, int? rischio, int? linea, bool esclusi, DateTime data,
            bool suggerito, bool effettivo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TrattamentoEsistente>>(Tutti().Where(t => t.Data == data && t.GruppoIdEnte == ente
                && t.GruppoIdRischio == rischio && (t.GruppoIdLinea is null || t.GruppoIdLinea == linea) && (t.GruppoEsclusi ?? false) == esclusi
                && t.Suggerito == suggerito && t.Effettivo == effettivo).ToList());

        public Task<IReadOnlyList<int>> DaEliminareAsync(int ente, int? rischio, bool? esclusi, DateTime data, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<int>>(Tutti().Where(t => t.GruppoIdEnte == ente && t.GruppoIdRischio == rischio
                && t.GruppoEsclusi == esclusi && t.Data == data).Select(t => t.Id).ToList());

        public Task<IReadOnlySet<int>> GeneratiDaAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<int>>(db.Tabella("Trattamento").Values
                .Select(r => r.GetValueOrDefault("GeneratoDa_IdTrattamento") as int?).OfType<int>().Where(ids.Contains).ToHashSet());

        public Task<TrattamentoEsistente?> GruppoEsclusiAsync(int ente, DateTime data, CancellationToken ct = default) =>
            Task.FromResult(Tutti().Where(t => t.Data == data && t.GruppoIdEnte == ente && t.GruppoEsclusi == true).MinBy(t => t.Id));

        public Task<IReadOnlyList<TrattamentoEsistente>> AutomaticiAsync(int? vigneto, int? ente, int? rischio, int? linea, DateTime dal,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TrattamentoEsistente>>(Tutti().Where(t => t.Automatico && t.Data >= dal && (vigneto is { } v
                ? t.IdVigneto == v
                : Vigneti[t.IdVigneto].Azienda == ente && Vigneti[t.IdVigneto].Rischio == rischio && Vigneti[t.IdVigneto].Linea == linea))
                .OrderBy(t => t.Data).ThenBy(t => t.Id).ToList());

        public Task<VignetoScrittura?> VignetoAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Vigneti.TryGetValue(id, out var v) ? new VignetoScrittura(id, v.Azienda, v.TraFila) : null);

        public Task<IReadOnlyList<VignetoScrittura>> VignetiGruppoAsync(int ente, int? rischio, int? linea, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<VignetoScrittura>>(Vigneti.Where(v => v.Value.Azienda == ente && (rischio is null || v.Value.Rischio == rischio)
                && (linea is null || v.Value.Linea == linea)).Select(v => new VignetoScrittura(v.Key, v.Value.Azienda, v.Value.TraFila)).ToList());

        public Task<IReadOnlyDictionary<int, int>> BbchAsync(IReadOnlyCollection<int> vigneti, DateTime data, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, int>>(vigneti.Where(v => v != 4252).ToDictionary(v => v, _ => 65));

        public Task<IReadOnlyList<CanopyRiga>> CanopyAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CanopyRiga>>([new(1, 0, 1m, 0.5m, 30m), new(40, 65, 1.2m, 0.6m, 20m)]);

        public Task<IReadOnlyList<Agrofarmaco>> AgrofarmaciAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Agrofarmaco>>([new(12, "11111", "RAME"), new(11, "22222", "ZOLFO"), new(5, "33333", "ALTRO")]);

        public Task<(IReadOnlyList<VoceFiltroDto> Avversita, IReadOnlyList<VoceFiltroDto> Transiti, IReadOnlyList<VoceFiltroDto> Bbch,
            IReadOnlyList<VoceFiltroDto> Rischi, IReadOnlyList<VoceFiltroDto> Linee)> OpzioniAsync(CancellationToken ct = default) =>
            Task.FromResult<(IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>, IReadOnlyList<VoceFiltroDto>,
                IReadOnlyList<VoceFiltroDto>)>(([new("12", "Peronospora"), new("11", "Oidio"), new("5", "Tignoletta")], [new("1", "A piedi")],
                [new("65", "65 - Fioritura")], [new("1", "ALTO")], [new("1", "Linea 1")]));

        public Task<bool> ElaborazioneInCorsoAsync(CancellationToken ct = default) => Task.FromResult(InCorso);
    }

    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db, Letture Letture) Avvia()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        var letture = new Letture(db);
        return (api.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<IPermessiRepository, Permessi>();
            s.AddSingleton<ILettureScrittureTrattamenti>(letture);
        })), db, letture);
    }

    private static TrattamentoSalvaDto Modulo(int? vigneto = null, int? azienda = null, params ProdottoTrattamentoSalvaDto[] prodotti) =>
        new(CalcoloTrattamenti.PeronosporaOidio, Giorno, "OLD", 1, "nota",
            prodotti.Length == 0 ? [new(12, "11111", 2m)] : prodotti, vigneto, azienda, azienda is null ? null : 1, azienda is null ? null : 1, -1);

    private static async Task<int> CreaAsync(HttpClient c, TrattamentoSalvaDto d)
    {
        var r = await c.PostAsJsonAsync("api/portale/trattamenti", d);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return int.Parse((await r.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Chiave!);
    }

    [Fact]
    public void Coefficienti_ComeCanopyDelPortale()
    {
        var c = ScrittureTrattamenti.Coeff(new CanopyRiga(40, 65, 1.2m, 0.6m, 20m), 250m, 1.2m, 0.6m);
        Assert.Equal(1m, c.Old);
        Assert.Equal(0.84m, Math.Round(c.Lwa, 6)); // 0,7 × (10000 / 2,5 × 2 × 1,2) / 10000 / 0,8
        Assert.Equal(0.875m, Math.Round(c.Trv, 6));
        // Senza interfila: 280 cm.
        Assert.Equal(0.75m, Math.Round(ScrittureTrattamenti.Coeff(new CanopyRiga(40, 65, 1.2m, 0.6m, 20m), null, 1.2m, 0.6m).Lwa, 6));
        Assert.Equal((1.75m, 2m, 1.68m, 1.75m), ScrittureTrattamenti.Dosaggi(2m, (1m, 0.84m, 0.875m), "TRV"));
    }

    [Fact]
    public async Task Nuovo_PerVigneto_SuggeritoConFaseDaiRilieviEDosaggi_LogOpSoloTestata()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);

        var r = await c.PostAsJsonAsync("api/portale/trattamenti", Modulo(vigneto: 4251));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var esito = await r.Content.ReadFromJsonAsync<EsitoScritturaDto>();
        Assert.Equal(["nuovo Trattamento"], esito!.Log);

        var t = Assert.Single(db.Tabella("Trattamento").Values);
        Assert.Equal((true, false, false, true), ((bool)t["Suggerito"]!, (bool)t["Effettivo"]!, (bool)t["Automatico"]!, (bool)t["Visibile"]!));
        Assert.Equal((65, 40), ((int)t["IdBBCH"]!, (int)t["IdCanopy"]!));
        Assert.Null(t["Gruppo_IdEnte"]);
        Assert.Equal(Consulente, t["IdUtenteCreazione"]);
        var p = Assert.Single(db.Tabella("TrattamentoAvversita").Values);
        Assert.Equal((2m, 2m, 1.68m, 1.75m, 2m), ((decimal)p["Dosaggio_Rif_Perleuve"]!, (decimal)p["Dosaggio_OLD"]!, (decimal)p["Dosaggio_LWA"]!,
            (decimal)p["Dosaggio_TRV"]!, (decimal)p["Dosaggio"]!));
    }

    [Fact]
    public async Task Nuovo_DiGruppo_UnoPerVigneto_FaseDaiRilieviOZero()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);

        await CreaAsync(c, Modulo(azienda: 425));

        var righe = db.Tabella("Trattamento").Values.OrderBy(r => (int)r["IdVigneto"]!).ToList();
        Assert.Equal([4251, 4252], righe.Select(r => (int)r["IdVigneto"]!));
        Assert.All(righe, r => Assert.Equal((425, 1, 1, false), ((int)r["Gruppo_IdEnte"]!, (int)r["Gruppo_IdRischio"]!,
            (int)r["Gruppo_IdTipologiaLinea"]!, (bool)r["Gruppo_Esclusi"]!)));
        Assert.Equal(0, righe[1]["IdBBCH"]); // 4252 senza ModelloBBCH: fase 0, Canopy 1
        Assert.Equal(2, db.Tabella("TrattamentoAvversita").Count);
    }

    [Fact]
    public async Task Validazioni_ComeIlPortale()
    {
        var (api, db, letture) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);

        async Task<string> ErroreAsync(TrattamentoSalvaDto d)
        {
            var r = await c.PostAsJsonAsync("api/portale/trattamenti", d);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            return string.Join(" ", (await PortaleDifesaTests.ErroriAsync(r)).SelectMany(e => e.Value));
        }

        Assert.Contains("due volte", await ErroreAsync(Modulo(4251, null, new(12, "11111", 1m), new(12, "11111", 2m))));
        Assert.Contains("non ammesso", await ErroreAsync(Modulo(4251, null, new ProdottoTrattamentoSalvaDto(12, "22222", 1m))));
        Assert.Contains("Avversità non ammessa", await ErroreAsync(Modulo(4251, null, new ProdottoTrattamentoSalvaDto(5, "33333", 1m))));
        Assert.Contains("Dosaggio", await ErroreAsync(Modulo(4251, null, new ProdottoTrattamentoSalvaDto(12, "11111", null))));
        Assert.Contains("Rischio o Tipologia", await ErroreAsync(Modulo(azienda: 425) with { IdRischio = null }));
        Assert.Contains("data", await ErroreAsync(Modulo(4251) with { Data = null }));
        Assert.Contains("CANOPY", await ErroreAsync(Modulo(4251) with { CoefficienteCanopy = "XYZ" }));
        Assert.Empty(db.Tabella("Trattamento"));

        letture.InCorso = true;
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("api/portale/trattamenti", Modulo(4251))).StatusCode);
    }

    [Fact]
    public async Task Permessi_InserireAbbinata_ModificareSoloAggiornamentiAbilitati()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var consulente = PortaleDifesaTests.Client(api, Consulente);
        var master = PortaleDifesaTests.Client(api, Master);

        var su426 = await CreaAsync(consulente, Modulo(vigneto: 4261)); // abbinata: inserire sì
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PutAsJsonAsync($"api/portale/trattamenti/{su426}", Modulo())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync($"api/portale/trattamenti/{su426}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await master.DeleteAsync($"api/portale/trattamenti/{su426}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PortaleDifesaTests.Client(api, SenzaPermessi).PostAsJsonAsync("api/portale/trattamenti", Modulo(4251))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await master.PutAsJsonAsync("api/portale/trattamenti/999", Modulo())).StatusCode);
        Assert.Empty(db.Tabella("Trattamento"));
    }

    [Fact]
    public async Task Modifica_TuttoIlGruppo_TogliVigneto_ProdottiAggiornati()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);
        var id = await CreaAsync(c, Modulo(azienda: 425, prodotti: [new(12, "11111", 2m), new(11, "22222", 1m)]));

        var r = await c.PutAsJsonAsync($"api/portale/trattamenti/{id}",
            Modulo(azienda: 425, prodotti: [new(12, "11111", 3m)]) with { CoefficienteCanopy = "LWA", Note = "cambiata", VignetiDaRimuovere = [4252] });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var rimasto = Assert.Single(db.Tabella("Trattamento").Values);
        Assert.Equal((4251, "cambiata", "LWA"), ((int)rimasto["IdVigneto"]!, (string)rimasto["Note"]!, (string)rimasto["CoefficienteCanopy"]!));
        var prodotti = db.Tabella("TrattamentoAvversita").Values.Where(p => Equals(p["IdTrattamento"], rimasto["Id"])).ToList();
        var rame = Assert.Single(prodotti);
        Assert.Equal((3m, 2.52m), ((decimal)rame["Dosaggio_Rif_Perleuve"]!, (decimal)rame["Dosaggio"]!)); // LWA 0,84 × 3
    }

    [Fact]
    public async Task Modifica_RimuoviDalGruppo_SoloQuesto()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);
        var id = await CreaAsync(c, Modulo(azienda: 425));

        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"api/portale/trattamenti/{id}", Modulo() with { RimuoviDalGruppo = true })).StatusCode);

        var righe = db.Tabella("Trattamento").Values.ToList();
        Assert.Null(righe.Single(r => Equals(r["Id"], id))["Gruppo_IdEnte"]);
        Assert.Equal(425, righe.Single(r => !Equals(r["Id"], id))["Gruppo_IdEnte"]);
    }

    [Fact]
    public async Task Effettivo_DelGruppo_NuoviTrattamentiEffettivi_SuggeritiNascosti_PoiNonSiEliminanoISuggeriti()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);
        var id = await CreaAsync(c, Modulo(azienda: 425));

        var r = await c.PostAsJsonAsync($"api/portale/trattamenti/{id}/effettivo", Modulo() with { Data = Giorno.AddHours(2) });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var righe = db.Tabella("Trattamento").Values.ToList();
        var effettivi = righe.Where(t => (bool)t["Effettivo"]!).ToList();
        Assert.Equal(2, effettivi.Count);
        Assert.All(effettivi, e => Assert.Equal(Giorno.AddHours(2), e["Data"]));
        Assert.All(righe.Where(t => (bool)t["Suggerito"]!), s => Assert.False((bool)s["Visibile"]!));
        Assert.Contains(id, effettivi.Select(e => (int)e["GeneratoDa_IdTrattamento"]!));
        Assert.Equal(4, db.Tabella("TrattamentoAvversita").Count);

        // Un effettivo non diventa di nuovo effettivo.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync($"api/portale/trattamenti/{effettivi[0]["Id"]}/effettivo", Modulo())).StatusCode);
        // Il suggerito è citato dagli effettivi: non si cancella; gli effettivi sì, tutti insieme.
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"api/portale/trattamenti/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.DeleteAsync($"api/portale/trattamenti/{effettivi[0]["Id"]}")).StatusCode);
        Assert.Equal(2, db.Tabella("Trattamento").Count);
    }

    [Fact]
    public async Task Escludi_SenzaGruppoEsclusi_IlVignetoPassaNegliEsclusiConISuoiDati()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);
        var id = await CreaAsync(c, Modulo(azienda: 425));
        var log = db.LogOp.Count;

        var r = await c.PutAsJsonAsync($"api/portale/trattamenti/{id}",
            Modulo(prodotti: [new(12, "11111", 4m)]) with { Note = "gruppo", VignetiDaEscludere = [4252] });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var righe = db.Tabella("Trattamento").Values.ToDictionary(t => (int)t["IdVigneto"]!);
        var escluso = righe[4252];
        Assert.Equal((425, null, null, true), (escluso["Gruppo_IdEnte"], escluso["Gruppo_IdRischio"], escluso["Gruppo_IdTipologiaLinea"],
            escluso["Gruppo_Esclusi"]));
        Assert.Equal("nota", escluso["Note"]); // tiene i suoi dati: il modulo vale solo per il gruppo
        Assert.Equal(("gruppo", false), (righe[4251]["Note"], righe[4251]["Gruppo_Esclusi"]));
        var prodottoEscluso = db.Tabella("TrattamentoAvversita").Values.Single(p => Equals(p["IdTrattamento"], escluso["Id"]));
        Assert.Equal(2m, prodottoEscluso["Dosaggio_Rif_Perleuve"]);
        // Una riga di LogOp per testata: il gruppo (1) e l'escluso (1).
        Assert.Equal(2, db.LogOp.Count - log);
    }

    [Fact]
    public async Task Escludi_ConGruppoEsclusiNelGiorno_PrendeDatiEProdottiDiQuello()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);
        var id = await CreaAsync(c, Modulo(azienda: 425));
        // Il gruppo «ESCLUSI» di quel giorno: un trattamento della 4261 (un'altra azienda nei dati finti, basta il Gruppo_IdEnte).
        db.Metti("Trattamento", 900, Riga(900, 4261, Giorno, automatico: false));
        var esclusi = db.Tabella("Trattamento")[900];
        esclusi["Gruppo_IdEnte"] = 425;
        esclusi["Gruppo_Esclusi"] = true;
        esclusi["Note"] = "esclusi";
        esclusi["CoefficienteCanopy"] = "TRV";
        db.Metti("TrattamentoAvversita", 900, Prodotto(900, 900, 11, "22222", 7, 1.5m));

        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"api/portale/trattamenti/{id}", Modulo() with { VignetiDaEscludere = [4252] })).StatusCode);

        var escluso = db.Tabella("Trattamento").Values.Single(t => Equals(t["IdVigneto"], 4252));
        Assert.Equal(("esclusi", "TRV", true, null), (escluso["Note"], escluso["CoefficienteCanopy"], escluso["Gruppo_Esclusi"], escluso["Gruppo_IdRischio"]));
        var prodotto = Assert.Single(db.Tabella("TrattamentoAvversita").Values, p => Equals(p["IdTrattamento"], escluso["Id"]));
        Assert.Equal(("22222", 7, 1.5m), (prodotto["NumRegProdottoVite"], prodotto["IdLineaDifesa"], prodotto["Dosaggio_Rif_Perleuve"]));
    }

    [Fact]
    public void ProdottiDaAutomatici_TuttiUnaVolta_ValeIlPrimoPerData()
    {
        TrattamentoEsistente T(int id, int giorni, params ProdottoEsistente[] p) => new(id, 4251, 425, null, Giorno.AddDays(giorni), true, false,
            false, true, 100, 65, 40, 0, 0, "OLD", null, null, null, null, null, null, null, p);
        ProdottoEsistente P(int id, string numero, decimal rif) => new(id, 12, numero, null, rif, rif, rif, rif, rif);

        var prodotti = ScrittureTrattamenti.ProdottiDaAutomatici([T(2, 2, P(3, "11111", 9m), P(4, "44444", 1m)), T(1, 1, P(1, "11111", 2m))]);

        Assert.Equal([("11111", 2m), ("44444", 1m)], prodotti.Select(p => (p.NumReg, p.Rif)));
    }

    [Fact]
    public async Task DaAutomatici_PerVigneto_SuggeritoDalPrimoAutomaticoConIProdottiDiTutti()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);
        var domani = DateTime.Today.AddDays(1).AddHours(6);
        db.Metti("Trattamento", 100, Riga(100, 4251, domani));
        db.Metti("Trattamento", 101, Riga(101, 4251, domani.AddDays(2)));
        db.Metti("Trattamento", 102, Riga(102, 4251, DateTime.Today.AddDays(-2))); // passato: non conta
        db.Metti("TrattamentoAvversita", 100, Prodotto(100, 100, 12, "11111", 5, 2m));
        db.Metti("TrattamentoAvversita", 101, Prodotto(101, 101, 11, "22222", 6, 1m));
        db.Metti("TrattamentoAvversita", 102, Prodotto(102, 102, 5, "33333", null, 1m));

        var proposta = await c.GetFromJsonAsync<PropostaSuggeritoDto>("api/portale/trattamenti/da-automatici?trattamento=100");
        Assert.Equal(4251, proposta!.IdVigneto);
        Assert.Equal(domani, proposta.Scheda.Data);
        Assert.Equal(["11111", "22222"], proposta.Scheda.Prodotti.Select(p => p.NumReg));

        var r = await c.PostAsJsonAsync("api/portale/trattamenti/da-automatici",
            Modulo(vigneto: 4251, prodotti: [new(12, "11111", 2m), new(11, "22222", 1m)]) with { Data = domani });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var nuovo = db.Tabella("Trattamento").Values.Single(t => !(bool)t["Automatico"]!);
        Assert.Equal((4251, true, false, true, 65, 40), ((int)nuovo["IdVigneto"]!, (bool)nuovo["Suggerito"]!, (bool)nuovo["Effettivo"]!,
            (bool)nuovo["Visibile"]!, (int)nuovo["IdBBCH"]!, (int)nuovo["IdCanopy"]!));
        Assert.Null(nuovo["Gruppo_IdEnte"]);
        var prodotti = db.Tabella("TrattamentoAvversita").Values.Where(p => Equals(p["IdTrattamento"], nuovo["Id"])).ToList();
        Assert.Equal([5, 6], prodotti.Select(p => (int)p["IdLineaDifesa"]!).Order()); // la linea di difesa degli automatici resta
        Assert.All(db.Tabella("Trattamento").Values.Where(t => (bool)t["Automatico"]!), a => Assert.True((bool)a["Visibile"]!));
    }

    [Fact]
    public async Task TrattaAzienda_UnSuggeritoDiGruppoPerVignetoConAutomatici_DosaggiSullaChiomaDelVigneto()
    {
        var (api, db, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Master);
        var domani = DateTime.Today.AddDays(1).AddHours(6);
        db.Metti("Trattamento", 100, Riga(100, 4251, domani));
        db.Metti("Trattamento", 101, Riga(101, 4252, domani.AddDays(1)));
        db.Metti("Trattamento", 102, Riga(102, 4261, domani)); // altra azienda
        db.Metti("TrattamentoAvversita", 100, Prodotto(100, 100, 12, "11111", 5, 2m));

        var tratta = Modulo(prodotti: [new(12, "11111", 2m)]) with { Data = domani, IdAzienda = 425, IdRischio = 1, IdTipologiaLinea = 1, CoefficienteCanopy = "LWA" };
        var vuota = await c.PostAsJsonAsync("api/portale/trattamenti/da-automatici", tratta with { IdRischio = null });
        Assert.Equal(HttpStatusCode.BadRequest, vuota.StatusCode);
        Assert.Contains("rischio e tipologia linea", string.Join(" ", (await PortaleDifesaTests.ErroriAsync(vuota)).SelectMany(e => e.Value)));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync("api/portale/trattamenti/da-automatici", tratta with { IdAzienda = 426, IdRischio = 9 })).StatusCode);

        var proposta = await c.GetFromJsonAsync<PropostaSuggeritoDto>("api/portale/trattamenti/da-automatici?azienda=425&rischio=1&linea=1");
        Assert.Equal([4251, 4252], proposta!.Vigneti);

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("api/portale/trattamenti/da-automatici", tratta)).StatusCode);

        var nuovi = db.Tabella("Trattamento").Values.Where(t => !(bool)t["Automatico"]!).OrderBy(t => (int)t["IdVigneto"]!).ToList();
        Assert.Equal([4251, 4252], nuovi.Select(t => (int)t["IdVigneto"]!));
        Assert.All(nuovi, t => Assert.Equal((425, 1, 1, false, true), ((int)t["Gruppo_IdEnte"]!, (int)t["Gruppo_IdRischio"]!,
            (int)t["Gruppo_IdTipologiaLinea"]!, (bool)t["Gruppo_Esclusi"]!, (bool)t["Suggerito"]!)));
        // LWA: 4251 con interfila 250 cm (0,84 × 2), 4252 senza interfila = 280 cm (0,75 × 2).
        var dosaggi = nuovi.Select(t => db.Tabella("TrattamentoAvversita").Values.Single(p => Equals(p["IdTrattamento"], t["Id"]))["Dosaggio"]);
        Assert.Equal([1.68m, 1.5m], dosaggi);
    }

    /// <summary>Un trattamento automatico (fase 65, chioma 40) con tutte le colonne che le letture finte guardano.</summary>
    private static Dictionary<string, object?> Riga(int id, int vigneto, DateTime data, bool automatico = true) => new()
    {
        ["Id"] = id, ["IdVigneto"] = vigneto, ["Data"] = data, ["Automatico"] = automatico, ["Suggerito"] = !automatico, ["Effettivo"] = false,
        ["Visibile"] = true, ["IdTipoTrattamento"] = CalcoloTrattamenti.PeronosporaOidio, ["IdBBCH"] = 65, ["IdCanopy"] = 40, ["VolumeAcqua"] = 0,
        ["VolumeAcquaRif"] = 0, ["CoefficienteCanopy"] = "OLD",
    };

    private static Dictionary<string, object?> Prodotto(int id, int trattamento, int avversita, string numero, int? linea, decimal rif) => new()
    {
        ["Id"] = id, ["IdTrattamento"] = trattamento, ["IdAvversitaDaTrattare"] = avversita, ["NumRegProdottoVite"] = numero,
        ["IdLineaDifesa"] = linea, ["Dosaggio_Rif_Perleuve"] = rif, ["Dosaggio"] = rif, ["Dosaggio_OLD"] = rif, ["Dosaggio_LWA"] = rif,
        ["Dosaggio_TRV"] = rif,
    };

    [Fact]
    public async Task Scritture_SoloTabelleAmmesse()
    {
        Assert.Contains(TabelleScrivibili.Trattamento, TabelleScrivibili.Tutte);
        Assert.True(TabelleScrivibili.TrattamentoAvversita.SenzaLogOp);
        var (api, _, _) = Avvia();
        await using var _a = api;
        var opzioni = await PortaleDifesaTests.Client(api, Consulente).GetFromJsonAsync<OpzioniTrattamentoDto>("api/portale/trattamenti/opzioni");
        Assert.Equal(["12", "11"], opzioni!.AvversitaPeronosporaOidio.Select(a => a.Id));
        Assert.Equal(["5"], opzioni.AvversitaAltre.Select(a => a.Id));
        Assert.Equal(2, opzioni.Aziende.Count);
        var prodotti = await PortaleDifesaTests.Client(api, Consulente).GetFromJsonAsync<List<VoceFiltroDto>>("api/portale/trattamenti/prodotti?avversita=12");
        Assert.Equal(["RAME"], prodotti!.Select(p => p.Nome));
    }
}
