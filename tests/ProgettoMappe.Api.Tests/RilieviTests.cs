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
/// Ultimo rilievo per vigneto (task #45): lettura delle righe, autore, endpoint e cache, tematismo
/// (classi per età, scheda, legenda) ed esportazione. Tutto con dati finti.
/// </summary>
public class RilieviTests
{
    private static readonly DateOnly Oggi = new(2026, 10, 13);

    // --- Lettura da 4Grapes ---

    [Theory]
    [InlineData("Mario", "Broggi", "M. Broggi")]
    [InlineData("  maria luisa ", " Rossi ", "M. Rossi")]
    [InlineData(null, "Broggi", "Broggi")]
    [InlineData("mario", "broggi", "M. Broggi")]
    [InlineData("MARIO", "DE LUCA", "M. De Luca")]
    [InlineData("Mario", "De Luca", "M. De Luca")]
    [InlineData("Mario", "McBride", "M. McBride")]
    [InlineData("anna", null, "Anna")]
    [InlineData("Mario", "  ", "Mario")]
    [InlineData(null, null, null)]
    public void L_autore_e_iniziale_del_nome_e_cognome(string? nome, string? cognome, string? atteso) =>
        Assert.Equal(atteso, FourGrapesRilieviRepository.Autore(nome, cognome));

    [Fact]
    public void Una_riga_si_legge_con_l_IdRilievo_com_e()
    {
        var idRilievo = new DateTime(2026, 10, 10, 8, 30, 15).AddTicks(1234567);
        var riga = FourGrapesRilieviRepository.LeggiRiga(Record(
            ("IdVigneto", 7), ("IdRilievo", idRilievo), ("InizioRilievo", new DateTime(2026, 10, 10, 8, 30, 0)),
            ("TipoRilevo", " Fenologia "), ("Nome", "Mario"), ("Cognome", "Broggi")));

        Assert.NotNull(riga);
        Assert.Equal(new UltimoRilievo(7, new DateTime(2026, 10, 10, 8, 30, 0), "Fenologia", "M. Broggi", false), riga.Value.Rilievo);
        Assert.Equal(idRilievo, riga.Value.IdRilievo); // tutta la precisione del datetime2(7)
    }

    [Fact]
    public void Una_riga_senza_data_o_senza_rilievo_si_salta()
    {
        Assert.Null(FourGrapesRilieviRepository.LeggiRiga(Record(
            ("IdVigneto", 7), ("IdRilievo", DateTime.Now), ("InizioRilievo", null), ("TipoRilevo", null), ("Nome", null), ("Cognome", null))));
        Assert.Null(FourGrapesRilieviRepository.LeggiRiga(Record(
            ("IdVigneto", 7), ("IdRilievo", null), ("InizioRilievo", DateTime.Now), ("TipoRilevo", null), ("Nome", null), ("Cognome", null))));
    }

    [Fact]
    public void Tipo_e_autore_mancanti_restano_vuoti()
    {
        var riga = FourGrapesRilieviRepository.LeggiRiga(Record(
            ("IdVigneto", 7), ("IdRilievo", 99), ("InizioRilievo", new DateTime(2026, 1, 2)), ("TipoRilevo", null), ("Nome", null), ("Cognome", null)));

        Assert.Equal(new UltimoRilievo(7, new DateTime(2026, 1, 2), null, null, false), riga!.Value.Rilievo);
    }

    // --- Endpoint e cache ---

    [Fact]
    public void Una_riga_per_vigneto_anche_per_i_mai_rilevati()
    {
        var letti = new RilieviLetti(
        [
            new(2, new DateTime(2026, 9, 1), "Sanitario", null, false),
            new(1, new DateTime(2026, 10, 10), "Fenologia", "M. Broggi", true),
            new(1, new DateTime(2026, 10, 1), "Vecchio", null, false),
            new(99, new DateTime(2026, 10, 1), "Di un altro vigneto", null, false),
        ], DateTimeOffset.UnixEpoch);

        var dto = RilieviController.Componi([3, 1, 2], letti);

        Assert.True(dto.Pronto);
        Assert.Equal(DateTimeOffset.UnixEpoch, dto.Aggiornamento);
        Assert.Equal(
        [
            new UltimoRilievoDto(1, new DateTime(2026, 10, 10), "Fenologia", "M. Broggi", true),
            new UltimoRilievoDto(2, new DateTime(2026, 9, 1), "Sanitario", null, false),
            new UltimoRilievoDto(3, null, null, null, false),
        ], dto.Rilievi);
    }

