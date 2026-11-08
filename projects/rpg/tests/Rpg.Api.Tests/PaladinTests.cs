using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Paladin by the API, on PostgreSQL (sprint 208, docs/classes-paladin.md): created like any class, and the Warriors who had its Aegis.</summary>
public class PaladinTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APaladin_IsCreated_WithTheAegis()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pa1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "paladin", cancel: Cancel);
        Assert.Equal("paladin", c.Class);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.Contains("aegis", GameData.Embedded.Class(c.Class)!.Spells);
    }

    [Fact]
    public async Task AWarriorWithRanksInTheAegis_IsReadWithoutThem()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pa2");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary bastien = await server.CreateCharacter("Bastien", "male-a", "warrior", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 100, []), Cancel);
        // Ranks and a deck saved before the sprint: the Aegis was the Warrior's.
        api.WithDb(db =>
        {
            Character warrior = db.Characters.Single(x => x.Id == bastien.Id);
            (warrior.Ranks, warrior.Deck) = ("aegis:4,cleave:3", "aegis,cleave,taunt");
            return db.SaveChanges();
        });
        CharacterSummary read = (await server.Characters(Cancel)).Single(x => x.Name == "Bastien");
        Assert.Null(read.Hero.Problem(GameData.Embedded));
        Assert.Equal(new Dictionary<string, int> { ["cleave"] = 3 }, read.Hero.Ranks);
        Assert.Equal(["cleave", "taunt"], read.Deck);
    }
}
