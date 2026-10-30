using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Filari;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.Filari;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>Filari (task #95): proiezione UTM, generazione dal primo filare, archivio a versioni ed endpoint.</summary>
public sealed class FilariTests : IDisposable
{
    // Un punto in Friuli (zona UTM 33): i poligoni di prova sono costruiti in metri attorno a lui.
    private static readonly ProiezioneUtm Utm = ProiezioneUtm.Per(13.2, 45.95);
    private static readonly (double E, double N) Origine = Utm.Avanti(13.2, 45.95);

    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "filari-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_cartella))
        {
            Directory.Delete(_cartella, recursive: true);
        }
    }

    /// <summary>(lon, lat) del punto a <paramref name="est"/>, <paramref name="nord"/> metri dall'origine.</summary>
    private static (double Lon, double Lat) Punto(double est, double nord) => Utm.Indietro(Origine.E + est, Origine.N + nord);

    private static (double Est, double Nord) Metri(double lon, double lat)
    {
        var (e, n) = Utm.Avanti(lon, lat);
        return (e - Origine.E, n - Origine.N);
    }

    /// <summary>Polygon GeoJSON dagli anelli in metri (il primo esterno, gli altri buchi).</summary>
    private static JsonNode Poligono(params (double E, double N)[][] anelli) =>
        new JsonObject
        {
            ["type"] = "Polygon",
            ["coordinates"] = new JsonArray(anelli.Select(anello => (JsonNode)new JsonArray(
                anello.Append(anello[0]).Select(p =>
                {
                    var (lon, lat) = Punto(p.E, p.N);
                    return (JsonNode)new JsonArray(lon, lat);
                }).ToArray())).ToArray()),
        };

    private static readonly (double, double)[] Quadrato = [(0, 0), (100, 0), (100, 100), (0, 100)];

    [Fact]
    public void Utm_sul_meridiano_centrale_e_andata_e_ritorno()
    {
        // 45° N sul meridiano centrale della zona 32 (9° E): est 500.000, nord = arco di meridiano × 0,9996.
        var zona32 = new ProiezioneUtm(32);
        var (est, nord) = zona32.Avanti(9, 45);
        Assert.Equal(500000, est, 3);
        Assert.Equal(4982950.4, nord, 0);

        Assert.Equal(33, ProiezioneUtm.ZonaDi(13.2));
        var (lon, lat) = Utm.Indietro(Utm.Avanti(13.71, 46.12).Est, Utm.Avanti(13.71, 46.12).Nord);
        Assert.Equal(13.71, lon, 7);
        Assert.Equal(46.12, lat, 7);
    }

    [Fact]
    public void Quadrato_di_cento_metri_con_interfila_2_5_ha_40_filari_da_100_metri()
    {
        // Primo filare verso nord, a 1 m dal lato ovest.
        var calcolati = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0);

        Assert.Equal(40, calcolati.NumFilari);
        Assert.Equal(4000, calcolati.LunghezzaM, 1);
        Assert.Equal(0, calcolati.AzimutGradi!.Value, 1);
        foreach (var f in calcolati.Filari)
        {
            var (e1, n1) = Metri(f.Lon1, f.Lat1);
            var (e2, n2) = Metri(f.Lon2, f.Lat2);
            Assert.Equal(e1, e2, 2);
            // Ogni filare dista dal primo un multiplo dell'interfila, e va da un lato all'altro nel verso del primo.
            var passi = (e1 - 1) / 2.5;
            Assert.Equal(Math.Round(passi), passi, 2);
            Assert.Equal(0, n1, 2);
            Assert.Equal(100, n2, 2);
        }
    }

    [Fact]
    public void Lo_scostamento_sposta_tutta_la_griglia_a_sinistra_del_primo_filare()
    {
        // Verso nord la sinistra è l'ovest: con 0,5 m i filari passano a 0,5 + 2,5·k metri dal lato ovest.
        var calcolati = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0.5);

        Assert.Equal(40, calcolati.NumFilari);
        var posizioni = calcolati.Filari.Select(f => Metri(f.Lon1, f.Lat1).Est).Order().ToList();
        Assert.Equal(0.5, posizioni[0], 2);
        Assert.Equal(98, posizioni[^1], 2);
    }

    [Fact]
    public void Il_triangolo_ritaglia_i_filari_dentro_il_poligono()
    {
        var calcolati = GeneratoreFilari.Genera(Poligono([(0, 0), (100, 0), (0, 100)]), Punto(1, 10), Punto(1, 50), 2.5, 0);

        // Filari a 1; 3,5; …; 98,5 m dal lato ovest, ognuno lungo 100 − x: 4000 − 1990 m.
        Assert.Equal(40, calcolati.NumFilari);
        Assert.Equal(2010, calcolati.LunghezzaM, 1);
        foreach (var f in calcolati.Filari)
        {
            var (e, n) = Metri(f.Lon2, f.Lat2);
            Assert.Equal(100 - e, n, 2);
        }
    }

    [Fact]
    public void Un_buco_nel_poligono_spezza_i_filari_che_lo_attraversano()
    {
        var calcolati = GeneratoreFilari.Genera(Poligono(Quadrato, [(40, 40), (60, 40), (60, 60), (40, 60)]),
            Punto(1, 10), Punto(1, 50), 2.5, 0);

        // Gli 8 filari fra 41 e 58,5 m diventano due pezzi ciascuno, 20 m più corti in tutto.
        Assert.Equal(48, calcolati.NumFilari);
        Assert.Equal(3840, calcolati.LunghezzaM, 1);
    }

    [Fact]
    public void Filari_obliqui_hanno_l_azimut_giusto_e_l_interfila_vera()
    {
        // Primo filare verso nord-est (45°): i filari consecutivi distano 3 m misurati in perpendicolare.
        var calcolati = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(10, 10), Punto(60, 60), 3, 0);

        Assert.Equal(45, calcolati.AzimutGradi!.Value, 1);
        var distanze = calcolati.Filari.Select(f =>
        {
            var (e, n) = Metri(f.Lon1, f.Lat1);
            return (e - n) / Math.Sqrt(2);
        }).Distinct().Order().ToList();
        for (var i = 1; i < distanze.Count; i++)
        {
            Assert.Equal(3, distanze[i] - distanze[i - 1], 2);
        }
    }

    [Theory]
    [InlineData(0.1, 0)]
    [InlineData(2.5, 80)]
    public void Valori_fuori_intervallo_sono_rifiutati(double interfila, double scostamento) =>
        Assert.Throws<ArgumentException>(() => GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 10), Punto(1, 50), interfila, scostamento));

    [Fact]
    public void Primo_filare_di_un_punto_solo_o_vigneto_senza_poligono_sono_rifiutati()
    {
        Assert.Throws<ArgumentException>(() => GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 10), Punto(1, 10), 2.5, 0));
        var punto = new JsonObject { ["type"] = "Point", ["coordinates"] = new JsonArray(13.2, 45.95) };
        Assert.Throws<ArgumentException>(() => GeneratoreFilari.Genera(punto, Punto(1, 10), Punto(1, 50), 2.5, 0));
    }

    [Fact]
    public void Misura_e_wkt_andata_e_ritorno()
    {
        var filari = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0).Filari;

        var wkt = GeneratoreFilari.Wkt(filari);
        Assert.StartsWith("MULTILINESTRING ((13.", wkt);
        var letti = GeneratoreFilari.DaWkt(wkt);
        Assert.Equal(filari.Count, letti.Count);
        Assert.Equal(4000, GeneratoreFilari.Misura(letti).LunghezzaM, 0);
        Assert.Equal(0, GeneratoreFilari.Misura(letti).AzimutGradi!.Value, 1);

        Assert.Equal("MULTILINESTRING EMPTY", GeneratoreFilari.Wkt([]));
        Assert.Empty(GeneratoreFilari.DaWkt("MULTILINESTRING EMPTY"));
        Assert.Empty(GeneratoreFilari.DaWkt("non è wkt"));
        Assert.Null(GeneratoreFilari.Misura([]).AzimutGradi);
    }

    private ArchivioFilari Archivio() =>
        new(Options.Create(new DisegniOptions { Database = Path.Combine(_cartella, "sotto", "filari.db") }));

    private static VersioneFilari Versione(int vigneto, int n, string autore = "ltrassinelli") =>
        new(vigneto, 0, $"MULTILINESTRING ((13.2 45.95, 13.2 45.951))", 2.5, 0, 0, n, 111.2, autore, "Lorenzo Trassinelli",
            new DateTime(2026, 10, 14, 11, 30, 0, DateTimeKind.Utc), null);

    [Fact]
    public async Task L_archivio_numera_le_versioni_e_la_piu_recente_e_quella_valida()
    {
        var archivio = Archivio();
        Assert.Empty(await archivio.UltimeAsync([7], CancellationToken.None)); // il file non c'è ancora: nessun errore

        var prima = await archivio.SalvaAsync(Versione(7, 40), CancellationToken.None);
        var seconda = await archivio.SalvaAsync(Versione(7, 42), CancellationToken.None);
        await archivio.SalvaAsync(Versione(8, 10), CancellationToken.None);

        Assert.Equal(1, prima.Versione);
        Assert.Equal(2, seconda.Versione);
        Assert.True(File.Exists(archivio.Percorso));

        var ultime = await archivio.UltimeAsync([7, 8, 9], CancellationToken.None);
        Assert.Equal([(7, 2, 42), (8, 1, 10)], ultime.OrderBy(u => u.IdVigneto).Select(u => (u.IdVigneto, u.Versione, u.NumFilari)));

        var storico = await archivio.StoricoAsync(7, CancellationToken.None);
        Assert.Equal([2, 1], storico.Select(s => s.Versione));
        Assert.Equal(new DateTime(2026, 10, 14, 11, 30, 0, DateTimeKind.Utc), storico[0].CreatoIl);
        Assert.Equal("Lorenzo Trassinelli", storico[0].AutoreNome);

        Assert.Equal(40, (await archivio.VersioneAsync(7, 1, CancellationToken.None))!.NumFilari);
        Assert.Null(await archivio.VersioneAsync(7, 3, CancellationToken.None));
    }

    private sealed class VignetiDiProva : IVignetiRepository
    {
        public static readonly Vigneto Quadrato = new()
        {
            Id = 501, AziendaId = 1, Nome = "Quadrato",
            GeometriaGeoJson = Poligono(FilariTests.Quadrato).ToJsonString(),
            FilariDichiarati = 35, TraFilaCm = 250m, SullaFilaCm = 90m, OrientamentoGradi = 0,
        };

        // 200 m est-ovest × 50 m: senza orientamento in 4Grapes i filari si stimano lungo il lato lungo (90°).
        public static readonly Vigneto Rettangolo = new()
        {
            Id = 503, AziendaId = 1, Nome = "Rettangolo",
            GeometriaGeoJson = Poligono([(0, 0), (200, 0), (200, 50), (0, 50)]).ToJsonString(),
            TraFilaCm = 2.5m,
        };

        public static readonly Vigneto SenzaSesto = new()
        {
            Id = 504, AziendaId = 1, Nome = "Senza sesto",
            GeometriaGeoJson = Poligono(FilariTests.Quadrato).ToJsonString(),
            TraFilaCm = 50m,
        };

        public static readonly Vigneto Pallino = new()
        {
            Id = 502, AziendaId = 1, Nome = "Pallino", SoloPunto = true,
            GeometriaGeoJson = """{"type":"Point","coordinates":[13.2,45.95]}""",
        };

        public Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Vigneto>>(aziendaId == 1 ? [Quadrato, Pallino, Rettangolo, SenzaSesto] : []);

        public Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default) => Task.FromResult<Vigneto?>(null);

        public Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SuperficieVigneto>>([]);
    }

    private FilariController Controller(string? cartellaProposte = null) =>
        new(CacheVignetiAziendeTests.Cache(new VignetiDiProva()), PerimetroAziendeTests.Perimetro(), Archivio(),
            disegni: Options.Create(new DisegniOptions { CartellaProposte = cartellaProposte ?? "" }));

    // --- Task #146: dai dati 4Grapes e carico in 4Grapes ---

    [Theory]
    [InlineData(250.0, 2.5, false)]
    [InlineData(100.0, 1.0, false)]
    [InlineData(500.0, 5.0, false)]
    [InlineData(2.8, 2.8, true)]   // scritto in metri
    [InlineData(50.0, null, true)] // fra 10 e 100: non credibile
    [InlineData(600.0, null, true)]
    [InlineData(0.0, null, true)]
    [InlineData(null, null, true)]
    public void Interfila_da_TraFila(double? traFila, double? attesa, bool conNota)
    {
        var (metri, nota) = SestoImpianto.Interfila(traFila is { } t ? (decimal)t : null);
        Assert.Equal(attesa, metri);
        Assert.Equal(conNota, nota is not null);
    }

    [Fact]
    public async Task Dai_dati_4Grapes_propone_il_primo_filare_con_orientamento_e_interfila()
    {
        var controller = Controller();

        var quadrato = Assert.IsType<FilariDa4GrapesDto>(Assert.IsType<OkObjectResult>(
            (await controller.DaFourGrapes(1, 501, CancellationToken.None)).Result).Value);
        Assert.Equal(2.5, quadrato.InterfilaM);
        Assert.Equal(0, quadrato.AzimutGradi);
        Assert.False(quadrato.OrientamentoStimato);
        Assert.Equal(35, quadrato.FilariIn4Grapes);
        Assert.Equal(0.9, quadrato.SullaFilaM);
        // Il primo filare attraversa il vigneto da sud a nord, nel mezzo.
        var (daE, daN) = Metri(quadrato.Da[0], quadrato.Da[1]);
        var (aE, aN) = Metri(quadrato.A[0], quadrato.A[1]);
        Assert.Equal(50, daE, 0);
        Assert.Equal(0, daN, 0);
        Assert.Equal(50, aE, 0);
        Assert.Equal(100, aN, 0);

        var generati = Assert.IsType<FilariGeneratiDto>(Assert.IsType<OkObjectResult>((await controller.Genera(1, 501,
            new GeneraFilariDto(quadrato.Da, quadrato.A, quadrato.InterfilaM, 0), CancellationToken.None)).Result).Value);
        Assert.InRange(generati.NumFilari, 39, 41);

        var rettangolo = Assert.IsType<FilariDa4GrapesDto>(Assert.IsType<OkObjectResult>(
            (await controller.DaFourGrapes(1, 503, CancellationToken.None)).Result).Value);
        Assert.True(rettangolo.OrientamentoStimato);
        Assert.Equal(90, rettangolo.AzimutGradi, 0);
        Assert.Equal(2.5, rettangolo.InterfilaM);
        Assert.Contains(rettangolo.Note, n => n.Contains("metri"));

        Assert.IsType<BadRequestObjectResult>((await controller.DaFourGrapes(1, 504, CancellationToken.None)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.DaFourGrapes(1, 502, CancellationToken.None)).Result);
        Assert.IsType<NotFoundResult>((await controller.DaFourGrapes(2, 501, CancellationToken.None)).Result);
    }

    [Fact]
    public void Un_filare_spezzato_da_un_buco_conta_una_volta_per_4Grapes()
    {
        var filari = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0).Filari.ToList();
        Assert.Equal(40, filari.Count);
        var primo = filari[0];
        var meta = new Filare(primo.Lon1, primo.Lat1, (primo.Lon1 + primo.Lon2) / 2, (primo.Lat1 + primo.Lat2) / 2);
        var resto = new Filare((primo.Lon1 + primo.Lon2) / 2 + 1e-7, (primo.Lat1 + primo.Lat2) / 2 + 1e-5, primo.Lon2, primo.Lat2);
        filari[0] = meta;
        filari.Insert(1, resto);

        Assert.Equal(40, PropostaFourGrapes.Allineamenti(filari, 0, 2.5));
    }

    [Fact]
    public async Task Carica_in_4Grapes_deposita_la_proposta_per_l_esecutore()
    {
        var spento = Controller();
        Assert.IsType<ConflictObjectResult>((await spento.GetCarico(1, 501, CancellationToken.None)).Result);

        var filari = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 0), Punto(1, 100), 2.5, 0).Filari.Select(f => f.ComeArray()).ToList();
        await spento.Salva(1, 501, new SalvaFilariDto(filari, 2.5, 0, null, "ltrassinelli", "Lorenzo Trassinelli"), CancellationToken.None);

        var anteprima = Assert.IsType<CaricoFilari4GrapesDto>(Assert.IsType<OkObjectResult>(
            (await spento.GetCarico(1, 501, CancellationToken.None)).Result).Value);
        Assert.Equal((1, 35, 0, 40, 0, false),
            (anteprima.Versione, anteprima.FilariOggi, anteprima.OrientamentoOggi, anteprima.FilariNuovi, anteprima.OrientamentoNuovo, anteprima.Attivo));

        var richiesta = new CaricaFilari4GrapesDto(1, "ltrassinelli", "Lorenzo Trassinelli");
        Assert.Equal(503, Assert.IsType<ObjectResult>((await spento.Carica(1, 501, richiesta, CancellationToken.None)).Result).StatusCode);

        var cartella = Path.Combine(_cartella, "proposte");
        Directory.CreateDirectory(cartella);
        var acceso = Controller(cartella);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await acceso.Carica(1, 501, richiesta with { Autore = " " }, CancellationToken.None)).Result).StatusCode);
        Assert.IsType<ConflictObjectResult>((await acceso.Carica(1, 501, richiesta with { Versione = 7 }, CancellationToken.None)).Result);
        Assert.IsType<ConflictObjectResult>((await acceso.Carica(1, 503, richiesta, CancellationToken.None)).Result);

        Assert.True(Assert.IsType<CaricoFilari4GrapesDto>(Assert.IsType<OkObjectResult>(
            (await acceso.Carica(1, 501, richiesta, CancellationToken.None)).Result).Value).Attivo);
        var file = Assert.Single(Directory.GetFiles(cartella));
        Assert.EndsWith("-mappe-filari-vigneto-501.json", file);

        using var proposta = JsonDocument.Parse(File.ReadAllText(file));
        var radice = proposta.RootElement;
        Assert.Equal("UPDATE dbo.Vigneto SET Filari = 40, Orientamento = 0 WHERE IdVigneto = 501 AND Filari = 35 AND Orientamento = 0;",
            radice.GetProperty("sql").GetString());
        Assert.Equal(1, radice.GetProperty("righe_attese").GetProperty("dbo.Vigneto").GetInt32());
        Assert.Equal("UPDATE dbo.Vigneto SET Filari = 35, Orientamento = 0 WHERE IdVigneto = 501;",
            radice.GetProperty("come_tornare_indietro").GetString());
        Assert.Equal(1, radice.GetProperty("controllo_dopo").GetProperty("atteso")[0].GetInt32());
        Assert.Contains("Lorenzo Trassinelli (ltrassinelli)", radice.GetProperty("richiesta_da").GetString());
        foreach (var campo in new[] { "titolo", "perche", "effetti_indiretti", "verifica_prima", "rischio" })
        {
            Assert.False(string.IsNullOrWhiteSpace(radice.GetProperty(campo).GetString()));
        }
    }

    [Fact]
    public void Testi_dai_dati_4Grapes_e_del_carico()
    {
        var sesto = new FilariDa4GrapesDto([0, 0], [0, 1], 2.5, 20.2, false, 110, 0.9, []);
        Assert.Equal("Dai dati 4Grapes: interfila 2,5 m, orientamento 20° da nord, piante ogni 0,9 m. In 4Grapes: 110 filari.",
            Web.Filari.TestiFilari.DaFourGrapes(sesto));
        Assert.Contains("stimato dalla forma", Web.Filari.TestiFilari.DaFourGrapes(sesto with { OrientamentoStimato = true, SullaFilaM = null }));
        Assert.Equal("35 → 40", Web.Filari.TestiFilari.Cambio(35, 40));
        Assert.Equal("non indicato → 12°", Web.Filari.TestiFilari.Cambio(null, 12, gradi: true));
        Assert.Equal("40 (uguale)", Web.Filari.TestiFilari.Cambio(40, 40));
    }

    [Fact]
    public void Proposta_con_valori_mancanti_in_4Grapes_usa_IS_NULL()
    {
        var vigneto = new Vigneto { Id = 9, Nome = "Nuovo\n vigneto" };
        var versione = new VersioneFilari(9, 2, "MULTILINESTRING EMPTY", 2.5, 179.6, 0, 3, 300, "x", null, DateTime.UtcNow, null);
        var proposta = PropostaFourGrapes.Componi(vigneto, "azienda 1", versione, new PropostaFourGrapes.Valori(3, 0), "x", null,
            new DateTime(2026, 10, 30, 15, 0, 0));
        Assert.Equal("UPDATE dbo.Vigneto SET Filari = 3, Orientamento = 0 WHERE IdVigneto = 9 AND Filari IS NULL AND Orientamento IS NULL;",
            proposta["sql"]!.GetValue<string>());
        Assert.Equal("UPDATE dbo.Vigneto SET Filari = NULL, Orientamento = NULL WHERE IdVigneto = 9;", proposta["come_tornare_indietro"]!.GetValue<string>());
        Assert.DoesNotContain("\n", proposta["titolo"]!.GetValue<string>());
        Assert.Equal("2026-10-30-150000-mappe-filari-vigneto-9.json", PropostaFourGrapes.NomeFile(9, new DateTime(2026, 10, 30, 15, 0, 0)));
    }

    private static double[] Coppia((double Lon, double Lat) p) => [p.Lon, p.Lat];

    [Fact]
    public async Task Genera_dal_vigneto_e_rifiuta_i_vigneti_senza_poligono_o_di_altre_aziende()
    {
        var controller = Controller();
        var richiesta = new GeneraFilariDto(Coppia(Punto(1, 20)), Coppia(Punto(1, 80)), 2.5, 0);

        var generati = Assert.IsType<FilariGeneratiDto>(Assert.IsType<OkObjectResult>((await controller.Genera(1, 501, richiesta, CancellationToken.None)).Result).Value);
        Assert.Equal(40, generati.NumFilari);
        Assert.Equal(40, generati.Filari.Count);
        Assert.Equal(4000, generati.LunghezzaM, 0);

        Assert.IsType<BadRequestObjectResult>((await controller.Genera(1, 502, richiesta, CancellationToken.None)).Result);
        Assert.IsType<NotFoundResult>((await controller.Genera(2, 501, richiesta, CancellationToken.None)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.Genera(1, 501, richiesta with { InterfilaM = 0 }, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Salva_solo_con_autore_e_ricalcola_numero_e_lunghezza()
    {
        var controller = Controller();
        var filari = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0).Filari
            .Skip(2).Select(f => f.ComeArray()).ToList(); // due filari tolti a mano
        var richiesta = new SalvaFilariDto(filari, 2.5, 0, "prima prova", null, null);

        var senzaAutore = Assert.IsType<ObjectResult>((await controller.Salva(1, 501, richiesta, CancellationToken.None)).Result);
        Assert.Equal(403, senzaAutore.StatusCode);

        var salvata = Assert.IsType<FilariVignetoDto>(Assert.IsType<OkObjectResult>(
            (await controller.Salva(1, 501, richiesta with { Autore = "ltrassinelli", AutoreNome = "Lorenzo Trassinelli" }, CancellationToken.None)).Result).Value);
        Assert.Equal(1, salvata.Versione);
        Assert.Equal(38, salvata.NumFilari);
        Assert.Equal(3800, salvata.LunghezzaM, 0);
        Assert.Equal("L. Trassinelli", salvata.AutoreBreve);
        Assert.Equal("prima prova", salvata.Note);

        Assert.IsType<NotFoundResult>((await controller.Salva(2, 501, richiesta with { Autore = "x" }, CancellationToken.None)).Result);
        var rotta = richiesta with { Autore = "x", Filari = [[[13.2, 45.95]]] };
        Assert.IsType<BadRequestObjectResult>((await controller.Salva(1, 501, rotta, CancellationToken.None)).Result);

        // Lo strato della mappa: una LineString per filare, con il vigneto e il numero.
        var azienda = Assert.IsType<FilariAziendaDto>(Assert.IsType<OkObjectResult>((await controller.GetFilariAzienda(1, CancellationToken.None)).Result).Value);
        Assert.True(azienda.Disponibile);
        Assert.Equal(38, Assert.Single(azienda.Vigneti).NumFilari);
        var features = azienda.GeoJson.GetProperty("features");
        Assert.Equal(38, features.GetArrayLength());
        Assert.Equal(501, features[0].GetProperty("properties").GetProperty("idVigneto").GetInt32());
        Assert.Equal(1, features[0].GetProperty("properties").GetProperty("numero").GetInt32());
        Assert.Equal("LineString", features[0].GetProperty("geometry").GetProperty("type").GetString());

        // Storico e ripristino: la versione 1 si rilegge con i suoi filari.
        await controller.Salva(1, 501, richiesta with { Autore = "ltrassinelli", Filari = [] }, CancellationToken.None);
        var storico = Assert.IsType<StoricoFilariDto>(Assert.IsType<OkObjectResult>((await controller.GetStorico(1, 501, CancellationToken.None)).Result).Value);
        Assert.Equal([2, 1], storico.Versioni.Select(v => v.Versione));
        var prima = Assert.IsType<VersioneFilariDto>(Assert.IsType<OkObjectResult>((await controller.GetVersione(1, 501, 1, CancellationToken.None)).Result).Value);
        Assert.Equal(38, prima.Filari.Count);

        // Ultima versione vuota: il vigneto non ha più filari sulla mappa.
        azienda = Assert.IsType<FilariAziendaDto>(Assert.IsType<OkObjectResult>((await controller.GetFilariAzienda(1, CancellationToken.None)).Result).Value);
        Assert.Empty(azienda.Vigneti);
    }

    [Fact]
    public void L_export_ha_lo_strato_filari_nei_tre_formati()
    {
        var filari = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(1, 20), Punto(1, 80), 2.5, 0).Filari
            .Take(3)
            .Select((f, i) => new Esportazione.FilareEsportato(501, "Albiana", "Quadrato", i + 1, GeneratoreFilari.Lunghezza(f), 2, 2.5,
                new JsonObject
                {
                    ["type"] = "LineString",
                    ["coordinates"] = new JsonArray(new JsonArray(f.Lon1, f.Lat1), new JsonArray(f.Lon2, f.Lat2)),
                }))
            .ToList();

        using var geojson = JsonDocument.Parse(Esportazione.EsportazioneVigneti.Scrivi(Esportazione.FormatoEsportazione.GeoJson, "albiana", [], null, filari));
        var feature = geojson.RootElement.GetProperty("features")[1];
        Assert.Equal("LineString", feature.GetProperty("geometry").GetProperty("type").GetString());
        Assert.Equal(2, feature.GetProperty("properties").GetProperty("filare").GetInt32());
        Assert.Equal(100, feature.GetProperty("properties").GetProperty("lung_m").GetDouble(), 0);
        Assert.Equal("filare", feature.GetProperty("properties").GetProperty("geometria").GetString());

        var kml = System.Text.Encoding.UTF8.GetString(Esportazione.EsportazioneVigneti.Scrivi(Esportazione.FormatoEsportazione.Kml, "albiana", [], null, filari));
        Assert.Contains("<name>Filari</name>", kml);
        Assert.Contains("<name>Quadrato – filare 3</name>", kml);
        Assert.Contains("<LineString>", kml);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(
            Esportazione.EsportazioneVigneti.Scrivi(Esportazione.FormatoEsportazione.Shapefile, "albiana", [], null, filari)));
        Assert.Contains(zip.Entries, e => e.Name == "albiana-filari.shp");
        Assert.Contains(zip.Entries, e => e.Name == "albiana-filari.dbf");
    }

    [Fact]
    public void Riga_della_scheda_e_misure_del_web()
    {
        var filari = new FilariVignetoDto(501, 3, 42, 3120, 2.5, 12, 0, "ltrassinelli", "L. Trassinelli",
            new DateTime(DateTime.UtcNow.Year, 10, 14, 10, 0, 0, DateTimeKind.Utc), null);
        Assert.Equal("42, interfila 2,5 m, 3,1 km, disegnati da L. Trassinelli il 14/10",
            Web.Filari.TestiFilari.Riassunto(filari, Web.Tematismi.TematismiBigot.FusoItalia));
        Assert.Equal("850 m", Web.Filari.TestiFilari.Metri(849.6));
        Assert.Equal("1 filare", Web.Filari.TestiFilari.Numero(1));

        // La misura veloce del Web sta entro lo 0,1 % di quella UTM dell'Api.
        var generati = GeneratoreFilari.Genera(Poligono(Quadrato), Punto(10, 10), Punto(60, 60), 2.5, 0);
        var web = Web.Filari.TestiFilari.LunghezzaTotale(generati.Filari.Select(f => f.ComeArray()));
        Assert.Equal(generati.LunghezzaM, web, generati.LunghezzaM * 0.001);
    }

    [Fact]
    public void Annulla_e_ripristina_con_i_cursori_uniti_in_un_passo()
    {
        var storia = new Web.Filari.StoriaModifiche<string>("vuoto");
        Assert.False(storia.PuoAnnullare);

        storia.Fai("generati");
        storia.Fai("cursore 2,6", unisci: true);
        storia.Fai("cursore 2,7", unisci: true); // stesso passo del precedente
        storia.Fai("tolto un filare");

        Assert.Equal("cursore 2,7", storia.Annulla());
        Assert.Equal("generati", storia.Annulla());
        Assert.Equal("vuoto", storia.Annulla());
        Assert.False(storia.PuoAnnullare);
        Assert.Equal("generati", storia.Ripristina());

        storia.Fai("nuovo ramo"); // una modifica dopo l'annulla cancella i passi avanti
        Assert.False(storia.PuoRipristinare);
        Assert.Equal("generati", storia.Annulla());
    }

    [Theory]
    [InlineData("Lorenzo Trassinelli", "L. Trassinelli")]
    [InlineData("mario de luca", "M. De Luca")]
    [InlineData("Broggi", "Broggi")]
    [InlineData(null, null)]
    public void Autore_breve(string? nome, string? atteso) => Assert.Equal(atteso, FilariController.AutoreBreve(nome));
}
