using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The families of monsters and their groups on the maps (sprint 72).</summary>
public class MonsterTests
{
    [Fact]
    public void TheBoars_AreFiveBoars_OneOfEachRole_InTheWoods()
    {
        MonsterFamily boars = Real.Families["boar"];
        Assert.Equal(("woods", "woods-glade", "woods-0-2"), (boars.Zone, boars.Battle, boars.BossMap));
        Assert.Equal(Enum.GetValues<MonsterRole>(), boars.Monsters.Select(m => m.Role).Order());
        // Five of the same kind: their looks are the boar's, by role.
        Assert.All(boars.Monsters, m => Assert.Equal("boar-" + m.Role.ToString().ToLowerInvariant(), m.Look));
        Assert.Equal("boar-king", boars[MonsterRole.Boss].Id);
        Assert.True(boars[MonsterRole.Boss].Hp > boars.Monsters.Where(m => m.Role != MonsterRole.Boss).Max(m => m.Hp));
        Assert.True(boars[MonsterRole.Tank].Hp > boars[MonsterRole.Melee].Hp);
        Assert.Contains(boars[MonsterRole.Support].Spells, id => Real.Spells[id].Effects?.Any(e => e is HealEffect) == true);
        Assert.True(Real.Spells[boars[MonsterRole.Ranged].Spells[0]].MaxRange >= 4);
    }

    [Fact]
    public void TheWolves_AreFiveWolves_OneOfEachRole_InTheSameWoods_TheirLeaderElsewhere()
    {
        MonsterFamily wolves = Real.Families["wolf"];
        Assert.Equal(("woods", "woods-glade", "woods-2-0"), (wolves.Zone, wolves.Battle, wolves.BossMap));
        Assert.Equal(Enum.GetValues<MonsterRole>(), wolves.Monsters.Select(m => m.Role).Order());
        Assert.All(wolves.Monsters, m => Assert.Equal("wolf-" + m.Role.ToString().ToLowerInvariant(), m.Look));
        Assert.NotEqual(Real.Families["boar"].BossMap, wolves.BossMap);
        // The she-wolf guards the pack: a shield on her allies.
        Assert.Contains(wolves[MonsterRole.Support].Spells, id => Real.Spells[id].Effects?.Any(e => e is ShieldEffect { Affects: Affects.Allies }) == true);
        // Both families meet in the woods, each group of one of them.
        Assert.Contains(Real.Groups.Values, g => g.Family == "wolf");
        Assert.Contains(Real.Groups.Values, g => g.Family == "boar");
        Assert.Single(Real.Groups.Values, g => g.Monsters.Contains("pack-leader"));
    }

    private static GameData WithBoars(params MonsterSpec[] monsters) => new(
        Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
        Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [.. Real.Families.Values.Select(f => f.Id == "boar" ? f with { Monsters = monsters } : f)]);

