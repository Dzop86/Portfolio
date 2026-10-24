using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The yardsticks of the levels after 30 (sprint 128, D79, T96): the monsters' templates, the budget of the pieces and
/// the rhythm of experience up to level 45, and T41 measured on a family made from the templates.
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
        for (int level = Templates.FirstChapter; level <= 50; level++)
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

    // ---- T41 on a family made from the templates ----

    /// <summary>A family made from the templates at a zone's level, as the fields' are at 26: its groups, its spells, its kits.</summary>
    private sealed class Synthetic
    {
        public GameData Data { get; }
        public int Level { get; }
        public IReadOnlyList<(string Scenario, int Size, bool InTheOpen)> Groups { get; }

        public Synthetic(int level)
        {
            Level = level;
            int by = level - Fields;
            var spells = new Dictionary<string, Spell>(StringComparer.Ordinal);
            var monsters = new Dictionary<string, FighterSpec>(StringComparer.Ordinal);
            // Each monster of the fields' families, as many levels higher, given its role's template at its new level: hit
            // points and initiative; its main spell (its first that hurts) the template's roll, its other spells in the same
            // proportion, a support's heal the template's; its resistances the template's, on the same elements; its passives.
            foreach ((MonsterFamily family, MonsterSpec m) in Real.Families.Values.Where(f => f.Zone == "fields").SelectMany(f => f.Monsters.Select(m => (f, m))))
            {
                int at = m.Level + by;
                MonsterTemplate t = Templates.Monster(m.Role, at);
                Spell main = m.Spells.Select(id => Real.Spells[id]).First(s => s.DamageMax > 0);
                double ratio = t.Damage / ((main.DamageMin + main.DamageMax) / 2.0);
                var cast = new List<string>();
                foreach (Spell s in m.Spells.Select(id => Real.Spells[id]))
                {
                    IReadOnlyList<SpellEffect>? effects = s.Effects?.Select(e => e switch
                    {
                        HealEffect h => h with { Min = t.Heal - t.Heal / 7, Max = t.Heal + t.Heal / 7 },
                        ShieldEffect sh => sh with { Amount = (int)Math.Round(sh.Amount * ratio) },
                        _ => e,
                    }).ToList();
                    Spell made = s == main
                        ? s with { DamageMin = t.Roll.Min, DamageMax = t.Roll.Max }
                        : s with { DamageMin = (int)Math.Round(s.DamageMin * ratio), DamageMax = (int)Math.Round(s.DamageMax * ratio) };
                    made = made with { Id = $"synth-{m.Id}-{s.Id}", Effects = effects };
                    spells[made.Id] = made;
                    cast.Add(made.Id);
                }
                Dictionary<Element, int> resistances = (m.Resistances ?? new Dictionary<Element, int>()).ToDictionary(r => r.Key, r => r.Value > 0 ? t.Resistance : t.Weakness);
                monsters[m.Id] = m.Fighter(0, family: family.Id) with { Hp = t.Hp, Initiative = t.Initiative, Spells = cast, Resistances = resistances, Level = at };
            }
            // The fields' groups, on the same battle maps.
            FighterSpec hero = new(Text, "female-d", 0, 80, 6, 3, 120, 0, ["strike"]);
            var scenarios = new List<Scenario>();
            var groups = new List<(string, int, bool)>();
            foreach (MonsterGroup g in Real.Groups.Values.Where(g => Real.Towns[g.Map].Zone == "fields" && g.Fight is null).OrderBy(g => g.Id, StringComparer.Ordinal))
            {
                string id = $"synth:{g.Id}";
                scenarios.Add(new Scenario(id, Text, Real.Families[g.Family].BattleOf(g.Id), [hero, .. g.Monsters.Select((m, i) => monsters[m] with { Start = i })]));
                // The Meadow's hordes of clod moles, left out of the "craft" kit's ceiling as in the fields (T64, T77).
                bool open = Real.Families[g.Family].BattleOf(g.Id) == "meadow" && g.Monsters.Count(m => m == "clod-mole") >= 3;
                groups.Add((id, g.Monsters.Count, open));
            }
            Groups = groups;
            Data = new GameData([.. Real.Spells.Values, .. spells.Values], Real.Maps.Values, [.. Real.Scenarios.Values, .. scenarios], Real.Classes,
                summons: Real.Summons.Values, items: [.. Real.Items.Values, .. Pieces(level)], sets: Real.Sets.Values, passives: Real.Passives.Values,
                talents: Real.Talents.Values, reactions: Real.Reactions.Values);
        }

        /// <summary>The level the fields' kits are measured at (T41), which the kits of the synthetic family are carried from.</summary>
        private const int Fields = 26;

        private static readonly string[] Kits = ["zone", "craft"];

        /// <summary>
        /// The pieces of the kits "zone" and "craft" at the fields' level (T41), carried to this level: the same slots and
        /// elements, each piece as many levels higher, its characteristics brought to the budget of its new level in the same
        /// proportions, its initiative kept.
        /// </summary>
        private static IEnumerable<Item> Pieces(int level)
        {
            int by = level - Fields;
            foreach (string id in Kits.SelectMany(k => Progression.Kit(k, "fields", Fields, Real).Values).Distinct())
            {
                Item real = Real.Items[id];
                int budget = Templates.Budget(real.Level + by, real.Slot!.Value) - (int)Math.Ceiling(0.3 * real.Initiative);
                Characteristics c = real.Stats!;
                double total = c.Vitality + c.Earth + c.Fire + c.Water + c.Air;
                int Part(int v) => (int)Math.Round(v * budget / total);
                int vitality = Part(c.Vitality), earth = Part(c.Earth), fire = Part(c.Fire), water = Part(c.Water);
                int air = c.Air > 0 ? budget - vitality - earth - fire - water : 0;
                yield return real with { Id = "synth-" + id, Level = real.Level + by, Stats = new Characteristics(vitality, earth, fire, water, air) };
            }
        }

        /// <summary>The hero of a class at the level, its points spent the simple way, wearing a kit ("none", "zone" or "craft") carried from the fields'.</summary>
        public Hero Hero(string id, string kit)
        {
            var worn = Progression.Kit(kit, "fields", Fields, Real).ToDictionary(w => w.Key, w => "synth-" + w.Value);
            Assert.Null(Equipment.Problem(worn, Level, Data));
            return Progression.Built(new Hero("Essai", "female-d", id, Level: Level), Data, worn);
        }

        public int Wins(string scenario, Hero hero, int fights)
        {
            int wins = 0;
            for (ulong seed = 0; seed < (ulong)fights; seed++)
            {
                var f = new Fight(Data, scenario, seed, hero);
                Ai.PlayOut(f);
                wins += f.WinningTeam == 0 ? 1 : 0;
            }
            return wins;
        }
    }

    [Theory]
    [InlineData(35)]
    [InlineData(40)]
    [InlineData(45)]
    public void AFamilyMadeFromTheTemplates_KeepsT41_SmallGroupsForOnePlayer_HordesForSeveral(int level)
    {
        var family = new Synthetic(level);
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none");
            var few = family.Groups.Where(g => g.Size <= 3).ToArray();
            int[] small = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, bare, 30))];
            kept &= small.Average() >= 27 && small.Count(w => w < 18) <= 2;
            string under = string.Join(", ", few.Zip(small).Where(x => x.Second < 18).Select(x => $"{x.First.Scenario} {x.Second}"));
            report.Add($"{id}: small {small.Average():0.0} of 30 ({small.Count(w => w < 18)} under 18: {under})");
            foreach (string kit in new[] { "none", "zone", "craft" })
            {
                Hero worn = family.Hero(id, kit);
                int[] large = [.. family.Groups.Where(g => g.Size >= 6 && !(kit == "craft" && g.InTheOpen)).AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, worn, 10))];
                kept &= large.Average() <= (kit == "none" ? 3 : 2);
                report.Add($"{id}: hordes, kit {kit}, {large.Average():0.0} of 10");
            }
        }
        Assert.True(kept, $"level {level}: " + string.Join("; ", report));
    }

    [Fact]
    public void TheTalentsOfTheSlice_KeepT41AtThirtyFive_AndGiveTheSentinelAtMostTenPercentMore()
    {
        // Sprint 137 (D80, T103): the Sentinel of 35 with Bogging and Up close, bare against the small groups, wearing the
        // kits against the hordes; the tree is worth at most +10 % of victories against the small groups (§2.3).
        var family = new Synthetic(35);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        Hero bare = family.Hero("sentinel", "none");
        Hero tree = bare with { Talents = ["bogging", "up-close"] };
        int[] without = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, bare, 30))];
        int[] with = [.. few.AsParallel().AsOrdered().Select(g => family.Wins(g.Scenario, tree, 30))];
        string report = $"small {without.Average():0.0} without, {with.Average():0.0} with ({with.Count(w => w < 18)} under 18)";
        Assert.True(with.Average() >= 27 && with.Count(w => w < 18) <= 2, report);
        Assert.True(with.Average() <= without.Average() + 3, report);
        foreach (string kit in new[] { "none", "zone", "craft" })
        {
            Hero worn = family.Hero("sentinel", kit) with { Talents = ["bogging", "up-close"] };
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
        var family = new Synthetic(level);
        var few = family.Groups.Where(g => g.Size <= 3).ToArray();
        var hordes = family.Groups.Where(g => g.Size >= 6 && !g.InTheOpen).ToArray();
        double Large(Hero h) => hordes.AsParallel().Select(g => family.Wins(g.Scenario, h, 10)).Average();
        double Small(Hero h) => few.AsParallel().Select(g => family.Wins(g.Scenario, h, 30)).Average();
        var report = new List<string>();
        bool kept = true;
        foreach (string id in Classes)
        {
            Hero bare = family.Hero(id, "none"), craft = family.Hero(id, "craft");
            double without = Small(bare);
            double best = new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }.Select(e => Large(OfOne(family, id, e, craft.Worn))).Append(Large(craft)).Max();
            var line = new List<string> { $"{id} bare {without:0.0} (best horde {best:0.0})" };
            var families = new List<double>();
            foreach (string build in new[] { "matter", "manner" })
            {
                double small = Small(Progression.Talented(bare, $"{id}-{build}", family.Data));
                families.Add(small);
                double large = Large(Progression.Talented(craft, $"{id}-{build}", family.Data, craft.Worn));
                kept &= small >= 27 && small <= without + 3 && large <= best + 1.5;
                line.Add($"{build} {small:0.0} (hordes {large:0.0})");
            }
            kept &= Math.Abs(families[0] - families[1]) <= 1.5;
            report.Add(string.Join(", ", line));
        }
        Assert.True(kept, $"level {level}: " + string.Join("; ", report));
    }

    /// <summary>A hero of one element and no talent: its class's spells of that element and the neutral ones, all its points in it.</summary>
    private static Hero OfOne(Synthetic family, string id, Element element, IReadOnlyDictionary<Slot, string>? worn)
    {
        Hero hero = family.Hero(id, "none");
        string[] deck = [.. family.Data.Class(id)!.Spells.Where(s => family.Data.Spells[s].Element is Element.Neutral || family.Data.Spells[s].Element == element)
            .Where(s => family.Data.Spells[s].Level <= hero.Level)];
        Hero built = Progression.Built(hero with { Deck = deck }, family.Data, worn);
        Characteristics st = built.Stats!;
        int all = st.Earth + st.Fire + st.Water + st.Air;
        int In(Element e) => e == element ? all : 0;
        return built with { Stats = new Characteristics(st.Vitality, In(Element.Earth), In(Element.Fire), In(Element.Water), In(Element.Air)) };
    }
}
