using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class CacheVignetiAziendeTests
{
    /// <summary>Cache vuota sui vigneti in-memory (o sul repository dato), come la registra Program.cs.</summary>
    internal static CacheVignetiAziende Cache(IVignetiRepository? vigneti = null)
    {
        var servizi = new ServiceCollection()
            .AddScoped(_ => vigneti ?? new InMemoryVignetiRepository())
            .BuildServiceProvider();
        return new CacheVignetiAziende(new MemoryCache(new MemoryCacheOptions()), servizi.GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public async Task Elenco_e_indici_della_stessa_azienda_fanno_una_lettura_sola()
    {
        var repository = new RepositoryContato();
        var cache = Cache(repository);
        var vigneti = new VignetiController(cache, PerimetroAziendeTests.Perimetro());
        var indici = new IndiciBigotController(cache, new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro());

        await vigneti.GetVignetiPerAzienda(1, CancellationToken.None);
        await indici.GetIndiciBigot(1, CancellationToken.None);
        await vigneti.GetVignetiPerAzienda(2, CancellationToken.None);

        Assert.Equal([1, 2], repository.Letture);
    }

    [Fact]
    public async Task Una_lettura_fallita_non_resta_in_cache()
    {
        var repository = new RepositoryContato { FallisciAllaPrima = true };
        var cache = Cache(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetVignetiPerAziendaAsync(1, CancellationToken.None));
        var vigneti = await cache.GetVignetiPerAziendaAsync(1, CancellationToken.None);

        Assert.NotEmpty(vigneti);
        Assert.Equal([1, 1], repository.Letture);
    }

    [Fact]
    public async Task Elenco_e_GeoJSON_arrivano_insieme_e_la_geometria_solo_nel_GeoJSON()
    {
        var controller = new VignetiController(Cache(), PerimetroAziendeTests.Perimetro());

        var risposta = Assert.IsType<VignetiAziendaDto>(
            Assert.IsType<OkObjectResult>((await controller.GetVignetiPerAzienda(1, CancellationToken.None)).Result).Value);

        var idFeature = risposta.GeoJson.GetProperty("features").EnumerateArray()
            .Select(f => f.GetProperty("id").GetInt32()).Order();
        Assert.NotEmpty(risposta.Vigneti);
        Assert.Equal(risposta.Vigneti.Where(v => v.HaGeometria).Select(v => v.Id).Order(), idFeature);
        Assert.Equal(risposta.Vigneti.Select(v => v.Nome).Order(StringComparer.Ordinal), risposta.Vigneti.Select(v => v.Nome));
    }

    [Fact]
    public async Task La_risposta_passata_in_JSON_arriva_intera_al_Web()
    {
        var controller = new VignetiController(Cache(), PerimetroAziendeTests.Perimetro());
        var risposta = (VignetiAziendaDto)Assert.IsType<OkObjectResult>(
            (await controller.GetVignetiPerAzienda(1, CancellationToken.None)).Result).Value!;

        // Stesse impostazioni JSON di ASP.NET Core (Api) e di GetFromJsonAsync (Web).
        var opzioni = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var letta = JsonSerializer.Deserialize<VignetiAziendaDto>(JsonSerializer.Serialize(risposta, opzioni), opzioni)!;

        Assert.Equal(risposta.Vigneti, letta.Vigneti);
        Assert.Equal("FeatureCollection", letta.GeoJson.GetProperty("type").GetString());
        Assert.Equal(risposta.GeoJson.GetRawText(), letta.GeoJson.GetRawText());
    }

    /// <summary>Vigneti in-memory che contano le letture per azienda.</summary>
    internal sealed class RepositoryContato : IVignetiRepository
    {
        private readonly InMemoryVignetiRepository _dati = new();

        public List<int> Letture { get; } = [];

        public bool FallisciAllaPrima { get; set; }

        public async Task<IReadOnlyList<Vigneto>> GetVignetiPerAziendaAsync(int aziendaId, CancellationToken ct = default)
        {
            Letture.Add(aziendaId);
            await Task.Yield();
            if (FallisciAllaPrima && Letture.Count == 1)
            {
                throw new InvalidOperationException("4Grapes non risponde");
            }

            return await _dati.GetVignetiPerAziendaAsync(aziendaId, ct);
        }

        public Task<Vigneto?> GetVignetoAsync(int id, CancellationToken ct = default) => _dati.GetVignetoAsync(id, ct);

        public int LettureSuperfici { get; private set; }

        public Task<IReadOnlyList<SuperficieVigneto>> GetSuperficiAsync(IReadOnlySet<int>? aziende, CancellationToken ct = default)
        {
            LettureSuperfici++;
            return _dati.GetSuperficiAsync(aziende, ct);
        }
    }
}
