using Rpg.Core;

namespace Rpg.Client.Tests;

public class ControllerTests
{
    private static readonly LocalizedText T = new("t", "t");
    private static readonly Spell Strike = new("strike", T, 3, 1, 1, true, false, 10, 10, 2);
    private static readonly Spell Bow = new("bow", T, 4, 2, 4, true, false, 5, 5, 1);

    private static FighterSpec Spec(int team, int start = 0, int initiative = 100, int hp = 50) =>
        new(T, "male-a", team, hp, 6, 3, initiative, start, ["strike", "bow"]);

    /// <summary>The player's fighter (team 0) plays first.</summary>
    private static FightController Make(string[] rows, params FighterSpec[] fighters)
    {
        var data = new GameData([Strike, Bow], [new MapSpec("m", T, rows)], [new Scenario("s", T, "m", fighters)]);
        return new FightController(new Fight(data, "s", 1));
    }

    [Fact]
    public void WithoutASpell_HoverShowsTheReachableCellsAndThePath()
    {
        FightController c = Make(["A.....B"], Spec(0, initiative: 200), Spec(1));
        Preview p = c.Hover(new Cell(2, 0));
        Assert.Equal(new HashSet<Cell> { new(1, 0), new(2, 0), new(3, 0) }, p.Reachable);
        Assert.Equal([new Cell(1, 0), new Cell(2, 0)], p.Path);
        Assert.Empty(c.Hover(new Cell(5, 0)).Path);
        Assert.Empty(c.Hover(null).Path);
    }

    [Fact]
    public void AClick_Walks_OrSaysWhyNot()
    {
        FightController c = Make(["A.....B"], Spec(0, initiative: 200), Spec(1));
        Assert.Null(c.Click(new Cell(5, 0)));
        Assert.Equal(ActionError.NotEnoughMp, c.LastError);
        Assert.Equal(new MoveAction(new Cell(2, 0)), c.Click(new Cell(2, 0)));
        Assert.Null(c.LastError);
        Assert.Equal(new Cell(2, 0), c.Fight.Current.Cell);
    }

    [Fact]
    public void WithASpell_HoverShowsItsRangeAndTheCellsItCanHit()
    {
        // The bow reaches 2 to 4 steps; the obstacle hides (4, 0).
        FightController c = Make(["A..#.B"], Spec(0, initiative: 200), Spec(1));
        c.SelectSpell(1);
        Assert.Equal(Bow, c.SelectedSpell);
        Preview p = c.Hover(new Cell(2, 0));
        Assert.Equal(new HashSet<Cell> { new(2, 0), new(4, 0) }, p.InRange);
        Assert.Equal(new HashSet<Cell> { new(2, 0) }, p.Targetable);
        Assert.Equal(new Cell(2, 0), p.Target);
        Assert.Null(c.Hover(new Cell(4, 0)).Target);
        Assert.Empty(p.Reachable);
    }

    [Fact]
    public void ASpellIsChosenForOneCast_AndChoosingItAgainCancelsIt()
    {
        FightController c = Make(["AB"], Spec(0, initiative: 200), Spec(1));
        c.SelectSpell(0);
        c.SelectSpell(0);
        Assert.Null(c.SelectedSpell);
        c.SelectSpell(0);
        Assert.Equal(new CastAction("strike", new Cell(1, 0)), c.Click(new Cell(1, 0)));
        Assert.Null(c.SelectedSpell);
        Assert.Equal(40, c.Fight.Fighters[1].Hp);
        c.SelectSpell(1);
        Assert.Null(c.SelectedSpell);
        Assert.Equal(ActionError.NotEnoughAp, c.LastError);
        c.SelectSpell(0);
        c.CancelSpell();
        Assert.Null(c.SelectedSpell);
    }

    [Fact]
    public void AMissedCast_KeepsTheSpell_AndSaysWhy()
    {
        FightController c = Make(["A..B"], Spec(0, initiative: 200), Spec(1));
        c.SelectSpell(0);
        Assert.Null(c.Click(new Cell(3, 0)));
        Assert.Equal(ActionError.OutOfRange, c.LastError);
        Assert.Equal(Strike, c.SelectedSpell);
    }

    [Fact]
    public void DuringTheAisTurn_ThePlayersControlsDoNothing()
    {
        FightController c = Make(["A...B"], Spec(0), Spec(1, initiative: 200));
        Assert.False(c.IsPlayerTurn);
        Assert.Same(Preview.None, c.Hover(new Cell(1, 0)));
        Assert.Null(c.Click(new Cell(1, 0)));
        Assert.Null(c.EndTurn());
        c.SelectSpell(0);
        Assert.Null(c.SelectedSpell);
        while (!c.IsPlayerTurn)
            Assert.NotNull(c.PlayAiStep());
        Assert.Null(c.PlayAiStep());
        Assert.NotNull(c.EndTurn());
    }

    /// <summary>Whole fights played through hover, spell choice and click give the same fight as the AI alone.</summary>
    [Fact]
    public void SelfPlay_GoesThroughTheControls_AndMatchesTheAiAlone()
    {
        foreach (string scenario in GameData.Embedded.Scenarios.Keys)
        {
            for (ulong seed = 0; seed < 100; seed++)
            {
                var controller = new FightController(new Fight(GameData.Embedded, scenario, seed));
                int actions = 0;
                SelfPlay.Run(controller, _ => actions++);
                var alone = new Fight(GameData.Embedded, scenario, seed);
                Ai.PlayOut(alone);
                Assert.Equal(alone.History, controller.Fight.History);
                Assert.Equal(actions, controller.Fight.History.Count);
                Assert.StartsWith($"SELFTEST OK seed {seed}: ", SelfPlay.Report(controller.Fight), StringComparison.Ordinal);
            }
        }
    }
}
