using Microsoft.EntityFrameworkCore;

namespace ProgettoMappe.Infrastructure.FourGrapes;

/// <summary>
/// Contesto EF Core in sola lettura verso il database 4Grapes: un sistema esistente che
/// questa applicazione non deve modificare. Niente migration, niente EnsureCreated/
/// EnsureDeleted, niente seed verso questo database. Il tracking delle query è disattivato
/// e le scritture sono bloccate esplicitamente a livello di codice.
///
/// I DbSet verso le tabelle reali (aziende, vigneti, geometrie, ...) vanno aggiunti solo
/// dopo aver analizzato lo schema effettivo di 4Grapes (nomi tabelle/colonne, relazioni,
/// SRID, formato delle geometrie) — tipicamente con "dotnet ef dbcontext scaffold" a
/// partire dal database esistente, mai con "dotnet ef migrations add". Finché questa
/// analisi non è stata fatta, il contesto resta volutamente senza DbSet.
/// </summary>
public class FourGrapesDbContext : DbContext
{
    public FourGrapesDbContext(DbContextOptions<FourGrapesDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(
            "FourGrapesDbContext è di sola lettura: il database 4Grapes non deve essere scritto da questa applicazione.");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "FourGrapesDbContext è di sola lettura: il database 4Grapes non deve essere scritto da questa applicazione.");
}
