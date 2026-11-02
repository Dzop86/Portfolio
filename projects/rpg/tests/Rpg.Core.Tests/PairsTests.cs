namespace Rpg.Core.Tests;

/// <summary>The five pairs after Lightning (sprint 184, D93, docs/elements-specialisations.md §v3.2): 24 twins each, at 80 % of the budget, their signature paid for by it.</summary>
public class PairsTests
{
    private static GameData Real => GameData.Embedded;

    public static TheoryData<Element, Stat, int> Signatures => new()
    {
        { Element.Ice, Stat.Ap, -1 },
        { Element.Mud, Stat.Mp, -1 },
        { Element.Magma, Stat.Resistance, -8 },
        { Element.Steam, Stat.Damage, -10 },
        { Element.Sand, Stat.Range, -1 },
    };

    private static Spell[] Twins(Element e) => [.. Real.Spells.Values.Where(s => s.Element == e)];

    [Fact]
    public void TheSixPairs_AreCompounds_WithTheParentsOfTheirReactions()
    {
        Element[] pairs = [.. Elements.Compounds.Where(e => Elements.CreatedFrom(e) == Elements.Pairs)];
        Assert.Equal(6, pairs.Length);
        Assert.All(pairs, e =>
        {
            Assert.Equal(2, Elements.Parents(e).Count);
            Assert.NotNull(Talents.ReactionOf(Elements.Parents(e)[0], Elements.Parents(e)[1], Real));
            Assert.Equal(Elements.Pairs, Elements.CreatedFrom(e));
        });
        Assert.Equal(6, pairs.Select(e => string.Join('+', Elements.Parents(e).Order())).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EachPair_HasTwentyFourTwins_EightAClass_OfItsParents(Element e, Stat stat, int value)
    {
        _ = (stat, value);
        Spell[] twins = Twins(e);
        Assert.Equal(24, twins.Length);
        Assert.All(twins, t => Assert.Contains(Real.Spells[t.Twin!].Element, Elements.Parents(e)));
        foreach (HeroClass c in Real.Classes)
            Assert.Equal(8, twins.Count(t => c.Spells.Contains(t.Twin!)));
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EachTwin_CostsWhatItsOriginCosts_AndHitsForLess(Element e, Stat stat, int value)
    {
        _ = (stat, value);
        Assert.All(Twins(e), t =>
        {
            Spell origin = Real.Spells[t.Twin!];
            Assert.Equal((origin.ApCost, origin.MinRange, origin.MaxRange, origin.Level, origin.Cooldown), (t.ApCost, t.MinRange, t.MaxRange, t.Level, t.Cooldown));
            // At 80 % of the budget (rounded by spells_gen.py): never more than its origin, Rockfall's step above the budget apart.
            Assert.InRange(t.DamageMax, 1, origin.DamageMax);
            Assert.InRange((double)t.DamageMax / origin.DamageMax, 0.65, 0.95);
        });
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EachTwin_BearsItsSignature_OnTheEnemiesItStrikes(Element e, Stat stat, int value)
    {
        Assert.All(Twins(e), t =>
        {
            StatusEffect[] signature = [.. t.AllEffects.OfType<StatusEffect>().Where(s => s.Stat == stat && s.Affects == Affects.Enemies)];
            Assert.Single(signature);
            // A spell that already took this from its origin keeps its own, the stronger: statuses of one caster do not add up.
            Assert.True(signature[0].Value <= value, $"{t.Id}: {signature[0].Value}");
            Assert.True(signature[0].Turns >= 1);
            Assert.False(string.IsNullOrEmpty(t.Name.Fr));
            Assert.False(string.IsNullOrEmpty(t.Name.En));
        });
    }

    [Theory]
    [InlineData(Element.Ice, Stat.Ap, -1)]
    [InlineData(Element.Mud, Stat.Mp, -1)]
    [InlineData(Element.Magma, Stat.Resistance, -8)]
    [InlineData(Element.Steam, Stat.Damage, -10)]
    [InlineData(Element.Sand, Stat.Range, -1)]
    public void TheSignature_LandsInAFight(Element e, Stat stat, int value)
    {
        // A twin reaching 2 cells, cast on a sparring partner: the status is on it, in its value, for the next turn.
        Spell twin = Twins(e).First(t => t.DamageMax > 0 && t.Area is null && t.MinRange <= 2 && t.MaxRange >= 2
            && t.AllEffects.OfType<StatusEffect>().Any(s => s.Stat == stat && s.Value == value));
        var text = new LocalizedText("essai", "test");
        FighterSpec me = new(text, "female-a", 0, 50, 20, 3, 200, 0, [twin.Id]), target = new(text, "orc", 1, 900, 6, 3, 0, 0, ["strike"]);
        var data = new GameData(Real.Spells.Values, [new MapSpec("m", text, ["A.B"])], [new Scenario("s", text, "m", [me, target])],
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values);
        var fight = new Fight(data, "s", 1);
        Fighter struck = fight.Fighters[1];
        Assert.Equal(ActionError.None, fight.Apply(new CastAction(twin.Id, struck.Cell)));
        Assert.Contains(struck.StatusList, s => s.Stat == stat && s.Value == value);
    }

    [Fact]
    public void TheNames_AreAllDifferent_InBothLanguages()
    {
        string[] fr = [.. Real.Spells.Values.Where(s => s.Twin is not null).Select(s => s.Name.Fr)], en = [.. Real.Spells.Values.Where(s => s.Twin is not null).Select(s => s.Name.En)];
        Assert.Equal(fr.Length, fr.Distinct().Count());
        Assert.Equal(en.Length, en.Distinct().Count());
    }

    [Theory]
    [InlineData(Element.Ice, Element.Water, Element.Air)]
    [InlineData(Element.Mud, Element.Earth, Element.Water)]
    [InlineData(Element.Magma, Element.Earth, Element.Fire)]
    [InlineData(Element.Steam, Element.Fire, Element.Water)]
    [InlineData(Element.Sand, Element.Earth, Element.Air)]
    public void ACompound_StrikesOnTheHighestOfItsParents(Element e, Element a, Element b)
    {
        var low = new Characteristics(Earth: 10, Fire: 10, Water: 10, Air: 10);
        Characteristics Only(Element x, int v) => x switch
        {
            Element.Earth => low with { Earth = v },
            Element.Fire => low with { Fire = v },
            Element.Water => low with { Water = v },
            _ => low with { Air = v },
        };
        Assert.Equal(90, Only(a, 90).For(e));
        Assert.Equal(90, Only(b, 90).For(e));
        Assert.Equal(10, low.For(e));
        Element other = new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }.First(x => x != a && x != b);
        Assert.Equal(10, Only(other, 90).For(e));
    }

    [Fact]
    public void ThePairs_ReachTheGrimoire_OnlyCreated_AndOnlyFromThirty()
    {
        Hero bare = new("Essai", "female-a", "mage", Level: 100);
        Assert.DoesNotContain(Talents.GrimoireOf(bare, Real).Spells, id => Real.Spells[id].Element == Element.Ice);
        Assert.Equal(8, Talents.GrimoireOf(bare with { Compounds = [Element.Ice] }, Real).Spells.Count(id => Real.Spells[id].Element == Element.Ice));
        Assert.NotNull(Elements.Problem([Element.Ice], 29));
        // Sprint 187 (D94): at most two pairs, that share no element; each of the six alone.
        Assert.All(Elements.Compounds.Where(e => Elements.Parents(e).Count == 2), e => Assert.Null(Elements.Problem([e], 30)));
        Assert.Null(Elements.Problem([Element.Lightning, Element.Mud], 30));
        Assert.NotNull(Elements.Problem([Element.Lightning, Element.Ice], 30));
        Assert.NotNull((bare with { Compounds = [.. Elements.Compounds] }).Problem(Real));
    }
}
