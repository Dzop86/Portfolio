using Microsoft.EntityFrameworkCore;

namespace Rpg.Api.Data;

public sealed class GameDb(DbContextOptions<GameDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<PendingFight> PendingFights => Set<PendingFight>();

    public DbSet<BattleRecord> BattleRecords => Set<BattleRecord>();

    public DbSet<BattleActionRecord> BattleActions => Set<BattleActionRecord>();

    public DbSet<ContactRecord> Contacts => Set<ContactRecord>();

    public DbSet<GuildRecord> Guilds => Set<GuildRecord>();

    public DbSet<GuildMemberRecord> GuildMembers => Set<GuildMemberRecord>();

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
            // Characters made before classes (sprint 47) became sentinels: the hero they already were.
            character.Property(c => c.Class).HasMaxLength(20).HasDefaultValue("sentinel");
            character.Property(c => c.Town).HasMaxLength(20);
            // Characters made before servers live on the first one.
            character.Property(c => c.Server).HasMaxLength(20).HasDefaultValue(Rpg.Core.Servers.Default);
            character.Property(c => c.Level).HasDefaultValue(1);
            character.HasIndex(c => c.NormalizedName).IsUnique();
            character.HasOne(c => c.Account).WithMany(a => a.Characters).HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Cascade);
            character.Property(c => c.Ranks).HasMaxLength(2000).HasDefaultValue("");
            character.Property(c => c.Quests).HasMaxLength(2000).HasDefaultValue("");
            character.Ignore(c => c.Stats);
            character.Ignore(c => c.RankList);
            character.Ignore(c => c.QuestList);
            character.Property(c => c.QuestLog).HasMaxLength(2000).HasDefaultValue("");
            character.Ignore(c => c.ActiveQuests);
            character.Property(c => c.Inventory).HasMaxLength(8000).HasDefaultValue("");
            character.Property(c => c.Worn).HasMaxLength(1000).HasDefaultValue("");
            character.Ignore(c => c.InventoryList);
            character.Ignore(c => c.WornList);
            character.Ignore(c => c.Hero);
            character.Property(c => c.Dungeon).HasMaxLength(40);
            character.Ignore(c => c.Run);
            character.Property(c => c.Jobs).HasMaxLength(1000).HasDefaultValue("");
            character.Ignore(c => c.JobList);
            character.Property(c => c.Waystones).HasMaxLength(1000).HasDefaultValue("");
            character.Ignore(c => c.WaystoneList);
            character.Property(c => c.Home).HasMaxLength(40);
            // Characters made before sprint 146 took their talents in the first tree.
            character.Property(c => c.TalentsVersion).HasDefaultValue(1);
        });
        modelBuilder.Entity<PendingFight>(fight =>
        {
            fight.ToTable("pending_fights");
            fight.Property(f => f.Scenario).HasMaxLength(40);
            fight.HasOne(f => f.Character).WithMany().HasForeignKey(f => f.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });
        // The fights with the party under way (sprint 125): their start, their actions added one by one.
        modelBuilder.Entity<BattleRecord>(battle =>
        {
            battle.ToTable("battles");
            battle.Property(b => b.Id).ValueGeneratedNever();
            battle.Property(b => b.Scenario).HasMaxLength(60);
            battle.Property(b => b.Map).HasMaxLength(40);
            battle.Property(b => b.Engine).HasMaxLength(40);
            battle.HasMany(b => b.Actions).WithOne().HasForeignKey(a => a.BattleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<BattleActionRecord>(action =>
        {
            action.ToTable("battle_actions");
            action.HasKey(a => new { a.BattleId, a.Number });
            action.Property(a => a.Action).HasMaxLength(2000);
        });
        // Each account's friends and ignored characters (sprint 126); gone with either side.
        modelBuilder.Entity<ContactRecord>(contact =>
        {
            contact.ToTable("contacts");
            contact.HasKey(c => new { c.AccountId, c.CharacterId });
            contact.HasOne<Account>().WithMany().HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Cascade);
            contact.HasOne<Character>().WithMany().HasForeignKey(c => c.CharacterId).OnDelete(DeleteBehavior.Cascade);
            contact.HasIndex(c => c.CharacterId);
        });
        // The guilds and their members (sprint 145): one guild a character, gone with the character or the guild.
        modelBuilder.Entity<GuildRecord>(guild =>
        {
            guild.ToTable("guilds");
            guild.Property(g => g.Name).HasMaxLength(20);
            guild.Property(g => g.NormalizedName).HasMaxLength(20);
            guild.HasIndex(g => g.NormalizedName).IsUnique();
        });
        modelBuilder.Entity<GuildMemberRecord>(member =>
        {
            member.ToTable("guild_members");
            member.HasKey(m => m.CharacterId);
            member.Property(m => m.Rank).HasConversion<string>().HasMaxLength(10);
            member.HasOne<Character>().WithMany().HasForeignKey(m => m.CharacterId).OnDelete(DeleteBehavior.Cascade);
            member.HasOne<GuildRecord>().WithMany().HasForeignKey(m => m.GuildId).OnDelete(DeleteBehavior.Cascade);
            member.HasIndex(m => m.GuildId);
        });
    }
}
