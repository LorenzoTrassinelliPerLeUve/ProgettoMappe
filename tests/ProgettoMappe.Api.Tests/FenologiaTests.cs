using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
/// Fenologia BBCH (task #44): riassunto dei rilievi per vigneto e anno, lettura delle righe,
/// endpoint, tematismo (colori, legenda, scheda) ed esportazione. Tutto con dati finti.
/// </summary>
public class FenologiaTests
{
    private static readonly Dictionary<int, string> Fasi = new()
    {
        [57] = "infiorescenze pienamente sviluppate; fiori tra di loro separati",
        [61] = "10% delle caliptre è caduto",
        [65] = "Piena fioritura: 50% delle caliptre è caduto",
        [89] = "Vendemmia",
    };

    private static readonly CatalogoTematismi Catalogo = CatalogoTematismi.Crea(new TematismiOptions(), NullLogger.Instance);

    private static Tematismo Tema => Catalogo.Trova(TematismiFenologia.Id)!;

    private static FenologiaVignetoDto Riga(int anno, int id, int bbch, string? descrizione = null, int mese = 6, int giorno = 12,
        params FaseRilevataDto[] precedenti) =>
        new(anno, id, bbch, descrizione, new DateOnly(anno, mese, giorno), precedenti);

    // --- Riassunto dei rilievi ---

    [Fact]
    public void L_ultima_fase_e_quella_del_giorno_piu_recente_con_le_precedenti_dalla_piu_vecchia()
    {
        var riassunto = CacheFenologia.Riassumi(
        [
            new(2026, 10, new DateOnly(2026, 6, 3), 61),
            new(2026, 10, new DateOnly(2026, 5, 28), 57),
            new(2026, 10, new DateOnly(2026, 6, 12), 65),
            new(2026, 10, new DateOnly(2026, 6, 5), 57), // fase già vista: conta il primo giorno
            new(2025, 10, new DateOnly(2025, 9, 20), 89),
        ], Fasi);

        Assert.Equal([2025, 2026], riassunto.Select(f => f.Anno));
        var f2026 = riassunto[1];
        Assert.Equal((65, new DateOnly(2026, 6, 12)), (f2026.Bbch, f2026.Data));
        Assert.Equal("Piena fioritura: 50% delle caliptre è caduto", f2026.Descrizione);
        Assert.Equal([(57, new DateOnly(2026, 5, 28)), (61, new DateOnly(2026, 6, 3))],
            f2026.Precedenti.Select(p => (p.Bbch, p.Data)));
        Assert.Empty(riassunto[0].Precedenti);
    }

    [Fact]
    public void Piu_rilievi_nello_stesso_ultimo_giorno_danno_la_fase_piu_avanzata()
    {
        var giorno = new DateOnly(2026, 6, 12);
        var f = Assert.Single(CacheFenologia.Riassumi(
        [
            new(2026, 10, giorno, 61),
            new(2026, 10, giorno, 65),
            new(2026, 10, giorno, 63),
            new(2026, 10, new DateOnly(2026, 6, 1), 65),
        ], Fasi));

        Assert.Equal(65, f.Bbch);
        Assert.Empty(f.Precedenti); // il 65 visto prima è la fase attuale, il 61 e il 63 sono dello stesso giorno
    }

    [Fact]
    public void Un_codice_senza_nome_resta_col_solo_numero()
    {
        var f = Assert.Single(CacheFenologia.Riassumi([new(2026, 10, new DateOnly(2026, 5, 20), 52)], Fasi));

        Assert.Null(f.Descrizione);
        Assert.Equal("52", TematismiFenologia.NomeFase(52, null));
    }

    // --- Lettura ---

    [Fact]
    public void Le_righe_sporche_si_leggono_senza_eccezioni()
    {
        Assert.Equal(new RilievoBbch(2026, 10, new DateOnly(2026, 6, 12), 65),
            CacheFenologia.LeggiRilievo(Record(("Anno", 2026), ("IdVigneto", 10), ("Data", new DateTime(2026, 6, 12, 9, 30, 0)), ("BBCH", 65))));
        Assert.Null(CacheFenologia.LeggiRilievo(Record(("Anno", 2026), ("IdVigneto", 10), ("Data", null), ("BBCH", 65))));
        Assert.Null(CacheFenologia.LeggiRilievo(Record(("Anno", 2026), ("IdVigneto", 10), ("Data", "ieri"), ("BBCH", 65))));

        Assert.Equal(new CacheFenologia.FaseBbch(0, "Dormienza"), CacheFenologia.LeggiFase(Record(("BBCH", 0), ("DescrizioneBBCH", "00 - Dormienza"))));
        Assert.Null(CacheFenologia.LeggiFase(Record(("BBCH", 1), ("DescrizioneBBCH", null))));
    }

