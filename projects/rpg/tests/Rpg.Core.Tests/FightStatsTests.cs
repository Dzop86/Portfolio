using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The statistics of a fight and the Éclats it gives (sprint 87).</summary>
public class FightStatsTests
{
    private static readonly Spell Blow = new("blow", Text, 2, 1, 6, false, false, 99, 99, 9);

    /// <summary>A hero who finishes off each monster (5 hit points) with one blow of 6.</summary>
    private static Fight Won(int heroLevel, params int[] monsterLevels)
    {
        FighterSpec[] monsters = [.. monsterLevels.Select((l, i) => Spec(1, i, hp: 5) with { Level = l })];
        string[] rows = [string.Concat(Enumerable.Repeat("B", monsters.Length)) + ".A"];
        var data = new GameData([.. Spells, Blow], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", [Spec(0, ap: 99, initiative: 999, spells: ["blow"]), .. monsters])],
            [new HeroClass("hunter", Text, Text, 50, 99, 0, 999, ["blow"])]);
        var f = new Fight(data, "s", 1, new Hero("Élise", "female-c", "hunter", Level: heroLevel));
        foreach (Fighter m in f.Fighters.Where(x => x.Team == 1))
            f.Apply(new CastAction("blow", m.Cell));
        Assert.Equal(0, f.WinningTeam);
        return f;
    }

    [Fact]
    public void WhatFollowsASpell_IsItsCastersDoing_DamageTakenByEachTarget_TheKillsCounted()
    {
        Fight f = Won(1, 1, 1, 1);
        IReadOnlyList<FighterStats> stats = FightStats.Of(f);
        Assert.Equal(f.Fighters.Select(x => x.Id), stats.Select(s => s.Fighter));
        int dealt = f.Events.OfType<Damaged>().Sum(d => d.Amount);
        Assert.Equal(new FighterStats(0, dealt, 0, 0, 0, 3), stats[0]);
        Assert.All(stats.Skip(1), s => Assert.Equal((0, 0, 0), (s.DamageDealt, s.Kills, s.Healing)));
        Assert.Equal(dealt, stats.Skip(1).Sum(s => s.DamageTaken));
    }

    [Fact]
    public void AWholeFight_AddsUp_AndReplaysTheSame()
    {
        GameData real = GameData.Embedded;
        var fight = new Fight(real, "training", 4, new Hero("Ronce", "female-c", "mage", Level: 3));
        Ai.PlayOut(fight);
        IReadOnlyList<FighterStats> stats = FightStats.Of(fight);
        // Every point of damage is taken by someone; dealt only by an enemy of its target, never more.
        Assert.Equal(fight.Events.OfType<Damaged>().Sum(d => d.Amount), stats.Sum(s => s.DamageTaken));
        Assert.InRange(stats.Sum(s => s.DamageDealt), 0, stats.Sum(s => s.DamageTaken));
        Assert.InRange(stats.Sum(s => s.Kills), 0, fight.Events.OfType<Died>().Count());
        Assert.DoesNotContain(stats, s => fight.Fighters[s.Fighter].IsSummon);
        Assert.Equal(stats, FightStats.Of(FightRecord.Of(fight).Replay(real)));
    }

    [Fact]
    public void AWonFight_GivesEachMonstersÉclats_WhateverItsLevel_ALostOneNothing()
    {
        Assert.Equal(Progression.MonsterShards(1) * 2, Progression.FightShards(Won(1, 1, 1)));
        // Unlike the experience, a monster far below still leaves its Éclats.
        Assert.Equal(Progression.MonsterShards(31) + Progression.MonsterShards(5), Progression.FightShards(Won(50, 31, 5)));
        Assert.True(Progression.MonsterShards(10) > Progression.MonsterShards(1));
        var lost = new Fight(GameData.Embedded, "training", 1, new Hero("Ronce", "female-c", "mage"));
        while (!lost.IsOver)
            lost.Apply(lost.Current.Team == 0 ? new EndTurnAction() : Ai.Decide(lost));
        Assert.Equal(1, lost.WinningTeam);
        Assert.Equal(0, Progression.FightShards(lost));
    }
}
