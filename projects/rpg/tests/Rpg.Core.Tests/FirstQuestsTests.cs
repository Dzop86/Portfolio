using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The first quests (sprint 89, D65): the start of the main quest from Ysaline, one or two side
/// quests in every zone, one quest for every dungeon; each one can be done, and none blocks another.
/// </summary>
public class FirstQuestsTests
{
    private static readonly string[] Main = ["whispering-stone", "the-registry", "restless-beasts", "rumble-under-the-wallow", "four-trails"];

    // Every quest a quest follows, near or far.
    private static HashSet<string> Before(Quest q) => [.. (q.After ?? []).SelectMany(a => Before(Real.Quests[a]).Append(a))];

    private static readonly string[] Graves = ["old-grave-1", "old-grave-2"];

    private static int ZoneLevel(string zone) => Real.Towns.Values.Where(t => t.Zone == zone).Min(t => t.Level);

    [Fact]
    public void TheMainQuest_StartsAtTheStone_EachStepAfterTheOneBefore_UpToTheFourTrails()
    {
        Quest[] main = [.. Main.Select(id => Real.Quests[id])];
        Assert.All(main, q => Assert.Equal("clairval/ysaline", q.Giver));
        // Since sprint 107 the stone comes after the tutorial's rats and Rose's stall; the woods' first step after Garance's edge of the woods.
        Assert.Equal(["cellar-rats", "roses-stall"], main[0].After!);
        Assert.Equal(["edge-of-the-woods"], main[2].After!);
        for (int i = 1; i < main.Length; i++)
        {
            Assert.Contains(main[i - 1].Id, Before(main[i]));
            Assert.True(main[i].Level >= main[i - 1].Level);
        }
        // The stone's fifth sign leads under the Boar King's wallow, then to the four trails of the four Primordials.
        Assert.Contains(main[3].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "boar-wallow");
        // The main quest is done alone at its level (sprint 95, D71): never a dungeon's last room, made for
        // several; the key to go in is given with the quest.
        Assert.All(main.SelectMany(q => q.Objectives!).Where(o => o.Goal == QuestGoal.Dungeon), o => Assert.InRange(o.Room, 1, 2));
        Assert.Equal([new ItemCount("boar-wallow-key", 1)], main[3].Grants!);
        Assert.Equal(["clairval/rose", "clairval/garance", "clairval/aubin"], main[4].Objectives!.Select(o => o.Target));
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
                    case QuestGoal.Bring when q.Zone is null:
                        // In the village (the tutorial, sprint 107): gathered, made, bought from Rose or left in a quest's own fight.
                        Assert.True(Real.Nodes.Values.Any(n => n.Item == o.Target) || Real.Jobs.Values.Any(j => j.RecipeFor(o.Target) is not null)
                            || Shop.SellPrice(Real.Items[o.Target], Real) is not null || Real.Scenarios.Values.Any(s => s.Fighters.Any(f => (f.Loot ?? []).Any(l => l.Item == o.Target))), $"{q.Id} {o.Target}");
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

    [Fact]
    public void TheTutorial_NineQuestsInClairval_FromAubinsWelcome_ToTheEdgeOfTheWoods_OneSystemEach()
    {
        // Sprint 107 (docs/textes-v5.md §4): each quest after the ones before it, levels 1 to 4, all given in the village.
        (string Id, string Giver, string[] After, QuestGoal[] Goals)[] tutorial =
        [
            ("welcome", "clairval/aubin", [], [QuestGoal.Talk]),
            ("first-lesson", "clairval/garance", ["welcome"], [QuestGoal.Win]),
            ("well-equipped", "clairval/garance", ["first-lesson"], [QuestGoal.Wear]),
            ("cellar-rats", "clairval/firmin", ["well-equipped"], [QuestGoal.Win, QuestGoal.Bring]),
            ("palisade-stakes", "clairval/hortense", ["well-equipped"], [QuestGoal.Bring, QuestGoal.Bring]),
            ("roses-stall", "clairval/rose", ["palisade-stakes"], [QuestGoal.Bring, QuestGoal.Bring]),
            ("whispering-stone", "clairval/ysaline", ["cellar-rats", "roses-stall"], [QuestGoal.Interact, QuestGoal.Talk, QuestGoal.Talk]),
            ("the-registry", "clairval/ysaline", ["whispering-stone"], [QuestGoal.Interact, QuestGoal.Interact, QuestGoal.Bring]),
            ("edge-of-the-woods", "clairval/garance", ["the-registry"], [QuestGoal.Defeat, QuestGoal.Bring]),
        ];
        foreach ((string id, string giver, string[] after, QuestGoal[] goals) in tutorial)
        {
            Quest q = Real.Quests[id];
            Assert.Equal(giver, q.Giver);
            Assert.Equal(after, q.After ?? []);
            Assert.Equal(goals, q.Objectives!.Select(o => o.Goal));
            Assert.InRange(q.Level, 1, 4);
            Assert.True(Shop.InVillage(Real.FindNpc(giver)!.Value.Town.Id));
        }
        // A new hero is offered the welcome alone; Garance takes it back herself, and says her line to the newcomer.
        Assert.Equal(["welcome"], Real.Quests.Values.Where(q => Quests.WhyNot(q, 1, [], []) is null).Select(q => q.Id));
        Assert.Equal("clairval/garance", Real.Quests["welcome"].Taker);
        Assert.NotNull(Real.Quests["welcome"].Objectives![0].Line);
        // Aubin and Odalric say what they know of the stone (§10.1).
        Assert.All(Real.Quests["whispering-stone"].Objectives!.Where(o => o.Goal == QuestGoal.Talk), o => Assert.NotNull(o.Line));
        // The loft's hatch starts the rats' fight, whose rats leave the tails for Firmin alone.
        Assert.Equal("loft-rats", Real.FindProp("loft-hatch")!.Value.Prop.Fight);
        Assert.All(Real.Scenarios["loft-rats"].Fighters.Where(f => f.Team == 1), f => Assert.Contains(new LootDrop("rat-tail", 100, 1, "cellar-rats"), f.Loot!));
        // The two old graves are at the chapel.
        Assert.All(Graves, g => Assert.Equal("clairval-2-2", Real.FindProp(g)!.Value.Town.Id));
    }
}
