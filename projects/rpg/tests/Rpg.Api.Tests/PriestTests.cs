using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Priest by the API, on PostgreSQL (sprint 201, docs/classes-pretre.md): created like any class, and the Mages who had its heals.</summary>
public class PriestTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task Level(ApiFactory api, string account, int level)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, []), Cancel);
    }

    [Fact]
    public async Task APriest_IsCreated_WithItsHeals()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pr1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "priest", cancel: Cancel);
        Assert.Equal("priest", c.Class);
        Assert.Contains("mend", GameData.Embedded.Class(c.Class)!.Spells);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
    }

    [Fact]
    public async Task AMageWithRanksInTheHealsThatLeftIt_IsReadWithoutThem_ItsPointsFreeAgain()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pr2");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary plop = await server.CreateCharacter("Plop", "female-a", "mage", cancel: Cancel);
        await Level(api, account, 100);
        // Ranks and a deck saved before the sprint: Mend and Rebirth were the Mage's.
        api.WithDb(db =>
        {
            Character mage = db.Characters.Single(x => x.Id == plop.Id);
            (mage.Ranks, mage.Deck) = ("mend:5,pebble:3,rebirth:4", "mend,pebble,frost-ward,rebirth");
            return db.SaveChanges();
        });
        CharacterSummary m = (await server.Characters(Cancel)).Single(x => x.Name == "Plop");
        Assert.Null(m.Hero.Problem(GameData.Embedded));
        Assert.Equal(new Dictionary<string, int> { ["pebble"] = 3 }, m.Hero.Ranks);
        Assert.Equal(["pebble", "frost-ward"], m.Deck);
        // The points the heals held are free again: all of them can go on the Mage's own spells.
        int all = Progression.SpellPoints(100);
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal) { ["pebble"] = 5, ["flare"] = 5, ["ice-shard"] = 5, ["gust"] = 5 };
        Assert.True(ranks.Values.Sum(Progression.RankCost) <= all);
        CharacterSummary saved = await server.SavePoints(m.Id, new Points(null, ranks), Cancel);
        Assert.Equal(ranks, saved.Hero.Ranks);
    }
}
