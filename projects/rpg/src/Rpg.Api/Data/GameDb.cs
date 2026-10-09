using Microsoft.EntityFrameworkCore;

namespace Rpg.Api.Data;

public sealed class GameDb(DbContextOptions<GameDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Character> Characters => Set<Character>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable("accounts");
            account.Property(a => a.Name).HasMaxLength(20);
            account.Property(a => a.NormalizedName).HasMaxLength(20);
            account.HasIndex(a => a.NormalizedName).IsUnique();
        });
        modelBuilder.Entity<Character>(character =>
        {
            character.ToTable("characters");
            character.Property(c => c.Name).HasMaxLength(20);
            character.Property(c => c.NormalizedName).HasMaxLength(20);
            character.Property(c => c.Look).HasMaxLength(20);
            character.HasIndex(c => c.NormalizedName).IsUnique();
            character.HasOne(c => c.Account).WithMany(a => a.Characters).HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
