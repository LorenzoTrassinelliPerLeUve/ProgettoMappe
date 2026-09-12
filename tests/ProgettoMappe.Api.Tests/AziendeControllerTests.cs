using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Data;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class AziendeControllerTests
{
    private static ProgettoMappeDbContext CreaDbContextInMemory()
    {
        var options = new DbContextOptionsBuilder<ProgettoMappeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProgettoMappeDbContext(options);
    }

    [Fact]
    public async Task GetAziende_restituisce_le_aziende_in_ordine_alfabetico()
    {
        await using var db = CreaDbContextInMemory();
        db.Aziende.AddRange(
            new Azienda { Nome = "Tenuta Beta" },
            new Azienda { Nome = "Azienda Alfa" });
        await db.SaveChangesAsync();

        var controller = new AziendeController(db);

        var risultato = await controller.GetAziende(CancellationToken.None);

        var aziende = Assert.IsAssignableFrom<IEnumerable<AziendaDto>>(
            Assert.IsType<OkObjectResult>(risultato.Result).Value);
        Assert.Equal(["Azienda Alfa", "Tenuta Beta"], aziende.Select(a => a.Nome));
    }
}
