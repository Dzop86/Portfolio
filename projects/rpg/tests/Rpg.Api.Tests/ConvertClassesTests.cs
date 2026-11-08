using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Guard becomes the Warrior and the Sentinel the Archer, on PostgreSQL (sprint 195, D96): the migration both ways, nothing lost.</summary>
public class ConvertClassesTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private const string Before = "20261105180920_AddFinal";

    private static string[] Classes(GameDb db) =>
        [.. db.Characters.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Class)];

    [Fact]
    public async Task TheMigration_ConvertsTheHeroes_AndKeepsAllTheyHave()
    {
        var api = new ApiFactory();
        await using (api)
        {
            GameServer server = await api.SignedIn("conversion");
            await server.CreateCharacter("Aube", "female-a", "archer", cancel: Cancel);
            await server.CreateCharacter("Brume", "female-a", "warrior", cancel: Cancel);
            await server.CreateCharacter("Cendre", "female-a", "mage", cancel: Cancel);
            api.WithDb(db =>
            {
                foreach (Character c in db.Characters)
                    (c.Level, c.Xp, c.Shards, c.Spec, c.Fork, c.Ranks, c.Deck) = (50, 1234, 77, c.Class == "warrior" ? "palisade" : null, null, "", "");
                return db.SaveChanges();
            });
            (int, long, long, string?)[] Kept(GameDb db) => [.. db.Characters.AsNoTracking().OrderBy(c => c.Name).Select(c => new { c.Level, c.Xp, c.Shards, c.Spec }).AsEnumerable().Select(c => (c.Level, c.Xp, c.Shards, c.Spec))];
            (int, long, long, string?)[] before = api.WithDb(Kept);
            api.WithDb(db =>
            {
                db.GetInfrastructure().GetRequiredService<IMigrator>().Migrate(Before);
                Assert.Equal(["sentinel", "guard", "mage"], Classes(db));
                db.Database.Migrate();
                Assert.Equal(["archer", "warrior", "mage"], Classes(db));
                return 0;
            });
            Assert.Equal(before, api.WithDb(Kept));
            Assert.All(await server.Characters(Cancel), c => Assert.NotNull(GameData.Embedded.Class(c.Class!)));
        }
    }

    [Theory]
    [InlineData("guard")]
    [InlineData("sentinel")]
    // Sprint 208: the Paladin is in the game, the Druid not yet.
    [InlineData("druid")]
    public async Task AClassNoLongerOrNotYetInTheGame_IsRefused(string id)
    {
        var api = new ApiFactory();
        await using (api)
        {
            GameServer server = await api.SignedIn("refused" + id);
            await Assert.ThrowsAsync<ServerException>(() => server.CreateCharacter("Aube", "female-a", id, cancel: Cancel));
        }
    }
}
