using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// Balance measured by simulation (lesson of the roguelike's sprint 23): a change of data that makes
/// a scenario trivial or hopeless fails here, not in a player's hands.
/// </summary>
public class BalanceTests
{
    private static int[] Results(string scenario, int fights, Hero? hero = null, Hero? rival = null)
    {
        int[] r = new int[3];
        for (ulong seed = 0; seed < (ulong)fights; seed++)
        {
            var f = new Fight(Real, scenario, seed, hero, rival);
            Ai.PlayOut(f);
            r[f.WinningTeam ?? 2]++;
        }
        return r;
    }

    [Fact]
    public void Training_TheHeroPlayedByTheAi_WinsMostFights_ButNotAll()
    {
        // 66 % since the training's enemies were made sturdier for the classes of sprint 67 (the heroine
        // went from 36 to 80 hit points with them); a human player chooses better than the AI.
        int[] r = Results("training", 1000);
        Assert.InRange(r[0], 600, 780);
        Assert.Equal(0, r[2]);
    }

    [Fact]
    public void Duel_NeitherSideAlwaysWins()
    {
        // About one in two since the AI plans its turns (sprint 56); the second to play won two in three before.
        int[] r = Results("duel", 1000);
        Assert.InRange(r[0], 400, 600);
        Assert.Equal(0, r[2]);
    }

    [Theory]
    [InlineData("sentinel", 450, 650)]
    [InlineData("guard", 880, 980)]
    [InlineData("mage", 700, 930)]
    public void Training_EveryClass_WinsMostFights_ButNotAll(string id, int least, int most)
    {
        // Measured on 1,000 fights after the classes were redone (sprint 67, T34): sentinel 83 %, guard 91 %,
        // mage 65 %. The guard fights up close and the training's enemies come to it: tuned down for it, the
        // other two fall off a cliff (README, limits). Since Sidestep counts (sprint 90, T57), the classes'
        // hit points keep the duels even, and the sentinel, played by the AI, wins about half its trainings:
        // sentinel 52 %, guard 95 %, mage 82 %.
        int[] r = Results("training", 1000, new Hero("Essai", "female-d", id));
        Assert.InRange(r[0], least, most);
        Assert.Equal(0, r[2]);
    }

