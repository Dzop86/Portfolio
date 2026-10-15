using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The families of D69 (sprints 91 to 94, docs/design-familles.md): each with its lesson, its family
/// passive, its weakness, a tank that protects or hinders rather than shields itself, a chief in two
/// phases; checked on the game's data.
/// </summary>
public class FamilyTests
{
    [Theory]
    [InlineData("boar", "herd-grudge", Element.Air, "woods-thicket")]
    [InlineData("wolf", "pack-instinct", Element.Fire, "woods-thicket")]
    [InlineData("mushroom", "final-spores", Element.Fire, "mouldy-pools", "spore-puffer")]
    [InlineData("zombie", "contagion-", Element.Water, "mouldy-pools", null, "bile-spitter")]
    [InlineData("frog", "damp-skin", Element.Earth, "lakeshore-pontoon")]
    [InlineData("crab", "molt", Element.Air, "lakeshore-pontoon", null, null, -10)]
    [InlineData("bee", "alarm", Element.Earth, "meadow-hedges")]
    [InlineData("mole", "bury", Element.Water, "meadow-hedges")]
    public void AFamily_SharesItsPassive_IsWeakToOneElement_AndFightsOnTwoMaps(string id, string passive, Element weakness, string second,
        string? ownPassive = null, string? strongInIt = null, int weak = -15)
    {
        MonsterFamily family = Real.Families[id];
        foreach (MonsterSpec m in family.Monsters)
        {
            // The puffer bursts instead (§4.3); each zombie's contagion is in its own element (§4.4).
            if (m.Id != ownPassive)
                Assert.Contains(m.Passives ?? [], p => p == passive || passive.EndsWith('-') && p.StartsWith(passive, StringComparison.Ordinal));
            // The bile spitter strikes in Water, its family's weakness: it resists it instead.
            if (m.Id == strongInIt)
            {
                Assert.True(m.Resistances![weakness] > 0);
                continue;
            }
            // -15 % for the monsters (-10 % for the armoured crabs, §4.6), -5 % for the chief (D69 §9, by default).
            Assert.Equal(m.Role == MonsterRole.Boss ? -5 : weak, m.Resistances![weakness]);
            Assert.True(m.Resistances.Count(r => r.Value < 0) == 1, $"{m.Id}: one weakness only");
        }
        // No more tank shielding itself and nothing else (D69 §1).
        Assert.DoesNotContain(family[MonsterRole.Tank].Spells.Select(s => Real.Spells[s]),
            s => s.AllEffects.Count > 0 && s.AllEffects.All(e => e is ShieldEffect { Affects: Affects.Caster }));
        // The chief changes when it is hurt.
        Assert.Contains(family[MonsterRole.Boss].Passives!, p => Real.Passives[p].Trigger == Trigger.LowHp);
        Assert.Equal([family.Battle, second], family.AllBattles);
        Real.Board(second);
    }

    [Theory]
    [InlineData("woods", "woods-glade", "woods-thicket")]
    [InlineData("mouldy", "mouldy-hollow", "mouldy-pools")]
    [InlineData("lake", "lakeshore", "lakeshore-pontoon")]
    [InlineData("fields", "meadow", "meadow-hedges")]
    public void TheTwoMapsOfAFamily_AreBothFoughtOn_EachGroupAlwaysOnTheSame(string zone, string first, string second)
    {
        MonsterGroup[] here = [.. Real.Groups.Values.Where(g => Real.Families[g.Family].Zone == zone && g.Fight is null)];
        string[] maps = [.. here.Select(g => Real.FindScenario(g.Scenario)!.Map).Distinct().Order()];
        Assert.Equal([first, second], maps);
    }

    /// <summary>
    /// A monster alone against a hero who strikes first with a blow of fixed damage, on a row of cells,
    /// with the game's spells, creatures, passives and items.
    /// </summary>
    private static Fight Against(string monster, string family, int hp, string row = "A.....B....", int damage = 6, int mp = 0)
    {
        Fight fight = Among(family, row, damage, mp, monster);
        fight.Fighters[1].Hp = hp;
        return fight;
    }

