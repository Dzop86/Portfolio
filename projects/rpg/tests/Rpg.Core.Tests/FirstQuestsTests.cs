using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The first quests (sprint 89, D65): the start of the main quest from Ysaline, one or two side
/// quests in every zone, one quest for every dungeon; each one can be done, and none blocks another.
/// </summary>
public class FirstQuestsTests
{
    private static readonly string[] Main = ["whispering-stone", "restless-beasts", "rumble-under-the-wallow", "four-trails"];

    private static int ZoneLevel(string zone) => Real.Towns.Values.Where(t => t.Zone == zone).Min(t => t.Level);

    [Fact]
    public void TheMainQuest_StartsAtTheStone_EachStepAfterTheOneBefore_UpToTheFourTrails()
    {
        Quest[] main = [.. Main.Select(id => Real.Quests[id])];
        Assert.All(main, q => Assert.Equal("clairval/ysaline", q.Giver));
        Assert.Null(main[0].After);
        for (int i = 1; i < main.Length; i++)
        {
            Assert.Equal([main[i - 1].Id], main[i].After!);
            Assert.True(main[i].Level >= main[i - 1].Level);
        }
        // The stone's fifth sign leads under the Boar King's wallow, then to the four trails of the four Primordials.
        Assert.Contains(main[2].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "boar-wallow");
        // The main quest is done alone at its level (sprint 95, D71): never a dungeon's last room, made for
        // several; the key to go in is given with the quest.
        Assert.All(main.SelectMany(q => q.Objectives!).Where(o => o.Goal == QuestGoal.Dungeon), o => Assert.InRange(o.Room, 1, 2));
        Assert.Equal([new ItemCount("boar-wallow-key", 1)], main[2].Grants!);
        Assert.Equal(["clairval/rose", "clairval/garance", "clairval/aubin"], main[3].Objectives!.Select(o => o.Target));
        Assert.Equal((20, 12), (Real.FindNpc("clairval/ysaline")!.Value.Npc.At.X, Real.FindNpc("clairval/ysaline")!.Value.Npc.At.Y));
    }

    [Fact]
    public void EveryZone_HasOneOrTwoSideQuests_AndEveryDungeonOne_AllGivenInTheVillage()
    {
        Quest[] side = [.. Real.Quests.Values.Where(q => q.IsGiven && !Main.Contains(q.Id) && !q.Id.EndsWith("-cleared", StringComparison.Ordinal))];
        foreach (string zone in Real.Families.Values.Select(f => f.Zone).Distinct())
            Assert.InRange(side.Count(q => q.Zone == zone), 1, 2);
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Quest q = Real.Quests[d.Id + "-cleared"];
            Assert.Equal([new QuestObjective(QuestGoal.Dungeon, d.Id)], q.Objectives!);
            Assert.Equal(Real.Families[d.Family].Zone, q.Zone);
        }
        // Handed back in the village: never to someone behind a dungeon's door, which needs a key.
        Assert.All(Real.Quests.Values.Where(q => q.IsGiven), q => Assert.StartsWith("clairval/", q.Giver, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryQuest_CanBeDone_InItsZone_AroundItsLevel_AndNoneWaitsForALaterOne()
    {
        foreach (Quest q in Real.Quests.Values.Where(q => q.IsGiven))
        {
            if (q.Zone is string zone)
                Assert.InRange(q.Level, 1, ZoneLevel(zone) + 3);
            foreach (QuestObjective o in q.Objectives!)
            {
                switch (o.Goal)
                {
                    case QuestGoal.Defeat:
                        Assert.Equal(q.Zone, Real.Families[o.Target].Zone);
                        break;
                    case QuestGoal.Bring:
                        // Left by a monster of the zone.
                        Assert.Contains(Real.Families.Values.Where(f => f.Zone == q.Zone).SelectMany(f => f.Monsters).SelectMany(m => m.Loot ?? []), l => l.Item == o.Target);
                        break;
                }
            }
            // The quests it follows can be taken as early.
            Assert.All(q.After ?? [], a => Assert.True(Real.Quests[a].Level <= q.Level, $"{q.Id} after {a}"));
            Assert.True(q.Xp >= 50 && q.Shards > 0, q.Id);
        }
        // A new hero can take something from the first day.
        Assert.Contains(Real.Quests.Values, q => q.IsGiven && Quests.WhyNot(q, 1, [], []) is null);
    }
}
