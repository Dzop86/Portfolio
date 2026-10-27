using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The bestiary (sprint 154, D83): what a fight counts, the four steps of knowledge, the weakness, the words of a rarity.</summary>
public class BestiaryTests
{
    [Fact]
    public void AWonFight_CountsAVictory_ForEachMonsterEachTimeItStood_ALostOne_MetOnly()
    {
        MonsterGroup boars = Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null && g.Monsters.Count == 2);
        var won = new Fight(Real, boars.Scenario, 1, Progression.Built(new Hero("Ronce", "female-c", "guard", Level: 60), Real));
        Ai.PlayOut(won);
        Dictionary<string, int> after = Bestiary.AfterFight(new Dictionary<string, int>(), Real, won);
        Assert.All(boars.Monsters.Distinct(), m => Assert.Equal(boars.Monsters.Count(x => x == m), after[m]));
        // A horde of zombies against a hero of level 1: lost.
        MonsterGroup horde = Real.Groups.Values.Where(g => g.Family == "zombie" && g.Fight is null).MaxBy(g => g.Monsters.Count)!;
        var lost = new Fight(Real, horde.Scenario, 1, new Hero("Ronce", "female-c", "guard", Level: 1));
        Ai.PlayOut(lost);
        Assert.NotEqual(0, lost.WinningTeam);
        Assert.All(Bestiary.AfterFight(new Dictionary<string, int>(), Real, lost), m => Assert.Equal(0, m.Value));
    }

    [Fact]
    public void ADungeonsRoom_IsFacedByItsRoles_TheChiefInTheLast()
    {
        Dungeon den = Real.Dungeons["wolf-den"];
        MonsterFamily wolves = Real.Families[den.Family];
        Assert.Equal(4, Bestiary.Faced(Real, den.Scenario(1)).Count);
        Assert.Contains(wolves[MonsterRole.Boss].Id, Bestiary.Faced(Real, den.Scenario(Dungeons.RoomCount)));
        Assert.Empty(Bestiary.Faced(Real, "training"));
    }

    [Theory]
    [InlineData(-1, Knowledge.Unknown)]
    [InlineData(0, Knowledge.Met)]
    [InlineData(1, Knowledge.Numbers)]
    [InlineData(9, Knowledge.Numbers)]
    [InlineData(10, Knowledge.Spells)]
    [InlineData(25, Knowledge.Loot)]
    public void TheVictories_OpenTheSteps(int wins, Knowledge known)
    {
        var victories = wins < 0 ? new Dictionary<string, int>() : new Dictionary<string, int> { ["boar-charger"] = wins };
        Assert.Equal(known, Bestiary.Of(victories, "boar-charger"));
    }

    [Fact]
    public void TheWeakness_IsTheLeastResisted_BelowZero_AndRarities_AreWords()
    {
        // The charging boar: Earth 10, Air −15.
        Assert.Equal(Element.Air, Bestiary.Weakness(Real.Monsters["boar-charger"]));
        Assert.Null(Bestiary.Weakness(Real.Monsters["boar-charger"] with { Resistances = new Dictionary<Element, int> { [Element.Earth] = 5 } }));
        Assert.Equal((Rarity.Common, Rarity.Rare, Rarity.VeryRare), (Bestiary.RarityOf(40), Bestiary.RarityOf(8), Bestiary.RarityOf(2)));
    }

    [Fact]
    public void TheCount_IsWrittenAndReadBack_AndPloufKnowsEverything()
    {
        var victories = new Dictionary<string, int> { ["wolf-alpha"] = 3, ["boar-charger"] = 0 };
        Assert.Equal("boar-charger:0,wolf-alpha:3", Bestiary.Write(victories));
        Assert.Equal(victories, Bestiary.Read(Bestiary.Write(victories)));
        Assert.Empty(Bestiary.Read("broken,also:bad"));
        Dictionary<string, int> full = Bestiary.Full(Real);
        Assert.Equal(Real.Monsters.Count, full.Count);
        Assert.All(full.Keys, m => Assert.Equal(Knowledge.Loot, Bestiary.Of(full, m)));
    }
}
