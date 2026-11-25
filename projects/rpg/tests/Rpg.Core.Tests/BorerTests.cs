using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The rock worms (sprint 226, docs/familles-mines.md §4, words of docs/familles-mines-textes.md §3): the Underroot Mines' second
/// family, whose lesson is choosing one's target: a worm's hide closes at the start of its turn (+20 % resistances) and opens when
/// it strikes (−20 % until its next turn), the same status from the same source taking the other's place; the millstone worm
/// holds a falling ally once a turn; the tunnel worm swaps places with a hurt one; their chief splits below half its hit points.
/// </summary>
public class BorerTests
{
    private static MonsterFamily Worms => Real.Families["borer"];

    private static readonly Spell Blow = new("blow", Text, 1, 1, 9, false, false, 9999, 9999, 99);
    private static readonly Spell Venom = new("venom", Text, 1, 1, 9, false, false, 0, 0, 99, Element.Water,
        Effects: [new StatusEffect(Affects.Enemies, Stat.Poison, 20, 2, Element.Water)]);
    private static readonly Spell Wash = new("wash", Text, 1, 1, 9, false, false, 0, 0, 99, Effects: [new PurgeEffect(Affects.Enemies)]);

    /// <summary>Worms of the family on the row's B cells in order, against a hero who plays first and cannot fall.</summary>
    private static Fight Among(string row, params string[] monsters)
    {
        FighterSpec hero = Spec(0, hp: 99999, ap: 99, mp: 9, initiative: 999, spells: ["blow", "venom", "wash"]);
        var data = new GameData([.. Real.Spells.Values, Blow, Venom, Wash], [new MapSpec("m", Text, [row])],
            [new Scenario("s", Text, "m", [hero, .. monsters.Select((m, i) => Real.Monsters[m].Fighter(i, family: "borer"))])],
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values);
        return new Fight(data, "s", 1);
    }

    private static void Until(Fight fight, Fighter who)
    {
        while (fight.Current != who)
            fight.Apply(new EndTurnAction());
    }

    [Fact]
    public void TheWorms_LiveInTheMines_TheCommonOnesOf49To52_TheirChiefOf60_OnTheTwoMapsOfTheMines()
    {
        Assert.Equal(("underroot", "underroot-4-4"), (Worms.Zone, Worms.BossMap));
        Assert.Equal([49, 50, 51, 52], Worms.Monsters.Where(m => m.Role != MonsterRole.Boss).Select(m => m.Level).Order());
        Assert.Equal(60, Worms[MonsterRole.Boss].Level);
        Assert.Equal(["underroot-pillars", "underroot-gallery"], Worms.AllBattles);
        Assert.Contains(Real.Groups.Values, g => g.Family == "borer" && g.Map == Worms.BossMap && g.Monsters.Contains("mountain-gnawer"));
        // The millstone worm alone is slow: it stays among its own, where it holds them (§4.1).
        Assert.Equal(["millstone-worm"], Worms.Monsters.Where(m => m.Mp == 2).Select(m => m.Id));
        foreach (MonsterSpec m in Worms.Monsters)
        {
            Assert.Equal(Templates.Initiative(m.Role, m.Level), m.Initiative);
            Assert.Contains("rock-hide", m.Passives!);
        }
        // Each of their blows opens them; nothing else does (§4.2).
        foreach (Spell s in Worms.Monsters.SelectMany(m => m.Spells).Distinct().Select(s => Real.Spells[s]))
            Assert.Equal(s.DamageMax > 0, s.AllEffects.Contains(new StatusEffect(Affects.Caster, Stat.Resistance, -20, 2)));
    }