    [Theory]
    [InlineData("65 - Piena fioritura: 50% delle caliptre è caduto", "Piena fioritura: 50% delle caliptre è caduto")]
    [InlineData("  09 – il germoglio si espande", "il germoglio si espande")]
    [InlineData("Vendemmia", "Vendemmia")]
    [InlineData("89 - ", "89 -")]
    public void Il_numero_davanti_al_nome_si_toglie(string db, string atteso) =>
        Assert.Equal(atteso, CacheFenologia.SenzaNumero(db));

    // --- Endpoint ---

    [Fact]
    public async Task Endpoint_restituisce_solo_i_vigneti_dell_azienda()
    {
        var risposta = Valore(await Controller(new InMemoryFenologiaRepository()).GetFenologia(1, CancellationToken.None));

        Assert.True(risposta.Pronto);
        Assert.NotNull(risposta.Aggiornamento);
        Assert.Equal([1, 2], risposta.Fasi.Select(f => f.IdVigneto).Distinct());
        var v1 = risposta.Fasi.Single(f => f.IdVigneto == 1 && f.Anno == 2025);
        Assert.Equal((65, new DateOnly(2025, 6, 12)), (v1.Bbch, v1.Data));
        Assert.Equal([57], v1.Precedenti.Select(p => p.Bbch));
    }

    [Fact]
    public async Task Azienda_inesistente_o_dati_non_pronti_danno_liste_vuote()
    {
        var vuota = Valore(await Controller(new InMemoryFenologiaRepository()).GetFenologia(9999, CancellationToken.None));
        var nonPronta = Valore(await Controller(new RepositoryFinto(false)).GetFenologia(1, CancellationToken.None));

        Assert.True(vuota.Pronto);
        Assert.Empty(vuota.Fasi);
        Assert.False(nonPronta.Pronto);
        Assert.Empty(nonPronta.Fasi);
    }

    [Theory]
    [InlineData(true, 202)]
    [InlineData(false, 409)]
    public void Ricarica_risponde_202_se_parte_e_409_se_ce_n_e_gia_una_in_corso(bool parte, int codice)
    {
        var risultato = Assert.IsAssignableFrom<ObjectResult>(Controller(new RepositoryFinto(parte)).Ricarica(new VitaFinta()).Result);

        Assert.Equal(codice, risultato.StatusCode);
    }

