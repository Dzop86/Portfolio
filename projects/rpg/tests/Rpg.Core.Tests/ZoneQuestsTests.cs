using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The quests of the zones (sprints 112 and 113, D74, docs/suite-apres-d69.md §5 ter.4, texts in docs/textes-v5.md
/// §5 to §8): nine a zone, rewarded by the designer's rule, their clues on the zones' maps, their quest loot falling
/// only while wanted. The Fields' last two steps, the Disjoint and the four trails, come in sprint 114.
/// </summary>
public class ZoneQuestsTests
{
    /// <summary>The main steps a zone's giver relays.</summary>
    private static readonly string[] Relayed = ["four-signs", "the-cracked-seal", "the-last-verse", "under-the-hedges"];

    public static readonly TheoryData<string, string> Zones = new() { { "woods", "woods/mathurin" }, { "mouldy", "mouldy/barnabe" }, { "lake", "lake/liane" } };

    [Theory]
    [MemberData(nameof(Zones))]
    public void AZone_HasNineQuests_FourMainSteps_ThreeSideQuestsOfItsGiver_AndItsTwoDungeonsToFinish(string zone, string giver)
    {
        Quest[] here = [.. Real.Quests.Values.Where(q => q.Zone == zone && q.Id != "edge-of-the-woods")];
        Assert.Equal(9, here.Length);
        Assert.Equal(2, here.Count(q => q.Id.EndsWith("-cleared", StringComparison.Ordinal)));
        Assert.Equal(4, here.Count(q => q.Giver == "clairval/ysaline") + 1);
        Assert.Equal(6, here.Count(q => q.Giver == giver));
        foreach (Quest q in here)
        {
            // The designer's rule: a main step 60 × its level, a side quest 40 ×, a dungeon finished 100 ×; Éclats 10 × (15 × for a dungeon).
            bool deep = q.Id.EndsWith("-cleared", StringComparison.Ordinal);
            bool main = q.Giver == "clairval/ysaline" || Relayed.Contains(q.Id);
            Assert.Equal((deep ? 100 : main ? 60 : 40) * q.Level, q.Xp);
            Assert.Equal((deep ? 15 : 10) * q.Level, q.Shards);
            // No set's piece as a reward: consumables and resources only.
            Assert.All(q.Items ?? [], i => Assert.NotEqual(ItemKind.Equipment, Real.Items[i.Item].Kind));
        }
    }

    [Fact]
    public void TheClues_StandOnTheZonesMaps_TheSealInTheCryptsSecondHall()
    {
        foreach ((string prop, string zone) in new[] { ("claw-marks-1", "woods"), ("claw-marks-2", "woods"), ("root-circle", "woods"),
            ("mycelium-knot-1", "mouldy"), ("mycelium-knot-2", "mouldy"), ("mycelium-knot-3", "mouldy"),
            ("singing-reed-1", "lake"), ("singing-reed-2", "lake"), ("singing-reed-3", "lake"),
            ("mole-tunnel-1", "fields"), ("mole-tunnel-2", "fields"), ("mole-tunnel-3", "fields") })
            Assert.Equal(zone, Real.FindProp(prop)!.Value.Town.Zone);
        Assert.Equal("zombie-crypt-2", Real.FindProp("crypt-seal")!.Value.Town.Id);
        Assert.Contains(Real.Quests["down-into-the-crypt"].Objectives!, o => o.Goal == QuestGoal.Dungeon && o.Target == "zombie-crypt" && o.Room == 2);
    }

    [Fact]
    public void TheMarkedFangs_AndTheRegistryPages_FallOnlyWhileTheirQuestWantsThem()
    {
        foreach ((string family, string item, string quest) in new[] { ("wolf", "marked-fang", "restless-beasts"), ("zombie", "registry-page", "the-undug-crypt") })
        {
            Assert.Equal(ItemKind.Quest, Real.Items[item].Kind);
            Assert.All(Real.Families[family].Monsters, m => Assert.Contains(new LootDrop(item, 35, 1, quest), m.Loot!));
            Assert.Contains(Real.Quests[quest].Objectives!, o => o.Goal == QuestGoal.Bring && o.Target == item);
        }
        // Sprint 113: the verse sung best by the toad king and the debris the mole king guards fall from them surely, from
        // the others of the family now and then, so that the main thread is done alone (no chief in a hall it needs);
        // the painted shards from the hermit crabs.
        foreach ((string chief, string item, string quest) in new[] { ("toad-king", "last-verse", "the-last-verse"), ("mole-king", "humming-debris", "under-the-hedges") })
        {
            Assert.Contains(new LootDrop(item, 100, 1, quest), Real.Monsters[chief].Loot!);
            Assert.All(Real.Families[Real.Families.Values.Single(f => f.Monsters.Any(m => m.Id == chief)).Id].Monsters, m => Assert.Contains(m.Loot!, l => l.Item == item && l.Quest == quest));
        }
        Assert.Contains(new LootDrop("painted-shard", 50, 1, "the-washed-name"), Real.Monsters["hermit-crab"].Loot!);
    }

    [Fact]
    public void TheFields_HaveTheirFirstSevenQuests_TheBurningHivesAndTheHedges_AndTheLakesJettiesTheirWitnesses()
    {
        Quest[] fields = [.. Real.Quests.Values.Where(q => q.Zone == "fields")];
        Assert.Equal(7, fields.Length);
        Assert.Equal(["burning-hives"], Real.Quests["under-the-hedges"].After!);
        Assert.Equal("fields/eglantine", Real.Quests["under-the-hedges"].Giver);
        // Joachim and Fleur, on the jetties by Liane, each with their own words and their line for the lullaby.
        foreach (string who in new[] { "lake/joachim", "lake/fleur" })
        {
            Assert.Equal("lake-3-4", Real.FindNpc(who)!.Value.Town.Id);
            Assert.Contains(Real.Quests["the-unlearned-lullaby"].Objectives!, o => o.Target == who && o.Line is not null);
        }
        // The harvest's loaves are baked: Rose does not sell oat bread.
        Assert.Null(Shop.SellPrice(Real.Items["oat-bread"], Real));
    }

    [Fact]
    public void TheOldLoners_AreCountedByTheirKind()
    {
        MonsterGroup loners = Real.Groups.Values.First(g => g.Monsters.Contains("old-loner") && g.Fight is null);
        var won = new Fight(Real, loners.Scenario, 1, Progression.Built(new Hero("Essai", "female-d", "guard", Level: 30), Real));
        Ai.PlayOut(won);
        Assert.Equal(0, won.WinningTeam);
        Quest quest = Real.Quests["the-old-loner"];
        QuestProgress p = Quests.AfterFight(quest, Quests.Start(quest), Quests.Defeated(Real, won), null);
        Assert.Equal(loners.Monsters.Count(m => m == "old-loner"), p.Counts[0]);
    }

    [Fact]
    public void APlayerTakingTwoOffersAtATime_ReachesTheHedgesByLevelThirty()
    {
        // Level by level, as the game offers them (two at most from one inhabitant, §5 ter.4 rule 3, QuestBook.Offers),
        // every quest taken then handed back done: no chain is stuck.
        var done = new HashSet<string>();
        for (int level = 1; level <= 30; level++)
        {
            bool more = true;
            while (more)
            {
                more = false;
                foreach (IGrouping<string?, Quest> byGiver in Real.Quests.Values.Where(q => q.IsGiven && Quests.WhyNot(q, level, done, []) is null).GroupBy(q => q.Giver))
                {
                    Quest[] offered = [.. byGiver.OrderBy(q => q.Level).Take(2)];
                    foreach (Quest q in offered)
                        more |= done.Add(q.Id);
                }
            }
        }
        Assert.Contains("the-cracked-seal", done);
        Assert.Contains("the-name-returned", done);
        Assert.Contains("under-the-hedges", done);
    }
}
