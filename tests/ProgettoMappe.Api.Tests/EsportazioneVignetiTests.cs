using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Tematismi;
using Xunit;
using WebEsportazione = ProgettoMappe.Web.Services.EsportazioneVigneti;

namespace ProgettoMappe.Api.Tests;

public class EsportazioneVignetiTests
{
    private const string Quadrato = """{"type":"Polygon","coordinates":[[[11.0,43.0],[11.01,43.0],[11.01,43.01],[11.0,43.01],[11.0,43.0]]]}""";

    // Due parti; la seconda ha l'anello aperto e antiorario: va chiuso e girato.
    private const string DueParti = """{"type":"MultiPolygon","coordinates":[[[[12.0,44.0],[12.0,44.01],[12.01,44.01],[12.01,44.0],[12.0,44.0]]],[[[12.1,44.1],[12.11,44.1],[12.11,44.11],[12.1,44.11]]]]}""";

    private static readonly IndiceBigotVigneto Indice2025 =
        new(2025, 1, 9, 84, 223, "MOLTO ALTA", 3.3m, 1.9m, 1.7m, 21, "MEDIO COMPATTO", 99.9m, "ASSENTE", 1m);

    private static List<VignetoEsportato> Vigneti() =>
    [
        new(new Vigneto { Id = 1, Nome = "Poggio <Nord> & Sud", NomeVigna = "Vigna Alta", Varieta = "Ribolla gialla",
            SuperficieEttari = 1.2345m, GeometriaGeoJson = Quadrato }, "Cantina Àrmonia", 2025, Indice2025),
        new(new Vigneto { Id = 2, Nome = "Colle", SuperficieEttari = null, GeometriaGeoJson = DueParti }, "Cantina Àrmonia", 2025, null),
        new(new Vigneto { Id = 3, Nome = "Senza poligono" }, "Cantina Àrmonia", 2025, null),
        new(new Vigneto { Id = 4, Nome = "WKT rotto", GeometriaGeoJson = "{non json" }, "Cantina Àrmonia", 2025, null),
    ];

    [Fact]
    public void GeoJson_valido_con_attributi_e_solo_vigneti_con_geometria()
    {
        using var documento = JsonDocument.Parse(EsportazioneVigneti.GeoJson(Vigneti()));
        var radice = documento.RootElement;

        Assert.Equal("FeatureCollection", radice.GetProperty("type").GetString());
        var features = radice.GetProperty("features").EnumerateArray().ToList();
        Assert.Equal([1, 2], features.Select(f => f.GetProperty("id").GetInt32()));

        var primo = features[0];
        Assert.Equal("Feature", primo.GetProperty("type").GetString());
        Assert.Equal("Polygon", primo.GetProperty("geometry").GetProperty("type").GetString());
        var p = primo.GetProperty("properties");
        Assert.Equal(EsportazioneVigneti.Campi.Select(c => c.Nome), p.EnumerateObject().Select(o => o.Name));
        Assert.Equal("Cantina Àrmonia", p.GetProperty("azienda").GetString());
        Assert.Equal("Vigna Alta", p.GetProperty("vigna").GetString());
        Assert.Equal("Poggio <Nord> & Sud", p.GetProperty("vigneto").GetString());
        Assert.Equal("Ribolla gialla", p.GetProperty("vitigno").GetString());
        Assert.Equal(1.2345m, p.GetProperty("sup_ha").GetDecimal());
        Assert.Equal(2025, p.GetProperty("anno_bigot").GetInt32());
        Assert.Equal(84, p.GetProperty("punteggio").GetInt32());
        Assert.Equal("MOLTO ALTA", p.GetProperty("vigoria").GetString());

        // Senza indice nell'anno: i parametri restano vuoti (null), l'anno resta.
        var secondo = features[1].GetProperty("properties");
        Assert.Equal(JsonValueKind.Null, secondo.GetProperty("punteggio").ValueKind);
        Assert.Equal(JsonValueKind.Null, secondo.GetProperty("sup_ha").ValueKind);
    }

    [Fact]
    public void Kml_ha_un_placemark_per_vigneto_con_dati_e_coordinate_lon_lat()
    {
        var kml = XDocument.Parse(Encoding.UTF8.GetString(KmlVigneti.Scrivi("cantina-armonia-20261013", Vigneti())));
        XNamespace ns = "http://www.opengis.net/kml/2.2";

        var placemark = kml.Descendants(ns + "Placemark").ToList();
        Assert.Equal(2, placemark.Count);
        Assert.Equal("Poggio <Nord> & Sud", placemark[0].Element(ns + "name")!.Value);

        var dati = placemark[0].Descendants(ns + "Data")
            .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element(ns + "value")!.Value);
        Assert.Equal("1.2345", dati["sup_ha"]);
        Assert.Equal("84", dati["punteggio"]);
        Assert.Equal("1", dati["biodivers"]);

