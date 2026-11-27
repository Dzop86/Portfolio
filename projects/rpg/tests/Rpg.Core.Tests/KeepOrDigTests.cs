using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// "Keep or dig", the Underroot Mines' side quests (sprint 232, D103, texts in docs/textes-mines-2.md): two rival quests
/// at 46, Bérold's wall and Berthe's widening, each followed by two more from the same giver; the side taken shuts the
/// other for good, changes the two greetings and the rewards, never the main thread.
/// </summary>
public class KeepOrDigTests
{
    private static readonly string[] Berold = ["wall-the-passage", "the-boundary-stone", "stone-for-stone"];
    private static readonly string[] Berthe = ["widen-the-passage", "follow-the-worms", "the-first-cart"];

    [Fact]
    public void EachSide_HasThreeSideQuests_TheFirstTwoRivals_AfterTheRoots_RewardedByTheRule()
    {
        foreach ((string[] side, string giver) in new[] { (Berold, "ancestors/berold"), (Berthe, "ancestors/berthe") })
        {
            Assert.Equal([46, 48, 51], side.Select(id => Real.Quests[id].Level));
            Assert.Equal(["roots-of-the-mother-tree"], Real.Quests[side[0]].After!);
            for (int i = 1; i < side.Length; i++)
                Assert.Equal([side[i - 1]], Real.Quests[side[i]].After!);
            foreach (Quest q in side.Select(id => Real.Quests[id]))
            {
                Assert.Equal((giver, "underroot", QuestKind.Side), (q.Giver!, q.Zone!, q.Kind));
                Assert.Equal((40 * q.Level, 10 * q.Level), (q.Xp, q.Shards));
                // Nothing to bring above the quest's own level; the rewards are elixirs or bread, never equipment.
                Assert.All(q.Objectives!.Where(o => o.Goal == QuestGoal.Bring), o => Assert.True(Real.Items[o.Target].Level <= q.Level, o.Target));
                Assert.All(q.Items!, i => Assert.Equal(ItemKind.Consumable, Real.Items[i.Item].Kind));
            }
        }
        Assert.Equal(["widen-the-passage"], Real.Quests["wall-the-passage"].Rivals!);
        Assert.Equal(["wall-the-passage"], Real.Quests["widen-the-passage"].Rivals!);
        Assert.Equal(2, Real.Quests.Values.Count(q => q.Rivals is { Count: > 0 }));
        // The same deeds at 46, told otherwise.
        Assert.Equal(Real.Quests["wall-the-passage"].Objectives, Real.Quests["widen-the-passage"].Objectives);
        // Each side ends with a word from the other.
        Assert.Equal("ancestors/berthe", Real.Quests["stone-for-stone"].Objectives!.Single(o => o.Goal == QuestGoal.Talk).Target);
        Assert.Equal("ancestors/berold", Real.Quests["the-first-cart"].Objectives!.Single(o => o.Goal == QuestGoal.Talk).Target);
        // No quest follows the side quests: the main thread never depends on the side taken.
        Assert.DoesNotContain(Real.Quests.Values, q => (q.After ?? []).Any(a => Berold.Contains(a) || Berthe.Contains(a)) && !Berold.Contains(q.Id) && !Berthe.Contains(q.Id));
    }

    [Fact]
    public void TheSideTaken_ShutsTheOther_AtOnce_AndForGood()
    {
        string[] done = ["roots-of-the-mother-tree"];
        Assert.Null(Quests.WhyNot(Real.Quests["wall-the-passage"], 46, done, []));
        Assert.Null(Quests.WhyNot(Real.Quests["widen-the-passage"], 46, done, []));
        Assert.NotNull(Quests.WhyNot(Real.Quests["widen-the-passage"], 45, done, []));
        Assert.NotNull(Quests.WhyNot(Real.Quests["widen-the-passage"], 46, done, [Quests.Start(Real.Quests["wall-the-passage"])]));
        Assert.NotNull(Quests.WhyNot(Real.Quests["wall-the-passage"], 60, [.. done, .. Berthe], []));
    }

    [Fact]
    public void TheFourThingsToExamine_StandInTheMines_OnTheirFloor()
    {
        foreach (string prop in new[] { "miners-passage", "boundary-stone", "worm-tunnel-1", "worm-tunnel-2" })
        {
            (Town town, Prop p) = Real.FindProp(prop)!.Value;
            Assert.Equal("underroot", town.Zone);
            // Reached from the map's arrival, by one of its four sides at least.
            Assert.Contains(new[] { new Cell(p.At.X + 1, p.At.Y), new Cell(p.At.X - 1, p.At.Y), new Cell(p.At.X, p.At.Y + 1), new Cell(p.At.X, p.At.Y - 1) }, town.CanStand);
        }
    }

    [Fact]
    public void TheGreetings_FollowTheSideTaken_TheLastQuestDoneWinning()
    {
        Dialogue berold = Real.Dialogues["berold"], berthe = Real.Dialogues["berthe"];
        Assert.Equal(berold.Lines[berold.Start].Text, berold.Greet([]));
        Assert.Equal(berthe.Lines[berthe.Start].Text, berthe.Greet(["widen-the-passage"]));
        Assert.NotEqual(berold.Greet(["wall-the-passage"]), berold.Greet(["widen-the-passage"]));
        Assert.NotEqual(berold.Lines[berold.Start].Text, berold.Greet(["wall-the-passage"]));
        Assert.NotEqual(berthe.Lines[berthe.Start].Text, berthe.Greet(["wall-the-passage"]));
        Assert.NotEqual(berthe.Lines[berthe.Start].Text, berthe.Greet(["widen-the-passage", "follow-the-worms", "the-first-cart"]));
        // Every greeting of the game follows a quest of the game, and has its words (checked here: tests build data with fewer quests).
        Assert.All(Real.Dialogues.Values.SelectMany(d => d.Greetings ?? []), g => Assert.True(Real.Quests.ContainsKey(g.After) && g.Text is not null, g.After));
    }
}
