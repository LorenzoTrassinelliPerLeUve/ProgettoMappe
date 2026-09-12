using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProgettoMappe.Infrastructure.Data;

/// <summary>
/// Factory usata dagli strumenti EF Core (es. "dotnet ef migrations add") per creare il DbContext
/// in fase di design, leggendo la connection string da appsettings.Development.json dell'Api.
/// </summary>
public class ProgettoMappeDbContextFactory : IDesignTimeDbContextFactory<ProgettoMappeDbContext>
{
    public ProgettoMappeDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ProgettoMappe.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("FourGrapes")
            ?? "Server=localhost;Database=ProgettoMappe;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<ProgettoMappeDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ProgettoMappeDbContext(optionsBuilder.Options);
    }
}
