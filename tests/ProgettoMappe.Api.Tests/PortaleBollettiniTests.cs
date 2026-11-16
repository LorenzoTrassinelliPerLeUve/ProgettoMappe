using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Api.Posta;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Scrittura;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Bollettini (sezione 12, task #280) sull'Api vera con 4Grapes e posta finti e i modelli HTML veri del portale: anteprima, invio
/// (numero dell'anno, una mail per lingua, LogOp «inviato», copia in archivio), posta che non parte, permessi, calendario.
/// </summary>
public class PortaleBollettiniTests
{
    private const int Master = PermessiFinti.Master, Consulente = PermessiFinti.Consulente, SenzaPermessi = PermessiFinti.SenzaPermessi;

    private sealed class Permessi : IPermessiRepository
    {
        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(idUtente switch
            {
                Master => new(Master, true, false, true, false, true, null, new HashSet<string> { "BOLLETTINI_AZIENDALI" }),
                Consulente => new(Consulente, true, false, false, false, false, null, new HashSet<string> { "BOLLETTINI_AZIENDALI" }),
                SenzaPermessi => new(SenzaPermessi, true, false, false, false, false, null, new HashSet<string>()),
                _ => null,
            });
    }

    private sealed class PostaFinta : IPosta
    {
        public List<MessaggioPosta> Inviati { get; } = [];
        public bool Rifiuta { get; set; }
        public bool Vera => true;

        public Task<EsitoPosta> InviaAsync(MessaggioPosta messaggio, CancellationToken ct = default)
        {
            if (Rifiuta) return Task.FromResult(new EsitoPosta(false, [], "Graph ha risposto 403"));
            Inviati.Add(messaggio);
            return Task.FromResult(new EsitoPosta(false, messaggio.Destinatari, null));
        }
    }

    /// <summary>Ente 425 con due trattamenti del gruppo ALTO/ALTO; 4 bollettini già mandati nel 2026; tre destinatari IT, EN, FR.</summary>
    private sealed class Letture(DbFinto db) : ILettureBollettini
    {
        public Task<LettiBollettino?> LeggiAsync(int ente, int tipo, int? linea, DateTime giorno, CancellationToken ct = default)
        {
            if (ente is not (425 or 426)) return Task.FromResult<LettiBollettino?>(null);
            IReadOnlyList<TrattamentoBollettino> t =
            [
                new(1, 4251, "Vigna Alta", "Merlot", 15000m, 1, false, 65, "Bagnare bene", "A piedi", [new(12, "RAME", 2m), new(11, "ZOLFO", 3m)]),
                new(2, 4252, "Vigna Bassa", "Cabernet", 5000m, 1, false, 65, null, "A piedi", [new(12, "RAME", 2m)]),
            ];
            return Task.FromResult<LettiBollettino?>(new LettiBollettino("Cantina Rossi & C.", "Linea 1", t, t,
                [new("Cormons", 2, 12.3m, 10m, 15m, 20m, 1, 3m, 11m, 16m, 21m)], new Dictionary<int, decimal> { [12] = 40m },
                new Dictionary<int, decimal> { [12] = 75m }, [(5, "Tignoletta")]));
        }

        public Task<int> UltimoNumeroAsync(int ente, int tipo, int anno, CancellationToken ct = default) =>
            Task.FromResult(4 + db.Tabella("Bollettino").Values.Count(b => Equals(b["IdEnte"], ente) && Equals(b["IdTipoBollettino"], tipo)
                && ((DateTime)b["GiornoRif"]!).Year == anno));

        public Task<IReadOnlyList<DestinatarioBollettino>> DestinatariAsync(int ente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DestinatarioBollettino>>(ente == 425
                ? [new("anna@esempio.it", "IT"), new("john@example.com", "EN"), new("pierre@exemple.fr", "FR")]
                : []);

        public Task<IReadOnlyList<BollettinoArchivioDto>> ArchivioAsync(IReadOnlyCollection<int>? aziende, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<BollettinoArchivioDto>>(db.Tabella("Bollettino").Values
                .Where(b => aziende is null || aziende.Contains((int)b["IdEnte"]!))
                .Select(b => new BollettinoArchivioDto((int)b["IdBollettino"]!, (int)b["IdEnte"]!, "Cantina", (int)b["IdTipoBollettino"]!,
                    (int)b["NumeroBollettino"]!, (DateTime)b["GiornoRif"]!, null, b["DataInvio"] as DateTime?, null, false)).ToList());
    }

    private static (WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions> Api, DbFinto Db, PostaFinta Posta, string Archivio) Avvia()
    {
        var (api, db) = PortaleDifesaTests.Avvia();
        var posta = new PostaFinta();
        var archivio = Path.Combine(Path.GetTempPath(), $"bollettini-{Guid.NewGuid():N}");
        return (api.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<IPermessiRepository, Permessi>();
            s.AddSingleton<ILettureBollettini>(new Letture(db));
            s.AddSingleton<IPosta>(posta);
            s.Configure<PostaOptions>(o => o.Archivio = archivio);
        })), db, posta, archivio);
    }

    private static readonly RichiestaBollettinoDto Richiesta = new(425, 100, 1, new DateOnly(2026, 6, 10));

    [Fact]
    public async Task Anteprima_NumeroDellAnno_ModelliVeriDelPortale_DestinatariPerLingua()
    {
        var (api, _, posta, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);

        var risposta = await c.GetAsync("api/portale/bollettini/anteprima?azienda=425&tipo=100&linea=1&giorno=2026-06-10");
        Assert.True(risposta.IsSuccessStatusCode, await risposta.Content.ReadAsStringAsync());
        var a = await risposta.Content.ReadFromJsonAsync<AnteprimaBollettinoDto>();

        Assert.Equal(5, a!.Numero);
        Assert.Contains("Cantina Rossi &amp; C.", a.HtmlIt);
        Assert.Contains("Vigna Alta - Merlot (1,50 ha)", a.HtmlIt);
        Assert.Contains("RAME - 4,000", a.HtmlIt); // 2 l/ha × 2 ha
        Assert.DoesNotContain("{BLOCCO_AA}", a.HtmlIt);
        Assert.DoesNotContain("{AZIENDA}", a.HtmlEn);
        Assert.Equal(["anna@esempio.it"], a.DestinatariIt);
        Assert.Equal(["john@example.com", "pierre@exemple.fr"], a.DestinatariEn);
        Assert.Empty(posta.Inviati);
    }

    [Fact]
    public async Task Invia_RegistraIlBollettino_UnaMailPerLingua_LogOpInviato_CopiaInArchivio()
    {
        var (api, db, posta, archivio) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);

        var r = await c.PostAsJsonAsync("api/portale/bollettini/invia", Richiesta);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var e = (await r.Content.ReadFromJsonAsync<EsitoBollettinoDto>())!;

        Assert.Equal((5, false, false, 3), (e.Numero, e.Prova, e.PostaSimulata, e.Destinatari));
        var b = Assert.Single(db.Tabella("Bollettino").Values);
        Assert.Equal((425, 100, 5, 1, Consulente), ((int)b["IdEnte"]!, (int)b["IdTipoBollettino"]!, (int)b["NumeroBollettino"]!,
            (int)b["IdTipologiaLinea"]!, (int)b["IdUtente"]!));
        Assert.NotNull(b["DataInvio"]);
        Assert.Equal([("Avviso Trattamento", 1), ("Tratment Advice", 2)], posta.Inviati.Select(m => (m.Oggetto, m.Destinatari.Count)));
        var inviati = db.LogOp.Where(l => l.Operazione == RigaLogOp.Inviato).ToList();
        Assert.Equal(2, inviati.Count);
        Assert.Contains("anna@esempio.it", inviati[0].Json);
        Assert.Contains("\"Esito\":\"inviato\"", inviati[0].Json);
        Assert.Contains(db.LogOp, l => l.Operazione == RigaLogOp.Nuovo && l.Tabella == "Bollettino");

        // Copia in archivio, poi il prossimo numero dell'anno.
        var archivioDto = await c.GetFromJsonAsync<List<BollettinoArchivioDto>>("api/portale/bollettini/archivio");
        var riga = Assert.Single(archivioDto!);
        Assert.True(riga.Copia);
        var copia = await c.GetFromJsonAsync<string>($"api/portale/bollettini/archivio/{riga.IdBollettino}/html?lingua=EN");
        Assert.Contains("Cantina Rossi", copia);
        Assert.Equal(6, (await c.GetFromJsonAsync<AnteprimaBollettinoDto>(
            "api/portale/bollettini/anteprima?azienda=425&tipo=100&linea=1&giorno=2026-06-11"))!.Numero);
        Directory.Delete(archivio, true);
    }

    [Fact]
    public async Task PostaCheNonParte_NienteRegistrato_LogOpDellErrore()
    {
        var (api, db, posta, _) = Avvia();
        await using var _a = api;
        posta.Rifiuta = true;

        var r = await PortaleDifesaTests.Client(api, Master).PostAsJsonAsync("api/portale/bollettini/invia", Richiesta);

        Assert.Equal(HttpStatusCode.BadGateway, r.StatusCode);
        Assert.Contains("403", await PortaleDifesaTests.TitoloAsync(r));
        Assert.Empty(db.Tabella("Bollettino"));
        var log = Assert.Single(db.LogOp);
        Assert.Equal(RigaLogOp.Inviato, log.Operazione);
        Assert.Contains("\"Esito\":\"errore\"", log.Json);
    }

    [Fact]
    public async Task Controlli_LineaObbligatoria_AziendaAbbinata_NessunDestinatario_SenzaSezione()
    {
        var (api, db, _, _) = Avvia();
        await using var _a = api;
        var c = PortaleDifesaTests.Client(api, Consulente);

        var senzaLinea = await c.PostAsJsonAsync("api/portale/bollettini/invia", Richiesta with { IdLinea = null });
        Assert.Equal(HttpStatusCode.BadRequest, senzaLinea.StatusCode);
        Assert.Contains("tipologia linea", await PortaleDifesaTests.TitoloAsync(senzaLinea));
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("api/portale/bollettini/invia", Richiesta with { IdAzienda = 999 })).StatusCode);
        var nessuno = await PortaleDifesaTests.Client(api, Master).PostAsJsonAsync("api/portale/bollettini/invia",
            Richiesta with { IdAzienda = 426, Tipo = 200, IdLinea = null });
        Assert.Equal(HttpStatusCode.BadRequest, nessuno.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PortaleDifesaTests.Client(api, SenzaPermessi).GetAsync("api/portale/bollettini/archivio")).StatusCode);
        Assert.Empty(db.Tabella("Bollettino"));
    }

    [Fact]
    public void Calendario_PulsantiPerLineaEAltreAvversita_SuggeritiTrasformatiNascosti()
    {
        VignetoTrattamenti V(int id, int linea) => new(id, "V" + id, 425, "Cantina", true, "R", "Regione", "P", "Prov", "C", "Comune", linea,
            "Linea " + linea, null, null, 1, "ALTO / ALTO");
        TrattamentoLetto T(int id, int vigneto, int giorno, int tipo, int? generatoDa = null, bool effettivo = false) => new(id, vigneto,
            new DateTime(2026, 6, giorno, 8, 0, 0), false, !effettivo, effettivo, tipo, null, 425, 1, null, null, null, false, generatoDa, []);
        var dati = new DatiTrattamenti([V(1, 1), V(2, 2), V(3, 1)],
            [T(10, 1, 10, 100), T(11, 3, 10, 100), T(12, 2, 10, 100), T(13, 1, 10, 200), T(14, 2, 11, 100), T(15, 2, 11, 100, 14, true)], [], []);

        var cal = CalcoloBollettini.Calendario(new FiltriTrattamenti(new DateOnly(2026, 6, 10)), 0, dati, null);

        Assert.Equal(7, cal.Giorni.Count);
        var riga = Assert.Single(cal.Aziende);
        Assert.Equal(["Linea 1", "Linea 2", "ALTRE AVV."], riga.Giorni[3].Select(p => p.Testo));
        Assert.Equal(["Linea 2"], riga.Giorni[4].Select(p => p.Testo)); // il suggerito 14 è diventato il 15: un pulsante
        Assert.Equal(["Linea 1", "Linea 2"],
            CalcoloBollettini.Calendario(new FiltriTrattamenti(new DateOnly(2026, 6, 10)), 100, dati, null).Aziende[0].Giorni[3].Select(p => p.Testo));
    }
}
