using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The rockdelvers (sprint 225, docs/familles-mines.md §3, words of docs/familles-mines-textes.md): the Underroot Mines' first
/// family, whose lesson is backing off in time: the shotfirer lays a powder keg beside it, which blows at its second turn,
/// two cells around, on everyone; the timberman wedges the hero so that it cannot leave; a rockdelver standing still strikes harder.
/// </summary>
public class DelverTests
{
    private static MonsterFamily Delvers => Real.Families["delver"];

    private static readonly Hero Guard = Progression.Built(new Hero("Essai", "female-d", "warrior", Level: 46), Real);

    /// <summary>The smallest group with a shotfirer, and a fight against it where everyone only ends its turn until the shotfirer plays.</summary>
    private static (Fight Fight, Fighter Shotfirer) AtTheShotfirersTurn(Func<MonsterGroup, bool>? also = null)
    {
        MonsterGroup group = Real.Groups.Values.Where(g => g.Family == "delver" && g.Monsters.Contains("shotfirer") && (also?.Invoke(g) ?? true))
            .OrderBy(g => g.Monsters.Count).ThenBy(g => g.Id, StringComparer.Ordinal).First();
        var fight = new Fight(Real, group.Scenario, 1, Guard);
        Fighter shotfirer = fight.Fighters.First(f => f.Spec.Look == "delver-melee");
        while (fight.Current != shotfirer)
            fight.Apply(new EndTurnAction());
        return (fight, shotfirer);
    }

