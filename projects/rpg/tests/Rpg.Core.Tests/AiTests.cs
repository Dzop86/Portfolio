using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class AiTests
{
    [Fact]
    public void InReach_ItHits_TheWeakestEnemyFirst()
    {
        Fight f = Fight(["B.", "AB"], Spec(0, spells: ["strike"]), Spec(1, 0, hp: 40), Spec(1, 1, hp: 20));
        Assert.Equal(new CastAction("strike", new Cell(1, 1)), Ai.Decide(f));
    }

    [Fact]
    public void ItPrefersTheStrongestHit_CappedAtTheTargetsHitPoints()
    {
        // Strike (10) beats Spear (5) on a healthy target; on a target with 4 hit points both kill: the first listed wins.
        Fight f = Fight(["AB"], Spec(0, spells: ["spear", "strike"]), Spec(1, hp: 40));
        Assert.Equal(new CastAction("strike", new Cell(1, 0)), Ai.Decide(f));
        Fight g = Fight(["AB"], Spec(0, spells: ["spear", "strike"]), Spec(1, hp: 4));
        Assert.Equal(new CastAction("spear", new Cell(1, 0)), Ai.Decide(g));
    }

    [Fact]
    public void OutOfReach_ItWalksToTheNearestCellFromWhichItCanHit()
    {
        Fight f = Fight(["A...B"], Spec(0, mp: 3, spells: ["strike"]), Spec(1));
        Assert.Equal(new MoveAction(new Cell(3, 0)), Ai.Decide(f));
        Fight g = Fight(["A......B"], Spec(0, mp: 3, spells: ["bow"]), Spec(1));
        // The bow reaches 5 steps: one step is enough, and the AI walks no further than needed.
        Assert.Equal(new MoveAction(new Cell(2, 0)), Ai.Decide(g));
    }

    [Fact]
    public void FarAway_ItWalksAroundObstaclesTowardsTheEnemy()
    {
        // Going right looks shorter but is a dead end; the real way goes down and round the wall.
        Fight f = Fight(["A...#..B", ".####...", "........"], Spec(0, mp: 2, spells: ["strike"]), Spec(1));
        Assert.Equal(new MoveAction(new Cell(0, 2)), Ai.Decide(f));
    }

    [Fact]
    public void WithNothingToDo_ItEndsItsTurn()
    {
        Assert.IsType<EndTurnAction>(Ai.Decide(Fight(["A#B"], Spec(0, mp: 0, spells: ["strike"]), Spec(1))));
        Assert.IsType<EndTurnAction>(Ai.Decide(Fight(["A~B"], Spec(0, mp: 4, spells: ["strike"]), Spec(1))));
    }

    /// <summary>Hundreds of AI-only fights on the real scenarios: every one ends, every action is valid, every spell hits an enemy.</summary>
    [Fact]
    public void AiFights_AlwaysEnd_WithValidActionsOnly()
    {
        foreach (string scenario in Real.Scenarios.Keys)
        {
            for (ulong seed = 0; seed < 300; seed++)
            {
                var f = new Fight(Real, scenario, seed);
                Ai.PlayOut(f);
                Assert.True(f.IsOver);
                for (int i = 0; i < f.Events.Count; i++)
                {
                    if (f.Events[i] is SpellCast cast)
                    {
                        Damaged hit = Assert.IsType<Damaged>(f.Events[i + 1]);
                        Assert.NotEqual(f.Fighters[cast.Fighter].Team, f.Fighters[hit.Fighter].Team);
                    }
                }
            }
        }
    }
}
