using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Warrior's six new final classes (sprint 197, D96, docs/classes-guerrier.md §2), each on the arena: a post held, the charge sounded, a blade that reaches two cells,
/// a cage left on the ground, a line broken, a fighter on the ropes; and the hero converted to its taken-back final, who fights as before.
/// </summary>
public class WarriorFinalsTests
{
    private static readonly Spell Slam = new("t-slam", Text, 1, 1, 1, true, false, 80, 80, 9);

    private static Hero Warrior(string spec, string fork, string? final, int level = 100, params string[] deck) =>
        new("Ondine", "female-a", "warrior", Level: level, Stats: new Characteristics(Earth: 50), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static GameData With(string[] rows, FighterSpec[] fighters) =>
        new([.. Real.Spells.Values, Slam], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    /// <summary>The hero plays first, against monsters that play after it; a second hero stands on team A if given.</summary>
    private static Fight On(string[] rows, Hero hero, FighterSpec[] monsters, Hero? ally = null) =>
        new(With(rows, [Spec(0, initiative: 9999), .. monsters]), "s", 1, hero, allies: ally is null ? null : [ally]);

    private static FighterSpec Foe(int start = 0, int hp = 900, params string[] spells) => Spec(1, start, hp: hp, ap: 9, mp: 3, initiative: 1, spells: spells);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static int Fired(Fight f, string passive) => f.Events.OfType<PassiveTriggered>().Count(p => p.Passive == passive);

    [Fact]
    public void AConvertedHero_OnItsTakenBackFinal_HasThePassivesAndTitleItHad()
    {
        Assert.Equal(["spec-close-ranks-17", "spec-not-today"], Specs.PassivesOf(Warrior("palisade", "rampart", "crusader"), Real));
        Assert.Equal(["spec-close-ranks-12"], Specs.PassivesOf(Warrior("palisade", "rampart", null), Real));
        Assert.Equal("Croisé", Specs.TitleOf(Warrior("palisade", "rampart", "crusader"), Real)!.Fr);
        Assert.Equal("Gardien", Specs.TitleOf(Warrior("palisade", "rampart", null), Real)!.Fr);
        Assert.Equal(["Chevalier", "Maître d'armes", "Gladiateur"], Real.Specs.Values.Where(s => s.Class == "warrior").Select(s => s.Name.Fr));
    }

    [Fact]
    public void TheSentry_ShieldsAgain_AfterATurnWithoutWalking_NotAfterAStep()
    {
        Fight f = On(["AA.......B", ".........."], Warrior("palisade", "rampart", "sentry"), [Foe()], Warrior("palisade", "rampart", null) with { Name = "Brume", Final = null, Spec = null, Fork = null });
        Assert.Equal(1, Fired(f, "spec-sentry"));
        Assert.Equal(24, f.HeroFighter!.Shield);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        // It stood still: the post holds again.
        Assert.Equal(2, Fired(f, "spec-sentry"));
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(0, 1))));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(2, Fired(f, "spec-sentry"));
        Assert.Equal(1, Compiled(Warrior("palisade", "rampart", "sentry"), "path-stand-fast").Cooldown);
    }

    [Fact]
    public void TheWarlord_SoundsTheChargeOnce_WithTwoMoreApOnItsFirstTurn()
    {
        Hero plain = Warrior("palisade", "bastion", null), lord = Warrior("palisade", "bastion", "warlord");
        Fight before = On(["A........B"], plain, [Foe()]), f = On(["A........B"], lord, [Foe()]);
        Assert.Equal(before.HeroFighter!.Ap + 2, f.HeroFighter!.Ap);
        Assert.Equal(before.HeroFighter.Mp + 2, f.HeroFighter.Mp);
        Assert.Equal(1, Fired(f, "spec-charge"));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(1, Fired(f, "spec-charge"));
        Assert.Equal(before.HeroFighter.Ap, f.HeroFighter.Ap);
    }

    [Fact]
    public void TheGhostBlade_HoldsAndCrushesFromTwoCells()
    {
        Hero ghost = Warrior("grip", "jailer", "ghost-blade");
        Assert.Equal(2, Compiled(ghost, "path-hold").MaxRange);
        Assert.Equal(2, Compiled(ghost, "path-crush").MaxRange);
        Assert.Equal(1, Compiled(Warrior("grip", "jailer", "blade-master"), "path-crush").MaxRange);
        Fight f = On(["A.B......."], ghost with { Deck = ["path-hold"] }, [Foe()]);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-hold", new Cell(2, 0))));
    }

    [Fact]
    public void TheGeneral_LeavesAGlyphOfMinusTwoMp_ThreeCellsAway_ForThreeTurns()
    {
        Spell ring = Compiled(Warrior("grip", "pincer", "general"), "path-iron-ring");
        Assert.Equal(3, ring.MaxRange);
        GlyphEffect glyph = Assert.Single(ring.AllEffects.OfType<GlyphEffect>());
        Assert.Equal((Affects.Enemies, 3), (glyph.Affects, glyph.Turns));
        Fight f = On(["A...B....."], Warrior("grip", "pincer", "general", 100, "path-iron-ring"), [Foe()]);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-iron-ring", new Cell(3, 0))));
        GlyphPlaced placed = Assert.Single(f.Events.OfType<GlyphPlaced>());
        Assert.Equal(3, placed.Turns);
        Assert.Contains(new Cell(4, 0), placed.Cells);
        Assert.Equal(Real.Spells["path-iron-ring"].MaxRange, Compiled(Warrior("grip", "pincer", "strategist"), "path-iron-ring").MaxRange);
    }

    [Fact]
    public void TheDestroyer_BreaksALineOfFour_AndAssaultsInACrossOfTwo()
    {
        Hero destroyer = Warrior("breach", "ram", "destroyer");
        Assert.Equal(new Area(AreaShape.Line, 3), Compiled(destroyer, "path-breakthrough").Area);
        Spell assault = Compiled(destroyer, "path-assault"), before = Compiled(Warrior("breach", "ram", null), "path-assault");
        Assert.Equal(new Area(AreaShape.Cross, 2), assault.Area);
        Assert.True(assault.DamageMax < before.DamageMax);
    }

    [Fact]
    public void ThePugilist_HitsHarderInMelee_AndOnTheRopes_PushesItsNeighboursOnce()
    {
        Assert.Equal(25, Specs.TiersOf(Warrior("breach", "harpooner", "pugilist"), Real).SelectMany(t => t.Rules ?? []).OfType<DistanceRule>().Sum(r => r.Percent));
        Fight f = On(["..BA....B."], Warrior("breach", "harpooner", "pugilist"), [Foe(0, spells: ["t-slam"]), Foe(1)]);
        Cell neighbour = f.Fighters.First(x => x.Team == 1).Cell;
        f.Apply(new EndTurnAction());
        // The monster strikes until the hero is under half its life: the ropes push it away, once.
        for (int i = 0; i < 20 && Fired(f, "spec-on-the-ropes") == 0 && !f.IsOver; i++)
        {
            if (f.Current.Team == 1 && f.Current.Spells.Any(s => s.Id == "t-slam"))
                f.Apply(new CastAction("t-slam", f.HeroFighter!.Cell));
            f.Apply(new EndTurnAction());
        }
        Assert.Equal(1, Fired(f, "spec-on-the-ropes"));
        Assert.NotEqual(neighbour, f.Fighters.First(x => x.Team == 1).Cell);
    }
}
