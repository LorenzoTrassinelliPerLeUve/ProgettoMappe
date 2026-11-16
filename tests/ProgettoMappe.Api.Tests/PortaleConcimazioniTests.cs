using System.Net;
using System.Net.Http.Json;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using ProgettoMappe.Web.Portale;
using Xunit;
using static ProgettoMappe.Api.Tests.PortaleDifesaTests;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Sezione 18 Concimazioni del Portale (tappa 3, task #260): per ogni API di scrittura inserimento, modifica, cancellazione (dove
/// il portale la ha) e permesso negato, sull'Api vera con 4Grapes finto (<see cref="DbFinto"/>).
/// </summary>
public class PortaleConcimazioniTests
{
    private static NuovoTargetDto Target(int azienda = 425, params int[] vigneti) =>
        new(2026, azienda, vigneti.Length == 0 ? [azienda * 10] : vigneti, 1, 40, 20, 60, 10);

    private static ConcimazioneRealeSalvaDto Reale(int azienda = 425, int[]? vigneti = null, decimal n = 12.5m, int? target = 1) =>
        new(azienda, null, vigneti ?? [azienda * 10], new DateTime(2026, 3, 15), target, "Nitrophoska", 1, 2, n, 8, 16, 2, 300, 0.85m);

    private static async Task<string> ChiaveAsync(HttpResponseMessage risposta)
    {
        Assert.Equal(HttpStatusCode.OK, risposta.StatusCode);
        return (await risposta.Content.ReadFromJsonAsync<EsitoScritturaDto>())!.Chiave!;
    }

    private static Dictionary<object, Dictionary<string, object?>> Unita(DbFinto db) => db.Tabella("UnitaBaseConcimazione");

    // ---- Target enologico (UnitaBaseConcimazione) ----

    [Fact]
    public async Task NuovoTarget_InserisceUnaRigaPerVigneto_ColonneMancantiAZero()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Consulente);

        Assert.Equal("2", await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4250, 4251))));
        var riga = Unita(db)[new ChiaveDoppia(2026, 4250)];
        Assert.Equal(425, riga["IdEnte"]);
        Assert.Equal(1, riga["IdTarget"]);
        Assert.Equal(40, riga["N"]);
        Assert.Equal(10, riga["MG"]);
        Assert.Equal(0, riga["CA"]);
        Assert.Equal(0, riga["RiserveInvernali"]);
        Assert.Equal(2, Unita(db).Count);
        Assert.Equal(["nuovo UnitaBaseConcimazione", "nuovo UnitaBaseConcimazione"], db.LogOp.Select(l => $"{l.Operazione} {l.Tabella}"));
        Assert.All(db.LogOp, l => Assert.Equal(0, l.IdRiga));
    }

    [Fact]
    public async Task NuovoTarget_VignetoGiaConTarget_NienteSiSalva_MessaggioDelPortale()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Consulente);
        await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4251)));

        var doppio = await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4250, 4251));
        Assert.Equal(HttpStatusCode.BadRequest, doppio.StatusCode);
        Assert.Equal(["Salvataggio non riuscito. E' già presente un vigneto target enologico Spumante base"],
            (await ErroriAsync(doppio))["IdVigneti"]);
        Assert.Single(Unita(db));
    }

    [Fact]
    public async Task NuovoTarget_CampiObbligatori_VignetoDiUnAltraAzienda()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Consulente);

        var vuota = await client.PostAsJsonAsync("api/portale/concimazioni/target", new NuovoTargetDto(null, null, [], null, null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, vuota.StatusCode);
        Assert.Equal(["Anno", "IdAzienda", "IdTargetEnologico", "IdVigneti", "K", "MG", "N", "P"],
            (await ErroriAsync(vuota)).Keys.Order());

        var altrui = await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4260));
        Assert.Equal(HttpStatusCode.BadRequest, altrui.StatusCode);
        Assert.Contains("IdVigneti", (await ErroriAsync(altrui)).Keys);
        Assert.Empty(Unita(db));
    }

    [Fact]
    public async Task NuovoTarget_PermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        // Senza CONCIMAZIONI la sezione è chiusa; un'azienda non abbinata non si tocca.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client(api, PermessiFinti.SenzaPermessi).PostAsJsonAsync("api/portale/concimazioni/target", Target())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client(api, PermessiFinti.Consulente).PostAsJsonAsync("api/portale/concimazioni/target", Target(500))).StatusCode);
        Assert.Empty(Unita(db));
        Assert.Empty(db.LogOp);
    }

    [Fact]
    public async Task CambiaTarget_AggiornaOCrea_PermessoDiModificaPerLeRigheCheCiSono()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var master = Client(api, PermessiFinti.Master);
        var consulente = Client(api, PermessiFinti.Consulente);
        await ChiaveAsync(await master.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4250)));
        await ChiaveAsync(await master.PostAsJsonAsync("api/portale/concimazioni/target", Target(426, 4260)));

        // 425 è modificabile dal consulente: la riga si aggiorna.
        Assert.Equal("2026/4250", await ChiaveAsync(await consulente.PutAsJsonAsync("api/portale/concimazioni/target/2026/4250",
            new CambiaTargetDto(425, 2, 55, 25, 70, 12))));
        var riga = Unita(db)[new ChiaveDoppia(2026, 4250)];
        Assert.Equal(2, riga["IdTarget"]);
        Assert.Equal(55, riga["N"]);
        Assert.Equal(0, riga["CA"]);

        // Un anno senza riga: si crea.
        await ChiaveAsync(await consulente.PutAsJsonAsync("api/portale/concimazioni/target/2025/4250", new CambiaTargetDto(425, 1, 1, 2, 3, 4)));
        Assert.Equal(425, Unita(db)[new ChiaveDoppia(2025, 4250)]["IdEnte"]);

        // 426 è abbinata ma non modificabile: la riga che c'è no, una nuova sì.
        var vietata = await consulente.PutAsJsonAsync("api/portale/concimazioni/target/2026/4260", new CambiaTargetDto(426, 2, 1, 1, 1, 1));
        Assert.Equal(HttpStatusCode.Forbidden, vietata.StatusCode);
        Assert.Equal(1, Unita(db)[new ChiaveDoppia(2026, 4260)]["IdTarget"]);
        await ChiaveAsync(await consulente.PutAsJsonAsync("api/portale/concimazioni/target/2024/4260", new CambiaTargetDto(426, 2, 1, 1, 1, 1)));

        // Vigneto di un'altra azienda.
        Assert.Equal(HttpStatusCode.NotFound,
            (await consulente.PutAsJsonAsync("api/portale/concimazioni/target/2026/4260", new CambiaTargetDto(425, 2, 1, 1, 1, 1))).StatusCode);
        Assert.Equal(["nuovo", "nuovo", "modificato", "nuovo", "nuovo"], db.LogOp.Select(l => l.Operazione));
    }

    [Fact]
    public async Task CambiaTarget_CampiObbligatori_EPermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var vuota = await Client(api, PermessiFinti.Consulente).PutAsJsonAsync("api/portale/concimazioni/target/2026/4250",
            new CambiaTargetDto(425, null, null, 1, 1, 1));
        Assert.Equal(["IdTarget", "N"], (await ErroriAsync(vuota)).Keys.Order());
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.SenzaPermessi)
            .PutAsJsonAsync("api/portale/concimazioni/target/2026/4250", new CambiaTargetDto(425, 1, 1, 1, 1, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(api, PermessiFinti.Consulente)
            .PutAsJsonAsync("api/portale/concimazioni/target/2026/5000", new CambiaTargetDto(500, 1, 1, 1, 1, 1))).StatusCode);
        Assert.Empty(Unita(db));
    }

    // ---- Concimazione reale ----

    [Fact]
    public async Task Reale_InserisciModificaElimina_ConLogOp()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Consulente);

        var id = int.Parse(await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/reale", Reale())));
        var riga = db.Tabella("ConcimazioneReale")[id];
        Assert.Equal(4250, riga["IdVigneto"]);
        Assert.Equal(425, riga["IdEnte"]);
        Assert.Equal(1, riga["IdInserimento"]);
        Assert.Equal(12.5m, riga["N"]);
        Assert.Equal("Nitrophoska", riga["Concime"]);
        Assert.Equal(new DateTime(2026, 3, 15), riga["Data"]);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"api/portale/concimazioni/reale/{id}", Reale(n: 20))).StatusCode);
        riga = db.Tabella("ConcimazioneReale")[id];
        Assert.Equal(20m, riga["N"]);
        Assert.Equal(4250, riga["IdVigneto"]);

        var resoconto = await client.GetFromJsonAsync<ResocontoConcimazioniDto>("api/portale/concimazioni/resoconto?anno=2026&azienda=425");
        var letta = Assert.Single(resoconto!.Reale);
        Assert.Equal(id, letta.Id);
        Assert.True(letta.Modificabile);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"api/portale/concimazioni/reale/{id}")).StatusCode);
        Assert.Empty(db.Tabella("ConcimazioneReale"));
        Assert.Equal(["nuovo ConcimazioneReale", "modificato ConcimazioneReale", "cancellato ConcimazioneReale"],
            db.LogOp.Select(l => $"{l.Operazione} {l.Tabella}"));
        Assert.All(db.LogOp, l => Assert.Equal(id, l.IdRiga));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"api/portale/concimazioni/reale/{id}")).StatusCode);
    }

    [Fact]
    public async Task Reale_SenzaVigneti_TuttiQuelliAttiviConIlTarget_StessoInserimento()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var client = Client(api, PermessiFinti.Master);
        // 4250 e 4252 hanno il target 1 (in anni diversi), 4251 è estirpato: resta fuori anche col target.
        await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4250, 4251)));
        await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/target", Target(425, 4252) with { Anno = 2025 }));
        await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(vigneti: [4253])));

        await ChiaveAsync(await client.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(vigneti: [])));
        var righe = db.Tabella("ConcimazioneReale").Values.Where(r => Equals(r["IdInserimento"], 2)).ToList();
        Assert.Equal([4250, 4252], righe.Select(r => (int)r["IdVigneto"]!).Order());

        // Nessun vigneto con quei filtri: il portale salverebbe niente dicendo «salvato»; qui un errore sul campo.
        var nessuno = await client.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(vigneti: [], target: 2));
        Assert.Equal(HttpStatusCode.BadRequest, nessuno.StatusCode);
        Assert.Contains("IdVigneti", (await ErroriAsync(nessuno)).Keys);
    }

    [Fact]
    public async Task Reale_CampiObbligatoriDellaScheda()
    {
        var (api, _) = Avvia();
        await using var __ = api;
        var vuota = await Client(api, PermessiFinti.Consulente).PostAsJsonAsync("api/portale/concimazioni/reale",
            new ConcimazioneRealeSalvaDto(null, null, null, null, null, null, null, null, null, null, null, null, null, 100_000m));
        Assert.Equal(["Data", "IdAzienda", "IdEpocaConcimazione", "IdModalitaConcimazione", "IdTargetEnologico", "K2O", "MGO", "N",
            "P2O5", "Prezzo"], (await ErroriAsync(vuota)).Keys.Order());
    }

    [Fact]
    public async Task Reale_PermessoNegato()
    {
        var (api, db) = Avvia();
        await using var _ = api;
        var master = Client(api, PermessiFinti.Master);
        var consulente = Client(api, PermessiFinti.Consulente);
        var id426 = int.Parse(await ChiaveAsync(await master.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(426))));

        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client(api, PermessiFinti.SenzaPermessi).PostAsJsonAsync("api/portale/concimazioni/reale", Reale())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(500))).StatusCode);
        // 426: abbinata, si inserisce ma non si modifica né si elimina.
        await ChiaveAsync(await consulente.PostAsJsonAsync("api/portale/concimazioni/reale", Reale(426)));
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.PutAsJsonAsync($"api/portale/concimazioni/reale/{id426}", Reale(426, n: 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulente.DeleteAsync($"api/portale/concimazioni/reale/{id426}")).StatusCode);
        Assert.Equal(12.5m, db.Tabella("ConcimazioneReale")[id426]["N"]);
        Assert.Equal(2, db.Tabella("ConcimazioneReale").Count);
    }

    // ---- Letture e regole pure ----

    [Fact]
    public async Task Opzioni_AziendeEPermessi()
    {
        var (api, _) = Avvia();
        await using var __ = api;
        var client = Client(api, PermessiFinti.Consulente);
        var opzioni = await client.GetFromJsonAsync<OpzioniConcimazioniDto>("api/portale/concimazioni/opzioni?azienda=426");
        Assert.Equal([425, 426], opzioni!.Aziende.Select(a => a.Id));
        Assert.True(opzioni.Inseribile);
        Assert.False(opzioni.Modificabile);
        Assert.Equal(4, opzioni.Vigneti.Count);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/concimazioni/opzioni?azienda=500")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/concimazioni/unita?anno=2026&azienda=500")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/portale/concimazioni/fattori?anno=2026&azienda=500")).StatusCode);
    }

    [Fact]
    public void Filtra_ComeIlPortale_TargetVignetoConTarget()
    {
        var unita = new UnitaConcimazioneDto(
            [new(1, "A", 1, "T1", 1, 1, 1, 1, 0, 0), new(2, "B", null, null, 1, 1, 1, 1, 0, 0), new(3, "C", 2, "T2", 1, 1, 1, 1, 0, 0)],
            [new(10, "Vigna", 1, "T1", 1, 1, 1, 1, 0, 0), new(10, "Vigna", null, null, 1, 1, 1, 1, 0, 0)],
            [new(425, "Az", 2, "T2", 1, 1, 1, 1, 0, 0)]);
        var conTarget = PortaleConcimazioniController.Filtra(unita, null, null, conTarget: true);
        Assert.Equal([1, 3], conTarget.Vigneti.Select(r => r.Id));
        Assert.Single(conTarget.Vigne);
        var t1 = PortaleConcimazioniController.Filtra(unita, 1, null, false);
        Assert.Equal([1], t1.Vigneti.Select(r => r.Id));
        Assert.Empty(t1.Aziende);
        // Il vigneto filtra solo la tabella dei vigneti.
        var vigneto = PortaleConcimazioniController.Filtra(unita, null, 2, false);
        Assert.Equal([2], vigneto.Vigneti.Select(r => r.Id));
        Assert.Equal(2, vigneto.Vigne.Count);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("1", "MOLTO BASSA")]
    [InlineData("2.49", "BASSA")]
    [InlineData("2.5", "MEDIO BASSA")]
    [InlineData("5.5", "MEDIO ALTA")]
    [InlineData("8.5", "MOLTO ALTA")]
    [InlineData("10", "ECCESSIVA")]
    public void ClasseVigoria_SoglieDelPortale(string? punteggio, string? classe) =>
        Assert.Equal(classe, PortaleConcimazioniController.ClasseVigoria(punteggio is null ? null : decimal.Parse(punteggio,
            System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void ModuliDelWeb_ComeLeSchedeDelPortale()
    {
        var target = new TargetEnologicoDto[] { new(1, "Spumante base", 40, 20, 60, 10, 0, 0, null, null, null, null, null, null, null, null) };
        var vigneti = new VignetoConcimazioneDto[]
        {
            new(10, "A", 1, "Vigna 1", 7, "Glera", true), new(11, "B", 1, "Vigna 1", 8, "Merlot", false), new(12, "C", 2, "Vigna 2", 7, "Glera", true),
        };
        var unita = new UnitaBaseDto[] { new(2025, 12, 1, 1, 1, 1, 1) };

        // Target enologico: scelto il target, i valori diventano i suoi; i vigneti sono gli attivi della vigna e del vitigno.
        var nuovo = new ModuloNuovoTarget { IdTargetEnologico = 1, IdVitigno = 7 };
        nuovo.Proponi(target);
        Assert.Equal((40, 20, 60, 10), (nuovo.N!.Value, nuovo.P!.Value, nuovo.K!.Value, nuovo.MG!.Value));
        Assert.Equal([10, 12], nuovo.Vigneti(vigneti).Select(v => v.IdVigneto));

        // Concimazione reale: con il target, solo i vigneti che lo hanno avuto (in un anno qualsiasi).
        var reale = new ModuloConcimazioneReale { IdTargetEnologico = 1, Concime = " " };
        Assert.Equal([12], reale.Vigneti(vigneti, unita).Select(v => v.IdVigneto));
        Assert.Null(reale.Dto().Concime);

        // Cambia target: si apre con la riga dell'anno, vuota se non c'è.
        var cambia = ModuloCambiaTarget.Da(2025, 425, 12, unita);
        Assert.Equal(1, cambia.IdTarget);
        Assert.Null(ModuloCambiaTarget.Da(2026, 425, 12, unita).IdTarget);
    }

    [Fact]
    public void ExcelUnita_TreFogliComeIlPortale()
    {
        var tabella = new TabellaPortaleDto(["_IdAzienda", "Vigneto", "N"], ["numero", "testo", "numero"], [[425, "Vigneto A", 40m]]);
        var file = ExcelRilievi.Crea(new EsportazioneConcimazioniDto("Azienda A", 2026,
            [new("VIGNETI", tabella), new("VIGNE", tabella), new("AZIENDE", tabella)]));
        using var libro = new ClosedXML.Excel.XLWorkbook(new MemoryStream(file));
        Assert.Equal(["VIGNETI", "VIGNE", "AZIENDE"], libro.Worksheets.Select(f => f.Name));
        Assert.Equal("_IdAzienda", libro.Worksheet("VIGNETI").Cell(1, 1).GetString());
        Assert.Equal("Vigneto A", libro.Worksheet("VIGNE").Cell(2, 2).GetString());
        Assert.Equal("Export Rilievi azienda Azienda A.xlsx", ExcelRilievi.NomeFile("Azienda A"));
    }

    [Fact]
    public void ChiaveDoppia_ComandiSql()
    {
        var tabella = TabelleScrivibili.UnitaBaseConcimazione;
        var chiave = new ChiaveDoppia(2026, 4250);
        Assert.Equal("SELECT * FROM dbo.[UnitaBaseConcimazione] WHERE [Anno] = @chiave AND [IdVigneto] = @chiave2;",
            ComandiScrittura.Leggi(tabella, chiave).Testo);
        Assert.Equal("UPDATE dbo.[UnitaBaseConcimazione] SET [N] = @v0 WHERE [Anno] = @chiave AND [IdVigneto] = @chiave2; SELECT @@ROWCOUNT;",
            ComandiScrittura.Aggiorna(tabella, chiave, new Dictionary<string, object?> { ["N"] = 1 }).Testo);
        Assert.Equal("SELECT COUNT(*) FROM dbo.[UnitaBaseConcimazione] WHERE [IdTarget] = @u0 AND NOT ([Anno] = @esclusa AND [IdVigneto] = @esclusa2);",
            ComandiScrittura.Conta(tabella, new Dictionary<string, object?> { ["IdTarget"] = 1 }, chiave).Testo);
        Assert.Equal(chiave, tabella.ChiaveDa(new Dictionary<string, object?> { ["Anno"] = 2026, ["IdVigneto"] = 4250 }));
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Leggi(tabella, 2026));
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Aggiorna(tabella, chiave, new Dictionary<string, object?> { ["IdVigneto"] = 1 }));
        Assert.Throws<ArgumentException>(() => ComandiScrittura.Leggi(TabelleScrivibili.ConcimazioneReale, chiave));
    }
}

