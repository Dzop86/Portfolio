using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Forest of the Ancestors' quests (sprint 136, D79, texts in docs/textes-v8.md): nine, as a zone of Clairval, given at
/// the miners' camp by Berthe after Anatole's first step; their clues on the forest's maps and in the dungeons' great halls,
/// the keepers' carved hearts falling only while wanted, and the slice's end handed back at Quatre-Chemins.
/// </summary>
public class ForestQuestsTests
{
    private static readonly string[] Main = ["the-tree-town", "the-unmapped-passage", "mossy-keepers", "roots-of-the-mother-tree"];

    [Fact]
    public void TheForest_HasNineQuests_FourMainSteps_ThreeSideQuests_AndItsTwoDungeonsToFinish_RewardedByTheRule()
    {
        Quest[] here = [.. Real.Quests.Values.Where(q => q.Zone == "ancestors")];
        Assert.Equal(9, here.Length);
        Assert.Equal(Main.Order(), here.Where(q => q.Kind == QuestKind.Main).Select(q => q.Id).Order());
        Assert.Equal(3, here.Count(q => q.Kind == QuestKind.Side));
        Assert.Equal(["bellowing-combe-cleared", "mother-tree-roots-cleared"], here.Where(q => q.Kind == QuestKind.Dungeon).Select(q => q.Id).Order());
        // Anatole sends the hero into the forest, Berthe gives the rest; the main thread follows "Where the Roads Part".
        Assert.Equal("four-ways/anatole", Real.Quests["the-tree-town"].Giver);
        Assert.All(here.Where(q => q.Id != "the-tree-town"), q => Assert.Equal("ancestors/berthe", q.Giver));
        Assert.Equal(["where-roads-part"], Real.Quests["the-tree-town"].After!);
        for (int i = 1; i < Main.Length; i++)
            Assert.Equal([Main[i - 1]], Real.Quests[Main[i]].After!);
        foreach (Quest q in here)
        {
            Assert.InRange(q.Level, 31, 45);
            bool deep = q.Kind == QuestKind.Dungeon;
            Assert.Equal((deep ? 100 : q.Kind == QuestKind.Main ? 60 : 40) * q.Level, q.Xp);
            Assert.Equal((deep ? 15 : 10) * q.Level, q.Shards);
            Assert.All(q.Items ?? [], i => Assert.NotEqual(ItemKind.Equipment, Real.Items[i.Item].Kind));
        }
        // The slice's end is handed back to Anatole, who writes to Ysaline: nothing opens.
        Assert.Equal("four-ways/anatole", Real.Quests["roots-of-the-mother-tree"].HandIn);
    }

    [Fact]
    public void EveryMainStep_IsDoneAlone_NoDeeperThanADungeonsSecondHall_TheKeysGivenOnce()
    {
        foreach (Quest q in Main.Select(id => Real.Quests[id]).Append(Real.Quests["the-bellow"]))
            Assert.All(q.Objectives!.Where(o => o.Goal == QuestGoal.Dungeon), o => Assert.Equal(2, o.Room));
        // S134's rule: the Roots' great hall at 41, the Combe's at 40.
        Assert.Equal(41, Real.Quests["roots-of-the-mother-tree"].Level);
        Assert.Equal(40, Real.Quests["the-bellow"].Level);
        Assert.Equal([new ItemCount("mother-tree-roots-key", 1)], Real.Quests["roots-of-the-mother-tree"].Grants!);
        Assert.Equal([new ItemCount("bellowing-combe-key", 1)], Real.Quests["the-bellow"].Grants!);
    }

    [Fact]
    public void BertheAndBerold_StandAtTheMinersCamp_ByTheWayIn_TheCluesOnTheForestsMaps_AndInTheGreatHalls()
    {
        foreach (string who in new[] { "ancestors/berthe", "ancestors/berold" })
        {
            Assert.Equal("ancestors-1-1", Real.FindNpc(who)!.Value.Town.Id);
            Assert.True(Real.Dialogues.ContainsKey(who.Split('/')[1]), who);
        }
        Town camp = Real.Towns["ancestors-1-1"];
        Assert.True(MonsterGroups.Depth(camp, [.. Real.Towns.Values.Where(t => t.Zone == "ancestors")]) <= MonsterGroups.Depth(Real.Towns["ancestors-4-4"], [.. Real.Towns.Values.Where(t => t.Zone == "ancestors")]));
        foreach (string pit in new[] { "trial-pit-1", "trial-pit-2", "trial-pit-3" })
            Assert.Equal("ancestors", Real.FindProp(pit)!.Value.Town.Zone);
        Assert.Equal("bellowing-combe-2", Real.FindProp("echo-stone")!.Value.Town.Id);
        Assert.Equal("mother-tree-roots-2", Real.FindProp("rooted-slab")!.Value.Town.Id);
    }

    [Fact]
    public void TheKeepersCarvedHearts_FallOnlyWhileTheirQuestWantsThem_SurelyFromTheMenhir()
    {
        Assert.Equal(ItemKind.Quest, Real.Items["keeper-heart"].Kind);
        foreach (MonsterSpec m in Real.Families["golem"].Monsters)
            Assert.Contains(new LootDrop("keeper-heart", m.Id == "menhir-keeper" ? 100 : 35, 1, "mossy-keepers"), m.Loot!);
        Assert.Contains(Real.Quests["mossy-keepers"].Objectives!, o => o.Goal == QuestGoal.Bring && o.Target == "keeper-heart" && o.Count == 3);
        Assert.Equal("ancestors/berold", Real.Quests["mossy-keepers"].HandIn);
    }

    [Fact]
    public void APlayerTakingTwoOffersAtATime_FinishesTheForestByLevelFortyFive()
    {
        // As ZoneQuestsTests does for the chapter 1: two offers at most from one inhabitant (QuestBook.Offers), every quest
        // taken then handed back done, level by level: Berthe's seven are all reached, none stuck behind another.
        var done = new HashSet<string>();
        for (int level = 1; level <= 45; level++)
        {
            bool more = true;
            while (more)
            {
                more = false;
                foreach (IGrouping<string?, Quest> byGiver in Real.Quests.Values.Where(q => q.IsGiven && Quests.WhyNot(q, level, done, []) is null).GroupBy(q => q.Giver))
                    foreach (Quest q in byGiver.OrderBy(q => q.Level).Take(2))
                        more |= done.Add(q.Id);
            }
        }
        Assert.All(Real.Quests.Values.Where(q => q.Zone == "ancestors"), q => Assert.Contains(q.Id, done));
    }
}
