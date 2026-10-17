using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The first quests (sprint 89, D65): the start of the main quest from Ysaline, side quests in every zone, one quest
/// for every dungeon; each one can be done, and none blocks another. Since sprint 112 (D74), the Woods' and the Mouldy
/// Woods' chains (docs/textes-v5.md §5 and §6): the zones' givers relay the main quest and give the side ones.
/// </summary>
public class FirstQuestsTests
{
    private static readonly string[] Main =
        ["whispering-stone", "the-registry", "restless-beasts", "the-root-circle", "rumble-under-the-wallow", "four-signs",
         "what-grows-between", "the-undug-crypt", "down-into-the-crypt", "the-cracked-seal",
         "the-unlearned-lullaby", "the-last-verse", "the-washed-name", "the-name-returned", "burning-hives", "under-the-hedges", "four-trails"];

    /// <summary>The main steps a zone's giver relays (sprint 112): the woodcutters' head, the hermit of the pools.</summary>
    private static readonly Dictionary<string, string> Relayed = new()
    {
        ["four-signs"] = "woods/mathurin",
        ["the-cracked-seal"] = "mouldy/barnabe",
        ["the-last-verse"] = "lake/liane",
        ["under-the-hedges"] = "fields/eglantine",
    };

    // Every quest a quest follows, near or far.
    private static HashSet<string> Before(Quest q) => [.. (q.After ?? []).SelectMany(a => Before(Real.Quests[a]).Append(a))];

    private static readonly string[] Graves = ["old-grave-1", "old-grave-2"];

    private static int ZoneLevel(string zone) => Real.Towns.Values.Where(t => t.Zone == zone).Min(t => t.Level);

    [Fact]
    public void TheMainQuest_StartsAtTheStone_EachStepAfterTheOneBefore_UpToTheFourTrails()
    {
        Quest[] main = [.. Main.Select(id => Real.Quests[id])];
        Assert.All(main, q => Assert.Equal(Relayed.GetValueOrDefault(q.Id, "clairval/ysaline"), q.Giver));
        // Since sprint 107 the stone comes after the tutorial's rats and Rose's stall; the woods' first step after Garance's edge of the woods.
        Assert.Equal(["cellar-rats", "roses-stall"], main[0].After!);
        Assert.Equal(["edge-of-the-woods"], main[2].After!);
        for (int i = 1; i < main.Length; i++)
        {
            Assert.Contains(main[i - 1].Id, Before(main[i]));
            Assert.True(main[i].Level >= main[i - 1].Level);
        }
        // The stone's fifth sign leads under the Boar King's wallow, then to the four trails of the four Primordials.
        Assert.Contains(main[4].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "boar-wallow");
        Assert.Contains(main[8].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "zombie-crypt");
        Assert.Contains(main[11].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "frog-grotto");
        // The main quest is done alone at its level (sprint 95, D71): never a dungeon's last room, made for
        // several; the key to go in is given with the quest.
        Assert.All(main.SelectMany(q => q.Objectives!).Where(o => o.Goal == QuestGoal.Dungeon), o => Assert.InRange(o.Room, 1, 2));
        Assert.Equal([new ItemCount("boar-wallow-key", 1)], main[4].Grants!);
        Assert.Equal([new ItemCount("zombie-crypt-key", 1)], main[8].Grants!);
        Assert.Equal([new ItemCount("frog-grotto-key", 1)], main[11].Grants!);
        Assert.Equal(["clairval/rose", "clairval/garance", "clairval/aubin"], main[^1].Objectives!.Select(o => o.Target));
        // Every target spoken to has its own line about the stone (§10.1).
        Assert.All(main.SelectMany(q => q.Objectives!).Where(o => o.Goal == QuestGoal.Talk && main.IndexOf(main.First(q => q.Objectives!.Contains(o))) < main.Length - 1), o => Assert.NotNull(o.Line));
        Assert.Equal((20, 12), (Real.FindNpc("clairval/ysaline")!.Value.Npc.At.X, Real.FindNpc("clairval/ysaline")!.Value.Npc.At.Y));
    }

