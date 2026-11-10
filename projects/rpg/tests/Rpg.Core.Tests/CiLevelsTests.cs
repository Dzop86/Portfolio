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

    /// <summary>
    /// The fights of each reference party: 40 until sprint 207; since sprint 208 (D97, the budget does not grow with the classes) a fixed budget of
    /// <see cref="PartyBudget"/> shared between them, the same first seeds.
    /// </summary>
    public static int PartyFights => Math.Min(40, PartyBudget / Parties.Length);

    /// <summary>What the reference parties play in all (sprint 208).</summary>
    public const int PartyBudget = 1000;

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

    /// <summary>
    /// What a hero of the smoke is (sprint 210, D98): the family's hero given its path, then its spell points spent knowing it, as a player would; the same hero as the bench's.
    /// </summary>
    public static Hero HeroOf(SyntheticFamily family, (string Class, int Level, string? Spec, string? Fork, string? Final) h)
    {
        ArgumentNullException.ThrowIfNull(family);
        Hero bare = family.Hero(h.Class, "none");
        return h.Spec is null ? bare : Progression.Built(bare with { Spec = h.Spec, Fork = h.Fork, Final = h.Final }, family.Data);
    }

    /// <summary>
    /// The hero a path is weighed against in the smoke: a final class against its lineage alone, a branch or a lineage against the hero without a path, at the same level.
    /// </summary>
    public static (string Class, int Level, string? Spec, string? Fork, string? Final) ParentOf((string Class, int Level, string? Spec, string? Fork, string? Final) h) =>
        h.Final is not null ? h with { Final = null } : (h.Class, h.Level, null, null, null);

    [Fact]
    public void TheSmoke_EveryClassAndPath_FinishesItsFights_AndChangesOne()
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

        // Each hero's fights, to tell whether a path changes any against the hero it is weighed against. Whether each path spell is played, the smoke cannot say
        // on a few fights a hero: the bench says it, off the CI (sprint 210, D98, Q4 of docs/jalon-2.md).
        var prints = new System.Collections.Concurrent.ConcurrentDictionary<(string, int, string?, string?, string?), string[]>();
        string[] problems = [.. heroes.AsParallel().AsOrdered().SelectMany(h =>
        {
            SyntheticFamily family = families[h.Level];
            Hero hero = HeroOf(family, h);
            string who = $"{h.Class} {h.Level} {h.Final ?? h.Fork ?? h.Spec ?? "-"}";
            var seen = new List<string>();
            var found = new List<string>();
            foreach (string group in Bench.MiddleGroups(family, SmokeGroups(h.Level)))
            {
                for (ulong seed = 0; seed < (ulong)EachAt(h.Level, heroes); seed++)
                {
                    var fight = new Fight(family.Data, group, seed, hero);
                    Ai.PlayOut(fight);
                    if (!fight.IsOver || fight.WinningTeam is null)
                        found.Add($"{who}: {group} seed {seed} never ends");
                    seen.Add(Fingerprint(fight));
                }
            }
            prints[h] = [.. seen];
            return found;
        })];
        // A path changes at least one fight of the hero it is weighed against, on the same seeds: a rule that never plays is caught (docs/classes-guerrier.md §7.4).
        string[] dead = [.. heroes.Where(h => h.Spec is not null).Where(h => prints[h].SequenceEqual(prints[ParentOf(h)])).Select(h => h.Final ?? h.Fork ?? h.Spec!)];
        string[] still = [.. dead.Except(Unchanged).Select(f => $"{f} changes no fight"), .. Unchanged.Except(dead).Select(f => $"{f} changes a fight now, take it off the list")];
        Assert.True(problems.Length == 0 && still.Length == 0, string.Join("; ", problems.Concat(still)));
    }

    [Fact(Explicit = true)] // The bench (N2, D98): off the CI, run with -- --explicit only
    public void TheBench_PlaysEachPathSpell_ButTheUnplayedOnes()
    {
        // Sprint 210 (Q4 of docs/jalon-2.md): what the smoke checked until sprint 209 on too few fights. A path spell no hero of its paths ever casts, at 35 and 50 on the
        // bench, is in Unplayed; one of Unplayed cast at all is to be taken off.
        var casts = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (int level in new[] { 35, Specs.Forks })
        {
            var family = new SyntheticFamily(level);
            foreach (HeroClass c in Real.Classes)
            {
                foreach (BenchRow row in Bench.Run(family, c.Id, 15, sample: 8).Paths)
                {
                    foreach ((string spell, double n) in row.Casts)
                        casts[spell] = casts.GetValueOrDefault(spell) + n;
                }
            }
        }
        string[] wrong = [.. casts.Where(c => c.Value == 0 && !Unplayed.Contains(c.Key)).Select(c => $"{c.Key} never played"),
            .. Unplayed.Where(s => casts.GetValueOrDefault(s) > 0).Select(s => $"{s} is played now ({casts[s]:0.00} a fight), take it off the list")];
        Assert.True(wrong.Length == 0, string.Join("; ", wrong));
    }

    /// <summary>
    /// The paths, mostly final classes, whose whole effect goes through a spell the AI hardly plays, or on allies the smoke has not: they change no fight of the smoke.
    /// The smoke fails when one does, so that the list only shrinks.
    /// </summary>
    public static readonly IReadOnlySet<string> Unchanged = new HashSet<string>(StringComparer.Ordinal)
    {
        // The Archer (sprint 198, docs/classes-archer.md §5): the AI does not cast Running Shot at 100. Its Beast Lord's second Hawk is called since sprint 211,
        // a fed creature worth its share of the Archer's characteristics.
        "war-hawk",
        // The Mage (sprint 200, docs/classes-mage-2.md §5): the Rune and the Crescendo unplayed; the Time Keeper and the Lord of Moments serve allies the smoke has not.
        "rune-master", "distortion-master", "time-keeper", "instant-lord",
        // Sprint 202: the Priest's 19 heroes of 100 leave each hero 5 fights (6 before). The Vaulter's echo and the Priest's finals that act through a spell played
        // late (Healing Wave, Exorcism, Zeal, Vision) or an ally (Life Warden) change none of them. Sprint 203: the Remedy, its heals grown with the level, now does.
        "saint", "life-warden", "zealot", "divine-sage",
        // The Rogue (sprint 204, docs/classes-voleur.md §6): the Dancing Blade's Vault and the Trap Master's second Caltrops unplayed; the Demolisher's
        // target without movement points hardly met alone; the Silent Reaper's Execution a turn sooner changes none of a hero's 5 fights.
        "dancing-blade", "demolisher", "trap-master", "silent-reaper",
        // Sprint 205: the Spectre's Shadow Step and the Master of Illusions' second Image, unplayed.
        "spectre", "illusion-master",
        // Sprint 207: the King of the Clans' Clan Cry unplayed; the Beheader's Beheading a turn sooner changes none of a hero's fights.
        "clan-king", "beheader",
        // Sprint 208: the Protector's finals act on allies, or on a hero falling, that the smoke has not.
        "holy-bastion", "eternal-guardian", "paladin-aegis",
        // Sprint 209: the Paladin's 19 heroes of 100 leave each hero fewer fights. The Living Ballista's Barrage without a cooldown changes none of them; the
        // Purifier's Sentence and the Hand of Judgement's Verdict sooner neither; the Zealot's Holy Fury, played for its points, a turn sooner neither.
        "hand-of-judgement", "paladin-zealot",
        // Sprint 210 (D98): the smoke's heroes rank their path spells. The Blade Master, the Vaulter, the Purifier, the Devourer, the Living Ballista and the Paladin's
        // Purifier change a fight now, the Destroyer too since the AI plays its Assault (a leap); the Forest Master's second Jaws and the Arcane Architect's Rune (traps
        // unplayed) and the Warchief's War Standard sooner change none.
        "forest-master", "magic-architect", "warchief",
        // Sprint 211 (D99): with movement taken, traps and pulls weighed by what they bring, the General changes a fight now; the Blade Master and the Destroyer
        // (its Breakthrough driving 2 cells) none of the few a hero of 100 plays.
        "blade-master", "destroyer",
    };

    /// <summary>
    /// The path spells the AI never plays alone, NOT CONCLUSIVE on the bench (`docs/mesures-classes.md`): a trap that does no damage on the
    /// spot, a leap, a pull, a loss of movement. The AI is to be mended, not the spells; the bench fails on any other, and when one of these is played (sprint 210, D98:
    /// the smoke until sprint 209).
    /// </summary>
    public static readonly IReadOnlySet<string> Unplayed = new HashSet<string>(StringComparer.Ordinal)
    {
        // The Warrior's (sprint 195): the Breach's Wrench, a pull, and the Pincer's Iron Ring, a loss of movement.
        "path-wrench", "path-iron-ring",
        // The Mage (sprint 199, docs/classes-mage.md §3): the Rune, a trap.
        "path-rune",
        // The Priest (sprint 202, docs/classes-pretre-2.md §4): the Resurrection raises a fallen ally, and the bench fights alone.
        "path-resurrect",
        // The Rogue (sprint 204): the Vault, a leap that strikes nothing.
        "path-vault",
        // Sprint 207: the Clan Cry, for allies the bench has not.
        "path-clan-cry",
        // Sprint 208 (docs/classes-paladin.md §4): the Defender's Shield of Faith serves allies the bench has not.
        "path-shield-of-faith",
        // Sprint 210 (D98): ranked, the Poisoner spends its points on Envenom; the Plague Vial, a poison over turns, never comes before it.
        "path-plague",
        // Played since sprint 210, their path spells ranked and the leaps worth their blow (D98): the Jaws and the Crescendo, rarely, the Assault, the Low Blow, the
        // Shadow Step, the Ravager's blows paid in hit points, the Barbarian's and the Gladiator's spells and the Feral Leap.
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
        // The Berserker's (sprint 206, measured the same way).
        (["warrior", "berserker"], 62), (["archer", "berserker"], 72), (["priest", "berserker"], 60),
        (["mage", "rogue", "berserker"], 100), (["warrior", "priest", "berserker"], 100), (["archer", "mage", "rogue", "berserker"], 100),
        // The Paladin's (sprint 208, measured the same way).
        (["mage", "paladin"], 55), (["archer", "paladin"], 60), (["rogue", "paladin"], 60),
        (["archer", "mage", "paladin"], 100), (["priest", "berserker", "paladin"], 95), (["warrior", "mage", "rogue", "paladin"], 100),
        // The Summoner's (sprint 211, measured the same way, 27 fights a party).
        (["warrior", "summoner"], 70), (["priest", "summoner"], 70), (["berserker", "summoner"], 66),
        (["archer", "rogue", "summoner"], 100), (["paladin", "mage", "summoner"], 100), (["warrior", "priest", "archer", "summoner"], 100),
    ];

    /// <summary>How far from its measure a party may stray, in points (§7.2).</summary>
    public const int Band = 15;

    /// <summary>The share of fights a party at 45, bare, wins against the hordes of six and more, in percent.</summary>
    internal static int Won(SyntheticFamily family, string[] classes)
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
