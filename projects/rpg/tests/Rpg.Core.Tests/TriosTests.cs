namespace Rpg.Core.Tests;

/// <summary>The four trios (sprint 185, D93, docs/elements-specialisations.md §v3.2): 4 spells a class, a signature, a reaction of three on a parent's mark.</summary>
public class TriosTests
{
    private static GameData Real => GameData.Embedded;

    private static readonly LocalizedText Text = new("essai", "test");

    public static TheoryData<Element, string> Trios => new()
    {
        { Element.Ether, "scattered" },
        { Element.Ash, "buried" },
        { Element.Sap, "rooted" },
        { Element.Salt, "parched" },
    };

    private static Spell[] Twins(Element e) => [.. Real.Spells.Values.Where(s => s.Element == e)];

    // A hero of 60 casting a trio's spell, an enemy at 2 cells, a second enemy and an ally around it.
    private static Fight Duel(string spell, string[]? rows = null, string[]? casterSpells = null)
    {
        FighterSpec me = new(Text, "female-a", 0, 100, 30, 3, 200, 0, casterSpells ?? [spell]) { Reacts = true };
        FighterSpec foe = new(Text, "orc", 1, 900, 6, 3, 0, 0, ["strike"]);
        var data = new GameData(Real.Spells.Values, [new MapSpec("m", Text, rows ?? ["A.B.."])], [new Scenario("s", Text, "m", [me, foe])],
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values);
        var fight = new Fight(data, "s", 1);
        fight.Fighters[0].LaysMarks = true;
        return fight;
    }

    private static Spell Reaching(Element e, int distance = 2) => Twins(e).First(t => t.DamageMax > 0 && t.Area is null && t.MinRange <= distance && t.MaxRange >= distance);

