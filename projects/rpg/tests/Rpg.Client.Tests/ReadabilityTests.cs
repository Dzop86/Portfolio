using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The hover panel, the spell cards and the colours of the elements (sprint 57).</summary>
public class ReadabilityTests
{
    private static readonly LocalizedText Orc = new("Orque", "Orc");
    private static readonly Spell Chill = new("chill", new LocalizedText("Givre", "Chill"), 3, 1, 4, true, false, 6, 8, 2, Element.Water, null,
        [new StatusEffect(Affects.Enemies, Stat.Mp, -2, 2)]);
    private static readonly Spell Ward = new("ward", new LocalizedText("Garde", "Ward"), 2, 0, 0, false, false, 0, 0, 1, Element.Neutral, null,
        [new ShieldEffect(Affects.Caster, 10, 2)]);

    private static FightController Make(int orcHp = 40)
    {
        FighterSpec me = new(new LocalizedText("Élise", "Élise"), "female-c", 0, 50, 6, 3, 200, 0, ["chill", "ward"]);
        FighterSpec orc = new(Orc, "orc", 1, orcHp, 6, 3, 100, 0, ["chill"], new Dictionary<Element, int> { [Element.Water] = 25, [Element.Fire] = -10 });
        var data = new GameData([Chill, Ward], [new MapSpec("m", Orc, ["A..B"])], [new Scenario("s", Orc, "m", [me, orc])]);
        return new FightController(new Fight(data, "s", 1));
    }

    [Fact]
    public void HoveringAFighter_TellsItsPointsResistancesAndEffects()
    {
        FightController c = Make();
        var fr = new Texts("fr");
        Assert.Equal("Orque · 40/40 PV · 6 PA · 3 PM\nRésistances : Feu -10 %, Eau 25 %", c.Info(new Cell(3, 0), fr));
        c.SelectSpell(1);
        c.Click(new Cell(0, 0));
        c.SelectSpell(0);
        c.Click(new Cell(3, 0));
        c.CancelSpell();
        // The one playing shows what is left of its points; a status shows its turns.
        Assert.Equal("Élise · 50/50 PV · 1 PA · 3 PM · bouclier 10", c.Info(new Cell(0, 0), fr));
        Assert.EndsWith("\nEffets : -2 PM (2 tour(s))", c.Info(new Cell(3, 0), fr), StringComparison.Ordinal);
        Assert.Equal("Effects: -2 MP (2 turn(s))", c.Info(new Cell(3, 0), new Texts("en"))!.Split('\n')[^1]);
        Assert.Null(c.Info(new Cell(1, 0), fr));
        Assert.Null(c.Info(null, fr));
    }

    [Fact]
    public void AimingASpell_ForecastsItsDamage_AboveTheTargetsCard()
    {
        FightController c = Make();
        c.SelectSpell(0);
        // 6 to 8 water damage, 25 % resisted.
        Assert.Equal("Orc: 4 to 6 damage\n\nOrc · 40/40 HP · 6 AP · 3 MP\nResistances: Fire -10%, Water 25%", c.Info(new Cell(3, 0), new Texts("en")));
        FightController weak = Make(orcHp: 4);
        weak.SelectSpell(0);
        Assert.StartsWith("Orque : 4 à 6 dégâts (mortel)\n", weak.Info(new Cell(3, 0), new Texts("fr")), StringComparison.Ordinal);
    }

    [Fact]
    public void ASpellCard_TellsCostRangeLimitsDamageAreaAndEffects()
    {
        Spell wave = GameData.Embedded.Spells["tidal-wave"];
        int crit = Spell.CritBonus(wave.DamageMax);
        Assert.Equal($"Raz-de-marée · 5 PA · portée 2-5, en ligne · 1 fois par tour\n{wave.DamageMin} à {wave.DamageMax} dégâts, Eau ({wave.DamageMin + crit} à {wave.DamageMax + crit} en critique, 10 %), zone : ligne de 2, poussée de 1",
            new Texts("fr").SpellCard(wave));
        // With the caster's points: 25 in Chance give 4 more Water damage, critical hit included; Strength gives nothing.
        Assert.StartsWith($"Tidal Wave · 5 AP · range 2-5, in line · 1 per turn\n{wave.DamageMin + 4} to {wave.DamageMax + 4} damage, Water ({wave.DamageMin + crit + 4} to {wave.DamageMax + crit + 4} on a critical hit, 10%)",
            new Texts("en").SpellCard(wave, new Characteristics(Strength: 90, Chance: 25)), StringComparison.Ordinal);
        Assert.Equal("Frost Ward · 3 AP · range 0-4 · 1 per turn · cooldown 2 turn(s)\nshield 10 (1 turn(s)) on allies",
            new Texts("en").SpellCard(GameData.Embedded.Spells["frost-ward"]));
        Assert.EndsWith("summons Ember", new Texts("en").SpellCard(GameData.Embedded.Spells["elemental"]), StringComparison.Ordinal);
        Assert.Equal("Givre · 3 PA · portée 1-4 · 2 fois par tour\n6 à 8 dégâts, Eau, -2 PM (2 tour(s))", new Texts("fr").SpellCard(Chill));
        // Every spell of the game has a card in both languages.
        foreach (Spell s in GameData.Embedded.Spells.Values)
        {
            Assert.DoesNotContain("{", new Texts("fr").SpellCard(s), StringComparison.Ordinal);
            Assert.DoesNotContain("{", new Texts("en").SpellCard(s), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EachElement_HasItsOwnColour_ApartFromHealingAndShields()
    {
        string[] colours = [.. Enum.GetValues<Element>().Select(ElementStyle.Colour), ElementStyle.Heal, ElementStyle.Shield];
        Assert.Equal(colours.Length, colours.Distinct().Count());
        Assert.All(colours, c => Assert.Matches("^#[0-9a-f]{6}$", c));
    }
}
