using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Tematismi;
using Xunit;
using IndiceWeb = ProgettoMappe.Web.Services.IndiceBigotDto;

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
    [InlineData("SCONOSCIUTA")]
    public void Categoria_mancante_o_sconosciuta_e_nessun_dato(string? vigoria)
    {
        Assert.Null(Classe("vigoria", Riga(vigoria: vigoria)));
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

        var classi = TematismiBigot.ClassiPerVigneto(TematismiBigot.Trova("punteggio")!, righe, 2025);

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

    // --- Endpoint ---

    [Fact]
    public async Task Endpoint_restituisce_gli_indici_di_tutti_gli_anni_dei_vigneti_dell_azienda()
    {
        var controller = new IndiciBigotController(new InMemoryVignetiRepository(), new InMemoryIndiciBigotRepository());

        var risposta = Assert.IsType<IndiciBigotAziendaDto>(
            Assert.IsType<OkObjectResult>((await controller.GetIndiciBigot(1, CancellationToken.None)).Result).Value);

        Assert.True(risposta.Pronto);
        Assert.Equal([1, 2], risposta.Indici.Select(i => i.IdVigneto).Distinct());
        Assert.Equal([2024, 2025], risposta.Indici.Select(i => i.Anno).Distinct().Order());
    }

    [Fact]
    public async Task Endpoint_per_un_azienda_senza_vigneti_restituisce_una_lista_vuota()
    {
        var controller = new IndiciBigotController(new InMemoryVignetiRepository(), new InMemoryIndiciBigotRepository());

        var risposta = Assert.IsType<IndiciBigotAziendaDto>(
            Assert.IsType<OkObjectResult>((await controller.GetIndiciBigot(9999, CancellationToken.None)).Result).Value);

        Assert.True(risposta.Pronto);
        Assert.Empty(risposta.Indici);
    }
}