    [Fact]
    public void La_data_arriva_intera_al_Web_dopo_il_passaggio_in_JSON()
    {
        var dto = new FenologiaAziendaDto(true, [Riga(2026, 1, 65, "Piena fioritura", precedenti: new FaseRilevataDto(57, null, new DateOnly(2026, 5, 28)))]);

        var copia = JsonSerializer.Deserialize<FenologiaAziendaDto>(JsonSerializer.Serialize(dto, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;

        Assert.Equal(new DateOnly(2026, 6, 12), copia.Fasi[0].Data);
        Assert.Equal(new DateOnly(2026, 5, 28), copia.Fasi[0].Precedenti[0].Data);
    }

    // --- Tematismo ---

    [Fact]
    public void Il_tematismo_classifica_per_codice_nell_anno_scelto_e_non_si_confronta()
    {
        var righe = new IRigaVigneto[] { Riga(2026, 1, 65), Riga(2025, 1, 89), Riga(2026, 2, 57) };

        var classificazione = TematismiBigot.ClassiPerVigneto(Tema, righe, 2026);

        Assert.Equal(65, classificazione.ClasseDi(1));
        Assert.Equal(57, classificazione.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, classificazione.ClasseDi(3));
        Assert.False(Tema.Confrontabile);
        Assert.True(Tema.AnnoRecente);
        Assert.Equal("Fenologia", Tema.Gruppo);
        Assert.DoesNotContain(Tema, Catalogo.Statistiche);
        Assert.False(ProgettoMappe.Web.Services.EsportazioneVigneti.TryRichiestaConfronto(Catalogo, Tema.Id, "2026", "2025", out _));
    }

    [Fact]
    public void La_fenologia_si_apre_sull_anno_piu_recente_anche_se_in_corso()
    {
        Assert.Equal(2026, TematismiBigot.AnnoPredefinito(Tema, [2024, 2026, 2025], 2026));
        Assert.Equal(2025, TematismiBigot.AnnoPredefinito(Catalogo.Trova("punteggio"), [2024, 2026, 2025], 2026));
    }

    [Fact]
    public void La_legenda_mostra_solo_le_fasi_presenti_col_nome_e_il_grigio_senza_rilievi()
    {
        var righe = new[] { Riga(2026, 1, 65, Fasi[65], precedenti: new FaseRilevataDto(57, Fasi[57], new DateOnly(2026, 5, 28))), Riga(2026, 2, 89, Fasi[89]) };
        var conteggi = new Dictionary<int, int> { [65] = 1, [89] = 1, [TematismiBigot.ClasseFuoriPerimetro] = 3 };

        var voci = LegendaTematismo.Voci(Tema, null, 2026, conteggi, null, TematismiFenologia.NomiFasi(righe));

        Assert.Equal(["65 – Piena fioritura", "89 – Vendemmia", "Nessun rilievo nel 2026"], voci.Select(v => v.Testo));
        Assert.Equal([1, 1, 3], voci.Select(v => v.Conteggio));
        Assert.Equal("fp", voci[^1].Css);
    }

    [Fact]
    public void I_colori_seguono_le_tappe_e_si_mescolano_in_mezzo()
    {
        TappaFenologiaOptions[] tappe = [new() { Bbch = 10, Colore = "#000000" }, new() { Bbch = 20, Colore = "#FFFFFF" }];

        Assert.Equal("#000000", TematismiFenologia.Colore(0, tappe));
        Assert.Equal("#808080", TematismiFenologia.Colore(15, tappe));
        Assert.Equal("#ffffff", TematismiFenologia.Colore(99, tappe));
        Assert.Equal(TematismiFenologia.CodiceMassimo + 1, Tema.Classi.Count);
        Assert.Equal("#fed976", Tema.Classi[65].Colore); // tappa della piena fioritura nella scala predefinita
    }

    [Fact]
    public void Una_scala_sbagliata_in_configurazione_torna_quella_predefinita()
    {
        var sbagliata = new FenologiaOptions { Tappe = [new() { Bbch = 60, Colore = "#fff" }, new() { Bbch = 10, Colore = "#000000" }] };

        Assert.Equal(2, TematismiFenologia.Errori(sbagliata).Count);
        Assert.Empty(TematismiFenologia.Errori(TematismiFenologia.Predefinita));
        var tema = TematismiFenologia.Crea(sbagliata, NullLogger.Instance);
        Assert.Equal(Tema.Classi.Select(c => c.Colore), tema.Classi.Select(c => c.Colore));
    }

    [Fact]
    public void La_scheda_dice_fase_e_data_e_in_piccolo_le_fasi_precedenti()
    {
        var riga = Riga(2026, 1, 65, Fasi[65], precedenti:
            [new FaseRilevataDto(57, Fasi[57], new DateOnly(2026, 5, 28)), new FaseRilevataDto(61, Fasi[61], new DateOnly(2026, 6, 3))]);

        Assert.Equal("65 – Piena fioritura, rilevata il 12/06", TematismiFenologia.TestoScheda(riga));
        Assert.Equal("57 il 28/05 · 61 il 03/06", TematismiFenologia.TestoPrecedenti(riga));
        Assert.Null(TematismiFenologia.TestoPrecedenti(Riga(2026, 1, 65)));
        Assert.Equal("BBCH 65 · 12/06", Tema.TestoValore(riga));
        Assert.Equal("57 – Infiorescenze pienamente sviluppate", TematismiFenologia.NomeFase(57, Fasi[57]));
    }

    // --- Esportazione ---

    [Fact]
    public void L_esportazione_ha_fase_nome_e_data()
    {
        var vigneto = new Vigneto { Id = 1, AziendaId = 1, Nome = "Vigneto 1" };
        var esportato = new VignetoEsportato(vigneto, "Azienda", 2026, null,
            Fenologia: new FenologiaVigneto(2026, 1, 65, "Piena fioritura: 50% delle caliptre è caduto", new DateOnly(2026, 6, 12), []));

        object? Campo(string nome) => EsportazioneVigneti.Campi.Single(c => c.Nome == nome).Valore(esportato);

        Assert.Equal(65, Campo("bbch"));
        Assert.Equal("Piena fioritura: 50% delle caliptre è caduto", Campo("fase_bbch"));
        Assert.Equal("2026-06-12", Campo("data_bbch"));
        Assert.All(EsportazioneVigneti.Campi, c => Assert.True(c.Nome.Length <= 10, c.Nome));
    }

    [Fact]
    public async Task L_endpoint_di_esportazione_mette_la_fenologia_dell_anno_scelto()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(), new InMemoryAziendeRepository(),
            new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro(), fenologia: new InMemoryFenologiaRepository());

        var file = Assert.IsType<FileContentResult>(await controller.Esporta(1, "geojson", 2025, "1", CancellationToken.None));

        using var documento = JsonDocument.Parse(file.FileContents);
        var proprieta = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray(), f => f.TryGetProperty("id", out _)).GetProperty("properties");
        Assert.Equal(65, proprieta.GetProperty("bbch").GetInt32());
        Assert.Equal("2025-06-12", proprieta.GetProperty("data_bbch").GetString());
    }

    private static FenologiaController Controller(IFenologiaRepository repository) =>
        new(CacheVignetiAziendeTests.Cache(), repository, PerimetroAziendeTests.Perimetro());

    private static FenologiaAziendaDto Valore(ActionResult<FenologiaAziendaDto> risultato) =>
        Assert.IsType<FenologiaAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

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

    private sealed class RepositoryFinto(bool pronto) : IFenologiaRepository
    {
        public Task<IReadOnlyList<FenologiaVigneto>?> GetPerVignetiAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FenologiaVigneto>?>(pronto ? [] : null);

        public StatoIndiciBigot Stato => new(pronto, true, null, null);

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
