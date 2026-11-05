using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The CI's levels of `docs/classes-conception.md` §7.2 (sprint 195, D97): N0, the smoke, has every class and path play; N1 keeps twelve reference parties
/// against the hordes. Their fights are a fixed budget, whatever the number of classes: more classes, fewer fights each. The bench (N2) is off the CI:
/// the tests marked <c>Explicit</c>, run with <c>-- --explicit only</c>.
/// </summary>
public class CiLevelsTests
{
    /// <summary>The smoke's fights, split between every class and path.</summary>
    public const int SmokeFights = 960;

    /// <summary>The fights of each reference party.</summary>
    public const int PartyFights = 40;

    private static readonly string[] Names = ["Aube", "Brume", "Cendre", "Dune"];

    /// <summary>Every class at 35 and 50: the hero without a path, each branch, each lineage.</summary>
    public static IReadOnlyList<(string Class, int Level, string? Spec, string? Fork)> Heroes(GameData data) =>
        [.. data.Classes.SelectMany(c => new[] { 35, Specs.Forks }.SelectMany(level =>
            Bench.PathsOf(c.Id, level, data).Select(p => (c.Id, level, (string?)p.Spec, p.Fork)).Prepend((c.Id, level, null, null))))];

    /// <summary>How many fights each hero of the smoke plays on each group: the budget split, at least one.</summary>
    public static int SmokeFightsEach(int heroes, int groups) => Math.Max(1, SmokeFights / (heroes * groups));

    [Fact]
    public void TheSmoke_StaysWithinItsBudget_WhateverTheNumberOfClasses()
    {
        Assert.Equal(8, SmokeFightsEach(30, 4));
        Assert.True(SmokeFightsEach(100, 4) * 100 * 4 <= SmokeFights);
        Assert.Equal(1, SmokeFightsEach(1000, 4));
    }

    [Fact]
    public void TheSmoke_EveryClassAndPath_FinishesItsFights_AndPlaysEachOfItsSpells()
    {
        var families = new Dictionary<int, SyntheticFamily> { [35] = new(35), [Specs.Forks] = new(Specs.Forks) };
        var heroes = Heroes(Real);
        // 3 classes: the hero without a path, 3 branches at 35, the hero and 6 lineages at 50.
        Assert.Equal(Real.Classes.Count * 11, heroes.Count);
        int each = SmokeFightsEach(heroes.Count, 4);
        string[] problems = [.. heroes.AsParallel().AsOrdered().SelectMany(h =>
        {
            SyntheticFamily family = families[h.Level];
            Hero hero = family.Hero(h.Class, "none") with { Spec = h.Spec, Fork = h.Fork };
            string who = $"{h.Class} {h.Level} {h.Fork ?? h.Spec ?? "-"}";
            var path = Specs.SpellsOf(hero, family.Data).Intersect(hero.DeckOf(family.Data), StringComparer.Ordinal)
                .Where(s => family.Data.Spells[s].Aims is not null || !family.Data.Spells[s].AllEffects.Any(e => e is SwapEffect)).ToHashSet(StringComparer.Ordinal);
            var cast = new HashSet<string>(StringComparer.Ordinal);
            var found = new List<string>();
            foreach (string group in Bench.MiddleGroups(family, 4))
            {
                for (ulong seed = 0; seed < (ulong)each; seed++)
                {
                    var fight = new Fight(family.Data, group, seed, hero);
                    Ai.PlayOut(fight);
                    if (!fight.IsOver || fight.WinningTeam is null)
                        found.Add($"{who}: {group} seed {seed} never ends");
                    cast.UnionWith(fight.History.OfType<CastAction>().Select(c => c.Spell));
                }
            }
            found.AddRange(path.Except(cast).Where(s => !Unplayed.Contains(s)).Select(s => $"{who}: {s} never played"));
            found.AddRange(path.Intersect(cast).Where(Unplayed.Contains).Select(s => $"{who}: {s} is played now, take it off the list"));
            return found;
        })];
        Assert.True(problems.Length == 0, string.Join("; ", problems));
    }

    /// <summary>
    /// The path spells the AI never plays alone, NOT CONCLUSIVE on the bench (`docs/mesures-classes.md`): a trap that does no damage on the
    /// spot, a leap, a pull, a loss of movement. The AI is to be mended, not the spells; the smoke fails on any other, and when one of these is played.
    /// </summary>
    public static readonly IReadOnlySet<string> Unplayed = new HashSet<string>(StringComparer.Ordinal)
    {
        "path-jaws", "path-assault", "path-wrench", "path-iron-ring",
    };

    /// <summary>
    /// The twelve reference parties of N1, four duos, five trios, three of four, with what they won against the hordes at 45 when measured
    /// (sprint 195, 40 fights, the same seeds): the band is ±15 points around it.
    /// </summary>
    public static readonly (string[] Classes, int Measured)[] Parties =
    [
        (["warrior", "archer"], 67), (["warrior", "mage"], 62), (["archer", "mage"], 75), (["warrior", "warrior"], 60),
        (["warrior", "archer", "mage"], 92), (["warrior", "warrior", "archer"], 90), (["warrior", "warrior", "mage"], 95),
        (["archer", "archer", "mage"], 97), (["mage", "mage", "warrior"], 100),
        (["warrior", "archer", "mage", "mage"], 100), (["warrior", "warrior", "archer", "mage"], 100), (["warrior", "archer", "archer", "mage"], 100),
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
    public void TheReferenceParties_AreTwelve_AndEachClassIsInSeveral()
    {
        Assert.Equal(12, Parties.Length);
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
