using Microsoft.EntityFrameworkCore;

namespace Rogue.Api.Data;

public sealed class ScoresDb(DbContextOptions<ScoresDb> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<RankedRun> Runs => Set<RankedRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(player =>
        {
            player.ToTable("players");
            player.Property(p => p.Name).HasMaxLength(20);
            player.Property(p => p.NormalizedName).HasMaxLength(20);
            player.HasIndex(p => p.NormalizedName).IsUnique();
        });
        modelBuilder.Entity<RankedRun>(run =>
        {
            run.ToTable("runs");
            // PostgreSQL has no unsigned 64-bit integer: numeric(20, 0) holds every seed and stays readable.
            run.Property(r => r.Seed).HasConversion<decimal>().HasColumnType("numeric(20,0)");
            run.Property(r => r.Status).HasConversion<string>().HasMaxLength(10);
            run.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(10);
            run.Property(r => r.Rejection).HasMaxLength(200);
            run.HasOne(r => r.Player).WithMany(p => p.Runs).HasForeignKey(r => r.PlayerId);
            run.Property(r => r.Version).IsRowVersion();
            run.HasIndex(r => new { r.Status, r.Score });
        });
    }
}