/// <summary>
/// Letture delle concimazioni sul <see cref="DbFinto"/>: per azienda quattro vigneti (azienda × 10 + 0…3, vigna = azienda, il +1
/// estirpato), target 1 «Spumante base» e 2 «Rosso strutturato», righe dalle tabelle UnitaBaseConcimazione e ConcimazioneReale.
/// </summary>
public sealed class LettureConcimazioniFinte(DbFinto db) : ILettureConcimazioniPortale
{
    private static readonly TargetEnologicoDto[] Target =
    [
        new(2, "Rosso strutturato", 50, 30, 80, 15, 0, 0, null, null, null, null, null, null, null, null),
        new(1, "Spumante base", 40, 20, 60, 10, 0, 0, 30, 50, null, null, null, null, null, null),
    ];

    public Task<IReadOnlyList<TargetEnologicoDto>> TargetAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TargetEnologicoDto>>(Target);

    public Task<IReadOnlyList<VocePortaleDto>> EpocheAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([new(1, "Fioritura"), new(2, "Pre germogliamento")]);

    public Task<IReadOnlyList<VocePortaleDto>> ModalitaAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VocePortaleDto>>([new(1, "Fertirrigazione"), new(2, "Spandiconcime")]);

    public Task<IReadOnlyList<VignetoConcimazioneDto>> VignetiAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VignetoConcimazioneDto>>(Enumerable.Range(0, 4)
            .Select(i => new VignetoConcimazioneDto(azienda * 10 + i, $"Vigneto {azienda * 10 + i}", azienda, "Vigna", 7, "Glera", i != 1))
            .ToList());

