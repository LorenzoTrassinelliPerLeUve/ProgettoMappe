using Microsoft.AspNetCore.Mvc;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class AziendeControllerTests
{
    [Fact]
    public async Task GetAziende_restituisce_le_aziende_in_ordine_alfabetico()
    {
        var controller = new AziendeController(new InMemoryAziendeRepository());

        var risultato = await controller.GetAziende(CancellationToken.None);

        var aziende = Assert.IsAssignableFrom<IEnumerable<AziendaDto>>(
            Assert.IsType<OkObjectResult>(risultato.Result).Value).ToList();
        Assert.NotEmpty(aziende);
        Assert.Equal(aziende.Select(a => a.Nome).OrderBy(n => n, StringComparer.Ordinal), aziende.Select(a => a.Nome));
    }

    [Fact]
    public async Task GetAzienda_restituisce_NotFound_se_non_esiste()
    {
        var controller = new AziendeController(new InMemoryAziendeRepository());

        var risultato = await controller.GetAzienda(9999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(risultato.Result);
    }
}
