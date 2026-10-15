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
        // The pack leader once in the woods, once more in the den's last hall (sprint 86).
        Assert.Single(Real.Groups.Values, g => g.Fight is null && g.Monsters.Contains("pack-leader"));
    }

    [Theory]
    [InlineData("mushroom", "mouldy", "mouldy-hollow", "mouldy-0-0", "wolf")]
    [InlineData("zombie", "mouldy", "mouldy-hollow", "mouldy-2-4", "wolf")]
    [InlineData("frog", "lake", "lakeshore", "lake-0-0", "zombie")]
    [InlineData("crab", "lake", "lakeshore", "lake-4-1", "zombie")]
    [InlineData("bee", "fields", "meadow", "fields-0-4", "crab")]
    [InlineData("mole", "fields", "meadow", "fields-4-3", "crab")]
    public void EveryZone_HasItsTwoFamilies_FiveOfAKindEach_StrongerThanTheZoneBefore(string id, string zone, string battle, string chiefMap, string before)
    {
        MonsterFamily f = Real.Families[id];
        Assert.Equal((zone, battle, chiefMap), (f.Zone, f.Battle, f.BossMap));
        Assert.Equal(Enum.GetValues<MonsterRole>(), f.Monsters.Select(m => m.Role).Order());
        Assert.All(f.Monsters, m => Assert.Equal(id + "-" + m.Role.ToString().ToLowerInvariant(), m.Look));
        Assert.True(f.Monsters.Min(m => m.Level) > Real.Families[before].Monsters.Where(m => m.Role != MonsterRole.Boss).Max(m => m.Level));
        // The support heals or shields its allies.
        Assert.Contains(f[MonsterRole.Support].Spells, s => Real.Spells[s].Effects?.Any(e => e is HealEffect or ShieldEffect { Affects: Affects.Allies }) == true);
        Assert.Equal(10, Real.Board(battle).Starts[1].Count);
    }

    private static GameData WithBoars(params MonsterSpec[] monsters) => new(
        Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values,
        Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [.. Real.Families.Values.Select(f => f.Id == "boar" ? f with { Monsters = monsters } : f)], Real.Dungeons.Values, Real.Jobs.Values);

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
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [Real.Families["boar"] with { BossMap = "clairval" }], null, Real.Jobs.Values));
        // Two families with the same monster.
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, [Real.Families["boar"], Real.Families["boar"] with { Id = "twin" }], null, Real.Jobs.Values));
    }

    [Fact]
    public void EveryMapOfAZone_HasTwoToFourGroups_TheVillageNone_AlwaysTheSame()
    {
        Assert.Empty(Real.GroupsOn("clairval"));
        Assert.All(Real.Towns.Values.Where(t => t.Zone is not null), t => Assert.InRange(Real.GroupsOn(t.Id).Count, MonsterGroups.Fewest, MonsterGroups.Most));
        // Every count happens.
        Assert.Equal([2, 3, 4], Real.Towns.Values.Where(t => t.Zone is not null).Select(t => Real.GroupsOn(t.Id).Count).Distinct().Order());
        // Drawn from the maps' names alone: the same data gives the same groups, on every platform (FNV-1a).
        static IEnumerable<string> Seen(GameData d) => d.Groups.Values.Select(g => $"{g.Id} {g.Home} {string.Join(",", g.Monsters)}");
        Assert.Equal(Seen(Real), Seen(WithBoars([.. Real.Families["boar"].Monsters])));
        Assert.Equal(0xaf63dc4c8601ec8cUL, MonsterGroups.Seed("a"));
    }

    [Fact]
    public void TheGroupsOfAMap_NeverCutIt_EvenOnThePontoonsOfTheLake()
    {
        foreach (Town map in Real.Towns.Values.Where(t => t.Zone is not null))
            Assert.True(MonsterGroups.KeepsWaysOpen(map, map.Board(), [.. Real.GroupsOn(map.Id).Select(g => g.Home)]), map.Id);
        // A group on a pontoon would cut the way: there are such cells, and no group stands on one.
        Town lake = Real.Towns["lake-2-2"];
        Cell[] cutting = [.. lake.Board().Cells().Where(c => lake[c] == '=' && !MonsterGroups.KeepsWaysOpen(lake, lake.Board(), [c]))];
        Assert.NotEmpty(cutting);
        Assert.DoesNotContain(Real.GroupsOn("lake-2-2"), g => cutting.Contains(g.Home));
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
    public void AGroup_IsOneFamily_OfOneToTen_SmallNearTheWayIn_ASupportForThreeAtMost_TheChiefWithFiveToNine()
    {
        IReadOnlyCollection<Town> world = [.. Real.Towns.Values];
        // The groups of the zones; those guarding a dungeon's halls fight its rooms' fights (sprint 86).
        MonsterGroup[] roaming = [.. Real.Groups.Values.Where(g => g.Fight is null)];
        foreach (MonsterGroup g in roaming)
        {
            MonsterSpec[] members = [.. g.Monsters.Select(id => Real.Monsters[id])];
            Assert.All(members, m => Assert.Contains(m, Real.Families[g.Family].Monsters));
            Assert.InRange(members.Length, 1, MonsterGroups.Largest);
            Assert.True(members.Count(m => m.Role == MonsterRole.Support) <= members.Length / 3, g.Id);
            bool chief = members[0].Role == MonsterRole.Boss;
            Assert.Equal(chief ? 1 : 0, members.Count(m => m.Role == MonsterRole.Boss));
            if (chief)
                Assert.InRange(members.Length, 6, MonsterGroups.Largest);
            else
                Assert.True(members.Length <= MonsterGroups.LargestAt(MonsterGroups.Depth(Real.Towns[g.Map], world)), g.Id);
        }
        // Each chief once, on its map.
        foreach (MonsterFamily f in Real.Families.Values)
            Assert.Equal(f.BossMap, Assert.Single(roaming, g => g.Monsters.Contains(f[MonsterRole.Boss].Id)).Map);
        // Next to the way in, small groups only: the woods from the village, the mouldy woods from the woods.
        Assert.Equal((0, 0, 2), (MonsterGroups.Depth(Real.Towns["woods-4-2"], world), MonsterGroups.Depth(Real.Towns["mouldy-4-1"], world), MonsterGroups.Depth(Real.Towns["woods-0-0"], world)));
        Assert.All(Real.GroupsOn("woods-4-2"), g => Assert.InRange(g.Monsters.Count, 1, 3));
        // Hordes of six and more, for several players, at the far end.
        Assert.Contains(Real.Groups.Values, g => g.Monsters.Count >= 6 && g.Monsters.All(m => Real.Monsters[m].Role != MonsterRole.Boss));
    }

    [Fact]
    public void FightingAGroup_IsAScenarioTheRulesBuild_PlayedReplayedAndRewarded()
    {
        MonsterGroup g = Real.Groups.Values.First(g => g.Monsters.Count == 2 && g.Map.StartsWith("woods", StringComparison.Ordinal));
        Scenario s = Real.FindScenario(g.Scenario)!;
        Assert.Equal(("group:" + g.Id, "woods-glade"), (s.Id, s.Map));
        Assert.Equal([0, 1, 1], s.Fighters.Select(f => f.Team));
        // A horde of ten fits on its battle map.
        MonsterGroup horde = Real.Groups.Values.MaxBy(g => g.Monsters.Count)!;
        Assert.Equal(horde.Monsters.Count, new Fight(Real, horde.Scenario, 1).Fighters.Count(f => f.Team == 1));
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
