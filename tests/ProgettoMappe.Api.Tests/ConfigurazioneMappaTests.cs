using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProgettoMappe.Web.Options;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class ConfigurazioneMappaTests
{
    private const string ChiaveSegreta = "chiave-di-prova-XYZ123";

    // --- Parsing da configurazione ---

    [Fact]
    public void Le_sorgenti_si_leggono_da_configurazione_con_Id_dalla_chiave_del_dizionario()
    {
        var opzioni = Lega(new()
        {
            ["Map:Sorgenti:osm:Nome"] = "OSM",
            ["Map:Sorgenti:osm:Ruolo"] = "Basemap",
            ["Map:Sorgenti:osm:Tipo"] = "style",
            ["Map:Sorgenti:osm:StyleUrl"] = "https://esempio.test/style.json",
            ["Map:Sorgenti:dem:Ruolo"] = "Terrain",
            ["Map:Sorgenti:dem:Tipo"] = "raster-dem",
            ["Map:Sorgenti:dem:TileUrl"] = "https://esempio.test/{z}/{x}/{y}.webp",
            ["Map:Sorgenti:dem:Encoding"] = "terrarium",
            ["Map:Sorgenti:dem:MaxZoom"] = "12",
            ["Map:Sorgenti:dem:Bounds:0"] = "6.5",
            ["Map:Sorgenti:dem:Bounds:1"] = "36.5",
            ["Map:Sorgenti:dem:Bounds:2"] = "18.6",
            ["Map:Sorgenti:dem:Bounds:3"] = "47.1",
            ["Map:BasemapPredefinita"] = "osm",
            ["Map:TerrainPredefinito"] = "dem",
        });

        var configurazione = ConfigurazioneMappa.Crea(opzioni);

        Assert.Equal(["dem", "osm"], configurazione.Sorgenti.Select(s => s.Id));
        Assert.Equal("osm", configurazione.BasemapPredefinitaId);
        Assert.Equal("dem", configurazione.TerrainId);
        var dem = configurazione.Sorgenti.Single(s => s.Id == "dem");
        Assert.Equal(RuoloSorgente.Terrain, dem.Ruolo);
        Assert.NotNull(dem.Bounds);
        Assert.Equal([6.5, 36.5, 18.6, 47.1], dem.Bounds);
        Assert.Equal("dem", dem.Nome); // senza Nome si usa l'Id
    }

    [Fact]
    public void La_configurazione_versionata_di_Web_e_valida()
    {
        var cartellaWeb = TrovaCartellaWeb();
        var produzione = LegaFile(Path.Combine(cartellaWeb, "appsettings.json"));
        var sviluppo = LegaFile(Path.Combine(cartellaWeb, "appsettings.json"), Path.Combine(cartellaWeb, "appsettings.Development.json"));

        var configProduzione = ConfigurazioneMappa.Crea(produzione);
        var configSviluppo = ConfigurazioneMappa.Crea(sviluppo);

        Assert.Empty(configProduzione.Scartate);
        Assert.Empty(configSviluppo.Scartate);
        Assert.Equal("openfreemap-liberty", configProduzione.BasemapPredefinitaId);
        Assert.Equal("openfreemap-liberty", configProduzione.BasemapFallbackId);
        Assert.Equal("mapterhorn", configProduzione.TerrainId);

        // Esri World Imagery: endpoint pubblico solo per sviluppo/benchmark, mai in produzione.
        Assert.DoesNotContain(configProduzione.Sorgenti, s => s.Id == "esri-world-imagery");
        Assert.Null(configProduzione.ImageryPredefinitaId);
        Assert.False(configProduzione.ModalitaSviluppatore);
        Assert.Equal("esri-world-imagery", configSviluppo.ImageryPredefinitaId);
        Assert.True(configSviluppo.ModalitaSviluppatore);
    }

    [Fact]
    public void Mapterhorn_ha_MaxZoom_12_esplicito_encoding_terrarium_e_tile_da_512()
    {
        var produzione = LegaFile(Path.Combine(TrovaCartellaWeb(), "appsettings.json"));

        var mapterhorn = ConfigurazioneMappa.Crea(produzione).Sorgenti.Single(s => s.Id == "mapterhorn");

        Assert.Equal(12, mapterhorn.MaxZoom);
        Assert.Equal("terrarium", mapterhorn.Encoding);
        Assert.Equal(512, mapterhorn.TileSize);
        // TileUrl diretto e non TileJSON: il TileJSON di Mapterhorn non dichiara maxzoom e
        // prevarrebbe sul valore configurato solo se lo facesse.
        Assert.NotNull(mapterhorn.TileUrl);
        Assert.Null(mapterhorn.TileJsonUrl);
    }

    // --- Validazione ---

    [Fact]
    public void Sorgente_disabilitata_non_arriva_al_browser()
    {
        var esito = Risolvi(Style() with { Enabled = false });

        Assert.Null(esito.Risolta);
        Assert.Equal("disabilitata", esito.Motivo);
    }

    [Theory]
    [MemberData(nameof(SorgentiIncomplete))]
    public void Sorgente_incompleta_viene_scartata(SorgenteMappaDati dati, string motivoAtteso)
    {
        var esito = Risolvi(dati);

        Assert.Null(esito.Risolta);
        Assert.Contains(motivoAtteso, esito.Motivo);
    }

    public static TheoryData<SorgenteMappaDati, string> SorgentiIncomplete() => new()
    {
        { Style() with { StyleUrl = null }, "StyleUrl mancante" },
        { Style() with { Ruolo = null }, "Ruolo mancante" },
        { Raster() with { TileUrl = null, TileJsonUrl = null }, "TileUrl o TileJsonUrl mancante" },
        { Dem() with { Encoding = null }, "Encoding" },
        { Dem() with { Encoding = "png" }, "Encoding" },
        { Dem() with { MaxZoom = null }, "MaxZoom obbligatorio" },
        { Raster() with { Ruolo = RuoloSorgente.Basemap }, "richiede Tipo 'style'" },
        { Style() with { Tipo = "wms" }, "non ancora supportato" },
        { Style() with { Tipo = "vector" }, "non supportato" },
        { Raster() with { MinZoom = 15, MaxZoom = 10 }, "MinZoom maggiore di MaxZoom" },
        { Raster() with { MaxZoom = 30 }, "fuori intervallo" },
        { Raster() with { Bounds = [10, 40, 5, 45] }, "Bounds non valido" },
        { Style() with { StyleUrl = "/style.json" }, "URL non assoluto" },
        { Style() with { StyleUrl = "ftp://esempio.test/style.json" }, "URL non assoluto" },
    };

    [Fact]
    public void Sorgente_con_ApiKeyName_ma_chiave_assente_viene_scartata()
    {
        var esito = Risolvi(StyleConChiave(), apiKeys: new Dictionary<string, string>());

        Assert.Null(esito.Risolta);
        Assert.Equal("chiave 'Provider' non configurata", esito.Motivo);
    }

    [Fact]
    public void Sorgente_con_ApiKeyName_e_chiave_vuota_viene_scartata()
    {
        var esito = Risolvi(StyleConChiave(), apiKeys: new Dictionary<string, string> { ["Provider"] = "  " });

        Assert.Null(esito.Risolta);
    }

    [Fact]
    public void Segnaposto_senza_ApiKeyName_o_ApiKeyName_senza_segnaposto_vengono_scartati()
    {
        Assert.Null(Risolvi(StyleConChiave() with { ApiKeyName = null }).Risolta);
        Assert.Null(Risolvi(Style() with { ApiKeyName = "Provider" }, apiKeys: new Dictionary<string, string> { ["Provider"] = ChiaveSegreta }).Risolta);
    }

    // --- Risoluzione sicura ---

    [Fact]
    public void La_chiave_viene_sostituita_nell_URL_con_escaping()
    {
        var esito = Risolvi(StyleConChiave(), apiKeys: new Dictionary<string, string> { ["Provider"] = "a b&c" });

        Assert.Equal("https://esempio.test/style.json?key=a%20b%26c", esito.Risolta!.StyleUrl);
    }

    [Fact]
    public void Il_valore_della_chiave_non_compare_in_log_scarti_o_ToString()
    {
        var logger = new LoggerDiProva();
        var opzioni = new MapOptions
        {
            ApiKeys = { ["Provider"] = ChiaveSegreta },
            BasemapPredefinita = "inesistente",
            Sorgenti =
            {
                ["con-chiave"] = StyleConChiave().ToSorgente(),
                ["rotta"] = (StyleConChiave() with { StyleUrl = "non-un-url?key={apiKey}" }).ToSorgente(),
            },
        };

        var configurazione = ConfigurazioneMappa.Crea(opzioni, logger);

        Assert.NotEmpty(logger.Messaggi);
        Assert.DoesNotContain(logger.Messaggi, m => m.Contains(ChiaveSegreta));
        Assert.DoesNotContain(configurazione.Scartate, s => s.Motivo.Contains(ChiaveSegreta));
        var risolta = configurazione.Sorgenti.Single();
        Assert.Contains(ChiaveSegreta, risolta.StyleUrl); // al browser serve...
        Assert.DoesNotContain(ChiaveSegreta, risolta.ToString()); // ...ma non finisce in stringhe di log
    }

    [Fact]
    public void Basemap_predefinita_non_valida_ripiega_sulla_riserva_e_lo_segnala()
    {
        var opzioni = new MapOptions
        {
            BasemapPredefinita = "provider-senza-chiave",
            BasemapFallback = "riserva",
            Sorgenti =
            {
                ["provider-senza-chiave"] = StyleConChiave().ToSorgente(),
                ["riserva"] = Style().ToSorgente(),
            },
        };

        var configurazione = ConfigurazioneMappa.Crea(opzioni);

        Assert.Equal("riserva", configurazione.BasemapPredefinitaId);
        Assert.Equal("riserva", configurazione.BasemapFallbackId);
        Assert.True(configurazione.BasemapPredefinitaSostituita);
    }

    [Fact]
    public void Id_di_default_con_ruolo_sbagliato_o_senza_sorgenti_restano_null()
    {
        var opzioni = new MapOptions
        {
            BasemapPredefinita = "dem",
            ImageryPredefinita = "manca",
            TerrainPredefinito = "dem",
            Sorgenti = { ["dem"] = Dem().ToSorgente() },
        };

        var configurazione = ConfigurazioneMappa.Crea(opzioni);

        Assert.Null(configurazione.BasemapPredefinitaId);
        Assert.Null(configurazione.BasemapFallbackId);
        Assert.Null(configurazione.ImageryPredefinitaId);
        Assert.Equal("dem", configurazione.TerrainId);
        Assert.True(configurazione.BasemapPredefinitaSostituita);
    }

    [Fact]
    public void Timeout_ed_esagerazione_vengono_limitati_a_valori_sensati()
    {
        var configurazione = ConfigurazioneMappa.Crea(new MapOptions { TimeoutStyleSecondi = 0, TerrainExaggeration = -3 });

        Assert.Equal(2000, configurazione.TimeoutStyleMs);
        Assert.Equal(1.0, configurazione.TerrainExaggeration);
    }

    // --- Supporto ---

    /// <summary>Dati di una sorgente come record (per i "with" nei test), convertiti in <see cref="SorgenteMappa"/>.</summary>
    public sealed record SorgenteMappaDati(
        RuoloSorgente? Ruolo, string Tipo, string? StyleUrl = null, string? TileUrl = null, string? TileJsonUrl = null,
        string? Encoding = null, int? MinZoom = null, int? MaxZoom = null, int? TileSize = null,
        string? ApiKeyName = null, double[]? Bounds = null, bool Enabled = true)
    {
        public SorgenteMappa ToSorgente() => new()
        {
            Ruolo = Ruolo, Tipo = Tipo, StyleUrl = StyleUrl, TileUrl = TileUrl, TileJsonUrl = TileJsonUrl,
            Encoding = Encoding, MinZoom = MinZoom, MaxZoom = MaxZoom, TileSize = TileSize,
            ApiKeyName = ApiKeyName, Bounds = Bounds ?? [], Enabled = Enabled,
        };
    }

    private static SorgenteMappaDati Style() => new(RuoloSorgente.Basemap, "style", StyleUrl: "https://esempio.test/style.json");

    private static SorgenteMappaDati StyleConChiave() =>
        Style() with { StyleUrl = "https://esempio.test/style.json?key={apiKey}", ApiKeyName = "Provider" };

    private static SorgenteMappaDati Raster() => new(RuoloSorgente.Imagery, "raster", TileUrl: "https://esempio.test/{z}/{y}/{x}", MaxZoom: 19);

    private static SorgenteMappaDati Dem() =>
        new(RuoloSorgente.Terrain, "raster-dem", TileUrl: "https://esempio.test/{z}/{x}/{y}.webp", Encoding: "terrarium", MaxZoom: 12, TileSize: 512);

    private static (SorgenteRisolta? Risolta, string? Motivo) Risolvi(SorgenteMappaDati dati, Dictionary<string, string>? apiKeys = null) =>
        ConfigurazioneMappa.Risolvi("prova", dati.ToSorgente(), apiKeys ?? new Dictionary<string, string>());

    private static MapOptions Lega(Dictionary<string, string?> valori) =>
        new ConfigurationBuilder().AddInMemoryCollection(valori).Build()
            .GetSection(MapOptions.SectionName).Get<MapOptions>() ?? new MapOptions();

    private static MapOptions LegaFile(params string[] percorsi)
    {
        var builder = new ConfigurationBuilder();
        foreach (var percorso in percorsi)
        {
            builder.AddJsonFile(percorso, optional: false);
        }

        return builder.Build().GetSection(MapOptions.SectionName).Get<MapOptions>() ?? new MapOptions();
    }

    private static string TrovaCartellaWeb()
    {
        for (var cartella = new DirectoryInfo(AppContext.BaseDirectory); cartella is not null; cartella = cartella.Parent)
        {
            var candidata = Path.Combine(cartella.FullName, "src", "ProgettoMappe.Web");
            if (File.Exists(Path.Combine(candidata, "appsettings.json")))
            {
                return candidata;
            }
        }

        throw new DirectoryNotFoundException("Cartella src/ProgettoMappe.Web non trovata risalendo da " + AppContext.BaseDirectory);
    }

    private sealed class LoggerDiProva : ILogger
    {
        public List<string> Messaggi { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messaggi.Add(formatter(state, exception));
    }
}
