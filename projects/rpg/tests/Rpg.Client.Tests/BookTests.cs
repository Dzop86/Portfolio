using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The spells screen and the looks of the inventory (sprint 65).</summary>
public class BookTests
{
    private static GameData Data => GameData.Embedded;
    private static readonly string[] Samples = ["copper-ring", "orc-club", "bread", "orc-fang", "garance-badge"];

    private static PointsEditor Guard() => new(new Hero("Élise", "female-c", "guard", Level: 10,
        Stats: new Characteristics(Strength: 25), Ranks: new Dictionary<string, int> { ["strike"] = 3 }), Data);

    [Fact]
    public void TheBook_ListsEveryClassSpell_TheLockedOnesToo_InTheOrderTheyUnlock()
    {
        var book = new SpellBook(Guard());
        Assert.Equal(Data.Class("guard")!.Spells, book.Spells.Select(s => s.Spell.Id));
        // Four at level 1, one at 5, one at 10: six for a tenth-level hero.
        Assert.Equal(6, book.Spells.Count(s => s.Unlocked));
        Assert.All(book.Spells.Where(s => !s.Unlocked), s => Assert.True(s.Spell.Level > 10));
    }

    [Fact]
    public void ASpellsDamage_IsAtItsRank_WithThePointsOfItsElementOnly_NormalCriticalAndNext()
    {
        BookSpell strike = new SpellBook(Guard()).Spells.Single(s => s.Spell.Id == "strike");
        Spell at3 = Data.Spells["strike"].AtRank(3), at4 = Data.Spells["strike"].AtRank(4);
        int crit = Spell.CritBonus(at3.DamageMax);
        // 25 Strength: 4 more Earth damage.
        Assert.Equal((3, (at3.DamageMin + 4, at3.DamageMax + 4), (at3.DamageMin + crit + 4, at3.DamageMax + crit + 4)), (strike.Rank, strike.Damage, strike.Critical));
        Assert.Equal((at4.DamageMin + 4, at4.DamageMax + 4), strike.NextRank);
        Assert.Equal(Progression.RankCost(4) - Progression.RankCost(3), strike.NextCost);
        // Fire is not Strength's: no bonus there.
        BookSpell blade = new SpellBook(Guard()).Spells.Single(s => s.Spell.Id == "burning-blade");
        Assert.Equal((Data.Spells["burning-blade"].DamageMin, Data.Spells["burning-blade"].DamageMax), blade.Damage);
        // A spell that does no damage has no range.
        Assert.Equal(((0, 0), (0, 0)), new SpellBook(Guard()).Spells.Where(s => s.Spell.Id == "bulwark").Select(s => (s.Damage, s.Critical)).Single());
    }

    [Fact]
    public void TheBook_FiltersByElement()
    {
        var book = new SpellBook(Guard());
        Assert.All(book.Of(Element.Fire), s => Assert.Equal(Element.Fire, s.Element));
        Assert.NotEmpty(book.Of(Element.Fire));
        Assert.Equal(book.Spells.Count, book.Of(null).Count);
    }

    [Fact]
    public void AnItem_LooksLikeItsSlotOrKind_InTheColourOfWhatItGivesMost()
    {
        Assert.Equal([Glyph.Ring, Glyph.Axe, Glyph.Potion, Glyph.Leaf, Glyph.Scroll],
            Samples.Select(id => ItemStyle.GlyphOf(Data.Items[id])));
        Assert.Equal(ElementStyle.Colour(Element.Fire), ItemStyle.Colour(Data.Items["ember-ring"]));
        Assert.Equal(ElementStyle.Heal, ItemStyle.Colour(Data.Items["orc-breastplate"]));
        Assert.Equal("#bdbdbd", ItemStyle.Colour(Data.Items["donkey"]));
        // A tie goes to the first in the sheet's order: Strength before Chance.
        var even = new Item("even", new LocalizedText("x", "x"), ItemKind.Equipment, ItemSlot.Belt, Stats: new Characteristics(Strength: 5, Chance: 5));
        Assert.Equal(ElementStyle.Colour(Element.Earth), ItemStyle.Colour(even));
        // Every slot has its own shape.
        ItemSlot[] slots = Enum.GetValues<ItemSlot>();
        Assert.Equal(slots.Length, slots.Select(s => ItemStyle.GlyphOf(new Item("x", new LocalizedText("x", "x"), ItemKind.Equipment, s))).Distinct().Count());
    }

    [Fact]
    public void TheSearch_FindsAllItsWords_InTheLanguage_CaseAndAccentsAside()
    {
        Item ring = Data.Items["copper-ring"];
        Assert.True(ItemStyle.Matches(ring, "CUIVRE anneau", "fr"));
        Assert.True(ItemStyle.Matches(ring, "copper", "en"));
        Assert.False(ItemStyle.Matches(ring, "copper", "fr"));
        Assert.False(ItemStyle.Matches(ring, "anneau fer", "fr"));
        Assert.True(ItemStyle.Matches(ring, "  ", "fr"));
        Assert.True(ItemStyle.Matches(Data.Items["donkey"], "ane", "fr"));
    }

    [Fact]
    public void ASpellsDetails_SayEverything_ThenTheNextRank_OrTheLevelThatUnlocksIt()
    {
        var book = new SpellBook(Guard());
        BookSpell strike = book.Spells.Single(s => s.Spell.Id == "strike");
        var fr = new Texts("fr");
        IReadOnlyList<string> lines = fr.SpellDetails(strike, new Characteristics(Strength: 25));
        Spell at3 = Data.Spells["strike"].AtRank(3);
        Assert.Equal($"Coût : {at3.ApCost} PA", lines[0]);
        Assert.Contains("Critique : 10 %", lines);
        Assert.Contains($"{strike.Damage.Min} à {strike.Damage.Max} dégâts Terre", lines);
        Assert.Contains($"{strike.Critical.Min} à {strike.Critical.Max} en critique", lines);
        Assert.Equal($"Rang suivant ({strike.NextCost} point(s) de sort) : {strike.NextRank!.Value.Min} à {strike.NextRank.Value.Max}", lines[^1]);
        BookSpell late = book.Spells[^1];
        Assert.Equal($"Unlocked at level {late.Spell.Level}", new Texts("en").SpellDetails(late, null)[^1]);
        // A heal counts Intelligence; a spell without damage says no range.
        BookSpell wind = book.Spells.Single(s => s.Spell.Id == "second-wind");
        Assert.DoesNotContain(new Texts("en").SpellDetails(wind, new Characteristics(Intelligence: 20)), l => l.Contains("damage", StringComparison.Ordinal));
        // Second Wind heals 10 to 14; 20 Intelligence add 4.
        Assert.Contains(new Texts("en").SpellDetails(wind, new Characteristics(Intelligence: 20)), l => l.StartsWith("heal 14 to 18", StringComparison.Ordinal));
        // Every spell of every class has its details in both languages.
        foreach (string c in new[] { "sentinel", "guard", "mage" })
        {
            var b = new SpellBook(new PointsEditor(new Hero("x", "female-a", c, Level: 100), Data));
            foreach (BookSpell s in b.Spells)
            {
                Assert.DoesNotContain(new Texts("fr").SpellDetails(s, null), l => l.Contains('{', StringComparison.Ordinal));
                Assert.DoesNotContain(new Texts("en").SpellDetails(s, null), l => l.Contains('{', StringComparison.Ordinal));
            }
        }
    }
}