    [Theory]
    [InlineData("sentinel", "guard")]
    [InlineData("sentinel", "mage")]
    [InlineData("guard", "mage")]
    public void AtLevel1_EveryClassDuel_IsWonByEitherSide(string a, string b)
    {
        // Both orders of play, 300 fights each; measured on 400 (sprint 56): 52 %, 38 %, 54 %; on 600 after
        // Sidestep, Momentum and Mana Surge started to count (sprint 90, T57): 55 %, 49 %, 57 %.
        int[] ab = Results("duel", 300, new Hero("Alpha", "female-a", a), new Hero("Bravo", "male-a", b));
        int[] ba = Results("duel", 300, new Hero("Alpha", "female-a", b), new Hero("Bravo", "male-a", a));
        Assert.InRange((ab[0] + ba[1]) / 6.0, 30, 70);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void AtHighLevels_ClassDuels_AlwaysEnd_HealingNeverOutlastsDamage(int level)
    {
        // Erosion takes back a tenth of each blow from the maximum: no duel reaches the round limit,
        // with points spent (sprint 58) or not.
        string[] classes = ["sentinel", "guard", "mage"];
        foreach (string a in classes)
        {
            foreach (string b in classes.Where(c => c != a))
            {
                var alpha = new Hero("Alpha", "female-a", a, Level: level);
                var bravo = new Hero("Bravo", "male-a", b, Level: level);
                Assert.Equal(0, Results("duel", 20, alpha, bravo)[2]);
                Assert.Equal(0, Results("duel", 20, Progression.Built(alpha, Real), Progression.Built(bravo, Real))[2]);
            }
        }
    }

    private static readonly string[] Classes = ["sentinel", "guard", "mage"];

    /// <summary>The battle maps where the hordes come in single file round the water (T59, T60).</summary>
    private static readonly string[] SingleFile = ["mouldy-pools", "lakeshore-pontoon"];

    /// <summary>
    /// The hordes of clod moles on the Meadow (three or more): frail against any full equipment, accepted as they are by
    /// Charles (T64), left out of the "craft" kit's ceiling (T77).
    /// </summary>
    private static bool ClodMolesInTheOpen(MonsterGroup g) =>
        Real.Families[g.Family].BattleOf(g.Id) == "meadow" && g.Monsters.Count(m => m == "clod-mole") >= 3;

    private static int Wins(string group, string id, int level, int fights, string kit = "none") =>
        Results(MonsterGroup.Prefix + group, fights, Progression.Built(new Hero("Essai", "female-d", id, Level: level), Real,
            Progression.Kit(kit, Real.Families[Real.Groups[group].Family].Zone, level, Real)))[0];

    [Theory]
    [InlineData("woods", 3)]
    [InlineData("mouldy", 10)]
    [InlineData("lake", 18)]
    [InlineData("fields", 26)]
    [InlineData("ancestors", 31)]
    public void AZone_GroupsOfOneToThree_AreForOnePlayerAtItsLevel_OfSixAndMore_ForSeveral(string zone, int level)
    {
        // Charles's rule (D63): a player alone beats groups of one to three easily, at the zone's level;
        // groups of six to ten are for several players, and a player alone often loses there. Measured on
        // 30 fights per group and class (sprints 72 to 74, T41): the woods at level 3, 98 to 100 % on
        // average, three cases under 80 % (17 to 22 of 30); the mouldy woods at level 10, 98 to 100 %,
        // four cases (16 to 22 of 30); the lake at level 18 (sprint 75, T42), 96 to 100 %, four cases,
        // one under 18 of 30 (13); the fields at level 26 (sprint 76, T43), 100 %; the hordes and the chiefs, 0 to 2 %.
        MonsterGroup[] here = [.. Real.Groups.Values.Where(g => Real.Towns[g.Map].Zone == zone)];
        foreach (string id in new[] { "sentinel", "guard", "mage" })
        {
            int[] small = [.. here.Where(g => g.Monsters.Count <= 3).AsParallel().Select(g => Wins(g.Id, id, level, 30))];
            Assert.True(small.Average() >= 27, $"{id}: {small.Average():0.0} of 30 against the small groups of {zone}");
            Assert.InRange(small.Count(w => w < 18), 0, 2);
            int[] large = [.. here.Where(g => g.Monsters.Count >= 6).AsParallel().Select(g => Wins(g.Id, id, level, 10))];
            Assert.NotEmpty(large);
            Assert.True(large.Average() <= 3, $"{id}: {large.Average():0.0} of 10 against the hordes of {zone}");
            // Since sprint 96 (D71, T64), the ceilings are measured again with two quick pieces of the zone worn, off the
            // maps where the hordes come in single file round the water (accepted by Charles, T59 and T60). With all of
            // them (the "fast" kit), the Fields' hordes of clod moles fall to a lone hero: a question for Charles.
            // Since sprint 109 (D76, T77), also with the craftsmen's pieces in every other slot (the "craft" kit): what a player
            // alone is sure to have.
            foreach (string kit in new[] { "zone", "craft" })
            {
                int[] equipped = [.. here.Where(g => g.Monsters.Count >= 6 && !SingleFile.Contains(Real.Families[g.Family].BattleOf(g.Id))
                        && !(kit == "craft" && ClodMolesInTheOpen(g)))
                    .AsParallel().Select(g => Wins(g.Id, id, level, 10, kit))];
                Assert.True(equipped.Average() <= 2, $"{id}: {equipped.Average():0.0} of 10 against the hordes of {zone}, kit {kit}");
            }
        }
    }

    [Fact]
    public void TheDisjoint_IsBeatenAlone_AtLevelThirty_WithTheCraftsmensPieces()
    {
        // Sprint 114 (design-familles.md §4.9): the chapter's boss, four beasts forced into one, wearing itself out (15 hit
        // points a turn) and bursting as it falls. Measured on 100 fights a class, with the "craft" kit (what a player alone
        // is sure to have): sentinel 100, guard 39, mage 45, three in five on average; every class can beat it alone.
        int[] wins = [.. Classes.Select(id =>
        {
            Hero hero = Progression.Built(new Hero("Essai", "female-d", id, Level: 30), Real, Progression.Kit("craft", "fields", 30, Real));
            return Results("the-disjoint", 100, hero)[0];
        })];
        Assert.All(wins, w => Assert.InRange(w, 30, 100));
        Assert.InRange(wins.Average(), 50, 75);
    }

    [Theory]
    [InlineData("sentinel")]
    [InlineData("guard")]
    [InlineData("mage")]
    public void TheWallowsGreatHall_TheMainQuestsStep_IsWonAloneAtItsLevel(string id)
    {
        // Sprint 95 (D71): "rumble-under-the-wallow" asks for the great hall, at least 7 times in 10 alone,
        // at the quest's level, with nothing worn.
        Quest quest = Real.Quests["rumble-under-the-wallow"];
        var hero = Progression.Built(new Hero("Essai", "female-d", id, Level: quest.Level), Real);
        int wins = Results(Real.Dungeons["boar-wallow"].Scenario(2), 100, hero)[0];
        Assert.True(wins >= 70, $"{id}: {wins} of 100 in the great hall at level {quest.Level}");
    }
}
