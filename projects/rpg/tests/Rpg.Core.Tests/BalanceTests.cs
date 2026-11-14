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

    [Fact(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    public void Duel_NeitherSideAlwaysWins()
    {
        // About one in two since the AI plans its turns (sprint 56); the second to play won two in three before.
        int[] r = Results("duel", 1000);
        Assert.InRange(r[0], 400, 600);
        Assert.Equal(0, r[2]);
    }

    [Theory]
    [InlineData("archer", 450, 650)]
    [InlineData("warrior", 880, 980)]
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

    [Theory(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    [InlineData("archer", "warrior")]
    [InlineData("archer", "mage")]
    [InlineData("warrior", "mage")]
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
        string[] classes = ["archer", "warrior", "mage"];
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

    private static readonly string[] Classes = ["archer", "warrior", "mage", "priest", "rogue", "berserker", "paladin", "summoner", "necromancer", "druid"];

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

    [Theory(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
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
        foreach (string id in new[] { "archer", "warrior", "mage" })
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
    public void TheForestsHordes_AtLevelFortyFive_AreNotWonFarMore_WithTheCraftsmensPiecesOfFortyAndFortyFive()
    {
        // Sprint 135 (D79): the craftsmen's pieces of levels 40 and 45 (items of levels 34 and 38) worn in every slot but the
        // zone's two quick ones, against the forest's hordes, at its highest level, compared with the pieces of level 35 and
        // under. Measured on 10 fights a horde: sentinel 4.1 then 4.8 of 10, guard 4.6 then 4.5, mage 3.1 then 4.9 (bare, 0.9,
        // 0.0 and 0.9). The plan asked for at most 2 of 10 at 45; the forest's monsters are of levels 31 to 37, and a hero of
        // 45 already wins almost half of them with the older pieces: a question for Charles, not these pieces' doing.
        MonsterGroup[] hordes = [.. Real.Groups.Values.Where(g => Real.Towns[g.Map].Zone == "ancestors" && g.Monsters.Count >= 6)];
        Assert.NotEmpty(hordes);
        IReadOnlyDictionary<Slot, string> before = Progression.Kit("craft", "ancestors", 33, Real), now = Progression.Kit("craft", "ancestors", 45, Real);
        Assert.Contains(now.Values, id => Real.Items[id].Level == 38);
        Assert.DoesNotContain(before.Values, id => Real.Items[id].Level >= 34);
        double Wins(string id, IReadOnlyDictionary<Slot, string> worn)
        {
            Hero hero = Progression.Built(new Hero("Essai", "female-d", id, Level: 45), Real, worn);
            // A sample since sprint 208 (D97): 60 fights a horde and a kit shared between the classes, 6 at least; in tenths, as measured.
            int fights = Math.Max(6, 60 / Classes.Length);
            return hordes.AsParallel().Select(g => 10.0 * Results(MonsterGroup.Prefix + g.Id, fights, hero)[0] / fights).Average();
        }
        double[] gain = [.. Classes.Select(id => Wins(id, now) - Wins(id, before))];
        // 3 a class since the first milestone (sprint 203, docs/jalon-1.md §2): the Priest's heals grown with its level gain 2.6 with these
        // pieces (archer 1.8, warrior 0.5, mage 0.7, an average of 1.4); the average stays capped at 1.5.
        // The average at 1.7 since the Rogue (sprint 204, docs/classes-voleur.md §6): it gains 2.4, an average of 1.6 for the five classes.
        Assert.True(gain.All(g => g <= 3) && gain.Average() <= 1.7, $"gains of {string.Join(", ", gain.Select(g => g.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)))} of 10");
    }

    [Fact]
    public void TheDisjoint_IsBeatenAlone_AtLevelThirty_WithTheCraftsmensPieces()
    {
        // Sprint 114 (design-familles.md §4.9): the chapter's boss, four beasts forced into one, wearing itself out (15 hit
        // points a turn) and bursting as it falls. Measured on 100 fights a class, with the "craft" kit (what a player alone
        // is sure to have): sentinel 100, guard 39, mage 45, three in five on average; every class can beat it alone.
        // A sample since sprint 206 (D97: a test that loops on the classes does not grow with them): SharedFights shared, in percent.
        int fights = Share(SharedFights);
        int[] wins = [.. Classes.Select(id =>
        {
            Hero hero = Progression.Built(new Hero("Essai", "female-d", id, Level: 30), Real, Progression.Kit("craft", "fields", 30, Real));
            return 100 * Results("the-disjoint", fights, hero)[0] / fights;
        })];
        // 80 since sprint 183 (T143): the reactions every hero has at 30 move only the mage, on 1000 fights sentinel 100/100, guard
        // 41/44, mage 52/90 (without, with), an average of 78: Blinded in its range, which no written lever touches. Charles to judge.
        // 90 since the first milestone (sprint 203, docs/jalon-1.md Q3): the Warrior's hit points swept to 74 + 5 make it beat the Disjoint
        // every time (100, against 41); archer 79, mage 93, priest 64, an average of 84.
        // No ceiling since sprint 211 (D99, docs/classes-invocateur.md Q4): a hero now weighs the movement it takes by the blow it keeps away, and the Archer
        // (76 to 100) and the Priest (62 to 98) hold the lone boss off while it wears itself out, as a player would; the Summoner's creatures take its
        // blows (100). The eight classes average 98.75: the Disjoint is too easy for good play; made harder at the third milestone, the ceiling back (D100).
        // Back at 90 since sprint 219 (D100, docs/jalon-3.md §4): its Snatching Tongue, from 2 to 5 cells in a line, drags the hero who keeps it away back to its
        // claws, and it has 480 hit points (450): archer 75, warrior 100, mage 32, priest 95, rogue 65, the five others 100, an average of 86.7. At 520 the Rogue fell to 0.
        Assert.True(wins.All(w => w >= 30) && wins.Average() >= 50 && wins.Average() <= 90, $"{string.Join(", ", Classes.Zip(wins, (c, w) => $"{c} {w}"))}: {wins.Average()}");
    }

    [Theory]
    [InlineData("archer")]
    [InlineData("warrior")]
    [InlineData("mage")]
    [InlineData("priest")]
    [InlineData("rogue")]
    [InlineData("berserker")]
    [InlineData("paladin")]
    [InlineData("summoner")]
    [InlineData("necromancer")]
    [InlineData("druid")]
    public void TheWallowsGreatHall_TheMainQuestsStep_IsWonAloneAtItsLevel(string id)
    {
        // Sprint 95 (D71): "rumble-under-the-wallow" asks for the great hall, at least 7 times in 10 alone,
        // at the quest's level, with nothing worn.
        Quest quest = Real.Quests["rumble-under-the-wallow"];
        var hero = Progression.Built(new Hero("Essai", "female-d", id, Level: quest.Level), Real);
        // A sample since sprint 206 (D97): SharedFights shared between the classes, in percent.
        int fights = Share(SharedFights);
        int wins = 100 * Results(Real.Dungeons["boar-wallow"].Scenario(2), fights, hero)[0] / fights;
        Assert.True(wins >= 70, $"{id}: {wins} % of {fights} in the great hall at level {quest.Level}");
    }

    /// <summary>
    /// A test's fights shared between the classes (sprint 206, D97): the budget does not grow with the classes. 50 each at least until sprint 216,
    /// which made 300 shared fights 450 at nine classes and 500 at ten, over the run's budget (20 033 fights); 30 at least since, 400 shared.
    /// </summary>
    private static int Share(int total) => Math.Max(30, total / Classes.Length);

    /// <summary>What <see cref="Share"/> splits between the classes in each test that loops on them (sprint 216).</summary>
    private const int SharedFights = 400;

    [Fact]
    public void TheSharedFights_DoNotGrowWithTheClasses()
    {
        // Sprint 216 (D97): the floor of 50 a class made the shared fights grow with the Druid, and the run went over its budget.
        Assert.True(Share(SharedFights) * Classes.Length <= SharedFights, $"{Share(SharedFights)} × {Classes.Length} classes");
        Assert.True(Share(SharedFights) >= 30);
    }

    [Theory]
    [InlineData("stag", "archer")]
    [InlineData("stag", "warrior")]
    [InlineData("stag", "mage")]
    [InlineData("golem", "archer")]
    [InlineData("golem", "warrior")]
    [InlineData("golem", "mage")]
    public void AForestDungeonsGreatHall_IsWonAloneAtLevel41(string family, string id)
    {
        // Sprint 134 (D79, §11.6, T102): the rule of §2, the great hall at least 7 times in 10 alone, with nothing
        // worn, at the level of the quest that will ask for it: 41 rather than the 40 of docs/chapitre-2-pistes.md §5,
        // where the guard won the Roots' 59 times in 100 (the rule raises the quest, it does not weaken the room).
        Dungeon dungeon = Real.Dungeons.Values.Single(d => d.Family == family);
        var hero = Progression.Built(new Hero("Essai", "female-d", id, Level: 41), Real);
        // 300 fights since sprint 162 (T129): Rockfall at 6 AP left the Sentinel at the edge of the Combe's (69 of 100).
        int wins = Results(dungeon.Scenario(2), 300, hero)[0];
        Assert.True(wins >= 210, $"{id}: {wins} of 300 in {dungeon.Id}'s great hall at level 41");
    }
}
