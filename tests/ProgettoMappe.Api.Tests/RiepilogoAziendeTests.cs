using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class RiepilogoAziendeTests
{
    private static RiepilogoAziendeController Controller(
        int[]? gruppi = null, IVignetiRepository? vigneti = null, IIndiciBigotRepository? indici = null) =>
        new(new InMemoryAziendeRepository(), CacheVignetiAziendeTests.Cache(vigneti),
            indici ?? new InMemoryIndiciBigotRepository(), PerimetroAziendeTests.Perimetro(gruppi ?? []));

    private static RiepilogoAziendeDto Valore(ActionResult<RiepilogoAziendeDto> risultato) =>
        Assert.IsType<RiepilogoAziendeDto>(Assert.IsType<OkObjectResult>(risultato.Result).Value);

    private static IndiceBigotVigneto Indice(int anno, int idVigneto, int? parametri, int? punteggio) =>
        new(anno, idVigneto, parametri, punteggio, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Riepilogo_con_vigneti_ettari_e_punteggio_medio_dell_ultimo_anno()
    {
        var riepilogo = Valore(await Controller().GetRiepilogo(CancellationToken.None));

        Assert.True(riepilogo.BigotPronto);
        Assert.Equal(2025, riepilogo.AnnoBigot);
        Assert.Equal(riepilogo.Aziende.Select(a => a.Nome).Order(StringComparer.CurrentCultureIgnoreCase), riepilogo.Aziende.Select(a => a.Nome));
        var tenuta = Assert.Single(riepilogo.Aziende, a => a.Id == 1);
        Assert.Equal(2, tenuta.NumeroVigneti);
        Assert.Equal(5.0m, tenuta.Ettari);
        Assert.Equal(72.5m, tenuta.PunteggioBigot); // (84 + 61) / 2, entrambi 9/9 nel 2025
        Assert.Equal(2, tenuta.VignetiConPunteggio);
    }

    [Fact]
    public async Task Fuori_perimetro_un_azienda_non_compare()
    {
        var riepilogo = Valore(await Controller(gruppi: [2]).GetRiepilogo(CancellationToken.None));

        Assert.Equal([2], riepilogo.Aziende.Select(a => a.Id));
    }

    [Fact]
    public async Task Le_superfici_si_leggono_una_volta_sola_per_piu_visite()
    {
        var repository = new CacheVignetiAziendeTests.RepositoryContato();
        var controller = Controller(vigneti: repository);

        await controller.GetRiepilogo(CancellationToken.None);
        await controller.GetRiepilogo(CancellationToken.None);

        Assert.Equal(1, repository.LettureSuperfici);
    }

    [Fact]
    public void Indici_non_pronti_niente_punteggi_ma_numeri_si()
    {
        var riepilogo = RiepilogoAziendeController.Calcola(
            [new Azienda { Id = 1, Nome = "A" }], [new SuperficieVigneto(10, 1, 2m)], null, 2026);

        Assert.False(riepilogo.BigotPronto);
        Assert.Null(riepilogo.AnnoBigot);
        var azienda = Assert.Single(riepilogo.Aziende);
        Assert.Equal((1, 2m, (decimal?)null, 0), (azienda.NumeroVigneti, azienda.Ettari, azienda.PunteggioBigot, azienda.VignetiConPunteggio));
    }

    [Fact]
    public void Media_solo_dei_vigneti_completi_dell_anno_e_superfici_mancanti_contate()
    {
        IReadOnlyList<Azienda> aziende = [new() { Id = 1, Nome = "Bianchi" }, new() { Id = 2, Nome = "Aldi" }, new() { Id = 3, Nome = "Conti" }];
        IReadOnlyList<SuperficieVigneto> vigneti =
        [
            new(10, 1, 1.5m), new(11, 1, null), new(12, 1, 2.25m),
            new(20, 2, 4m),
        ];
        IReadOnlyList<IndiceBigotVigneto> indici =
        [
            Indice(2025, 10, 9, 80), Indice(2025, 12, 9, 71),
            Indice(2025, 11, 8, 10),   // incompleto: non confrontabile, escluso
            Indice(2024, 10, 9, 20),   // anno precedente: escluso
            Indice(2027, 10, 9, 99),   // anno futuro (dato sporco): non sceglie l'anno
            Indice(2025, 20, 5, 50),   // l'azienda 2 non ha vigneti completi
            Indice(2025, 99, 9, 1),    // vigneto fuori dalle aziende: ignorato
        ];

        var riepilogo = RiepilogoAziendeController.Calcola(aziende, vigneti, indici, 2026);

        Assert.Equal(2025, riepilogo.AnnoBigot); // il 2026 non ha ancora indici
        Assert.Equal(["Aldi", "Bianchi", "Conti"], riepilogo.Aziende.Select(a => a.Nome));
        var bianchi = riepilogo.Aziende[1];
        Assert.Equal((3, 3.75m, 1, 75.5m, 2),
            (bianchi.NumeroVigneti, bianchi.Ettari, bianchi.VignetiSenzaSuperficie, bianchi.PunteggioBigot, bianchi.VignetiConPunteggio));
        Assert.Null(riepilogo.Aziende[0].PunteggioBigot);
        var conti = riepilogo.Aziende[2];
        Assert.Equal((0, 0m, (decimal?)null), (conti.NumeroVigneti, conti.Ettari, conti.PunteggioBigot));
    }

    [Fact]
    public void I_vigneti_solo_punto_contano_come_vigneti_ma_non_negli_ettari()
    {
        IReadOnlyList<SuperficieVigneto> vigneti =
        [
            new(10, 1, 1.5m), new(11, 1, 3m, SoloPunto: true), new(12, 1, null, SoloPunto: true), new(13, 1, null),
        ];

        var azienda = Assert.Single(RiepilogoAziendeController.Calcola([new Azienda { Id = 1, Nome = "A" }], vigneti, null, 2026).Aziende);

        Assert.Equal((4, 1.5m, 1, 2),
            (azienda.NumeroVigneti, azienda.Ettari, azienda.VignetiSenzaSuperficie, azienda.VignetiSoloPunto));
    }
}
