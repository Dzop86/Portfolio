using Rpg.Core;
using Xunit;
using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Druid's third branch (sprint 218, docs/classes-chaman.md): the Shaman, its spirits without a body that last two turns and its ground
/// that strikes; the Spirit Caller calls more of them, the Nature Elementalist strikes from the ground. On the real data.
/// </summary>
public class DruidShamanTests
{
    private static Hero Druid(string spec, string? fork, int level, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "druid", Level: level, Stats: new Characteristics(Earth: 100), Deck: deck.Length == 0 ? null : deck) { Spec = spec, Fork = fork, Final = final };

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    /// <summary>A training fight at the hero's turn, with all the points it needs, an enemy next to it.</summary>
    private static (Fight Fight, Fighter Me, Fighter Foe) Training(Hero hero)
    {
        Fight f = new(Real, "training", 1, hero);
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        Fighter me = f.HeroFighter!;
        me.Ap = 99;
        Fighter foe = f.Fighters.First(x => x.Team == 1);
        foe.Cell = me.Cell.Neighbours().First(f.IsFree);
        foe.Hp = 999;
        return (f, me, foe);
    }

    /// <summary>Ends turns until the hero's comes back, its points refilled.</summary>
    private static void NextTurn(Fight f)
    {
        do
            f.Apply(new EndTurnAction());
        while (f.Current != f.HeroFighter);
        f.HeroFighter!.Ap = 99;
    }

    /// <summary>A free cell the spirit may be called on, 2 cells from the hero.</summary>
    private static Cell Room(Fight f, Fighter me) =>
        me.Cell.Neighbours().SelectMany(c => c.Neighbours()).First(c => c.DistanceTo(me.Cell) == 2 && f.IsFree(c));

    [Fact]
    public void TheShaman_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["shaman"];
        Assert.Equal("druid", spec.Class);
        Assert.Equal(["path-spirit", "path-rumbling-ground"], spec.Base.Spells);
        Assert.Equal(["spirit-caller", "nature-elementalist"], spec.Forks.Select(x => x.Id));
        Assert.Contains(new SummonRule("spirit", 15, 35), Specs.RulesOf(Druid("shaman", null, 40), Real));
        // The Spirit Caller: three spirits, better fed (the larger share is taken), and its Communion around it.
        Assert.Equal(3, Compiled(Druid("shaman", "spirit-caller", 60), "path-spirit").AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Contains(new SummonRule("spirit", 20, 40), Specs.RulesOf(Druid("shaman", "spirit-caller", 60), Real));
        Assert.Contains(Compiled(Druid("shaman", "spirit-caller", 100, "spirit-master"), "path-communion").AllEffects, e => e is StatusEffect { Affects: Affects.Allies, Stat: Stat.Ap, Value: 2 });
        Assert.Equal(1, Compiled(Druid("shaman", "spirit-caller", 100, "living-ancestor"), "path-communion").Cooldown);
        // The Nature Elementalist strikes harder those on its glyphs; its Living Storm leaves one.
        Assert.Contains(new GlyphTargetRule(15, 0), Specs.RulesOf(Druid("shaman", "nature-elementalist", 60), Real));
        Assert.DoesNotContain(Specs.RulesOf(Druid("shaman", "spirit-caller", 60), Real), r => r is GlyphTargetRule);
        Spell storm = Real.Spells["path-living-storm"];
        Assert.True(storm.DamageMax > 0);
        Assert.Contains(storm.AllEffects, e => e is GlyphEffect { Affects: Affects.Enemies, DamageMax: > 0 });
        Assert.Contains(Compiled(Druid("shaman", "nature-elementalist", 100, "elements-lord"), "path-living-storm").AllEffects, e => e is StatusEffect { Affects: Affects.Enemies, Stat: Stat.Resistance, Value: -10 });
        Assert.Equal(1, Compiled(Druid("shaman", "nature-elementalist", 100, "druid-primal-avatar"), "path-living-storm").Cooldown);
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
    }

    [Fact]
    public void TheSpirit_HoldsNoCell_HidesNothing_PlaysTwoTurns_ThenFades()
    {
        (Fight f, Fighter me, _) = Training(Druid("shaman", null, 40, null, "path-spirit"));
        Cell cell = Room(f, me);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-spirit", cell)));
        Fighter spirit = f.Fighters.Single(x => x.SummonKind == "spirit");
        Assert.True(spirit.Incorporeal);
        Assert.False(f.BlocksSight(cell));
        // Fed by its caller: 15 % of its hit points.
        Assert.Equal(Math.Max(1, me.MaxHp * 15 / 100), spirit.Hp);
        int played = 0;
        for (int round = 0; round < 4 && spirit.IsAlive; round++)
        {
            do
            {
                f.Apply(new EndTurnAction());
                if (f.Current == spirit)
                    played++;
            }
            while (f.Current != f.HeroFighter);
            f.HeroFighter!.Ap = 99;
        }
        Assert.Equal(2, played);
        Assert.False(spirit.IsAlive);
        Assert.Contains(f.Events.OfType<Died>(), d => d.Fighter == spirit.Id);
        Assert.True(me.IsAlive);
        // Gone, it frees its place: the caller may call it again.
        while (me.CooldownLeft(me.Spells.Single(s => s.Id == "path-spirit")) > 0)
            NextTurn(f);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-spirit", Room(f, me))));
    }

    [Fact]
    public void TheRumblingGround_StrikesTheEnemies_AndSpursTheAllies_ItIsCastOn()
    {
        (Fight f, Fighter me, Fighter foe) = Training(Druid("shaman", null, 40, null, "path-rumbling-ground"));
        Cell target = me.Cell.Neighbours().First(c => f.IsFree(c) && c.DistanceTo(foe.Cell) <= 2);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-rumbling-ground", target)));
        Assert.Equal([Affects.Enemies, Affects.Allies], f.Glyphs.Select(g => g.Effect.Affects));
        Assert.True(f.Glyphs[0].Effect.DamageMax > 0);
        Assert.True(f.Glyphs[^1].Effect.Welcomes);
    }

    [Fact]
    public void WhatSetsTheSpiritsApart_FromTheSummonersCreatures()
    {
        // docs/classes-conception.md §1.4: the Shaman's spirits help without holding a cell and last little; the Conjurer's creatures block the ground.
        SummonSpec spirit = Real.Summons["spirit"];
        Assert.True(spirit.Spirit);
        Assert.Equal(2, spirit.Lasts);
        Assert.All(Real.Summons.Values.Where(s => s.Id != "spirit"), s => Assert.False(s.Spirit || s.Lasts > 0, s.Id));
        // Every spell that calls a spirit is the Shaman's.
        string[] callers = [.. Real.Spells.Values.Where(s => s.AllEffects.OfType<SummonEffect>().Any(e => e.Summon == "spirit")).Select(s => s.Twin ?? s.Id)];
        Assert.Equal(["path-spirit"], callers.Distinct());
    }
}
