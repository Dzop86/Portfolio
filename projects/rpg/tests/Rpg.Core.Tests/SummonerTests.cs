using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Summoner (sprint 211, docs/classes-invocateur.md): its order and its creatures' AI on small boards; its kit, its fed creatures and the three
/// creatures a hero at most on the real data.
/// </summary>
public class SummonerTests
{
    private static readonly Spell Fang = new("fang", Text, 3, 1, 1, true, false, 10, 10, 9);
    private static readonly Spell Call = new("call", Text, 1, 1, 1, false, false, 0, 0, 9, Effects: [new SummonEffect(Affects.All, "pet", Max: 4)]);
    private static readonly Spell Order = new("order", Text, 1, 1, 9, false, false, 0, 0, 9, Effects: [new StatusEffect(Affects.Enemies, Stat.Ordered, 25, 2)]);
    private static readonly SummonSpec Pet = new("pet", Text, "male-d", 50, 6, 2, 50, ["fang"]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Fang, Call, Order], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], summons: [Pet]), "s", 1);

    private static FighterSpec Caller() => Spec(0, ap: 99, mp: 0, initiative: 999, spells: ["call", "order"]);

    private static FighterSpec Foe(int start) => Spec(1, start, hp: 900, initiative: 1);

    /// <summary>The caller's creature on <paramref name="cell"/>, playing next.</summary>
    private static Fighter Raise(Fight f, Cell cell)
    {
        Assert.Equal(ActionError.None, f.Apply(new CastAction("call", cell)));
        return f.Fighters[^1];
    }

    [Fact]
    public void AnOrder_MakesTheCreaturesOfItsCaster_StrikeTheBearerAQuarterHarder()
    {
        int Left(bool ordered)
        {
            Fight f = Play(["A.B"], Caller(), Foe(0));
            Fighter foe = f.Fighters[1];
            Fighter pet = Raise(f, new Cell(1, 0));
            if (ordered)
                Assert.Equal(ActionError.None, f.Apply(new CastAction("order", foe.Cell)));
            f.Apply(new EndTurnAction());
            Assert.Same(pet, f.Current);
            Assert.Equal(ActionError.None, f.Apply(new CastAction("fang", foe.Cell)));
            return foe.Hp;
        }
        Assert.Equal(890, Left(ordered: false));
        Assert.Equal(888, Left(ordered: true));
    }

    [Fact]
    public void OnlyItsCreatures_StrikeHarder_AndTheCasterItselfDoesNot()
    {
        Fight f = Play(["A.B"], Caller(), Foe(0));
        Fighter foe = f.Fighters[1];
        Fighter pet = Raise(f, new Cell(1, 0));
        f.Apply(new CastAction("order", foe.Cell));
        Assert.Equal(25, Fight.OrderOn(pet, foe));
        Assert.Equal(0, Fight.OrderOn(f.Fighters[0], foe));
        Assert.Equal(0, Fight.OrderOn(foe, foe));
        // The AI's estimate reads it too.
        Assert.Equal(1.25 * Fight.Expected(10, f.Fighters[0], foe, Element.Neutral), Fight.Expected(10, pet, foe, Element.Neutral), 9);
    }

    [Fact]
    public void OneEnemyOrdered_ByEachCaster_AtATime()
    {
        Fight f = Play(["A..B", "...B"], Caller(), Foe(0), Foe(1));
        Raise(f, new Cell(1, 0));
        f.Apply(new CastAction("order", f.Fighters[1].Cell));
        f.Apply(new CastAction("order", f.Fighters[2].Cell));
        Assert.DoesNotContain(f.Fighters[1].Statuses, s => s.Stat == Stat.Ordered);
        Assert.Contains(f.Fighters[2].Statuses, s => s is { Stat: Stat.Ordered, Value: 25, Source: 0 });
    }

    [Fact]
    public void ACreature_StrikesTheEnemyOrdered_RatherThanTheOneListedFirst()
    {
        FighterSpec Decided(bool ordered)
        {
            Fight f = Play(["B.B", ".A.", "..."], Caller(), Foe(0), Foe(1));
            Fighter pet = Raise(f, new Cell(1, 0));
            if (ordered)
                f.Apply(new CastAction("order", f.Fighters[2].Cell));
            f.Apply(new EndTurnAction());
            Assert.Same(pet, f.Current);
            CastAction cast = Assert.IsType<CastAction>(Ai.Decide(f));
            return f.At(cast.Target)!.Spec;
        }
        // Alike, the one listed first; ordered, the other.
        Assert.Equal("f10", Decided(ordered: false).Name.En);
        Assert.Equal("f11", Decided(ordered: true).Name.En);
    }

    [Fact]
    public void ACreature_WalksToTheEnemyOrdered_RatherThanTheNearest()
    {
        Cell Walked(bool ordered)
        {
            // The pet raised above its caster, 5 steps from the one on the left, 6 from the other.
            Fight f = Play(["B..........B", ".....A......"], Caller(), Foe(0), Foe(1));
            Fighter pet = Raise(f, new Cell(5, 0));
            if (ordered)
                f.Apply(new CastAction("order", f.Fighters[2].Cell));
            f.Apply(new EndTurnAction());
            Assert.Same(pet, f.Current);
            return Assert.IsType<MoveAction>(Ai.Decide(f)).To;
        }
        Assert.True(Walked(ordered: false).X < 5);
        Assert.True(Walked(ordered: true).X > 5);
    }

    [Fact]
    public void TheOrder_IsWorthWhatItsCreaturesInReachWouldAdd_NothingWithoutOne()
    {
        Fight f = Play(["A.B"], Caller(), Foe(0));
        Fighter me = f.Fighters[0];
        Fighter foe = f.Fighters[1];
        Assert.Equal(0, Ai.Value(f, me, Order, me.Cell, foe.Cell));
        Raise(f, new Cell(1, 0));
        // Three fangs of 10 a round (6 action points, 3 each, at most 9 a turn: 2), a quarter of them, over 2 turns.
        Assert.Equal(0.25 * 20 * 2, Ai.Value(f, me, Order, me.Cell, foe.Cell), 9);
        // Renewed on the same enemy, it adds nothing.
        f.Apply(new CastAction("order", foe.Cell));
        Assert.Equal(0, Ai.Value(f, me, Order, me.Cell, foe.Cell));
    }

    private static readonly string[] Creatures = ["hound", "wisp", "colossus"];

    private static Hero Summoner(int level, params string[] deck) =>
        new("Ondine", "female-c", "summoner", Level: level, Stats: new Characteristics(Earth: 100), Deck: deck.Length == 0 ? null : deck);

    /// <summary>A training fight on the real data, the hero to play with all the points it needs.</summary>
    private static Fight Training(Hero hero)
    {
        Fight f = new(Real, "training", 1, hero);
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        f.HeroFighter!.Ap = 99;
        return f;
    }

    private static ActionError Raise(Fight f, string spell)
    {
        Fighter me = f.HeroFighter!;
        me.Cooldowns.Clear();
        me.CastsThisTurn.Clear();
        Cell free = me.Cell.Neighbours().Concat(me.Cell.Neighbours().SelectMany(c => c.Neighbours())).First(c => f.IsFree(c) && f.CheckCast(me, f.Data.Spells[spell], me.Cell, c) is ActionError.None or ActionError.TooManySummons);
        return f.Apply(new CastAction(spell, free));
    }

    [Fact]
    public void ItsCreatures_AreFedByItsHitPointsAndCharacteristics_AndStrikeInItsElement()
    {
        Fight f = Training(Summoner(60, "call-hound", "call-wisp", "call-colossus"));
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, Raise(f, "call-hound"));
        Fighter hound = f.Fighters[^1];
        Assert.Equal((me.MaxHp * 20 / 100, me.Spec.Characteristics.Earth * 30 / 100), (hound.MaxHp, hound.Spec.Characteristics.Earth));
        Assert.Equal(Element.Earth, hound.Spells.Single().Element);
        Assert.Equal(ActionError.None, Raise(f, "call-colossus"));
        Assert.Equal(me.MaxHp * 40 / 100, f.Fighters[^1].MaxHp);
    }

    [Fact]
    public void TwoHounds_ThenThreeCreaturesAHero_AtMost()
    {
        Fight f = Training(Summoner(60, "call-hound", "call-wisp", "call-colossus"));
        Assert.Equal(ActionError.None, Raise(f, "call-hound"));
        Assert.Equal(ActionError.None, Raise(f, "call-hound"));
        Assert.Equal(ActionError.TooManySummons, Raise(f, "call-hound"));
        Assert.Equal(ActionError.None, Raise(f, "call-wisp"));
        // A Colossus of its own kind, but a fourth creature.
        Assert.Equal(ActionError.TooManySummons, Raise(f, "call-colossus"));
        Assert.Equal(3, f.Fighters.Count(c => c.IsAlive && c.Summoner == f.HeroFighter!.Id));
    }

    [Fact]
    public void TheSummonersSheet_AndItsKit()
    {
        HeroClass s = Real.Class("summoner")!;
        Assert.Equal((54, 3, 8, 4, 10), (s.Hp, s.HpPerLevel, s.Ap, s.Mp, s.Initiative));
        Assert.Equal([new SummonRule("hound", 20, 30), new SummonRule("wisp", 12, 30), new SummonRule("colossus", 40, 30)], s.Rules);
        Spell[] kit = [.. s.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(k => k.Level));
        // Two creatures from level 1, the order at 5.
        Assert.Equal((1, 2), (Real.Spells["call-hound"].Level, Real.Spells["call-hound"].AllEffects.OfType<SummonEffect>().Single().Max));
        Assert.Equal(5, Real.Spells["command"].Level);
        Assert.Contains(Real.Spells["command"].AllEffects, e => e is StatusEffect { Stat: Stat.Ordered, Affects: Affects.Enemies, Value: 25, Turns: 2 });
        // What sets it apart from the Priest (classes-conception.md §1.4): no heal; its blows within 1 to 6 cells.
        Assert.DoesNotContain(kit, k => k.AllEffects.Any(e => e is HealEffect or ReviveEffect));
        Assert.All(kit.Where(k => k.DamageMax > 0), k => Assert.InRange(k.MaxRange, 4, 6));
        // At 0.75 of the budget: the Clod against the Mage's Pebble (1), 3 action points from 1 to 5 cells.
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.InRange(Average("clod") / Average("pebble"), 0.7, 0.82);
        // Its creatures strike in its strongest element.
        Assert.All(Creatures, c => Assert.All(Real.Summons[c].Spells, id => Assert.True(Real.Spells[id].Adaptive, id)));
    }
}
