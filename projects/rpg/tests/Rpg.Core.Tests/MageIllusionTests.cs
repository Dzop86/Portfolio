using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Mage's second half (sprint 200, docs/classes-mage-2.md): the Illusionist's three bricks on the arena (an image in its caster's look, a lure that draws
/// the monsters' blows, the bonus near it), its lineages, then the Mage's twelve final classes.
/// </summary>
public class MageIllusionTests
{
    private static Hero Mage(string? spec, string? fork, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "mage", Level: level, Stats: new Characteristics(Fire: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static GameData With(string[] rows, FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    /// <summary>The hero plays first, then the monsters by their initiative.</summary>
    private static Fight On(string[] rows, Hero hero, params FighterSpec[] monsters) => new(With(rows, [Spec(0, initiative: 9999), .. monsters]), "s", 1, hero);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static TalentSpec Path(Hero hero) => Specs.AsTalent(Specs.RulesOf(hero, Real));

    // ---- The bricks (§3) ----

    [Fact]
    public void TheImage_WearsItsCastersLook_HasThirtyPercentOfItsHitPoints_AndIsALure()
    {
        Fight f = On(["A.....B...", ".........."], Mage("illusionist", null, 40, deck: "path-image"), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-image", new Cell(2, 1))));
        Fighter image = f.Fighters.Single(x => x.SummonKind == "mirror-image");
        Assert.Equal("female-c", me.Spec.Look);
        Assert.Equal(me.Spec.Look, image.Spec.Look);
        Assert.NotEqual(Real.Summons["mirror-image"].Look, image.Spec.Look);
        Assert.True(image.Lure);
        Assert.Equal(me.MaxHp * 30 / 100, image.MaxHp);
        Assert.Equal((0, 0), (image.Ap, image.Mp));
    }

    [Fact]
    public void TheLures_AreData_TheDecoyKeepsItsPull_TheHawkHasNone()
    {
        Assert.True(Real.Summons["decoy"].Lure);
        Assert.False(Real.Summons["decoy"].Mirror);
        Assert.True(Real.Summons["mirror-image"] is { Lure: true, Mirror: true });
        Assert.True(Real.Summons["mirror-double"] is { Lure: true, Mirror: true });
        Assert.All(Real.Summons.Values.Where(s => s.Id is not ("decoy" or "mirror-image" or "mirror-double")), s => Assert.False(s.Lure || s.Mirror));
    }

    [Fact]
    public void AMonsterNextToTheImage_StrikesIt_AndSparesTheMage()
    {
        // The monster, rooted, next to both: the Mage beside it, the Image below it.
        Fight f = On(["AB", ".."], Mage("illusionist", null, 40, deck: "path-image"), Spec(1, hp: 900, mp: 0, initiative: 1, spells: ["strike"]));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-image", new Cell(1, 1))));
        Fighter image = f.Fighters.Single(x => x.SummonKind == "mirror-image");
        f.Apply(new EndTurnAction());
        // The Image does not act.
        Assert.Equal(image, f.Current);
        Assert.Equal(new EndTurnAction(), Ai.Decide(f));
        f.Apply(new EndTurnAction());
        Assert.Equal(new CastAction("strike", image.Cell), Ai.Decide(f));
    }

    [Fact]
    public void TheIllusionist_HitsHarder_WhatStandsWithinTwoCellsOfItsImage()
    {
        TalentSpec path = Path(Mage("illusionist", null, 40));
        Assert.Equal(10, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(LureCells: 2)));
        Assert.Equal(0, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(LureCells: 3)));
        Assert.Equal(0, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext()));
        // On the board: the distance to the nearest of its own lures.
        Fight f = On(["A.....B...", ".........."], Mage("illusionist", null, 40, deck: "path-image"), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!, monster = f.Fighters[1];
        Assert.Equal(int.MaxValue, f.ContextOf(me, null, me.Cell, monster).LureCells);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-image", new Cell(4, 0))));
        Assert.Equal(2, f.ContextOf(me, null, me.Cell, monster).LureCells);
    }

    [Fact]
    public void TheDazzle_StrikesAndShortensTheEnemysRange()
    {
        Fight f = On(["A...B.....", ".........."], Mage("illusionist", null, 40, deck: "path-dazzle"), Spec(1, hp: 900, initiative: 1));
        Fighter monster = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-dazzle", monster.Cell)));
        Assert.True(monster.Hp < 900);
        Assert.Contains(monster.StatusList, s => s is { Stat: Stat.Range, Value: -2 });
    }

    // ---- The lineages (§1) ----

    [Fact]
    public void TheMasterOfIllusions_HasADoubleThatStrikes()
    {
        Fight f = On(["A...B.....", ".........."], Mage("illusionist", "mirage-master", 60, deck: "path-double"), Spec(1, hp: 900, initiative: 1));
        Fighter monster = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-double", new Cell(3, 1))));
        Fighter twin = f.Fighters.Single(x => x.SummonKind == "mirror-double");
        Assert.True(twin.Lure);
        Assert.Equal(f.HeroFighter!.Spec.Look, twin.Spec.Look);
        // It plays right after its summoner, and strikes.
        f.Apply(new EndTurnAction());
        Assert.Equal(twin, f.Current);
        for (int i = 0; i < 10 && f.Current == twin; i++)
            f.Apply(Ai.Decide(f));
        Assert.True(monster.Hp < 900);
    }

    [Fact]
    public void TheMentalist_GripsAMind_HurtsIt_AndHitsHarderWhatItWeakened()
    {
        Fight f = On(["A...B.....", ".........."], Mage("illusionist", "mentalist", 60, deck: "path-grip"), Spec(1, hp: 900, initiative: 1));
        Fighter monster = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-grip", monster.Cell)));
        Assert.True(monster.Hp < 900);
        Assert.Contains(monster.StatusList, s => s is { Stat: Stat.Ap, Value: -2 });
        Assert.Contains(monster.StatusList, s => s is { Stat: Stat.Damage, Value: -15 });
        Assert.Equal(2, monster.States);
        TalentSpec path = Path(Mage("illusionist", "mentalist"));
        Assert.Equal(10, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(TargetStates: 2)));
        Assert.Equal(0, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(TargetStates: 1)));
    }

    // ---- The twelve final classes (§4) ----

    [Theory]
    [InlineData("rune-weaver", "rune-master", "magic-architect")]
    [InlineData("scholar", "arcane-sage", "archmage")]
    [InlineData("mirage-master", "mirage-weaver", "illusion-lord")]
    [InlineData("mentalist", "supreme-telepath", "mind-dominator")]
    [InlineData("time-weaver", "time-keeper", "instant-lord")]
    [InlineData("distorter", "time-traveller", "distortion-master")]
    public void EachMageLineage_EndsInTwoFinals_OfTwoTemplates(string lineage, string first, string second)
    {
        SpecFork fork = Real.Specs.Values.Where(s => s.Class == "mage").SelectMany(s => s.Forks).Single(f => f.Id == lineage);
        Assert.Equal([first, second], fork.Finals!.Select(f => f.Id));
        Assert.NotEqual(fork.Finals![0].Template, fork.Finals[1].Template);
        Assert.Equal([Specs.Forks], fork.Tiers.Select(t => t.Level));
        Assert.All(fork.Finals, x => Assert.Equal("spec-" + x.Id, x.Icon));
    }

    [Fact]
    public void TheRuneMaster_LaysTwoRunes_TheArchitectFartherAndWider()
    {
        Spell master = Compiled(Mage("arcanist", "rune-weaver", 100, "rune-master"), "path-rune");
        Assert.Equal((2, 1), (master.AllEffects.OfType<GlyphEffect>().Single().Max, master.Cooldown));
        Spell architect = Compiled(Mage("arcanist", "rune-weaver", 100, "magic-architect"), "path-rune");
        Assert.Equal((8, new Area(AreaShape.Circle, 1)), (architect.MaxRange, architect.Zone));
        Assert.Equal(1, Path(Mage("arcanist", "rune-weaver", 100, "magic-architect")).Rules.OfType<GlyphTurnsRule>().Sum(r => r.Extra));
    }

    [Fact]
    public void TheArchmage_OpensWithThreePointsMore_AndHitsHarderWithItsBigSpells()
    {
        Fight f = On(["A.....B..."], Mage("arcanist", "scholar", 100, "archmage"), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!;
        // The Arcanist's point for not walking, and the Archmage's three on the first turn.
        Assert.Equal(me.Spec.Ap + 1 + 3, me.Ap);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(me.Spec.Ap + 1, me.Ap);
        TalentSpec path = Path(Mage("arcanist", "scholar", 100, "archmage"));
        Assert.Equal(20, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(ApCost: 7)));
        Assert.Equal((1, 4), (Compiled(Mage("arcanist", "scholar", 100, "arcane-sage"), "path-ray").Cooldown, Compiled(Mage("arcanist", "scholar", 100, "arcane-sage"), "path-ray").ApCost));
    }

    [Fact]
    public void TheIllusionFinals_TwoImages_OrAStrongDouble()
    {
        Spell image = Compiled(Mage("illusionist", "mirage-master", 100, "mirage-weaver"), "path-image");
        Assert.Equal(2, image.AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Equal(2, image.Cooldown);
        SummonRule share = Path(Mage("illusionist", "mirage-master", 100, "illusion-lord")).Rules.OfType<SummonRule>().Single();
        Assert.Equal(("mirror-double", 25, 0), (share.Summon, share.HpPercent, share.StatsPercent));
        Assert.Equal(15, Talents.Percent([Path(Mage("illusionist", "mirage-master", 100, "mirage-weaver"))], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(LureCells: 1)));
        TalentSpec dominator = Path(Mage("illusionist", "mentalist", 100, "mind-dominator"));
        Assert.Equal(20, Talents.Percent([dominator], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(TargetStates: 2)));
        Spell grip = Compiled(Mage("illusionist", "mentalist", 100, "supreme-telepath"), "path-grip");
        Assert.Equal((9, 2), (grip.MaxRange, grip.ApCost));
    }

    [Fact]
    public void TheTimeFinals_SaveAnAlly_HurryEveryTurn_DelayTwice_OrCrescendoEveryTurn()
    {
        Hero keeper = Mage("chronomancer", "time-weaver", 100, "time-keeper");
        Assert.Contains("spec-not-today", Specs.TiersOf(keeper, Real).SelectMany(t => t.Passives ?? []));
        Assert.Equal(7, Compiled(keeper, "path-quicken").MaxRange);
        Assert.Equal(1, Compiled(Mage("chronomancer", "time-weaver", 100, "instant-lord"), "path-quicken").Cooldown);
        Spell offbeat = Compiled(Mage("chronomancer", "distorter", 100, "time-traveller"), "path-offbeat");
        Assert.Equal(2, offbeat.AllEffects.OfType<DelayEffect>().Sum(d => d.Places));
        Assert.Equal(1, Compiled(Mage("chronomancer", "distorter", 100, "distortion-master"), "path-crescendo").Cooldown);
    }
}
