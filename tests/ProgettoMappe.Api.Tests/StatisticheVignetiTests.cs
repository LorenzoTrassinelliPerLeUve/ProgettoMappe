using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Tematismi sulle viste statistiche di 4Grapes (rischio oidio/peronospora, combinato, vigoria,
/// potenziale idrico): classificazione, scale da configurazione, endpoint, lettura ed esportazione.
/// Tutto con dati finti.
/// </summary>
public class StatisticheVignetiTests
{
    private static readonly CatalogoTematismi Catalogo = CatalogoTematismi.Crea(new TematismiOptions(), NullLogger.Instance);

    private static Tematismo Tema(string id) => Catalogo.Trova(id)!;

    private static string EtichettaDi(string tema, IRigaVigneto riga) => Tema(tema).Classi[Tema(tema).Classifica(riga)!.Value].Etichetta;

    // --- Rischio combinato ---

    [Theory]
    [InlineData(3, 7, 7)]
    [InlineData(8, 2, 8)]
    [InlineData(null, 5, 5)]
    [InlineData(4, null, 4)]
    [InlineData(null, null, null)]
    public void Rischio_combinato_e_il_piu_alto_dei_due(int? oidio, int? peronospora, int? atteso) =>
        Assert.Equal((decimal?)atteso, RischioCombinato.Calcola(oidio, peronospora));

    [Fact]
    public void La_legenda_del_combinato_dice_che_la_formula_e_una_proposta()
    {
        Assert.Contains("proposta, da confermare col proprietario", Tema("rischio-combinato").Nota);
        Assert.Contains(RischioCombinato.Descrizione, Tema("rischio-combinato").Nota);
    }

    // --- Classi ---

    [Theory]
    [InlineData(1, "Basso")]
    [InlineData(3, "Basso")]
    [InlineData(4, "Medio")]
    [InlineData(5, "Medio")]
    [InlineData(6, "Alto")]
    [InlineData(8, "Molto alto")]
    [InlineData(9, "Molto alto")]
    public void Rischio_oidio_per_classi(int valore, string classe) =>
        Assert.Equal(classe, EtichettaDi("rischio-oidio", new RischioVignetoDto(2025, 1, valore, null)));

    [Fact]
    public void Rischio_combinato_usa_il_piu_alto_e_la_peronospora_la_sua_colonna()
    {
        var riga = new RischioVignetoDto(2025, 1, 3m, 7m);

        Assert.Equal("Alto", EtichettaDi("rischio-combinato", riga));
        Assert.Equal("Alto", EtichettaDi("rischio-peronospora", riga));
        Assert.Equal("Basso", EtichettaDi("rischio-oidio", riga));
    }

    [Fact]
    public void La_legenda_scrive_nome_e_soglie_di_ogni_classe()
    {
        Assert.Equal(["Basso: < 4", "Medio: 4 – 6", "Alto: 6 – 8", "Molto alto: ≥ 8"], Tema("rischio-oidio").Classi.Select(c => c.Testo));
        Assert.Equal("#d73027", Tema("rischio-oidio").Classi[^1].Colore);
        Assert.Equal(["< 50 mm", "50 – 100 mm", "100 – 150 mm", "150 – 200 mm", "≥ 200 mm"],
            Tema("potenziale-idrico").Classi.Select(c => c.Testo));
    }

    [Theory]
    [InlineData(1, "Molto bassa")]
    [InlineData(2, "Bassa")]
    [InlineData(5, "Media")]
    [InlineData(6, "Medio alta")]
    [InlineData(9, "Molto alta")]
    [InlineData(10, "Eccessiva")]
    public void Vigoria_con_le_classi_di_4Grapes(int valore, string classe) =>
        Assert.Equal(classe, EtichettaDi("vigoria-punteggio", new VigoriaVignetoDto(2025, 1, valore)));

    [Fact]
    public void Righe_di_un_altra_fonte_non_hanno_classe()
    {
        Assert.Null(Tema("rischio-oidio").Classifica(new VigoriaVignetoDto(2025, 1, 5m)));
        Assert.Null(Tema("vigoria-punteggio").Classifica(new RischioVignetoDto(2025, 1, 5m, 5m)));
    }

    [Fact]
    public void Gli_id_del_menu_sono_unici_e_i_nuovi_stanno_dopo_il_BIGOT()
    {
        Assert.Equal(Catalogo.Tutti.Count, Catalogo.Tutti.Select(t => t.Id).Distinct().Count());
        Assert.Equal(["rischio-oidio", "rischio-peronospora", "rischio-combinato", "vigoria-punteggio", "potenziale-idrico"],
            Catalogo.Statistiche.Select(t => t.Id));
        Assert.Equal(FonteTematismo.Bigot, Catalogo.Tutti[0].Fonte);
    }

    // --- Classificazione per anno ---

    [Fact]
    public void Classificazione_per_anno_distingue_nessun_dato_e_non_valutato()
    {
        IRigaVigneto[] righe =
        [
            new RischioVignetoDto(2025, 1, 7m, 2m),
            new RischioVignetoDto(2025, 2, null, null),
            new RischioVignetoDto(2024, 3, 9m, 9m),
        ];

        var esito = TematismiBigot.ClassiPerVigneto(Tema("rischio-oidio"), righe, 2025);

        Assert.Equal(2, esito.ClasseDi(1)); // Alto
        Assert.Equal(TematismiBigot.ClasseNessunDato, esito.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, esito.ClasseDi(3)); // solo nel 2024
        Assert.Equal(TematismiStatistiche.FuoriRischi, Tema("rischio-oidio").EtichettaFuoriPerimetro);
    }

    [Fact]
    public void Il_potenziale_idrico_non_ha_anno()
    {
        IRigaVigneto[] righe = [new PotenzialeIdricoVignetoDto(1, 182.4m), new PotenzialeIdricoVignetoDto(2, 30m)];

        var esito = TematismiBigot.ClassiPerVigneto(Tema("potenziale-idrico"), righe, null);

        Assert.True(Tema("potenziale-idrico").SenzaAnno);
        Assert.Equal(3, esito.ClasseDi(1)); // 150 – 200
        Assert.Equal(0, esito.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, esito.ClasseDi(3));
        Assert.Equal("182,4 mm", Tema("potenziale-idrico").TestoValore(righe[0]));
    }

    [Fact]
    public void Nel_confronto_un_rischio_che_scende_e_un_miglioramento()
    {
        var tema = Tema("rischio-peronospora");
        IRigaVigneto[] righe =
        [
            new RischioVignetoDto(2024, 1, null, 6m), new RischioVignetoDto(2025, 1, null, 4m),
            new RischioVignetoDto(2024, 2, null, 3m), new RischioVignetoDto(2025, 2, null, 5m),
        ];

        var regola = Catalogo.Regola(tema);
        var confronto = ConfrontoBigot.Classifica(tema, regola, righe, 2024, 2025);
        var classi = ConfrontoBigot.Classi(tema, regola);

        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, confronto.ClasseDi(1)); // −2
        Assert.Equal(ConfrontoBigot.ClasseMoltoPeggio, confronto.ClasseDi(2)); // +2
        Assert.Equal("Molto migliorato: −2 o più", classi[ConfrontoBigot.ClasseMoltoMeglio].Testo);
        Assert.Equal("#1b7837", classi[ConfrontoBigot.ClasseMoltoMeglio].Colore);
        Assert.Equal(ConfrontoBigot.EtichettaNonValutato, ConfrontoBigot.EtichettaSenzaRiga(tema));
    }

    [Fact]
    public void Nel_confronto_la_vigoria_migliora_avvicinandosi_all_equilibrio()
    {
        var tema = Tema("vigoria-punteggio");
        IRigaVigneto[] righe = [new VigoriaVignetoDto(2024, 1, 8m), new VigoriaVignetoDto(2025, 1, 6m)];

        Assert.Equal(ConfrontoBigot.ClasseMoltoMeglio, ConfrontoBigot.Classifica(tema, Catalogo.Regola(tema), righe, 2024, 2025).ClasseDi(1));
    }

    // --- Scheda ---

    [Fact]
    public void La_scheda_mostra_i_valori_dell_anno_e_il_potenziale_idrico()
    {
        var statistiche = new StatisticheAziendaDto(true,
            [new RischioVignetoDto(2025, 1, 3m, 7m), new RischioVignetoDto(2024, 1, 9m, 9m)],
            [new VigoriaVignetoDto(2025, 1, 5m)],
            [new PotenzialeIdricoVignetoDto(1, 120m)]);

        var righe = SchedaBigot.RigheStatistiche(Catalogo.Statistiche, statistiche, 1, 2025);

        Assert.Equal(["3", "7", "7", "5", "120 mm"], righe.Select(r => r.Valore.Testo));
        Assert.Equal("#d73027".Length, righe[0].Valore.Colore!.Length);
        Assert.Empty(SchedaBigot.RigheStatistiche(Catalogo.Statistiche, statistiche, 99, 2025));
    }

    // --- Configurazione ---

    [Fact]
    public void Le_scale_in_appsettings_del_Web_sono_valide()
    {
        var cartella = new DirectoryInfo(AppContext.BaseDirectory);
        while (cartella is not null && !File.Exists(Path.Combine(cartella.FullName, "ProgettoMappe.sln")))
        {
            cartella = cartella.Parent;
        }
        Assert.NotNull(cartella);

        var configurazione = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(cartella.FullName, "src", "ProgettoMappe.Web", "appsettings.json"))
            .Build();
        var opzioni = configurazione.GetSection(TematismiOptions.SectionName).Get<TematismiOptions>();

        Assert.NotNull(opzioni);
        foreach (var scala in new[] { opzioni.Rischio, opzioni.Vigoria, opzioni.PotenzialeIdrico })
        {
            Assert.NotNull(scala);
            Assert.Empty(TematismiStatistiche.Errori(scala));
        }
    }

    [Fact]
    public void Le_scale_della_configurazione_cambiano_soglie_colori_e_nomi()
    {
        var opzioni = new TematismiOptions
        {
            Rischio = new ScalaTematismoOptions { Soglie = [5m], Colori = ["#00ff00", "#ff0000"], Etichette = ["Basso", "Alto"] },
        };

        var tema = CatalogoTematismi.Crea(opzioni, NullLogger.Instance).Trova("rischio-oidio")!;

        Assert.Equal(["Basso: < 5", "Alto: ≥ 5"], tema.Classi.Select(c => c.Testo));
        Assert.Equal(1, tema.Classifica(new RischioVignetoDto(2025, 1, 6m, null)));
    }

    [Fact]
    public void Una_scala_non_valida_usa_quella_predefinita()
    {
        var opzioni = new TematismiOptions
        {
            Rischio = new ScalaTematismoOptions { Soglie = [6m, 4m], Colori = ["#00ff00", "rosso"] },
        };

        var tema = CatalogoTematismi.Crea(opzioni, NullLogger.Instance).Trova("rischio-oidio")!;

        Assert.Equal(TematismiStatistiche.RischioPredefinito.Colori, tema.Classi.Select(c => c.Colore));
        Assert.Equal(3, TematismiStatistiche.Errori(opzioni.Rischio).Count); // ordine, numero e forma dei colori
    }

    // --- Endpoint ---

    [Fact]
    public async Task Endpoint_restituisce_solo_i_vigneti_dell_azienda()
    {
        var controller = Controller(new InMemoryStatisticheVignetiRepository());

        var risposta = Valore(await controller.GetStatistiche(1, CancellationToken.None));

        Assert.True(risposta.Pronto);
        Assert.NotNull(risposta.Aggiornamento);
        Assert.Equal([1, 2], risposta.Rischi.Select(r => r.IdVigneto).Distinct());
        Assert.Equal([2024, 2025], risposta.Rischi.Select(r => r.Anno).Distinct().Order());
        Assert.Equal([1], risposta.PotenzialeIdrico.Select(r => r.IdVigneto));
        Assert.NotEmpty(risposta.Vigoria);
    }

    [Fact]
    public async Task Azienda_inesistente_o_dati_non_pronti_danno_liste_vuote()
    {
        var vuota = Valore(await Controller(new InMemoryStatisticheVignetiRepository()).GetStatistiche(9999, CancellationToken.None));
        var nonPronta = Valore(await Controller(new RepositoryFinto(false)).GetStatistiche(1, CancellationToken.None));

        Assert.True(vuota.Pronto);
        Assert.Empty(vuota.Rischi);
        Assert.False(nonPronta.Pronto);
        Assert.Empty(nonPronta.Rischi);
    }

    [Theory]
    [InlineData(true, 202)]
    [InlineData(false, 409)]
    public void Ricarica_risponde_202_se_parte_e_409_se_ce_n_e_gia_una_in_corso(bool parte, int codice)
    {
        var risultato = Assert.IsAssignableFrom<ObjectResult>(Controller(new RepositoryFinto(parte)).Ricarica(new VitaFinta()).Result);

        Assert.Equal(codice, risultato.StatusCode);
        var stato = Assert.IsType<StatoIndiciBigotDto>(risultato.Value);
        Assert.True(stato.InCaricamento);
    }

    // --- Lettura e cache ---

    [Fact]
    public void Le_righe_sporche_si_leggono_senza_eccezioni()
    {
        var rischio = CacheStatisticheVigneti.LeggiRischio(Riga(("Anno", 2025), ("IdVigneto", 10), ("Rischio_OIDIO", 4.00m), ("Rischio_PERONOSPORA", "alto")));
        var vigoria = CacheStatisticheVigneti.LeggiVigoria(Riga(("Anno", null), ("IdVigneto", 10), ("PunteggioVigoria", 5m)));
        var potenziale = CacheStatisticheVigneti.LeggiPotenzialeIdrico(Riga(("IdVigneto", 10), ("PotenzialeIdrico", 139.4m)));

        Assert.Equal(new RischioVigneto(2025, 10, 4m, null), rischio);
        Assert.Null(vigoria); // senza anno: si salta
        Assert.Equal(new PotenzialeIdricoVigneto(10, 139.4m), potenziale);
    }

    [Fact]
    public async Task Ricarico_fallito_lascia_l_ora_dell_errore_e_nessun_dato()
    {
        var servizi = new ServiceCollection().BuildServiceProvider();
        var cache = new CacheStatisticheVigneti(servizi.GetRequiredService<IServiceScopeFactory>(), NullLogger<CacheStatisticheVigneti>.Instance);

        Assert.True(cache.AvviaRicarico(CancellationToken.None));
        for (var i = 0; i < 100 && (cache.InCaricamento || cache.UltimoErrore is null); i++)
        {
            await Task.Delay(20);
        }

        Assert.False(cache.Pronta);
        Assert.Null(cache.Per([1]));
        Assert.NotNull(cache.Stato.UltimoErrore);
    }

    // --- Esportazione ---

    [Fact]
    public void L_esportazione_ha_rischi_vigoria_e_potenziale_idrico()
    {
        var vigneto = new Vigneto { Id = 1, AziendaId = 1, Nome = "Vigneto 1" };
        var esportato = new VignetoEsportato(vigneto, "Azienda", 2025, null,
            new RischioVigneto(2025, 1, 3m, 7m), new VigoriaVigneto(2025, 1, 5m), new PotenzialeIdricoVigneto(1, 120m));

        object? Campo(string nome) => EsportazioneVigneti.Campi.Single(c => c.Nome == nome).Valore(esportato);

        Assert.Equal(3m, Campo("ris_oidio"));
        Assert.Equal(7m, Campo("ris_perono"));
        Assert.Equal(7m, Campo("ris_comb"));
        Assert.Equal(5m, Campo("vigoria_pt"));
        Assert.Equal(120m, Campo("pot_idrico"));
        Assert.All(EsportazioneVigneti.Campi, c => Assert.True(c.Nome.Length <= 10, c.Nome));
    }

    private static StatisticheVignetiController Controller(IStatisticheVignetiRepository repository) =>
        new(CacheVignetiAziendeTests.Cache(), repository, PerimetroAziendeTests.Perimetro());

    private static StatisticheAziendaDto Valore(ActionResult<StatisticheAziendaDto> risultato) =>
        Assert.IsType<StatisticheAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

    private static IDataRecord Riga(params (string Colonna, object? Valore)[] valori)
    {
        var tabella = new DataTable();
        foreach (var (colonna, _) in valori)
        {
            tabella.Columns.Add(colonna, typeof(object));
        }

        tabella.Rows.Add(valori.Select(v => v.Valore ?? DBNull.Value).ToArray());
        var reader = tabella.CreateDataReader();
        reader.Read();
        return reader;
    }

    private sealed class RepositoryFinto(bool pronto) : IStatisticheVignetiRepository
    {
        public Task<StatisticheVigneti?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
            Task.FromResult<StatisticheVigneti?>(null);

        public StatoIndiciBigot Stato => new(false, true, null, null);

        public bool AvviaRicarico(CancellationToken stop) => pronto;
    }

    private sealed class VitaFinta : Microsoft.Extensions.Hosting.IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
