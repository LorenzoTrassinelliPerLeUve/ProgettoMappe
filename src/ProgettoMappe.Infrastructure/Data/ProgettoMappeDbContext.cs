using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Data;

/// <summary>
/// Contesto EF Core verso il database 4Grapes (SQL Server). Lo schema è un primo abbozzo:
/// verrà rifinito nel mese 1 del piano, quando saranno definiti l'accesso e le API reali.
/// </summary>
public class ProgettoMappeDbContext : DbContext
{
    public ProgettoMappeDbContext(DbContextOptions<ProgettoMappeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Azienda> Aziende => Set<Azienda>();

    public DbSet<Vigneto> Vigneti => Set<Vigneto>();

    public DbSet<LayerMappa> Layer => Set<LayerMappa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Azienda>(entity =>
        {
            entity.Property(a => a.Nome).HasMaxLength(200).IsRequired();
            entity.Property(a => a.CodiceFourGrapes).HasMaxLength(50);
        });

        modelBuilder.Entity<Vigneto>(entity =>
        {
            entity.Property(v => v.Nome).HasMaxLength(200).IsRequired();
            entity.Property(v => v.CodiceFourGrapes).HasMaxLength(50);
            entity.Property(v => v.SuperficieEttari).HasColumnType("decimal(10,4)");

            entity.HasOne(v => v.Azienda)
                .WithMany(a => a.Vigneti)
                .HasForeignKey(v => v.AziendaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LayerMappa>(entity =>
        {
            entity.Property(l => l.Nome).HasMaxLength(200).IsRequired();

            entity.HasOne(l => l.Vigneto)
                .WithMany(v => v.Layer)
                .HasForeignKey(l => l.VignetoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
