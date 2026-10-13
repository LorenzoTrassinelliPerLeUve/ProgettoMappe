using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Api.Avvisi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Web.Avvisi;
using ProgettoMappe.Web.Filtri;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Avvisi dalle regole semplici (task #70): regole, ordine, link, file e testi del Web. Dati finti.</summary>
public class AvvisiTests
{
    // In stagione (aprile–settembre): le regole di campagna producono avvisi.
    private static readonly DateOnly Oggi = new(2026, 7, 13);
    private static readonly DateTimeOffset Adesso = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static readonly RegolaAvvisoOptions NonRegistrati = new()
        { Id = "nessun-trattamento-registrato", Tipo = TipoRegolaAvviso.NessunTrattamentoRegistrato };

    private static readonly RegolaAvvisoOptions Peronospora = new()
        { Id = "peronospora-senza-trattamento", Tipo = TipoRegolaAvviso.RischioSenzaTrattamento, Malattia = MalattiaAvviso.Peronospora, Soglia = 4, Giorni = 10 };

    private static readonly RegolaAvvisoOptions Oidio = new()
        { Id = "oidio-senza-trattamento", Tipo = TipoRegolaAvviso.RischioSenzaTrattamento, Malattia = MalattiaAvviso.Oidio, Soglia = 4, Giorni = 10 };

    private static readonly RegolaAvvisoOptions Rilievi = new() { Id = "senza-rilievi", Tipo = TipoRegolaAvviso.SenzaRilievi, Giorni = 30 };

    private static readonly RegolaAvvisoOptions Stazioni = new() { Id = "stazione-ferma", Tipo = TipoRegolaAvviso.StazioneFerma, Giorni = 10 };

    private static readonly Azienda Collio = new() { Id = 1, Nome = "Collio" };
    private static readonly Azienda Brda = new() { Id = 2, Nome = "Brda" };

    private static DatiAvvisi Dati(
        IReadOnlyList<RischioVigneto>? rischi = null,
        IReadOnlyList<UltimoTrattamento>? trattamenti = null,
        IReadOnlyList<UltimoRilievo>? rilievi = null,
        IReadOnlyList<StazioneMeteo>? stazioni = null) =>
        new([Collio, Brda],
            [new VignetoAvvisi(10, 1, "Ronco"), new VignetoAvvisi(11, 1, "Bosco"), new VignetoAvvisi(20, 2, "Ponca")],
            rischi ?? [], trattamenti ?? [], rilievi ?? [], stazioni ?? []);

    private static UltimoTrattamento Trattato(int idVigneto, int giorniFa) =>
        new(1, idVigneto, Oggi.AddDays(-giorniFa).ToDateTime(new TimeOnly(8, 30)), [], [], []);

    private static UltimoRilievo Rilevato(int idVigneto, int giorniFa) =>
        new(idVigneto, Oggi.AddDays(-giorniFa).ToDateTime(new TimeOnly(9, 0)), "PERONOSPORA", "M. Rossi", false);

    private static AvvisiDto Calcola(DatiAvvisi dati, params RegolaAvvisoOptions[] regole) => RegoleAvvisi.Calcola(regole, dati, Oggi, Adesso);

    // --- Rischio e trattamenti ---

    [Fact]
    public void Rischio_alto_senza_trattamento_recente_fa_un_avviso_con_link_al_tema_dell_anno()
    {
        var avvisi = Calcola(Dati(rischi: [new RischioVigneto(2026, 10, 2m, 6m)], trattamenti: [Trattato(10, 23)]), Peronospora);

        var avviso = Assert.Single(avvisi.Avvisi);
        Assert.Equal("peronospora-senza-trattamento:vigneto-10", avviso.Chiave);
        Assert.Equal((1, "Collio", OggettoAvviso.Vigneto, 10, "Ronco"), (avviso.AziendaId, avviso.Azienda, avviso.Oggetto, avviso.IdVigneto, avviso.Nome));
        Assert.Equal("rischio peronospora 6 · ultimo trattamento 23 giorni fa", avviso.Valore);
        Assert.Equal(23, avviso.Giorni);
        Assert.Equal("mappa/1?vigneto=10&tema=rischio-peronospora&anno=2026", avviso.Link);
    }

    [Theory]
    [InlineData(3.99, 30, false)] // rischio sotto la soglia
    [InlineData(4, 30, true)]     // la soglia conta («da 4 in su»)
    [InlineData(9, 10, false)]    // trattato 10 giorni fa: non ancora «più di 10 giorni»
    [InlineData(9, 11, true)]
    public void Soglia_del_rischio_e_giorni_dal_trattamento(double rischio, int giorniFa, bool avviso)
    {
        var avvisi = Calcola(Dati(rischi: [new RischioVigneto(2026, 10, null, (decimal)rischio)], trattamenti: [Trattato(10, giorniFa)]), Peronospora);

        Assert.Equal(avviso, avvisi.Avvisi.Count == 1);
    }

    [Fact]
    public void Un_vigneto_mai_trattato_con_rischio_alto_e_un_avviso()
    {
        // Collio registra trattamenti (sul vigneto 10): il vigneto 11 mai trattato fa un avviso.
        var avviso = Assert.Single(Calcola(Dati(rischi: [new RischioVigneto(2026, 11, 5.5m, null)], trattamenti: [Trattato(10, 3)]), Oidio).Avvisi);

        Assert.Equal("rischio oidio 5,5 · mai trattato", avviso.Valore);
        Assert.Null(avviso.Giorni);
        Assert.Equal("mappa/1?vigneto=11&tema=rischio-oidio&anno=2026", avviso.Link);
    }

    [Fact]
    public void Conta_solo_il_rischio_dell_anno_corrente()
    {
        var avvisi = Calcola(Dati(rischi: [new RischioVigneto(2025, 10, null, 9m)]), Peronospora);

        Assert.Empty(avvisi.Avvisi);
    }

    [Fact]
    public void Oidio_e_peronospora_restano_avvisi_separati()
    {
        var avvisi = Calcola(Dati(rischi: [new RischioVigneto(2026, 10, 7m, 8m)], trattamenti: [Trattato(11, 3)]), Peronospora, Oidio);

        Assert.Equal(["peronospora-senza-trattamento", "oidio-senza-trattamento"], avvisi.Avvisi.Select(a => a.Regola));
    }

    [Fact]
    public void Due_trattamenti_conta_il_piu_recente()
    {
        var avvisi = Calcola(Dati(rischi: [new RischioVigneto(2026, 10, null, 6m)], trattamenti: [Trattato(10, 40), Trattato(10, 2)]), Peronospora);

        Assert.Empty(avvisi.Avvisi);
    }

    [Fact]
    public void Senza_rischi_o_trattamenti_la_regola_non_e_disponibile()
    {
        var dati = Dati() with { Trattamenti = null };

        var regola = Assert.Single(Calcola(dati, Peronospora).Regole);

        Assert.False(regola.Disponibile);
        Assert.Equal("Peronospora senza trattamento", regola.Breve);
    }

    // --- Aziende senza trattamenti registrati (task #82) ---

    [Fact]
    public void Un_azienda_senza_trattamenti_nell_anno_non_ha_avvisi_per_vigneto_ma_uno_solo_per_l_azienda()
    {
        // Brda (vigneto 20) non ha trattamenti nel 2026; Collio sì (vigneto 10).
        var dati = Dati(rischi: [new RischioVigneto(2026, 11, null, 7m), new RischioVigneto(2026, 20, null, 9m)], trattamenti: [Trattato(10, 2)]);

        var avvisi = Calcola(dati, Peronospora, NonRegistrati);

        Assert.Equal(["peronospora-senza-trattamento:vigneto-11"], avvisi.Avvisi.Where(a => a.Regola == Peronospora.Id).Select(a => a.Chiave));
        var azienda = Assert.Single(avvisi.Avvisi, a => a.Regola == NonRegistrati.Id);
        Assert.Equal(("nessun-trattamento-registrato:azienda-2", 2, OggettoAvviso.Azienda, "nessun trattamento registrato nel 2026"),
            (azienda.Chiave, azienda.AziendaId, azienda.Oggetto, azienda.Valore));
        Assert.Equal("mappa/2?tema=ultimo-trattamento", azienda.Link);
        Assert.Equal("Trattamenti non registrati", avvisi.Regole[1].Breve);
    }

    [Fact]
    public void Trattamenti_solo_degli_anni_prima_contano_come_nessun_trattamento()
    {
        var vecchio = Trattato(20, 0) with { Data = new DateTime(2025, 8, 1) };
        var dati = Dati(rischi: [new RischioVigneto(2026, 20, null, 9m)], trattamenti: [vecchio, Trattato(10, 2)]);

        Assert.Equal([2], RegoleAvvisi.AziendeSenzaTrattamenti(dati.Vigneti, dati.Trattamenti!, Oggi));
        Assert.Empty(Calcola(dati, Peronospora).Avvisi);
        Assert.Equal(["nessun-trattamento-registrato:azienda-2"], Calcola(dati, NonRegistrati).Avvisi.Select(a => a.Chiave));
    }

    [Fact]
    public void Senza_trattamenti_leggibili_anche_la_regola_per_azienda_non_e_disponibile()
    {
        var regola = Assert.Single(Calcola(Dati() with { Trattamenti = null }, NonRegistrati).Regole);

        Assert.False(regola.Disponibile);
    }

    // --- Stagione (task #82) ---

    [Fact]
    public void Fuori_stagione_le_regole_di_campagna_sono_ferme_e_le_stazioni_no()
    {
        var ottobre = new DateOnly(2026, 10, 13);
        var dati = Dati(rischi: [new RischioVigneto(2026, 11, null, 9m)], trattamenti: [Trattato(10, 200)], rilievi: [],
            stazioni: [new StazioneMeteo(1, 10, 3, "S", null, null, new DateTime(2026, 9, 1))]);

        var avvisi = RegoleAvvisi.Calcola([Peronospora, NonRegistrati, Rilievi, Stazioni], dati, ottobre, Adesso);

        Assert.Equal(["stazione-ferma:stazione-3"], avvisi.Avvisi.Select(a => a.Chiave));
        Assert.Equal(["Regola ferma fino ad aprile", "Regola ferma fino ad aprile", "Regola ferma fino ad aprile", null],
            avvisi.Regole.Select(r => r.Ferma));
        Assert.All(avvisi.Regole, r => Assert.True(r.Disponibile));
        Assert.Equal("Senza rilievi: regola ferma fino ad aprile", ElencoAvvisi.Ferme(avvisi)[2]);
    }

    [Fact]
    public void Fuori_stagione_i_dati_mancanti_non_rendono_la_regola_non_disponibile()
    {
        var avvisi = RegoleAvvisi.Calcola([Rilievi], Dati() with { Rilievi = null }, new DateOnly(2026, 1, 5), Adesso);

        Assert.True(Assert.Single(avvisi.Regole).Disponibile);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(9, true)]
    [InlineData(3, false)]
    [InlineData(10, false)]
    public void La_stagione_predefinita_va_da_aprile_a_settembre(int mese, bool attiva)
    {
        Assert.Equal(attiva, RegoleAvvisi.InStagione(Rilievi, new DateOnly(2026, mese, 1)));
        Assert.True(RegoleAvvisi.InStagione(Stazioni, new DateOnly(2026, mese, 1)));
    }

    [Fact]
    public void I_mesi_configurati_decidono_la_stagione_e_il_mese_di_ripresa()
    {
        var estate = new RegolaAvvisoOptions { Id = "estate", Tipo = TipoRegolaAvviso.SenzaRilievi, Giorni = 30, Mesi = [6, 7] };
        var inverno = new RegolaAvvisoOptions { Id = "inverno", Tipo = TipoRegolaAvviso.SenzaRilievi, Giorni = 30, Mesi = [12, 1] };

        Assert.Equal("Regola ferma fino a giugno", RegoleAvvisi.Ferma(estate, new DateOnly(2026, 8, 1)));
        Assert.Equal("Regola ferma fino a dicembre", RegoleAvvisi.Ferma(inverno, new DateOnly(2026, 2, 1)));
        Assert.Null(RegoleAvvisi.Ferma(inverno, new DateOnly(2026, 1, 31)));
        Assert.Equal("Regola ferma fino ad agosto",
            RegoleAvvisi.Ferma(new RegolaAvvisoOptions { Id = "a", Mesi = [8] }, new DateOnly(2026, 9, 1)));
    }

    // --- Rilievi ---

    [Fact]
    public void Vigneti_senza_rilievi_da_piu_di_30_giorni_o_mai_rilevati()
    {
        var avvisi = Calcola(Dati(rilievi: [Rilevato(10, 30), Rilevato(11, 31)]), Rilievi);

        Assert.Equal(
            [("senza-rilievi:vigneto-20", "mai rilevato"), ("senza-rilievi:vigneto-11", "ultimo rilievo 31 giorni fa")],
            avvisi.Avvisi.Select(a => (a.Chiave, a.Valore)).OrderBy(a => a.Chiave, StringComparer.Ordinal).Reverse());
        Assert.Equal("mappa/1?vigneto=11&tema=ultimo-rilievo", avvisi.Avvisi.Single(a => a.IdVigneto == 11).Link);
    }

    [Fact]
    public void Un_rilievo_con_data_futura_conta_come_oggi()
    {
        var avvisi = Calcola(Dati(rilievi: [Rilevato(10, -3), Rilevato(11, 0), Rilevato(20, 1)]), Rilievi);

        Assert.Empty(avvisi.Avvisi);
    }

    // --- Stazioni ---

    [Fact]
    public void Solo_le_stazioni_ferme_da_piu_di_10_giorni_mai_quelle_senza_dati()
    {
        var avvisi = Calcola(Dati(stazioni:
        [
            new StazioneMeteo(1, 10, 3, "Stazione Collio", 45.9, 13.5, Oggi.AddDays(-12).ToDateTime(TimeOnly.MinValue)),
            new StazioneMeteo(1, 11, 4, "Stazione nuova", null, null, Oggi.AddDays(-10).ToDateTime(TimeOnly.MinValue)),
            new StazioneMeteo(2, 20, null, null, null, null, null),
        ]), Stazioni);

        var avviso = Assert.Single(avvisi.Avvisi);
        Assert.Equal(("stazione-ferma:stazione-3", OggettoAvviso.Stazione, "Stazione Collio", "ultimo dato 12 giorni fa"),
            (avviso.Chiave, avviso.Oggetto, avviso.Nome, avviso.Valore));
        Assert.Equal("mappa/1?vigneto=10", avviso.Link);
    }

    [Fact]
    public void Una_stazione_senza_Id_ha_una_chiave_dal_suo_vigneto()
    {
        var avviso = Assert.Single(Calcola(Dati(stazioni: [new StazioneMeteo(2, 20, null, null, null, null, new DateTime(2026, 6, 1))]), Stazioni).Avvisi);

        Assert.Equal(("stazione-ferma:stazione-v20", "Stazione meteo"), (avviso.Chiave, avviso.Nome));
    }

    [Fact]
    public void Le_stazioni_di_aziende_fuori_elenco_non_contano()
    {
        var avvisi = Calcola(Dati(stazioni: [new StazioneMeteo(99, 990, 5, "Altrove", null, null, new DateTime(2026, 1, 1))]), Stazioni);

        Assert.Empty(avvisi.Avvisi);
    }

    // --- Insieme ---

    [Fact]
    public void Ordine_per_azienda_poi_regola_poi_i_piu_vecchi_e_conteggi_per_regola()
    {
        var dati = Dati(rischi: [new RischioVigneto(2026, 11, null, 7m)], trattamenti: [Trattato(11, 15)], rilievi: [Rilevato(10, 40), Rilevato(11, 50)]);

        var avvisi = Calcola(dati, Peronospora, Rilievi);

        Assert.Equal(
            ["Brda:senza-rilievi:vigneto-20", "Collio:peronospora-senza-trattamento:vigneto-11", "Collio:senza-rilievi:vigneto-11", "Collio:senza-rilievi:vigneto-10"],
            avvisi.Avvisi.Select(a => $"{a.Azienda}:{a.Chiave}"));
        Assert.Equal([1, 3], avvisi.Regole.Select(r => r.Conteggio));
        Assert.True(avvisi.Pronto);
        Assert.Equal(Oggi, avvisi.Giorno);
    }

    [Fact]
    public void Per_azienda_rifa_i_conteggi()
    {
        var tutti = Calcola(Dati(rilievi: []), Rilievi);

        var brda = RegoleAvvisi.PerAzienda(tutti, 2);

        Assert.Equal([20], brda.Avvisi.Select(a => a.IdVigneto));
        Assert.Equal(1, Assert.Single(brda.Regole).Conteggio);
    }

    [Fact]
    public void Le_regole_si_leggono_dalla_configurazione_e_quelle_sbagliate_si_saltano()
    {
        var configurazione = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Avvisi:Regole:0:Id"] = "peronospora",
            ["Avvisi:Regole:0:Tipo"] = "RischioSenzaTrattamento",
            ["Avvisi:Regole:0:Malattia"] = "Peronospora",
            ["Avvisi:Regole:0:Giorni"] = "10",
            ["Avvisi:Regole:1:Id"] = "peronospora",
            ["Avvisi:Regole:1:Tipo"] = "SenzaRilievi",
            ["Avvisi:Regole:2:Tipo"] = "SenzaRilievi",
            ["Avvisi:Regole:3:Id"] = "spenta",
            ["Avvisi:Regole:3:Tipo"] = "StazioneFerma",
            ["Avvisi:Regole:3:Attiva"] = "false",
            ["Avvisi:Regole:4:Id"] = "mesi-sbagliati",
            ["Avvisi:Regole:4:Tipo"] = "SenzaRilievi",
            ["Avvisi:Regole:4:Mesi:0"] = "13",
            ["Avvisi:Regole:5:Id"] = "estate",
            ["Avvisi:Regole:5:Tipo"] = "SenzaRilievi",
            ["Avvisi:Regole:5:Mesi:0"] = "6",
            ["Avvisi:Regole:5:Mesi:1"] = "7",
        }).Build();
        var opzioni = configurazione.GetSection(AvvisiOptions.Sezione).Get<AvvisiOptions>()!;
        var scartate = new List<string>();

        var valide = RegoleAvvisi.Valide(opzioni.Regole, scartate.Add);

        Assert.Equal(["peronospora", "estate"], valide.Select(r => r.Id));
        var regola = valide[0];
        Assert.Equal((4m, 10), (regola.Soglia, regola.Giorni));
        Assert.Null(regola.Mesi);
        Assert.Equal([4, 5, 6, 7, 8, 9], RegoleAvvisi.Mesi(regola));
        Assert.Equal([6, 7], RegoleAvvisi.Mesi(valide[1]));
        Assert.Equal("Rischio peronospora da 4 in su e nessun trattamento da più di 10 giorni", RegoleAvvisi.Nome(regola));
        Assert.Equal(["Id «peronospora» ripetuto", "senza Id", "«mesi-sbagliati»: mesi fuori da 1–12"], scartate);
    }

    [Fact]
    public void Il_file_ha_link_assoluti_e_un_riassunto_per_azienda()
    {
        var avvisi = Calcola(Dati(rilievi: []), Rilievi);

        var file = FileAvvisi.Da(avvisi, "https://prova-mappe.esempio.it/");

        Assert.Equal(3, file.Totale);
        Assert.Equal("https://prova-mappe.esempio.it/mappa/2?vigneto=20&tema=ultimo-rilievo", file.Avvisi[0].Link);
        Assert.Equal([("Brda", 1, "https://prova-mappe.esempio.it/mappa/2"), ("Collio", 2, "https://prova-mappe.esempio.it/mappa/1")],
            file.Aziende.Select(a => (a.Nome, a.Conteggio, a.Link)));
        Assert.Equal("mappa/2?vigneto=20&tema=ultimo-rilievo", FileAvvisi.Da(avvisi, "").Avvisi[0].Link);
    }

    [Fact]
    public async Task Il_calcolo_sui_dati_di_esempio_e_completo_e_scrive_il_file()
    {
        var cartella = Directory.CreateTempSubdirectory("avvisi-");
        try
        {
            var servizi = new ServiceCollection()
                .AddScoped<IAziendeRepository, InMemoryAziendeRepository>()
                .AddScoped<IGruppiRepository, InMemoryGruppiRepository>()
                .AddScoped<IStatisticheVignetiRepository, InMemoryStatisticheVignetiRepository>()
                .AddScoped(_ => new PerimetroAziende(new InMemoryGruppiRepository(), Options.Create(new PerimetroOptions())))
                .BuildServiceProvider();
            var opzioni = new AvvisiOptions { Regole = [Peronospora, Oidio, Rilievi, Stazioni], File = "avvisi/avvisi.json" };
            var calcolo = new CalcoloAvvisi(CacheVignetiAziendeTests.Cache(), servizi.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(opzioni), new Ambiente(cartella.FullName), NullLogger<CalcoloAvvisi>.Instance);

            Assert.False(calcolo.Ultimi.Pronto);
            Assert.True(await calcolo.AggiornaAsync(CancellationToken.None));

            Assert.True(calcolo.Ultimi.Pronto);
            Assert.All(calcolo.Ultimi.Regole, r => Assert.True(r.Disponibile));
            var file = await File.ReadAllTextAsync(Path.Combine(cartella.FullName, "avvisi", "avvisi.json"));
            Assert.Contains($"\"totale\": {calcolo.Ultimi.Avvisi.Count}", file);
            Assert.False(File.Exists(Path.Combine(cartella.FullName, "avvisi", "avvisi.json.tmp")));
        }
        finally
        {
            cartella.Delete(true);
        }
    }

    private sealed class Ambiente(string cartella) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = cartella;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    // --- Web ---

    [Fact]
    public void Il_link_dell_avviso_lo_legge_la_mappa()
    {
        var link = LinkMappa.Leggi(RegoleAvvisi.Link(1, 10, "rischio-oidio", 2026));

        Assert.Equal((10, "rischio-oidio", 2026), (link.Vigneto, link.Tema, link.Anno));
        Assert.Equal("mappa/1?vigneto=10&tema=rischio-oidio&anno=2026", link.Indirizzo(1));
    }

    [Fact]
    public void Un_azienda_aperta_mostra_12_schede_finche_non_si_chiede_tutto()
    {
        var avvisi = Enumerable.Range(1, 30)
            .Select(i => new AvvisoDto($"r:vigneto-{i}", "r", 1, "Collio", OggettoAvviso.Vigneto, i, $"V{i}", "", null, ""))
            .ToList();
        var gruppo = Assert.Single(ElencoAvvisi.PerAzienda(avvisi));

        Assert.Equal(12, ElencoAvvisi.Visibili(gruppo, false).Count());
        Assert.Equal(30, ElencoAvvisi.Visibili(gruppo, true).Count());
    }

    [Fact]
    public void Riassunto_e_gruppi_della_pagina_avvisi()
    {
        var avvisi = Calcola(Dati(rilievi: [], stazioni: [new StazioneMeteo(1, 10, 3, "S", null, null, new DateTime(2026, 6, 1))]), Rilievi, Stazioni);

        Assert.Equal("4 avvisi in 2 aziende", ElencoAvvisi.Riassunto(avvisi));
        Assert.Equal([("Brda", 1), ("Collio", 3)], ElencoAvvisi.PerAzienda(avvisi.Avvisi).Select(g => (g.Azienda, g.Avvisi.Count)));
        Assert.Equal([("Collio", 1)], ElencoAvvisi.PerAzienda(avvisi.Avvisi, "stazione-ferma").Select(g => (g.Azienda, g.Avvisi.Count)));
        Assert.Equal([("senza-rilievi", 2), ("stazione-ferma", 1)], ElencoAvvisi.PerAzienda(avvisi.Avvisi).Single(g => g.AziendaId == 1).Conteggi);
        Assert.Null(ElencoAvvisi.Riassunto(AvvisiDto.NonPronti));
        Assert.Equal("Nessun avviso", ElencoAvvisi.Riassunto(Calcola(Dati(rilievi: [Rilevato(10, 1), Rilevato(11, 1), Rilevato(20, 1)]), Rilievi)));
        Assert.Equal("Senza rilievi", ElencoAvvisi.NonDisponibili(Calcola(Dati() with { Rilievi = null }, Rilievi)));
    }
}
