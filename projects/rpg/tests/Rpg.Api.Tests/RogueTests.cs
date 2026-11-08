using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Rogue by the API, on PostgreSQL (sprint 204, docs/classes-voleur.md): created like any class, with its kit and its engine.</summary>
public class RogueTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ARogue_IsCreated_WithItsKitAndItsEngine()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("ro1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "rogue", cancel: Cancel);
        Assert.Equal("rogue", c.Class);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.Contains("fade", GameData.Embedded.Class(c.Class)!.Spells);
        // Read back from the server, it fights with its class's rule.
        CharacterSummary read = (await server.Characters(Cancel)).Single(x => x.Id == c.Id);
        var fight = new Fight(GameData.Embedded, "training", 1, read.Hero);
        Assert.Contains(fight.HeroFighter!.Talents.SelectMany(t => t.Rules), r => r is IsolatedRule);
    }
}
