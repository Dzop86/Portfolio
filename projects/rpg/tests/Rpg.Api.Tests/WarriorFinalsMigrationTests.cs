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
/// The Warrior's lineages end in final classes (sprint 197, D96, docs/classes-guerrier.md §6), on PostgreSQL: a Warrior of 100 on a lineage takes the final class taken back
/// from its old evolution at 100, and fights as before; nobody else changes, and the migration goes back.
/// </summary>
public class WarriorFinalsMigrationTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private const string Before = "20261105183526_ConvertClasses";

    [Fact]
    public void EachTakenBackFinal_IsTheFirstOfItsLineage_InTheData()
    {
        SpecFork[] forks = [.. GameData.Embedded.Specs.Values.Where(s => s.Class == "warrior").SelectMany(s => s.Forks)];
        Assert.Equal(forks.Select(f => (f.Id, f.Finals![0].Id)).Order(), WarriorFinals.Taken.Order());
    }

    [Fact]
    public async Task TheMigration_GivesTheWarriorsOf100TheirTakenBackFinal_AndNobodyElse()
    {
        var api = new ApiFactory();
        await using (api)
        {
            GameServer server = await api.SignedIn("finals");
            foreach ((string name, string cls) in new[] { ("Aube", "warrior"), ("Brume", "warrior"), ("Cendre", "warrior"), ("Dune", "archer"), ("Eider", "warrior") })
                await server.CreateCharacter(name, "female-a", cls, cancel: Cancel);
            api.WithDb(db =>
            {
                foreach (Character c in db.Characters)
                {
                    (c.Level, c.Spec, c.Fork) = c.Name switch
                    {
                        "Aube" => (100, "palisade", "rampart"),
                        "Brume" => (100, "breach", "harpooner"),
                        "Cendre" => (60, "grip", "jailer"),
                        "Dune" => (100, "sightline", "marksman"),
                        _ => (100, "palisade", null),
                    };
                }
                return db.SaveChanges();
            });
            string?[] Finals(GameDb db) => [.. db.Characters.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Final)];
            api.WithDb(db =>
            {
                IMigrator migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
                migrator.Migrate(Before);
                Assert.All(Finals(db), Assert.Null);
                db.Database.Migrate();
                Assert.Equal(["crusader", "arena-champion", null, null, null], Finals(db));
                migrator.Migrate(Before);
                Assert.All(Finals(db), Assert.Null);
                db.Database.Migrate();
                return 0;
            });
            Assert.All(await server.Characters(Cancel), c => Assert.Null(c.Hero.Problem(GameData.Embedded)));
            Assert.Equal("Croisé", Specs.TitleOf((await server.Characters(Cancel)).Single(c => c.Name == "Aube").Hero, GameData.Embedded)!.Fr);
        }
    }
}
