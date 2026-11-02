using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>A compound consumes its parents (sprint 187, D94): its spells take their place, no other fusion may use them, and it sets off its own reaction.</summary>
public class CompoundsConsumeTests
{
    private static Hero Mage(int level, params Element[] compounds) =>
        new("Ondine", "female-a", "mage", Level: level, Compounds: compounds.Length == 0 ? null : compounds);

    private static IEnumerable<Element> ElementsOf(Hero h) => Talents.GrimoireOf(h, Real).Spells.Select(id => Real.Spells[id].Element);

    [Fact]
    public void Lightning_TakesThePlaceOfFireAndAir_AndDissolvingGivesThemBack()
    {
        Assert.Contains(Element.Fire, ElementsOf(Mage(100)));
        Assert.Contains(Element.Air, ElementsOf(Mage(100)));
        Hero lightning = Mage(100, Element.Lightning);
        Assert.DoesNotContain(Element.Fire, ElementsOf(lightning));
        Assert.DoesNotContain(Element.Air, ElementsOf(lightning));
        Assert.Contains(Element.Earth, ElementsOf(lightning));
        Assert.Contains(Element.Water, ElementsOf(lightning));
        Assert.Equal(8, ElementsOf(lightning).Count(e => e == Element.Lightning));
    }

    [Theory]
    [InlineData("mage")]
    [InlineData("guard")]
    [InlineData("sentinel")]
    public void ThePairs_LeaveABookOfTwentyFourSpells(string cls)
    {
        var hero = new Hero("Ondine", "female-a", cls, Level: 100);
        int count = Talents.GrimoireOf(hero, Real).Spells.Count;
        Assert.Equal(count, Talents.GrimoireOf(hero with { Compounds = [Element.Lightning] }, Real).Spells.Count);
        Assert.Equal(count, Talents.GrimoireOf(hero with { Compounds = [Element.Lightning, Element.Mud] }, Real).Spells.Count);
    }

    [Fact]
    public void TwoCompoundsSharingAnElement_AreRefused_TwoDisjointPairsAccepted()
    {
        Assert.Null(Mage(100, Element.Lightning, Element.Mud).Problem(Real));
        Assert.Null(Mage(100, Element.Magma, Element.Ice).Problem(Real));
        Assert.Null(Mage(100, Element.Sand, Element.Steam).Problem(Real));
        Assert.Equal("'Lightning' shares an element with 'Magma'.", Mage(100, Element.Lightning, Element.Magma).Problem(Real));
        Assert.NotNull(Mage(100, Element.Ether, Element.Mud).Problem(Real));
        Assert.NotNull(Mage(100, Element.Ether, Element.Ash).Problem(Real));
        Assert.Null(Mage(100, Element.Ether).Problem(Real));
    }

    [Fact]
    public void Legal_KeepsTheFirstCreated_WithinTheLevel()
    {
        Element[] all = [Element.Sand, Element.Lightning, Element.Ice, Element.Steam, Element.Mud, Element.Ash];
        Assert.Equal([Element.Sand, Element.Steam], Elements.Legal(all, 100));
        Assert.Equal([Element.Mud], Elements.Legal([Element.Ether, Element.Mud], 30));
        Assert.Equal([Element.Ether], Elements.Legal([Element.Ether, Element.Mud], 50));
        Assert.Empty(Elements.Legal([Element.Lightning], 29));
    }

    [Fact]
    public void BelowItsLevel_ACompoundConsumesNothing()
    {
        Assert.Contains(Element.Fire, ElementsOf(Mage(29, Element.Lightning)));
        Assert.Empty(Talents.ConsumedBy(Mage(29, Element.Lightning), Real));
    }

    [Fact]
    public void TheDeck_FollowsTheCompounds_SpellForSpell()
    {
        Spell fire = Real.Spells.Values.First(s => s.Twin is null && s.Element == Element.Fire && Real.Class("mage")!.Spells.Contains(s.Id));
        Spell twin = Real.Spells.Values.Single(s => s.Twin == fire.Id && s.Element == Element.Lightning);
        Spell earth = Real.Spells.Values.First(s => s.Twin is null && s.Element == Element.Earth && Real.Class("mage")!.Spells.Contains(s.Id));
        Assert.Equal([earth.Id, twin.Id], Talents.FitDeck([earth.Id, fire.Id], Mage(100, Element.Lightning), Real));
        Assert.Equal([fire.Id, earth.Id], Talents.FitDeck([twin.Id, earth.Id], Mage(100), Real));
        Assert.Equal([earth.Id], Talents.FitDeck([earth.Id, "no-such-spell"], Mage(100), Real));
        Assert.Null((Mage(100, Element.Lightning) with { Deck = Talents.FitDeck([earth.Id, fire.Id], Mage(100, Element.Lightning), Real) }).Problem(Real));
    }

