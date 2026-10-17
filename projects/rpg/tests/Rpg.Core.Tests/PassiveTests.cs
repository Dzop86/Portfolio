using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The monsters' passives, leaps and swaps (sprint 90, D69), and the action and movement points a status
/// gives the fighter playing at once; on small boards written here.
/// </summary>
public class PassiveTests
{
    // Fixed numbers where a test counts hit points; nine casts a turn, so that one fighter can do it all.
    private static readonly Spell Hit = new("hit", Text, 1, 1, 6, false, false, 10, 10, 9);
    private static readonly Spell Venom = new("venom", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2)]);
    private static readonly Spell Shove = new("shove", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PushEffect(Affects.Enemies, 2)]);
    private static readonly Spell Cure = new("cure", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.All, 100, 100)]);
    private static readonly Spell Dodge = new("dodge", Text, 1, 0, 0, false, false, 0, 0, 1, Element.Neutral, null, [new StatusEffect(Affects.Caster, Stat.Mp, 2, 1)]);
    private static readonly Spell Slow = new("slow", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Mp, -1, 1)]);
    private static readonly Spell Jump = new("jump", Text, 2, 2, 4, false, false, 0, 0, 1, Element.Neutral, null, [new LeapEffect(Affects.Caster)]);
    private static readonly Spell Trade = new("trade", Text, 2, 1, 4, false, false, 0, 0, 1, Element.Neutral, null, [new SwapEffect(Affects.Allies)]);
    private static readonly Spell[] All = [.. Spells, Hit, Venom, Shove, Cure, Dodge, Slow, Jump, Trade];

    private static PassiveSpec Passive(string id, Trigger trigger, params SpellEffect[] effects) =>
        new(id, Text, Text, trigger, effects);

    private static readonly PassiveSpec Mend = Passive("mend", Trigger.TurnStart, new HealEffect(Affects.Caster, 4, 4));
    private static readonly PassiveSpec Struck = Passive("struck", Trigger.Hit, new ShieldEffect(Affects.Caster, 1, 1));
    private static readonly PassiveSpec Thorns = Passive("thorns", Trigger.Hit, new StatusEffect(Affects.Attacker, Stat.Mp, -1, 1)) with { Melee = true };
    private static readonly PassiveSpec Low = Passive("low", Trigger.LowHp, new StatusEffect(Affects.Caster, Stat.Ap, 1, 3)) with { Percent = 50 };
    private static readonly PassiveSpec Grudge = Passive("grudge", Trigger.AllyDies, new StatusEffect(Affects.Caster, Stat.Damage, 15, 2)) with { Range = 3 };
    private static readonly PassiveSpec Boom = Passive("boom", Trigger.Death) with { Area = new Area(AreaShape.Circle, 1), DamageMin = 10, DamageMax = 10 };
    private static readonly PassiveSpec Aura = Passive("aura", Trigger.TurnStart, new StatusEffect(Affects.Allies, Stat.Damage, 10, 3)) with { Area = new Area(AreaShape.Circle, 2) };
    private static readonly PassiveSpec Pups = Passive("pups", Trigger.LowHp, new SummonEffect(Affects.All, "pup", Count: 2)) with { Percent = 50 };
    private static readonly PassiveSpec[] Passives = [Mend, Struck, Thorns, Low, Grudge, Boom, Aura, Pups];
    private static readonly SummonSpec Pup = new("pup", Text, "wolf-melee", 10, 6, 3, 50, ["strike"], Scale: 70);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], summons: [Pup], passives: Passives);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) => new(Data(rows, fighters), "s", 1);

    /// <summary>Team A's caster: plays first, every point it needs, never walks unless asked.</summary>
    private static FighterSpec Caster(int mp = 0, params string[] spells) => Spec(0, ap: 99, mp: mp, initiative: 999, spells: spells.Length == 0 ? ["hit", "venom", "shove", "cure", "slow"] : spells);

    private static FighterSpec Monster(int start, int hp = 50, int initiative = 100, string? family = null, params string[] passives) =>
        Spec(1, start, hp: hp, initiative: initiative) with { Passives = passives, Family = family };

    private static int[] Fired(Fight f, string passive) => [.. f.Events.OfType<PassiveTriggered>().Where(p => p.Passive == passive).Select(p => p.Fighter)];

    [Fact]
    public void TurnStart_FiresAtTheStartOfTheBearersTurn_AfterItsPoisons()
    {
        Fight f = Play(["AB"], Caster(), Monster(0, passives: "mend"));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        f.Apply(new CastAction("venom", new Cell(1, 0)));
        Assert.Empty(Fired(f, "mend"));
        f.Apply(new EndTurnAction());
        // Its turn: the poison strikes (40 to 34), then the passive heals 4.
        int poison = f.Events.ToList().FindIndex(e => e is Damaged { Fighter: 1, Amount: 6 });
        int mend = f.Events.ToList().FindIndex(e => e is PassiveTriggered { Fighter: 1 });
        Assert.True(poison >= 0 && mend > poison);
        Assert.IsType<Healed>(f.Events[mend + 1]);
        Assert.Equal(38, f.Fighters[1].Hp);
        Assert.Equal([1], Fired(f, "mend"));
    }

    [Fact]
    public void TurnStart_FiresForTheFirstFighter_AsTheFightStarts()
    {
        Fight f = Play(["AB"], Spec(0, initiative: 1), Monster(0, initiative: 999, passives: "mend"));
        Assert.Equal([1], Fired(f, "mend"));
    }

    [Fact]
    public void Hit_FiresOncePerSpell_NeverForAPoisonACollisionOrAPassive()
    {
        // The bearer stands against the wall, so that a push of 2 cells collides; a bloater next to it.
        Fight f = Play(["A.BB#", "....."], Caster(), Monster(1, hp: 99, passives: "struck"), Monster(0, hp: 10, passives: "boom"));
        f.Apply(new CastAction("hit", new Cell(3, 0)));
        Assert.Equal([1], Fired(f, "struck"));
        f.Apply(new CastAction("venom", new Cell(3, 0)));
        f.Apply(new CastAction("shove", new Cell(3, 0)));
        Assert.Contains(f.Events, e => e is Pushed { Fighter: 1, Blocked: 2 });
        // The neighbour explodes next to it: passive damage.
        f.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.Equal([2], Fired(f, "boom"));
        int boom = f.Events.ToList().FindIndex(x => x is PassiveTriggered { Passive: "boom" });
        Assert.IsType<Damaged>(f.Events[boom + 1]);
        f.Apply(new EndTurnAction());
        // Its poison strikes at the start of its turn: still the one hit, by the first spell.
        Assert.Contains(f.Events, e => e is Damaged { Fighter: 1, Amount: 6 });
        Assert.Equal([1], Fired(f, "struck"));
    }

    [Fact]
    public void Hit_Melee_OnlyAnswersABlowFromTheNextCell_AndTakesTheAttackersMovementPointAtOnce()
    {
        Fight f = Play(["A.B"], Caster(mp: 3), Monster(0, passives: "thorns"));
        f.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.Empty(Fired(f, "thorns"));
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(1, 0))));
        Assert.Equal(2, f.Fighters[0].Mp);
        f.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.Equal([1], Fired(f, "thorns"));
        // The attacker is playing: its movement point goes now (sprint 90, D69 §2.1).
        Assert.Equal(1, f.Fighters[0].Mp);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(3, f.Fighters[0].Mp);
    }

    [Fact]
    public void LowHp_FiresOnce_EvenHealedAndHurtAgain()
    {
        Fight f = Play(["AB"], Caster(), Monster(0, passives: "low"));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        // 30 hit points of 50: not below half yet.
        Assert.Empty(Fired(f, "low"));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Equal([1], Fired(f, "low"));
        f.Apply(new CastAction("cure", new Cell(1, 0)));
        Assert.Equal(f.Fighters[1].MaxHp, f.Fighters[1].Hp);
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.True(f.Fighters[1].Hp * 2 < 50);
        Assert.Equal([1], Fired(f, "low"));
    }

    [Fact]
    public void AllyDies_FiresForTheSameFamily_WithinItsRange()
    {
        // The victim at (1, 0); a boar 2 steps away, a boar 5 steps away, a wolf next to it.
        Fight f = Play(["AB.B..B", ".B....."],
            Caster(), Monster(0, hp: 10, family: "boar"), Monster(1, family: "boar", passives: "grudge"), Monster(2, family: "boar", passives: "grudge"),
            Monster(3, family: "wolf", passives: "grudge"));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Equal([2], Fired(f, "grudge"));
        Assert.Equal(15, f.Fighters[2].DamageBonusFor(Element.Neutral));
    }

    [Fact]
    public void Death_ChainsExplosions_InOrder_ButNotWhenTheFightIsOver()
    {
        // Two bloaters side by side, a third monster far away: the first one's blast kills the second.
        Fight f = Play(["A.BB....B"], Caster(), Monster(0, hp: 10, passives: "boom"), Monster(1, hp: 5, passives: "boom"), Monster(2));
        f.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.Equal([1, 2], Fired(f, "boom"));
        Assert.False(f.Fighters[2].IsAlive);
        Assert.False(f.IsOver);

        Fight last = Play(["A.B"], Caster(), Monster(0, hp: 10, passives: "boom"));
        last.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.True(last.IsOver);
        Assert.Empty(Fired(last, "boom"));
    }

    [Fact]
    public void Auras_OfTwoBearersStack_ABearersOwnIsRenewed()
    {
        Fight f = Play(["A...BB"], Spec(0, initiative: 1), Monster(0, initiative: 999, passives: "aura"), Monster(1, initiative: 998, passives: "aura"));
        // The first monster's turn has started; the second's next.
        f.Apply(new EndTurnAction());
        Assert.Equal(20, f.Fighters[2].DamageBonusFor(Element.Neutral));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        // A new round: the first monster's aura again, renewed on both of them, not stacked.
        Assert.Equal(1, f.Fighters[2].Statuses.Count(s => s.Source == 1));
        Assert.Equal(20, f.Fighters[2].DamageBonusFor(Element.Neutral));
        Assert.Equal(20, f.Fighters[1].DamageBonusFor(Element.Neutral));
    }

    [Fact]
    public void ASummoningPassive_PlacesItsCreaturesOnTheFreeCellsNextToTheBearer_InOrder()
    {
        // The bearer at (2, 1): the cell on its right is an obstacle, so the next steps come first.
        Fight f = Play(["..B..", "A.B#.", "....."], Caster(), Monster(1, hp: 30, passives: "pups"), Monster(0));
        f.Apply(new CastAction("hit", new Cell(2, 1)));
        f.Apply(new CastAction("hit", new Cell(2, 1)));
        Summoned[] pups = [.. f.Events.OfType<Summoned>()];
        // Below, then on the left; above is taken by the other monster.
        Assert.Equal([new Cell(2, 2), new Cell(1, 1)], pups.Select(p => p.Cell));
        Assert.All(pups, p => Assert.Equal(1, p.Summoner));
        // They play right after their summoner, in the order they came.
        int at = f.TurnOrder.ToList().IndexOf(f.Fighters[1]);
        Assert.Equal([pups[0].Fighter, pups[1].Fighter], f.TurnOrder.Skip(at + 1).Take(2).Select(x => x.Id));
        Assert.Equal(70, Pup.Scale);
    }

    [Fact]
    public void ALeap_LandsOnAFreeCellOnly()
    {
        Fight f = Play(["A.B..", "....."], Caster(spells: ["jump"]), Monster(0));
        Assert.Equal(ActionError.TargetNotFree, f.Check(new CastAction("jump", new Cell(2, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("jump", new Cell(3, 0))));
        Assert.Equal(new Cell(3, 0), f.Fighters[0].Cell);
        Assert.Contains(new Leaped(0, new Cell(0, 0), new Cell(3, 0)), f.Events);
    }

    [Fact]
    public void ASwap_TradesPlacesWithAnAlly_NeverWithAnEnemyWhenItAffectsAllies()
    {
        Fight f = Play(["A.A.B"], Caster(spells: ["trade"]), Spec(0, 1), Monster(0));
        Assert.Equal(ActionError.NoValidTarget, f.Check(new CastAction("trade", new Cell(4, 0))));
        Assert.Equal(ActionError.NoValidTarget, f.Check(new CastAction("trade", new Cell(3, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("trade", new Cell(2, 0))));
        Assert.Equal(new Cell(2, 0), f.Fighters[0].Cell);
        Assert.Equal(new Cell(0, 0), f.Fighters[1].Cell);
        Assert.Contains(new Swapped(0, 1), f.Events);
    }

    [Fact]
    public void AStatusOfMovementPoints_OnTheFighterPlaying_CountsAtOnce_OnAnotherFromItsNextTurn()
    {
        Fight f = Play(["A.....B"], Caster(mp: 3, "dodge", "slow"), Monster(0));
        f.Apply(new CastAction("dodge", new Cell(0, 0)));
        // Sidestep's +2 MP for one turn: this very turn (it never counted before sprint 90).
        Assert.Equal(5, f.Fighters[0].Mp);
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(5, 0))));
        f.Apply(new CastAction("slow", new Cell(6, 0)));
        Assert.Equal(3, f.Fighters[1].Mp);
        f.Apply(new EndTurnAction());
        Assert.Equal(2, f.Fighters[1].Mp);
        f.Apply(new EndTurnAction());
        Assert.Equal(3, f.Fighters[0].Mp);
        f.Apply(new EndTurnAction());
        Assert.Equal(3, f.Fighters[1].Mp);
    }

    [Fact]
    public void AFightWithPassives_ReplaysTheSame()
    {
        // Two heroes who strike up close, three boars who play first and only walk.
        GameData data = Data(["A..BB", ".A..B", "....B"], Spec(0, initiative: 1, spells: ["strike", "venom"]), Spec(0, 1, initiative: 1, spells: ["strike"]),
            Monster(0, hp: 30, family: "boar", passives: ["boom", "struck"]), Monster(1, hp: 40, family: "boar", passives: ["thorns", "low", "pups"]),
            Monster(2, hp: 40, family: "boar", passives: ["mend", "aura", "grudge"]));
        var fight = new Fight(data, "s", 42);
        Ai.PlayOut(fight);
        // Every trigger fired at least once.
        Assert.Equal(Enum.GetValues<Trigger>(), fight.Events.OfType<PassiveTriggered>().Select(p => Passives.First(x => x.Id == p.Passive).Trigger).Distinct().Order());
        Fight replayed = FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(data);
        Assert.Equal(Fingerprint(fight), Fingerprint(replayed));
    }

    [Fact]
    public void WhatAPassiveDoes_CountsForItsBearer()
    {
        // The bloater explodes next to the caster who finished it.
        Fight f = Play(["AB......B"], Caster(), Monster(0, hp: 10, passives: "boom"), Monster(1));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        IReadOnlyList<FighterStats> stats = FightStats.Of(f);
        Assert.Equal(10, stats.First(s => s.Fighter == 1).DamageDealt);
        Assert.Equal(10, stats.First(s => s.Fighter == 0).DamageTaken);
        Assert.Equal(1, stats.First(s => s.Fighter == 0).Kills);
    }

    [Fact]
    public void TheAi_LeapsWhenItCannotWalk_ToStrikeFromWhereItLands()
    {
        Fight f = Play(["A...B"], Spec(0, mp: 0, initiative: 999, spells: ["jump", "strike"]), Monster(0));
        Assert.Equal(new CastAction("jump", new Cell(3, 0)), Ai.Decide(f));
    }

    [Fact]
    public void TheAi_WalksRatherThanLeaps_WhenTheWalkLeavesMorePointsToStrike()
    {
        Fight f = Play(["A..B"], Spec(0, mp: 3, initiative: 999, spells: ["jump", "strike"]), Monster(0));
        Assert.Equal(new MoveAction(new Cell(2, 0)), Ai.Decide(f));
    }

    [Fact]
    public void TheAi_SwapsWithAnAlly_OnlyToSaveItFromAnEnemyNextToIt()
    {
        // The ally at (2, 0), an enemy next to it at (3, 0).
        Fight f = Play(["A.AB"], Spec(0, mp: 0, initiative: 999, spells: ["trade"]), Spec(0, 1), Monster(0));
        Assert.Equal(new EndTurnAction(), Ai.Decide(f));
        f.Fighters[1].Hp = 20;
        Assert.Equal(new CastAction("trade", new Cell(2, 0)), Ai.Decide(f));
        // Never to put a weaker one in its place.
        f.Fighters[0].Hp = 10;
        Assert.Equal(new EndTurnAction(), Ai.Decide(f));
    }

    [Fact]
    public void AFamilysGroups_FightOnItsBattleMaps_EachGroupAlwaysOnTheSame()
    {
        var family = new MonsterFamily("f", Text, "z", "a", "boss", [], ["a", "b", "c"]);
        string[] maps = [.. Enumerable.Range(0, 60).Select(i => family.BattleOf($"woods-1-{i % 5}.{i / 5}"))];
        Assert.Equal(["a", "b", "c"], maps.Distinct().Order());
        Assert.Equal(family.BattleOf("woods-1-2.0"), family.BattleOf("woods-1-2.0"));
        // Without a list, the one battle map, as before sprint 90.
        Assert.Equal("a", (family with { Battles = null }).BattleOf("woods-1-2.0"));
    }

    [Fact]
    public void InconsistentPassives_AreRefused()
    {
        string[] rows = ["AB"];
        GameData With(PassiveSpec p, params string[] on) =>
            new(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", [Spec(0), Monster(0, passives: on.Length == 0 ? [p.Id] : on)])], summons: [Pup], passives: [.. Passives.Where(x => x.Id != p.Id), p]);
        // A percentage for a low-health passive only; melee and the attacker for a hit only; a range for an ally's death only.
        Assert.Throws<InvalidDataException>(() => With(Mend with { Percent = 50 }));
        Assert.Throws<InvalidDataException>(() => With(Low with { Percent = 0 }));
        Assert.Throws<InvalidDataException>(() => With(Low with { Percent = 100 }));
        Assert.Throws<InvalidDataException>(() => With(Mend with { Melee = true }));
        Assert.Throws<InvalidDataException>(() => With(Passive("x", Trigger.Death, new StatusEffect(Affects.Attacker, Stat.Mp, -1, 1))));
        Assert.Throws<InvalidDataException>(() => With(Mend with { Range = 2 }));
        // Effects in a spell's bounds, no leap, a known creature, one to four of them, a real area.
        Assert.Throws<InvalidDataException>(() => With(Passive("x", Trigger.Death, new StatusEffect(Affects.Enemies, Stat.Mp, -1, 11))));
        Assert.Throws<InvalidDataException>(() => With(Passive("x", Trigger.LowHp, new LeapEffect(Affects.Caster)) with { Percent = 50 }));
        Assert.Throws<InvalidDataException>(() => With(Pups with { Effects = [new SummonEffect(Affects.All, "nobody")] }));
        Assert.Throws<InvalidDataException>(() => With(Pups with { Effects = [new SummonEffect(Affects.All, "pup", Count: 5)] }));
        Assert.Throws<InvalidDataException>(() => With(Boom with { Area = new Area(AreaShape.Point) }));
        // Three passives at most, each known and once.
        Assert.Throws<InvalidDataException>(() => With(Mend, "mend", "struck", "low", "grudge"));
        Assert.Throws<InvalidDataException>(() => With(Mend, "mend", "mend"));
        Assert.Throws<InvalidDataException>(() => With(Mend, "nothing"));
        // A leap and a swap act on one cell, a leap on the caster, a swap on someone else.
        GameData Spell(Spell sp) => new([sp], [new MapSpec("m", Text, rows)], []);
        Assert.Throws<InvalidDataException>(() => Spell(Jump with { Area = new Area(AreaShape.Circle, 1) }));
        Assert.Throws<InvalidDataException>(() => Spell(Jump with { Effects = [new LeapEffect(Affects.Enemies)] }));
        Assert.Throws<InvalidDataException>(() => Spell(Trade with { Effects = [new SwapEffect(Affects.Caster)] }));
        Assert.Throws<InvalidDataException>(() => Spell(Hit with { Effects = [new StatusEffect(Affects.Attacker, Stat.Mp, -1, 1)] }));
    }
}
