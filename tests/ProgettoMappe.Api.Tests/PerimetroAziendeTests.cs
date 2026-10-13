using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProgettoMappe.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class PerimetroAziendeTests
{
    /// <summary>Perimetro sui gruppi in-memory: gruppo 1 = aziende 1 e 2, gruppo 2 = azienda 2.</summary>
    internal static PerimetroAziende Perimetro(params int[] gruppi) =>
        new(new InMemoryGruppiRepository(), Options.Create(new PerimetroOptions { Gruppi = gruppi }));

    private static T Valore<T>(ActionResult<T> risultato) =>
        (T)Assert.IsType<OkObjectResult>(risultato.Result).Value!;

    [Fact]
    public async Task Senza_gruppi_configurati_non_c_e_restrizione()
    {
        var perimetro = Perimetro();

        Assert.Null(await perimetro.GetAziendeAsync());
        Assert.True(await perimetro.ContieneAsync(9999));
        Assert.Equal(2, (await perimetro.GetGruppiAsync()).Count);
    }

    [Fact]
    public async Task Il_perimetro_tiene_solo_le_aziende_dei_gruppi_configurati()
    {
        var perimetro = Perimetro(2, 42);

        Assert.Equal([2], (await perimetro.GetAziendeAsync())!);
        Assert.False(await perimetro.ContieneAsync(1));
        Assert.Equal([2], (await perimetro.GetGruppiAsync()).Select(g => g.Id));
    }

    [Fact]
    public async Task Le_aziende_fuori_perimetro_non_compaiono_da_nessuna_parte()
    {
        var aziende = new AziendeController(new InMemoryAziendeRepository(), Perimetro(2));
        var vigneti = new VignetiController(CacheVignetiAziendeTests.Cache(), Perimetro(2));
        var indici = new IndiciBigotController(CacheVignetiAziendeTests.Cache(), new InMemoryIndiciBigotRepository(), Perimetro(2));
        var gruppi = new GruppiController(Perimetro(2));

        Assert.Equal([2], Valore(await aziende.GetAziende(CancellationToken.None))!.Select(a => a.Id));
        var vignetiFuori = Valore(await vigneti.GetVignetiPerAzienda(1, CancellationToken.None))!;
        Assert.Empty(vignetiFuori.Vigneti);
        Assert.Equal(0, vignetiFuori.GeoJson.GetProperty("features").GetArrayLength());
        Assert.Empty(Valore(await indici.GetIndiciBigot(1, CancellationToken.None))!.Indici);
        Assert.Equal([2], Valore(await gruppi.GetGruppi(CancellationToken.None))!.Select(g => g.Id));
    }

    [Fact]
    public async Task Le_aziende_nel_perimetro_restano_visibili()
    {
        var aziende = new AziendeController(new InMemoryAziendeRepository(), Perimetro(1));
        var vigneti = new VignetiController(CacheVignetiAziendeTests.Cache(), Perimetro(1));

        Assert.Equal([1, 2], Valore(await aziende.GetAziende(CancellationToken.None))!.Select(a => a.Id).Order());
        var vignetiDentro = Valore(await vigneti.GetVignetiPerAzienda(1, CancellationToken.None))!;
        Assert.NotEmpty(vignetiDentro.Vigneti);
        Assert.NotEqual(0, vignetiDentro.GeoJson.GetProperty("features").GetArrayLength());
    }

    [Fact]
    public void La_configurazione_dell_Api_ha_i_gruppi_VTS()
    {
        // I numeri stanno in appsettings.json, non nel codice: il test controlla il file.
        var percorso = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "ProgettoMappe.Api", "appsettings.json");
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(percorso));

        var gruppi = json.RootElement.GetProperty("Perimetro").GetProperty("Gruppi")
            .EnumerateArray().Select(e => e.GetInt32());

        Assert.Equal([1, 3, 4], gruppi);
    }
}