    /// <summary>Several monsters of a family, on the row's B cells in order, against the same hero.</summary>
    private static Fight Among(string family, string row, int damage, int mp, params string[] monsters)
    {
        FighterSpec hero = Spec(0, ap: 99, mp: mp, initiative: 999, spells: ["blow"]);
        var blow = new Spell("blow", Text, 1, 1, 9, false, false, damage, damage, 99);
        var data = new GameData([.. Real.Spells.Values, blow], [new MapSpec("m", Text, [row])],
            [new Scenario("s", Text, "m", [hero, .. monsters.Select((m, i) => Real.Monsters[m].Fighter(i, family: family))])],
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values);
        return new Fight(data, "s", 1);
    }

    [Fact]
    public void ThePackLeader_BelowHalfItsHitPoints_CallsTwoCubs()
    {
        MonsterSpec leader = Real.Monsters["pack-leader"];
        Fight f = Against("pack-leader", "wolf", leader.Hp / 2 + 3);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Summoned[] cubs = [.. f.Events.OfType<Summoned>()];
        Assert.Equal(2, cubs.Length);
        Assert.All(cubs, c => Assert.Equal("wolf-cub", f.Fighters[c.Fighter].SummonKind));
        Assert.Equal(70, Real.Summons["wolf-cub"].Scale);
        Assert.Equal("wolf-melee", Real.Summons["wolf-cub"].Look);
    }