        var esterno = placemark[0].Descendants(ns + "outerBoundaryIs").Single();
        Assert.StartsWith("11,43 11.01,43", esterno.Value.Trim());

        // MultiPolygon → MultiGeometry con due poligoni.
        var multi = placemark[1].Element(ns + "MultiGeometry");
        Assert.NotNull(multi);
        Assert.Equal(2, multi.Elements(ns + "Polygon").Count());
    }

    [Fact]
    public void Shapefile_zip_si_riapre_con_geometrie_e_attributi()
    {
        var zip = ShapefileVigneti.Scrivi("cantina-armonia-20261013", Vigneti());

        using var archivio = new ZipArchive(new MemoryStream(zip));
        Assert.Equal(
            new[] { ".cpg", ".dbf", ".prj", ".shp", ".shx" },
            archivio.Entries.Select(e => Path.GetExtension(e.Name)).Order());
        Assert.All(archivio.Entries, e => Assert.StartsWith("cantina-armonia-20261013.", e.Name));

        var prj = Leggi(archivio, ".prj");
        Assert.Contains("GCS_WGS_1984", Encoding.ASCII.GetString(prj));

        var features = Shapefile.ReadAllFeatures(new MemoryStream(Leggi(archivio, ".shp")), new MemoryStream(Leggi(archivio, ".dbf")));
        Assert.Equal(2, features.Length);

        var primo = features[0];
        Assert.Equal(1, Convert.ToInt32(primo.Attributes["id"]));
        Assert.Equal("Poggio <Nord> & Sud", primo.Attributes["vigneto"]);
        Assert.Equal("Cantina Àrmonia", primo.Attributes["azienda"]);
        Assert.Equal(1.2345m, Convert.ToDecimal(primo.Attributes["sup_ha"]));
        Assert.Equal(84, Convert.ToInt32(primo.Attributes["punteggio"]));
        Assert.Equal(1e-4, primo.Geometry.Area, 8);

        // La seconda parte aveva l'anello aperto: chiuso, due poligoni, area di entrambi.
        var secondo = Assert.IsType<MultiPolygon>(features[1].Geometry);
        Assert.Equal(2, secondo.NumGeometries);
        Assert.Equal(2e-4, secondo.Area, 8);
        Assert.Null(features[1].Attributes["punteggio"]);
    }

    [Fact]
    public void Shapefile_scrive_anelli_esterni_in_senso_orario()
    {
        var antiorario = """{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]}""";
        var poligoni = ShapefileVigneti.Poligoni(System.Text.Json.Nodes.JsonNode.Parse(antiorario));

        var esterno = ((Polygon)poligoni!.GetGeometryN(0)).ExteriorRing;
        Assert.False(NetTopologySuite.Algorithm.Orientation.IsCCW(esterno.CoordinateSequence));
    }

    private static List<VignetoEsportato> ConPunto() =>
    [
        .. Vigneti(),
        new(new Vigneto { Id = 5, Nome = "Solo punto", SuperficieEttari = 0.8m, SoloPunto = true,
            GeometriaGeoJson = """{"type":"Point","coordinates":[9.25,45.0]}""" }, "Cantina Àrmonia", 2025, null),
    ];

    [Fact]
    public void GeoJson_tiene_i_punti_con_il_campo_geometria()
    {
        using var documento = JsonDocument.Parse(EsportazioneVigneti.GeoJson(ConPunto()));
        var features = documento.RootElement.GetProperty("features").EnumerateArray().ToList();

        Assert.Equal([1, 2, 5], features.Select(f => f.GetProperty("id").GetInt32()));
        Assert.Equal("poligono", features[0].GetProperty("properties").GetProperty("geometria").GetString());
        Assert.Equal("punto", features[2].GetProperty("properties").GetProperty("geometria").GetString());
        Assert.Equal("Point", features[2].GetProperty("geometry").GetProperty("type").GetString());
    }

    [Fact]
    public void Kml_mette_i_punti_in_una_cartella_a_parte()
    {
        var kml = XDocument.Parse(Encoding.UTF8.GetString(KmlVigneti.Scrivi("x", ConPunto())));
        XNamespace ns = "http://www.opengis.net/kml/2.2";

        var cartelle = kml.Descendants(ns + "Folder").ToList();
        Assert.Equal(2, cartelle.Count);
        Assert.Equal(2, cartelle[0].Elements(ns + "Placemark").Count());
        var punto = Assert.Single(cartelle[1].Elements(ns + "Placemark"));
        Assert.Equal("Solo punto", punto.Element(ns + "name")!.Value);
        Assert.Equal("9.25,45", punto.Descendants(ns + "coordinates").Single().Value.Trim());

        // Senza punti, nessuna cartella vuota dei punti.
        var senza = XDocument.Parse(Encoding.UTF8.GetString(KmlVigneti.Scrivi("x", Vigneti())));
        Assert.Single(senza.Descendants(ns + "Folder"));
    }

    [Fact]
    public void Shapefile_scrive_i_punti_in_un_secondo_shapefile()
    {
        using var archivio = new ZipArchive(new MemoryStream(ShapefileVigneti.Scrivi("cantina", ConPunto())));

        Assert.Equal(10, archivio.Entries.Count);
        var poligoni = Shapefile.ReadAllFeatures(new MemoryStream(Leggi(archivio, "cantina.shp")), new MemoryStream(Leggi(archivio, "cantina.dbf")));
        Assert.Equal(2, poligoni.Length);

        var punti = Shapefile.ReadAllFeatures(new MemoryStream(Leggi(archivio, "cantina-punti.shp")), new MemoryStream(Leggi(archivio, "cantina-punti.dbf")));
        var punto = Assert.Single(punti);
        var geometria = Assert.IsType<Point>(punto.Geometry);
        Assert.Equal((9.25, 45.0), (geometria.X, geometria.Y));
        Assert.Equal("Solo punto", punto.Attributes["vigneto"]);
        Assert.Equal("punto", punto.Attributes["geometria"]);
    }

    [Theory]
    [InlineData("Cantina Àrmonia S.r.l.", "cantina-armonia-s-r-l-20261013")]
    [InlineData("  ##  ", "azienda-7-20261013")]
    public void Nome_file_senza_accenti_ne_spazi(string azienda, string atteso)
    {
        Assert.Equal(atteso, EsportazioneVigneti.NomeBase(azienda, 7, new DateOnly(2026, 10, 13)));
    }

    [Fact]
    public async Task Endpoint_restituisce_il_file_dei_vigneti_scelti()
    {
        var controller = Controller();

        var risultato = await controller.Esporta(1, "geojson", 2025, "2", CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(risultato);
        Assert.Equal("application/geo+json", file.ContentType);
        Assert.Equal("tenuta-di-esempio-dato-di-prova-20261013.geojson", file.FileDownloadName);
        using var documento = JsonDocument.Parse(file.FileContents);
        var feature = Assert.Single(documento.RootElement.GetProperty("features").EnumerateArray());
        Assert.Equal(2, feature.GetProperty("id").GetInt32());
        Assert.Equal(61, feature.GetProperty("properties").GetProperty("punteggio").GetInt32());
    }

    [Fact]
    public async Task Endpoint_shapefile_e_kml_hanno_tipo_e_estensione_giusti()
    {
        var shp = Assert.IsType<FileContentResult>(await Controller().Esporta(1, "SHP", null, null, CancellationToken.None));
        Assert.Equal("application/zip", shp.ContentType);
        Assert.EndsWith(".zip", shp.FileDownloadName);

        var kml = Assert.IsType<FileContentResult>(await Controller().Esporta(1, "kml", null, null, CancellationToken.None));
        Assert.EndsWith(".kml", kml.FileDownloadName);
        Assert.Equal(2, XDocument.Parse(Encoding.UTF8.GetString(kml.FileContents)).Descendants(XName.Get("Placemark", "http://www.opengis.net/kml/2.2")).Count());
    }

    [Fact]
    public async Task Endpoint_rifiuta_formati_e_ids_sbagliati_e_aziende_fuori_perimetro()
    {
        Assert.IsType<BadRequestObjectResult>(await Controller().Esporta(1, "dxf", null, null, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await Controller().Esporta(1, "kml", null, "1,due", CancellationToken.None));
        Assert.IsType<NotFoundResult>(await Controller().Esporta(999, "kml", null, null, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await Controller(PerimetroAziendeTests.Perimetro(2)).Esporta(1, "kml", null, null, CancellationToken.None));
    }

    [Fact]
    public void Web_inoltra_solo_parametri_controllati()
    {
        Assert.Equal("api/aziende/5/vigneti/esporta?formato=kml&anno=2025&ids=1,-2,3",
            WebEsportazione.IndirizzoApi(5, "kml", "2025", "1,-2,3"));
        Assert.Equal("api/aziende/5/vigneti/esporta?formato=shp", WebEsportazione.IndirizzoApi(5, "shp", null, null));
        Assert.Equal("api/aziende/5/vigneti/esporta?formato=geojson&ids=,", WebEsportazione.IndirizzoApi(5, "geojson", "", ""));
        Assert.Null(WebEsportazione.IndirizzoApi(5, "exe", null, null));
        Assert.Null(WebEsportazione.IndirizzoApi(5, "kml", "2025&x=1", null));
        Assert.Null(WebEsportazione.IndirizzoApi(5, "kml", null, "1;drop"));

        Assert.Equal("esporta/5?formato=shp&anno=2024&ids=1,2", WebEsportazione.Link(5, "shp", 2024, [1, 2]));
        Assert.Equal("esporta/5?formato=kml", WebEsportazione.Link(5, "kml", null, null));
        Assert.Equal("esporta/5?formato=kml&anno=2025&da=2023&tema=punteggio", WebEsportazione.Link(5, "kml", 2025, null, 2023, "punteggio"));
    }

    [Fact]
    public void Il_confronto_del_link_si_controlla_e_diventa_una_colonna_di_variazione()
    {
        var catalogo = new CatalogoTematismi([.. TematismiBigot.Tutti]);

        Assert.True(WebEsportazione.TryRichiestaConfronto(catalogo, null, "2025", null, out var nessuno));
        Assert.Null(nessuno);
        Assert.True(WebEsportazione.TryRichiestaConfronto(catalogo, "punteggio", "2025", "2023", out var richiesta));
        Assert.Equal((2023, 2025), (richiesta!.AnnoDa, richiesta.Anno));
        Assert.False(WebEsportazione.TryRichiestaConfronto(catalogo, "inventato", "2025", "2023", out _));
        Assert.False(WebEsportazione.TryRichiestaConfronto(catalogo, "punteggio", "2025", "2025", out _));
        Assert.False(WebEsportazione.TryRichiestaConfronto(catalogo, "punteggio", null, "2023", out _));
        Assert.False(WebEsportazione.TryRichiestaConfronto(catalogo, "punteggio", "2025", "-1", out _));

        ProgettoMappe.Contracts.IndiceBigotDto[] righe =
        [
            new(2023, 1, 9, 70, 223, null, null, null, null, null, null, null, null, null),
            new(2025, 1, 9, 58, 223, null, null, null, null, null, null, null, null, null),
        ];
        var tema = TematismiBigot.Trova("punteggio")!;
        var confronto = WebEsportazione.Confronto(tema, catalogo.Regola(tema), righe, 2023, 2025);

        var vigneto = Vigneti()[0] with { Confronto = confronto, Variazione = confronto.Vigneti.Single() };
        object? Campo(string nome) => EsportazioneVigneti.Campi.Single(c => c.Nome == nome).Valore(vigneto);
        Assert.Equal("Indice BIGOT (punteggio)", Campo("conf_param"));
        Assert.Equal(2023, Campo("anno_da"));
        Assert.Equal(-12m, Campo("variaz"));
        Assert.Equal("Molto peggiorato", Campo("var_classe"));
        Assert.Null(EsportazioneVigneti.Campi.Single(c => c.Nome == "variaz").Valore(Vigneti()[1]));
    }

    private static EsportazioneController Controller(ProgettoMappe.Api.Servizi.PerimetroAziende? perimetro = null) =>
        new(CacheVignetiAziendeTests.Cache(), new InMemoryAziendeRepository(), new InMemoryIndiciBigotRepository(),
            perimetro ?? PerimetroAziendeTests.Perimetro(), new OrologioFisso(new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero)));

    private static byte[] Leggi(ZipArchive archivio, string estensione)
    {
        using var flusso = archivio.Entries.Single(e => e.Name.EndsWith(estensione)).Open();
        using var copia = new MemoryStream();
        flusso.CopyTo(copia);
        return copia.ToArray();
    }

    private sealed class OrologioFisso(DateTimeOffset adesso) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => adesso;
    }
}
