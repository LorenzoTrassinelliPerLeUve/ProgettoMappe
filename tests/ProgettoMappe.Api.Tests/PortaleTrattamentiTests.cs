using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Portale;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Sezione 11 «Trattamenti» in lettura (tappa 4a, task #269): la pagina giornaliera di TrattamentiAziendaliGiornalieri del
/// portale su dati finti e l'Api (permessi, scheda solo dei vigneti abbinati).
/// </summary>
public class PortaleTrattamentiTests
{
    private const int Po = CalcoloTrattamenti.PeronosporaOidio;
    private const int Altre = CalcoloTrattamenti.AltreAvversita;
    private static readonly DateOnly Giorno = new(2026, 6, 10);

    // Azienda 1 «Alfa»: vigneti 10 (rischio 1 ALTO), 11 (rischio 2 BASSO). Azienda 2 «Beta»: vigneto 20 (comune B, linea 6).
    private static readonly VignetoTrattamenti[] Vigneti =
    [
        new(10, "Zeta", 1, "Alfa", true, "R1", "Friuli", "P1", "Udine", "A", "Comune A", 5, "Linea 5", 7, "Glera", 1, "ALTO"),
        new(11, "Alba", 1, "Alfa", true, "R1", "Friuli", "P1", "Udine", "A", "Comune A", 5, "Linea 5", 8, "Merlot", 2, "BASSO"),
        new(20, "Gamma", 2, "Beta", true, "R1", "Friuli", "P2", "Gorizia", "B", "Comune B", 6, "Linea 6", 7, "Glera", 1, "ALTO"),
    ];

    private static readonly ProdottoTrattamento Pero = new(12, "Peronospora", "11111", "RAME", 2.5m);
    private static readonly ProdottoTrattamento Oidio = new(11, "Oidio", "22222", "ZOLFO", null);

    private static TrattamentoLetto T(int id, int vigneto, DateTime data, bool suggerito = true, bool effettivo = false,
        int tipo = Po, int? gruppoEnte = 1, int? gruppoRischio = 1, bool? esclusi = false, int? generatoDa = null,
        bool automatico = false, params ProdottoTrattamento[] prodotti) =>
        new(id, vigneto, data, automatico, suggerito, effettivo, tipo, tipo == Po ? "OidioPeronospora" : "AltreAmpelopatie",
            gruppoEnte, gruppoRischio, gruppoRischio == 1 ? "ALTO" : "BASSO", 5, "Linea 5", esclusi, generatoDa,
            prodotti.Length == 0 ? [Pero, Oidio] : prodotti);

    private static DatiTrattamenti Dati(params TrattamentoLetto[] trattamenti) =>
        new(Vigneti, trattamenti, [new GruppoAzienda(3, 1, "VTS FVG"), new GruppoAzienda(4, 2, "VTS Estero")],
            [new VignetoConsulente(50, 20, "Mario Rossi")]);

    private static readonly FiltriTrattamenti Tutti = new(Giorno);

    [Fact]
    public void Colonne_OttoGiorniDaTreGiorniPrima_TitoloAlPiuTre()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti, Dati(), null);

        Assert.Equal(new DateOnly(2026, 6, 7), r.Dal);
        Assert.Equal(new DateOnly(2026, 6, 13), r.Al);
        Assert.Equal(8, r.Giorni.Count);
        Assert.Equal(Giorno, r.Giorni[3]);
        Assert.Equal(new DateOnly(2026, 6, 14), r.Giorni[7]);
        // Tutti i vigneti e tutte le aziende filtrate, anche senza trattamenti.
        Assert.Equal(3, r.Vigneti.Count);
        Assert.Equal(2, r.Aziende.Count);
        Assert.All(r.Vigneti.SelectMany(v => v.Giorni), c => Assert.Equal(0, c.Conta));
    }

    [Fact]
    public void Vigneto_UnPulsantePerTrattamento_TestoAvversitaTooltipOraEProdotti()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti, Dati(T(1, 10, new DateTime(2026, 6, 10, 8, 30, 0))), null);

        var cella = r.Vigneti.Single(v => v.Id == 10).Giorni[3];
        var p = Assert.Single(cella.Pulsanti);
        Assert.Equal("Peronospora | Oidio", p.Testo);
        Assert.Equal(["Alle: 08:30", "", "RAME [2.5]", "ZOLFO"], p.Suggerimento);
        Assert.Equal("suggerito", p.Stato);
        // La riga col trattamento sale in cima, poi le altre per nome.
        Assert.Equal([10, 11, 20], r.Vigneti.Select(v => v.Id));
    }

    [Fact]
    public void Stati_ColoriDelPortale()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti, Dati(
            T(1, 10, new DateTime(2026, 6, 9), suggerito: false, effettivo: true),
            T(2, 10, new DateTime(2026, 6, 10), tipo: Altre),
            T(3, 10, new DateTime(2026, 6, 11), esclusi: true),
            T(4, 10, new DateTime(2026, 6, 12), automatico: true)), null);

        var riga = r.Vigneti.Single(v => v.Id == 10);
        Assert.Equal("effettivo", riga.Giorni[2].Pulsanti[0].Stato);
        Assert.Equal("suggerito-altre", riga.Giorni[3].Pulsanti[0].Stato);
        Assert.Equal(("ESCLUSI", "esclusi"), (riga.Giorni[4].Pulsanti[0].Testo, riga.Giorni[4].Pulsanti[0].Stato));
        Assert.Equal("automatico", riga.Giorni[5].Pulsanti[0].Stato);
        Assert.Equal(4, riga.Totale);
    }

    [Fact]
    public void SuggeritoGiaTrasformatoInEffettivo_Nascosto()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti, Dati(
            T(1, 10, new DateTime(2026, 6, 10, 8, 0, 0)),
            T(2, 10, new DateTime(2026, 6, 10, 9, 0, 0), suggerito: false, effettivo: true, generatoDa: 1)), null);

        var p = Assert.Single(r.Vigneti.Single(v => v.Id == 10).Giorni[3].Pulsanti);
        Assert.Equal(2, p.Id);
    }

    [Fact]
    public void Azienda_TrattamentiDelloStessoGruppoInUnPulsante_TestoRischioDelVignetoELinea()
    {
        var giorno = new DateTime(2026, 6, 10, 8, 0, 0);
        var r = CalcoloTrattamenti.Calcola(Tutti, Dati(
            T(1, 10, giorno), T(2, 11, giorno),
            T(3, 11, giorno, gruppoRischio: 2),
            T(4, 10, giorno, esclusi: null), T(5, 11, giorno, esclusi: null),
            T(6, 10, giorno, tipo: Altre)), null);

        var cella = r.Aziende.Single(a => a.Id == 1).Giorni[3];
        Assert.Equal(6, cella.Conta);
        // 1 e 2 uniti; 3 (altro rischio) a parte; 4 e 5 con Gruppo_Esclusi NULL mai uniti (come il portale); 6 altre avversità.
        // In ordine di rischio del gruppo: rischio 1 (1, 4, 5, 6), poi rischio 2 (3).
        Assert.Equal([1, 4, 5, 6, 3], cella.Pulsanti.Select(p => p.Id));
        Assert.Equal("ALTO Linea 5", cella.Pulsanti[0].Testo);
        Assert.Equal("BASSO Linea 5", cella.Pulsanti[4].Testo);
        Assert.Equal(["BASSO Linea 5"], cella.Pulsanti[4].Suggerimento);
        Assert.Equal("ALTRE AVV.", cella.Pulsanti[3].Testo);
    }

    [Fact]
    public void Filtri_RestringonoVignetiEVoci()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti with { Comune = "B" }, Dati(), null);

        Assert.Equal([20], r.Vigneti.Select(v => v.Id));
        Assert.Equal(["Beta"], r.Filtri.Aziende.Select(a => a.Nome));
        Assert.Equal(["VTS Estero"], r.Filtri.Gruppi.Select(g => g.Nome));
        Assert.Equal(["Mario Rossi"], r.Filtri.Consulenti.Select(c => c.Nome));

        Assert.Equal([10, 11], CalcoloTrattamenti.Calcola(Tutti with { Gruppo = 3 }, Dati(), null).Vigneti.Select(v => v.Id).Order());
        Assert.Equal([20], CalcoloTrattamenti.Calcola(Tutti with { Consulente = 50 }, Dati(), null).Vigneti.Select(v => v.Id));
        Assert.Equal([10, 20], CalcoloTrattamenti.Calcola(Tutti with { Rischio = 1 }, Dati(), null).Vigneti.Select(v => v.Id).Order());
    }

    [Fact]
    public void NonMaster_SoloVignetiAbbinati_SenzaConsulenti()
    {
        var r = CalcoloTrattamenti.Calcola(Tutti with { Consulente = 50 }, Dati(T(1, 20, new DateTime(2026, 6, 10))),
            new HashSet<int> { 10, 11 });

        Assert.Equal([10, 11], r.Vigneti.Select(v => v.Id).Order());
        Assert.Empty(r.Filtri.Consulenti);
        Assert.All(r.Vigneti.SelectMany(v => v.Giorni), c => Assert.Equal(0, c.Conta));
    }

    [Fact]
    public void TipoTrattamento_FiltraEVociDaiTrattamenti()
    {
        var dati = Dati(T(1, 10, new DateTime(2026, 6, 10)), T(2, 11, new DateTime(2026, 6, 10), tipo: Altre));

        Assert.Equal(["100", "200"], CalcoloTrattamenti.Calcola(Tutti, dati, null).Filtri.TipiTrattamento.Select(t => t.Id));
        var r = CalcoloTrattamenti.Calcola(Tutti with { TipoTrattamento = Altre }, dati, null);
        Assert.Equal(1, r.Vigneti.Sum(v => v.Totale));
    }

    [Fact]
    public void Automatici_CinqueGiorniDaOggi_EntiUnitiPerOraEAvversita()
    {
        var ora = new DateTime(2026, 6, 10, 9, 0, 0);
        var dati = Dati(
            T(1, 10, ora, suggerito: false, automatico: true),
            T(2, 11, ora, suggerito: false, automatico: true),
            T(3, 11, ora, suggerito: false, automatico: true, prodotti: [Oidio]),
            T(4, 20, new DateTime(2026, 6, 9), suggerito: false, effettivo: true));

        var r = CalcoloTrattamenti.Automatici(Giorno, new FiltriTrattamenti(Giorno), dati, null);

        Assert.Equal(5, r.Giorni.Count);
        Assert.Equal(Giorno, r.Giorni[0]);
        Assert.Equal(new DateOnly(2026, 6, 13), r.Al);
        var alfa = r.Aziende.Single(a => a.Id == 1).Giorni[0];
        Assert.Equal([1, 3], alfa.Pulsanti.Select(p => p.Id)); // 1 e 2 stessa ora e stesse avversità: uno solo
        Assert.All(alfa.Pulsanti, p => Assert.Equal("automatico", p.Stato));
        Assert.Equal(0, r.Vigneti.Single(v => v.Id == 20).Totale); // il trattamento di ieri non c'è
    }

    [Fact]
    public async Task Api_Automatici()
    {
        var r = await Client(1).GetFromJsonAsync<TrattamentiGiornalieriDto>("api/portale/trattamenti/automatici");
        Assert.Equal(1, r!.Vigneti.Single(v => v.Id == 10).Totale);
    }

    [Fact]
    public void PiuDi500Righe_ContaLeAltre()
    {
        var molti = Enumerable.Range(1, 520)
            .Select(i => new VignetoTrattamenti(i, $"V{i:000}", 1, "Alfa", true, "R1", "Friuli", "P1", "Udine", "A", "Comune A", 5, "L", null, null, null, null))
            .ToList();
        var r = CalcoloTrattamenti.Calcola(Tutti, new DatiTrattamenti(molti, [], [], []), null);

        Assert.Equal(500, r.Vigneti.Count);
        Assert.Equal(20, r.AltriVigneti);
    }

    // ---- Api ----

    private sealed class PermessiTrattamenti : IPermessiRepository
    {
        public Task<PermessiUtente?> GetPermessiAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<PermessiUtente?>(idUtente switch
            {
                1 => new(1, true, false, true, false, true, null, new HashSet<string> { "TRATTAMENTI" }),
                2 => new(2, true, false, false, false, false, null, new HashSet<string> { "TRATTAMENTI_AZIENDALI" }),
                3 => new(3, true, false, false, false, false, null, new HashSet<string>()),
                4 => new(4, true, false, false, false, false, SezioniPortale.AppSardegna, new HashSet<string>()),
                _ => null,
            });
    }

    private sealed class LettureFinte : ILettureTrattamenti
    {
        public Task<DatiTrattamenti> LeggiAsync(DateTime dal, bool conConsulenti, CancellationToken ct = default) =>
            Task.FromResult(Dati(T(1, 10, new DateTime(2026, 6, 10))));

        public Task<DatiTrattamenti> LeggiAutomaticiAsync(DateTime dal, CancellationToken ct = default) =>
            Task.FromResult(Dati(T(2, 10, dal.AddHours(9), suggerito: false, automatico: true)));

        public Task<IReadOnlySet<int>> VignetiUtenteAsync(int idUtente, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<int>>(new HashSet<int> { 20 });

        public Task<(SchedaTrattamentoDto Scheda, int IdVigneto)?> SchedaAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<(SchedaTrattamentoDto, int)?>(id == 1
                ? (new SchedaTrattamentoDto(1, new DateTime(2026, 6, 10), "suggerito", true, Po, "OidioPeronospora", "Alfa", "ALTO",
                    "Linea 5", true, false, null, null, null, null, null, "65 - Fioritura", null, "OLD", null, [], []), 10)
                : null);

        public Task<IReadOnlyList<VignetoTabella>> VignetiTabelleAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<VignetoTabella>>(PortaleTabelleTrattamentiTests.Vigneti);

        public Task<IReadOnlyList<GruppoAzienda>> GruppiAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GruppoAzienda>>([]);

        public Task<DatiPressione> PressioneAsync(DateTime giorno, CancellationToken ct = default) =>
            Task.FromResult(new DatiPressione([new ControlloVigneto(10, 12, 1, 20, 10)], new Dictionary<int, int>(), []));

        public Task<IReadOnlyList<MeteoTabella>> MeteoAsync(DateTime dal, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MeteoTabella>>([]);

        public Task<IReadOnlyList<ProtezioneTabella>> ProtezioneAsync(DateTime dal, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProtezioneTabella>>([]);
    }

    [Fact]
    public async Task Api_Tabelle_PressioneProbabilitaProtezione()
    {
        var pressione = await Client(1).GetFromJsonAsync<PressioneTrattamentiDto>("api/portale/trattamenti/pressione?data=2026-06-10&avversita=12");
        Assert.NotEmpty(pressione!.Aziende);
        var vuota = await Client(1).GetFromJsonAsync<PressioneTrattamentiDto>("api/portale/trattamenti/pressione?data=2026-06-10");
        Assert.Empty(vuota!.Aziende);
        Assert.Equal(2, vuota.Avversita.Count);

        var storico = await Client(1).GetFromJsonAsync<ProbabilitaTrattamentiDto>("api/portale/trattamenti/probabilita?data=2020-06-10");
        Assert.True(storico!.Storico);
        Assert.Equal(HttpStatusCode.OK, (await Client(2).GetAsync("api/portale/trattamenti/protezione?data=2026-06-10")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(3).GetAsync("api/portale/trattamenti/protezione")).StatusCode);
    }

    private static HttpClient Client(int utente)
    {
        var api = new WebApplicationFactory<ProgettoMappe.Api.Servizi.ChiaveApiOptions>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<IPermessiRepository, PermessiTrattamenti>();
                s.AddSingleton<ILettureTrattamenti, LettureFinte>();
            }));
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add(PortaleControllerBase.IntestazioneUtente, utente.ToString());
        return client;
    }

    [Fact]
    public async Task Api_Giornalieri_ConUnaDelleAbilitazioniOAppSardegna()
    {
        var master = await Client(1).GetFromJsonAsync<TrattamentiGiornalieriDto>("api/portale/trattamenti/giornalieri?data=2026-06-10");
        Assert.Equal(3, master!.Vigneti.Count);
        Assert.Equal(1, master.Vigneti.Single(v => v.Id == 10).Totale);

        var consulente = await Client(2).GetFromJsonAsync<TrattamentiGiornalieriDto>("api/portale/trattamenti/giornalieri?data=2026-06-10");
        Assert.Equal([20], consulente!.Vigneti.Select(v => v.Id));

        Assert.Equal(HttpStatusCode.OK, (await Client(4).GetAsync("api/portale/trattamenti/giornalieri")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client(3).GetAsync("api/portale/trattamenti/giornalieri")).StatusCode);
    }

    [Fact]
    public async Task Api_Scheda_SoloDeiVignetiAbbinati()
    {
        var scheda = await Client(1).GetFromJsonAsync<SchedaTrattamentoDto>("api/portale/trattamenti/1");
        Assert.Equal("65 - Fioritura", scheda!.Bbch);

        // Il consulente ha solo il vigneto 20: il trattamento del vigneto 10 per lui non esiste.
        Assert.Equal(HttpStatusCode.NotFound, (await Client(2).GetAsync("api/portale/trattamenti/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client(1).GetAsync("api/portale/trattamenti/99")).StatusCode);
    }
}