    [Fact]
    public void TheBoarKing_BelowHalfItsHitPoints_GainsAnActionPointAndEarthResistance()
    {
        MonsterSpec king = Real.Monsters["boar-king"];
        Fight f = Against("boar-king", "boar", king.Hp / 2 + 3);
        int earth = f.Fighters[1].Resistance(Element.Earth);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new PassiveTriggered(1, "etched-shard"), f.Events);
        Assert.Equal(earth + 20, f.Fighters[1].Resistance(Element.Earth));
        f.Apply(new EndTurnAction());
        Assert.Equal(king.Ap + 1, f.Fighters[1].Ap);
    }

    [Fact]
    public void TheOldLoner_TakesAMovementPointFromWhoeverStrikesItUpClose()
    {
        Fight f = Against("old-loner", "boar", Real.Monsters["old-loner"].Hp, "AB", damage: 1, mp: 3);
        f.Apply(new CastAction("blow", new Cell(1, 0)));
        Assert.Equal(2, f.Fighters[0].Mp);
    }

    [Fact]
    public void TheElderFungus_BelowAThirdOfItsHitPoints_RootsItself()
    {
        MonsterSpec elder = Real.Monsters["elder-fungus"];
        Fight f = Against("elder-fungus", "mushroom", elder.Hp * 3 / 10 + 3);
        Fighter fungus = f.Fighters[1];
        int earth = fungus.Resistance(Element.Earth);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new PassiveTriggered(1, "network-retracts"), f.Events);
        Assert.Equal(elder.Hp * 3 / 10 + 3 - 6 + 30, fungus.Hp);
        Assert.Equal(earth + 30, fungus.Resistance(Element.Earth));
        f.Apply(new EndTurnAction());
        Assert.Equal(Math.Max(0, elder.Mp - 2), fungus.Mp);
    }

    [Fact]
    public void ThePuffer_Bursts_PoisoningTwoCellsAround_ButFinalSporesOnlyWhoIsNextToIt()
    {
        // The hero two cells from a puffer, then from a toadstool, each left with 1 hit point.
        Fight puffer = Among("mushroom", "A.B.B", 1, 0, "spore-puffer", "spore-puffer");
        puffer.Fighters[1].Hp = 1;
        puffer.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Contains(new StatusAdded(0, Stat.Poison, 4, 2, Element.Water), puffer.Events);
        Fight toadstool = Among("mushroom", "A.B.B", 1, 0, "bruiser-toadstool", "bruiser-toadstool");
        toadstool.Fighters[1].Hp = 1;
        toadstool.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Contains(new PassiveTriggered(1, "final-spores"), toadstool.Events);
        Assert.DoesNotContain(toadstool.Events, e => e is StatusAdded { Fighter: 0 });
    }

    [Theory]
    [InlineData("shambling-brute", Element.Fire)]
    [InlineData("bile-spitter", Element.Water)]
    [InlineData("bloated-zombie", Element.Earth)]
    [InlineData("bonesetter", Element.Air)]
    public void AZombie_PoisonsWhoeverStrikesItUpClose_InItsOwnElement_NotFromAfar(string zombie, Element element)
    {
        Fight close = Against(zombie, "zombie", Real.Monsters[zombie].Hp, "AB", damage: 1);
        close.Apply(new CastAction("blow", new Cell(1, 0)));
        Assert.Contains(new StatusAdded(0, Stat.Poison, 2, 2, element), close.Events);
        Fight far = Against(zombie, "zombie", Real.Monsters[zombie].Hp, "A.B", damage: 1);
        far.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.DoesNotContain(far.Events, e => e is StatusAdded { Fighter: 0 });
    }

    [Fact]
    public void TheBloatedZombie_Explodes_HurtingItsOwnAlliesToo()
    {
        Fight f = Among("zombie", "A.BB", 1, 0, "bloated-zombie", "shambling-brute");
        f.Fighters[1].Hp = 1;
        f.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Contains(new PassiveTriggered(1, "explosion"), f.Events);
        Assert.Contains(f.Events, e => e is Damaged { Fighter: 0, Element: Element.Earth });
        Assert.Contains(f.Events, e => e is Damaged { Fighter: 2, Element: Element.Earth });
    }

    [Fact]
    public void TheZombieOverlord_BelowHalfItsHitPoints_LetsABloaterComeAway()
    {
        MonsterSpec overlord = Real.Monsters["zombie-overlord"];
        Fight f = Against("zombie-overlord", "zombie", overlord.Hp / 2 + 3);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Summoned spawn = Assert.Single(f.Events.OfType<Summoned>());
        Assert.Equal("bloated-spawn", f.Fighters[spawn.Fighter].SummonKind);
        Assert.Equal(["explosion"], Real.Summons["bloated-spawn"].Passives);
        Assert.Equal(["final-spores"], Real.Summons["spore-sprout"].Passives);
    }

    [Fact]
    public void AFrog_HealsItselfAtTheStartOfItsTurn()
    {
        Fight f = Against("brawler-frog", "frog", 20);
        f.Apply(new EndTurnAction());
        Assert.Contains(new PassiveTriggered(1, "damp-skin"), f.Events);
        Assert.Equal(24, f.Fighters[1].Hp);
    }

    [Fact]
    public void TheBullfrog_PushesBackWhoeverStrikesItUpClose()
    {
        Fight f = Against("bullfrog", "frog", Real.Monsters["bullfrog"].Hp, ".AB.", damage: 1);
        f.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Equal(new Cell(0, 0), f.Fighters[0].Cell);
    }

    [Fact]
    public void TheToadKing_BelowHalfItsHitPoints_SingsAwayTwoActionPoints_OfWhoeverIsNear()
    {
        MonsterSpec king = Real.Monsters["toad-king"];
        Fight f = Against("toad-king", "frog", king.Hp / 2 + 3, "A..B");
        int ap = f.Fighters[0].Ap;
        f.Apply(new CastAction("blow", new Cell(3, 0)));
        Assert.Contains(new PassiveTriggered(1, "lullaby"), f.Events);
        // The hero playing loses them at once (D69 §2.1); the blow cost one.
        Assert.Equal(ap - 1 - 2, f.Fighters[0].Ap);
        Fight far = Against("toad-king", "frog", king.Hp / 2 + 3, "A....B");
        far.Apply(new CastAction("blow", new Cell(5, 0)));
        Assert.Equal(ap - 1, far.Fighters[0].Ap);
    }

    [Fact]
    public void ACrab_BelowTwoFifthsOfItsHitPoints_MoltsItsArmourForSpeedAndStrength()
    {
        MonsterSpec crab = Real.Monsters["pincer-crab"];
        Fight f = Against("pincer-crab", "crab", crab.Hp * 4 / 10 + 3);
        Fighter pincer = f.Fighters[1];
        int earth = pincer.Resistance(Element.Earth);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new PassiveTriggered(1, "molt"), f.Events);
        Assert.Equal(earth - 30, pincer.Resistance(Element.Earth));
        f.Apply(new EndTurnAction());
        Assert.Equal(crab.Mp + 2, pincer.Mp);
    }

    [Fact]
    public void TheGiantCrab_BelowHalfItsHitPoints_ShieldsItselfAndAHermitJoinsIt()
    {
        MonsterSpec giant = Real.Monsters["giant-crab"];
        Fight f = Against("giant-crab", "crab", giant.Hp / 2 + 3);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new Shielded(1, 40, 2), f.Events);
        Summoned hermit = Assert.Single(f.Events.OfType<Summoned>());
        Assert.Equal("hermit-spawn", f.Fighters[hermit.Fighter].SummonKind);
        Assert.Equal(70, Real.Summons["hermit-spawn"].Scale);
    }

    [Fact]
    public void ABee_StruckAlertsTheBeesThreeCellsAround_NotFarther()
    {
        Fight f = Among("bee", "A.BB..B", 1, 0, "worker-bee", "archer-bee", "archer-bee");
        f.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Contains(new PassiveTriggered(1, "alarm"), f.Events);
        Assert.Contains(f.Events, e => e is StatusAdded { Fighter: 1, Stat: Stat.Damage, Value: 20 });
        Assert.Contains(f.Events, e => e is StatusAdded { Fighter: 2, Stat: Stat.Damage, Value: 20 });
        Assert.DoesNotContain(f.Events, e => e is StatusAdded { Fighter: 3 });
    }

    [Fact]
    public void TheQueen_BelowHalfItsHitPoints_Overheats()
    {
        MonsterSpec queen = Real.Monsters["queen-bee"];
        Fight f = Against("queen-bee", "bee", queen.Hp / 2 + 3);
        int air = f.Fighters[1].Resistance(Element.Air);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new PassiveTriggered(1, "overheat"), f.Events);
        Assert.Equal(air - 15, f.Fighters[1].Resistance(Element.Air));
        Assert.Contains(f.Events, e => e is StatusAdded { Fighter: 1, Stat: Stat.Damage, Value: 30 });
    }

    [Fact]
    public void AMole_BelowHalfItsHitPoints_BurrowsDown_ForOneTurn()
    {
        MonsterSpec digger = Real.Monsters["digger-mole"];
        Fight f = Against("digger-mole", "mole", digger.Hp / 2 + 3);
        int earth = f.Fighters[1].Resistance(Element.Earth);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Equal(earth + 40, f.Fighters[1].Resistance(Element.Earth));
        f.Apply(new EndTurnAction());
        Assert.Equal(digger.Mp + 2, f.Fighters[1].Mp);
        // A mole surfaces anywhere it can land, out of sight too.
        Spell surface = Real.Spells["surface"];
        Assert.False(surface.LineOfSight);
        Assert.Contains(surface.AllEffects, e => e is LeapEffect);
    }

    [Fact]
    public void TheMoleKing_BelowHalfItsHitPoints_DigsUpTheDebris_AndTwoDiggersSurface()
    {
        MonsterSpec king = Real.Monsters["mole-king"];
        Fight f = Against("mole-king", "mole", king.Hp / 2 + 3);
        f.Apply(new CastAction("blow", new Cell(6, 0)));
        Assert.Contains(new PassiveTriggered(1, "the-debris"), f.Events);
        Summoned[] diggers = [.. f.Events.OfType<Summoned>()];
        Assert.Equal(2, diggers.Length);
        Assert.All(diggers, d => Assert.Equal("digger-spawn", f.Fighters[d.Fighter].SummonKind));
        f.Apply(new EndTurnAction());
        Assert.Equal(king.Ap + 2, f.Fighters[1].Ap);
    }

    [Fact]
    public void EveryDungeonsGreatHall_IsFoughtOnItsFamilysSecondMap()
    {
        foreach (Dungeon d in Real.Dungeons.Values)
            Assert.Equal(Real.Maps[Real.Families[d.Family].AllBattles[1]].Rows, Real.Maps[d.Rooms[1]].Rows);
    }

    [Fact]
    public void TheEightSpells_OfTheTanksThatOnlyShieldedThemselves_AreGone()
    {
        string[] gone = ["mud-wallow", "thick-fur", "hard-cap", "rotting-hide", "plague-wave", "toad-skin", "wax-coat", "burrow"];
        Assert.DoesNotContain(Real.Spells.Keys, gone.Contains);
    }
}
