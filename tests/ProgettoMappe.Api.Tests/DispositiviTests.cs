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
using ProgettoMappe.Web.Dispositivi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Stazioni meteo, dispositivi e trappole (task #69): lettura delle righe, colori, posizione,
/// endpoint e cache, testi del popup e segnaposti per la mappa. Tutto con dati finti.
/// </summary>
public class DispositiviTests
{
    private static readonly DateOnly Oggi = new(2026, 10, 13);

    // --- Lettura da 4Grapes ---

    [Fact]
    public void Una_stazione_prende_l_azienda_del_vigneto_e_la_sua_posizione()
    {
        var stazione = FourGrapesDispositiviRepository.LeggiStazione(Record(
            ("EnteVigneto", 5), ("EnteStazione", 9), ("IdVigneto", 7), ("IdStazione", 3), ("Descrizione", " Stazione Collio "),
            ("Latitudine", 45.9), ("Longitudine", 13.5), ("Ultimo", new DateTime(2026, 10, 12))), null);

        Assert.Equal(new StazioneMeteo(5, 7, 3, "Stazione Collio", 45.9, 13.5, new DateTime(2026, 10, 12)), stazione);
    }

    [Fact]
    public void Una_stazione_col_vigneto_fuori_dalle_aziende_prende_quella_della_stazione()
    {
        var stazione = FourGrapesDispositiviRepository.LeggiStazione(Record(
            ("EnteVigneto", 5), ("EnteStazione", 9), ("IdVigneto", 7), ("IdStazione", null), ("Descrizione", null),
            ("Latitudine", 0.0), ("Longitudine", 0.0), ("Ultimo", null)), new HashSet<int> { 9 });

        Assert.Equal(9, stazione!.AziendaId);
        Assert.Null(stazione.Latitudine); // 0/0 non è una posizione
        Assert.Null(stazione.UltimoDato);
        Assert.Null(FourGrapesDispositiviRepository.LeggiStazione(Record(
            ("EnteVigneto", 5), ("EnteStazione", null), ("IdVigneto", 7), ("IdStazione", null), ("Descrizione", null),
            ("Latitudine", null), ("Longitudine", null), ("Ultimo", null)), new HashSet<int> { 9 }));
    }

    [Fact]
    public void Dispositivi_e_trappole_senza_dati_essenziali_si_saltano()
    {
        Assert.Equal(new SensoreVigneto(5, 7, "ABC1", "Umidità suolo", "Device not found", null),
            FourGrapesDispositiviRepository.LeggiSensore(Record(("IdEnte", 5), ("IdVigneto", 7), ("Codice", "ABC1"),
                ("Sensore", "Umidità suolo"), ("Status", "Device not found"), ("DataRegistrazione", null))));
        Assert.Null(FourGrapesDispositiviRepository.LeggiSensore(Record(("IdEnte", 5), ("IdVigneto", 7), ("Codice", " "),
            ("Sensore", null), ("Status", null), ("DataRegistrazione", null))));
        Assert.Null(FourGrapesDispositiviRepository.LeggiTrappole(Record(("IdEnte", 5), ("IdVigneto", 7), ("Trappole", 0), ("Ultimo", null))));
        Assert.Equal(4, FourGrapesDispositiviRepository.LeggiTrappole(Record(("IdEnte", 5), ("IdVigneto", 7), ("Trappole", 4), ("Ultimo", null)))!.Trappole);
    }

    // --- Colori ---

    [Theory]
    [InlineData(0, StatoDispositivo.Attivo)]
    [InlineData(1, StatoDispositivo.Attivo)]
    [InlineData(2, StatoDispositivo.InRitardo)]
    [InlineData(10, StatoDispositivo.InRitardo)]
    [InlineData(11, StatoDispositivo.Fermo)]
    [InlineData(-3, StatoDispositivo.Attivo)] // data futura: errore di 4Grapes, conta come oggi
    public void Il_colore_dipende_dai_giorni_dall_ultimo_dato(int giorniFa, StatoDispositivo atteso)
    {
        var (giorni, stato) = DispositiviController.Classifica(Oggi.AddDays(-giorniFa).ToDateTime(new TimeOnly(23, 30)), Oggi);

        Assert.Equal(atteso, stato);
        Assert.Equal(Math.Max(0, giorniFa), giorni);
    }

    [Fact]
    public void Senza_dato_il_segnaposto_e_grigio()
    {
        Assert.Equal((null, StatoDispositivo.Mai), DispositiviController.Classifica(null, Oggi));
        Assert.Equal("grigio", TestiDispositivi.Colore(StatoDispositivo.Mai));
        Assert.Equal("grigio", TestiDispositivi.Colore(StatoDispositivo.NonDisponibile));
        Assert.Equal("rosso", TestiDispositivi.Colore(StatoDispositivo.Fermo));
    }

    // --- Composizione ---

    [Fact]
    public void Senza_posizione_propria_si_usa_il_centro_del_vigneto()
    {
        var vigneto = new Vigneto
        {
            Id = 7, AziendaId = 5, Nome = "Ronco",
            GeometriaGeoJson = """{"type":"Polygon","coordinates":[[[13,45],[13.2,45],[13.2,45.4],[13,45.4],[13,45]]]}""",
        };
        var letti = new DispositiviLetti(
            [new StazioneMeteo(5, 7, 3, null, null, null, new DateTime(2026, 10, 1))],
            [new SensoreVigneto(5, 7, "ABC1", "Temperatura aria", "Correctly updated", new DateTime(2024, 5, 3)),
             new SensoreVigneto(5, 99, "SENZA", null, null, null)],
            [new TrappoleVigneto(5, 7, 3, new DateTime(2026, 10, 8)), new TrappoleVigneto(6, 8, 2, null)]);

        var dispositivi = DispositiviController.Componi(5, [vigneto], letti, Oggi);

        Assert.Equal(["stazione-3", "sensore-ABC1", "sensore-SENZA", "trappole-7"], dispositivi.Select(d => d.Id));
        var stazione = dispositivi[0];
        Assert.Equal("Stazione 3", stazione.Nome);
        Assert.Equal(13.1, stazione.Longitudine!.Value, 6);
        Assert.Equal(45.2, stazione.Latitudine!.Value, 6);
        Assert.Equal(StatoDispositivo.Fermo, stazione.Stato);
        Assert.Equal(StatoDispositivo.NonDisponibile, dispositivi[1].Stato);
        Assert.Null(dispositivi[2].Longitudine); // vigneto non dell'azienda: nessuna posizione
        Assert.Equal("Ronco", dispositivi[3].Nome);
        Assert.Equal(3, dispositivi[3].Trappole);
        Assert.Equal(StatoDispositivo.InRitardo, dispositivi[3].Stato);
    }

    [Fact]
    public void Il_riepilogo_conta_i_fermi_per_azienda()
    {
        var letti = new DispositiviLetti(
            [new StazioneMeteo(5, 7, 3, "A", 45, 13, new DateTime(2026, 9, 1)), new StazioneMeteo(6, 8, 4, "B", 45, 13, new DateTime(2026, 10, 12))],
            [new SensoreVigneto(6, 8, "X", null, "Device not found", null)],
            [new TrappoleVigneto(5, 7, 2, null)]);

        var riepilogo = DispositiviController.Riepiloga(letti, Oggi);

        Assert.Equal([new RiepilogoDispositiviDto(5, 2, 1), new RiepilogoDispositiviDto(6, 2, 0)], riepilogo.Aziende);
    }

    // --- Endpoint e cache ---

    [Fact]
    public async Task L_endpoint_da_i_dispositivi_dell_azienda_con_posizione()
    {
        var dto = Valore(await Controller().GetAzienda(1, CancellationToken.None));

        Assert.True(dto.Pronti);
        Assert.Equal([TipoDispositivo.Stazione, TipoDispositivo.Stazione, TipoDispositivo.Sensore, TipoDispositivo.Trappole],
            dto.Dispositivi.Select(d => d.Tipo));
        Assert.All(dto.Dispositivi, d => Assert.NotNull(d.Longitudine));
        Assert.NotNull(dto.Aggiornamento);
    }

    [Fact]
    public async Task Fuori_perimetro_nessun_dispositivo()
    {
        var dto = Valore(await Controller(PerimetroAziendeTests.Perimetro(2)).GetAzienda(1, CancellationToken.None));

        Assert.Empty(dto.Dispositivi);
    }

    [Fact]
    public async Task La_pagina_iniziale_sa_quali_aziende_hanno_dispositivi_fermi()
    {
        var risposta = await Controller().GetTutte(CancellationToken.None);
        var dto = Assert.IsType<DispositiviAziendeDto>(Assert.IsType<OkObjectResult>(risposta.Result).Value);

        var azienda = Assert.Single(dto.Aziende);
        Assert.Equal(1, azienda.AziendaId);
        Assert.Equal(1, azienda.Fermi); // la stazione di prova ferma da 15 giorni
    }

    [Fact]
    public async Task Se_4Grapes_non_risponde_l_endpoint_da_503_e_non_resta_in_cache()
    {
        var rotto = new RepositoryContato { Rotto = true };
        var controller = new DispositiviController(CacheVignetiAziendeTests.Cache(dispositivi: rotto), PerimetroAziendeTests.Perimetro(),
            NullLogger<DispositiviController>.Instance);

        var risposta = await controller.GetAzienda(1, CancellationToken.None);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(risposta.Result).StatusCode);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>((await controller.GetTutte(CancellationToken.None)).Result).StatusCode);

        rotto.Rotto = false;
        Assert.NotEmpty(Valore(await controller.GetAzienda(1, CancellationToken.None)).Dispositivi);
        Assert.Equal(3, rotto.Letture);
    }

    [Fact]
    public async Task Tutte_le_aziende_si_leggono_una_volta_sola_per_10_minuti()
    {
        var repository = new RepositoryContato();
        var controller = new DispositiviController(CacheVignetiAziendeTests.Cache(dispositivi: repository), PerimetroAziendeTests.Perimetro(),
            NullLogger<DispositiviController>.Instance);

        await controller.GetAzienda(1, CancellationToken.None);
        await controller.GetAzienda(2, CancellationToken.None);
        await controller.GetTutte(CancellationToken.None);

        Assert.Equal(1, repository.Letture);
        Assert.Equal(TimeSpan.FromMinutes(10), CacheVignetiAziende.DurataDispositivi);
    }

    // --- Testi e segnaposti (Web) ---

    [Fact]
    public void Il_popup_dice_tipo_ultimo_dato_e_da_quanto()
    {
        var stazione = new DispositivoDto("stazione-3", TipoDispositivo.Stazione, "Collio", 7, 13, 45, new DateTime(2026, 10, 12), 1,
            StatoDispositivo.Attivo);
        var trappole = new DispositivoDto("trappole-7", TipoDispositivo.Trappole, "Ronco", 7, 13, 45, new DateTime(2026, 9, 20), 23,
            StatoDispositivo.Fermo, Trappole: 5);
        var mai = trappole with { UltimoDato = null, Giorni = null, Stato = StatoDispositivo.Mai, Trappole = 1 };
        var sensore = new DispositivoDto("sensore-X", TipoDispositivo.Sensore, "X", 7, 13, 45, null, null, StatoDispositivo.NonDisponibile,
            Sensore: "Umidità suolo", StatoFourGrapes: "Correctly updated", Registrato: new DateTime(2024, 5, 3));

        Assert.Equal(["Stazione meteo", "Ultimo dato: 12/10 (ieri)"], TestiDispositivi.Righe(stazione, Oggi));
        Assert.Equal(["5 trappole", "Ultimo controllo: 20/09 (23 giorni fa) · ferme"], TestiDispositivi.Righe(trappole, Oggi));
        Assert.Equal(["1 trappola", "Nessun controllo registrato"], TestiDispositivi.Righe(mai, Oggi));
        Assert.Equal(["Sensore: Umidità suolo", "Ultimo dato non disponibile in 4Grapes", "Stato in 4Grapes: Correctly updated",
            "Registrato il 03/05/2024"], TestiDispositivi.Righe(sensore, Oggi));
        Assert.Equal("oggi", TestiDispositivi.DaQuanto(0));
        Assert.Equal("12/10/2025", TestiDispositivi.Giorno(new DateTime(2025, 10, 12), Oggi));
    }

    [Fact]
    public void Sulla_mappa_vanno_solo_i_dispositivi_con_posizione()
    {
        var con = new DispositivoDto("stazione-3", TipoDispositivo.Stazione, "Collio", 7, 13.1, 45.2, null, null, StatoDispositivo.Mai);
        var senza = con with { Id = "stazione-4", Longitudine = null, Latitudine = null };

        var segnaposto = Assert.Single(TestiDispositivi.Segnaposti([con, senza], Oggi));

        Assert.Equal(new SegnapostoDispositivo("stazione-3", "stazione", 13.1, 45.2, "grigio", "Collio", segnaposto.Righe), segnaposto);
        Assert.Equal("1 dispositivo fermo da oltre 10 giorni", TestiDispositivi.Fermi(1));
    }

    private static DispositiviController Controller(PerimetroAziende? perimetro = null) =>
        new(CacheVignetiAziendeTests.Cache(), perimetro ?? PerimetroAziendeTests.Perimetro(), NullLogger<DispositiviController>.Instance);

    private static DispositiviAziendaDto Valore(ActionResult<DispositiviAziendaDto> risultato) =>
        Assert.IsType<DispositiviAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

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

    /// <summary>Dispositivi in-memory che contano le letture; <see cref="Rotto"/> = 4Grapes non risponde.</summary>
    private sealed class RepositoryContato : IDispositiviRepository
    {
        public bool Rotto { get; set; }

        public int Letture { get; private set; }

        public Task<DispositiviLetti> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
        {
            Letture++;
            return Rotto
                ? Task.FromException<DispositiviLetti>(new InvalidOperationException("4Grapes non risponde"))
                : new InMemoryDispositiviRepository().GetAsync(aziende, ct);
        }
    }
}
