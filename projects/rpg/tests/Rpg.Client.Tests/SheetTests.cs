using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The characteristics sheet (sprint 64): points put in, points worn, and what they give.</summary>
public class SheetTests
{
    private static Hero Guard(Dictionary<Slot, string>? worn = null) => new("Élise", "female-c", "guard", Level: 10,
        Stats: new Characteristics(Vitality: 20, Earth: 30, Water: 9), Worn: worn);

    [Fact]
    public void EachLine_AddsWhatIsWorn_ToThePointsPutIn_AndSaysWhatTheTotalGives()
    {
        var sheet = new CharacterSheet(Guard(new() { [Slot.Ring1] = "copper-ring", [Slot.Amulet] = "pebble-amulet", [Slot.TwoHanded] = "orc-club" }), GameData.Embedded);
        Assert.Equal(new SheetLine(Characteristic.Vitality, null, 20, 5, 25, 25, 0, 25), sheet[Characteristic.Vitality]);
        // 30 put in, 21 worn (the orc's club counts as two pieces since sprint 109): 51 Earth; past 35 (3 × level 10 + 5,
        // sprint 162) they count a quarter, 39, 6 more Earth damage.
        Assert.Equal(new SheetLine(Characteristic.Earth, Element.Earth, 30, 21, 51, 6, 0, 39), sheet[Characteristic.Earth]);
        // 9 points: not yet a whole 10, no bonus; nothing else but its own element.
        Assert.Equal((9, 0, Element.Water), (sheet[Characteristic.Water].Total, sheet[Characteristic.Water].Bonus, sheet[Characteristic.Water].Element));
        Assert.Equal((Element.Fire, Element.Air), (sheet[Characteristic.Fire].Element!.Value, sheet[Characteristic.Air].Element!.Value));
        // 70 hit points at level 1, 3 a level, then Vitality; 8 AP, 4 MP and 10 initiative for every class (D62); 90 points at level 10, 59 spent.
        Assert.Equal((74 + 27 + 25, 8, 4, 10, 31), (sheet.MaxHp, sheet.Ap, sheet.Mp, sheet.Initiative, sheet.PointsLeft));
    }

    [Fact]
    public void TheSheet_SaysWhatTheFightCounts()
    {
        Hero hero = Guard(new() { [Slot.Mount] = "donkey" }) with { Level = 20 };
        var sheet = new CharacterSheet(hero, GameData.Embedded);
        var fight = new Fight(GameData.Embedded, "training", 1, hero);
        Fighter me = fight.Fighters[0];
        Assert.Equal((me.MaxHp, me.Ap, me.Mp), (sheet.MaxHp, sheet.Ap, sheet.Mp));
        Assert.Equal(5, sheet.Mp);
        Assert.Equal(me.Spec.Characteristics.Earth, sheet[Characteristic.Earth].Total);
    }

    [Fact]
    public void PastTwiceTheLevel_TheSheetSaysWhatCounts_AndTheSpellBookAgrees()
    {
        // Sprint 162 (D85): at 50, 245 Earth count as 177, +34 damage; the line says "245 (177)" and why, in both languages.
        Hero hero = new("Élise", "female-c", "guard", Level: 50, Stats: new Characteristics(Earth: 245));
        SheetLine earth = new CharacterSheet(hero, GameData.Embedded)[Characteristic.Earth];
        Assert.Equal((245, 177, 34), (earth.Total, earth.Effective, earth.Bonus));
        Assert.Equal("245 (177)", new Texts("fr")["sheet.effective", 245, 177]);
        Assert.StartsWith("245 points, efficace 177 : au-delà de 155 points", new Texts("fr")["sheet.effective.help", 155, 245, 177], StringComparison.Ordinal);
        Assert.StartsWith("245 points, effective 177: above 155 points", new Texts("en")["sheet.effective.help", 155, 245, 177], StringComparison.Ordinal);
        var fight = new Fight(GameData.Embedded, "training", 1, hero);
        Assert.Equal(Characteristics.Threshold(50), fight.Fighters[0].Spec.Threshold);
    }

    [Fact]
    public void AHeroWithoutAClass_HasNoSheet() =>
        Assert.Throws<ArgumentException>(() => new CharacterSheet(new Hero("Ana", "female-a"), GameData.Embedded));

    [Fact]
    public void TheSheet_SaysWhoTheHeroPlaysBefore_AndTheFighterCard_ItsInitiative()
    {
        // Sprint 95 (D71): initiative found on boots, capes and rings; heroes first on a tie.
        var fr = new Texts("fr");
        Assert.StartsWith("Vous jouez après toutes les familles", fr.PlaysBefore(10), StringComparison.Ordinal);
        // At 100, before every family of the first chapter; the forest's play earlier (sprint 128: initiative grows after 30).
        Assert.Equal("Vous jouez avant : " + string.Join(", ", GameData.Embedded.Families.Values.Where(f => f.Monsters.All(m => m.Initiative <= 100)).Select(f => f.Name.Fr)), fr.PlaysBefore(100));
        Assert.DoesNotContain("Sylvecerfs", fr.PlaysBefore(100), StringComparison.Ordinal);
        string[] at75 = [.. GameData.Embedded.Families.Values.Where(f => f.Monsters.All(m => m.Initiative <= 75)).Select(f => f.Name.En)];
        Assert.NotEmpty(at75);
        Assert.Equal("You play before: " + string.Join(", ", at75), new Texts("en").PlaysBefore(75));
        var fight = new Fight(GameData.Embedded, "training", 1);
        Assert.Contains($"Initiative : {fight.Fighters[0].Spec.Initiative}", fr.FighterCard(fight.Fighters[0], fight), StringComparison.Ordinal);
    }
}
