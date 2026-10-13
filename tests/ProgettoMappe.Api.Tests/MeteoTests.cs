using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Filtri;
using ProgettoMappe.Web.Meteo;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Meteo per azienda (task #67): lettura delle righe, composizione (7 giorni, ieri, infezioni),
/// endpoint e cache, testi del cartellino e del pannello, ordine per pioggia. Tutto con dati finti.
/// </summary>
public class MeteoTests
{
    private static readonly DateOnly Oggi = new(2026, 10, 13);

    private static readonly AvversitaInfezione[] Avversita =
        [new(IMeteoRepository.Oidio, "Oidio", 30m), new(IMeteoRepository.Peronospora, "Peronospora", 30m)];

    // --- Lettura da 4Grapes ---

    [Fact]
    public void Un_giorno_della_vista_si_legge_con_pioggia_e_temperature()
    {
        var giorno = FourGrapesMeteoRepository.LeggiGiorno(Record(("IdEnte", 5), ("Data", new DateTime(2026, 10, 12)),
            ("PrecipitazioneMedia", 12.345678m), ("TemperaturaMinMedia", 8.5m), ("TemperaturaMaxMedia", null)));

        Assert.Equal(new MeteoGiornoAzienda(5, new DateOnly(2026, 10, 12), 12.345678m, 8.5m, null), giorno);
        Assert.Null(FourGrapesMeteoRepository.LeggiGiorno(Record(("IdEnte", 5), ("Data", null),
            ("PrecipitazioneMedia", 1m), ("TemperaturaMinMedia", null), ("TemperaturaMaxMedia", null))));
    }

    [Theory]
    [InlineData(34.5, true)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void Una_probabilita_fuori_da_0_100_si_salta(double percentuale, bool letta)
    {
        var riga = FourGrapesMeteoRepository.LeggiInfezione(Record(("IdEnte", 5), ("IdVigneto", 7),
            ("IdAvversitaDaTrattare", 12), ("PercInfezione", (decimal)percentuale)));

        Assert.Equal(letta, riga is not null);
    }

    [Fact]
    public void Il_nome_dell_avversita_prende_l_iniziale_maiuscola_e_una_soglia_assurda_vale_assente()
    {
        Assert.Equal(new AvversitaInfezione(11, "Oidio", 30m),
            FourGrapesMeteoRepository.LeggiAvversita(Record(("Id", 11), ("Descrizione", " OIDIO "), ("SogliaProbabilitaInfezione", 30m))));
        Assert.Equal(new AvversitaInfezione(12, "Peronospora", null),
            FourGrapesMeteoRepository.LeggiAvversita(Record(("Id", 12), ("Descrizione", null), ("SogliaProbabilitaInfezione", 0m))));
    }

    // --- Composizione ---

    [Fact]
    public void Sette_giorni_prima_di_oggi_dal_piu_vecchio_con_i_buchi_vuoti()
    {
        var letto = new MeteoLetto(Oggi,
        [
            new(1, Oggi.AddDays(-1), 5m, 8m, 19m),
            new(1, Oggi.AddDays(-3), 0m, 7m, 18m),
            new(1, Oggi.AddDays(-7), 30.5m, null, null),
            new(1, Oggi, 99m, 1m, 2m), // oggi è previsione: non entra
            new(2, Oggi.AddDays(-1), 50m, 1m, 2m),
        ], [], Avversita);

        var meteo = MeteoController.Componi(1, letto);

        Assert.Equal(Enumerable.Range(1, 7).Reverse().Select(i => Oggi.AddDays(-i)), meteo.Giorni.Select(g => g.Data));
        Assert.Equal([30.5m, null, null, null, 0m, null, 5m], meteo.Giorni.Select(g => g.Pioggia));
        Assert.Equal(35.5m, meteo.Pioggia);
        Assert.Equal(3, meteo.GiorniConPioggia);
        Assert.Equal(new MeteoGiornoDto(Oggi.AddDays(-1), 5m, 8m, 19m), meteo.Ieri);
        Assert.True(meteo.ConMeteo);
    }

    [Fact]
    public void Senza_dati_la_pioggia_e_null_non_zero()
    {
        var meteo = MeteoController.Componi(3, new MeteoLetto(Oggi, [new(1, Oggi.AddDays(-1), 5m, 8m, 19m)], [], Avversita));

        Assert.Null(meteo.Pioggia);
        Assert.Null(meteo.Ieri);
        Assert.False(meteo.ConMeteo);
        Assert.Equal(7, meteo.Giorni.Count);
        Assert.All(meteo.Giorni, g => Assert.Null(g.Pioggia));
    }

    [Fact]
    public void Una_settimana_asciutta_fa_zero_vero_e_una_pioggia_negativa_e_un_dato_mancante()
    {
        var meteo = MeteoController.Componi(1, new MeteoLetto(Oggi,
            [new(1, Oggi.AddDays(-1), 0m, 8m, 19m), new(1, Oggi.AddDays(-2), -3m, 8m, 19m)], [], Avversita));

        Assert.Equal(0m, meteo.Pioggia);
        Assert.Equal(1, meteo.GiorniConPioggia);
    }

    [Fact]
    public void Infezioni_separate_con_massimo_e_vigneti_sopra_soglia()
    {
        var meteo = MeteoController.Componi(1, new MeteoLetto(Oggi, [],
        [
            new(1, 10, IMeteoRepository.Oidio, 12m),
            new(1, 11, IMeteoRepository.Oidio, 29.9m),
            new(1, 10, IMeteoRepository.Peronospora, 30m),
            new(1, 11, IMeteoRepository.Peronospora, 45m),
            new(1, 12, IMeteoRepository.Peronospora, 5m),
            new(2, 20, IMeteoRepository.Peronospora, 90m),
        ], Avversita));

        Assert.Equal(
        [
            new InfezioneAziendaDto(IMeteoRepository.Peronospora, "Peronospora", 45m, 3, 30m, 2),
            new InfezioneAziendaDto(IMeteoRepository.Oidio, "Oidio", 29.9m, 2, 30m, 0),
        ], meteo.Infezioni);
        Assert.Equal([true, false], meteo.Infezioni.Select(i => i.Alta));
        Assert.Null(meteo.Pioggia); // infezioni senza meteo: resta «nessun dato meteo»
    }

    [Fact]
    public void Senza_soglia_non_si_dice_alta_ne_bassa()
    {
        var meteo = MeteoController.Componi(1, new MeteoLetto(Oggi, [], [new(1, 10, IMeteoRepository.Oidio, 80m)], []));

        var oidio = Assert.Single(meteo.Infezioni);
        Assert.Equal("Oidio", oidio.Nome);
        Assert.Null(oidio.Alta);
    }

    [Theory]
    [InlineData("2026-10-12T21:59:00Z", "2026-10-12")] // 23:59 in Italia (ora legale)
    [InlineData("2026-10-12T22:00:00Z", "2026-10-13")] // mezzanotte in Italia
    public void Oggi_e_il_giorno_italiano(string adesso, string atteso) =>
        Assert.Equal(DateOnly.Parse(atteso), MeteoController.Oggi(DateTimeOffset.Parse(adesso)));

    // --- Endpoint e cache ---

    [Fact]
    public async Task L_endpoint_di_tutte_da_una_riga_per_azienda_del_perimetro()
    {
        var dto = Valore(await Controller().GetTutte(CancellationToken.None));

        Assert.Equal(Oggi, dto.Oggi);
        Assert.Equal([1, 2], dto.Aziende.Select(a => a.AziendaId));
        Assert.NotNull(dto.Aziende[0].Pioggia);
        Assert.Null(dto.Aziende[1].Pioggia); // l'azienda 2 di esempio non ha meteo
        Assert.NotNull(dto.Aggiornamento);
    }

    [Fact]
    public async Task Fuori_perimetro_nessun_dato_meteo()
    {
        var repository = new RepositoryContato();
        var controller = Controller(PerimetroAziendeTests.Perimetro(2), repository);

        var meteo = Valore(await controller.GetAzienda(1, CancellationToken.None));

        Assert.Null(meteo.Pioggia);
        Assert.Empty(meteo.Infezioni);
        Assert.Equal(0, repository.Letture);
    }

    [Fact]
    public async Task Pagina_iniziale_e_mappe_usano_una_lettura_sola()
    {
        var repository = new RepositoryContato();
        var controller = Controller(repository: repository);

        await controller.GetTutte(CancellationToken.None);
        var azienda1 = Valore(await controller.GetAzienda(1, CancellationToken.None));
        await controller.GetAzienda(2, CancellationToken.None);

        Assert.Equal(1, repository.Letture);
        Assert.Equal(Oggi, repository.Giorno);
        Assert.Equal(CacheVignetiAziende.GiorniMeteo, repository.Giorni);
        Assert.NotNull(azienda1.Pioggia);
        Assert.Equal(TimeSpan.FromMinutes(30), CacheVignetiAziende.DurataMeteo);
    }

    [Fact]
    public async Task Se_4Grapes_non_risponde_503_e_non_resta_in_cache()
    {
        var rotto = new RepositoryContato { Rotto = true };
        var controller = Controller(repository: rotto);

        Assert.Equal(503, Assert.IsType<StatusCodeResult>((await controller.GetTutte(CancellationToken.None)).Result).StatusCode);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>((await controller.GetAzienda(1, CancellationToken.None)).Result).StatusCode);

        rotto.Rotto = false;
        Assert.NotNull(Valore(await controller.GetAzienda(1, CancellationToken.None)).Pioggia);
    }

    // --- Testi della pagina iniziale e del pannello ---

    [Fact]
    public void Il_cartellino_dice_pioggia_e_infezioni_alte()
    {
        var meteo = Meteo([0m, 12.4m, 3m, 0m, 0m, 21.6m, 5.2m],
            new InfezioneAziendaDto(12, "Peronospora", 45m, 3, 30m, 2), new InfezioneAziendaDto(11, "Oidio", 31m, 3, 30m, 1));

        Assert.Equal("7 gg: 42 mm · peronospora alta · oidio alto", TestiMeteo.Cartellino(meteo));
    }

    [Fact]
    public void Il_cartellino_senza_meteo_dice_nessun_dato_e_un_buco_ha_l_asterisco()
    {
        Assert.Equal("nessun dato meteo", TestiMeteo.Cartellino(Meteo([null, null, null, null, null, null, null])));
        Assert.Equal("nessun dato meteo · peronospora alta",
            TestiMeteo.Cartellino(Meteo([null, null, null, null, null, null, null], new InfezioneAziendaDto(12, "Peronospora", 45m, 3, 30m, 1))));
        Assert.Equal("7 gg: 3,5 mm*", TestiMeteo.Cartellino(Meteo([1m, 2.5m, null, 0m, 0m, 0m, 0m], new InfezioneAziendaDto(12, "Peronospora", 5m, 3, 30m, 0))));
        Assert.Contains("6 giorni su 7", TestiMeteo.TitoloCartellino(Meteo([1m, 2.5m, null, 0m, 0m, 0m, 0m])));
    }

    [Fact]
    public void L_infezione_si_spiega_con_soglia_e_vigneti()
    {
        Assert.Equal("Peronospora: alta, sopra soglia in 2 vigneti su 3, al massimo 45% (soglia 30%)",
            TestiMeteo.Infezione(new InfezioneAziendaDto(12, "Peronospora", 45m, 3, 30m, 2)));
        Assert.Equal("Oidio: basso, al massimo 12% su 1 vigneto (soglia 30%)",
            TestiMeteo.Infezione(new InfezioneAziendaDto(11, "Oidio", 12m, 1, 30m, 0)));
        Assert.Equal("Oidio: al massimo 80% su 4 vigneti",
            TestiMeteo.Infezione(new InfezioneAziendaDto(11, "Oidio", 80m, 4, null, 0)));
    }

    [Fact]
    public void Le_barrette_seguono_la_pioggia_piu_alta_con_una_scala_minima()
    {
        var barre = TestiMeteo.Barre(Meteo([0m, 20m, 10m, null, 0.1m, 5m, 0m]));

        Assert.Equal([0, 100, 50, 0, 4, 25, 0], barre.Select(b => b.Altezza));
        Assert.Contains("nessun dato", barre[3].Titolo);
        Assert.Equal([0, 20, 0, 0, 4, 0, 0], TestiMeteo.Barre(Meteo([0m, 2m, 0m, 0m, 0.2m, 0m, 0m])).Select(b => b.Altezza));
    }

    [Fact]
    public void Temperature_di_ieri()
    {
        var giorni = Meteo([0m, 0m, 0m, 0m, 0m, 0m, 0m]).Giorni;
        var ieri = giorni[^1] with { TemperaturaMin = 8.3m, TemperaturaMax = 19m };

        Assert.Equal("Ieri: min 8,3 °C, max 19 °C", TestiMeteo.Temperature(new MeteoAziendaDto(1, giorni, 0m, ieri, [])));
        Assert.Null(TestiMeteo.Temperature(new MeteoAziendaDto(1, giorni, 0m, null, [])));
    }

    [Fact]
    public void Ordine_per_pioggia_dalla_piu_piovosa_senza_dato_in_fondo()
    {
        RiepilogoAziendaDto Azienda(int id, string nome) => new(id, nome, 1, 1m, 0, null, 0);
        var aziende = new[] { Azienda(1, "Alfa"), Azienda(2, "Beta"), Azienda(3, "Gamma"), Azienda(4, "Delta") };
        var pioggia = new Dictionary<int, decimal?> { [1] = 5m, [2] = null, [3] = 42m };

        var ordinate = ElencoAziende.Filtra(aziende, null, null, OrdineAziende.Pioggia, pioggia);

        Assert.Equal(["Gamma", "Alfa", "Beta", "Delta"], ordinate.Select(a => a.Nome));
    }

    // --- Aiuti ---

    private static MeteoAziendaDto Meteo(decimal?[] pioggia, params InfezioneAziendaDto[] infezioni)
    {
        var giorni = pioggia.Select((p, i) => new MeteoGiornoDto(Oggi.AddDays(i - 7), p, null, null)).ToList();
        var conDato = giorni.Where(g => g.Pioggia is not null).ToList();
        return new MeteoAziendaDto(1, giorni, conDato.Count == 0 ? null : conDato.Sum(g => g.Pioggia!.Value), null, infezioni);
    }

    private static MeteoController Controller(PerimetroAziende? perimetro = null, IMeteoRepository? repository = null) =>
        new(CacheVignetiAziendeTests.Cache(meteo: repository), perimetro ?? PerimetroAziendeTests.Perimetro(1),
            NullLogger<MeteoController>.Instance, new Orologio(new DateTimeOffset(2026, 10, 13, 9, 0, 0, TimeSpan.Zero)));

    private static T Valore<T>(ActionResult<T> risultato) =>
        Assert.IsType<T>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

    private static IDataRecord Record(params (string Colonna, object? Valore)[] valori)
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

    private sealed class Orologio(DateTimeOffset adesso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => adesso;
    }

    /// <summary>Meteo in-memory che conta le letture; <see cref="Rotto"/> = 4Grapes non risponde.</summary>
    private sealed class RepositoryContato : IMeteoRepository
    {
        public int Letture { get; private set; }
        public bool Rotto { get; set; }
        public DateOnly Giorno { get; private set; }
        public int Giorni { get; private set; }

        public Task<MeteoLetto> GetAsync(IReadOnlySet<int>? aziende, DateOnly oggi, int giorni, CancellationToken ct = default)
        {
            Letture++;
            (Giorno, Giorni) = (oggi, giorni);
            return Rotto
                ? Task.FromException<MeteoLetto>(new InvalidOperationException("4Grapes non risponde"))
                : new InMemoryMeteoRepository().GetAsync(aziende, oggi, giorni, ct);
        }
    }
}
