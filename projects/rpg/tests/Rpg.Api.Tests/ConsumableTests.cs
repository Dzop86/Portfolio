using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The potions on the server (sprint 111; out of a fight only since sprint 115, D77): bought, drunk out of a fight, used up.</summary>
public class ConsumableTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APotionBoughtFromRose_HealsOutOfAFight_AndIsRefusedInOne()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pot");
        CharacterSummary c = await server.CreateCharacter("Fiole", "female-c", "guard", cancel: Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            Cell square = GameData.Embedded.Towns[Shop.Village].Spawn;
            (ch.Shards, ch.Town, ch.X, ch.Y) = (40, Shop.Village, square.X, square.Y);
            ch.SetLife(10, api.Clock.GetUtcNow());
            return db.SaveChanges();
        });
        // At Rose's, 10 Éclats and one a level.
        Assert.Equal(14L, (await server.Buy(c.Id, "healing-potion", 2, Cancel)).Shards);
        CharacterSummary healed = await server.Eat(c.Id, "healing-potion", Cancel);
        int max = Life.MaxHp(healed.Hero, GameData.Embedded);
        Assert.Equal(Math.Min(max, 10 + GameData.Embedded.Items["healing-potion"].Heal), healed.LifeAt(api.Clock.GetUtcNow(), GameData.Embedded));
        Assert.Equal(1, healed.Inventory!.Single(i => i.Item == "healing-potion").Count);
        // In a fight, not at all.
        await server.StartFight(c.Id, "training", Cancel);
        var refused = await Assert.ThrowsAsync<ServerException>(() => server.Eat(c.Id, "healing-potion", Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
    }
}
