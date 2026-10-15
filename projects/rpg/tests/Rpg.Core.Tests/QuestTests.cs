using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The quests given by inhabitants (sprint 88): the data checked, taking one, how it moves on, handing it back.</summary>
public class QuestTests
{
    private static readonly Quest Edge = Real.Quests["edge-of-the-woods"];

    private static GameData WithQuests(params Quest[] quests) => new(
        Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
        quests, Real.Items.Values, Real.Sets.Values, Real.Families.Values, Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values);

    [Fact]
    public void AGivenQuest_HasAKnownGiver_ItsWords_AndObjectivesOfKnownThings()
    {
        Assert.True(Edge.IsGiven);
        Assert.Equal(("clairval", "aubin"), Real.FindNpc(Edge.Giver!) is (Town t, Npc n) ? (t.Id, n.Id) : default);
        Assert.Null(Real.FindNpc("clairval/nobody"));
        Assert.Null(Real.FindNpc("nowhere"));
        // The first lesson stays a fight's quest.
        Assert.False(Real.Quests["first-lesson"].IsGiven);
        Assert.NotNull(WithQuests(Edge).Quests["edge-of-the-woods"]);
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Giver = "clairval/nobody" }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Scenario = "training" }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Text = null }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Defeat, "dragon", 3)] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Talk, "clairval/rose", 2)] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Bring, "no-such-item")] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { After = ["no-such-quest"] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Zone = "moon" }));
    }

    [Fact]
    public void AQuest_IsTakenOnce_FromItsLevel_AfterTheQuestsItFollows()
    {
        Quest later = Edge with { Id = "later", Level = 5, After = ["edge-of-the-woods"] };
        Assert.Null(Quests.WhyNot(Edge, 1, [], []));
        Assert.NotNull(Quests.WhyNot(Edge, 1, ["edge-of-the-woods"], []));
        Assert.NotNull(Quests.WhyNot(Edge, 1, [], [Quests.Start(Edge)]));
        Assert.NotNull(Quests.WhyNot(later, 5, [], []));
        Assert.NotNull(Quests.WhyNot(later, 4, ["edge-of-the-woods"], []));
        Assert.Null(Quests.WhyNot(later, 5, ["edge-of-the-woods"], []));
        Assert.NotNull(Quests.WhyNot(Real.Quests["first-lesson"], 1, [], []));
        QuestProgress[] full = [.. Enumerable.Range(0, Quests.MaxActive).Select(i => new QuestProgress("q" + i, [0]))];
        Assert.NotNull(Quests.WhyNot(Edge, 1, [], full));
    }

    [Fact]
    public void AWonFight_CountsItsFamily_ATalkItsInhabitant_AndTheItemsAreThoseOwned()
    {
        QuestProgress p = Quests.Start(Edge);
        Assert.Equal([0, 0, 0], p.Counts);
        // A group of boars of the woods, won: as many boars as it had, never more than asked.
        MonsterGroup boars = Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null && g.Monsters.Count == 2);
        Fight won = Won(boars.Scenario);
        Assert.Equal(new Dictionary<string, int> { ["boar"] = 2 }, Quests.Defeated(Real, won));
        p = Quests.AfterFight(Edge, p, Quests.Defeated(Real, won), null);
        p = Quests.AfterFight(Edge, p, Quests.Defeated(Real, won), null);
        Assert.Equal([3, 0, 0], p.Counts);
        // Wolves or a lost fight count for nothing here.
        Assert.Equal(p, Quests.AfterFight(Edge, p, new Dictionary<string, int> { ["wolf"] = 4 }, null));
        Assert.Empty(Quests.Defeated(Real, new Fight(Real, "training", 1)));
        p = Quests.AfterTalk(Edge, p, "clairval/garance");
        Assert.Equal([3, 0, 0], p.Counts);
        p = Quests.AfterTalk(Edge, p, "clairval/rose");
        Assert.Equal([3, 0, 1], p.Counts);
        Assert.False(Quests.IsComplete(Edge, p, [new ItemCount("boar-bristle", 1)]));
        Assert.True(Quests.IsComplete(Edge, p, [new ItemCount("boar-bristle", 3)]));
        Assert.Equal([new ItemCount("boar-bristle", 2)], Quests.Taken(Edge));
        // A dungeon finished counts for a "dungeon" objective.
        Quest den = Edge with { Objectives = [new QuestObjective(QuestGoal.Dungeon, "wolf-den")] };
        Assert.Equal([1], Quests.AfterFight(den, Quests.Start(den), new Dictionary<string, int>(), "wolf-den").Counts);
        Assert.Equal([0], Quests.AfterFight(den, Quests.Start(den), new Dictionary<string, int>(), "boar-wallow").Counts);
        // As the server keeps them.
        List<QuestProgress> log = [p, new QuestProgress("other", [1, 0])];
        Assert.Equal(log, Quests.Read(Quests.Write(log)));
        Assert.Empty(Quests.Read(""));
    }

    /// <summary>A fight of the scenario won by a hero far stronger, played by the AI.</summary>
    private static Fight Won(string scenario)
    {
        var fight = new Fight(Real, scenario, 1, Progression.Built(new Hero("Ronce", "female-c", "guard", Level: 60), Real));
        Ai.PlayOut(fight);
        Assert.Equal(0, fight.WinningTeam);
        return fight;
    }
}
