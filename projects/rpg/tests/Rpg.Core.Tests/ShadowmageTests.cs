using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Rogue's second half (sprint 205, docs/classes-voleur-2.md): its poisons that stack, on small boards and for the AI; then the Shadowmage, its
/// strike from the shadow, its two lineages and four final classes on the real data.
/// </summary>
public class ShadowmageTests
{
    private static readonly Spell Venom = new("venom", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 4, 3, Element.Water)]);
    private static readonly Spell Brief = new("brief", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 4, 1, Element.Water)]);
    private static readonly Spell Fire = new("fire", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 5, 2, Element.Fire)]);
    private static readonly Spell Stab = new("stab", Text, 1, 1, 1, false, false, 20, 20, 9);
    private static readonly Spell Fade = new("fade", Text, 1, 0, 0, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Caster, Stat.Hidden, 1, 2)]);
    private static readonly Spell[] All = [.. Spells, Venom, Brief, Fire, Stab, Fade];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(IReadOnlyList<TalentRule> rules, params string[] spells) =>
        Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells) with { SpecRules = rules };

    private static Status PoisonOf(Fighter f, Element e) => f.Statuses.Single(s => s.Stat == Stat.Poison && s.Element == e);

    // ---- Poisons that stack (§1) ----

    [Fact]
    public void TheRoguesPoisons_Stack_UpToThreeDoses_TheLongerTurnsKept()
    {
        Fight f = Play(["AB"], Caster([new StackRule(3)], "venom", "brief", "fire"), Spec(1, hp: 900, initiative: 1));
        Fighter target = f.Fighters[1];
        f.Apply(new CastAction("venom", target.Cell));
        f.Apply(new CastAction("brief", target.Cell));
        Assert.Equal((8, 3), (PoisonOf(target, Element.Water).Value, PoisonOf(target, Element.Water).TurnsLeft));
        f.Apply(new CastAction("venom", target.Cell));
        f.Apply(new CastAction("venom", target.Cell));
        Assert.Equal(12, PoisonOf(target, Element.Water).Value);
        // Another element is another poison.
        f.Apply(new CastAction("fire", target.Cell));
        Assert.Equal(5, PoisonOf(target, Element.Fire).Value);
        Assert.Contains(f.Events.OfType<StatusAdded>(), e => e is { Stat: Stat.Poison, Value: 12 });
    }

    [Fact]
    public void WithoutTheRule_APoisonIsRenewed_AsEver()
    {
        Fight f = Play(["AB"], Caster([], "venom"), Spec(1, hp: 900, initiative: 1));
        f.Apply(new CastAction("venom", f.Fighters[1].Cell));
        f.Apply(new CastAction("venom", f.Fighters[1].Cell));
        Assert.Equal(4, PoisonOf(f.Fighters[1], Element.Water).Value);
    }

    [Fact]
    public void TheRogue_StacksBecauseOfItsClass_AndTheAiValuesTheDoseAdded()
    {
        Assert.Contains(new StackRule(3), Real.Class("rogue")!.Rules!);
        Fight f = new(Real, "training", 1, new Hero("Ondine", "female-c", "rogue"));
        Assert.Equal(3, Fight.StackOf(f.HeroFighter!));
        Assert.Null(Fight.StackOf(new Fight(Real, "training", 1, new Hero("Ondine", "female-c", "archer")).HeroFighter!));
    }

    // ---- The Shadowmage (§2) ----

    [Fact]
    public void ABlowStruckWhileHidden_IsTwentyPercentHarder_WithTheShadowmagesRule()
    {
        Fight f = Play(["AB.B"], Caster([new ShadowRule(20)], "fade", "stab"), Spec(1, hp: 500, initiative: 1), Spec(1, 1, hp: 500, initiative: 0));
        Fighter target = f.Fighters[1];
        f.Apply(new CastAction("stab", target.Cell));
        Assert.Equal(480, target.Hp);
        f.Apply(new CastAction("fade", f.Fighters[0].Cell));
        f.Apply(new CastAction("stab", target.Cell));
        Assert.Equal(456, target.Hp);
        // The blow ends the Stealth: the next one is plain.
        f.Apply(new CastAction("stab", target.Cell));
        Assert.Equal(436, target.Hp);
    }

    private static Hero Rogue(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "rogue", Level: level, Stats: new Characteristics(Air: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static GameData Board(string[] rows, params FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    [Fact]
    public void TheShadowStep_LeapsAndHides_TheNightStalkersStealthLastsATurnMore()
    {
        Assert.Equal(20, Specs.RulesOf(Rogue("shadowmage", level: 40), Real).OfType<ShadowRule>().Single().Percent);
        Hero bare = Rogue("shadowmage", level: 40, deck: "path-shadow-step"), stalker = Rogue("shadowmage", "night-stalker", 50, deck: "path-shadow-step");
        foreach ((Hero hero, int turns) in new[] { (bare, 1), (stalker, 2) })
        {
            Fight f = new(Board(["A...B"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 1, hero);
            Fighter me = f.HeroFighter!;
            Assert.Equal(ActionError.None, f.Apply(new CastAction("path-shadow-step", new Cell(me.Cell.X + 3, me.Cell.Y))));
            Assert.Equal(new Cell(3, 0), me.Cell);
            Assert.Contains(me.Statuses, s => s.Stat == Stat.Hidden && s.TurnsLeft == turns);
        }
    }

    [Fact]
    public void TheFalseFront_LeavesAnImage_AndHidesTheRogue()
    {
        Hero illusionist = Rogue("shadowmage", "shadow-illusionist", 50, deck: "path-false-front");
        Fight f = new(Board(["A...B"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 1, illusionist);
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-false-front", new Cell(me.Cell.X + 1, me.Cell.Y))));
        Assert.Contains(f.Fighters, x => x.IsAlive && x.Summoner == me.Id && x.SummonKind == "mirror-image");
        Assert.True(me.Hidden);
    }

    [Fact]
    public void TheShadowmagesFinals_ChangeWhatTheirLineageHas()
    {
        Assert.Equal(6, Compiled(Rogue("shadowmage", "night-stalker", 100, "spectre"), "path-shadow-step").MaxRange);
        Assert.Equal(2, Specs.RulesOf(Rogue("shadowmage", "night-stalker", 100, "shadow-lord"), Real).OfType<PointsRule>().Where(r => r.UntilTurn == 1).Sum(r => r.Ap));
        Assert.Contains("spec-vanish", Specs.PassivesOf(Rogue("shadowmage", "shadow-illusionist", 100, "phantom"), Real));
        Assert.All(Real.Specs["shadowmage"].Forks, f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
    }

    [Fact]
    public void ThePhantom_VanishesOnceBelowAThirdOfItsHitPoints()
    {
        Hero phantom = Rogue("shadowmage", "shadow-illusionist", 100, "phantom", "pommel-strike");
        Fight f = new(Board(["AB"], Spec(0, initiative: 9999), Spec(1, hp: 9000, initiative: 1, spells: ["strike"])), "s", 1, phantom);
        Fighter me = f.HeroFighter!;
        me.Hp = me.MaxHp * 31 / 100;
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("strike", me.Cell));
        Assert.True(me.Hidden);
        Assert.Contains(f.Events.OfType<PassiveTriggered>(), p => p.Passive == "spec-vanish");
    }
}
