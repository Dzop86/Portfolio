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
    public void AFamily_SharesItsPassive_IsWeakToOneElement_AndFightsOnTwoMaps(string id, string passive, Element weakness, string second)
    {
        MonsterFamily family = Real.Families[id];
        foreach (MonsterSpec m in family.Monsters)
        {
            Assert.Contains(passive, m.Passives ?? []);
            // -15 % for the monsters, -5 % for the chief (D69 §9, by default).
            Assert.Equal(m.Role == MonsterRole.Boss ? -5 : -15, m.Resistances![weakness]);
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

    [Fact]
    public void TheTwoMapsOfAFamily_AreBothFoughtOn_EachGroupAlwaysOnTheSame()
    {
        MonsterGroup[] woods = [.. Real.Groups.Values.Where(g => g.Family is "boar" or "wolf" && g.Fight is null)];
        string[] maps = [.. woods.Select(g => Real.FindScenario(g.Scenario)!.Map).Distinct().Order()];
        Assert.Equal(["woods-glade", "woods-thicket"], maps);
    }

    /// <summary>
    /// A monster alone against a hero who strikes first with a blow of fixed damage, on a row of cells,
    /// with the game's spells, creatures, passives and items.
    /// </summary>
    private static Fight Against(string monster, string family, int hp, string row = "A.....B....", int damage = 6, int mp = 0)
    {
        FighterSpec hero = Spec(0, ap: 99, mp: mp, initiative: 999, spells: ["blow"]);
        var blow = new Spell("blow", Text, 1, 1, 9, false, false, damage, damage, 99);
        var data = new GameData([.. Real.Spells.Values, blow], [new MapSpec("m", Text, [row])],
            [new Scenario("s", Text, "m", [hero, Real.Monsters[monster].Fighter(0, family: family)])],
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values);
        var fight = new Fight(data, "s", 1);
        fight.Fighters[1].Hp = hp;
        return fight;
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
}
