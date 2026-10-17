using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The quests of the Woods and the Mouldy Woods (sprint 112, D74, docs/suite-apres-d69.md §5 ter.4, texts in
/// docs/textes-v5.md §5 and §6): nine a zone, rewarded by the designer's rule, their clues on the zones' maps, their
/// quest loot falling only while wanted.
/// </summary>
public class ZoneQuestsTests
{
    public static readonly TheoryData<string, string> Zones = new() { { "woods", "woods/mathurin" }, { "mouldy", "mouldy/barnabe" } };

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
            bool main = q.Giver == "clairval/ysaline" || q.Objectives!.Any(o => o.Goal == QuestGoal.Talk && o.Target == "clairval/ysaline");
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
            ("mycelium-knot-1", "mouldy"), ("mycelium-knot-2", "mouldy"), ("mycelium-knot-3", "mouldy") })
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
    public void APlayerTakingTwoOffersAtATime_ReachesTheEndOfTheMouldyWoodsByLevelTwenty()
    {
        // Level by level, as the game offers them (two at most from one inhabitant, §5 ter.4 rule 3, QuestBook.Offers),
        // every quest taken then handed back done: no chain is stuck.
        var done = new HashSet<string>();
        for (int level = 1; level <= 20; level++)
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
    }
}