    [Fact]
    public async Task L_endpoint_da_i_rilievi_dei_vigneti_dell_azienda()
    {
        var dto = Valore(await Controller().GetUltimiRilievi(1, CancellationToken.None));

        Assert.True(dto.Pronto);
        Assert.Equal([1, 2], dto.Rilievi.Select(r => r.IdVigneto));
        Assert.True(dto.Rilievi[0].ConFoto);
        Assert.NotNull(dto.Aggiornamento);
    }

    [Fact]
    public async Task Fuori_perimetro_nessun_rilievo()
    {
        var dto = Valore(await Controller(PerimetroAziendeTests.Perimetro(2)).GetUltimiRilievi(1, CancellationToken.None));

        Assert.Empty(dto.Rilievi);
    }

    [Fact]
    public async Task Se_4Grapes_non_risponde_l_endpoint_da_503_e_non_resta_in_cache()
    {
        var rotto = new RepositoryContato { Rotto = true };
        var cache = CacheVignetiAziendeTests.Cache(rilievi: rotto);
        var controller = new RilieviController(cache, PerimetroAziendeTests.Perimetro(), NullLogger<RilieviController>.Instance);

        var risposta = await controller.GetUltimiRilievi(1, CancellationToken.None);
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(risposta.Result).StatusCode);

