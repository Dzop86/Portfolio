using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The yardsticks of the levels after 30 (sprint 128, D79, T96): the monsters' templates, the budget of the pieces and
/// the rhythm of experience up to level 45 (60 since sprint 161), and T41 measured on a family made from the templates.
/// </summary>
public class TemplateTests
{
    private static readonly string[] Classes = ["sentinel", "guard", "mage"];

    [Theory]
    [InlineData(MonsterRole.Melee, 70)]
    [InlineData(MonsterRole.Ranged, 85)]
    [InlineData(MonsterRole.Tank, 45)]
    [InlineData(MonsterRole.Support, 60)]
    [InlineData(MonsterRole.Boss, 60)]
    public void AMonstersInitiative_IsItsRolesBase_TimesOnePlusItsLevelOverForty(MonsterRole role, int at0)
    {
        Assert.Equal(at0, Templates.Initiative(role, 0));
        Assert.Equal(at0 * 2, Templates.Initiative(role, 40));
        Assert.True(Templates.Initiative(role, 45) > Templates.Initiative(role, 35));
    }

    [Fact]
    public void TheTemplates_GrowWithTheLevel_TheChiefAboveAll_TheTankSturdierThanTheRanged()
    {
        for (int level = Templates.FirstChapter; level <= 60; level++)
        {
            MonsterTemplate[] t = [.. Enum.GetValues<MonsterRole>().Select(r => Templates.Monster(r, level))];
            MonsterTemplate melee = t[0], ranged = t[1], tank = t[2], support = t[3], boss = t[4];
            Assert.True(boss.Hp > 5 * tank.Hp && tank.Hp > melee.Hp && melee.Hp > ranged.Hp, $"level {level}");
            Assert.True(boss.Damage > melee.Damage && melee.Damage > ranged.Damage && support.Heal > 0 && melee.Heal == 0, $"level {level}");
            Assert.True(tank.Resistance > melee.Resistance && t.All(x => x.Weakness < 0), $"level {level}");
            Assert.True(melee.Roll.Min < melee.Damage && melee.Damage < melee.Roll.Max);
            if (level > Templates.FirstChapter)
            {
                MonsterTemplate before = Templates.Monster(MonsterRole.Melee, level - 1);
                Assert.True(melee.Hp >= before.Hp && melee.Damage >= before.Damage && melee.Initiative > before.Initiative);
            }
        }
    }

    [Fact]
    public void TheTemplates_AtLevelThirty_AreAboutTheFieldsFamilies()
    {
        // Drawn from the bees and the moles (levels 24 to 30): a template is within a fifth of their average, carried on to 30.
        foreach (MonsterRole role in new[] { MonsterRole.Melee, MonsterRole.Ranged, MonsterRole.Tank, MonsterRole.Support })
        {
            MonsterSpec[] fields = [.. Real.Families.Values.Where(f => f.Zone == "fields").Select(f => f[role])];
            double hp = fields.Average(m => m.Hp);
            Assert.InRange(Templates.Monster(role, 30).Hp, hp * 0.8, hp * 1.4);
        }
        Assert.InRange(Templates.Monster(MonsterRole.Boss, 30).Hp, 370, 400);
    }

