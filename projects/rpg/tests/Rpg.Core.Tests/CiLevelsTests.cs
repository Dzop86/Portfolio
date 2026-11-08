using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The CI's levels of `docs/classes-conception.md` §7.2 (sprint 195, D97): N0, the smoke, has every class and path play; N1 keeps twelve reference parties
/// against the hordes. Their fights are a fixed budget, whatever the number of classes: more classes, fewer fights each. The bench (N2) is off the CI:
/// the tests marked <c>Explicit</c>, run with <c>-- --explicit only</c>.
/// </summary>
public class CiLevelsTests
{
    /// <summary>The smoke's fights, split between every class and path at 35 and 50.</summary>
    public const int SmokeFights = 960;

    /// <summary>The smoke's fights at 100, split between the final classes and their lineages alone (sprint 197): a budget of their own, the first one's untouched.</summary>
    public const int FinalSmokeFights = 760;

    /// <summary>The fights of each reference party.</summary>
    public const int PartyFights = 40;

    private static readonly string[] Names = ["Aube", "Brume", "Cendre", "Dune"];

    /// <summary>
    /// Every class at 35 and 50: the hero without a path, each branch, each lineage; at 100 too once its lineages end in final classes (sprint 197, docs/classes-guerrier.md
    /// §7.4): the hero, each lineage without its final class, each final class.
    /// </summary>
    public static IReadOnlyList<(string Class, int Level, string? Spec, string? Fork, string? Final)> Heroes(GameData data) =>
        [.. data.Classes.SelectMany(c => new[] { 35, Specs.Forks, Specs.Finals }
            .Where(level => level < Specs.Finals || data.Specs.Values.Any(s => s.Class == c.Id && s.Forks.Any(f => f.Finals is { Count: > 0 })))
            .SelectMany(level => Bench.PathsOf(c.Id, level, data).Select(p => (c.Id, level, (string?)p.Spec, p.Fork, p.Final)).Prepend((c.Id, level, null, null, null))))];

    /// <summary>The groups a hero of the smoke plays: four, two at 100, where the heroes are many.</summary>
    public static int SmokeGroups(int level) => level < Specs.Finals ? 4 : 2;

    /// <summary>How many fights each pair of a hero and a group of the smoke plays: its budget split between the pairs, at least one.</summary>
    public static int SmokeFightsEach(int pairs, int budget = SmokeFights) => Math.Max(1, budget / pairs);

    private static int EachAt(int level, IReadOnlyList<(string Class, int Level, string? Spec, string? Fork, string? Final)> heroes)
    {
        bool last = level >= Specs.Finals;
        return SmokeFightsEach(heroes.Where(h => h.Level >= Specs.Finals == last).Sum(h => SmokeGroups(h.Level)), last ? FinalSmokeFights : SmokeFights);
    }

    [Fact]
    public void TheSmoke_StaysWithinItsBudget_WhateverTheNumberOfClasses()
    {
        Assert.Equal(8, SmokeFightsEach(30 * 4));
        Assert.True(SmokeFightsEach(100 * 4) * 100 * 4 <= SmokeFights);
        Assert.Equal(1, SmokeFightsEach(1000 * 4));
        var heroes = Heroes(Real);
        int below = heroes.Where(h => h.Level < Specs.Finals).Sum(h => SmokeGroups(h.Level)), at = heroes.Where(h => h.Level >= Specs.Finals).Sum(h => SmokeGroups(h.Level));
        Assert.True(EachAt(35, heroes) * below <= SmokeFights && EachAt(Specs.Finals, heroes) * at <= FinalSmokeFights, $"{below} and {at} pairs");
        // Ten classes of 30 heroes, 11 below 100 and 19 at it: within the two budgets, one fight a pair.
        Assert.True(10 * 11 * 4 <= SmokeFights && 10 * 19 * 2 <= FinalSmokeFights);
    }

