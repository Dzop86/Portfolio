using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class FightTests
{
    [Fact]
    public void TurnOrder_HighestInitiativeFirst_TiesKeepTheScenarioOrder()
    {
        Fight f = Fight(["AAB.B"], Spec(0, 0, initiative: 10), Spec(0, 1, initiative: 50), Spec(1, 0, initiative: 50), Spec(1, 1, initiative: 90));
        Assert.Equal([3, 1, 2, 0], f.TurnOrder.Select(x => x.Id));
        Assert.Equal(3, f.Current.Id);
        Assert.Equal(new TurnStarted(3, 1), f.Events[0]);
    }

    [Fact]
    public void Moving_CostsOneMovementPointPerStep()
    {
        Fight f = Fight(["A.....B"], Spec(0, mp: 3), Spec(1));
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(2, 0))));
        Assert.Equal((new Cell(2, 0), 1), (f.Current.Cell, f.Current.Mp));
        Assert.Equal(ActionError.NotEnoughMp, f.Apply(new MoveAction(new Cell(4, 0))));
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(3, 0))));
        Assert.Equal(0, ((Moved)f.Events[1]).Fighter);
        Assert.Equal([new Cell(1, 0), new Cell(2, 0)], ((Moved)f.Events[1]).Path);
    }

    [Fact]
    public void Moving_RefusesWallsHolesFightersAndTheBoardEdge()
    {
        Fight f = Fight([".#.", "A~B", "..."], Spec(0, mp: 6), Spec(1));
        Assert.Equal(ActionError.NotFloor, f.Apply(new MoveAction(new Cell(1, 0))));
        Assert.Equal(ActionError.NotFloor, f.Apply(new MoveAction(new Cell(1, 1))));
        Assert.Equal(ActionError.Occupied, f.Apply(new MoveAction(new Cell(2, 1))));
        Assert.Equal(ActionError.Occupied, f.Apply(new MoveAction(new Cell(0, 1))));
        Assert.Equal(ActionError.OffBoard, f.Apply(new MoveAction(new Cell(3, 1))));
        Assert.Empty(f.History);
    }

    [Fact]
    public void Fighters_BlockTheWay()
    {
        // The only way to (3, 0) goes through the enemy.
        Fight f = Fight(["A.B.."], Spec(0, mp: 6), Spec(1));
        Assert.Equal(ActionError.Unreachable, f.Apply(new MoveAction(new Cell(3, 0))));
    }

    [Fact]
    public void Casting_ChecksRangeLineAndSight()
    {
        Fight f = Fight(["A..#B", ".....", "....."], Spec(0, mp: 0, ap: 20, spells: ["strike", "bow", "spear"]), Spec(1));
        Assert.Equal(ActionError.OutOfRange, f.Apply(new CastAction("strike", new Cell(2, 0))));
        Assert.Equal(ActionError.OutOfRange, f.Apply(new CastAction("bow", new Cell(1, 0))));
        Assert.Equal(ActionError.OutOfRange, f.Apply(new CastAction("bow", new Cell(4, 2))));
        Assert.Equal(ActionError.NotInLine, f.Apply(new CastAction("spear", new Cell(1, 1))));
        Assert.Equal(ActionError.NoLineOfSight, f.Apply(new CastAction("bow", new Cell(4, 0))));
        Assert.Equal(ActionError.NotFloor, f.Apply(new CastAction("bow", new Cell(3, 0))));
        Assert.Equal(ActionError.OffBoard, f.Apply(new CastAction("bow", new Cell(0, 4))));
        Assert.Equal(ActionError.UnknownSpell, f.Apply(new CastAction("lob", new Cell(1, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("bow", new Cell(3, 1))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("spear", new Cell(0, 2))));
    }

    [Fact]
    public void HolesLetSpellsThrough_FightersDoNot_UnlessTheSpellIgnoresSight()
    {
        Fight f = Fight(["A~.B", "....", "A...", "...."], Spec(0, 0, mp: 0, ap: 20, spells: ["bow", "lob"]), Spec(1), Spec(0, 1));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("bow", new Cell(2, 0))));
        // The ally on (0, 2) stands between the caster and (0, 3).
        Assert.Equal(ActionError.NoLineOfSight, f.Apply(new CastAction("bow", new Cell(0, 3))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("lob", new Cell(0, 3))));
    }

    [Fact]
    public void FromAnotherCell_TheCastersOwnBodyNoLongerBlocksTheView()
    {
        // Standing on (1, 0) between (0, 0) and the enemy: from (0, 0) the line would cross (1, 0), left empty.
        Fight f = Fight([".A.B"], Spec(0, spells: ["bow"]), Spec(1));
        Fighter me = f.Current;
        Assert.Same(me, f.At(new Cell(1, 0)));
        Assert.Equal(ActionError.None, f.CheckCast(me, Bow, new Cell(0, 0), new Cell(3, 0)));
    }

    [Fact]
    public void ActionPoints_AndCastsPerTurn_AreLimited()
    {
        Fight f = Fight(["AB"], Spec(0, ap: 7, mp: 0, spells: ["strike", "lob"]), Spec(1, hp: 500));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("strike", new Cell(1, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("strike", new Cell(1, 0))));
        Assert.Equal(ActionError.NotEnoughAp, f.Apply(new CastAction("lob", new Cell(1, 0))));
        Assert.Equal(1, f.Current.Ap);
        Fight g = Fight(["AB"], Spec(0, ap: 30, mp: 0, spells: ["strike"]), Spec(1, hp: 500));
        g.Apply(new CastAction("strike", new Cell(1, 0)));
        g.Apply(new CastAction("strike", new Cell(1, 0)));
        Assert.Equal(ActionError.CastLimit, g.Apply(new CastAction("strike", new Cell(1, 0))));
    }

    [Fact]
    public void Damage_IsRolledWithinTheSpellsRange()
    {
        var seen = new HashSet<int>();
        for (ulong seed = 0; seed < 200; seed++)
        {
            var f = new Fight(Data(["A.B"], Spec(0, mp: 0, spells: ["bow"]), Spec(1, hp: 500)), "s", seed);
            f.Apply(new CastAction("bow", new Cell(2, 0)));
            Damaged d = f.Events.OfType<Damaged>().Single();
            Assert.InRange(d.Amount, 4, 8);
            Assert.Equal(500 - d.Amount, d.HpLeft);
            seen.Add(d.Amount);
        }
        Assert.Equal(5, seen.Count);
    }

    [Fact]
    public void ARefusedAction_ChangesNothing()
    {
        Fight f = Fight(["A.#B"], Spec(0, spells: ["bow"]), Spec(1));
        string before = Fingerprint(f);
        (int ap, int mp) = (f.Current.Ap, f.Current.Mp);
        Assert.NotEqual(ActionError.None, f.Apply(new CastAction("bow", new Cell(3, 0))));
        Assert.NotEqual(ActionError.None, f.Apply(new MoveAction(new Cell(2, 0))));
        Assert.Equal(before, Fingerprint(f));
        Assert.Equal((ap, mp), (f.Current.Ap, f.Current.Mp));
        Assert.Empty(f.History);
    }

    [Fact]
    public void EndingTheTurn_RefillsThePointsOfTheNextFighter_AndCountsRounds()
    {
        Fight f = Fight(["A...B"], Spec(0, ap: 6, mp: 3, spells: ["lob"]), Spec(1, ap: 5, mp: 2));
        f.Apply(new MoveAction(new Cell(1, 0)));
        f.Apply(new CastAction("lob", new Cell(4, 0)));
        f.Apply(new EndTurnAction());
        Assert.Equal((1, 1, 5, 2), (f.Current.Id, f.Round, f.Current.Ap, f.Current.Mp));
        f.Apply(new EndTurnAction());
        Assert.Equal((0, 2, 6, 3), (f.Current.Id, f.Round, f.Current.Ap, f.Current.Mp));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("lob", new Cell(4, 0))));
    }

    [Fact]
    public void Killing_FreesTheCell_SkipsTheDead_AndEndsTheFightWhenATeamIsGone()
    {
        Fight f = Fight(["ABB"], Spec(0, ap: 6, mp: 0, initiative: 9, spells: ["strike"]), Spec(1, 0, hp: 10, initiative: 5), Spec(1, 1, hp: 10, initiative: 7));
        f.Apply(new CastAction("strike", new Cell(1, 0)));
        Assert.Contains(new Died(1), f.Events);
        Assert.Null(f.At(new Cell(1, 0)));
        Assert.False(f.IsOver);
        f.Apply(new EndTurnAction());
        Assert.Equal(2, f.Current.Id);
        f.Apply(new EndTurnAction());
        Assert.Equal((0, 2), (f.Current.Id, f.Round));
        Fight g = Fight(["AB"], Spec(0, spells: ["strike"]), Spec(1, hp: 10));
        g.Apply(new CastAction("strike", new Cell(1, 0)));
        Assert.True(g.IsOver);
        Assert.Equal(0, g.WinningTeam);
        Assert.Equal(new FightEnded(0), g.Events[^1]);
        Assert.Equal(ActionError.FightOver, g.Apply(new EndTurnAction()));
    }

    [Fact]
    public void DyingDuringOnesOwnTurn_HandsOverAtOnce()
    {
        Fight f = Fight(["AAB"], Spec(0, 0, initiative: 9, spells: ["sacrifice"]), Spec(0, 1, initiative: 8), Spec(1, initiative: 7));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("sacrifice", new Cell(0, 0))));
        Assert.False(f.Fighters[0].IsAlive);
        Assert.False(f.IsOver);
        Assert.Equal(1, f.Current.Id);
    }

    [Fact]
    public void AFightWhereNobodyCanHit_IsADrawAtTheRoundLimit()
    {
        Fight f = Fight(["A#B"], Spec(0, mp: 0), Spec(1, mp: 0));
        for (int i = 0; i < 2 * Rpg.Core.Fight.RoundLimit - 1; i++)
            Assert.Equal(ActionError.None, f.Apply(new EndTurnAction()));
        Assert.False(f.IsOver);
        f.Apply(new EndTurnAction());
        Assert.True(f.IsOver);
        Assert.Null(f.WinningTeam);
        Assert.Equal(Rpg.Core.Fight.RoundLimit, f.Round);
        Assert.Equal(new FightEnded(null), f.Events[^1]);
    }
}