    [Fact]
    public void EveryZone_HasSideQuests_AndEveryDungeonOne_NeverGivenBehindADungeonsDoor()
    {
        // The tutorial's last quest (the edge of the woods, Garance's) is the village's, not the zone's.
        Quest[] side = [.. Real.Quests.Values.Where(q => q.IsGiven && !Main.Contains(q.Id) && !q.Id.EndsWith("-cleared", StringComparison.Ordinal) && q.Id != "edge-of-the-woods")];
        foreach (string zone in Real.Families.Values.Select(f => f.Zone).Distinct())
            Assert.InRange(side.Count(q => q.Zone == zone), 1, 3);
        // The Woods' and the Mouldy Woods' side quests come from their givers, in two small chains (sprint 112).
        Assert.All(side.Where(q => q.Zone == "woods"), q => Assert.Equal("woods/mathurin", q.Giver));
        Assert.All(side.Where(q => q.Zone == "mouldy"), q => Assert.Equal("mouldy/barnabe", q.Giver));
        Assert.All(side.Where(q => q.Zone == "lake"), q => Assert.Equal("lake/liane", q.Giver));
        Assert.All(side.Where(q => q.Zone == "fields"), q => Assert.Equal("fields/eglantine", q.Giver));
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Quest q = Real.Quests[d.Id + "-cleared"];
            Assert.Equal([new QuestObjective(QuestGoal.Dungeon, d.Id)], q.Objectives!);
            Assert.Equal(Real.Families[d.Family].Zone, q.Zone);
        }
        // Handed back in the village or at a zone's giver: never to someone behind a dungeon's door, which needs a key.
        Assert.All(Real.Quests.Values.Where(q => q.IsGiven), q => Assert.False(Real.FindNpc(q.Taker!)!.Value.Town.IsHall, q.Id));
    }

    [Fact]
    public void EveryQuest_CanBeDone_InItsZone_AroundItsLevel_AndNoneWaitsForALaterOne()
    {
        foreach (Quest q in Real.Quests.Values.Where(q => q.IsGiven))
        {
            // A zone's quests between its level and the next zone's (sprint 112: the Woods 5 to 10 for maps of level 3).
            if (q.Zone is string zone)
                Assert.InRange(q.Level, 1, ZoneLevel(zone) + 7);
            foreach (QuestObjective o in q.Objectives!)
            {
                switch (o.Goal)
                {
                    case QuestGoal.Defeat:
                        // A family, or since sprint 112 a kind of monster (the old loners).
                        Assert.Equal(q.Zone, Real.Families.TryGetValue(o.Target, out MonsterFamily? family) ? family.Zone
                            : Real.Families.Values.Single(f => f.Monsters.Any(m => m.Id == o.Target)).Zone);
                        break;
                    case QuestGoal.Bring when q.Zone is null:
                        // In the village (the tutorial, sprint 107): gathered, made, bought from Rose or left in a quest's own fight.
                        Assert.True(Real.Nodes.Values.Any(n => n.Item == o.Target) || Real.Jobs.Values.Any(j => j.RecipeFor(o.Target) is not null)
                            || Shop.SellPrice(Real.Items[o.Target], Real) is not null || Real.Scenarios.Values.Any(s => s.Fighters.Any(f => (f.Loot ?? []).Any(l => l.Item == o.Target))), $"{q.Id} {o.Target}");
                        break;
                    case QuestGoal.Bring:
                        // Left by a monster of the zone, or since sprint 112 gathered on its maps at the quest's level or under, or
                        // since sprint 113 made by a trade (the harvest's loaves).
                        Assert.True(Real.Families.Values.Where(f => f.Zone == q.Zone).SelectMany(f => f.Monsters).SelectMany(m => m.Loot ?? []).Any(l => l.Item == o.Target)
                            || Real.Nodes.Values.Any(n => n.Item == o.Target && Real.Towns[n.Map].Zone == q.Zone && n.Level <= q.Level)
                            || Real.Jobs.Values.Any(j => j.RecipeFor(o.Target) is { } r && r.Level <= q.Level), $"{q.Id} {o.Target}");
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