    [Fact]
    public void AFamily_WithoutOneOfTheRoles_OrWithTwoOfOne_IsRefused()
    {
        MonsterSpec[] boars = [.. Real.Families["boar"].Monsters];
        Assert.NotNull(WithBoars(boars).Families["boar"]);
        Assert.Throws<InvalidDataException>(() => WithBoars(boars[..4]));
        Assert.Throws<InvalidDataException>(() => WithBoars([.. boars[..4], boars[0] with { Id = "twin" }]));
        Assert.Throws<InvalidDataException>(() => WithBoars([.. boars[..4], boars[4] with { Spells = ["no-such-spell"] }]));
        Assert.Throws<InvalidDataException>(() => WithBoars([.. boars[..4], boars[4] with { Loot = [new LootDrop("no-such-item", 50)] }]));
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [Real.Families["boar"] with { BossMap = "clairval" }]));
        // Two families with the same monster.
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [Real.Families["boar"], Real.Families["boar"] with { Id = "twin" }]));
    }

    [Fact]
    public void EveryMapOfTheWoods_HasTwoGroups_TheVillageNone_AlwaysTheSame()
    {
        Assert.Empty(Real.GroupsOn("clairval"));
        Assert.All(Real.Towns.Values.Where(t => t.Zone == "woods"), t => Assert.Equal(MonsterGroups.PerMap, Real.GroupsOn(t.Id).Count));
        Assert.Equal(50, Real.Groups.Count);
        // Drawn from the maps' names alone: the same data gives the same groups, on every platform (FNV-1a).
        static IEnumerable<string> Seen(GameData d) => d.Groups.Values.Select(g => $"{g.Id} {g.Home} {string.Join(",", g.Monsters)}");
        Assert.Equal(Seen(Real), Seen(WithBoars([.. Real.Families["boar"].Monsters])));
        Assert.Equal(0xaf63dc4c8601ec8cUL, MonsterGroups.Seed("a"));
    }

    [Fact]
    public void AGroup_LivesOnAFreeCell_AwayFromTheWays_AndFromTheOtherGroup()
    {
        foreach (MonsterGroup g in Real.Groups.Values)
        {
            Town map = Real.Towns[g.Map];
            Assert.True(map.CanStand(g.Home), g.Id);
            Assert.All((map.Links ?? []).Select(l => l.At).Concat(map.Exits.Select(e => e.At)).Append(map.Spawn),
                c => Assert.True(c.DistanceTo(g.Home) >= MonsterGroups.Distance, g.Id));
            Assert.All(Real.GroupsOn(g.Map).Where(o => o != g), o => Assert.True(o.Home.DistanceTo(g.Home) >= MonsterGroups.Distance, g.Id));
        }
    }

    [Fact]
    public void AGroup_IsOneFamily_SmallNearTheVillage_ASupportNeverAloneNorTwice_TheKingOnHisMapOnly()
    {
        foreach (MonsterGroup g in Real.Groups.Values)
        {
            MonsterSpec[] members = [.. g.Monsters.Select(id => Real.Monsters[id])];
            Assert.All(members, m => Assert.Contains(m, Real.Families[g.Family].Monsters));
            Assert.InRange(members.Length, 1, 3);
            Assert.True(members.Count(m => m.Role == MonsterRole.Support) <= (members.Length == 1 ? 0 : 1), g.Id);
            if (!g.Monsters.Contains("boar-king"))
                Assert.True(members.Length <= MonsterGroups.Largest(Real.Towns[g.Map]), g.Id);
        }
        MonsterGroup king = Assert.Single(Real.Groups.Values, g => g.Monsters.Contains("boar-king"));
        Assert.Equal(("woods-0-2", 2, "boar-king"), (king.Map, king.Monsters.Count, king.Monsters[0]));
        // Next to the village, one boar at a time.
        Assert.All(Real.GroupsOn("woods-4-2"), g => Assert.Single(g.Monsters));
        Assert.Contains(Real.Groups.Values, g => g.Monsters.Count == 3);
    }

    [Fact]
    public void FightingAGroup_IsAScenarioTheRulesBuild_PlayedReplayedAndRewarded()
    {
        MonsterGroup g = Real.Groups.Values.First(g => g.Monsters.Count == 2);
        Scenario s = Real.FindScenario(g.Scenario)!;
        Assert.Equal(("group:" + g.Id, "woods-glade"), (s.Id, s.Map));
        Assert.Equal([0, 1, 1], s.Fighters.Select(f => f.Team));
        Assert.Equal(g.Monsters.Select(id => Real.Monsters[id].Name), s.Fighters.Skip(1).Select(f => f.Name));
        Assert.Null(Real.FindScenario("group:nowhere.0"));
        Assert.Same(Real.Scenarios["training"], Real.FindScenario("training"));

        Hero hero = Progression.Built(new Hero("Essai", "female-d", "guard", Level: 5), Real);
        var fight = new Fight(Real, g.Scenario, 4, hero);
        Ai.PlayOut(fight);
        FightRecord record = FightRecord.Of(fight);
        Assert.Equal(fight.WinningTeam, record.Replay(Real).WinningTeam);
        if (fight.WinningTeam == 0)
            Assert.Equal(g.Monsters.Sum(id => Progression.MonsterXp(Real.Monsters[id].Level)), Progression.FightXp(fight));
    }
}