    [Fact]
    public void TheSmoke_EveryClassAndPath_FinishesItsFights_AndPlaysEachOfItsSpells()
    {
        var families = new Dictionary<int, SyntheticFamily> { [35] = new(35), [Specs.Forks] = new(Specs.Forks), [Specs.Finals] = new(Specs.Finals) };
        var heroes = Heroes(Real);
        // Each class: the hero without a path, 3 branches at 35, the hero and 6 lineages at 50; a class converted, at 100 the hero, 6 lineages and 12 final classes;
        // a class without its paths yet (the Priest of sprint 201), the hero at 35 and at 50; with 2 branches of 3 (the Rogue of sprint 204), 8 and 13.
        int Expected(HeroClass c)
        {
            int branches = Real.Specs.Values.Count(s => s.Class == c.Id);
            bool converted = Real.Specs.Values.Any(s => s.Class == c.Id && s.Forks.Any(f => f.Finals is { Count: > 0 }));
            return 2 + (3 * branches) + (converted ? 1 + (6 * branches) : 0);
        }
        Assert.Equal(Real.Classes.Sum(Expected), heroes.Count);

        // Each hero's fights, to tell whether a final class changes any against its lineage alone.
        var prints = new System.Collections.Concurrent.ConcurrentDictionary<(string, int, string?, string?), string[]>();
        string[] problems = [.. heroes.AsParallel().AsOrdered().SelectMany(h =>
        {
            SyntheticFamily family = families[h.Level];
            Hero hero = family.Hero(h.Class, "none") with { Spec = h.Spec, Fork = h.Fork, Final = h.Final };
            string who = $"{h.Class} {h.Level} {h.Final ?? h.Fork ?? h.Spec ?? "-"}";
            var seen = new List<string>();
            var path = Specs.SpellsOf(hero, family.Data).Intersect(hero.DeckOf(family.Data), StringComparer.Ordinal)
                .Where(s => family.Data.Spells[s].Aims is not null || !family.Data.Spells[s].AllEffects.Any(e => e is SwapEffect)).ToHashSet(StringComparer.Ordinal);
            var cast = new HashSet<string>(StringComparer.Ordinal);
            var found = new List<string>();
            foreach (string group in Bench.MiddleGroups(family, SmokeGroups(h.Level)))
            {
                for (ulong seed = 0; seed < (ulong)EachAt(h.Level, heroes); seed++)
                {
                    var fight = new Fight(family.Data, group, seed, hero);
                    Ai.PlayOut(fight);
                    if (!fight.IsOver || fight.WinningTeam is null)
                        found.Add($"{who}: {group} seed {seed} never ends");
                    cast.UnionWith(fight.History.OfType<CastAction>().Select(c => c.Spell));
                    seen.Add(Fingerprint(fight));
                }
            }
            if (h.Level >= Specs.Finals && h.Fork is not null)
                prints[(h.Class, h.Level, h.Fork, h.Final)] = [.. seen];
            // Each spell played, below 100 only: at 100 the AI hardly plays Crush, Harpoon nor Retaliation, its lineages alone included (sprint 197, T160); the final classes
            // are judged there by the fights they change.
            if (h.Level < Specs.Finals)
            {
                bool Excused(string s) => Unplayed.Contains(s) || (h.Level >= Specs.Forks && UnplayedOn.Contains((h.Fork ?? "", s)));
                found.AddRange(path.Except(cast).Where(s => !Excused(s)).Select(s => $"{who}: {s} never played"));
                found.AddRange(path.Intersect(cast).Where(Excused).Select(s => $"{who}: {s} is played now, take it off the list"));
            }
            return found;
        })];
        // A final class changes at least one fight of its lineage alone, on the same seeds: a rule that never plays is caught (docs/classes-guerrier.md §7.4).
        string[] dead = [.. heroes.Where(h => h.Final is not null).Where(h => prints[(h.Class, h.Level, h.Fork, h.Final)].SequenceEqual(prints[(h.Class, h.Level, h.Fork, null)]))
            .Select(h => h.Final!)];
        string[] still = [.. dead.Except(Unchanged).Select(f => $"{f} changes no fight"), .. Unchanged.Except(dead).Select(f => $"{f} changes a fight now, take it off the list")];
        Assert.True(problems.Length == 0 && still.Length == 0, string.Join("; ", problems.Concat(still)));
    }