    [Theory]
    [MemberData(nameof(Trios))]
    public void EachTrio_HasFourSpellsAClass_OfItsThreeParents_At75PercentOfTheBudget(Element e, string reaction)
    {
        _ = reaction;
        Assert.Equal(3, Elements.Parents(e).Count);
        Assert.Equal(Elements.Trios, Elements.CreatedFrom(e));
        Spell[] twins = Twins(e);
        Assert.Equal(12, twins.Length);
        foreach (HeroClass c in Real.Classes)
            Assert.Equal(4, twins.Count(t => c.Spells.Contains(t.Twin!)));
        Assert.All(twins, t =>
        {
            Spell origin = Real.Spells[t.Twin!];
            Assert.Contains(origin.Element, Elements.Parents(e));
            Assert.Equal((origin.ApCost, origin.MinRange, origin.MaxRange, origin.Level), (t.ApCost, t.MinRange, t.MaxRange, t.Level));
            Assert.InRange((double)t.DamageMax / origin.DamageMax, 0.6, 0.9);
        });
        Assert.Equal(3, twins.Select(t => Real.Spells[t.Twin!].Element).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Trios))]
    public void ATrio_IsRefusedAt49_AndReachesTheGrimoireAt50(Element e, string reaction)
    {
        _ = reaction;
        Assert.NotNull(Elements.Problem([e], 49));
        Assert.Null(Elements.Problem([e], 50));
        Hero hero = new("Essai", "female-a", "mage", Level: 50, Compounds: [e]);
        Assert.Contains(Talents.GrimoireOf(hero, Real).Spells, id => Real.Spells[id].Element == e);
        Assert.DoesNotContain(Talents.GrimoireOf(hero with { Level = 49 }, Real).Spells, id => Real.Spells[id].Element == e);
    }

    [Theory]
    [InlineData(Element.Ether, "scattered", Element.Fire)]
    [InlineData(Element.Ether, "scattered", Element.Water)]
    [InlineData(Element.Ether, "scattered", Element.Air)]
    [InlineData(Element.Ash, "buried", Element.Earth)]
    [InlineData(Element.Sap, "rooted", Element.Water)]
    [InlineData(Element.Salt, "parched", Element.Air)]
    public void ATrioSpell_OnAParentsMark_SetsOffTheTriosReaction_NotThePairs(Element e, string reaction, Element parent)
    {
        Spell twin = Reaching(e);
        Fight fight = Duel(twin.Id);
        Fighter foe = fight.Fighters[1];
        foe.Mark = new Mark(parent, fight.Fighters[0].Id, 2);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction(twin.Id, foe.Cell)));
        Assert.Equal(reaction, Assert.Single(fight.Events.OfType<ReactionTriggered>()).Reaction);
        Assert.Null(foe.Mark);
    }

    [Theory]
    [InlineData(Element.Ether, Element.Earth)]
    [InlineData(Element.Ash, Element.Water)]
    [InlineData(Element.Sap, Element.Air)]
    [InlineData(Element.Salt, Element.Fire)]
    public void ATrioSpell_OnTheMarkOfTheElementItLacks_SetsNothingOff_AndLeavesItsOwn(Element e, Element absent)
    {
        Spell twin = Reaching(e);
        Fight fight = Duel(twin.Id);
        Fighter foe = fight.Fighters[1];
        foe.Mark = new Mark(absent, fight.Fighters[0].Id, 2);
        fight.Apply(new CastAction(twin.Id, foe.Cell));
        Assert.Empty(fight.Events.OfType<ReactionTriggered>());
        // Sprint 187 (D94): a compound leaves its mark.
        Assert.Equal(e, foe.Mark?.Element);
    }

    [Fact]
    public void Below30_NoReaction_ATrioNeitherConsumesAMark()
    {
        Spell twin = Reaching(Element.Salt);
        FighterSpec me = new(Text, "female-a", 0, 100, 30, 3, 200, 0, [twin.Id]);
        FighterSpec foe = new(Text, "orc", 1, 900, 6, 3, 0, 0, ["strike"]);
        var data = new GameData(Real.Spells.Values, [new MapSpec("m", Text, ["A.B.."])], [new Scenario("s", Text, "m", [me, foe])], reactions: Real.Reactions.Values, summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values);
        var fight = new Fight(data, "s", 1);
        fight.Fighters[0].LaysMarks = true;
        fight.Fighters[1].Mark = new Mark(Element.Air, 0, 2);
        fight.Apply(new CastAction(twin.Id, fight.Fighters[1].Cell));
        Assert.Empty(fight.Events.OfType<ReactionTriggered>());
    }

    [Fact]
    public void Scattered_TakesTheShields_AndTheCellStopsBlockingWayAndSight()
    {
        Spell twin = Reaching(Element.Ether);
        Fight fight = Duel(twin.Id, ["A.B.."]);
        Fighter me = fight.Fighters[0], foe = fight.Fighters[1];
        foe.Shields.Add((50, 3));
        foe.Mark = new Mark(Element.Fire, me.Id, 2);
        Assert.False(foe.Incorporeal);
        fight.Apply(new CastAction(twin.Id, foe.Cell));
        Assert.Equal(0, foe.Shield);
        Assert.True(foe.Incorporeal);
        Assert.False(fight.BlocksSight(foe.Cell));
        // One may walk through it, never stop on it.
        var reach = fight.ReachableCells();
        Cell beyond = foe.Cell + new Cell(1, 0);
        Assert.True(reach.ContainsKey(beyond));
        Assert.False(reach.ContainsKey(foe.Cell));
    }

    [Fact]
    public void Buried_LaysAnAshGlyph_OnTheTargetAndItsFourNeighbours()
    {
        Spell twin = Reaching(Element.Ash);
        Fight fight = Duel(twin.Id, [".....", "A.B..", "....."]);
        Fighter foe = fight.Fighters[1];
        foe.Mark = new Mark(Element.Earth, fight.Fighters[0].Id, 2);
        fight.Apply(new CastAction(twin.Id, foe.Cell));
        GlyphPlaced buried = fight.Events.OfType<GlyphPlaced>().Single(g => g.Cells.Count == 5);
        Assert.Equal(Element.Ash, buried.Element);
        Assert.Equal(2, buried.Turns);
        Assert.Equal(5, buried.Cells.Count);
        Assert.All(new[] { new Cell(0, 0), new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) }, d => Assert.Contains(foe.Cell + d, buried.Cells));
    }

    [Fact]
    public void Rooted_TakesAllTheMovement_AndHealsTheAlliesAround()
    {
        Spell twin = Reaching(Element.Sap);
        Fight fight = Duel(twin.Id);
        Fighter me = fight.Fighters[0], foe = fight.Fighters[1];
        foe.Mark = new Mark(Element.Water, me.Id, 2);
        me.Hp = 40;
        fight.Apply(new CastAction(twin.Id, foe.Cell));
        Assert.Contains(foe.StatusList, s => s.Stat == Stat.Mp && s.Value <= -foe.Spec.Mp);
        Assert.Contains(fight.Events.OfType<Healed>(), h => h.Fighter == me.Id);
        Assert.True(me.Hp > 40);
    }

    [Fact]
    public void Parched_RefusesHealingAndShields_ForTwoTurns()
    {
        Spell twin = Reaching(Element.Salt);
        Fight fight = Duel(twin.Id);
        Fighter me = fight.Fighters[0], foe = fight.Fighters[1];
        foe.Mark = new Mark(Element.Air, me.Id, 2);
        fight.Apply(new CastAction(twin.Id, foe.Cell));
        Status status = Assert.Single(foe.Statuses, s => s.Stat == Stat.Parched);
        Assert.Equal(2, status.TurnsLeft);
        Assert.True(foe.Parched);
        foe.Hp = 100;
        Assert.Equal(0, foe.HealFor(40));
        Assert.Equal(100, foe.Hp);
    }

    [Fact]
    public void TheSignatures_Traversal_Fallout_Bud_AndBrine()
    {
        Assert.All(Twins(Element.Ether), t => Assert.False(t.LineOfSight));
        Assert.All(Twins(Element.Ash), t => Assert.Contains(t.AllEffects, x => x is GlyphEffect { Turns: 2, Element: Element.Ash, DamageMax: > 0 }));
        Assert.All(Twins(Element.Salt), t => Assert.Contains(t.AllEffects, x => x is StatusEffect { Stat: Stat.Healing, Value: -50, Turns: 2, Affects: Affects.Enemies }));
        // Bud: the most wounded ally within 2 cells of the caster, for 20 % of the damage.
        Spell bud = Reaching(Element.Sap);
        Fight fight = Duel(bud.Id);
        Fighter me = fight.Fighters[0];
        me.Hp = 40;
        fight.Apply(new CastAction(bud.Id, fight.Fighters[1].Cell));
        int dealt = fight.Events.OfType<Damaged>().First().Amount;
        Assert.Equal(40 + dealt * Elements.Bud / 100, me.Hp);
    }

    [Fact]
    public void Brine_HalvesTheHealingReceived()
    {
        Spell brine = Reaching(Element.Salt);
        Fight fight = Duel(brine.Id);
        Fighter foe = fight.Fighters[1];
        fight.Apply(new CastAction(brine.Id, foe.Cell));
        foe.Hp = 100;
        Assert.Equal(20, foe.HealFor(40));
        Assert.Equal(120, foe.Hp);
    }

    [Fact]
    public void TheAi_PlaysAHeroWithEveryCompound_ToTheEnd()
    {
        Hero hero = Progression.Talented(new Hero("Essai", "female-a", "mage", Level: 60), [], Real, compounds: [.. Elements.Compounds]);
        var fight = new Fight(Real, Real.Scenarios.Keys.First(k => k == "the-disjoint"), 3, hero);
        Assert.True(Ai.PlayOut(fight) > 0);
        Assert.True(fight.IsOver);
    }
}
