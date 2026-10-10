using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The characteristics sheet (sprint 64): points put in, points worn, and what they give.</summary>
public class SheetTests
{
    private static Hero Guard(Dictionary<Slot, string>? worn = null) => new("Élise", "female-c", "guard", Level: 10,
        Stats: new Characteristics(Vitality: 20, Strength: 30, Chance: 9), Worn: worn);

    [Fact]
    public void EachLine_AddsWhatIsWorn_ToThePointsPutIn_AndSaysWhatTheTotalGives()
    {
        var sheet = new CharacterSheet(Guard(new() { [Slot.Ring1] = "copper-ring", [Slot.Amulet] = "pebble-amulet", [Slot.TwoHanded] = "orc-club" }), GameData.Embedded);
        Assert.Equal(new SheetLine(Characteristic.Vitality, null, 20, 5, 25, 25, 0), sheet[Characteristic.Vitality]);
        // 30 put in, 20 worn: 50 Strength, 10 more Earth damage.
        Assert.Equal(new SheetLine(Characteristic.Strength, Element.Earth, 30, 20, 50, 10, 0), sheet[Characteristic.Strength]);
        // 9 points: not yet a whole 10, no bonus; nothing else but its own element.
        Assert.Equal((9, 0, Element.Water), (sheet[Characteristic.Chance].Total, sheet[Characteristic.Chance].Bonus, sheet[Characteristic.Chance].Element));
        Assert.Equal((Element.Fire, Element.Air), (sheet[Characteristic.Intelligence].Element!.Value, sheet[Characteristic.Agility].Element!.Value));
        // 70 hit points at level 1, 3 a level, then Vitality; the class's points; 90 points at level 10, 59 spent.
        Assert.Equal((70 + 27 + 25, 7, 3, 100, 31), (sheet.MaxHp, sheet.Ap, sheet.Mp, sheet.Initiative, sheet.PointsLeft));
    }

    [Fact]
    public void TheSheet_SaysWhatTheFightCounts()
    {
        Hero hero = Guard(new() { [Slot.Mount] = "donkey" }) with { Level = 20 };
        var sheet = new CharacterSheet(hero, GameData.Embedded);
        var fight = new Fight(GameData.Embedded, "training", 1, hero);
        Fighter me = fight.Fighters[0];
        Assert.Equal((me.MaxHp, me.Ap, me.Mp), (sheet.MaxHp, sheet.Ap, sheet.Mp));
        Assert.Equal(4, sheet.Mp);
        Assert.Equal(me.Spec.Characteristics.Strength, sheet[Characteristic.Strength].Total);
    }

    [Fact]
    public void AHeroWithoutAClass_HasNoSheet() =>
        Assert.Throws<ArgumentException>(() => new CharacterSheet(new Hero("Ana", "female-a"), GameData.Embedded));
}