    /// <summary>
    /// The final classes whose whole effect goes through a spell of <see cref="Unplayed"/>: they cannot change a fight of the smoke until the AI plays it. The smoke
    /// fails when one does, so that the list only shrinks.
    /// </summary>
    public static readonly IReadOnlySet<string> Unchanged = new HashSet<string>(StringComparer.Ordinal)
    {
        "general",
        // The Archer (sprint 198, docs/classes-archer.md §5): the AI calls no second Hawk, and does not cast Running Shot at 100.
        "beast-lord", "war-hawk",
        // Sprint 200: the Blade Master's Crush, hardly played at 100, changes none of the 6 fights a hero has left since the Mage's 18 heroes of 100 share the budget (10 before).
        "blade-master",
        // The Mage (sprint 200, docs/classes-mage-2.md §5): the Rune and the Crescendo unplayed; the Time Keeper and the Lord of Moments serve allies the smoke has not.
        "rune-master", "distortion-master", "time-keeper", "instant-lord",
        // Sprint 202: the Priest's 19 heroes of 100 leave each hero 5 fights (6 before). The Vaulter's echo and the Priest's finals that act through a spell played
        // late (Healing Wave, Exorcism, Zeal, Vision) or an ally (Life Warden) change none of them. Sprint 203: the Remedy, its heals grown with the level, now does.
        "vaulter", "saint", "life-warden", "purifier", "zealot", "divine-sage",
        // The Rogue (sprint 204, docs/classes-voleur.md §6): the Dancing Blade's Vault and the Trap Master's second Caltrops unplayed; the Demolisher's
        // target without movement points hardly met alone; the Silent Reaper's Execution a turn sooner changes none of a hero's 5 fights.
        "dancing-blade", "demolisher", "trap-master", "silent-reaper",
        // Sprint 205: the Spectre's Shadow Step and the Master of Illusions' second Image, unplayed.
        "spectre", "illusion-master",
    };

    /// <summary>
    /// A path spell the AI plays elsewhere but not on this lineage, at 50 (sprint 198): the Bounty Hunter's AI prefers the quarry to Weak Spot, the Skirmisher's the
    /// Barrage come down to 30 to Running Shot. Not conclusive, as <see cref="Unplayed"/>; the smoke fails when one is played.
    /// </summary>
    public static readonly IReadOnlySet<(string Fork, string Spell)> UnplayedOn = new HashSet<(string, string)>
    {
        ("bounty-hunter", "path-weak-spot"), ("runner", "path-running-shot"),
        // Sprint 200: the Master of Illusions' AI spends its points on the Image and the Double before Dazzle.
        ("mirage-master", "path-dazzle"),
        // Sprint 202: with fewer fights each, the Time Weaver's Quicken, played for allies, no longer comes up alone.
        ("time-weaver", "path-quicken"),
        // Sprint 205: the Night Stalker's AI spends its points on the Waylay before the Shadow Blade.
        ("night-stalker", "path-shadow-blade"),
    };

    /// <summary>
    /// The path spells the AI never plays alone, NOT CONCLUSIVE on the bench (`docs/mesures-classes.md`): a trap that does no damage on the
    /// spot, a leap, a pull, a loss of movement. The AI is to be mended, not the spells; the smoke fails on any other, and when one of these is played.
    /// </summary>
    public static readonly IReadOnlySet<string> Unplayed = new HashSet<string>(StringComparer.Ordinal)
    {
        "path-jaws", "path-assault", "path-wrench", "path-iron-ring",
        // The Mage (sprint 199, docs/classes-mage.md §3): the Rune, a trap as the Jaws; the Crescendo, 10 AP the AI never saves nor borrows for.
        "path-rune", "path-crescendo",
        // The Priest (sprint 202, docs/classes-pretre-2.md §4): the Resurrection raises a fallen ally, and the smoke fights alone.
        "path-resurrect",
        // The Rogue (sprint 204): the Vault, a leap, as the Assault (its 0.1 a fight of T152); the Low Blow, weaker a point of action than the kit's
        // spells and their ranks, even at 3 AP.
        "path-vault", "path-backstab",
        // Sprint 205 (docs/classes-voleur-2.md §3): the Shadow Step, a leap as the Vault.
        "path-shadow-step",
    };

