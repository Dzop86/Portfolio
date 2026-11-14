using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The end of chapter 1 (sprint 114, D74, design-familles.md §4.9, docs/textes-v5.md §8): the Disjoint, fought at the
/// Burrows' gate, wears itself out and bursts as it falls; then "Four trails" closes the chapter at level 30. Made harder at the third milestone
/// (sprint 219, D100): a hero alone kept it away while it wore itself out; its Snatching Tongue drags that hero back to its claws.
/// </summary>
public class DisjointTests
{
    private static readonly Hero Guard = Progression.Built(new Hero("Essai", "female-d", "warrior", Level: 30), Real);

    [Fact]
    public void TheDisjoint_LosesFifteenHitPointsAtTheStartOfEachOfItsTurns()
    {
        var fight = new Fight(Real, "the-disjoint", 1, Guard);
        Fighter disjoint = fight.Fighters.Single(f => f.Team == 1);
        // 480 since sprint 219 (D100, docs/jalon-3.md §4): 450 before, beaten 98.75 times in 100 by a hero who kept it away.
        Assert.Equal((480, 30), (disjoint.Spec.Hp, disjoint.Spec.Level));
        while (fight.Current != disjoint)
            fight.Apply(Ai.Decide(fight));
        // Its own passive, at the start of its turn, on itself.
        int before = fight.Events.Count;
        Assert.Contains(fight.Events, e => e is PassiveTriggered { Passive: "unstable" } p && p.Fighter == disjoint.Id);
        Assert.Contains(fight.Events, e => e is Damaged { Amount: 15 } d && d.Fighter == disjoint.Id);
        Assert.True(before > 0);
    }

    [Fact]
    public void TheDisjoint_SplitsAsItIsAboutToFall_TwoCellsAround()
    {
        // A death that ends the fight sets off no passive (sprint 90): it splits below a tenth of its hit points.
        PassiveSpec rupture = Real.Passives["rupture"];
        Assert.Equal((Trigger.LowHp, 10, AreaShape.Circle, 2, 15, 20), (rupture.Trigger, rupture.Percent, rupture.Area!.Shape, rupture.Area.Radius, rupture.DamageMin, rupture.DamageMax));
        var fight = new Fight(Real, "the-disjoint", 2, Progression.Built(new Hero("Essai", "female-d", "archer", Level: 30), Real, Progression.Kit("craft", "fields", 30, Real)));
        Ai.PlayOut(fight);
        Assert.Equal(0, fight.WinningTeam);
        Assert.Contains(fight.Events, e => e is PassiveTriggered { Passive: "rupture" });
    }

    [Fact]
    public void TheBurrowsGate_StartsTheFight_WhileTheQuestWantsIt_ThenTheFourTrailsCloseTheChapter()
    {
        (Town map, Prop gate) = Real.FindProp("burrows-gate")!.Value;
        Assert.Equal(("fields-4-3", "the-disjoint"), (map.Id, gate.Fight));
        Assert.Equal(Real.Dungeons["mole-burrows"].Map, map.Id);
        Quest disjoint = Real.Quests["the-disjoint"], trails = Real.Quests["four-trails"];
        Assert.Equal([new QuestObjective(QuestGoal.Win, "the-disjoint")], disjoint.Objectives!);
        Assert.Equal(["under-the-hedges"], disjoint.After!);
        Assert.Equal(30, disjoint.Level);
        // The chapter's end at level 30: the three roads told by Rose, Garance and Aubin, each with their line.
        Assert.Equal(["the-disjoint"], trails.After!);
        Assert.Equal((30, 1800L), (trails.Level, trails.Xp));
        Assert.All(trails.Objectives!, o => Assert.NotNull(o.Line));
        // The main thread of the chapter: forty-nine quests in all, the last one "Four trails"; since sprint 129, the second
        // chapter's first step after it; since sprint 136, the Forest of the Ancestors' nine; since sprint 186, the quest of the paths.
        Assert.Equal(60, Real.Quests.Count);
        Assert.Equal(["where-roads-part"], Real.Quests.Values.Where(q => (q.After ?? []).Contains("four-trails")).Select(q => q.Id));
    }

    [Fact]
    public void ItsTongue_DragsBackToItsClaws_TheHeroWhoKeepsItAway()
    {
        var fight = new Fight(Real, "the-disjoint", 1, new Hero("Ondine", "female-c", "archer", Level: 30));
        Fighter boss = fight.Fighters.Single(f => f.Team == 1);
        while (fight.Current != boss)
            fight.Apply(new EndTurnAction());
        Fighter hero = fight.HeroFighter!;
        // The hero 4 cells away in a line, nothing between them.
        Cell Along(Cell d, int n) => new(boss.Cell.X + (d.X * n), boss.Cell.Y + (d.Y * n));
        Cell step = Cell.Steps.First(d => Enumerable.Range(1, 4).All(n => fight.Board.IsFloor(Along(d, n)) && (fight.IsFree(Along(d, n)) || Along(d, n) == hero.Cell)));
        Cell away = Along(step, 4);
        hero.Cell = away;
        boss.Ap = 8;
        int hp = hero.Hp;
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("snatching-tongue", away)));
        Assert.True(hero.Hp < hp);
        Assert.Equal(1, hero.Cell.DistanceTo(boss.Cell));
    }

    [Fact]
    public void ItsTongue_IsItsOwn_AndReachesFartherThanItsOtherBlows()
    {
        Spell tongue = Real.Spells["snatching-tongue"];
        Assert.Equal((2, 5, true), (tongue.MinRange, tongue.MaxRange, tongue.InLine));
        Assert.Contains(tongue.AllEffects, e => e is PullEffect { Cells: 3 });
        FighterSpec disjoint = Real.Scenarios["the-disjoint"].Fighters.Single(f => f.Team == 1);
        Assert.Contains("snatching-tongue", disjoint.Spells);
        Assert.All(disjoint.Spells.Where(s => s != "snatching-tongue"), s => Assert.True(Real.Spells[s].MaxRange <= 2, s));
        // No other fighter of the game has it.
        Assert.DoesNotContain(Real.Monsters.Values, m => m.Spells.Contains("snatching-tongue"));
    }
}