    public Task<IReadOnlyList<UnitaBaseDto>> UnitaBaseAsync(int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UnitaBaseDto>>(db.Tabella("UnitaBaseConcimazione").Values
            .Where(r => Equals(r["IdEnte"], azienda))
            .Select(r => new UnitaBaseDto((int)r["Anno"]!, (int)r["IdVigneto"]!, (int)r["IdTarget"]!, (int)r["N"]!, (int)r["P"]!,
                (int)r["K"]!, (int)r["MG"]!))
            .ToList());

    public async Task<UnitaConcimazioneDto> UnitaConcimazioneAsync(int anno, int azienda, CancellationToken ct = default) =>
        new((await UnitaBaseAsync(azienda, ct)).Where(u => u.Anno == anno)
            .Select(u => new RigaUnitaConcimazioneDto(u.IdVigneto, $"Vigneto {u.IdVigneto}", u.IdTarget, null, u.N, u.P, u.K, u.MG, 0, 0))
            .ToList(), [], []);

    public Task<IReadOnlyList<FattoreConcimazioneDto>> FattoriAsync(int anno, int azienda, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FattoreConcimazioneDto>>([]);

    public Task<ResocontoConcimazioniDto> ResocontoAsync(int anno, int azienda, int? target, int? epoca, CancellationToken ct = default) =>
        Task.FromResult(new ResocontoConcimazioniDto([], [], db.Tabella("ConcimazioneReale")
            .Where(r => Equals(r.Value["IdEnte"], azienda) && r.Value["Data"] is DateTime d && d.Year == anno)
            .Select(r => new ConcimazioneRealeRigaDto((int)r.Key, r.Value["IdTargetEnologico"] as int?, null, r.Value["IdVigneto"] as int?,
                null, r.Value["Data"] as DateTime?, r.Value["Concime"] as string, null, null, null, null, r.Value["N"] as decimal?, null,
                null, null, null, null, null, null, null, null, false))
            .ToList(), false));

    public Task<ConcimazioneRealeDto?> ConcimazioneRealeAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("ConcimazioneReale").TryGetValue(id, out var r)
            ? new ConcimazioneRealeDto(id, r["IdEnte"] as int?, r["IdVigneto"] as int?, null, r["Data"] as DateTime?,
                r["IdTargetEnologico"] as int?, r["Concime"] as string, r["IdEpocaConcimazione"] as int?, r["IdModalitaConcimazione"] as int?,
                r["N"] as decimal?, r["P2O5"] as decimal?, r["K2O"] as decimal?, r["MGO"] as decimal?, r["Dose"] as decimal?,
                r["Prezzo"] as decimal?)
            : null);

    public Task<int> ProssimoInserimentoAsync(CancellationToken ct = default) =>
        Task.FromResult(db.Tabella("ConcimazioneReale").Values.Select(r => r["IdInserimento"] as int? ?? 0).DefaultIfEmpty(0).Max() + 1);

    public Task<IReadOnlyList<(string Foglio, TabellaPortaleDto Tabella)>> EsportaUnitaAsync(string lingua, int anno, int azienda,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<(string, TabellaPortaleDto)>>([]);
}
