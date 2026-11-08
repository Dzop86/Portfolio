using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Priest (sprint 201, docs/classes-pretre.md): its three bricks on small boards (a heal's excess as a shield, healing over time, the purge), then
/// its kit, the Mage's new neutral spells, and what the AI makes of them.
/// </summary>
public class PriestTests
{
    // Fixed numbers where a test counts hit points.
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 15, 15, Overflow: true)]);
    private static readonly Spell Plain = new("plain", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 15, 15)]);
    private static readonly Spell Prayer = new("prayer", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Allies, Stat.Regen, 6, 2)]);
    private static readonly Spell Hurt = new("hurt", Text, 1, 0, 6, false, false, 20, 20, 9, Element.Neutral);
    private static readonly Spell Venom = new("venom", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null,
        [new StatusEffect(Affects.All, Stat.Poison, 4, 3, Element.Water), new StatusEffect(Affects.All, Stat.Ap, -2, 2)]);
    private static readonly Spell Rage = new("rage", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null,
        [new StatusEffect(Affects.All, Stat.Damage, 50, 2), new ShieldEffect(Affects.All, 12, 2)]);
    private static readonly Spell Purge = new("purge", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PurgeEffect(Affects.All)]);
    private static readonly Spell Parch = new("parch", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.All, Stat.Parched, 1, 2)]);
    private static readonly Spell[] All = [.. Spells, Mend, Plain, Prayer, Hurt, Venom, Rage, Purge, Parch];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(params string[] spells) => Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells);

    // ---- The class's engine: a heal's excess becomes a shield (§1) ----

    [Fact]
    public void AHealsExcess_BecomesAShieldOfTwoTurns_NeverMoreThanTheRoll()
    {
        // Struck for 20, the ally keeps 48 of its hit points at most (the erosion): healed to the top, the rest as a shield.
        Fight f = Play(["A.A.B"], Caster("hurt", "mend", "plain"), Spec(0, 1, hp: 50, initiative: 1), Spec(1, initiative: 0));
        Fighter ally = f.Fighters[1];
        f.Apply(new CastAction("hurt", ally.Cell));
        Assert.Equal(30, ally.Hp);
        f.Apply(new CastAction("mend", ally.Cell));
        Assert.Equal((45, 0), (ally.Hp, ally.Shield));
        Assert.Equal(48, ally.MaxHp);
        f.Apply(new CastAction("mend", ally.Cell));
        Assert.Equal((48, 12), (ally.Hp, ally.Shield));
        Assert.Contains(f.Events.OfType<Shielded>(), e => e is { Fighter: 1, Amount: 12, Turns: 2 });
        // At full hit points, the whole roll; a heal without the flag leaves nothing.
        f.Apply(new CastAction("mend", ally.Cell));
        Assert.Equal(27, ally.Shield);
        f.Apply(new CastAction("plain", ally.Cell));
        Assert.Equal(27, ally.Shield);
    }

    [Fact]
    public void APriestsHeal_GrowsWithItsStrongestElement_AsItsBlowsDo()
    {
        // The flat bonus of a hit: 2 a ten points of the strongest element, its damage spells' (D61), under the threshold of its level.
        Hero priest = new("Ondine", "female-c", "priest", Level: 30, Stats: new Characteristics(Fire: 80, Water: 40), Deck: ["mend"]);
        Fight f = new(Real, "training", 1, priest);
        Assert.Equal(16, Fight.HealBonus(f.HeroFighter!));
        // And its dice with its level, as a blow's ranks raise it (sprint 203): 10 at level 30, 13, then the bonus.
        HealEffect mend = Real.Spells["mend"].AllEffects.OfType<HealEffect>().Single();
        Assert.Equal(13 + 16, Fight.Grown(mend, f.HeroFighter!, 10));
        Assert.Equal(10, Fight.Grown(mend with { Grows = false }, f.HeroFighter!, 10));
        Assert.All(Real.Class("priest")!.Spells.SelectMany(id => Real.Spells[id].AllEffects).OfType<HealEffect>(), h => Assert.True(h.Grows));
        // Nobody else's heals grow (its spells' twins, a compound's, and its paths' are its own).
        string[] paths = [.. Real.Specs.Values.Where(s => s.Class == "priest").SelectMany(s => s.Forks.SelectMany(f => s.SpellsWith(f.Id)))];
        Assert.All(Real.Spells.Values.Where(s => !Real.Class("priest")!.Spells.Contains(s.Twin ?? s.Id) && !paths.Contains(s.Id)).SelectMany(s => s.AllEffects).OfType<HealEffect>(), h => Assert.False(h.Grows));
    }

    [Fact]
    public void AParchedFighter_GetsNeitherTheHealNorItsShield()
    {
        Fight f = Play(["A.A.B"], Caster("parch", "mend"), Spec(0, 1, hp: 50, initiative: 1), Spec(1, initiative: 0));
        Fighter ally = f.Fighters[1];
        f.Apply(new CastAction("parch", ally.Cell));
        f.Apply(new CastAction("mend", ally.Cell));
        Assert.Equal((50, 0), (ally.Hp, ally.Shield));
    }

    // ---- Healing over time (§2) ----

    [Fact]
    public void Regen_HealsAtTheStartOfEachOfItsTurns_ThenEnds()
    {
        Fight f = Play(["A.A..B"], Caster("hurt", "prayer"), Spec(0, 1, hp: 50, ap: 0, mp: 0, initiative: 10), Spec(1, hp: 900, ap: 0, mp: 0, initiative: 1));
        Fighter ally = f.Fighters[1];
        f.Apply(new CastAction("hurt", ally.Cell));
        f.Apply(new CastAction("prayer", ally.Cell));
        Assert.Equal(30, ally.Hp);
        f.Apply(new EndTurnAction());
        Assert.Equal((ally, 36), (f.Current, ally.Hp));
        Assert.Contains(f.Events.OfType<Healed>(), e => e is { Fighter: 1, Amount: 6 });
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal((ally, 42), (f.Current, ally.Hp));
        // Two turns: over.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal((ally, 42), (f.Current, ally.Hp));
        Assert.DoesNotContain(ally.Statuses, s => s.Stat == Stat.Regen);
    }

    [Fact]
    public void Regen_IsData_ItsValuePositive()
    {
        Spell bad = Prayer with { Id = "bad", Effects = [new StatusEffect(Affects.Allies, Stat.Regen, -6, 2)] };
        Assert.Throws<InvalidDataException>(() => new GameData([.. All, bad], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])]));
    }

    // ---- The purge (§2) ----

    [Fact]
    public void ThePurge_TakesAnAllysAilments_AndLeavesItsBoons()
    {
        Fight f = Play(["A.A.B"], Caster("venom", "rage", "purge"), Spec(0, 1, hp: 50, initiative: 1), Spec(1, initiative: 0));
        Fighter ally = f.Fighters[1];
        f.Apply(new CastAction("venom", ally.Cell));
        f.Apply(new CastAction("rage", ally.Cell));
        f.Apply(new CastAction("purge", ally.Cell));
        Assert.Equal([Stat.Damage], ally.Statuses.Select(s => s.Stat));
        Assert.Equal(12, ally.Shield);
        Assert.Contains(f.Events.OfType<StatusEnded>(), e => e is { Fighter: 1, Stat: Stat.Poison });
    }

    [Fact]
    public void ThePurge_TakesAnEnemysBoonsAndShields_AndLeavesItsAilments()
    {
        Fight f = Play(["A.B"], Caster("venom", "rage", "purge"), Spec(1, hp: 50, initiative: 1));
        Fighter enemy = f.Fighters[1];
        f.Apply(new CastAction("venom", enemy.Cell));
        f.Apply(new CastAction("rage", enemy.Cell));
        f.Apply(new CastAction("purge", enemy.Cell));
        Assert.Equal([Stat.Poison, Stat.Ap], enemy.Statuses.Select(s => s.Stat));
        Assert.Equal(0, enemy.Shield);
        Assert.Contains(f.Events.OfType<ShieldAbsorbed>(), e => e is { Fighter: 1, Amount: 12, ShieldLeft: 0 });
    }

    // ---- What the AI makes of them ----

    [Fact]
    public void TheAi_PurgesAPoisonedAlly_AndNotAHealthyOne()
    {
        static Fight Ready() => Play(["A.A.B"], Caster("venom"), Spec(0, 1, hp: 50, ap: 4, mp: 0, initiative: 500, spells: ["purge"]), Spec(1, hp: 50, ap: 0, mp: 0, initiative: 1));
        // Nothing to take: worth nothing (the AI may still cast what is worth nothing, when nothing else is worth more).
        Fight healthy = Ready();
        healthy.Apply(new EndTurnAction());
        Fighter whole = healthy.Fighters[1];
        Assert.Equal(0, Ai.Value(healthy, whole, whole.Spells.Single(s => s.Id == "purge"), whole.Cell, whole.Cell));
        Fight sick = Ready();
        Fighter ally = sick.Fighters[1];
        sick.Apply(new CastAction("venom", ally.Cell));
        sick.Apply(new EndTurnAction());
        Assert.Equal(new CastAction("purge", ally.Cell), Ai.Decide(sick));
    }

    [Fact]
    public void TheAi_PraysOverItselfHurt_AndNotWhole()
    {
        static Fight Ready() => Play(["A.A.B"], Caster("hurt"), Spec(0, 1, hp: 50, ap: 2, mp: 0, initiative: 500, spells: ["prayer"]), Spec(1, hp: 50, ap: 0, mp: 0, initiative: 1));
        Fight whole = Ready();
        whole.Apply(new EndTurnAction());
        Fighter me = whole.Fighters[1];
        Assert.Equal(0, Ai.Value(whole, me, me.Spells.Single(s => s.Id == "prayer"), me.Cell, me.Cell));
        Fight hurt = Ready();
        Fighter ally = hurt.Fighters[1];
        hurt.Apply(new CastAction("hurt", ally.Cell));
        hurt.Apply(new EndTurnAction());
        Assert.Equal(new CastAction("prayer", ally.Cell), Ai.Decide(hurt));
    }

    // ---- The kit (§3) and the Mage's (§4), on the real data ----

    private static readonly string[] Heals = ["mend", "renewal", "sanctuary", "grace"];
    private static readonly string[] MageSpells = ["blink", "slow", "mana-barrier", "siphon"];

    [Fact]
    public void ThePriest_HasItsCard_AndTwentyFourSpells_FourOfEachElement()
    {
        HeroClass priest = Real.Class("priest")!;
        Assert.Equal((66, 3, 8, 4, 10), (priest.Hp, priest.HpPerLevel, priest.Ap, priest.Mp, priest.Initiative));
        Spell[] kit = [.. priest.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        foreach (Element e in new[] { Element.Earth, Element.Fire, Element.Water, Element.Air })
            Assert.Equal(4, kit.Count(s => s.Element == e));
        Assert.Equal(8, kit.Count(s => s.Element == Element.Neutral));
        Assert.Subset(priest.Spells.ToHashSet(), Heals.ToHashSet());
    }

    [Fact]
    public void ThePriest_NeverStrikesFromNextToItsTarget_AndEveryHealOfItsOverflows()
    {
        Spell[] kit = [.. Real.Class("priest")!.Spells.Select(id => Real.Spells[id])];
        Assert.All(kit.Where(s => s.DamageMax > 0), s => Assert.True(s.MinRange >= 2, s.Id));
        HealEffect[] heals = [.. kit.SelectMany(s => s.AllEffects).OfType<HealEffect>()];
        Assert.Equal(5, heals.Length);
        Assert.All(heals, h => Assert.True(h.Overflow));
        // Prayer heals over time, Purify purges.
        Assert.Contains(Real.Spells["prayer"].AllEffects, e => e is StatusEffect { Stat: Stat.Regen, Value: > 0 });
        Assert.Contains(Real.Spells["purify"].AllEffects, e => e is PurgeEffect { Affects: Affects.Allies });
    }

    [Fact]
    public void ThePriest_StrikesAtEightTenthsOfTheBudget()
    {
        // Blessed Stone and the Mage's Pebble: the same cost, level and reach of 5 cells; rounded to whole points, small numbers stray a little.
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.InRange(Average("blessed-stone") / Average("pebble"), 0.75, 0.88);
        Assert.InRange(Average("lightning-holy-flame") / Average("lightning-flare"), 0.75, 0.88);
        // On every rank of the Priest's damaging spells, against what the budget gives a class of 1.
        Spell[] kit = [.. Real.Class("priest")!.Spells.Select(id => Real.Spells[id]).Where(s => s.DamageMax > 0)];
        Spell[] mages = [.. Real.Class("mage")!.Spells.Select(id => Real.Spells[id]).Where(s => s.DamageMax > 0)];
        Assert.InRange(kit.Average(s => (s.DamageMin + s.DamageMax) / 2.0 / s.ApCost) / mages.Average(s => (s.DamageMin + s.DamageMax) / 2.0 / s.ApCost), 0.7, 0.9);
    }

    [Fact]
    public void TheHeals_LeftTheMage_ForFourNewSpellsAtTheirLevels()
    {
        HeroClass mage = Real.Class("mage")!;
        Assert.Empty(mage.Spells.Intersect(Heals));
        Assert.DoesNotContain(Real.Spells.Keys, id => id == "rebirth");
        Assert.Equal([1, 55, 85, 90], MageSpells.Select(id => Real.Spells[id].Level));
        Assert.All(MageSpells, id => Assert.Contains(id, mage.Spells));
        // Classes share no spell.
        Assert.Equal(Real.Classes.Sum(c => c.Spells.Count), Real.Classes.SelectMany(c => c.Spells).Distinct().Count());
    }

    private static GameData Board(string[] rows, params FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    [Fact]
    public void TheMagesSiphon_TakesTwoPointsFromTheEnemy_AndGivesThemToItAtOnce()
    {
        Hero mage = new("Ondine", "female-c", "mage", Level: 90, Deck: ["siphon"]);
        Fight f = new(Board(["A..B"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 1, mage);
        Fighter me = f.HeroFighter!, enemy = f.Fighters[1];
        int ap = me.Ap;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("siphon", enemy.Cell)));
        Assert.Equal(ap, me.Ap);
        Assert.Contains(enemy.Statuses, s => s is { Stat: Stat.Ap, Value: -2 });
    }

    [Fact]
    public void APriest_HealsItsHurtFriend_RatherThanStrike()
    {
        // A Priest of 20 and a Warrior struck down to a third of its hit points, a monster in the Priest's reach.
        Hero priest = Progression.Built(new Hero("Ondine", "female-c", "priest", Level: 20), Real);
        Hero warrior = Progression.Built(new Hero("Bastien", "male-c", "warrior", Level: 20), Real);
        Fight f = new(Board(["A.A..B"], Spec(0, initiative: 9999), Spec(1, hp: 900, mp: 0, initiative: 1)), "s", 1, priest, allies: [warrior]);
        Fighter me = f.HeroFighter!, friend = f.HeroFighters[1];
        friend.Hp = friend.MaxHp / 3;
        while (f.Current != me)
            f.Apply(new EndTurnAction());
        Assert.True(Ai.Decide(f) is CastAction { Spell: "mend" or "prayer" } c && c.Target == friend.Cell, $"{Ai.Decide(f)}");
    }
}
