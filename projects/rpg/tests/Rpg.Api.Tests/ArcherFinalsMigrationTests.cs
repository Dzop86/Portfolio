using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Api.Data.Migrations;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The Archer's tree of four ranks (sprint 198, D96, docs/classes-archer.md §4), on PostgreSQL: an Archer of 100 on a lineage that stays takes its final class taken back,
/// the Portcullis and the Hedgehopper give way to their sister in the branch kept, their final class to choose; nobody else changes, and the migration goes back.
/// </summary>
public class ArcherFinalsMigrationTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private const string Before = "20261106115728_WarriorFinals";

    [Fact]
    public void EachTakenBackFinal_IsTheFirstOfItsLineage_AndEachSisterIsInTheBranchKept()
    {
        SpecFork[] forks = [.. GameData.Embedded.Specs.Values.Where(s => s.Class == "archer").SelectMany(s => s.Forks)];
        Assert.All(ArcherFinals.Taken, t => Assert.Equal(t.Final, forks.Single(f => f.Id == t.Fork).Finals![0].Id));
        Assert.All(ArcherFinals.Moved, m => Assert.NotNull(GameData.Embedded.Specs[m.Spec].ForkOf(m.To)));
        Assert.All(ArcherFinals.Moved, m => Assert.Null(GameData.Embedded.Specs[m.Spec].ForkOf(m.From)));
    }

    [Fact]
    public async Task TheMigration_GivesTheArchersOf100TheirFinal_MovesTheLineagesGone_AndNobodyElse()
    {
        var api = new ApiFactory();
        await using (api)
        {
            GameServer server = await api.SignedIn("archers");
            foreach ((string name, string cls) in new[] { ("Aube", "archer"), ("Brume", "archer"), ("Cendre", "archer"), ("Dune", "archer"), ("Eider", "warrior"), ("Fenouil", "archer") })
                await server.CreateCharacter(name, "female-a", cls, cancel: Cancel);
            api.WithDb(db =>
            {
                foreach (Character c in db.Characters)
                {
                    (c.Level, c.Spec, c.Fork) = c.Name switch
                    {
                        "Aube" => (100, "sightline", "marksman"),
                        "Brume" => (100, "sightline", "barrage"),
                        "Cendre" => (60, "trail", "leaper"),
                        "Dune" => (100, "ambush", "falconer"),
                        "Eider" => (100, "palisade", "rampart"),
                        _ => (40, "trail", null),
                    };
                }
                return db.SaveChanges();
            });
            (string?, string?, string?)[] Rows(GameDb db) => [.. db.Characters.AsNoTracking().OrderBy(c => c.Name).Select(c => new { c.Spec, c.Fork, c.Final }).AsEnumerable()
                .Select(c => (c.Spec, c.Fork, c.Final))];
            api.WithDb(db =>
            {
                IMigrator migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
                migrator.Migrate(Before);
                // Eider, a Warrior, stays as it was: this migration reads the Archers alone.
                Assert.Equal([("sightline", "marksman", null), ("sightline", "barrage", null), ("trail", "leaper", null), ("ambush", "falconer", null), ("palisade", "rampart", null), ("trail", null, null)],
                    Rows(db));
                db.Database.Migrate();
                Assert.Equal([("sightline", "marksman", "dragon-eye"), ("sightline", "bounty-hunter", null), ("trail", "runner", null), ("ambush", "falconer", "beast-lord"),
                    ("palisade", "rampart", null), ("trail", null, null)], Rows(db));
                migrator.Migrate(Before);
                Assert.Equal([("sightline", "marksman", null), ("sightline", "barrage", null), ("trail", "runner", null), ("ambush", "falconer", null), ("palisade", "rampart", null), ("trail", null, null)],
                    Rows(db));
                db.Database.Migrate();
                return 0;
            });
            Assert.All(await server.Characters(Cancel), c => Assert.Null(c.Hero.Problem(GameData.Embedded)));
            Assert.Equal("Œil du dragon", Specs.TitleOf((await server.Characters(Cancel)).Single(c => c.Name == "Aube").Hero, GameData.Embedded)!.Fr);
        }
    }
}
