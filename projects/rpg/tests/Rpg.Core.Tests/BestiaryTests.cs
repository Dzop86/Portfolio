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
    [InlineData("boar-charger", -1, Knowledge.Unknown)]
    [InlineData("boar-charger", 0, Knowledge.Met)]
    [InlineData("boar-charger", 1, Knowledge.Studied)]
    [InlineData("boar-charger", 4, Knowledge.Studied)]
    [InlineData("boar-charger", 5, Knowledge.Known)]
    [InlineData("boar-charger", 24, Knowledge.Known)]
    [InlineData("boar-charger", 25, Knowledge.Mastered)]
    [InlineData("pack-leader", 1, Knowledge.Studied)]
    [InlineData("pack-leader", 2, Knowledge.Known)]
    [InlineData("pack-leader", 5, Knowledge.Mastered)]
    public void TheVictories_OpenTheSteps_FewerForAChief(string monster, int wins, Knowledge known)
    {
        var victories = wins < 0 ? new Dictionary<string, int>() : new Dictionary<string, int> { [monster] = wins };
        Assert.Equal(known, Bestiary.Of(victories, Real.Monsters[monster]));
    }

    [Fact]
    public void TheNextStep_SaysWhatIsLeft_NoneOnceMastered()
    {
        MonsterSpec boar = Real.Monsters["boar-charger"], chief = Real.Monsters["pack-leader"];
        Assert.Equal(MonsterRole.Boss, chief.Role);
        Assert.Equal((Knowledge.Met, 0), Bestiary.Next(new Dictionary<string, int>(), boar));
        Assert.Equal((Knowledge.Studied, 1), Bestiary.Next(new Dictionary<string, int> { ["boar-charger"] = 0 }, boar));
        Assert.Equal((Knowledge.Known, 2), Bestiary.Next(new Dictionary<string, int> { ["boar-charger"] = 3 }, boar));
        Assert.Equal((Knowledge.Mastered, 15), Bestiary.Next(new Dictionary<string, int> { ["boar-charger"] = 10 }, boar));
        Assert.Equal((Knowledge.Mastered, 3), Bestiary.Next(new Dictionary<string, int> { ["pack-leader"] = 2 }, chief));
        Assert.Null(Bestiary.Next(new Dictionary<string, int> { ["pack-leader"] = 9 }, chief));
    }

    [Fact]
    public void TheAccountsLog_KeepsEachMonstersBestCount_AMeetingIncluded()
    {
        Dictionary<string, int> best = Bestiary.Best([
            new Dictionary<string, int> { ["boar-charger"] = 3, ["pack-leader"] = 0 },
            null,
            new Dictionary<string, int> { ["boar-charger"] = 1, ["pack-leader"] = 2, ["spitting-frog"] = 0 },
        ]);
        Assert.Equal(new Dictionary<string, int> { ["boar-charger"] = 3, ["pack-leader"] = 2, ["spitting-frog"] = 0 }, best);
        Assert.Empty(Bestiary.Best([]));
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
        Assert.All(full.Keys, m => Assert.Equal(Knowledge.Mastered, Bestiary.Of(full, Real.Monsters[m])));
    }
}
