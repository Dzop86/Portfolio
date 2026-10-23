using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The end of chapter 1 (sprint 114, D74, design-familles.md §4.9, docs/textes-v5.md §8): the Disjoint, fought at the
/// Burrows' gate, wears itself out and bursts as it falls; then "Four trails" closes the chapter at level 30.
/// </summary>
public class DisjointTests
{
    private static readonly Hero Guard = Progression.Built(new Hero("Essai", "female-d", "guard", Level: 30), Real);

    [Fact]
    public void TheDisjoint_LosesFifteenHitPointsAtTheStartOfEachOfItsTurns()
    {
        var fight = new Fight(Real, "the-disjoint", 1, Guard);
        Fighter disjoint = fight.Fighters.Single(f => f.Team == 1);
        Assert.Equal((450, 30), (disjoint.Spec.Hp, disjoint.Spec.Level));
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
        var fight = new Fight(Real, "the-disjoint", 2, Progression.Built(new Hero("Essai", "female-d", "sentinel", Level: 30), Real, Progression.Kit("craft", "fields", 30, Real)));
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
        // chapter's first step after it; since sprint 136, the Forest of the Ancestors' nine.
        Assert.Equal(59, Real.Quests.Count);
        Assert.Equal(["where-roads-part"], Real.Quests.Values.Where(q => (q.After ?? []).Contains("four-trails")).Select(q => q.Id));
    }
}