    // Two strikes of a compound: the mark, then the reaction, on a small board.
    private static readonly Spell Bolt = new("bolt", Text, 1, 1, 6, false, false, 1, 1, 9, Element.Lightning);
    private static readonly Spell Rime = new("rime", Text, 1, 1, 6, false, false, 1, 1, 9, Element.Ice);
    private static readonly Spell Veil = new("veil", Text, 1, 1, 6, false, false, 1, 1, 9, Element.Ether);

    private static Fight Struck(bool marks = true)
    {
        var tester = new HeroClass("tester", Text, Text, 500, 99, 6, 999, ["bolt", "rime", "veil"], HpPerLevel: 0);
        var data = new GameData([.. Spells, Bolt, Rime, Veil], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0, initiative: 999), Spec(1, 0, hp: 500, initiative: 1)])], [tester], reactions: Real.Reactions.Values);
        return new Fight(data, "s", 1, new Hero("Ondine", "female-a", "tester", Level: 60), compoundMarks: marks);
    }

    private static void Cast(Fight f, string spell) => f.Apply(new CastAction(spell, new Cell(1, 0)));

    [Fact]
    public void ACompound_LeavesItsMark_AndItsNextSpellSetsOffItsReaction()
    {
        Fight f = Struck();
        Cast(f, "bolt");
        Assert.Equal(Element.Lightning, f.Fighters[1].Mark!.Element);
        Assert.Empty(f.Events.OfType<ReactionTriggered>());
        Cast(f, "bolt");
        Assert.Equal("electrified", f.Events.OfType<ReactionTriggered>().Single().Reaction);
        Assert.Null(f.Fighters[1].Mark);
    }

    [Fact]
    public void ACaster_SetsOffOneReactionATurn()
    {
        Fight f = Struck();
        Cast(f, "bolt");
        Cast(f, "bolt");
        Cast(f, "bolt");
        Cast(f, "bolt");
        Assert.Single(f.Events.OfType<ReactionTriggered>());
        // The next turn, again: its mark is still there.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Cast(f, "bolt");
        Assert.Equal(2, f.Events.OfType<ReactionTriggered>().Count());
    }

    [Fact]
    public void AnotherCompound_ReplacesTheMark_WithoutAReaction()
    {
        Fight f = Struck();
        Cast(f, "bolt");
        Cast(f, "rime");
        Assert.Empty(f.Events.OfType<ReactionTriggered>());
        Assert.Equal(Element.Ice, f.Fighters[1].Mark!.Element);
    }

    [Fact]
    public void ATrio_SetsOffItsReactionOnItsOwnMark()
    {
        Fight f = Struck();
        Cast(f, "veil");
        Assert.Equal(Element.Ether, f.Fighters[1].Mark!.Element);
        Cast(f, "veil");
        Assert.Equal("scattered", f.Events.OfType<ReactionTriggered>().Single().Reaction);
    }

    [Fact]
    public void ARecordOfVersionFive_ReplaysWithoutTheCompoundsMarks()
    {
        Fight f = Struck(marks: false);
        Cast(f, "bolt");
        Cast(f, "bolt");
        Assert.Null(f.Fighters[1].Mark);
        Assert.Empty(f.Events.OfType<ReactionTriggered>());
    }

    [Fact]
    public void AFightWithACompound_ReplaysTheSame()
    {
        Hero hero = Progression.Talented(new Hero("Ondine", "female-a", "mage", Level: 40), [], Real, compounds: [Element.Lightning, Element.Mud]);
        var fight = new Fight(Real, "training", 5, hero);
        Ai.PlayOut(fight);
        Assert.Equal(FightRecord.CurrentVersion, FightRecord.Of(fight).Version);
        Assert.Equal(Fingerprint(fight), Fingerprint(FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(Real)));
    }
}