    [Fact]
    public void AMonsterAfterThirty_HasItsRolesInitiative_OrTheDataIsRefused()
    {
        // Up to 30 the first chapter's families stay as written (the bees' melee: 95 for 70 × 1.6 = 112).
        Assert.True(Real.Monsters.Values.All(Templates.InitiativeFits));
        MonsterFamily mole = Real.Families["mole"];
        MonsterSpec deep = mole[MonsterRole.Melee] with { Level = 35, Initiative = Templates.Initiative(MonsterRole.Melee, 35) };
        Assert.True(Templates.InitiativeFits(deep));
        Assert.True(Templates.InitiativeFits(deep with { Initiative = deep.Initiative * 109 / 100 }));
        Assert.False(Templates.InitiativeFits(deep with { Initiative = 60 }));
        MonsterFamily slow = mole with { Monsters = [.. mole.Monsters.Select(m => m.Role == MonsterRole.Melee ? deep with { Initiative = 60 } : m)] };
        var e = Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, Real.Families.Values.Select(f => f.Id == "mole" ? slow : f),
            Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values));
        Assert.Contains("initiative 60", e.Message);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(35)]
    [InlineData(40)]
    [InlineData(45)]
    [InlineData(50)]
    [InlineData(55)]
    [InlineData(60)]
    public void ThePaceOfExperience_StaysAboutTenMonstersOfOnesLevel_ALevel(int level)
    {
        // As in the first chapter's zones (9.6 at 26): 100 × level to go up, 20 + 10 × level a monster.
        Assert.InRange(Templates.MonstersPerLevel(level), 9, 10.5);
        Assert.InRange(Templates.MonstersPerLevel(26), 9, 10.5);
    }

    [Theory]
    [InlineData("sentinel", "rockfall", "searing-shot", "venom-arrow")]
    [InlineData("guard", "quake", "smite", "groundswell")]
    [InlineData("mage", "landslide", "meteor", "freeze")]
    public void EveryClass_OpensASpellAtThirtyFive_Forty_AndFortyFive(string id, string at35, string at40, string at45)
    {
        HeroClass c = Real.Class(id)!;
        string[] opened = [.. c.Spells.Where(s => Real.Spells[s].Level is > 30 and <= 45).OrderBy(s => Real.Spells[s].Level)];
        Assert.Equal([at35, at40, at45], opened);
        Assert.Equal([35, 40, 45], opened.Select(s => Real.Spells[s].Level));
    }

    [Theory]
    [InlineData("sentinel", "gale", "focus", "stake")]
    [InlineData("guard", "cyclone-blade", "war-cry", "titan-blow")]
    [InlineData("mage", "cyclone", "renewal", "fault-line")]
    public void EveryClass_OpensASpellAtFifty_FiftyFive_AndSixty(string id, string at50, string at55, string at60)
    {
        // Sprint 161 (D83): a spell of a fourth element at 50, a spell that helps at 55, a heavy blow of Earth at 60.
        HeroClass c = Real.Class(id)!;
        string[] opened = [.. c.Spells.Where(s => Real.Spells[s].Level is > 45 and <= 60).OrderBy(s => Real.Spells[s].Level)];
        Assert.Equal([at50, at55, at60], opened);
        Assert.Equal([50, 55, 60], opened.Select(s => Real.Spells[s].Level));
    }

    // ---- T41 on a family made from the templates ----

    [Theory]
    [InlineData(35)]
    [InlineData(40)]
    [InlineData(45)]
    [InlineData(50)]
    [InlineData(55)]
    [InlineData(60)]
    public void AFamilyMadeFromTheTemplates_KeepsT41_SmallGroupsForOnePlayer_HordesForSeveral(int level)
    {
        var family = new SyntheticFamily(level);
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none");
            var few = family.Groups.Where(g => g.Size <= 3).ToArray();
            int[] small = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, bare, 30))];
            // From 50 (sprint 161, D83): the hero without a specialisation keeps 29 of 30 and at most 2 hordes of 10 bare,
            // the margin left to the specialisations of S163 to S165.
            kept &= small.Average() >= (level >= 50 ? 29 : 27) && small.Count(w => w < 18) <= 2;
            string under = string.Join(", ", few.Zip(small).Where(x => x.Second < 18).Select(x => $"{x.First.Scenario} {x.Second}"));
            report.Add($"{id}: small {small.Average():0.0} of 30 ({small.Count(w => w < 18)} under 18: {under})");
            foreach (string kit in new[] { "none", "zone", "craft" })
            {
                Hero worn = family.Hero(id, kit);
                int[] large = [.. family.Groups.Where(g => g.Size >= 6 && !(kit == "craft" && g.InTheOpen)).AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, worn, 10))];
                kept &= large.Average() <= (kit == "none" && level < 50 ? 3 : 2);
                report.Add($"{id}: hordes, kit {kit}, {large.Average():0.0} of 10");
            }
        }
        Assert.True(kept, $"level {level}: " + string.Join("; ", report));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(60)]
    public void AHeroOfOneElement_AtTheMonstersLevel_BeatsNoMoreHordesThanT41Allows(int level)
    {
        // The question left open in T110 (sprint 144): at 45 the Forest's hordes (31 to 34) fell to a bare hero of one
        // element; at the monsters' own level, measured at 50 and 60 (sprint 161), each element of each class, bare and
        // with each kit, it beat too many (1.2 to 7.7 of 10). The ceiling of this strongest hero (D85): 3 of 10 with each kit
        // against the solid hordes, held since the elements' returns (sprint 162, T129: a quarter past 3 × level + 5, Rockfall
        // at 6 AP); the light ones are printed.
        var family = new SyntheticFamily(level);
        var hordes = family.Groups.Where(g => g.Size >= 6 && family.Solid(g.Scenario)).ToArray();
        var light = family.Groups.Where(g => g.Size >= 6 && !family.Solid(g.Scenario)).ToArray();
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
            foreach (string kit in new[] { "none", "zone", "craft" })
            {
                Hero worn = family.Hero(id, kit);
                var line = new List<string>();
                foreach (Element e in new[] { Element.Earth, Element.Fire, Element.Water, Element.Air })
                {
                    Hero one = Progression.OfOne(family.Hero(id, "none"), e, family.Data, worn.Worn);
                    double large = hordes.Where(g => !(kit == "craft" && g.InTheOpen)).AsParallel().Select(g => family.Wins(g.Scenario, one, 40)).Average() / 4;
                    double easy = light.AsParallel().Select(g => family.Wins(g.Scenario, one, 40)).Average() / 4;
                    kept &= large <= 3;
                    line.Add($"{e} {large:0.0} (light {easy:0.0})");
                }
                report.Add($"{id} {kit}: " + string.Join(", ", line));
            }
        Assert.True(kept, $"level {level}, {hordes.Length} solid hordes: " + string.Join("; ", report));
    }

    [Fact]
    public void TheTalentsOfTheSlice_KeepT41AtThirtyFive_AndGiveTheSentinelAtMostTenPercentMore()
    {
        // Sprint 137 (D80, T103): the Sentinel of 35 with Up close (Bogging, the slice's other talent, is every hero's since sprint 182), bare against the small groups, wearing the
        // kits against the hordes; the tree is worth at most +10 % of victories against the small groups (§2.3).
        var family = new SyntheticFamily(35);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        Hero bare = family.Hero("sentinel", "none");
        Hero tree = bare with { Talents = ["up-close"] };
        int[] without = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, bare, 30))];
        int[] with = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, tree, 30))];
        string report = $"small {without.Average():0.0} without, {with.Average():0.0} with ({with.Count(w => w < 18)} under 18)";
        Assert.True(with.Average() >= 27 && with.Count(w => w < 18) <= 2, report);
        Assert.True(with.Average() <= without.Average() + 3, report);
        foreach (string kit in new[] { "none", "zone", "craft" })
        {
            Hero worn = family.Hero("sentinel", kit) with { Talents = ["up-close"] };
            double large = family.Groups.Where(g => g.Size >= 6 && !(kit == "craft" && g.InTheOpen)).AsParallel().Select(g => family.Wins(g.Scenario, worn, 10)).Average();
            Assert.True(large <= (kit == "none" ? 3 : 2), $"hordes, kit {kit}, {large:0.0} of 10; {report}");
        }
    }

    [Theory]
    [InlineData(35)]
    [InlineData(40)]
    [InlineData(45)]
    public void TheReferenceBuilds_KeepT41_TheTreeWorthAtMostTenPercent_TheFamiliesEven(int level)
    {
        // Sprint 144 (D80, T110, docs/talents-arbre.md §4.4): each class bare, with its build of Matter and of Manner, 30
        // fights a small group (at most 3 more victories with the tree, the two families within 1.5 of each other), 10 a
        // horde with the craftsmen's kit. A Conversion makes a hero strike on all its points, like one who plays a single
        // element without a talent, who already beats more hordes than T41's mixed hero (Charles, sprint 144): the tree is
        // held to the best bare hero, mixed or of one element, give or take 1.5 (the noise of 10 fights a horde).
        var family = new SyntheticFamily(level);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        // Against the solid hordes since sprint 162 (T129), and no lower than 3 of 10: held to the hero of one element alone,
        // the tree followed it down when the elements' returns brought it under the ceiling.
        var hordes = family.Groups.Where(g => g.Size >= 6 && !g.InTheOpen && family.Solid(g.Scenario)).ToArray();
        double Large(Hero h) => hordes.AsParallel().Select(g => family.Wins(g.Scenario, h, 10)).Average();
        double Small(Hero h) => few.AsParallel().Select(g => family.Wins(g.Scenario, h, 30)).Average();
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none"), craft = family.Hero(id, "craft");
            double without = Small(bare);
            double best = new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }.Select(e => Large(Progression.OfOne(family.Hero(id, "none"), e, family.Data, craft.Worn))).Append(Large(craft)).Max();
            var line = new List<string> { $"{id} bare {without:0.0} (best horde {best:0.0})" };
            var families = new List<double>();
            // Since sprint 147 (J4 b), the build of Pure element too: a Heart and a style, held to the same ceilings.
            foreach (string build in new[] { "matter", "manner", "pure" })
            {
                double small = Small(Progression.Talented(bare, $"{id}-{build}", family.Data));
                if (build != "pure")
                    families.Add(small);
                double large = Large(Progression.Talented(craft, $"{id}-{build}", family.Data, craft.Worn));
                kept &= small >= 27 && small <= without + 3 && large <= Math.Max(best + 1.5, 3);
                line.Add($"{build} {small:0.0} (hordes {large:0.0})");
            }
            kept &= Math.Abs(families[0] - families[1]) <= 1.5;
            report.Add(string.Join(", ", line));
        }
        Assert.True(kept, $"level {level}: " + string.Join("; ", report));
    }

    // Each style with a node of each floor up to its key (J3 a): the key a player alone would take (Clear Line, not Artillery,
    // which strikes nothing at 1 or 2 cells). Support is not here: up to -30 % of one's own damage to help an ally, it is a
    // false decision alone by design, and measured in a duo (PartyBalanceTests). The styles have no door since sprint 182.
    private static readonly Dictionary<string, string[]> Styles = new()
    {
        ["Alchemy"] = ["lasting-mark", "wave"],
        ["Melee"] = ["up-close", "heavy-push", "close-quarters"],
        ["Distance"] = ["long-sight", "tight-zone", "clear-line"],
        ["Terrain"] = ["side-step", "seal", "scorched-earth"],
        ["Tempo"] = ["heavy-blows", "short-cooldown", "one-gesture"],
    };

    [Fact]
    public void AtFortyFive_TheStyles_AreWorthAlike()
    {
        // Sprint 147 (D82, J3 c): no style a false decision: each within 1.5 victories of 30 of the others' average, against
        // the small groups, bare, for each class.
        var family = new SyntheticFamily(45);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        double Small(Hero h) => few.AsParallel().Select(g => family.Wins(g.Scenario, h, 30)).Average();
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none");
            Dictionary<string, double> values = Styles.ToDictionary(x => x.Key, x => Small(Progression.Talented(bare, x.Value, family.Data)));
            foreach ((string name, double v) in values)
                kept &= Math.Abs(v - values.Where(o => o.Key != name).Average(o => o.Value)) <= 1.5;
            report.Add($"{id} styles: " + string.Join(", ", values.Select(x => $"{x.Key} {x.Value:0.0}")));
        }
        Assert.True(kept, string.Join("; ", report));
    }

    [Theory]
    [InlineData("sentinel", 35)]
    [InlineData("sentinel", 45)]
    [InlineData("sentinel", 50)]
    [InlineData("guard", 35)]
    [InlineData("guard", 45)]
    [InlineData("guard", 50)]
    public void AClasssPaths_AreWorthAlike_AgainstTheMiddleGroups(string cls, int level)
    {
        // Sprint 193 (D96, `docs/classes-conception.md` §7.2): the CI's fence, on a sample of 8 groups and 15 fights a group, whatever the number of classes: about 9 points of
        // noise at 120 fights, so 15 points from the mean. The story's 8 points are judged on the bench, at 480 fights (`Rpg.Sim --matrix`, `docs/mesures-classes.md`). The Sentinel's
        // exception of 22 went with sprint 194 (T154), though the AI still does not play its traps and leaps. Against the groups of 4 and 5: no bare hero of these levels wins the solid hordes,
        // path or not, so a test on them would hold whatever the paths did. Never more than the +15 of M2 over a hero without a path, with the same noise allowed for.
        BenchReport bench = Bench.Run(new SyntheticFamily(level), cls, 15, sample: 8);
        string report = $"{cls} level {level}, {bench.Groups} groups: no path {bench.Plain.Points:0.0}; " + string.Join(", ", bench.Paths.Select(p => $"{p.Path} {p.Points:0.0}"));
        Assert.True(bench.Spread <= 15, report);
        Assert.True(bench.Paths.All(p => p.Points - bench.Plain.Points <= 24), report);
        // It would prove nothing at either end.
        Assert.True(bench.Plain.Points is > 5 and < 85, report);
    }

    [Fact]
    public void AtFortyFive_ThePairs_AreWorthAlike()
    {
        // Sprint 184 (D93, the J3 c of sprint 147 again, on the compounds): no pair created a false decision: each within 1.5
        // victories of 30 of the others' average, against the small groups, bare, its twins first in the deck, for each class.
        var family = new SyntheticFamily(45);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        double Small(Hero h) => few.AsParallel().Select(g => family.Wins(g.Scenario, h, 30)).Average();
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none");
            Dictionary<Element, double> values = Elements.Compounds.Where(e => Elements.CreatedFrom(e) == Elements.Pairs).ToDictionary(e => e, e => Small(Progression.Talented(bare, [], family.Data, compounds: [e])));
            foreach ((Element e, double v) in values)
                kept &= Math.Abs(v - values.Where(o => o.Key != e).Average(o => o.Value)) <= 1.5;
            report.Add($"{id}: " + string.Join(", ", values.Select(x => $"{x.Key} {x.Value:0.0}")));
        }
        Assert.True(kept, string.Join("; ", report));
    }
}
