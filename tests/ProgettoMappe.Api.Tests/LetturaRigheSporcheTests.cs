using System.Data;
using ProgettoMappe.Infrastructure.GeoJson;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Letture dei repository 4Grapes su righe con NULL, tipi inattesi e WKT malformati: una riga
/// sporca non deve mai far fallire l'elenco intero. Le righe sono costruite con un DataTable
/// (colonne di tipo object, così si possono mettere anche tipi "sbagliati").
/// </summary>
public class LetturaRigheSporcheTests
{
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

    private static IDataRecord RigaVigneto(object? id = null, object? nome = null, object? codice = null,
        object? wkt = null, object? superficie = null, object? idEnte = null, object? area = null, object? annoImpianto = null,
        object? lat = null, object? lon = null) =>
        Riga(("IdVigneto", id), ("Codice", codice), ("NomeVigneto", nome),
            ("SuperficieMisurata", null), ("SuperficieDichiarata", null), ("SuperficieCalcolataDaGis", null),
            ("Superficie", superficie), ("AnnoImpianto", annoImpianto), ("PoligonoWkt", wkt), ("AreaPoligonoMq", area),
            ("Latitudine", lat), ("Longitudine", lon), ("IdEnte", idEnte), ("IdVigna", null), ("NomeVigna", null), ("IdVitigno", null), ("NomeVitigno", null));

    // --- LetturaSicura ---

    [Fact]
    public void LetturaSicura_NullETipiInattesi_DiventanoNull()
    {
        var riga = Riga(("Nullo", null), ("Testo", "abc"), ("Vuoto", "   "), ("Data", "ieri"), ("Infinito", double.PositiveInfinity));

        Assert.Null(riga.Intero("Nullo"));
        Assert.Null(riga.Intero("Testo"));
        Assert.Null(riga.Decimale("Testo"));
        Assert.Null(riga.Reale("Testo"));
        Assert.Null(riga.Testo("Nullo"));
        Assert.Null(riga.Testo("Vuoto"));
        Assert.Null(riga.Data("Data"));
        Assert.Null(riga.Decimale("Infinito"));
        Assert.Null(riga.Reale("Infinito"));
    }

    [Fact]
    public void LetturaSicura_TipiNumericiCompatibili_SiConvertono()
    {
        var riga = Riga(("Long", 42L), ("Short", (short)7), ("DecimaleIntero", 12m), ("DecimaleConVirgola", 1.5m),
            ("LongEnorme", long.MaxValue), ("Double", 2.25), ("TestoNumero", " 15 "), ("Data", new DateTime(2026, 10, 13, 8, 0, 0)));

        Assert.Equal(42, riga.Intero("Long"));
        Assert.Equal(7, riga.Intero("Short"));
        Assert.Equal(12, riga.Intero("DecimaleIntero"));
        Assert.Null(riga.Intero("DecimaleConVirgola"));
        Assert.Null(riga.Intero("LongEnorme"));
        Assert.Equal(2.25m, riga.Decimale("Double"));
        Assert.Equal(1.5, riga.Reale("DecimaleConVirgola"));
        Assert.Equal(15, riga.Intero("TestoNumero"));
        Assert.Equal(new DateOnly(2026, 10, 13), riga.Data("Data"));
        Assert.Equal("15", riga.Testo("TestoNumero"));
    }

    // --- Aziende ---

    [Fact]
    public void Azienda_SenzaNomi_UsaIlNumero()
    {
        var azienda = FourGrapesAziendeRepository.LeggiAzienda(
            Riga(("IdEnte", 12), ("RagioneSociale", null), ("NomeCommerciale", "  ")));

        Assert.NotNull(azienda);
        Assert.Equal(12, azienda.Id);
        Assert.Equal("Azienda 12", azienda.Nome);
    }

    [Fact]
    public void Azienda_NomeCommercialeVuoto_UsaLaRagioneSociale()
    {
        var azienda = FourGrapesAziendeRepository.LeggiAzienda(
            Riga(("IdEnte", 5), ("RagioneSociale", " Cantina Rossi "), ("NomeCommerciale", "")));

        Assert.Equal("Cantina Rossi", azienda?.Nome);
    }

