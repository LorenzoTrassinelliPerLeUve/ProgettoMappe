using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Filtri;
using Xunit;
using VignetoApi = ProgettoMappe.Api.Contracts.VignetoDto;
using VignetoWeb = ProgettoMappe.Web.Services.VignetoDto;

namespace ProgettoMappe.Api.Tests;

public class FiltriVignetiTests
{
    private static VignetoWeb V(int id, int? vignaId, string? vigna, int? vitignoId, string? vitigno) =>
        new(id, 1, $"Vigneto {id}", vitigno, null, null, vignaId, vigna, vitignoId);

    private static readonly VignetoWeb[] Vigneti =
    [
        V(1, 10, "Vigna Alta", 100, "Nebbiolo"),
        V(2, 10, "Vigna Alta", 101, "Barbera"),
        V(3, 20, "Vigna Bassa", 100, "Nebbiolo"),
        V(4, 20, "Vigna Bassa", null, null),
    ];

    [Fact]
    public void Le_vigne_sono_ordinate_per_nome_con_i_conteggi()
    {
        var vigne = FiltriVigneti.Vigne(Vigneti);

        Assert.Equal([("Vigna Alta", 2), ("Vigna Bassa", 2)], vigne.Select(o => (o.Nome, o.Conteggio)));
    }

    [Fact]
    public void I_vitigni_seguono_la_vigna_scelta_e_includono_Non_specificato()
    {
        Assert.Equal(["Barbera", "Nebbiolo", "Non specificato"], FiltriVigneti.Vitigni(Vigneti, null).Select(o => o.Nome));
        Assert.Equal(["Barbera", "Nebbiolo"], FiltriVigneti.Vitigni(Vigneti, 10).Select(o => o.Nome));
        Assert.Equal([("Nebbiolo", 1), ("Non specificato", 1)], FiltriVigneti.Vitigni(Vigneti, 20).Select(o => (o.Nome, o.Conteggio)));
    }

    [Fact]
    public void Applica_combina_vigna_e_vitigno()
    {
        Assert.Equal([1, 2, 3, 4], FiltriVigneti.Applica(Vigneti, null, null).Select(v => v.Id));
        Assert.Equal([3, 4], FiltriVigneti.Applica(Vigneti, 20, null).Select(v => v.Id));
        Assert.Equal([1, 3], FiltriVigneti.Applica(Vigneti, null, 100).Select(v => v.Id));
        Assert.Equal([3], FiltriVigneti.Applica(Vigneti, 20, 100).Select(v => v.Id));
        Assert.Empty(FiltriVigneti.Applica(Vigneti, 10, 999));
    }

    [Fact]
    public void Non_specificato_seleziona_i_vigneti_senza_vitigno()
    {
        Assert.Equal([4], FiltriVigneti.Applica(Vigneti, null, FiltriVigneti.NonSpecificato).Select(v => v.Id));
    }

    [Fact]
    public void Vigna_senza_nome_usa_un_etichetta_con_l_id()
    {
        var vigne = FiltriVigneti.Vigne([V(1, 30, null, null, null)]);

        Assert.Equal("Vigna 30", vigne.Single().Nome);
    }

    [Fact]
    public async Task L_Api_espone_vigna_e_vitigno_dei_vigneti()
    {
        var controller = new VignetiController(new InMemoryVignetiRepository(), PerimetroAziendeTests.Perimetro());

        var vigneti = Assert.IsAssignableFrom<IEnumerable<VignetoApi>>(
            Assert.IsType<OkObjectResult>((await controller.GetVignetiPerAzienda(1, CancellationToken.None)).Result).Value).ToList();

        Assert.All(vigneti, v => Assert.Equal(10, v.VignaId));
        Assert.Contains(vigneti, v => v.VitignoId == 100 && v.Varieta == "Sangiovese");
        Assert.All(vigneti, v => Assert.False(string.IsNullOrWhiteSpace(v.Vigna)));
    }
}
