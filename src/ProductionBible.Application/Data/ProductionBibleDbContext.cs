using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Data;

public class ProductionBibleDbContext : DbContext
{
    public ProductionBibleDbContext(DbContextOptions<ProductionBibleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Beat> Beats => Set<Beat>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAttribute> AssetAttributes => Set<AssetAttribute>();
    public DbSet<AssetBeat> AssetBeats => Set<AssetBeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetBeat>(e =>
        {
            e.HasKey(ab => new { ab.AssetId, ab.BeatId });
            e.HasOne(ab => ab.Asset).WithMany(a => a.AssetBeats).HasForeignKey(ab => ab.AssetId);
            e.HasOne(ab => ab.Beat).WithMany(b => b.AssetBeats).HasForeignKey(ab => ab.BeatId);
        });

        modelBuilder.Entity<AssetType>().HasIndex(t => t.Name).IsUnique();
        modelBuilder.Entity<Asset>().HasIndex(a => a.Code);

        modelBuilder.Entity<Episode>()
            .HasOne(e => e.Project).WithMany(p => p.Episodes).HasForeignKey(e => e.ProjectId);
        modelBuilder.Entity<Beat>()
            .HasOne(b => b.Episode).WithMany(e => e.Beats).HasForeignKey(b => b.EpisodeId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Episode).WithMany(e => e.Assets).HasForeignKey(a => a.EpisodeId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.AssetType).WithMany().HasForeignKey(a => a.AssetTypeId);
        modelBuilder.Entity<AssetAttribute>()
            .HasOne(at => at.Asset).WithMany(a => a.Attributes).HasForeignKey(at => at.AssetId);
        modelBuilder.Entity<Phase>()
            .HasOne(ph => ph.Project).WithMany().HasForeignKey(ph => ph.ProjectId);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Phase).WithMany(ph => ph.Assets).HasForeignKey(a => a.PhaseId);
    }
}