    [Fact]
    public void Azienda_SenzaIdLeggibile_SiSalta()
    {
        Assert.Null(FourGrapesAziendeRepository.LeggiAzienda(Riga(("IdEnte", null), ("RagioneSociale", "X"), ("NomeCommerciale", null))));
        Assert.Null(FourGrapesAziendeRepository.LeggiAzienda(Riga(("IdEnte", "non un numero"), ("RagioneSociale", "X"), ("NomeCommerciale", null))));
    }

    // --- Vigneti ---

    [Fact]
    public void Vigneto_TuttoNull_TornaConValoriDiRiserva()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 7), out var geometriaNonValida);

        Assert.NotNull(vigneto);
        Assert.False(geometriaNonValida);
        Assert.Equal(7, vigneto.Id);
        Assert.Equal("Vigneto 7", vigneto.Nome);
        Assert.Equal(0, vigneto.AziendaId);
        Assert.Null(vigneto.GeometriaGeoJson);
        Assert.Null(vigneto.SuperficieEttari);
        Assert.Null(vigneto.VignaId);
        Assert.Null(vigneto.Varieta);
    }

    [Fact]
    public void Vigneto_SenzaNome_UsaIlCodice()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 7, codice: " V-07 "), out _);
        Assert.Equal("V-07", vigneto?.Nome);
    }

    [Fact]
    public void Vigneto_SenzaIdLeggibile_SiSalta()
    {
        Assert.Null(FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: null, nome: "X"), out _));
        Assert.Null(FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: "sette", nome: "X"), out _));
    }

    [Fact]
    public void Vigneto_WktRotto_RestaInElencoSenzaGeometria()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 8, nome: "Vigna rotta", wkt: "POLYGON ((13.1 45.9, 13.2 abc, 13.1 45.9))", idEnte: 3),
            out var geometriaNonValida);

        Assert.True(geometriaNonValida);
        Assert.NotNull(vigneto);
        Assert.Equal("Vigna rotta", vigneto.Nome);
        Assert.Equal(3, vigneto.AziendaId);
        Assert.Null(vigneto.GeometriaGeoJson);
    }

    [Fact]
    public void Vigneto_TipiInattesi_NonFannoFallireLaLettura()
    {
        // Superficie come testo, area come testo, IdEnte decimale: niente eccezioni.
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 9L, nome: 123, superficie: "boh", area: "boh", idEnte: 4m,
                wkt: "POLYGON ((13.1 45.9, 13.2 45.9, 13.2 46.0, 13.1 45.9))"),
            out var geometriaNonValida);

        Assert.NotNull(vigneto);
        Assert.False(geometriaNonValida);
        Assert.Equal(9, vigneto.Id);
        Assert.Equal("123", vigneto.Nome);
        Assert.Equal(4, vigneto.AziendaId);
        Assert.Null(vigneto.SuperficieEttari);
        Assert.Contains("\"Polygon\"", vigneto.GeometriaGeoJson);
    }

    // --- Vigneti senza poligono: un punto da Latitudine/Longitudine ---

    [Fact]
    public void Vigneto_SenzaPoligonoConLatLon_DiventaUnPunto()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 11, nome: "Solo punto", lat: 45.012345678901m, lon: 9.123456789012m, superficie: 12_000m),
            out var geometriaNonValida);

        Assert.NotNull(vigneto);
        Assert.False(geometriaNonValida);
        Assert.True(vigneto.SoloPunto);
        Assert.Equal("""{"type":"Point","coordinates":[9.123456789012,45.012345678901]}""", vigneto.GeometriaGeoJson);
        // La superficie si legge come per ogni vigneto senza poligono (m²); i totali la escludono.
        Assert.Equal(1.2m, vigneto.SuperficieEttari);
    }

    [Fact]
    public void Vigneto_ConPoligono_IgnoraLatLon()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 12, wkt: PoligonoQualsiasi, lat: 45m, lon: 9m), out _);

        Assert.False(vigneto?.SoloPunto);
        Assert.Contains("\"Polygon\"", vigneto?.GeometriaGeoJson);
    }

    [Fact]
    public void Vigneto_WktRottoOEnorme_NonDiventaUnPunto()
    {
        var rotto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 13, wkt: "POLYGON ((13.1 abc))", lat: 45m, lon: 9m), out var nonValida);
        var enorme = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 14, wkt: PoligonoQualsiasi, area: 300_000_000.0, lat: 45m, lon: 9m), out _, sogliaEttari: 500);

        Assert.True(nonValida);
        Assert.False(rotto?.SoloPunto);
        Assert.Null(rotto?.GeometriaGeoJson);
        Assert.False(enorme?.SoloPunto);
        Assert.Null(enorme?.GeometriaGeoJson);
    }

    [Theory]
    [InlineData(null, 9.0)]
    [InlineData(45.0, null)]
    [InlineData(0.0, 0.0)]
    [InlineData(91.0, 9.0)]
    [InlineData(45.0, -181.0)]
    [InlineData("boh", 9.0)]
    public void Vigneto_LatLonMancantiOImplausibili_NessunPunto(object? lat, object? lon)
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 15, lat: lat, lon: lon), out _);

        Assert.False(vigneto?.SoloPunto);
        Assert.Null(vigneto?.GeometriaGeoJson);
    }

    // --- Poligoni enormi (QualitaGeometrie) ---

    private const string PoligonoQualsiasi = "POLYGON ((13.1 45.9, 13.2 45.9, 13.2 46.0, 13.1 45.9))";

    [Fact]
    public void Vigneto_PoligonoOltreLaSoglia_InElencoSenzaGeometria()
    {
        // ~30.000 ha come i vigneti 16792/16793; superficie coerente col poligono enorme.
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 16792, nome: "Enorme", wkt: PoligonoQualsiasi, area: 300_000_000.0, superficie: 300_000_000m),
            out var geometriaNonValida, sogliaEttari: 500);

        Assert.NotNull(vigneto);
        Assert.False(geometriaNonValida);
        Assert.True(vigneto.GeometriaNonAttendibile);
        Assert.Null(vigneto.GeometriaGeoJson);
        Assert.Null(vigneto.SuperficieEttari);
    }

    [Fact]
    public void Vigneto_PoligonoOltreLaSoglia_SuperficiePlausibileRestaInM2()
    {
        // Il poligono non fa da controllo: la superficie si legge come senza poligono (m²).
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 16793, wkt: PoligonoQualsiasi, area: 300_000_000.0, superficie: 25_000m),
            out _, sogliaEttari: 500);

        Assert.True(vigneto?.GeometriaNonAttendibile);
        Assert.Equal(2.5m, vigneto?.SuperficieEttari);
    }

    [Fact]
    public void Vigneto_PoligonoSottoLaSoglia_Resta()
    {
        var vigneto = FourGrapesVignetiRepository.LeggiVigneto(
            RigaVigneto(id: 10, wkt: PoligonoQualsiasi, area: 4_000_000.0, superficie: 4_000_000m),
            out _, sogliaEttari: 500);

        Assert.False(vigneto?.GeometriaNonAttendibile);
        Assert.NotNull(vigneto?.GeometriaGeoJson);
        Assert.Equal(400m, vigneto?.SuperficieEttari);
    }

    [Theory]
    [InlineData(5_000_001.0, 500.0, true)]
    [InlineData(5_000_000.0, 500.0, false)]
    [InlineData(300_000_000.0, 0.0, false)]
    [InlineData(300_000_000.0, -1.0, false)]
    [InlineData(double.PositiveInfinity, 500.0, false)]
    [InlineData(double.NaN, 500.0, false)]
    public void QualitaGeometrie_Enorme(double areaMq, double sogliaEttari, bool atteso)
    {
        Assert.Equal(atteso, QualitaGeometrie.Enorme(areaMq, sogliaEttari));
    }

    [Fact]
    public void QualitaGeometrie_AreaMancante_NonEEnorme()
    {
        Assert.False(QualitaGeometrie.Enorme(null, 500));
    }

    // --- Indici BIGOT ---

    private static IDataRecord RigaBigot(object? anno, object? idVigneto, object? punteggio = null, object? vigoria = null) =>
        Riga(("Anno", anno), ("IdVigneto", idVigneto), ("NumParametri", null), ("Punteggio", punteggio), ("Peso", null),
            ("Vigoria", vigoria), ("SFE", null), ("Produzione", "n.d."), ("SFEKg", null), ("EtaVigneto", 1.5m),
            ("Morfologia", null), ("Ampelopatie", null), ("RegimeIdrico", null), ("Biodiversita", null));

    [Fact]
    public void Bigot_ValoriNullOStrani_DiventanoDatiMancanti()
    {
        var riga = CacheIndiciBigot.Leggi(RigaBigot(2025, 10, punteggio: "alto", vigoria: " Media "));

        Assert.NotNull(riga);
        Assert.Equal(2025, riga.Anno);
        Assert.Equal(10, riga.IdVigneto);
        Assert.Null(riga.Punteggio);
        Assert.Null(riga.Produzione);
        Assert.Null(riga.EtaVigneto);
        Assert.Equal("Media", riga.Vigoria);
    }

    [Fact]
    public void Bigot_SenzaAnnoOIdVigneto_SiSalta()
    {
        Assert.Null(CacheIndiciBigot.Leggi(RigaBigot(null, 10)));
        Assert.Null(CacheIndiciBigot.Leggi(RigaBigot(2025, null)));
    }

    // --- WKT malformati ---

    [Theory]
    [InlineData("POLYGON ((13.1 45.9, 13.2 abc, 13.1 45.9))")]
    [InlineData("POLYGON ((13.1 45.9, 13.2 45.9, 13.1 45.9)")]
    [InlineData("POLYGON ((13.1 45.9, 13.2, 13.1 45.9))")]
    [InlineData("POLYGON (())")]
    [InlineData("POLYGON (13.1 45.9, 13.2 45.9)")]
    [InlineData("POINT (NaN 45.9)")]
    [InlineData("POINT (1e400 45.9)")]
    [InlineData("POINT ()")]
    [InlineData("MULTIPOLYGON (((13.1 45.9, 13.2 45.9)), ((")]
    [InlineData("CIRCULARSTRING (0 0, 1 1, 2 0)")]
    [InlineData("testo a caso")]
    public void Wkt_Malformato_NonLanciaESiRiconosce(string wkt)
    {
        Assert.False(WktGeoJsonConverter.TryToGeoJson(wkt, out var geoJson));
        Assert.Null(geoJson);
        Assert.Null(WktGeoJsonConverter.ToGeoJson(wkt));
    }

    [Theory]
    [InlineData(1987, 1987)]
    [InlineData(1900, 1900)]
    [InlineData(2026, 2026)]
    [InlineData(0, null)]
    [InlineData(1899, null)]
    [InlineData(2028, null)]
    [InlineData(null, null)]
    public void AnnoImpianto_SoloAnniPlausibili(int? anno, int? atteso) =>
        Assert.Equal(atteso, FourGrapesVignetiRepository.AnnoImpiantoPlausibile(anno, 2026));

    [Fact]
    public void AnnoImpianto_SporcoNonFermaLaRiga()
    {
        Assert.Equal(1998, FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 1, annoImpianto: (short)1998), out _)?.AnnoImpianto);
        Assert.Null(FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 1, annoImpianto: "boh"), out _)?.AnnoImpianto);
        Assert.Null(FourGrapesVignetiRepository.LeggiVigneto(RigaVigneto(id: 1, annoImpianto: 0), out _)?.AnnoImpianto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("POLYGON EMPTY")]
    public void Wkt_Assente_NonEUnErrore(string? wkt)
    {
        Assert.True(WktGeoJsonConverter.TryToGeoJson(wkt, out var geoJson));
        Assert.Null(geoJson);
    }
}
