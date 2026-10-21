using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// A fight with the party (sprint 120, D74): up to three allies on the free starts of team A, after the scenario's
/// fighters; a record of version 2 that replays them; the experience and the loot of each hero its own.
/// </summary>
public class PartyFightTests
{
    private static readonly MonsterGroup Group = Real.Groups.Values.First(g => Real.Towns[g.Map].Dungeon is null);

    private static Hero Of(string name, string cls, int level = 5) => Progression.Built(new Hero(name, "female-c", cls, Level: level), Real);

    private static Fight Played(Fight f)
    {
        for (int i = 0; i < 2000 && !f.IsOver; i++)
        {
            if (f.Apply(Ai.Decide(f)) != ActionError.None)
                f.Apply(new EndTurnAction());
        }
        return f;
    }

    [Fact]
    public void TheAllies_StandOnTheFreeStartsOfTeamA_AfterTheScenariosFighters()
    {
        Hero[] allies = [Of("Brume", "mage"), Of("Cendre", "guard"), Of("Dune", "mage")];
        var f = new Fight(Real, Group.Scenario, 7, Of("Aube", "guard"), null, null, allies, [null, 10, null]);
        int scenario = Real.FindScenario(Group.Scenario)!.Fighters.Count;
        Assert.Equal(scenario + 3, f.Fighters.Count);
        Assert.Equal(4, f.HeroFighters.Count);
        Assert.Equal(["Aube", "Brume", "Cendre", "Dune"], f.HeroFighters.Select(h => h.Spec.Name.In("fr")));
        Assert.Equal([scenario, scenario + 1, scenario + 2], f.HeroFighters.Skip(1).Select(h => h.Id));
        Assert.All(f.HeroFighters, h => Assert.Equal(0, h.Team));
        Assert.Equal(4, f.HeroFighters.Select(h => h.Cell).Distinct().Count());
        Assert.Equal(10, f.HeroFighters[2].Hp);
        // A summon comes after the allies: they stay the heroes.
        Fight played = Played(new Fight(Real, Group.Scenario, 3, Of("Aube", "mage", 20), null, null, [Of("Brume", "mage", 20)]));
        Assert.Equal(["Aube", "Brume"], played.HeroFighters.Select(h => h.Spec.Name.In("fr")));
        // Without allies, the fight alone is the same as before.
        var alone = new Fight(Real, Group.Scenario, 7, Of("Aube", "guard"));
        Assert.Equal(scenario, alone.Fighters.Count);
        Assert.Equal([alone.HeroFighter!], alone.HeroFighters);
        Assert.Throws<ArgumentException>(() => new Fight(Real, Group.Scenario, 7, Of("Aube", "guard"), allies: [.. allies, Of("Écume", "mage")]));
        Assert.Throws<ArgumentException>(() => new Fight(Real, Group.Scenario, 7, null, allies: allies));
    }

    [Fact]
    public void ARecordOfVersion2_ReplaysTheParty_AndAVersion1_TheHeroAlone()
    {
        Fight f = Played(new Fight(Real, Group.Scenario, 11, Of("Aube", "guard", 9), null, null, [Of("Brume", "mage", 9)], [30]));
        Assert.True(f.IsOver);
        FightRecord record = FightRecord.Of(f);
        Assert.Equal(2, record.Version);
        Fight again = FightRecord.FromJson(record.ToJson()).Replay(Real);
        Assert.Equal(f.WinningTeam, again.WinningTeam);
        Assert.Equal(f.HeroFighters.Select(h => h.Hp), again.HeroFighters.Select(h => h.Hp));
        // A fight alone, written as before, still replays; version 1 with allies does not.
        Fight alone = Played(new Fight(Real, Group.Scenario, 11, Of("Aube", "guard", 9)));
        FightRecord old = FightRecord.Of(alone) with { Version = 1 };
        Assert.Equal(alone.WinningTeam, old.Replay(Real).WinningTeam);
        Assert.Throws<InvalidFightRecordException>(() => (record with { Version = 1 }).Replay(Real));
        Assert.Throws<InvalidFightRecordException>(() => (record with { Allies = [Of("A", "guard"), Of("B", "guard"), Of("C", "guard"), Of("D", "guard")] }).Replay(Real));
    }

    [Fact]
    public void EachHero_EarnsItsOwnExperience_ByItsLevel_AndDrawsItsOwnLoot()
    {
        Fight won = Enumerable.Range(0, 40).Select(s => Played(new Fight(Real, Group.Scenario, (ulong)s, Of("Aube", "guard", 60), null, null, [Of("Brume", "mage", 1)])))
            .First(f => f.WinningTeam == 0);
        Hero high = won.Hero!, low = won.Allies[0];
        // A hero far above the monsters (more than LevelGap levels) earns nothing from them; the other one does, +10 % for its ally.
        Assert.Equal(0, Progression.FightXp(won, high));
        long alone = won.Fighters.Where(f => f.Team == 1 && !f.IsSummon).Sum(f => Progression.MonsterXp(f.Spec.Level));
        Assert.Equal(alone * (100 + Progression.GroupBonus) / 100, Progression.FightXp(won, low));
        Assert.Equal(Progression.FightXp(won), Progression.FightXp(won, high));
        Assert.Equal(Equipment.Loot(won, null, 1), Equipment.Loot(won, null, 1));
        Assert.Equal(Equipment.Loot(won), Equipment.Loot(won, null, 0));
    }
}