    /// <summary>The hero put beside the shotfirer, the keg laid on another free cell beside it; the keg is returned.</summary>
    private static Fighter LayAKeg(Fight fight, Fighter shotfirer, bool heroBeside = true)
    {
        Fighter hero = fight.HeroFighter!;
        if (heroBeside)
            hero.Cell = shotfirer.Cell.Neighbours().First(fight.IsFree);
        Cell spot = shotfirer.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("powder-charge", spot)));
        Summoned laid = fight.Events.OfType<Summoned>().Last();
        Assert.Equal((shotfirer.Id, spot), (laid.Summoner, laid.Cell));
        return fight.Fighters[laid.Fighter];
    }

    [Fact]
    public void TheDelvers_LiveInTheMines_TheCommonOnesOf46To49_TheirChiefOf60_SharingTheMinesTwoMapsWithTheWorms()
    {
        Assert.Equal(("underroot", "underroot-0-4"), (Delvers.Zone, Delvers.BossMap));
        Assert.Equal([46, 47, 48, 49], Delvers.Monsters.Where(m => m.Role != MonsterRole.Boss).Select(m => m.Level).Order());
        Assert.Equal(60, Delvers[MonsterRole.Boss].Level);
        Assert.Equal(["underroot-gallery", "underroot-pillars"], Delvers.AllBattles);
        Assert.Contains(Real.Groups.Values, g => g.Family == "delver" && g.Map == Delvers.BossMap && g.Monsters.Contains("stonelistener"));
        // No slow tank here: a timberman of 2 movement points would let the hero walk out of a keg without trying (§3.1).
        Assert.All(Delvers.Monsters, m => Assert.Equal(3, m.Mp));
        foreach (MonsterSpec m in Delvers.Monsters)
            Assert.Equal(Templates.Initiative(m.Role, m.Level), m.Initiative);
        // The one gap from the templates: the shotfirer's pick costs 4 AP, once a turn, so that its 2 left lay the keg (§0.1, point 2).
        Spell pick = Real.Spells["pick-blow"];
        Assert.Equal((4, 1), (pick.ApCost, pick.PerTurn));
        Assert.Equal(6, pick.ApCost + Real.Spells["powder-charge"].ApCost);
    }

    [Fact]
    public void TheKegs_HaveNoSpellNorPoint_TheirFuseBurnsAHitPoint_TheirBlastStrikesEveryone()
    {
        foreach ((string id, string blast, int min, int max) in new[] { ("powder-keg", "keg-blast", 20, 26), ("powder-barrel", "barrel-blast", 34, 42) })
        {
            SummonSpec keg = Real.Summons[id];
            Assert.Equal((2, 0, 0), (keg.Hp, keg.Ap, keg.Mp));
            Assert.Empty(keg.Spells);
            Assert.Equal(["short-fuse", blast], keg.Passives!);
            PassiveSpec b = Real.Passives[blast];
            Assert.Equal((Trigger.Death, AreaShape.Circle, 2, min, max, Element.Fire), (b.Trigger, b.Area!.Shape, b.Area.Radius, b.DamageMin, b.DamageMax, b.Element));
        }
        PassiveSpec fuse = Real.Passives["short-fuse"];
        Assert.Equal((Trigger.TurnStart, 1, 1, (Area?)null), (fuse.Trigger, fuse.DamageMin, fuse.DamageMax, fuse.Area));
        // The scriptwriter's words: a keg, not a "charge" (the Warrior's spell), and the miners' fuse, not the gardener's cord.
        Assert.Equal(("Tonnelet de poudre", "Powder Keg"), (Real.Spells["powder-charge"].Name.Fr, Real.Spells["powder-charge"].Name.En));
        Assert.Equal("Tonnelet de poudre", Real.Summons["powder-keg"].Name.Fr);
        Assert.Equal(("Mèche courte", "Bout de mèche", "Morceau de filon"), (fuse.Name.Fr, Real.Items["fuse-cord"].Name.Fr, Real.Items["lode-chip"].Name.Fr));
    }

    [Fact]
    public void APowderKeg_LaidBesideTheShotfirer_LosesAHitPoint_ThenBlowsAtItsSecondTurn()
    {
        (Fight fight, Fighter shotfirer) = AtTheShotfirersTurn();
        Fighter keg = LayAKeg(fight, shotfirer);
        Fighter hero = fight.HeroFighter!;
        // It plays right after its shotfirer: its fuse burns at once.
        fight.Apply(new EndTurnAction());
        Assert.Equal((keg, 1), (fight.Current, keg.Hp));
        // Then everyone, the hero once, plays before its second turn, where it blows.
        bool heroPlayed = false;
        for (int turns = 0; keg.IsAlive && turns < 20; turns++)
        {
            heroPlayed |= fight.Current == hero;
            fight.Apply(new EndTurnAction());
        }
        Assert.True(heroPlayed);
        Assert.False(keg.IsAlive);
        int blast = fight.Events.ToList().FindIndex(e => e is PassiveTriggered { Passive: "keg-blast" } p && p.Fighter == keg.Id);
        Assert.True(blast >= 0);
        Damaged[] hits = [.. fight.Events.Skip(blast).OfType<Damaged>().Where(d => d.Element == Element.Fire)];
        // The hero and the shotfirer both stand beside the keg: both are hit.
        Assert.Contains(hits, d => d.Fighter == hero.Id);
        Assert.Contains(hits, d => d.Fighter == shotfirer.Id);
    }

    [Fact]
    public void APowderKeg_BlowsAtOnce_WhenItsShotfirerFalls_UnlessThatEndsTheFight()
    {
        foreach (bool alone in new[] { false, true })
        {
            (Fight fight, Fighter shotfirer) = AtTheShotfirersTurn(g => alone ? g.Monsters.Count == 1 : g.Monsters.Count > 1);
            Fighter keg = LayAKeg(fight, shotfirer);
            Fighter hero = fight.HeroFighter!;
            while (fight.Current != hero)
                fight.Apply(new EndTurnAction());
            Assert.True(keg.IsAlive);
            shotfirer.Hp = 1;
            hero.Ap = 12;
            Spell blow = hero.Spells.First(s => s.MinRange <= 1 && s.DamageMin > 0 && fight.CheckCast(hero, s, hero.Cell, shotfirer.Cell) == ActionError.None);
            Assert.Equal(ActionError.None, fight.Apply(new CastAction(blow.Id, shotfirer.Cell)));
            Assert.False(shotfirer.IsAlive);
            Assert.False(keg.IsAlive);
            Assert.Equal(!alone, fight.Events.Any(e => e is PassiveTriggered { Passive: "keg-blast" }));
        }
    }

    [Fact]
    public void APowderKeg_StruckOrPushed_BlowsWhereItStops()
    {
        (Fight fight, Fighter shotfirer) = AtTheShotfirersTurn(g => g.Monsters.Count > 1);
        Fighter keg = LayAKeg(fight, shotfirer);
        Fighter hero = fight.HeroFighter!;
        while (fight.Current != hero)
            fight.Apply(new EndTurnAction());
        hero.Cell = keg.Cell.Neighbours().FirstOrDefault(fight.IsFree, hero.Cell);
        hero.Ap = 12;
        Spell blow = hero.Spells.First(s => s.DamageMin > 0 && fight.CheckCast(hero, s, hero.Cell, keg.Cell) == ActionError.None);
        Cell at = keg.Cell;
        Assert.Equal(ActionError.None, fight.Apply(new CastAction(blow.Id, keg.Cell)));
        Assert.False(keg.IsAlive);
        Assert.Contains(fight.Events, e => e is PassiveTriggered { Passive: "keg-blast" } p && p.Fighter == keg.Id);
        // Not anchored: if the blow pushed it, it blew where it stopped, never back where it stood.
        Pushed? pushed = fight.Events.OfType<Pushed>().LastOrDefault(p => p.Fighter == keg.Id);
        Assert.Equal(pushed is null ? at : pushed.Path[^1], keg.Cell);
    }

    [Fact]
    public void Dogged_FiresOnTheFirstTurn_AndAfterAStillTurn_NotAfterAWalk()
    {
        PassiveSpec dogged = Real.Passives["dogged"];
        Assert.True(dogged.Still);
        Assert.Equal(Trigger.TurnStart, dogged.Trigger);
        Assert.All(Delvers.Monsters, m => Assert.Contains("dogged", m.Passives!));
        (Fight fight, Fighter shotfirer) = AtTheShotfirersTurn();
        int Fired() => fight.Events.Count(e => e is PassiveTriggered { Passive: "dogged" } p && p.Fighter == shotfirer.Id);
        // Its first turn: it has not walked yet.
        Assert.Equal(1, Fired());
        // It stands still this turn: it fires again at its next.
        do fight.Apply(new EndTurnAction()); while (fight.Current != shotfirer);
        Assert.Equal(2, Fired());
        // It walks a cell: no more at its next.
        Cell step = shotfirer.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new MoveAction(step)));
        do fight.Apply(new EndTurnAction()); while (fight.Current != shotfirer);
        Assert.Equal(2, Fired());
    }

    [Fact]
    public void Wedging_TakesAMovementPoint_AtTheHerosNextTurn_AndSwig_HealsAndStrengthensTheAlly()
    {
        StatusEffect wedge = Assert.Single(Real.Spells["wedging"].AllEffects.OfType<StatusEffect>());
        Assert.Equal((Affects.Enemies, Stat.Mp, -1, 1), (wedge.Affects, wedge.Stat, wedge.Value, wedge.Turns));
        Spell swig = Real.Spells["swig"];
        Assert.Contains(swig.AllEffects, e => e is HealEffect { Affects: Affects.Allies, Min: 21, Max: 27 });
        Assert.Contains(swig.AllEffects, e => e is StatusEffect { Affects: Affects.Allies, Stat: Stat.Damage, Value: 15, Turns: 1 });
        MonsterGroup group = Real.Groups.Values.Where(g => g.Family == "delver" && g.Monsters.Contains("timberman"))
            .OrderBy(g => g.Monsters.Count).ThenBy(g => g.Id, StringComparer.Ordinal).First();
        var fight = new Fight(Real, group.Scenario, 1, Guard);
        Fighter timberman = fight.Fighters.First(f => f.Spec.Look == "delver-tank"), hero = fight.HeroFighter!;
        while (fight.Current != timberman)
            fight.Apply(new EndTurnAction());
        hero.Cell = timberman.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("wedging", hero.Cell)));
        while (fight.Current != hero)
            fight.Apply(new EndTurnAction());
        Assert.Equal(hero.Spec.Mp - 1, hero.Mp);
    }

    [Fact]
    public void TheStonelistener_BelowHalf_GainsTwoActionPoints_LaysTwoKegsAtItsFeet()
    {
        PassiveSpec ringing = Real.Passives["ringing-stone"];
        Assert.Equal((Trigger.LowHp, 50), (ringing.Trigger, ringing.Percent));
        Assert.Contains(ringing.Effects!, e => e is StatusEffect { Stat: Stat.Ap, Value: 2 });
        Assert.Contains(ringing.Effects!, e => e is StatusEffect { Stat: Stat.Resistance, Value: -10 });
        SummonEffect kegs = Assert.Single(ringing.Effects!.OfType<SummonEffect>());
        Assert.Equal(("powder-keg", 2), (kegs.Summon, kegs.Count));
        // The rock rings, it shows no strata: those are the mountain gnawer's (docs/familles-mines-textes.md §2, point 2).
        Assert.DoesNotContain("couche", ringing.Description.Fr, StringComparison.Ordinal);
        Assert.Equal(["great-pick", "powder-barrel", "lamp-hurl"], Delvers[MonsterRole.Boss].Spells);
    }

    [Fact]
    public void TheSet_HasFourPieces_TheBootsWithInitiative_ThePickTwoHanded_AndTheChiefDropsGold()
    {
        ItemSet set = Real.Sets["delver"];
        Assert.Equal([2, 3, 4], set.Bonuses.Select(b => b.Pieces));
        Assert.Equal((0, 1), (set.Bonuses[^1].Ap, set.Bonuses[^1].Mp));
        Assert.Equal([ItemSlot.Boots, ItemSlot.Hat, ItemSlot.Shoulders, ItemSlot.TwoHanded], Real.Items.Values.Where(i => i.Set == "delver").Select(i => i.Slot));
        Assert.Equal(("Bottes cloutées", "Hobnailed Boots"), (Real.Items["gallery-boots"].Name.Fr, Real.Items["gallery-boots"].Name.En));
        foreach (MonsterSpec m in Delvers.Monsters)
        {
            int drops = (m.Loot ?? []).Count(l => Real.Items[l.Item].Set == "delver");
            Assert.Equal(m.Role switch { MonsterRole.Boss => 4, MonsterRole.Support => 0, _ => 1 }, drops);
        }
        Assert.Contains(Delvers[MonsterRole.Boss].Loot!, l => l.Item == "gold-ore");
    }

    [Fact]
    public void AFightWithKegs_ReplaysIdentically()
    {
        // Played by hand, not by the AI: the suite's budget of fights is full (D96, REVIEW.md, sprint 222). Only actions, the hero
        // left where it stands: a record holds actions, not a fighter moved by a test.
        (Fight fight, Fighter shotfirer) = AtTheShotfirersTurn(g => g.Monsters.Count > 1);
        Fighter keg = LayAKeg(fight, shotfirer, heroBeside: false);
        for (int turns = 0; keg.IsAlive && turns < 20; turns++)
            fight.Apply(new EndTurnAction());
        Assert.Contains(fight.Events, e => e is PassiveTriggered { Passive: "keg-blast" });
        Assert.Equal(Fingerprint(fight), Fingerprint(FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(Real)));
    }
}
