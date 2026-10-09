using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Elements, areas and the effects of spells (sprint 54), on small boards written here.</summary>
public class EffectsTests
{
    // Fixed numbers where a test counts hit points.
    private static readonly Spell Flame = new("flame", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Burst = new("burst", Text, 2, 0, 6, false, false, 10, 10, 9, Element.Neutral, new Area(AreaShape.Cross, 1));
    private static readonly Spell Ring = new("ring", Text, 2, 0, 6, false, false, 10, 10, 9, Element.Neutral, new Area(AreaShape.Circle, 2));
    private static readonly Spell Lance = new("lance", Text, 2, 1, 6, false, true, 10, 10, 9, Element.Neutral, new Area(AreaShape.Line, 2));
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 15, 15)]);
    private static readonly Spell Ward = new("ward", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new ShieldEffect(Affects.Allies, 8, 2)]);
    private static readonly Spell Shove = new("shove", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PushEffect(Affects.Enemies, 3)]);
    private static readonly Spell Hook = new("hook", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PullEffect(Affects.Enemies, 5)]);
    private static readonly Spell Venom = new("venom", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2, Element.Water)]);
    private static readonly Spell Slow = new("slow", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null,
        [new StatusEffect(Affects.Enemies, Stat.Ap, -2, 1), new StatusEffect(Affects.Enemies, Stat.Mp, -1, 1)]);
    private static readonly Spell Rage = new("rage", Text, 1, 0, 0, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Caster, Stat.Damage, 50, 1)]);
    private static readonly Spell Guard = new("guard", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.All, Stat.Resistance, 30, 2, Element.Fire)]);
    private static readonly Spell[] All = [.. Spells, Flame, Burst, Ring, Lance, Mend, Ward, Shove, Hook, Venom, Slow, Rage, Guard];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(params string[] spells) => Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells);

    private static Dictionary<Element, int> Resist(Element e, int percent) => new() { [e] = percent };

    [Fact]
    public void AnElement_IsResisted_ANeutralHitIsNot()
    {
        Fight f = Play(["A.B.B"], Caster("flame", "strike"), Spec(1, 0, hp: 50) with { Resistances = Resist(Element.Fire, 50) }, Spec(1, 1, hp: 50) with { Resistances = Resist(Element.Fire, -20) });
        f.Apply(new CastAction("flame", new Cell(2, 0)));
        f.Apply(new CastAction("flame", new Cell(4, 0)));
        Assert.Equal([45, 38], f.Fighters.Skip(1).Select(x => x.Hp));
        // A neutral hit ignores resistances; an element the fighter does not resist is the roll itself.
        Assert.Equal(10, Fight.Damage(10, f.Fighters[0], f.Fighters[1], Element.Neutral));
        Assert.Equal(10, Fight.Damage(10, f.Fighters[0], f.Fighters[1], Element.Earth));
    }

    [Fact]
    public void Resistance_StopsAt90Percent()
    {
        // 80 % of its own and 30 % from a status: 90 %, a tenth of the hit goes through.
        Fight f = Play(["A.B"], Caster("guard", "flame"), Spec(1, hp: 50) with { Resistances = Resist(Element.Fire, 80) });
        f.Apply(new CastAction("guard", new Cell(2, 0)));
        Assert.Equal(90, f.Fighters[1].Resistance(Element.Fire));
        f.Apply(new CastAction("flame", new Cell(2, 0)));
        Assert.Equal(49, f.Fighters[1].Hp);
    }

    [Fact]
    public void Areas_TouchTheirCells_AlliesIncluded()
    {
        // Cross of radius 1 on (1, 1): itself and its four neighbours, a friend and the caster among them.
        Fight cross = Play([".B.", "ABA", ".B.", "..."], Caster("burst"), Spec(1, 0), Spec(1, 1), Spec(1, 2), Spec(0, 1));
        cross.Apply(new CastAction("burst", new Cell(1, 1)));
        Assert.Equal([40, 40, 40, 40], cross.Fighters.Skip(1).Select(x => x.Hp));
        Assert.Equal(40, cross.Fighters[0].Hp);

        // Circle of radius 2: diagonals within two steps too, not three.
        Fight ring = Play(["A....", ".....", "..B..", "...B.", "....B"], Caster("ring"), Spec(1, 0), Spec(1, 1), Spec(1, 2));
        ring.Apply(new CastAction("ring", new Cell(2, 2)));
        Assert.Equal([40, 40, 50], ring.Fighters.Skip(1).Select(x => x.Hp));

        // Line: from the target onwards, away from the caster.
        Fight line = Play(["A.BBBB"], Caster("lance"), Spec(1, 0), Spec(1, 1), Spec(1, 2), Spec(1, 3));
        line.Apply(new CastAction("lance", new Cell(2, 0)));
        Assert.Equal([40, 40, 40, 50], line.Fighters.Skip(1).Select(x => x.Hp));
    }

    [Fact]
    public void Healing_FillsMissingHitPoints_OfAlliesOnly()
    {
        Fight f = Play(["AA.B"], Caster("mend", "strike"), Spec(0, 1, hp: 50), Spec(1));
        f.Fighters[1].Hp = 30;
        f.Apply(new CastAction("mend", new Cell(1, 0)));
        Assert.Equal(45, f.Fighters[1].Hp);
        f.Apply(new CastAction("mend", new Cell(1, 0)));
        Assert.Equal(50, f.Fighters[1].Hp);
        Assert.Equal(new Healed(1, 5, 50), f.Events.OfType<Healed>().Last());
        f.Fighters[2].Hp = 10;
        f.Apply(new CastAction("mend", new Cell(3, 0)));
        Assert.Equal(10, f.Fighters[2].Hp);
    }

    [Fact]
    public void AShield_TakesTheDamageFirst_ThenWearsOff()
    {
        Fight f = Play(["AB"], Spec(0, initiative: 10, ap: 20, mp: 0, spells: ["strike"]), Spec(1, initiative: 99, ap: 20, mp: 0, spells: ["ward"]));
        // B shields itself (8 for two of its turns), then A strikes for 10.
        f.Apply(new CastAction("ward", new Cell(1, 0)));
        Assert.Equal(8, f.Fighters[1].Shield);
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("strike", new Cell(1, 0)));
        Assert.Equal((0, 48), (f.Fighters[1].Shield, f.Fighters[1].Hp));
        Assert.Contains(new ShieldAbsorbed(1, 8, 0), f.Events);
        // A fresh shield lasts two of B's turns: gone after the second.
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("ward", new Cell(1, 0)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(8, f.Fighters[1].Shield);
        f.Apply(new EndTurnAction());
        Assert.Equal(0, f.Fighters[1].Shield);
    }

    [Fact]
    public void APush_MovesAway_AndWhatStopsItHurts()
    {
        Fight open = Play(["AB....."], Caster("shove"), Spec(1));
        open.Apply(new CastAction("shove", new Cell(1, 0)));
        Assert.Equal((new Cell(4, 0), 50), (open.Fighters[1].Cell, open.Fighters[1].Hp));
        Pushed pushed = open.Events.OfType<Pushed>().Single();
        Assert.Equal((1, 0), (pushed.Fighter, pushed.Blocked));
        Assert.Equal([new Cell(2, 0), new Cell(3, 0), new Cell(4, 0)], pushed.Path);

        // Against a tree after one cell: two cells not travelled, two collisions.
        Fight wall = Play(["AB.#"], Caster("shove"), Spec(1));
        wall.Apply(new CastAction("shove", new Cell(1, 0)));
        Assert.Equal((new Cell(2, 0), 50 - 2 * Fight.CollisionDamage), (wall.Fighters[1].Cell, wall.Fighters[1].Hp));

        // Against the board's edge.
        Fight edge = Play(["AB"], Caster("shove"), Spec(1));
        edge.Apply(new CastAction("shove", new Cell(1, 0)));
        Assert.Equal(50 - 3 * Fight.CollisionDamage, edge.Fighters[1].Hp);

        // Against another fighter, who stays where it is.
        Fight crowd = Play(["AB.B"], Caster("shove"), Spec(1, 0), Spec(1, 1));
        crowd.Apply(new CastAction("shove", new Cell(1, 0)));
        Assert.Equal((new Cell(2, 0), 50 - 2 * Fight.CollisionDamage), (crowd.Fighters[1].Cell, crowd.Fighters[1].Hp));
        Assert.Equal((new Cell(3, 0), 50), (crowd.Fighters[2].Cell, crowd.Fighters[2].Hp));
    }

    [Fact]
    public void APull_BringsCloser_AndStopsNextToTheCaster()
    {
        Fight f = Play(["A....B"], Caster("hook"), Spec(1));
        f.Apply(new CastAction("hook", new Cell(5, 0)));
        Assert.Equal((new Cell(1, 0), 50), (f.Fighters[1].Cell, f.Fighters[1].Hp));
    }

    [Fact]
    public void APoison_StrikesAtTheStartOfTheVictimsTurns_ThenEnds()
    {
        Fight f = Play(["A.B"], Caster("venom"), Spec(1, hp: 50, ap: 6, mp: 0) with { Resistances = Resist(Element.Water, 50) });
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        // B's first turn: 6 water damage, halved by its resistance.
        Assert.Equal(47, f.Fighters[1].Hp);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(44, f.Fighters[1].Hp);
        f.Apply(new EndTurnAction());
        Assert.Contains(new StatusEnded(1, Stat.Poison), f.Events);
        f.Apply(new EndTurnAction());
        Assert.Equal(44, f.Fighters[1].Hp);
    }

    [Fact]
    public void APoison_CanEndTheFight()
    {
        Fight f = Play(["A.B"], Caster("venom"), Spec(1, hp: 5));
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        Assert.True(f.IsOver);
        Assert.Equal(0, f.WinningTeam);
    }

    [Fact]
    public void Statuses_ChangePointsDamageAndResistances_ForTheirTurns()
    {
        Fight f = Play(["A.B"], Caster("slow", "rage", "flame", "guard"), Spec(1, hp: 99, ap: 6, mp: 3));
        f.Apply(new CastAction("slow", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        Assert.Equal((4, 2), (f.Current.Ap, f.Current.Mp));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal((6, 3), (f.Current.Ap, f.Current.Mp));
        f.Apply(new EndTurnAction());
        // +50 % damage for the caster, +30 % fire resistance for everyone in reach of "guard".
        f.Apply(new CastAction("rage", new Cell(0, 0)));
        f.Apply(new CastAction("flame", new Cell(2, 0)));
        Assert.Equal(99 - 15, f.Fighters[1].Hp);
        f.Apply(new CastAction("guard", new Cell(2, 0)));
        f.Apply(new CastAction("flame", new Cell(2, 0)));
        Assert.Equal(99 - 15 - 10, f.Fighters[1].Hp);
    }

    [Fact]
    public void AFightWithEffects_ReplaysTheSame()
    {
        var data = new GameData(All, [new MapSpec("m", Text, ["A.B..", "A...B"])],
            [new Scenario("s", Text, "m", [Caster("venom", "mend", "shove", "burst", "slow"), Spec(0, 1, mp: 2, spells: ["strike"]), Spec(1, 0, spells: ["strike", "bow"]), Spec(1, 1, spells: ["lob"])])]);
        var f = new Fight(data, "s", 5);
        Ai.PlayOut(f);
        Assert.Contains(f.Events, e => e is StatusAdded or Pushed or Healed);
        Fight again = FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(data);
        Assert.Equal(Fingerprint(f), Fingerprint(again));
    }

    [Fact]
    public void TheAi_HealsAHurtFriend_AndSparesFriendsInAnArea()
    {
        // Nobody in reach to hurt: the healer heals.
        Fight heal = Play(["AA...#B"], Spec(0, ap: 6, mp: 0, initiative: 999, spells: ["mend", "strike"]), Spec(0, 1), Spec(1));
        heal.Fighters[1].Hp = 20;
        Assert.Equal(new CastAction("mend", new Cell(1, 0)), Ai.Decide(heal));

        // The cross would hit two enemies and the friend between them: worse than a single strike.
        Fight spare = Play(["A.....", "BAB..."], Spec(0, ap: 6, mp: 0, initiative: 999, spells: ["burst", "flame"]), Spec(0, 1), Spec(1, 0), Spec(1, 1));
        FightAction choice = Ai.Decide(spare);
        Assert.Equal("flame", ((CastAction)choice).Spell);
    }

    [Theory]
    [InlineData("""{ "id": "x", "name": { "fr": "x", "en": "x" }, "apCost": 1, "minRange": 0, "maxRange": 1, "lineOfSight": true, "inLine": false, "damageMin": 0, "damageMax": 0, "perTurn": 1, "area": { "shape": "Cross", "radius": 9 } }""")]
    [InlineData("""{ "id": "x", "name": { "fr": "x", "en": "x" }, "apCost": 1, "minRange": 0, "maxRange": 1, "lineOfSight": true, "inLine": false, "damageMin": 0, "damageMax": 0, "perTurn": 1, "effects": [ { "kind": "heal", "affects": "Allies", "min": 5, "max": 2 } ] }""")]
    [InlineData("""{ "id": "x", "name": { "fr": "x", "en": "x" }, "apCost": 1, "minRange": 0, "maxRange": 1, "lineOfSight": true, "inLine": false, "damageMin": 0, "damageMax": 0, "perTurn": 1, "effects": [ { "kind": "push", "affects": "Caster", "cells": 2 } ] }""")]
    [InlineData("""{ "id": "x", "name": { "fr": "x", "en": "x" }, "apCost": 1, "minRange": 0, "maxRange": 1, "lineOfSight": true, "inLine": false, "damageMin": 0, "damageMax": 0, "perTurn": 1, "effects": [ { "kind": "status", "affects": "Enemies", "stat": "Poison", "value": -3, "turns": 2 } ] }""")]
    public void InconsistentEffects_AreRefused(string json)
    {
        Spell spell = System.Text.Json.JsonSerializer.Deserialize<Spell>(json, GameData.Json)!;
        Assert.Throws<InvalidDataException>(() => new GameData([spell], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])]));
    }

    [Fact]
    public void SpellsWithEffects_AreReadFromJson()
    {
        const string json = """{ "id": "x", "name": { "fr": "x", "en": "x" }, "apCost": 3, "minRange": 1, "maxRange": 4, "lineOfSight": true, "inLine": false, "damageMin": 4, "damageMax": 6, "perTurn": 2, "element": "Water", "area": { "shape": "Circle", "radius": 1 }, "effects": [ { "kind": "status", "affects": "Enemies", "stat": "Mp", "value": -1, "turns": 1 } ] }""";
        Spell spell = System.Text.Json.JsonSerializer.Deserialize<Spell>(json, GameData.Json)!;
        Assert.Equal((Element.Water, AreaShape.Circle, 1), (spell.Element, spell.Zone.Shape, spell.Zone.Radius));
        Assert.Equal(new StatusEffect(Affects.Enemies, Stat.Mp, -1, 1), Assert.Single(spell.AllEffects));
        Assert.Throws<InvalidDataException>(() => new GameData([], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0) with { Resistances = new Dictionary<Element, int> { [Element.Fire] = 95 } }, Spec(1)])]));
    }
}