    [Fact]
    public void AWorm_ClosesAtItsTurnStart_OpensWhenItStrikes_UntilItsNextTurn()
    {
        Fight fight = Among("AB.........", "stonegnawer");
        Fighter hero = fight.Fighters[0], worm = fight.Fighters[1];
        // Earth 10, Water −15 on its sheet: 30 and 5 closed, −10 and −35 open (§4.3).
        Assert.Equal((10, -15), (worm.Resistance(Element.Earth), worm.Resistance(Element.Water)));
        Until(fight, worm);
        Assert.Equal((30, 5), (worm.Resistance(Element.Earth), worm.Resistance(Element.Water)));
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("stone-bite", hero.Cell)));
        Assert.Equal((-10, -35), (worm.Resistance(Element.Earth), worm.Resistance(Element.Water)));
        // One status, the other's place taken.
        Assert.Single(worm.Statuses, s => s.Stat == Stat.Resistance);
        Until(fight, hero);
        Assert.Equal((-10, -35), (worm.Resistance(Element.Earth), worm.Resistance(Element.Water)));
        Until(fight, worm);
        Assert.Equal((30, 5), (worm.Resistance(Element.Earth), worm.Resistance(Element.Water)));
    }

    [Fact]
    public void AWorm_ThatHealsOrDives_StaysClosed()
    {
        Fight fight = Among("A....B.B...", "tunnel-worm", "stonegnawer");
        Fighter tunnel = fight.Fighters[1], gnawer = fight.Fighters[2];
        Until(fight, tunnel);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("rock-slime", gnawer.Cell)));
        Assert.Equal(5, tunnel.Resistance(Element.Water));
        Until(fight, gnawer);
        Cell landing = new(gnawer.Cell.X - 3, gnawer.Cell.Y);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("burrow-dive", landing)));
        Assert.Equal((landing, 5), (gnawer.Cell, gnawer.Resistance(Element.Water)));
    }

    [Fact]
    public void APoison_StrikesAWormStillOpen_BeforeItsHideCloses()
    {
        Fight fight = Among("AB.........", "stonegnawer");
        Fighter hero = fight.Fighters[0], worm = fight.Fighters[1];
        Until(fight, worm);
        fight.Apply(new CastAction("stone-bite", hero.Cell));
        Until(fight, hero);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("venom", worm.Cell)));
        int from = fight.Events.Count;
        Until(fight, worm);
        FightEvent[] start = [.. fight.Events.Skip(from)];
        // 20 of Water against −35: 27, then the hide closes.
        int poisoned = Array.FindIndex(start, e => e is Damaged { Element: Element.Water } d && d.Fighter == worm.Id);
        int closed = Array.FindIndex(start, e => e is StatusAdded { Stat: Stat.Resistance, Value: 20 } s && s.Fighter == worm.Id);
        Assert.True(poisoned >= 0 && closed > poisoned);
        Assert.Equal(27, ((Damaged)start[poisoned]).Amount);
    }

    [Fact]
    public void APurge_TakesAClosedWormsHide_NotAnOpenOnesWound()
    {
        Fight fight = Among("AB.B.......", "stonegnawer", "grit-spitter");
        Fighter hero = fight.Fighters[0], gnawer = fight.Fighters[1], spitter = fight.Fighters[2];
        Until(fight, gnawer);
        // The spitter (initiative 189) played before the gnawer (157): closed, the gnawer open, at the hero's turn.
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("stone-bite", hero.Cell)));
        Until(fight, hero);
        Assert.Equal((-35, 5), (gnawer.Resistance(Element.Water), spitter.Resistance(Element.Water)));
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("wash", spitter.Cell)));
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("wash", gnawer.Cell)));
        Assert.Equal((-35, -15), (gnawer.Resistance(Element.Water), spitter.Resistance(Element.Water)));
    }

    [Fact]
    public void TheMillstoneWorm_HoldsOneFallingAllyATurn_WithinTwoCells_NotItself()
    {
        PassiveSpec coils = Real.Passives["coils"];
        Assert.Equal((Trigger.AllyFalls, 2, true), (coils.Trigger, coils.Range, coils.OncePerTurn));
        Assert.Equal(["millstone-worm", "mountain-gnawer"], Worms.Monsters.Where(m => m.Passives!.Contains("coils")).Select(m => m.Id));
        Fight fight = Among("AB.B...B...", "stonegnawer", "millstone-worm", "grit-spitter");
        Fighter gnawer = fight.Fighters[1], millstone = fight.Fighters[2], spitter = fight.Fighters[3];
        int Held() => fight.Events.Count(e => e is PassiveTriggered { Passive: "coils" } p && p.Fighter == millstone.Id);
        // Two cells from the millstone worm: held at 1 hit point, once; the second blow fells it.
        fight.Apply(new CastAction("blow", gnawer.Cell));
        Assert.Equal((1, 1), (gnawer.Hp, Held()));
        fight.Apply(new CastAction("blow", gnawer.Cell));
        Assert.False(gnawer.IsAlive);
        // Four cells away: not held. Nor the millstone worm itself.
        fight.Apply(new CastAction("blow", spitter.Cell));
        fight.Apply(new CastAction("blow", millstone.Cell));
        Assert.False(spitter.IsAlive || millstone.IsAlive);
        Assert.Equal(1, Held());
    }

    [Fact]
    public void TheMillstoneWorm_HoldsAgain_AfterItsOwnTurn()
    {
        Fight fight = Among("AB.B.......", "stonegnawer", "millstone-worm");
        Fighter hero = fight.Fighters[0], gnawer = fight.Fighters[1], millstone = fight.Fighters[2];
        fight.Apply(new CastAction("blow", gnawer.Cell));
        Until(fight, millstone);
        Until(fight, hero);
        fight.Apply(new CastAction("blow", gnawer.Cell));
        Assert.Equal(1, gnawer.Hp);
        Assert.Equal(2, fight.Events.Count(e => e is PassiveTriggered { Passive: "coils" }));
    }

    [Fact]
    public void TheTunnelWorm_SwapsOnlyWithAHurtAllyBesideAHero()
    {
        foreach (bool hurt in new[] { true, false })
        {
            Fight fight = Among("AB....B....", "stonegnawer", "tunnel-worm");
            Fighter gnawer = fight.Fighters[1], tunnel = fight.Fighters[2];
            // At 1 hit point: a Rock Slime alone would not lift it above half, and the AI heals what it can heal.
            if (hurt)
                gnawer.Hp = 1;
            Until(fight, tunnel);
            for (int actions = 0; fight.Current == tunnel && actions < 20; actions++)
                fight.Apply(Ai.Decide(fight));
            Assert.Equal(hurt, fight.Events.Any(e => e is Swapped s && s.A == tunnel.Id && s.B == gnawer.Id));
        }
    }

    [Fact]
    public void TheMountainGnawer_BelowHalf_StrikesHarder_AndTwoWormlingsCrawlOut()
    {
        PassiveSpec strata = Real.Passives["strata"];
        Assert.Equal((Trigger.LowHp, 50), (strata.Trigger, strata.Percent));
        Assert.Contains(strata.Effects!, e => e is StatusEffect { Stat: Stat.Damage, Value: 20, Turns: 10 });
        SummonEffect wormlings = Assert.Single(strata.Effects!.OfType<SummonEffect>());
        Assert.Equal(("wormling", 2), (wormlings.Summon, wormlings.Count));
        // No phase that lowers its resistances: its Rock Hide would take that status's place at each turn (§4.1).
        Assert.DoesNotContain(strata.Effects!, e => e is StatusEffect { Stat: Stat.Resistance });
        SummonSpec wormling = Real.Summons["wormling"];
        Assert.Equal(50, wormling.Hp);
        Assert.Equal(["stone-bite"], wormling.Spells);
        Assert.Null(wormling.Passives);

        var blow = new Spell("half", Text, 1, 1, 9, false, false, 450, 450, 99);
        FighterSpec hero = Spec(0, hp: 99999, ap: 99, initiative: 999, spells: ["half"]);
        var data = new GameData([.. Real.Spells.Values, blow], [new MapSpec("m", Text, ["A...B......"])],
            [new Scenario("s", Text, "m", [hero, Real.Monsters["mountain-gnawer"].Fighter(0, family: "borer")])],
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values);
        var fight = new Fight(data, "s", 1);
        Fighter chief = fight.Fighters[1];
        fight.Apply(new CastAction("half", chief.Cell));
        Assert.True(chief.IsAlive && chief.Hp * 2 < chief.MaxHp, $"{chief.Hp}/{chief.MaxHp}");
        Assert.Equal(2, fight.Events.OfType<Summoned>().Count(s => s.Summoner == chief.Id));
        Assert.True(chief.DamageBonusFor(Element.Earth) >= 20);
    }

    [Fact]
    public void TheSet_HasFourPieces_TheCapeWithInitiative_AShieldAndAOneHandedWeapon()
    {
        ItemSet set = Real.Sets["borer"];
        Assert.Equal([2, 3, 4], set.Bonuses.Select(b => b.Pieces));
        Assert.Equal((0, 0), (set.Bonuses[^1].Ap, set.Bonuses[^1].Mp));
        Assert.Equal([ItemSlot.Cape, ItemSlot.Amulet, ItemSlot.Shield, ItemSlot.OneHanded], Real.Items.Values.Where(i => i.Set == "borer").Select(i => i.Slot));
        foreach (MonsterSpec m in Worms.Monsters)
        {
            int drops = (m.Loot ?? []).Count(l => Real.Items[l.Item].Set == "borer");
            Assert.Equal(m.Role switch { MonsterRole.Boss => 4, MonsterRole.Support => 0, _ => 1 }, drops);
        }
        // The worms keep nothing of what they eat: no ore of the zone (§4.5); since sprint 227, their dungeon's key.
        Assert.All(Worms.Monsters.SelectMany(m => m.Loot ?? []), l => Assert.Contains(l.Item, (string[])["borer-tooth", "rock-slough", "mountains-belly-key", .. Real.Items.Values.Where(i => i.Set == "borer").Select(i => i.Id)]));
    }

    [Fact]
    public void AFightWithHidesAndCoils_ReplaysIdentically()
    {
        // Played by hand, not by the AI: the suite's budget of fights is full (D96, REVIEW.md, sprint 222).
        MonsterGroup group = Real.Groups.Values.Where(g => g.Family == "borer" && g.Monsters.Contains("millstone-worm") && g.Monsters.Count > 1)
            .OrderBy(g => g.Monsters.Count).ThenBy(g => g.Id, StringComparer.Ordinal).First();
        var fight = new Fight(Real, group.Scenario, 1, Progression.Built(new Hero("Essai", "female-d", "warrior", Level: 46), Real));
        for (int turns = 0; turns < 8 && !fight.IsOver; turns++)
            fight.Apply(new EndTurnAction());
        Assert.Contains(fight.Events, e => e is PassiveTriggered { Passive: "rock-hide" });
        Assert.Equal(Fingerprint(fight), Fingerprint(FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(Real)));
    }
}
