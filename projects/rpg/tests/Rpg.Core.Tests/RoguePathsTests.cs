using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Rogue's paths (sprint 204, docs/classes-voleur.md §5): the Assassin and the Malandrin, their four lineages and eight final classes on the
/// real data, with no brick of their own; the Shadowmage of sprint 205 (docs/classes-voleur-2.md) in <see cref="ShadowmageTests"/>.
/// </summary>
public class RoguePathsTests
{
    private static Hero Rogue(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "rogue", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static GameData Board(string[] rows, params FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    [Fact]
    public void TheTree_IsAssassinMalandrinShadowmage_TwoLineagesEach_TwelveFinals()
    {
        Specialisation[] branches = [.. Real.Specs.Values.Where(s => s.Class == "rogue")];
        Assert.Equal(["assassin", "scoundrel", "shadowmage"], branches.Select(b => b.Id));
        Assert.Equal(["black-blade", "poisoner", "swashbuckler", "saboteur", "night-stalker", "shadow-illusionist"], branches.SelectMany(b => b.Forks).Select(f => f.Id));
        Assert.Equal(12, branches.SelectMany(b => b.Forks).SelectMany(f => f.Finals!).Count());
        Assert.All(branches, b => Assert.Equal(2, b.SpellsWith(null).Count()));
        Spell[] paths = [.. branches.SelectMany(b => b.Forks.SelectMany(f => b.SpellsWith(f.Id))).Distinct().Select(id => Real.Spells[id])];
        Assert.Equal(12, paths.Length);
        // The Shadowmage strikes one target: no area of its own (docs/classes-conception.md §1.4).
        Assert.All(Real.Specs["shadowmage"].Forks.SelectMany(f => Real.Specs["shadowmage"].SpellsWith(f.Id)), id => Assert.Equal(AreaShape.Point, Real.Spells[id].Zone.Shape));
        // The Vault and the Feint of before are the Malandrin's; every path spell reaches 4 cells at most.
        Assert.Equal(["path-vault", "path-hamstring"], Real.Specs["scoundrel"].Base.Spells);
        Assert.Equal(["path-feint"], Real.Specs["scoundrel"].ForkOf("swashbuckler")!.Tiers[0].Spells);
        Assert.All(paths, sp => Assert.InRange(sp.MaxRange, 0, 4));
        // No spell of the game is left without a path.
        string[] onPaths = [.. Real.Specs.Values.SelectMany(s => s.Forks.SelectMany(f => s.SpellsWith(f.Id)))];
        Assert.All(Real.Spells.Keys.Where(id => id.StartsWith("path-", StringComparison.Ordinal)), id => Assert.Contains(id, onPaths));
    }

    [Fact]
    public void TheAssassin_StrikesHarderUnderAState_ThePoisonersPoisonsLastLonger()
    {
        Assert.Equal(10, Specs.RulesOf(Rogue("assassin", level: 40), Real).OfType<TargetStateRule>().Single(r => r.States == 1).Percent);
        // Its poisons a turn longer: the Coated Blade's 3 turns become 4 on the target.
        Hero poisoner = Rogue("assassin", "poisoner", 50, deck: "path-envenom");
        Fight f = new(Board(["AB"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 1, poisoner);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-envenom", f.Fighters[1].Cell)));
        Assert.Contains(f.Fighters[1].Statuses, st => st is { Stat: Stat.Poison, TurnsLeft: 4 });
    }

    [Fact]
    public void TheMalandrin_StrikesHarderForTheCellsWalked_TheSwashbucklerHasAMovementPointMore()
    {
        Assert.Equal(new DanceRule(3, 15), Specs.RulesOf(Rogue("scoundrel", level: 40), Real).OfType<DanceRule>().Single());
        Assert.Equal(1, Specs.RulesOf(Rogue("scoundrel", "swashbuckler", 50), Real).OfType<PointsRule>().Sum(r => r.Mp));
        // The Duelist's adds up to 5 % a cell, 25 % in all.
        TalentSpec duelist = Specs.AsTalent(Specs.RulesOf(Rogue("scoundrel", "swashbuckler", 100, "rogue-duelist"), Real));
        Assert.Equal(25, Talents.Percent([duelist], new Cell(0, 0), new Cell(1, 0), context: new HitContext(CellsSinceSpell: 5)));
        Assert.Equal(10, Talents.Percent([duelist], new Cell(0, 0), new Cell(1, 0), context: new HitContext(CellsSinceSpell: 2)));
    }

    [Fact]
    public void TheSaboteursCaltrops_LieAroundIt_AndHoldWhoeverStepsIn()
    {
        Hero saboteur = Rogue("scoundrel", "saboteur", 50, deck: "path-caltrops");
        Fight f = new(Board(["A.B"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 1, saboteur);
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-caltrops", me.Cell)));
        Assert.Contains(f.Events.OfType<GlyphPlaced>(), g => g.Cells.Contains(new Cell(me.Cell.X + 1, me.Cell.Y)) && g.Cells.Contains(me.Cell));
    }

    [Fact]
    public void TheFinals_ChangeWhatTheirLineageHas()
    {
        Assert.Equal(1, Compiled(Rogue("assassin", "black-blade", 100, "silent-reaper"), "path-execute").Cooldown);
        Assert.Contains("spec-bounty", Specs.PassivesOf(Rogue("assassin", "black-blade", 100, "deadly-shadow"), Real));
        Spell plague = Compiled(Rogue("assassin", "poisoner", 100, "deadly-alchemist"), "path-plague");
        Assert.Equal((4, new Area(AreaShape.Circle, 1)), (plague.MaxRange, plague.Area));
        Assert.Equal(20, Specs.RulesOf(Rogue("assassin", "poisoner", 100, "toxin-master"), Real).OfType<TargetStateRule>().Single(r => r.States == 2).Percent);
        Assert.Equal(0, Compiled(Rogue("scoundrel", "swashbuckler", 100, "dancing-blade"), "path-vault").Cooldown);
        Assert.Equal(25, Specs.RulesOf(Rogue("scoundrel", "saboteur", 100, "demolisher"), Real).OfType<TargetStateRule>().Single().Percent);
        Assert.Equal(2, Compiled(Rogue("scoundrel", "saboteur", 100, "trap-master"), "path-caltrops").PerTurn);
        // Two sisters of two templates, every one with its words in both languages.
        Assert.All(Real.Specs.Values.Where(s => s.Class == "rogue").SelectMany(s => s.Forks), f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
    }
}
