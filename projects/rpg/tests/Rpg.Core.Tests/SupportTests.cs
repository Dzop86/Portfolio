using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The branch Support (sprint 143, D80, T109, Charles's idea): a spell cast on an ally helps it by its element, one a
/// turn, never oneself; Double Hand, Banner, Shared Momentum, Long Hand, Devotion, Harmony, Relief; the AI that supports.
/// </summary>
public class SupportTests
{
    // 20 of damage, 4 AP, nine casts a turn, 1 to 4 cells: one per element, and Lightning.
    private static readonly Spell[] Mine = [.. new[] { Element.Earth, Element.Fire, Element.Water, Element.Air, Element.Lightning }
        .Select(e => new Spell(e.ToString().ToLowerInvariant(), Text, 4, 1, 4, false, false, 20, 20, 9, e))];
    private static readonly HeroClass Tester = new("tester", Text, Text, 400, 20, 4, 999, [.. Mine.Take(4).Select(s => s.Id)], HpPerLevel: 0);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, .. Mine.Take(4), Mine[4] with { Twin = "fire" }], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    private static Hero Hero(params string[] talents) => new("Ondine", "female-a", "tester", Level: 100, Talents: WithDoor(talents));

    private static readonly Hero Friend = new("Romane", "female-b", "tester", Level: 100);

    /// <summary>The hero at (0, 0), its friend at (2, 0) with <paramref name="friendHp"/>, a monster at (6, 0).</summary>
    private static Fight Play(Hero hero, int? friendHp = null, string row = "A.A...B") =>
        new(Data([row], Spec(0, initiative: 999, spells: ["strike"]), Spec(1, 0, hp: 300, initiative: 1, spells: ["strike"])), "s", 1,
            hero, allies: [Friend], alliesHp: [friendHp]);

    private static Cell C(int x, int y = 0) => new(x, y);

    [Theory]
    [InlineData("fire", 8, 0, 0, 0, 0)]
    [InlineData("earth", 0, 12, 0, 0, 0)]
    [InlineData("water", 0, 0, 20, 0, 0)]
    [InlineData("air", 0, 0, 0, 2, 1)]
    public void EachElement_HelpsTheAllyItsOwnWay(string spell, int heal, int shield, int resistance, int mp, int range)
    {
        // 20 of damage: Fire heals 40 % (8), Earth shields 60 % (12), Water 5 % a point (20), Air 2 MP and 1 range.
        Fight f = Play(Hero("helping-hand"), friendHp: 300);
        Fighter friend = f.Fighters[2];
        Assert.Equal(ActionError.None, f.Apply(new CastAction(spell, friend.Cell)));
        Assert.Contains(new Supported(0, 2), f.Events);
        Assert.Equal(300 + heal, friend.Hp);
        Assert.Equal(shield, friend.Shield);
        Assert.Equal(resistance, friend.Statuses.Where(s => s.Stat == Stat.Resistance).Sum(s => s.Value));
        Assert.Equal((mp, range), (friend.Statuses.Where(s => s.Stat == Stat.Mp).Sum(s => s.Value), friend.Statuses.Where(s => s.Stat == Stat.Range).Sum(s => s.Value)));
        Assert.DoesNotContain(f.Events, e => e is Damaged);
    }

    [Fact]
    public void TheForecast_OfASupport_TellsItsHeal_NotDamage()
    {
        Fight f = Play(Hero("helping-hand"), friendHp: 300);
        Forecast fc = Assert.Single(f.Foresee(f.HeroFighter!.Spells.First(s => s.Id == "fire"), C(2)));
        Assert.Equal((f.Fighters[2], 0, 8), (fc.Fighter, fc.DamageMax, fc.HealMax));
    }

    [Fact]
    public void APair_GivesBothItsParentsAtSixtyPercent_AndAHeal_AtMostFifteenPercentOfTheMaximum()
    {
        // Lightning: Fire and Air at 60 %: a heal of 4.8 (5), 1 MP (0.6 × 2) and 1 range (0.6).
        Assert.Equal(new SupportEffect(5, 0, 0, 1, 1), SupportEffect.Of(Element.Lightning, 20, 4, 400, 100));
        Assert.Equal(15, SupportEffect.Of(Element.Fire, 1000, 4, 100, 100).Heal);
    }

    [Fact]
    public void OneSupportATurn_NeverOneself_AndWithoutTheTalent_TheAllyIsHit()
    {
        Fight f = Play(Hero("helping-hand"), friendHp: 300);
        Fighter friend = f.Fighters[2];
        f.Apply(new CastAction("fire", friend.Cell));
        Assert.Equal(ActionError.SupportSpent, f.Check(new CastAction("water", friend.Cell)));
        Assert.Null(f.SupportTarget(f.HeroFighter!, f.HeroFighter!.Spells[0], f.HeroFighter.Cell));
        Fight plain = Play(Hero(), friendHp: 300);
        plain.Apply(new CastAction("fire", plain.Fighters[2].Cell));
        // 20, the trunk's 3 % on the caster and its 3 % of resistance on the ally (sprint 146): 19.
        Assert.Equal(281, plain.Fighters[2].Hp);
    }

    [Fact]
    public void DoubleHand_TwoSupportsAtSeventyFivePercent_Devotion_HalfAsMuchAgain()
    {
        Fight f = Play(Hero("helping-hand", "banner", "double-hand"), friendHp: 300);
        Fighter friend = f.Fighters[2];
        f.Apply(new CastAction("earth", friend.Cell));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("earth", friend.Cell)));
        // 12 × 0.75 = 9, twice.
        Assert.Equal(18, friend.Shield);
        Fight devoted = Play(Hero("helping-hand", "banner", "long-hand", "devotion"), friendHp: 300);
        devoted.Apply(new CastAction("earth", devoted.Fighters[2].Cell));
        Assert.Equal(18, devoted.Fighters[2].Shield);
    }

    [Fact]
    public void Banner_SharedMomentum_AndLongHand()
    {
        Fight f = Play(Hero("helping-hand", "banner", "shared-momentum", "long-hand"), friendHp: 300);
        Fighter me = f.HeroFighter!, friend = f.Fighters[2];
        // Banner at the start of the turn: the friend 2 cells away, +5 %.
        Assert.Contains(friend.Statuses, s => s.Stat == Stat.Damage && s.Value == 5);
        f.Apply(new CastAction("water", friend.Cell));
        Assert.Contains(friend.Statuses, s => s.Stat == Stat.Damage && s.Value == 10);
        Assert.Contains(me.Statuses, s => s.Stat == Stat.Damage && s.Value == -10);
        // Long Hand: an ally 6 cells away within reach (4 + 2), an enemy 6 cells away not (4 − 1).
        Fight far = Play(Hero("helping-hand", "banner", "long-hand", "shared-momentum"), friendHp: 300, row: "A.....A......B");
        Assert.Equal(ActionError.None, far.Check(new CastAction("fire", C(6))));
    }

    [Fact]
    public void Harmony_AddsTheElementsResistance_ForAnActionPointMore()
    {
        Fight f = Play(Hero("helping-hand", "banner", "long-hand", "harmony"), friendHp: 300);
        Fighter me = f.HeroFighter!, friend = f.Fighters[2];
        int ap = me.Ap;
        f.Apply(new CastAction("fire", friend.Cell));
        Assert.Equal(ap - 5, me.Ap);
        Assert.Contains(friend.Statuses, s => s.Stat == Stat.Resistance && s.Element == Element.Fire && s.Value == 15);
    }

    [Fact]
    public void Relief_DoublesASupportOnAnAllyUnderAQuarter_ForFree_OnceAFight()
    {
        Fight f = Play(Hero("helping-hand", "banner", "long-hand", "relief"), friendHp: 50);
        Fighter me = f.HeroFighter!, friend = f.Fighters[2];
        int ap = me.Ap;
        f.Apply(new CastAction("earth", friend.Cell));
        Assert.Equal((ap, 24), (me.Ap, friend.Shield));
        f.Apply(new EndTurnAction());
        while (f.Current != me)
            f.Apply(new EndTurnAction());
        // 2 AP less the next turn; Relief is spent.
        Assert.Equal(20 - 2, me.Ap);
        f.Apply(new CastAction("earth", friend.Cell));
        Assert.Equal(20 - 2 - 4, me.Ap);
    }

    [Fact]
    public void TheAI_HealsAHurtAlly_RatherThanStrikingAFarMonster()
    {
        Fight f = Play(Hero("helping-hand"), friendHp: 100, row: "A.A.......B");
        Assert.Equal(new CastAction("fire", C(2)), Ai.Decide(f));
    }
}
