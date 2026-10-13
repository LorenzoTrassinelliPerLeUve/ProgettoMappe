using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Ultimo trattamento e linea di difesa per vigneto (task #68): lettura e riassunto delle righe,
/// endpoint e cache, i due tematismi (classi, legenda, scheda) ed esportazione. Tutto con dati finti.
/// </summary>
public class DifesaTests
{
    private static readonly DateOnly Oggi = new(2026, 10, 13);

    // --- Lettura da 4Grapes ---

    [Fact]
    public void Una_riga_di_trattamento_si_legge_e_pulisce_gli_spazi()
    {
        var riga = FourGrapesDifesaRepository.LeggiTrattamento(Record(
            ("IdEnte", 5), ("IdVigneto", 7), ("Data", new DateTime(2026, 7, 3, 12, 21, 0)),
            ("Avversita", "MUFFA GRIGIA O BOTRITE  - Botrytis Cinerea"), ("NumRegProdottoVite", "12345"),
            ("NomeCommerciale", " CYPRUS 25 WG BLU "), ("PrincipioAttivo", "RAME")));

        Assert.Equal(new FourGrapesDifesaRepository.RigaTrattamento(5, 7, new DateTime(2026, 7, 3, 12, 21, 0),
            "MUFFA GRIGIA O BOTRITE - Botrytis Cinerea", "CYPRUS 25 WG BLU", "RAME"), riga);
    }

    [Fact]
    public void Un_prodotto_fuori_anagrafica_resta_col_suo_numero()
    {
        var riga = FourGrapesDifesaRepository.LeggiTrattamento(Record(
            ("IdEnte", 5), ("IdVigneto", 7), ("Data", new DateTime(2026, 7, 3)),
            ("Avversita", null), ("NumRegProdottoVite", "12345"), ("NomeCommerciale", null), ("PrincipioAttivo", null)));

        Assert.Equal("n. reg. 12345", riga!.Prodotto);
        Assert.Null(riga.PrincipioAttivo);
    }

    [Fact]
    public void Una_riga_senza_vigneto_o_data_si_salta()
    {
        Assert.Null(FourGrapesDifesaRepository.LeggiTrattamento(Record(
            ("IdEnte", 5), ("IdVigneto", null), ("Data", new DateTime(2026, 7, 3)),
            ("Avversita", null), ("NumRegProdottoVite", null), ("NomeCommerciale", null), ("PrincipioAttivo", null))));
        Assert.Null(FourGrapesDifesaRepository.LeggiTrattamento(Record(
            ("IdEnte", 5), ("IdVigneto", 7), ("Data", "ieri"),
            ("Avversita", null), ("NumRegProdottoVite", null), ("NomeCommerciale", null), ("PrincipioAttivo", null))));
    }

    [Fact]
    public void Una_riga_di_linea_si_legge_anche_senza_rischio()
    {
        var linea = FourGrapesDifesaRepository.LeggiLinea(Record(
            ("IdEnte", 5), ("IdVigneto", 7), ("IdTipologiaLinea", 36), ("Linea", "SAFE  - DECISION 4GRAPES"), ("Rischio", null)));

        Assert.Equal(new LineaDifesaVigneto(5, 7, 36, "SAFE - DECISION 4GRAPES", null), linea);
    }

    [Fact]
    public void Due_trattamenti_dello_stesso_giorno_si_uniscono_senza_ripetizioni()
    {
        var mattina = new DateTime(2026, 7, 3, 8, 0, 0);
        var sera = new DateTime(2026, 7, 3, 18, 30, 0);
        FourGrapesDifesaRepository.RigaTrattamento Riga(DateTime data, string? avversita, string? prodotto, string? principio) =>
            new(5, 7, data, avversita, prodotto, principio);

        var riassunti = FourGrapesDifesaRepository.Riassumi(
        [
            Riga(mattina, "Peronospora", "CYPRUS 25 WG BLU", "RAME"),
            Riga(mattina, "Oidio", "CYPRUS 25 WG BLU", "RAME"),
            Riga(sera, "Oidio", "THIOVIT JET", "ZOLFO"),
            Riga(new DateTime(2026, 6, 20), "Peronospora", "VECCHIO", "VECCHIO"), // giorno precedente: non conta
            new(5, 8, new DateTime(2026, 6, 1), null, null, null),
        ]);

        Assert.Equal(2, riassunti.Count);
        var t = riassunti[0];
        Assert.Equal(7, t.IdVigneto);
        Assert.Equal(sera, t.Data);
        Assert.Equal(["RAME", "ZOLFO"], t.PrincipiAttivi);
        Assert.Equal(["CYPRUS 25 WG BLU", "THIOVIT JET"], t.Prodotti);
        Assert.Equal(["Oidio", "Peronospora"], t.Avversita);
        Assert.Empty(riassunti[1].PrincipiAttivi);
    }

    // --- Endpoint e cache ---

    [Fact]
    public void Una_riga_per_vigneto_anche_senza_trattamenti_ne_linea()
    {
        var letta = new DifesaLettaAlle(new DifesaLetta(
            [
                new(1, 2, new DateTime(2026, 7, 3), ["RAME"], ["CYPRUS"], ["Peronospora"]),
                new(1, 99, new DateTime(2026, 7, 3), ["ZOLFO"], ["THIOVIT"], ["Oidio"]),
            ],
            [
                new(1, 1, 4, "BIO 4 KG", "ALTO / BASSO"),
                new(1, 3, null, null, "BASSO / BASSO"), // rischio senza linea
            ]), DateTimeOffset.UnixEpoch);

        var dto = DifesaController.Componi([3, 1, 2], letta);

        Assert.True(dto.Pronto);
        Assert.Equal(DateTimeOffset.UnixEpoch, dto.Aggiornamento);
        Assert.Equal([1, 2, 3], dto.Vigneti.Select(v => v.IdVigneto));
        Assert.Null(dto.Vigneti[0].Data);
        Assert.Equal(4, dto.Vigneti[0].IdLinea);
        Assert.Equal("BIO 4 KG", dto.Vigneti[0].Linea);
        Assert.Equal("ALTO / BASSO", dto.Vigneti[0].Rischio);
        Assert.Equal(new DateTime(2026, 7, 3), dto.Vigneti[1].Data);
        Assert.Equal(["RAME"], dto.Vigneti[1].PrincipiAttivi);
        Assert.Null(dto.Vigneti[1].IdLinea);
        Assert.Null(dto.Vigneti[2].IdLinea);
        Assert.Equal("BASSO / BASSO", dto.Vigneti[2].Rischio);
    }

    [Fact]
    public async Task L_endpoint_da_la_difesa_dei_vigneti_dell_azienda()
    {
        var dto = Valore(await Controller().GetDifesa(1, CancellationToken.None));

        Assert.True(dto.Pronto);
        Assert.Equal([1, 2], dto.Vigneti.Select(v => v.IdVigneto));
        Assert.Equal("BIO 4 KG", dto.Vigneti[0].Linea);
        Assert.NotNull(dto.Aggiornamento);
    }

    [Fact]
    public async Task Fuori_perimetro_nessun_dato_di_difesa()
    {
        var dto = Valore(await Controller(PerimetroAziendeTests.Perimetro(2)).GetDifesa(1, CancellationToken.None));

        Assert.Empty(dto.Vigneti);
    }

    [Fact]
    public async Task Se_4Grapes_non_risponde_l_endpoint_da_503_e_non_resta_in_cache()
    {
        var rotto = new RepositoryContato { Rotto = true };
        var controller = new DifesaController(CacheVignetiAziendeTests.Cache(difesa: rotto), PerimetroAziendeTests.Perimetro(),
            NullLogger<DifesaController>.Instance);

        var risposta = await controller.GetDifesa(1, CancellationToken.None);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(risposta.Result).StatusCode);

        rotto.Rotto = false;
        Assert.Equal(2, Valore(await controller.GetDifesa(1, CancellationToken.None)).Vigneti.Count);
        Assert.Equal(2, rotto.Letture);
    }

    [Fact]
    public async Task Tutte_le_aziende_si_leggono_una_volta_sola_per_10_minuti()
    {
        var repository = new RepositoryContato();
        var controller = new DifesaController(CacheVignetiAziendeTests.Cache(difesa: repository), PerimetroAziendeTests.Perimetro(),
            NullLogger<DifesaController>.Instance);

        await controller.GetDifesa(1, CancellationToken.None);
        await controller.GetDifesa(1, CancellationToken.None);
        await controller.GetDifesa(2, CancellationToken.None);

        Assert.Equal(1, repository.Letture);
        Assert.Equal(TimeSpan.FromMinutes(10), CacheVignetiAziende.DurataDifesa);
    }

    // --- Tematismo «Ultimo trattamento» ---

    [Theory]
    [InlineData(0, TematismiDifesa.ClasseRecente)]
    [InlineData(6, TematismiDifesa.ClasseRecente)]
    [InlineData(7, TematismiDifesa.ClasseDueSettimane)]
    [InlineData(14, TematismiDifesa.ClasseDueSettimane)]
    [InlineData(15, TematismiDifesa.ClasseMese)]
    [InlineData(30, TematismiDifesa.ClasseMese)]
    [InlineData(31, TematismiDifesa.ClasseScoperto)]
    [InlineData(-2, TematismiDifesa.ClasseRecente)] // data futura (dato sporco): conta come oggi
    public void La_classe_dipende_dai_giorni_dall_ultimo_trattamento(int giorniFa, int classe) =>
        Assert.Equal(classe, TematismiDifesa.Classe(Difesa(Oggi.ToDateTime(new TimeOnly(23, 50)).AddDays(-giorniFa)), Oggi));

    [Fact]
    public void Mai_trattato_sta_con_gli_oltre_30_giorni()
    {
        var tema = TematismiDifesa.CreaUltimoTrattamento(() => Oggi);
        var mai = Difesa(null);

        Assert.Equal(TematismiDifesa.ClasseScoperto, tema.Classifica(mai));
        Assert.Equal("Oltre 30 giorni o mai", tema.Classi[TematismiDifesa.ClasseScoperto].Etichetta);
        Assert.Equal("Mai trattato", tema.TestoValore(mai));
        Assert.True(tema.ValoreConfronto(mai) > tema.ValoreConfronto(Difesa(new DateTime(2020, 1, 1))));
    }

    [Fact]
    public void I_tematismi_della_difesa_sono_senza_anno_nel_gruppo_Difesa()
    {
        var catalogo = CatalogoTematismi.Crea(new TematismiOptions(), NullLogger.Instance);

        foreach (var id in new[] { TematismiDifesa.IdUltimoTrattamento, TematismiDifesa.IdLinea })
        {
            var tema = catalogo.Trova(id);
            Assert.NotNull(tema);
            Assert.True(tema.SenzaAnno);
            Assert.False(tema.Confrontabile);
            Assert.Equal("Difesa", tema.Gruppo);
            Assert.DoesNotContain(catalogo.Statistiche, t => t.Id == id);
        }
        Assert.Equal(4, catalogo.Trova(TematismiDifesa.IdUltimoTrattamento)!.Classi.Count);
    }

    [Fact]
    public void La_legenda_dell_ultimo_trattamento_conta_i_vigneti_per_classe()
    {
        var tema = TematismiDifesa.CreaUltimoTrattamento(() => Oggi);
        IReadOnlyList<IRigaVigneto> righe =
        [
            Difesa(new DateTime(2026, 10, 12), id: 1),
            Difesa(new DateTime(2026, 10, 1), id: 2),
            Difesa(new DateTime(2026, 9, 20), id: 3),
            Difesa(new DateTime(2026, 7, 3), id: 4),
            Difesa(null, id: 5),
        ];

        var classificazione = TematismiBigot.ClassiPerVigneto(tema, righe, null);
        var conteggi = TematismiBigot.Conteggi(classificazione, [1, 2, 3, 4, 5]);
        var voci = LegendaTematismo.Voci(tema, null, null, conteggi, null);

        Assert.Equal([1, 1, 1, 2], voci.Where(v => v.Classe >= 0).Select(v => v.Conteggio));
        Assert.DoesNotContain(voci, v => v.Classe < 0); // ogni vigneto ha la sua riga
        Assert.Equal([4, 5], TematismiBigot.FiltraPerClasse([1, 2, 3, 4, 5], id => id, classificazione, TematismiDifesa.ClasseScoperto));
    }

    [Fact]
    public void La_scheda_dice_giorno_principio_avversita_e_linea()
    {
        Assert.Equal("03/07, rame (peronospora) · linea BIO 4 KG",
            TematismiDifesa.TestoScheda(Difesa(new DateTime(2026, 7, 3, 12, 21, 0), ["RAME"], ["CYPRUS"], ["Peronospora"], 4, "BIO 4 KG"), Oggi));
        Assert.Equal("03/07, rame, zolfo (oidio, muffa grigia o botrite)",
            TematismiDifesa.TestoScheda(Difesa(new DateTime(2026, 7, 3), ["RAME", "ZOLFO"], [], ["Oidio", "MUFFA GRIGIA O BOTRITE - Botrytis Cinerea"]), Oggi));
        Assert.Equal("03/07/2025, a, b, c e altri 2",
            TematismiDifesa.TestoScheda(Difesa(new DateTime(2025, 7, 3), ["A", "B", "C", "D", "E"]), Oggi));
        Assert.Equal("03/07, THIOVIT JET (oidio)",
            TematismiDifesa.TestoScheda(Difesa(new DateTime(2026, 7, 3), [], ["THIOVIT JET"], ["Oidio"]), Oggi));
        Assert.Equal("Nessun trattamento registrato · linea SOFT",
            TematismiDifesa.TestoScheda(Difesa(null, idLinea: 3, linea: "SOFT"), Oggi));
        Assert.Equal("Nessun trattamento registrato", TematismiDifesa.TestoScheda(Difesa(null), Oggi));
        Assert.Equal("03/07 · rame e altri 1", TematismiDifesa.TestoElenco(Difesa(new DateTime(2026, 7, 3), ["RAME", "ZOLFO"]), Oggi));
        Assert.Equal("Prodotti: CYPRUS, THIOVIT", TematismiDifesa.TestoProdotti(Difesa(new DateTime(2026, 7, 3), [], ["CYPRUS", "THIOVIT"])));
    }

    [Theory]
    [InlineData("ALTO / BASSO", "peronospora alto, oidio basso")]
    [InlineData("BASSO/ALTO", "peronospora basso, oidio alto")]
    [InlineData("MEDIO", "medio")]
    [InlineData(" ", null)]
    [InlineData(null, null)]
    public void La_classe_di_rischio_si_legge_peronospora_poi_oidio(string? rischio, string? atteso) =>
        Assert.Equal(atteso, TematismiDifesa.TestoRischio(rischio));

    // --- Tematismo «Linea di difesa» ---

    [Fact]
    public void Ogni_linea_ha_la_sua_classe_e_i_senza_linea_sono_grigi()
    {
        var tema = TematismiDifesa.CreaLinea();

        Assert.Equal(4, tema.Classifica(Difesa(null, idLinea: 4, linea: "BIO 4 KG")));
        Assert.Equal(TematismiDifesa.ClasseNessunaLinea, tema.Classifica(Difesa(null)));
        Assert.Null(tema.Classifica(Difesa(null, idLinea: 5000)));
        Assert.Equal(TematismiDifesa.EtichettaNessunaLinea, tema.TestoValore(Difesa(null)));
        Assert.Equal("BIO 4 KG", tema.TestoValore(Difesa(null, idLinea: 4, linea: "BIO 4 KG")));
        Assert.Equal("#a6a6a6", tema.Classi[TematismiDifesa.ClasseNessunaLinea].Colore);
    }

    [Fact]
    public void Le_linee_usate_hanno_colori_diversi()
    {
        int[] usate = [1, 2, 3, 4, 5, 20, 25, 27, 36, 38];

        Assert.Equal(usate.Length, usate.Select(TematismiDifesa.ColoreLinea).Distinct().Count());
        Assert.DoesNotContain("#a6a6a6", usate.Select(TematismiDifesa.ColoreLinea));
    }

    [Fact]
    public void La_legenda_delle_linee_ha_solo_quelle_presenti_coi_loro_nomi()
    {
        var tema = TematismiDifesa.CreaLinea();
        List<DifesaVignetoDto> righe =
        [
            Difesa(null, id: 1, idLinea: 4, linea: "BIO 4 KG"),
            Difesa(null, id: 2, idLinea: 4, linea: "BIO 4 KG"),
            Difesa(null, id: 3, idLinea: 38, linea: "TESTUGGINE"),
            Difesa(null, id: 4),
        ];

        var classificazione = TematismiBigot.ClassiPerVigneto(tema, righe, null);
        var conteggi = TematismiBigot.Conteggi(classificazione, [1, 2, 3, 4]);
        var voci = LegendaTematismo.Voci(tema, null, null, conteggi, null, TematismiDifesa.NomiLinee(righe));

        Assert.Equal(["Nessuna linea assegnata", "BIO 4 KG", "TESTUGGINE"], voci.Select(v => v.Testo));
        Assert.Equal([1, 2, 1], voci.Select(v => v.Conteggio));
        Assert.Equal([1, 2], TematismiBigot.FiltraPerClasse([1, 2, 3, 4], id => id, classificazione, 4));
    }

    [Fact]
    public void I_nomi_delle_linee_non_cambiano_la_legenda_dell_ultimo_trattamento()
    {
        var tema = TematismiDifesa.CreaUltimoTrattamento(() => Oggi);
        var nomi = new Dictionary<int, string> { [0] = "Nessuna linea assegnata", [3] = "SOFT" };

        var voci = LegendaTematismo.Voci(tema, null, null, new Dictionary<int, int> { [0] = 1, [3] = 1 }, null, nomi);

        Assert.Equal(tema.Classi.Select(c => c.Testo), voci.Select(v => v.Testo));
    }

    // --- Esportazione ---

    [Fact]
    public void L_esportazione_ha_le_colonne_tratt()
    {
        var vigneto = new Vigneto { Id = 1, AziendaId = 1, Nome = "Vigneto 1" };
        var esportato = new VignetoEsportato(vigneto, "Azienda", null, null,
            Trattamento: new UltimoTrattamento(1, 1, new DateTime(2026, 7, 3, 12, 0, 0), ["RAME", "ZOLFO"], ["CYPRUS"], ["Oidio", "Peronospora"]),
            GiorniDalTrattamento: 102, Linea: new LineaDifesaVigneto(1, 1, 4, "BIO 4 KG", "ALTO / BASSO"));
        var vuoto = new VignetoEsportato(vigneto, "Azienda", null, null, Linea: new LineaDifesaVigneto(1, 1, null, null, null));

        object? Campo(VignetoEsportato v, string nome) => EsportazioneVigneti.Campi.Single(c => c.Nome == nome).Valore(v);
        string[] colonne = ["tratt_data", "tratt_gg", "tratt_pa", "tratt_prod", "tratt_avv", "tratt_lin", "tratt_risc"];

        Assert.Equal("2026-07-03", Campo(esportato, "tratt_data"));
        Assert.Equal(102, Campo(esportato, "tratt_gg"));
        Assert.Equal("RAME; ZOLFO", Campo(esportato, "tratt_pa"));
        Assert.Equal("CYPRUS", Campo(esportato, "tratt_prod"));
        Assert.Equal("Oidio; Peronospora", Campo(esportato, "tratt_avv"));
        Assert.Equal("BIO 4 KG", Campo(esportato, "tratt_lin"));
        Assert.Equal("ALTO / BASSO", Campo(esportato, "tratt_risc"));
        Assert.All(colonne, c => Assert.Null(Campo(vuoto, c)));
        Assert.All(EsportazioneVigneti.Campi, c => Assert.True(c.Nome.Length <= 10, c.Nome));
    }

    [Fact]
    public async Task L_endpoint_di_esportazione_mette_trattamento_e_linea()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(), new InMemoryAziendeRepository(),
            new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var file = Assert.IsType<FileContentResult>(await controller.Esporta(1, "geojson", null, "1", CancellationToken.None));

        using var documento = JsonDocument.Parse(file.FileContents);
        var proprieta = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray(), f => f.TryGetProperty("id", out _)).GetProperty("properties");
        Assert.Equal("RAME (IDROSSIDO)", proprieta.GetProperty("tratt_pa").GetString());
        Assert.Equal("BIO 4 KG", proprieta.GetProperty("tratt_lin").GetString());
        Assert.InRange(proprieta.GetProperty("tratt_gg").GetInt32(), 3, 4);
    }

    [Fact]
    public async Task Se_i_trattamenti_non_si_leggono_l_esportazione_esce_lo_stesso()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(difesa: new RepositoryContato { Rotto = true }),
            new InMemoryAziendeRepository(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var file = Assert.IsType<FileContentResult>(await controller.Esporta(1, "geojson", null, "1", CancellationToken.None));

        using var documento = JsonDocument.Parse(file.FileContents);
        var proprieta = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray(), f => f.TryGetProperty("id", out _)).GetProperty("properties");
        Assert.Equal(JsonValueKind.Null, proprieta.GetProperty("tratt_lin").ValueKind);
    }

    private static DifesaVignetoDto Difesa(DateTime? data, IReadOnlyList<string>? principi = null, IReadOnlyList<string>? prodotti = null,
        IReadOnlyList<string>? avversita = null, int? idLinea = null, string? linea = null, int id = 1) =>
        new(id, data, principi ?? [], prodotti ?? [], avversita ?? [], idLinea, linea, null);

    private static DifesaController Controller(PerimetroAziende? perimetro = null) =>
        new(CacheVignetiAziendeTests.Cache(), perimetro ?? PerimetroAziendeTests.Perimetro(), NullLogger<DifesaController>.Instance);

    private static DifesaAziendaDto Valore(ActionResult<DifesaAziendaDto> risultato) =>
        Assert.IsType<DifesaAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

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

    /// <summary>Difesa in-memory che conta le letture; <see cref="Rotto"/> = 4Grapes non risponde.</summary>
    private sealed class RepositoryContato : IDifesaRepository
    {
        public bool Rotto { get; set; }

        public int Letture { get; private set; }

        public Task<DifesaLetta> GetAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
        {
            Letture++;
            return Rotto
                ? Task.FromException<DifesaLetta>(new InvalidOperationException("4Grapes non risponde"))
                : new InMemoryDifesaRepository().GetAsync(aziende, ct);
        }
    }
}