    /// <summary>
    /// The twelve reference parties of N1, four duos, five trios, three of four, and six more with each class written since (the Priest, sprint 201), with what they won against the hordes at 45 when measured
    /// (sprint 195, 40 fights, the same seeds): the band is ±15 points around it.
    /// </summary>
    public static readonly (string[] Classes, int Measured)[] Parties =
    [
        (["warrior", "archer"], 67), (["warrior", "mage"], 62), (["archer", "mage"], 75), (["warrior", "warrior"], 60),
        (["warrior", "archer", "mage"], 92), (["warrior", "warrior", "archer"], 90), (["warrior", "warrior", "mage"], 95),
        (["archer", "archer", "mage"], 97), (["mage", "mage", "warrior"], 100),
        (["warrior", "archer", "mage", "mage"], 100), (["warrior", "warrior", "archer", "mage"], 100), (["warrior", "archer", "archer", "mage"], 100),
        // The Priest's (sprint 201, measured the same way; the twelve above measured again, unchanged).
        (["warrior", "priest"], 57), (["archer", "priest"], 72), (["mage", "priest"], 62),
        (["warrior", "archer", "priest"], 95), (["warrior", "mage", "priest"], 87), (["warrior", "archer", "mage", "priest"], 100),
        // The Rogue's (sprint 204, measured the same way).
        (["warrior", "rogue"], 60), (["archer", "rogue"], 60), (["mage", "rogue"], 57),
        (["warrior", "archer", "rogue"], 100), (["warrior", "priest", "rogue"], 97), (["archer", "mage", "priest", "rogue"], 100),
    ];

    /// <summary>How far from its measure a party may stray, in points (§7.2).</summary>
    public const int Band = 15;

    /// <summary>The share of fights a party at 45, bare, wins against the hordes of six and more, in percent.</summary>
    private static int Won(SyntheticFamily family, string[] classes)
    {
        string[] hordes = [.. family.Groups.Where(g => g.Size >= 6).Select(g => g.Scenario)];
        Hero[] party = [.. classes.Select((c, k) => family.Hero(c, "none") with { Name = Names[k] })];
        int won = 0;
        Parallel.For(0, PartyFights, i =>
        {
            var fight = new Fight(family.Data, hordes[i % hordes.Length], (ulong)i, party[0], null, null, party[1..]);
            Ai.PlayOut(fight);
            Assert.True(fight.IsOver);
            if (fight.WinningTeam == 0)
                Interlocked.Increment(ref won);
        });
        return 100 * won / PartyFights;
    }

    [Fact]
    public void TheReferenceParties_AreTwelve_AndSixForEachNewClass_AndEachClassIsInSeveral()
    {
        Assert.Equal(12 + (6 * (Real.Classes.Count - 3)), Parties.Length);
        Assert.All(Parties, p => Assert.InRange(p.Classes.Length, 2, 4));
        Assert.All(Parties, p => Assert.All(p.Classes, c => Assert.Contains(Real.Classes, k => k.Id == c)));
        Assert.All(Real.Classes, c => Assert.True(Parties.Count(p => p.Classes.Contains(c.Id)) >= 6, c.Id));
    }

    [Fact]
    public void TheReferenceParties_AgainstTheHordes_StayWithinTheirBands()
    {
        var family = new SyntheticFamily(45);
        string[] astray = [.. Parties.Select(p => (p, Won: Won(family, p.Classes))).Where(r => Math.Abs(r.Won - r.p.Measured) > Band)
            .Select(r => $"{string.Join("+", r.p.Classes)}: {r.Won} %, measured {r.p.Measured} %")];
        Assert.True(astray.Length == 0, string.Join("; ", astray));
    }
}
