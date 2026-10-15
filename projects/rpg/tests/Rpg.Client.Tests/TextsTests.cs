using System.Text.RegularExpressions;
using Rpg.Core;

namespace Rpg.Client.Tests;

public partial class TextsTests
{
    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void BothLanguages_HaveTheSameKeysAndPlaceholders()
    {
        Assert.Equal(Texts.Fr.Keys.Order(StringComparer.Ordinal), Texts.En.Keys.Order(StringComparer.Ordinal));
        foreach (string key in Texts.Fr.Keys)
        {
            Assert.Equal(Placeholder().Matches(Texts.Fr[key]).Select(m => m.Value).Order(StringComparer.Ordinal),
                Placeholder().Matches(Texts.En[key]).Select(m => m.Value).Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EveryErrorAPlayerCanMeet_HasAMessage()
    {
        foreach (ActionError e in Enum.GetValues<ActionError>().Where(e => e != ActionError.None))
        {
            Assert.False(string.IsNullOrWhiteSpace(new Texts("fr").Error(e)), e.ToString());
            Assert.False(string.IsNullOrWhiteSpace(new Texts("en").Error(e)), e.ToString());
        }
    }

    [Fact]
    public void TheLog_TellsTheFightInThePlayersLanguage()
    {
        var fight = new Fight(GameData.Embedded, "training", 7);
        Ai.PlayOut(fight);
        var fr = new Texts("fr");
        string[] lines = [.. fight.Events.Select(e => fr.Describe(e, fight)).OfType<string>()];
        Assert.Contains(lines, l => l.StartsWith("Héroïne lance ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.EndsWith(" est vaincu(e).", StringComparison.Ordinal));
        Assert.Equal("Round 3", new Texts("en")["round", 3]);
        Assert.Equal("Flèche (4 PA, portée 2-6)", fr.Spell(GameData.Embedded.Spells["arrow"]));
        Assert.Equal("en", new Texts("de").Lang);
    }

    [Fact]
    public void EveryPlayableLook_HasAName_InBothLanguages()
    {
        Assert.Equal("Femme C", new Texts("fr").Look("female-c"));
        Assert.Equal("Man F", new Texts("en").Look("male-f"));
        foreach (string look in Hero.Looks)
        {
            Assert.DoesNotContain("-", new Texts("fr").Look(look), StringComparison.Ordinal);
            Assert.DoesNotContain("-", new Texts("en").Look(look), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AClass_IsDescribedWithItsPointsAndElements_ThenItsSpellsByLevel()
    {
        HeroClass mage = GameData.Embedded.Class("mage")!;
        Assert.Equal("Fragile, mais frappe en zone, soigne ses alliés et invoque une braise. 86 PV · 8 PA · 4 PM · Terre, Feu, Eau, Air", new Texts("fr").Class(mage));
        Assert.EndsWith("74 HP · 8 AP · 4 MP · Earth, Fire, Water, Air", new Texts("en").Class(GameData.Embedded.Class("guard")!), StringComparison.Ordinal);
        Assert.StartsWith("Niv. 1 : Caillou, Flammèche, Éclat de glace, Bourrasque, Soin · Niv. 5 : Bouclier de givre · ", new Texts("fr").ClassSpells(mage), StringComparison.Ordinal);
        Assert.EndsWith(" · Lv 90: Rebirth · Lv 95: Light Step", new Texts("en").ClassSpells(mage), StringComparison.Ordinal);
    }

    [Fact]
    public void TheLog_TellsTheEffects_InBothLanguages()
    {
        var t = new LocalizedText("t", "t");
        var venom = new Spell("venom", t, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null,
            [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2, Element.Water), new StatusEffect(Affects.Enemies, Stat.Ap, -2, 1), new PushEffect(Affects.Enemies, 1)]);
        var mend = new Spell("mend", t, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Caster, 5, 5), new ShieldEffect(Affects.Caster, 4, 1)]);
        var sting = new Spell("sting", t, 1, 0, 0, false, false, 10, 10, 9);
        var data = new GameData([venom, mend, sting], [new MapSpec("m", t, ["A.B."])],
            [new Scenario("s", t, "m", [new FighterSpec(new LocalizedText("Ana", "Ana"), "female-a", 0, 50, 20, 0, 99, 0, ["venom", "mend", "sting"]), new FighterSpec(new LocalizedText("Orc", "Orc"), "orc", 1, 50, 6, 0, 1, 0, [])])]);
        var fight = new Fight(data, "s", 1);
        // Ana hurts herself first, to have something to heal.
        fight.Apply(new CastAction("sting", new Cell(0, 0)));
        fight.Apply(new CastAction("venom", new Cell(2, 0)));
        fight.Apply(new CastAction("mend", new Cell(0, 0)));
        fight.Apply(new EndTurnAction());
        string[] fr = [.. fight.Events.Select(e => new Texts("fr").Describe(e, fight)).OfType<string>()];
        Assert.Contains("Orc : poison 6 (Eau) pendant 2 tour(s).", fr);
        Assert.Contains("Orc : -2 PA pendant 1 tour(s).", fr);
        Assert.Contains("Orc est déplacé(e) de 1 case(s).", fr);
        Assert.Contains("Ana récupère 5 PV.", fr);
        Assert.Contains("Ana gagne un bouclier de 4.", fr);
        string[] en = [.. fight.Events.Select(e => new Texts("en").Describe(e, fight)).OfType<string>()];
        Assert.Contains("Orc: poison 6 (Water) for 2 turn(s).", en);
        Assert.Contains("Orc loses 6 HP.", en);
    }

    [Fact]
    public void APassive_IsOnTheMonstersCard_AndItsFiring_LeapsAndSwapsInTheLog()
    {
        // Sprint 90: a monster with a passive that answers a blow, a leap and a swap.
        var text = new LocalizedText("x", "x");
        var hit = new Spell("hit", new LocalizedText("Coup", "Blow"), 1, 1, 6, false, false, 5, 5, 9);
        var jump = new Spell("jump", text, 1, 2, 4, false, false, 0, 0, 9, Effects: [new LeapEffect(Affects.Caster)]);
        var trade = new Spell("trade", text, 1, 1, 4, false, false, 0, 0, 9, Effects: [new SwapEffect(Affects.Allies)]);
        var thorns = new PassiveSpec("thorns", new LocalizedText("Épines", "Thorns"), new LocalizedText("Qui le frappe au contact perd 1 PM.", "Whoever strikes it up close loses 1 MP."),
            Trigger.Hit, [new StatusEffect(Affects.Attacker, Stat.Mp, -1, 1)], Melee: true);
        FighterSpec Who(int team, int start, string name, params string[] spells) =>
            new(new LocalizedText(name, name), "male-a", team, 50, 9, 3, team == 0 ? 99 - start : 1, start, spells);
        var data = new GameData([hit, jump, trade], [new MapSpec("m", text, ["A.AB..."])],
            [new Scenario("s", text, "m", [Who(0, 0, "Ada", "jump", "trade", "hit"), Who(0, 1, "Bob"), Who(1, 0, "Ours") with { Passives = ["thorns"] }])], passives: [thorns]);
        var fight = new Fight(data, "s", 1);
        Assert.EndsWith("Épines : Qui le frappe au contact perd 1 PM.", new Texts("fr").FighterCard(fight.Fighters[2], fight), StringComparison.Ordinal);
        Assert.EndsWith("Thorns: Whoever strikes it up close loses 1 MP.", new Texts("en").FighterCard(fight.Fighters[2], fight), StringComparison.Ordinal);
        // Ada trades places with Bob, leaps next to the bear and strikes it.
        fight.Apply(new CastAction("trade", new Cell(2, 0)));
        fight.Apply(new CastAction("jump", new Cell(4, 0)));
        fight.Apply(new CastAction("hit", new Cell(3, 0)));
        string[] lines = [.. fight.Events.Select(e => new Texts("fr").Describe(e, fight)).OfType<string>()];
        Assert.Contains("Ada et Bob échangent leurs places.", lines);
        Assert.Contains("Ada bondit.", lines);
        Assert.Contains("Ours : Épines !", lines);
        Assert.Contains("swap places on allies", new Texts("en").SpellCard(trade), StringComparison.Ordinal);
        Assert.EndsWith("\nbond sur la case", new Texts("fr").SpellCard(jump), StringComparison.Ordinal);
    }
}
