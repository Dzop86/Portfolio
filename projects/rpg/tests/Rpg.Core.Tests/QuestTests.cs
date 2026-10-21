using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The quests given by inhabitants (sprint 88): the data checked, taking one, how it moves on, handing it back.</summary>
public class QuestTests
{
    // Garance's edge of the woods, taken alone: without the tutorial's quests before it (sprint 107).
    private static readonly Quest Edge = Real.Quests["edge-of-the-woods"] with { After = null, Level = 1 };

    private static GameData WithQuests(params Quest[] quests) => new(
        Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
        quests, Real.Items.Values, Real.Sets.Values, Real.Families.Values, Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values);

    [Fact]
    public void AGivenQuest_HasAKnownGiver_ItsWords_AndObjectivesOfKnownThings()
    {
        Assert.True(Edge.IsGiven);
        Assert.Equal(("clairval-0-1", "garance"), Real.FindNpc(Edge.Giver!) is (Town t, Npc n) ? (t.Id, n.Id) : default);
        Assert.Null(Real.FindNpc("clairval/nobody"));
        Assert.Null(Real.FindNpc("nowhere"));
        // Since sprint 107 the first lesson is Garance's too, won by its fight; no fight's quest is left.
        Assert.Equal(["first-lesson"], Real.Quests.Values.Where(q => q.Objectives?.Any(o => o.Goal == QuestGoal.Win && o.Target == "training") == true).Select(q => q.Id));
        Assert.All(Real.Quests.Values, q => Assert.True(q.IsGiven));
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
        // Sprint 116: a dungeon's quest, and only it, asks just to finish a dungeon.
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Kind = QuestKind.Dungeon }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Dungeon, "boar-wallow")] }));
        Assert.Equal(QuestKind.Dungeon, WithQuests(Edge with { Kind = QuestKind.Dungeon, Objectives = [new QuestObjective(QuestGoal.Dungeon, "boar-wallow")] }).Quests[Edge.Id].Kind);
        // Sprint 107: a taker known, a line only for a talk.
        Assert.Equal("clairval/rose", WithQuests(Edge with { HandIn = "clairval/rose" }).Quests[Edge.Id].Taker);
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { HandIn = "clairval/nobody" }));
        Assert.Throws<InvalidDataException>(() => WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Defeat, "boar", 3, Line: Edge.Name)] }));
        Assert.NotNull(WithQuests(Edge with { Objectives = [new QuestObjective(QuestGoal.Talk, "clairval/rose", Line: Edge.Name)] }));
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
        // The edge of the woods as it was before sprint 107, Rose to see as well.
        Quest Edge = QuestTests.Edge with { Objectives = [.. QuestTests.Edge.Objectives!, new QuestObjective(QuestGoal.Talk, "clairval/rose")] };
        QuestProgress p = Quests.Start(Edge);
        Assert.Equal([0, 0, 0], p.Counts);
        // A group of boars of the woods, won: as many boars as it had, never more than asked.
        MonsterGroup boars = Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null && g.Monsters.Count == 2);
        Fight won = Won(boars.Scenario);
        // The family, and since sprint 112 each kind of monster of the group.
        Assert.Equal(2, Quests.Defeated(Real, won)["boar"]);
        Assert.All(boars.Monsters.Distinct(), m => Assert.Equal(boars.Monsters.Count(x => x == m), Quests.Defeated(Real, won)[m]));
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
        // A dungeon finished (its last room won) counts for a "dungeon" objective; an earlier room, for one that names it (sprint 95).
        Quest den = Edge with { Objectives = [new QuestObjective(QuestGoal.Dungeon, "wolf-den")] };
        Assert.Equal([1], Quests.AfterFight(den, Quests.Start(den), new Dictionary<string, int>(), ("wolf-den", 3)).Counts);
        Assert.Equal([0], Quests.AfterFight(den, Quests.Start(den), new Dictionary<string, int>(), ("wolf-den", 2)).Counts);
        Assert.Equal([0], Quests.AfterFight(den, Quests.Start(den), new Dictionary<string, int>(), ("boar-wallow", 3)).Counts);
        Quest hall = Edge with { Objectives = [new QuestObjective(QuestGoal.Dungeon, "wolf-den", Room: 2)] };
        Assert.Equal([0], Quests.AfterFight(hall, Quests.Start(hall), new Dictionary<string, int>(), ("wolf-den", 1)).Counts);
        Assert.Equal([1], Quests.AfterFight(hall, Quests.Start(hall), new Dictionary<string, int>(), ("wolf-den", 2)).Counts);
        Assert.Equal([1], Quests.AfterFight(hall, Quests.Start(hall), new Dictionary<string, int>(), ("wolf-den", 3)).Counts);
        // As the server keeps them.
        List<QuestProgress> log = [p, new QuestProgress("other", [1, 0])];
        Assert.Equal(log, Quests.Read(Quests.Write(log)));
        Assert.Empty(Quests.Read(""));
    }

    [Fact]
    public void AClueExamined_AFightWon_AnItemWorn_MoveAQuestOn()
    {
        // Sprint 105 (D75): the old stone of the square, the training fight, the copper ring.
        Quest q = Edge with
        {
            Objectives = [new QuestObjective(QuestGoal.Interact, "old-stone"), new QuestObjective(QuestGoal.Win, "training"), new QuestObjective(QuestGoal.Wear, "copper-ring")],
        };
        Assert.NotNull(WithQuests(q).Quests[q.Id]);
        Assert.Equal(("clairval", "stone"), Real.FindProp("old-stone") is (Town t, Prop p0) ? (t.Id, p0.Look) : default);
        Assert.Null(Real.FindProp("no-such-thing"));
        QuestProgress p = Quests.Start(q);
        p = Quests.AfterInteract(q, p, "a-stone-elsewhere");
        Assert.Equal([0, 0, 0], p.Counts);
        p = Quests.AfterInteract(q, p, "old-stone");
        // Another scenario, or none (a lost fight), counts for nothing.
        p = Quests.AfterFight(q, p, new Dictionary<string, int>(), null, "dungeon:boar-wallow:1");
        p = Quests.AfterFight(q, p, new Dictionary<string, int>(), null);
        Assert.Equal([1, 0, 0], p.Counts);
        p = Quests.AfterFight(q, p, new Dictionary<string, int>(), null, "training");
        Assert.Equal([1, 1, 0], p.Counts);
        // The ring counts while it is worn, not while it is owned.
        Assert.False(Quests.IsComplete(q, p, [new ItemCount("copper-ring", 1)]));
        Assert.False(Quests.IsComplete(q, p, [], ["acorn-ring"]));
        Assert.True(Quests.IsComplete(q, p, [new ItemCount("copper-ring", 1)], ["copper-ring"]));
        Assert.Empty(Quests.Taken(q));
        // Unknown things are refused, and these goals ask for one each.
        Assert.Throws<InvalidDataException>(() => WithQuests(q with { Objectives = [new QuestObjective(QuestGoal.Interact, "no-such-thing")] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(q with { Objectives = [new QuestObjective(QuestGoal.Win, "no-such-fight")] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(q with { Objectives = [new QuestObjective(QuestGoal.Wear, "boar-bristle")] }));
        Assert.Throws<InvalidDataException>(() => WithQuests(q with { Objectives = [new QuestObjective(QuestGoal.Interact, "old-stone", 2)] }));
    }

    [Fact]
    public void AQuestsLoot_FallsOnlyWhileTheQuestWantsIt_TheDrawsStayingTheSame()
    {
        // A quest item the boars leave every time (100 %), only for a quest that asks to bring two (the families kept
        // in their order: the groups of the world are drawn from it).
        Quest q = Edge with { Objectives = [new QuestObjective(QuestGoal.Bring, "garance-badge", 2)] };
        MonsterGroup boars = Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null && g.Monsters.Count == 2);
        MonsterFamily boar = Real.Families["boar"];
        MonsterFamily marked = boar with { Monsters = [.. boar.Monsters.Select(m => m with { Loot = [.. m.Loot ?? [], new LootDrop("garance-badge", 100, 1, q.Id)] })] };
        GameData data = new(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
            [.. Real.Quests.Values.Where(x => x.Id != q.Id), q], Real.Items.Values, Real.Sets.Values, [.. Real.Families.Values.Select(f => f.Id == "boar" ? marked : f)],
            Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values);
        var fight = new Fight(data, boars.Scenario, 1, Progression.Built(new Hero("Ronce", "female-c", "guard", Level: 60), data));
        Ai.PlayOut(fight);
        Assert.Equal(0, fight.WinningTeam);
        // No quest under way: the other loot only, drawn as it always was.
        IReadOnlyList<ItemCount> plain = Equipment.Loot(fight);
        Assert.DoesNotContain(plain, i => i.Item == "garance-badge");
        Assert.Equal(plain, Equipment.Loot(fight, Quests.LootWanted(data, [], [])));
        // Under way: the two boars leave one each; owned already, nothing more.
        IReadOnlySet<string> wanted = Quests.LootWanted(data, [Quests.Start(q)], []);
        Assert.Equal(["edge-of-the-woods/garance-badge"], wanted);
        Assert.Equal(2, Equipment.Loot(fight, wanted).Single(i => i.Item == "garance-badge").Count);
        Assert.Equal(plain, Equipment.Loot(fight, wanted).Where(i => i.Item != "garance-badge"));
        Assert.Empty(Quests.LootWanted(data, [Quests.Start(q)], [new ItemCount("garance-badge", 2)]));
        // A quest's loot must be a quest item the quest asks to bring.
        MonsterFamily wrong = boar with { Monsters = [.. boar.Monsters.Select(m => m with { Loot = [new LootDrop("boar-bristle", 50, 1, q.Id)] })] };
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
            [.. Real.Quests.Values.Where(x => x.Id != q.Id), q], Real.Items.Values, Real.Sets.Values, [.. Real.Families.Values.Select(f => f.Id == "boar" ? wrong : f)],
            Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values));
    }

    [Fact]
    public void AQuestUnderWay_IsFittedToItsObjectivesAsTheyAreNow()
    {
        // A quest given more objectives since (the whispering stone, sprint 105) keeps its counts, the new at zero.
        Assert.Equal(3, Real.Quests["whispering-stone"].Objectives!.Count);
        // The edge of the woods lost its talk to Rose (sprint 107): its third count goes.
        List<QuestProgress> fitted = Quests.Fit(Real, [new QuestProgress("whispering-stone", [1, 1]), new QuestProgress("gone", [1]), new QuestProgress(Edge.Id, [1, 2, 0])]);
        Assert.Equal([new QuestProgress("whispering-stone", [1, 1, 0]), new QuestProgress(Edge.Id, [1, 2])], fitted);
        Assert.Equal([new QuestProgress(Edge.Id, [1, 2])], Quests.Fit(Real, [new QuestProgress(Edge.Id, [1, 2, 0, 5])]));
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