        rotto.Rotto = false;
        Assert.Equal(2, Valore(await controller.GetUltimiRilievi(1, CancellationToken.None)).Rilievi.Count);
        Assert.Equal(2, rotto.Letture);
    }

    [Fact]
    public async Task I_rilievi_della_stessa_azienda_si_leggono_una_volta_sola()
    {
        var repository = new RepositoryContato();
        var controller = new RilieviController(CacheVignetiAziendeTests.Cache(rilievi: repository), PerimetroAziendeTests.Perimetro(),
            NullLogger<RilieviController>.Instance);

        await controller.GetUltimiRilievi(1, CancellationToken.None);
        await controller.GetUltimiRilievi(1, CancellationToken.None);
        await controller.GetUltimiRilievi(2, CancellationToken.None);

        Assert.Equal(2, repository.Letture);
        Assert.Equal(TimeSpan.FromMinutes(10), CacheVignetiAziende.DurataRilievi);
    }

    // --- Tematismo ---

    [Theory]
    [InlineData(0, TematismiRilievi.ClasseRecente)]
    [InlineData(6, TematismiRilievi.ClasseRecente)]
    [InlineData(7, TematismiRilievi.ClasseMese)]
    [InlineData(29, TematismiRilievi.ClasseMese)]
    [InlineData(30, TematismiRilievi.ClasseScoperto)]
    [InlineData(400, TematismiRilievi.ClasseScoperto)]
    [InlineData(-2, TematismiRilievi.ClasseRecente)] // data futura (dato sporco): conta come oggi
    public void La_classe_dipende_dai_giorni_dall_ultimo_rilievo(int giorniFa, int classe)
    {
        var rilievo = new UltimoRilievoDto(1, Oggi.ToDateTime(new TimeOnly(23, 50)).AddDays(-giorniFa), null, null, false);

        Assert.Equal(classe, TematismiRilievi.Classe(rilievo, Oggi));
    }

    [Fact]
    public void Mai_rilevato_ha_la_sua_classe_e_va_in_cima_all_elenco()
    {
        var tema = TematismiRilievi.Crea(() => Oggi);
        var mai = new UltimoRilievoDto(1, null, null, null, false);
        var vecchio = new UltimoRilievoDto(2, new DateTime(2020, 1, 1), null, null, false);

        Assert.Equal(TematismiRilievi.ClasseMaiRilevato, tema.Classifica(mai));
        Assert.Equal("Mai rilevato", tema.Classi[TematismiRilievi.ClasseMaiRilevato].Etichetta);
        Assert.True(tema.ValoreConfronto(mai) > tema.ValoreConfronto(vecchio));
        Assert.Equal("Mai rilevato", tema.TestoValore(mai));
    }

    [Fact]
    public void Il_tematismo_e_senza_anno_e_non_si_confronta()
    {
        var tema = TematismiRilievi.Crea(() => Oggi);

        Assert.True(tema.SenzaAnno);
        Assert.False(tema.Confrontabile);
        Assert.Equal(4, tema.Classi.Count);
        Assert.Equal("Rilievi in campo", tema.Gruppo);
    }

    [Fact]
    public void Il_catalogo_ha_l_ultimo_rilievo_fuori_dalle_statistiche()
    {
        var catalogo = CatalogoTematismi.Crea(new TematismiOptions(), NullLogger.Instance);

        Assert.NotNull(catalogo.Trova(TematismiRilievi.Id));
        Assert.DoesNotContain(catalogo.Statistiche, t => t.Id == TematismiRilievi.Id);
    }

    [Fact]
    public void La_scheda_dice_tipo_giorno_autore_e_foto()
    {
        Assert.Equal("Fenologia, 10/10 da M. Broggi, con foto",
            TematismiRilievi.TestoScheda(new UltimoRilievoDto(1, new DateTime(2026, 10, 10, 8, 0, 0), "Fenologia", "M. Broggi", true), Oggi));
        Assert.Equal("Sanitario, 03/12/2025",
            TematismiRilievi.TestoScheda(new UltimoRilievoDto(1, new DateTime(2025, 12, 3), "Sanitario", null, false), Oggi));
        Assert.Equal("10/10 da M. Broggi",
            TematismiRilievi.TestoScheda(new UltimoRilievoDto(1, new DateTime(2026, 10, 10), null, "M. Broggi", false), Oggi));
        Assert.Equal("Mai rilevato", TematismiRilievi.TestoScheda(new UltimoRilievoDto(1, null, null, null, false), Oggi));
        Assert.Equal("10/10 · Fenologia",
            TematismiRilievi.TestoElenco(new UltimoRilievoDto(1, new DateTime(2026, 10, 10), "Fenologia", null, false), Oggi));
        Assert.Equal("Peronospora, 03/07 da M. Broggi",
            TematismiRilievi.TestoScheda(new UltimoRilievoDto(1, new DateTime(2026, 7, 3), "PERONOSPORA - Plasmopara viticola", "M. Broggi", false), Oggi));
    }

    [Theory]
    [InlineData("PERONOSPORA - Plasmopara viticola", "03/07 · Peronospora")]
    [InlineData("STATO IDRICO DELLA PIANTA (classi)", "03/07 · Stato idrico")]
    [InlineData("MORFOLOGIA GRAPPOLO", "03/07 · Morfologia grappolo")]
    [InlineData("FASE FENOLOGICA", "03/07 · Fase fenologica")]
    [InlineData("OIDIO Erysiphe Necator - Oidium Tuckeri", "03/07 · Oidio")]
    [InlineData("ERIOFIDE ACARIOSI  - Calepitrimerus vitis", "03/07 · Eriofide acariosi")]
    [InlineData("CICALINA LIBICA - JACOBIASCA LYBICA", "03/07 · Cicalina libica")]
    [InlineData("POTENZIALE IDRICO DELLA PIANTA PREDAWN (MPa)", "03/07 · Potenziale idrico predawn")]
    [InlineData("VIROSI - GLRaV", "03/07 · Virosi GLRaV")]
    [InlineData("FLAVESCENZA_DORATA", "03/07 · Flavescenza dorata")]
    [InlineData("Ragnetto giallo - Eotetranycus carpini", "03/07 · Ragnetto giallo")]
    [InlineData("STATO NUTRIZIONALE e/o CARENZE", "03/07 · Stato nutrizionale e/o carenze")]
    [InlineData("  ", "03/07")]
    [InlineData(null, "03/07")]
    public void Nell_elenco_il_tipo_e_corto(string? tipo, string atteso) =>
        Assert.Equal(atteso, TematismiRilievi.TestoElenco(new UltimoRilievoDto(1, new DateTime(2026, 7, 3), tipo, null, false), Oggi));

    [Fact]
    public void La_legenda_conta_i_vigneti_per_classe()
    {
        var tema = TematismiRilievi.Crea(() => Oggi);
        IReadOnlyList<IRigaVigneto> righe =
        [
            new UltimoRilievoDto(1, new DateTime(2026, 10, 12), "Fenologia", null, false),
            new UltimoRilievoDto(2, new DateTime(2026, 10, 1), "Fenologia", null, false),
            new UltimoRilievoDto(3, new DateTime(2026, 8, 1), "Fenologia", null, false),
            new UltimoRilievoDto(4, new DateTime(2026, 7, 1), "Fenologia", null, false),
            new UltimoRilievoDto(5, null, null, null, false),
        ];

        var classificazione = TematismiBigot.ClassiPerVigneto(tema, righe, null);
        var conteggi = TematismiBigot.Conteggi(classificazione, [1, 2, 3, 4, 5]);
        var voci = LegendaTematismo.Voci(tema, null, null, conteggi, null);

        Assert.Equal([1, 1, 2, 1], voci.Where(v => v.Classe >= 0).Select(v => v.Conteggio));
        Assert.Equal("Scoperto: oltre 30 giorni", voci[TematismiRilievi.ClasseScoperto].Testo);
        Assert.DoesNotContain(voci, v => v.Classe < 0); // ogni vigneto ha la sua riga
        Assert.Equal([3, 4], TematismiBigot.FiltraPerClasse([1, 2, 3, 4, 5], id => id, classificazione, TematismiRilievi.ClasseScoperto));
    }

    // --- Esportazione ---

    [Fact]
    public void L_esportazione_ha_data_giorni_tipo_autore_e_foto()
    {
        var vigneto = new Vigneto { Id = 1, AziendaId = 1, Nome = "Vigneto 1" };
        var esportato = new VignetoEsportato(vigneto, "Azienda", null, null,
            Rilievo: new UltimoRilievo(1, new DateTime(2026, 10, 10, 8, 0, 0), "Fenologia", "M. Broggi", true), GiorniDalRilievo: 3);
        var mai = new VignetoEsportato(vigneto, "Azienda", null, null);

        object? Campo(VignetoEsportato v, string nome) => EsportazioneVigneti.Campi.Single(c => c.Nome == nome).Valore(v);

        Assert.Equal("2026-10-10", Campo(esportato, "ril_data"));
        Assert.Equal(3, Campo(esportato, "ril_giorni"));
        Assert.Equal("Fenologia", Campo(esportato, "ril_tipo"));
        Assert.Equal("M. Broggi", Campo(esportato, "ril_autore"));
        Assert.Equal(1, Campo(esportato, "ril_foto"));
        Assert.All(["ril_data", "ril_giorni", "ril_tipo", "ril_autore", "ril_foto"], c => Assert.Null(Campo(mai, c)));
        Assert.All(EsportazioneVigneti.Campi, c => Assert.True(c.Nome.Length <= 10, c.Nome));
    }

    [Fact]
    public async Task L_endpoint_di_esportazione_mette_l_ultimo_rilievo_anche_senza_anno()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(), new InMemoryAziendeRepository(),
            new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var file = Assert.IsType<FileContentResult>(await controller.Esporta(1, "geojson", null, "1", CancellationToken.None));

        using var documento = JsonDocument.Parse(file.FileContents);
        var proprieta = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray()).GetProperty("properties");
        Assert.Equal("Fenologia", proprieta.GetProperty("ril_tipo").GetString());
        Assert.Equal(1, proprieta.GetProperty("ril_foto").GetInt32());
        // 3 giorni fa nell'ora del server; contati sul giorno italiano, verso mezzanotte possono essere 4.
        Assert.InRange(proprieta.GetProperty("ril_giorni").GetInt32(), 3, 4);
    }

    [Fact]
    public async Task Se_i_rilievi_non_si_leggono_l_esportazione_esce_lo_stesso()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(rilievi: new RepositoryContato { Rotto = true }),
            new InMemoryAziendeRepository(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        var file = Assert.IsType<FileContentResult>(await controller.Esporta(1, "geojson", null, "1", CancellationToken.None));

        using var documento = JsonDocument.Parse(file.FileContents);
        var proprieta = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray()).GetProperty("properties");
        Assert.Equal(JsonValueKind.Null, proprieta.GetProperty("ril_tipo").ValueKind);
    }

    private static RilieviController Controller(PerimetroAziende? perimetro = null) =>
        new(CacheVignetiAziendeTests.Cache(), perimetro ?? PerimetroAziendeTests.Perimetro(), NullLogger<RilieviController>.Instance);

    private static UltimiRilieviAziendaDto Valore(ActionResult<UltimiRilieviAziendaDto> risultato) =>
        Assert.IsType<UltimiRilieviAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

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

    /// <summary>Rilievi in-memory che contano le letture; <see cref="Rotto"/> = 4Grapes non risponde.</summary>
    private sealed class RepositoryContato : IRilieviRepository
    {
        public bool Rotto { get; set; }

        public int Letture { get; private set; }

        public Task<IReadOnlyList<UltimoRilievo>> GetUltimiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
        {
            Letture++;
            return Rotto
                ? Task.FromException<IReadOnlyList<UltimoRilievo>>(new InvalidOperationException("4Grapes non risponde"))
                : new InMemoryRilieviRepository().GetUltimiPerAziendaAsync(aziendaId, ct);
        }
    }
}
