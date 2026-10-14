using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The dungeons (sprint 80, D65): one per family, three rooms, a key or the keyring.</summary>
public class DungeonTests
{
    private static readonly MonsterRole[] Common = [MonsterRole.Melee, MonsterRole.Ranged, MonsterRole.Tank, MonsterRole.Support];

    [Fact]
    public void EveryFamily_HasItsDungeon_ItsDoorOnTheChiefsMap_ItsKeyAndThreeRoomsOfItsOwn()
    {
        Assert.Equal(Real.Families.Keys.Order(), Real.Dungeons.Values.Select(d => d.Family).Order());
        Assert.Equal(Real.Dungeons.Count * Dungeons.RoomCount, Real.Dungeons.Values.SelectMany(d => d.Rooms).Distinct().Count());
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Town map = Real.Towns[d.Map];
            Assert.Equal(Real.Families[d.Family].BossMap, d.Map);
            Assert.Equal([new DungeonDoor(d.Door, d.Id)], map.Doors);
            Assert.Equal(new DungeonDoor(d.Door, d.Id), map.DoorAt(d.Door));
            Assert.True(map.CanStand(d.Door), d.Id);
            Assert.Equal(ItemKind.Resource, Real.Items[d.Key].Kind);
            Assert.False(string.IsNullOrWhiteSpace(d.Name.Fr) || string.IsNullOrWhiteSpace(d.Name.En));
        }
        Assert.Equal(ItemKind.Quest, Real.Items[Dungeons.Keyring].Kind);
        // The keyring is the operator's: no monster leaves it.
        Assert.DoesNotContain(Real.Monsters.Values, m => (m.Loot ?? []).Any(l => l.Item == Dungeons.Keyring));
    }

    [Fact]
    public void TheFirstRoom_HasOneMonsterOfEachRole_Weakened_TheSecondTheSame_TheThirdTheChiefToo()
    {
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            MonsterFamily family = Real.Families[d.Family];
            for (int room = 1; room <= Dungeons.RoomCount; room++)
            {
                Scenario s = Real.FindScenario(d.Scenario(room))!;
                Assert.Equal(d.Rooms[room - 1], s.Map);
                FighterSpec[] monsters = [.. s.Fighters.Where(f => f.Team == 1)];
                MonsterRole[] roles = room == 3 ? [.. Common, MonsterRole.Boss] : Common;
                Assert.Equal(roles.Select(r => family[r].Name), monsters.Select(m => m.Name));
                int power = room == 1 ? Dungeons.EasyPower : 100;
                Assert.All(monsters.Zip(roles), p => Assert.Equal((family[p.Second].Hp * power / 100, power), (p.First.Hp, p.First.Power)));
                Assert.Single(s.Fighters, f => f.Team == 0);
            }
        }
        Assert.Null(Real.FindScenario("dungeon:boar-wallow:4"));
        Assert.Null(Real.FindScenario("dungeon:nowhere:1"));
    }

    [Fact]
    public void AWeakenedMonster_HitsThatMuchLess_AFullOneAsBefore()
    {
        FighterSpec full = Spec(1, spells: "strike"), weak = full with { Power = 60 };
        Fight f = Fight(["AB", "B."], Spec(0), full, weak with { Start = 1 });
        Fighter target = f.Fighters[0];
        Assert.Equal(10, Fight.Damage(10, f.Fighters[1], target, Element.Neutral));
        Assert.Equal(6, Fight.Damage(10, f.Fighters[2], target, Element.Neutral));
        Assert.Equal(13, Fight.Damage(23, f.Fighters[2], target, Element.Neutral));
    }

    [Fact]
    public void TheIdOfARoom_IsReadBack_AndNothingElse()
    {
        Assert.Equal(("boar-wallow", 2), Dungeons.Parse("dungeon:boar-wallow:2"));
        Assert.Null(Dungeons.Parse("dungeon:boar-wallow:0"));
        Assert.Null(Dungeons.Parse("dungeon:boar-wallow:4"));
        Assert.Null(Dungeons.Parse("dungeon:boar-wallow"));
        Assert.Null(Dungeons.Parse("dungeon:boar-wallow:-1"));
        Assert.Null(Dungeons.Parse("group:woods-0-0.0"));
    }

    [Fact]
    public void TheKey_OpensItsDungeonAndIsUsedUp_TheKeyringOpensThemAll_AndStays()
    {
        Dungeon wallow = Real.Dungeons["boar-wallow"], den = Real.Dungeons["wolf-den"];
        ItemCount[] key = [new(wallow.Key, 1)], keyring = [new(Dungeons.Keyring, 1)];
        Assert.True(Dungeons.CanEnter(wallow, key));
        Assert.False(Dungeons.CanEnter(den, key));
        Assert.False(Dungeons.CanEnter(wallow, []));
        Assert.False(Dungeons.CanEnter(wallow, [new(wallow.Key, 0)]));
        Assert.Equal(new ItemCount(wallow.Key, 1), Dungeons.UsedOnEntry(wallow, key));
        Assert.All(Real.Dungeons.Values, d => Assert.True(Dungeons.CanEnter(d, keyring)));
        Assert.All(Real.Dungeons.Values, d => Assert.Null(Dungeons.UsedOnEntry(d, [.. keyring, new(d.Key, 1)])));
    }

    [Fact]
    public void NoGroup_StandsNearADoor_NorCutsTheWayToIt()
    {
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Assert.All(Real.GroupsOn(d.Map), g => Assert.True(g.Home.DistanceTo(d.Door) >= MonsterGroups.Distance, g.Id));
            Town map = Real.Towns[d.Map];
            Assert.True(MonsterGroups.KeepsWaysOpen(map, map.Board(), [.. Real.GroupsOn(d.Map).Select(g => g.Home)]));
            // A group on the door itself would shut it.
            Assert.False(MonsterGroups.KeepsWaysOpen(map, map.Board(), [d.Door]));
        }
    }

    [Fact]
    public void ADungeon_WithoutItsFamily_KeyRoomsOrAReachableDoor_IsRefused()
    {
        Dungeon wallow = Real.Dungeons["boar-wallow"];
        GameData With(Dungeon d) => new(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, Real.Families.Values, [d], Real.Jobs.Values);
        Assert.Equal(wallow, With(wallow).Dungeons["boar-wallow"]);
        Assert.Throws<InvalidDataException>(() => With(wallow with { Family = "dragon" }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Map = "clairval" }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Map = "lake-0-0" }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Door = new Cell(0, 0) }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Door = Real.Towns[wallow.Map].Spawn }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Key = "boar-tusk-key" }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Key = "copper-ring" }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Rooms = wallow.Rooms.Take(2).ToList() }));
        Assert.Throws<InvalidDataException>(() => With(wallow with { Rooms = [.. wallow.Rooms.Take(2), "nowhere"] }));
        // The first room of the old arena has no room for five monsters.
        Assert.Throws<InvalidDataException>(() => With(wallow with { Rooms = [.. wallow.Rooms.Take(2), "arena"] }));
    }
}
