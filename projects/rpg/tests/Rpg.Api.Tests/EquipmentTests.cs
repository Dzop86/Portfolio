using System.Net;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Loot, the inventory and what a character wears, on the server (sprint 59).</summary>
public class EquipmentTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>Wins training fights until the first one, which does the quest; returns the character after it.</summary>
    private static async Task<CharacterSummary> FirstWin(GameServer server, CharacterSummary c, List<FightResult> results)
    {
        for (int i = 0; i < 12; i++)
        {
            FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
            CharacterSummary now = (await server.Characters(Cancel)).Single();
            var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, now.Hero);
            Ai.PlayOut(fight);
            FightResult result = await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(fight), Cancel);
            results.Add(result);
            Assert.Equal(Equipment.Loot(fight).Concat(result.Quest is null ? [] : GameData.Embedded.Quests[result.Quest].Items!), result.Loot!);
            if (fight.WinningTeam == 0)
                return (await server.Characters(Cancel)).Single();
        }
        throw new InvalidOperationException("Twelve training fights without a win.");
    }

    [Fact]
    public async Task WhatAFightLeaves_GoesToTheInventory_TheQuestsItemsToo()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "sentinel", cancel: Cancel);
        Assert.Empty(c.Inventory ?? []);
        var results = new List<FightResult>();
        CharacterSummary after = await FirstWin(server, c, results);
        var expected = results.SelectMany(r => r.Loot!).GroupBy(i => i.Item).Select(g => new ItemCount(g.Key, g.Sum(i => i.Count))).OrderBy(i => i.Item, StringComparer.Ordinal);
        Assert.Equal(expected, after.Inventory!);
        Assert.Contains(new ItemCount("garance-badge", 1), after.Inventory!);
        Assert.Contains(new ItemCount("copper-ring", 1), after.Inventory!);
    }

    [Fact]
    public async Task ACharacter_WearsWhatItOwns_WhereItFits_AndFightsWithIt()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "guard", cancel: Cancel);
        c = await FirstWin(server, c, []);
        CharacterSummary wearing = await server.SaveEquipment(c.Id, new Dictionary<Slot, string> { [Slot.Ring2] = "copper-ring" }, Cancel);
        Assert.Equal("copper-ring", wearing.Worn![Slot.Ring2]);
        Assert.Equal(wearing, (await server.Characters(Cancel)).Single());
        // Two rings need two; a dagger not owned; a ring on the head; a quest badge on a finger.
        foreach ((Dictionary<Slot, string> wrong, string why) in new[]
        {
            (new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring", [Slot.Ring2] = "copper-ring" }, "not owned, or not that many"),
            (new Dictionary<Slot, string> { [Slot.OneHanded] = "dagger", [Slot.Ring1] = "copper-ring" }, wearing.Inventory!.Any(i => i.Item == "dagger") ? "" : "'dagger': not owned"),
            (new Dictionary<Slot, string> { [Slot.Hat] = "copper-ring" }, "does not go in Hat"),
            (new Dictionary<Slot, string> { [Slot.Ring1] = "garance-badge" }, "does not go in Ring1"),
        })
        {
            if (why.Length == 0)
                continue;
            var e = await Assert.ThrowsAsync<ServerException>(() => server.SaveEquipment(c.Id, wrong, Cancel));
            Assert.Equal(HttpStatusCode.BadRequest, e.Status);
            Assert.Contains(why, e.Message, StringComparison.Ordinal);
        }
        // The next fight is played with the ring: without it, the server refuses the record.
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        var bare = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, wearing.Hero with { Worn = null });
        Ai.PlayOut(bare);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, FightRecord.Of(bare), Cancel))).Status);
        ticket = await server.StartFight(c.Id, "training", Cancel);
        var ringed = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, wearing.Hero);
        Assert.Null(wearing.Hero.Problem(GameData.Embedded));
        Assert.Equal(GameData.Embedded.Class("guard")!.Hp + 5, ringed.Fighters[0].MaxHp - GameData.Embedded.Class("guard")!.HpPerLevel * (wearing.Level - 1));
        Ai.PlayOut(ringed);
        await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(ringed), Cancel);
        // Taking it off.
        Assert.Empty((await server.SaveEquipment(c.Id, new Dictionary<Slot, string>(), Cancel)).Worn!);
    }
}
