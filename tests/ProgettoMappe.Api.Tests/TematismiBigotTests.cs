using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Tematismi;
using Xunit;
using IndiceWeb = ProgettoMappe.Contracts.IndiceBigotDto;

namespace ProgettoMappe.Api.Tests;

public class TematismiBigotTests
{
    private static IndiceWeb Riga(int? numParametri = 9, int? punteggio = 80, string? vigoria = null, decimal? produzione = null,
        int? eta = null, string? morfologia = null, decimal? ampelopatie = null, int anno = 2025, int id = 1) =>
        new(anno, id, numParametri, punteggio, 223, vigoria, null, produzione, null, eta, morfologia, ampelopatie, null, null);

    private static int? Classe(string tematismo, IndiceWeb riga) => TematismiBigot.Trova(tematismo)!.Classifica(riga);

    private static string Etichetta(string tematismo, IndiceWeb riga) =>
        TematismiBigot.Trova(tematismo)!.Classi[Classe(tematismo, riga)!.Value].Etichetta;

    // --- Punteggio: solo vigneti completi ---

    [Fact]
    public void Punteggio_di_un_vigneto_completo_cade_nella_fascia_giusta()
    {
        Assert.Equal("70 – 85", Etichetta("punteggio", Riga(numParametri: 9, punteggio: 83)));
        Assert.Equal("≥ 85", Etichetta("punteggio", Riga(numParametri: 9, punteggio: 88)));
        Assert.Equal("< 40", Etichetta("punteggio", Riga(numParametri: 9, punteggio: 21)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(null)]
    public void Punteggio_di_un_vigneto_incompleto_va_nella_classe_Incompleto(int? numParametri)
    {
        var classe = TematismiBigot.Trova("punteggio")!.Classi[Classe("punteggio", Riga(numParametri: numParametri, punteggio: 83))!.Value];

        Assert.True(classe.Speciale);
        Assert.StartsWith("Incompleto", classe.Etichetta);
    }

    [Fact]
    public void Punteggio_nullo_e_nessun_dato()
    {
        Assert.Null(Classe("punteggio", Riga(punteggio: null)));
    }

    // --- Soglie numeriche ---

    [Fact]
    public void Un_valore_uguale_alla_soglia_cade_nella_classe_superiore()
    {
        Assert.Equal(0, TematismiBigot.ClasseNumerica(0.99m, [1m, 2m]));
        Assert.Equal(1, TematismiBigot.ClasseNumerica(1m, [1m, 2m]));
        Assert.Equal(2, TematismiBigot.ClasseNumerica(2m, [1m, 2m]));
    }

    [Fact]
    public void Produzione_usa_le_etichette_in_kg_per_ceppo_con_la_virgola()
    {
        Assert.Equal("1,5 – 2 kg/ceppo", Etichetta("produzione", Riga(produzione: 1.66m)));
    }

    [Fact]
    public void Ampelopatie_alte_sono_nella_classe_positiva_verde()
    {
        var tema = TematismiBigot.Trova("ampelopatie")!;
        var classe = tema.Classi[Classe("ampelopatie", Riga(ampelopatie: 99.9m))!.Value];

        Assert.Equal("≥ 99,5", classe.Etichetta);
        Assert.Equal("#1a9850", classe.Colore);
    }

    // --- Età: dati anomali ---

    [Theory]
    [InlineData(-7)]
    [InlineData(2026)]
    public void Eta_negativa_o_non_plausibile_e_un_dato_anomalo(int eta)
    {
        var tema = TematismiBigot.Trova("eta")!;
        var classe = tema.Classi[Classe("eta", Riga(eta: eta))!.Value];

        Assert.True(classe.Speciale);
        Assert.Equal("Dato anomalo", classe.Etichetta);
    }

    [Fact]
    public void Eta_plausibile_e_classificata_in_anni()
    {
        Assert.Equal("15 – 30 anni", Etichetta("eta", Riga(eta: 25)));
    }

    // --- Categorie ---

    [Fact]
    public void Le_categorie_seguono_l_ordine_naturale_ignorando_maiuscole_e_spazi()
    {
        Assert.Equal(0, Classe("vigoria", Riga(vigoria: "MOLTO BASSA")));
        Assert.Equal(7, Classe("vigoria", Riga(vigoria: " eccessiva ")));
        Assert.Equal("Medio compatto", Etichetta("morfologia", Riga(morfologia: "MEDIO COMPATTO")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Categoria_mancante_e_nessun_dato(string? vigoria)
    {
        Assert.Null(Classe("vigoria", Riga(vigoria: vigoria)));
    }

    [Fact]
    public void Categoria_non_prevista_va_in_Altro_con_un_colore_suo_e_viene_segnalata()
    {
        var tema = TematismiBigot.Trova("vigoria")!;
        var righe = new[] { Riga(id: 1, vigoria: " NUOVA "), Riga(id: 2, vigoria: "MEDIA"), Riga(id: 3, vigoria: null) };

        var esito = TematismiBigot.ClassiPerVigneto(tema, righe, 2025);
        var altro = tema.Classi[esito.Classi[1]];

        Assert.Equal(TematismiBigot.EtichettaAltro, altro.Etichetta);
        Assert.True(altro.Speciale);
        Assert.DoesNotContain(tema.Classi.Take(tema.Classi.Count - 1), c => c.Colore == altro.Colore);
        Assert.Equal(["NUOVA"], esito.ValoriNonPrevisti);
        Assert.False(esito.Classi.ContainsKey(3));
    }

    [Fact]
    public void Le_categorie_note_non_vengono_segnalate()
    {
        var esito = TematismiBigot.ClassiPerVigneto(TematismiBigot.Trova("morfologia")!, [Riga(morfologia: "molto spargolo")], 2025);

        Assert.Empty(esito.ValoriNonPrevisti);
    }

    [Fact]
    public void Le_categorie_scrivono_in_legenda_le_soglie_della_vista_4Grapes()
    {
        var vigoria = TematismiBigot.Trova("vigoria")!;

        Assert.Equal("Bassa: 1 – 2,5", vigoria.Classi[1].Testo);
        Assert.Equal("Eccessiva: ≥ 10", vigoria.Classi[7].Testo);
        Assert.Equal("Molto compatto: 1 – 2", TematismiBigot.Trova("morfologia")!.Classi[5].Testo);
        Assert.Equal("Assente: 0 – 1", TematismiBigot.Trova("regimeidrico")!.Classi[0].Testo);
        // Le classi numeriche hanno già l'intervallo come etichetta; "Altro" non ha soglia.
        Assert.Equal("< 1,5", TematismiBigot.Trova("sfe")!.Classi[0].Testo);
        Assert.Equal(TematismiBigot.EtichettaAltro, vigoria.Classi[^1].Testo);
    }

    // --- Nessun dato, fuori perimetro e filtro dalla legenda ---

    private static ClassificazioneTematismo VigoriaDi3Vigneti() => TematismiBigot.ClassiPerVigneto(
        TematismiBigot.Trova("vigoria")!,
        [Riga(id: 1, vigoria: "MEDIA"), Riga(id: 2, vigoria: null), Riga(id: 3, vigoria: "MEDIA", anno: 2024)],
        2025);

    [Fact]
    public void Nessun_dato_e_distinto_da_fuori_perimetro()
    {
        var esito = VigoriaDi3Vigneti();

        Assert.Equal(3, esito.ClasseDi(1));
        // Riga BIGOT nell'anno ma parametro vuoto: nessun dato.
        Assert.Equal(TematismiBigot.ClasseNessunDato, esito.ClasseDi(2));
        // Nessuna riga nell'anno (quella del 2024 non conta): fuori perimetro.
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, esito.ClasseDi(3));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, esito.ClasseDi(99));
    }

    [Fact]
    public void I_conteggi_della_legenda_contano_anche_nessun_dato_e_fuori_perimetro()
    {
        var conteggi = TematismiBigot.Conteggi(VigoriaDi3Vigneti(), [1, 2, 3, 4]);

        Assert.Equal(new Dictionary<int, int>
        {
            [3] = 1,
            [TematismiBigot.ClasseNessunDato] = 1,
            [TematismiBigot.ClasseFuoriPerimetro] = 2,
        }, conteggi);
    }

    [Fact]
    public void Il_filtro_della_legenda_lascia_solo_i_vigneti_della_voce_scelta()
    {
        var esito = VigoriaDi3Vigneti();
        int[] vigneti = [1, 2, 3, 4];

        Assert.Equal([1], TematismiBigot.FiltraPerClasse(vigneti, v => v, esito, 3));
        Assert.Equal([3, 4], TematismiBigot.FiltraPerClasse(vigneti, v => v, esito, TematismiBigot.ClasseFuoriPerimetro));
        Assert.Empty(TematismiBigot.FiltraPerClasse(vigneti, v => v, esito, 0));
        // Nessuna voce scelta, o tematismo spento: tutti.
        Assert.Equal(vigneti, TematismiBigot.FiltraPerClasse(vigneti, v => v, esito, null));
        Assert.Equal(vigneti, TematismiBigot.FiltraPerClasse(vigneti, v => v, null, 3));
    }

    // --- Definizioni e anno ---

    [Fact]
    public void Ogni_tematismo_ha_un_colore_per_classe_e_id_univoci()
    {
        Assert.Equal(TematismiBigot.Tutti.Count, TematismiBigot.Tutti.Select(t => t.Id).Distinct().Count());
        Assert.All(TematismiBigot.Tutti, t => Assert.All(t.Classi, c => Assert.Matches("^#[0-9a-f]{6}$", c.Colore)));
    }

    [Fact]
    public void Classi_per_vigneto_considerano_solo_l_anno_scelto_e_escludono_i_dati_mancanti()
    {
        var righe = new[]
        {
            Riga(id: 1, anno: 2025, punteggio: 83),
            Riga(id: 1, anno: 2026, punteggio: 20),
            Riga(id: 2, anno: 2025, punteggio: null),
        };

        var classi = TematismiBigot.ClassiPerVigneto(TematismiBigot.Trova("punteggio")!, righe, 2025).Classi;

        Assert.Equal(new Dictionary<int, int> { [1] = 3 }, classi);
    }

    [Theory]
    [InlineData(new[] { 2019, 2024, 2025, 2026 }, 2026, 2025)]
    [InlineData(new[] { 2026 }, 2026, 2026)]
    [InlineData(new[] { 2022, 2023 }, 2026, 2023)]
    public void Anno_predefinito_e_l_ultimo_concluso_o_altrimenti_l_ultimo_disponibile(int[] anni, int annoCorrente, int atteso)
    {
        Assert.Equal(atteso, TematismiBigot.AnnoPredefinito(anni, annoCorrente));
    }

    [Fact]
    public void Anno_predefinito_senza_dati_e_null()
    {
        Assert.Null(TematismiBigot.AnnoPredefinito([], 2026));
    }

    // --- Ora dell'aggiornamento ---

    [Fact]
    public void Etichetta_aggiornamento_di_oggi_mostra_solo_l_ora_italiana()
    {
        var aggiornato = new DateTimeOffset(2026, 10, 13, 8, 5, 0, TimeSpan.Zero); // 10:05 in Italia (ora legale)
        var adesso = new DateTimeOffset(2026, 10, 13, 15, 0, 0, TimeSpan.Zero);

        Assert.Equal("BIGOT aggiornato alle 10:05",
            TematismiBigot.EtichettaAggiornamento(aggiornato, adesso, TematismiBigot.FusoItalia));
    }

    [Fact]
    public void Etichetta_aggiornamento_di_un_altro_giorno_aggiunge_la_data()
    {
        var aggiornato = new DateTimeOffset(2026, 10, 12, 22, 30, 0, TimeSpan.Zero); // 00:30 del 13 in Italia
        var adesso = new DateTimeOffset(2026, 10, 14, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal("BIGOT aggiornato alle 00:30 del 13/10",
            TematismiBigot.EtichettaAggiornamento(aggiornato, adesso, TematismiBigot.FusoItalia));
    }

    // --- Endpoint ---

    [Fact]
    public async Task Endpoint_restituisce_gli_indici_di_tutti_gli_anni_dei_vigneti_dell_azienda()
    {
        var controller = new IndiciBigotController(CacheVignetiAziendeTests.Cache(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var risposta = Assert.IsType<IndiciBigotAziendaDto>(
            Assert.IsType<OkObjectResult>((await controller.GetIndiciBigot(1, CancellationToken.None)).Result).Value);

        Assert.True(risposta.Pronto);
        Assert.NotNull(risposta.Aggiornamento);
        Assert.Equal([1, 2], risposta.Indici.Select(i => i.IdVigneto).Distinct());
        Assert.Equal([2024, 2025], risposta.Indici.Select(i => i.Anno).Distinct().Order());
    }

    [Fact]
    public async Task Endpoint_per_un_azienda_senza_vigneti_restituisce_una_lista_vuota()
    {
        var controller = new IndiciBigotController(CacheVignetiAziendeTests.Cache(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var risposta = Assert.IsType<IndiciBigotAziendaDto>(
            Assert.IsType<OkObjectResult>((await controller.GetIndiciBigot(9999, CancellationToken.None)).Result).Value);

        Assert.True(risposta.Pronto);
        Assert.Empty(risposta.Indici);
    }

    [Fact]
    public void Stato_espone_pronto_e_ora_dell_ultimo_aggiornamento()
    {
        var controller = new IndiciBigotController(CacheVignetiAziendeTests.Cache(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var stato = Assert.IsType<StatoIndiciBigotDto>(Assert.IsType<OkObjectResult>(controller.GetStato().Result).Value);

        Assert.True(stato.Pronto);
        Assert.False(stato.InCaricamento);
        Assert.NotNull(stato.Aggiornamento);
        Assert.Null(stato.UltimoErrore);
    }

    [Theory]
    [InlineData(true, 202)]
    [InlineData(false, 409)]
    public void Ricarica_risponde_202_se_parte_e_409_se_ce_n_e_gia_una_in_corso(bool parte, int codice)
    {
        var controller = new IndiciBigotController(CacheVignetiAziendeTests.Cache(), new RicaricoFinto(parte), PerimetroAziendeTests.Perimetro());

        var risultato = Assert.IsAssignableFrom<ObjectResult>(controller.Ricarica(new VitaFinta()).Result);

        Assert.Equal(codice, risultato.StatusCode);
        Assert.IsType<StatoIndiciBigotDto>(risultato.Value);
    }

    [Fact]
    public async Task Ricarico_della_cache_fallito_lascia_l_ora_dell_errore_e_nessun_dato()
    {
        var servizi = new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider();
        var cache = new ProgettoMappe.Infrastructure.Repositories.FourGrapes.CacheIndiciBigot(
            servizi.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProgettoMappe.Infrastructure.Repositories.FourGrapes.CacheIndiciBigot>.Instance);

        Assert.True(cache.AvviaRicarico(CancellationToken.None));
        for (var i = 0; i < 100 && (cache.InCaricamento || cache.UltimoErrore is null); i++)
        {
            await Task.Delay(20);
        }

        Assert.False(cache.Pronta);
        Assert.False(cache.InCaricamento);
        Assert.NotNull(cache.UltimoErrore);
    }

    private sealed class RicaricoFinto(bool parte) : ProgettoMappe.Infrastructure.Repositories.IIndiciBigotRepository
    {
        public Task<IReadOnlyList<ProgettoMappe.Domain.Entities.IndiceBigotVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProgettoMappe.Domain.Entities.IndiceBigotVigneto>?>(null);

        public DateTimeOffset? UltimoAggiornamento => null;

        public ProgettoMappe.Infrastructure.Repositories.StatoIndiciBigot Stato => new(false, true, null, null);

        public bool AvviaRicarico(CancellationToken stop) => parte;
    }

    private sealed class VitaFinta : Microsoft.Extensions.Hosting.IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
