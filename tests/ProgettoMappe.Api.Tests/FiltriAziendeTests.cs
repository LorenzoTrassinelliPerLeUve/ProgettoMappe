using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Web.Filtri;
using ProgettoMappe.Web.Services;
using Xunit;
using GruppoApi = ProgettoMappe.Api.Contracts.GruppoDto;

namespace ProgettoMappe.Api.Tests;

public class FiltriAziendeTests
{
    private static readonly AziendaDto[] Aziende =
    [
        new(3, "Alfa", null, null),
        new(1, "Beta", null, null),
        new(2, "Gamma", null, null),
    ];

    private static readonly GruppoDto Gruppo = new(7, "Consorzio", false, [2, 3, 99]);

    [Fact]
    public void Senza_gruppo_restano_tutte_le_aziende()
    {
        Assert.Equal([3, 1, 2], FiltriAziende.DelGruppo(Aziende, null).Select(a => a.Id));
    }

    [Fact]
    public void Il_gruppo_tiene_solo_i_membri_disponibili_nell_ordine_dell_elenco()
    {
        // 99 non è tra le aziende disponibili: ignorata.
        Assert.Equal([3, 2], FiltriAziende.DelGruppo(Aziende, Gruppo).Select(a => a.Id));
    }

    [Fact]
    public void L_azienda_corrente_resta_se_fa_parte_del_gruppo()
    {
        var delGruppo = FiltriAziende.DelGruppo(Aziende, Gruppo);

        Assert.Equal(2, FiltriAziende.AziendaDaMostrare(delGruppo, 2));
        Assert.Equal(3, FiltriAziende.AziendaDaMostrare(delGruppo, 1));
        Assert.Null(FiltriAziende.AziendaDaMostrare([], 1));
    }

    [Fact]
    public void L_etichetta_mostra_conteggio_e_gruppo_concluso()
    {
        Assert.Equal("Consorzio (2)", FiltriAziende.Etichetta(Gruppo, 2));
        Assert.Equal("Consorzio (2) – concluso", FiltriAziende.Etichetta(Gruppo with { Concluso = true }, 2));
    }

    [Fact]
    public async Task L_api_restituisce_i_gruppi_ordinati_con_lo_stato_concluso()
    {
        var controller = new GruppiController(PerimetroAziendeTests.Perimetro());

        var risultato = await controller.GetGruppi(CancellationToken.None);

        var gruppi = Assert.IsAssignableFrom<IEnumerable<GruppoApi>>(Assert.IsType<OkObjectResult>(risultato.Result).Value).ToList();
        Assert.Equal(["Gruppo concluso di prova", "Gruppo di prova"], gruppi.Select(g => g.Nome));
        Assert.True(gruppi[0].Concluso);
        Assert.False(gruppi[1].Concluso);
        Assert.Equal([1, 2], gruppi[1].AziendeIds);
    }
}
