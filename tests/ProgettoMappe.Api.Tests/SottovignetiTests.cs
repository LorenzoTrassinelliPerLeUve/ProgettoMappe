using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Api.GeoJson;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Formati;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Sottovigneti (task #66): lettura delle righe di V_SubVigneto, GeoJSON per la mappa, endpoint e
/// cache, strato a parte nell'esportazione. Tutto con dati finti.
/// </summary>
public class SottovignetiTests
{
    private const string Quadrato = "POLYGON ((11 43, 11.001 43, 11.001 43.001, 11 43.001, 11 43))";

    // --- Lettura da 4Grapes ---

    [Fact]
    public void La_superficie_in_metri_quadri_diventa_ettari()
    {
        var s = Leggi(("IdSubVigneto", 5), ("Nome", " Filare 1 "), ("IdVigneto", 7), ("Superficie", 9000m),
            ("PoligonoWkt", Quadrato), ("AreaPoligonoMq", 9000.0), out var scartata);

        Assert.NotNull(s);
        Assert.False(scartata);
        Assert.Equal((5, 7, "Filare 1", 0.9m), (s.Id, s.IdVigneto, s.Nome, s.SuperficieEttari));
        Assert.Contains("\"Polygon\"", s.GeometriaGeoJson);
    }

    [Fact]
    public void Una_superficie_gia_in_ettari_si_riconosce_dal_poligono()
    {
        var s = Leggi(("IdSubVigneto", 5), ("Nome", "A"), ("IdVigneto", 7), ("Superficie", 0.9m),
            ("PoligonoWkt", Quadrato), ("AreaPoligonoMq", 9000.0), out _);

        Assert.Equal(0.9m, s!.SuperficieEttari);
    }

    [Fact]
    public void Una_superficie_incoerente_col_poligono_resta_mancante()
    {
        var s = Leggi(("IdSubVigneto", 5), ("Nome", "A"), ("IdVigneto", 7), ("Superficie", 500m),
            ("PoligonoWkt", Quadrato), ("AreaPoligonoMq", 9000.0), out _);

        Assert.Null(s!.SuperficieEttari);
        Assert.NotNull(s.GeometriaGeoJson);
    }

    [Fact]
    public void Un_poligono_illeggibile_o_enorme_lascia_il_sottovigneto_senza_forma()
    {
        var rotto = Leggi(("IdSubVigneto", 5), ("Nome", "A"), ("IdVigneto", 7), ("Superficie", 9000m),
            ("PoligonoWkt", "POLYGON ((rotto"), ("AreaPoligonoMq", 9000.0), out var scartataRotto);
        var enorme = Leggi(("IdSubVigneto", 6), ("Nome", "B"), ("IdVigneto", 7), ("Superficie", 9000m),
            ("PoligonoWkt", Quadrato), ("AreaPoligonoMq", 600.0 * 10_000), out var scartataEnorme);

        Assert.True(scartataRotto);
        Assert.Null(rotto!.GeometriaGeoJson);
        Assert.Equal(0.9m, rotto.SuperficieEttari);
        Assert.True(scartataEnorme);
        Assert.Null(enorme!.GeometriaGeoJson);
        Assert.Equal(0.9m, enorme.SuperficieEttari); // senza poligono attendibile: assunta in m²
    }

    [Fact]
    public void Righe_senza_id_si_saltano_e_il_nome_mancante_si_compone()
    {
        Assert.Null(Leggi(("IdSubVigneto", null), ("Nome", "A"), ("IdVigneto", 7), ("Superficie", null),
            ("PoligonoWkt", null), ("AreaPoligonoMq", null), out _));
        Assert.Null(Leggi(("IdSubVigneto", 5), ("Nome", "A"), ("IdVigneto", "x"), ("Superficie", null),
            ("PoligonoWkt", null), ("AreaPoligonoMq", null), out _));

        var senzaNome = Leggi(("IdSubVigneto", 5), ("Nome", "  "), ("IdVigneto", 7), ("Superficie", null),
            ("PoligonoWkt", null), ("AreaPoligonoMq", null), out var scartata);
        Assert.Equal("Sottovigneto 5", senzaNome!.Nome);
        Assert.Null(senzaNome.SuperficieEttari);
        Assert.False(scartata);
    }

    // --- GeoJSON e endpoint ---

    [Fact]
    public void Il_GeoJSON_dei_sottovigneti_porta_vigneto_e_nome_e_salta_quelli_senza_forma()
    {
        var geojson = VignetoGeoJsonBuilder.BuildSottovigneti(
        [
            ..DiProva(),
            new Sottovigneto { Id = 9, IdVigneto = 1, Nome = "Senza forma" },
        ]);

        var features = geojson["features"]!.AsArray();
        Assert.Equal(2, features.Count);
        Assert.Equal(1001, features[0]!["id"]!.GetValue<int>());
        Assert.Equal(1, features[0]!["properties"]!["idVigneto"]!.GetValue<int>());
        Assert.Equal("Poggio Nord - ovest (dato di prova)", features[0]!["properties"]!["nome"]!.GetValue<string>());
    }

    [Fact]
    public void L_elenco_e_per_vigneto_e_per_nome_con_i_numeri_in_ordine()
    {
        var dto = SottovignetiController.Componi(
        [
            new Sottovigneto { Id = 1, IdVigneto = 2, Nome = "Filare 10", GeometriaGeoJson = """{"type":"Point","coordinates":[11,43]}""" },
            new Sottovigneto { Id = 2, IdVigneto = 2, Nome = "filare 2" },
            new Sottovigneto { Id = 3, IdVigneto = 1, Nome = "Z" },
        ]);

        Assert.True(dto.Pronti);
        Assert.Equal(["Z", "filare 2", "Filare 10"], dto.Sottovigneti.Select(s => s.Nome));
        Assert.Equal([false, false, true], dto.Sottovigneti.Select(s => s.HaGeometria));
    }

    [Fact]
    public async Task L_endpoint_da_i_sottovigneti_dell_azienda_e_niente_fuori_perimetro()
    {
        var dentro = Valore(await Controller().GetSottovigneti(1, CancellationToken.None));
        var altra = Valore(await Controller().GetSottovigneti(2, CancellationToken.None));
        var fuori = Valore(await Controller(PerimetroAziendeTests.Perimetro(2)).GetSottovigneti(1, CancellationToken.None));

        Assert.Equal([1002, 1001], dentro.Sottovigneti.Select(s => s.Id)); // «est» prima di «ovest»
        Assert.Equal(2, dentro.GeoJson!.Value.GetProperty("features").GetArrayLength());
        Assert.Empty(altra.Sottovigneti);
        Assert.Empty(fuori.Sottovigneti);
    }

    [Fact]
    public async Task Se_4Grapes_non_risponde_l_endpoint_da_503_e_la_cache_non_tiene_l_errore()
    {
        var repository = new RepositoryContato { Rotto = true };
        var controller = new SottovignetiController(CacheVignetiAziendeTests.Cache(sottovigneti: repository),
            PerimetroAziendeTests.Perimetro(), NullLogger<SottovignetiController>.Instance);

        var risultato = await controller.GetSottovigneti(1, CancellationToken.None);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<StatusCodeResult>(risultato.Result).StatusCode);

        repository.Rotto = false;
        Assert.Equal(2, Valore(await controller.GetSottovigneti(1, CancellationToken.None)).Sottovigneti.Count);
    }

    [Fact]
    public async Task Mappa_ed_esportazione_della_stessa_azienda_fanno_una_lettura_sola()
    {
        var repository = new RepositoryContato();
        var cache = CacheVignetiAziendeTests.Cache(sottovigneti: repository);

        await cache.GetSottovignetiPerAziendaAsync(1, CancellationToken.None);
        await cache.GetSottovignetiPerAziendaAsync(1, CancellationToken.None);

        Assert.Equal(1, repository.Letture);
    }

    // --- Esportazione ---

    [Fact]
    public void I_campi_dei_sottovigneti_stanno_nel_dbf()
    {
        Assert.All(EsportazioneVigneti.CampiSottovigneti, c => Assert.True(c.Nome.Length <= 10, c.Nome));
        Assert.Equal(EsportazioneVigneti.CampiSottovigneti.Count, EsportazioneVigneti.CampiSottovigneti.Select(c => c.Nome).Distinct().Count());
    }

    [Fact]
    public void Nel_GeoJSON_i_sottovigneti_seguono_i_vigneti_senza_id()
    {
        using var documento = JsonDocument.Parse(EsportazioneVigneti.GeoJson([Vigneto()], Sottovigneti()));

        var features = documento.RootElement.GetProperty("features").EnumerateArray().ToList();
        Assert.Equal(3, features.Count);
        Assert.Equal(1, features[0].GetProperty("id").GetInt32());
        var sotto = features[1];
        Assert.False(sotto.TryGetProperty("id", out _));
        var p = sotto.GetProperty("properties");
        Assert.Equal(EsportazioneVigneti.CampiSottovigneti.Select(c => c.Nome), p.EnumerateObject().Select(o => o.Name));
        Assert.Equal("sottovigneto", p.GetProperty("geometria").GetString());
        Assert.Equal(1001, p.GetProperty("id").GetInt32());
        Assert.Equal(1, p.GetProperty("id_vigneto").GetInt32());
        Assert.Equal("Vigneto Poggio Nord (dato di prova)", p.GetProperty("vigneto").GetString());
        Assert.Equal(1.6m, p.GetProperty("sup_ha").GetDecimal());
    }

    [Fact]
    public void Nel_KML_i_sottovigneti_sono_una_cartella_a_parte()
    {
        XNamespace kml = "http://www.opengis.net/kml/2.2";
        var documento = XDocument.Parse(Encoding.UTF8.GetString(KmlVigneti.Scrivi("x", [Vigneto()], Sottovigneti())));

        var cartelle = documento.Descendants(kml + "Folder").ToList();
        Assert.Equal(["Vigneti", "Sottovigneti"], cartelle.Select(c => c.Element(kml + "name")!.Value));
        var segnaposti = cartelle[1].Elements(kml + "Placemark").ToList();
        Assert.Equal(["sottovigneto-1001", "sottovigneto-1002"], segnaposti.Select(s => s.Attribute("id")!.Value));
        Assert.Equal("#sottovigneto", segnaposti[0].Element(kml + "styleUrl")!.Value);

        // Senza sottovigneti la cartella non c'è.
        var senza = XDocument.Parse(Encoding.UTF8.GetString(KmlVigneti.Scrivi("x", [Vigneto()])));
        Assert.DoesNotContain(senza.Descendants(kml + "Folder"), c => c.Element(kml + "name")!.Value == "Sottovigneti");
    }

    [Fact]
    public void Nello_zip_i_sottovigneti_sono_un_terzo_shapefile()
    {
        using var archivio = new ZipArchive(new MemoryStream(ShapefileVigneti.Scrivi("cantina", [Vigneto()], Sottovigneti())));

        var nomi = archivio.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("cantina.shp", nomi);
        Assert.Equal(["cantina-sottovigneti.cpg", "cantina-sottovigneti.dbf", "cantina-sottovigneti.prj", "cantina-sottovigneti.shp", "cantina-sottovigneti.shx"],
            nomi.Where(n => n.StartsWith("cantina-sottovigneti.", StringComparison.Ordinal)).Order(StringComparer.Ordinal));

        using var senza = new ZipArchive(new MemoryStream(ShapefileVigneti.Scrivi("cantina", [Vigneto()])));
        Assert.DoesNotContain(senza.Entries, e => e.FullName.Contains("sottovigneti", StringComparison.Ordinal));
    }

    [Fact]
    public async Task L_endpoint_di_esportazione_mette_solo_i_sottovigneti_dei_vigneti_scelti()
    {
        var controller = new EsportazioneController(CacheVignetiAziendeTests.Cache(), new InMemoryAziendeRepository(),
            new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        Assert.Equal(2, await SottovignetiNelFile(await controller.Esporta(1, "geojson", null, "1", CancellationToken.None)));
        Assert.Equal(0, await SottovignetiNelFile(await controller.Esporta(1, "geojson", null, "2", CancellationToken.None)));

        // Se i sottovigneti non si leggono il file esce lo stesso, con i soli vigneti.
        var rotto = new EsportazioneController(CacheVignetiAziendeTests.Cache(sottovigneti: new RepositoryContato { Rotto = true }),
            new InMemoryAziendeRepository(), new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());
        Assert.Equal(0, await SottovignetiNelFile(await rotto.Esporta(1, "geojson", null, null, CancellationToken.None)));

        static Task<int> SottovignetiNelFile(IActionResult risultato)
        {
            using var documento = JsonDocument.Parse(Assert.IsType<FileContentResult>(risultato).FileContents);
            return Task.FromResult(documento.RootElement.GetProperty("features").EnumerateArray()
                .Count(f => f.GetProperty("properties").GetProperty("geometria").GetString() == "sottovigneto"));
        }
    }

    // --- Scheda ---

    [Theory]
    [InlineData(null, "–")]
    [InlineData("0.0004", "< 0,01 ha")]
    [InlineData("0.006", "0,01 ha")]
    [InlineData("1.25", "1,25 ha")]
    public void La_superficie_breve_non_sparisce_arrotondando(string? ettari, string atteso) =>
        Assert.Equal(atteso, FormatoSuperficie.Breve(ettari is null ? null : decimal.Parse(ettari, System.Globalization.CultureInfo.InvariantCulture)));

    // --- Aiuti ---

    private static VignetoEsportato Vigneto() =>
        new(new InMemoryVignetiRepository().GetVignetoAsync(1).GetAwaiter().GetResult()!, "Tenuta", null, null);

    /// <summary>I due sottovigneti di prova del vigneto 1 (azienda 1).</summary>
    private static IReadOnlyList<Sottovigneto> DiProva() =>
        new InMemorySottovignetiRepository().GetPerAziendaAsync(1).GetAwaiter().GetResult();

    private static List<SottovignetoEsportato> Sottovigneti() =>
        DiProva().Select(s => new SottovignetoEsportato(s, "Tenuta", Vigneto().Vigneto.Nome)).ToList();

    private static Sottovigneto? Leggi((string, object?) a, (string, object?) b, (string, object?) c, (string, object?) d,
        (string, object?) e, (string, object?) f, out bool scartata)
    {
        var tabella = new DataTable();
        (string Colonna, object? Valore)[] valori = [a, b, c, d, e, f];
        foreach (var (colonna, _) in valori)
        {
            tabella.Columns.Add(colonna, typeof(object));
        }

        tabella.Rows.Add(valori.Select(v => v.Valore ?? DBNull.Value).ToArray());
        using var reader = tabella.CreateDataReader();
        reader.Read();
        return FourGrapesSottovignetiRepository.LeggiSottovigneto(reader, out scartata);
    }

    private static SottovignetiController Controller(PerimetroAziende? perimetro = null) =>
        new(CacheVignetiAziendeTests.Cache(), perimetro ?? PerimetroAziendeTests.Perimetro(), NullLogger<SottovignetiController>.Instance);

    private static SottovignetiAziendaDto Valore(ActionResult<SottovignetiAziendaDto> risultato) =>
        Assert.IsType<SottovignetiAziendaDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

    /// <summary>Sottovigneti in-memory che contano le letture; <see cref="Rotto"/> = 4Grapes non risponde.</summary>
    private sealed class RepositoryContato : ISottovignetiRepository
    {
        public bool Rotto { get; set; }

        public int Letture { get; private set; }

        public Task<IReadOnlyList<Sottovigneto>> GetPerAziendaAsync(int aziendaId, CancellationToken ct = default)
        {
            Letture++;
            return Rotto
                ? Task.FromException<IReadOnlyList<Sottovigneto>>(new InvalidOperationException("4Grapes non risponde"))
                : new InMemorySottovignetiRepository().GetPerAziendaAsync(aziendaId, ct);
        }
    }
}
