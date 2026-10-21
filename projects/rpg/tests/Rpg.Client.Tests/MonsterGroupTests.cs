using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The groups of monsters on the maps, as the player meets them (sprint 72).</summary>
public class MonsterGroupTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void AttackingAGroup_WalksUpToIt_AndStartsItsFight_ALeftClickOnlyWalks()
    {
        var town = new TownController(Data, "woods-2-2");
        Assert.Equal(Data.GroupsOn("woods-2-2").Select(g => (g.Id, g.Home)), town.Groups.Select(g => (g.Key, g.Value)));
        MonsterGroup group = Data.GroupsOn("woods-2-2")[0];
        // A left click walks up to it, no more (sprint 78).
        TownStep walk = town.Click(group.Home)!;
        Assert.Null(walk.Group);
        Assert.Null(town.Fight);
        Assert.Null(town.Attack(town.Town.Spawn));
        TownStep step = town.Attack(group.Home)!;
        Assert.Equal(group, step.Group);
        Assert.Equal((group.Scenario, group), (town.Fight, town.FightGroup));
        // Next to the group, never on it.
        Assert.Equal(1, Math.Max(Math.Abs(town.Position.X - group.Home.X), Math.Abs(town.Position.Y - group.Home.Y)));
        Assert.Equal(group, town.GroupAt(group.Home));
        // A walk cut short forgets the fight.
        town.Interrupt(town.Position);
        Assert.Null(town.FightGroup);
        Assert.Null(town.Fight);
    }

    [Fact]
    public void AGroup_BlocksTheWay_AndABeatenOne_IsNotThere()
    {
        MonsterGroup group = Data.GroupsOn("woods-2-2")[0];
        var town = new TownController(Data, "woods-2-2");
        Assert.False(town.IsFree(group.Home));
        Assert.All(town.PathTo(new Cell(8, 5)) ?? [], c => Assert.NotEqual(group.Home, c));
        var without = new TownController(Data, "woods-2-2", absent: [group.Id]);
        Assert.DoesNotContain(group.Id, without.Groups.Keys);
        Assert.True(without.IsFree(group.Home));
        Assert.Null(without.GroupAt(group.Home));
        // Through a way, the beaten groups of the next map stay away.
        var edge = new TownController(Data, "woods-3-2");
        ZoneLink west = edge.Town.Links!.First(l => l.To == "woods-2-2");
        Assert.DoesNotContain(group.Id, edge.Enter(west, [group.Id]).Groups.Keys);
        Assert.Contains(group.Id, edge.Enter(west).Groups.Keys);
        Assert.Empty(new TownController(Data, "clairval").Groups);
    }

    [Fact]
    public void AGroup_MovesOnlyOntoAFreeCell_WhereThePlayerIsNot()
    {
        var town = new TownController(Data, "woods-2-2");
        MonsterGroup group = Data.GroupsOn("woods-2-2")[0];
        Cell free = Wander.Steps.Select(s => group.Home + s).First(c => town.IsFree(c) && c != town.Position);
        town.MoveGroup(group.Id, free);
        Assert.Equal(free, town.Groups[group.Id]);
        Assert.Throws<ArgumentException>(() => town.MoveGroup(group.Id, new Cell(0, 0)));
        Assert.Throws<ArgumentException>(() => town.MoveGroup(group.Id, town.Position));
        Assert.Throws<KeyNotFoundException>(() => town.MoveGroup("woods-0-0.0", free));
    }

    [Fact]
    public void AGroupFoughtBySomeoneElse_LeavesTheMap_AndComesBack_OnAFreeCell()
    {
        // Sprint 118 (D74): the server says a group is fought or beaten; back, where the clock puts it, or at home.
        MonsterGroup group = Data.GroupsOn("woods-2-2")[0];
        var town = new TownController(Data, "woods-2-2");
        town.RemoveGroup(group.Id);
        Assert.Null(town.GroupAt(group.Home));
        Assert.False(town.AddGroup("woods-0-0.0", group.Home));
        Cell free = group.Home.Neighbours().First(town.IsFree);
        Assert.True(town.AddGroup(group.Id, free));
        Assert.Equal(free, town.Groups[group.Id]);
        Assert.False(town.AddGroup(group.Id, free));
        // Its place taken (a wall, or the player): at home.
        town.RemoveGroup(group.Id);
        Assert.True(town.AddGroup(group.Id, new Cell(0, 0)));
        Assert.Equal(group.Home, town.Groups[group.Id]);
    }

    [Fact]
    public void ABeatenGroup_IsAwayForTwoMinutes_ThenBack()
    {
        var presence = new MonsterPresence();
        var t0 = new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
        Assert.False(presence.IsAway("woods-2-2.0", t0));
        presence.Beaten("woods-2-2.0", t0);
        Assert.True(presence.IsAway("woods-2-2.0", t0 + TimeSpan.FromSeconds(119)));
        Assert.Equal(["woods-2-2.0"], presence.Away(t0 + TimeSpan.FromMinutes(1)));
        Assert.False(presence.IsAway("woods-2-2.0", t0 + MonsterPresence.Respawn));
        Assert.Empty(presence.Away(t0 + TimeSpan.FromMinutes(3)));
    }

    [Fact]
    public void AGroupsLabel_NamesEachMonster_WithItsLevel()
    {
        var group = new MonsterGroup("x", "woods-2-2", "boar", new Cell(1, 1), ["boar-sow", "boar-charger"]);
        Assert.Equal("Laie (niv. 2), Sanglier chargeur (niv. 2)", new Texts("fr").GroupLabel(group, Data));
        Assert.Equal("Sow (lvl 2), Charging Boar (lvl 2)", new Texts("en").GroupLabel(group, Data));
        // More than three: the family, how many, their levels.
        var horde = group with { Monsters = ["boar-sow", "boar-charger", "acorn-piglet", "old-loner", "boar-king", "boar-sow"] };
        Assert.Equal("Sangliers ×6 (niv. 1 à 6)", new Texts("fr").GroupLabel(horde, Data));
        Assert.Equal("Boars ×6 (lvl 1 to 6)", new Texts("en").GroupLabel(horde, Data));
    }
}
