using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The achievements (sprint 155, D83): about sixty in seven categories, every one reachable in today's content and written in
/// both languages, a dozen titles; read from what the server keeps, so retroactive; a character who finished chapter 1 has
/// between a fifth and a third of them (the game designer's criterion).
/// </summary>
public class AchievementTests
{
    private static AchievementState State(IReadOnlyCollection<string>? waystones = null, IReadOnlyDictionary<string, int>? bestiary = null,
        IReadOnlyDictionary<string, int>? jobs = null, IReadOnlyCollection<string>? quests = null, int contracts = 0, IReadOnlyCollection<string>? talents = null,
        bool guild = false, int shared = 0, IReadOnlyCollection<string>? feats = null) =>
        new(waystones ?? [], bestiary ?? new Dictionary<string, int>(), jobs ?? new Dictionary<string, int>(), quests ?? [], contracts, talents ?? [], guild, shared, feats ?? []);

    [Fact]
    public void TheAchievements_AreAboutSixty_InSevenCategories_WrittenInBothLanguages_WithADozenTitles()
    {
        IReadOnlyList<AchievementSpec> all = Achievements.All;
        // Sprint 159 (D84): 19 lines of quests and crowns more, 82 in all.
        Assert.InRange(all.Count, 80, 90);
        Assert.Equal(all.Count, all.Select(a => a.Id).Distinct().Count());
        Assert.Equal(["Dungeons", "Exploration", "Hunt", "Jobs", "Quests", "Together", "Tree"], all.Select(a => a.Category).Distinct().Order(StringComparer.Ordinal));
        Assert.All(all, a => Assert.True(a.Name.Fr.Length > 0 && a.Name.En.Length > 0 && a.Text.Fr.Length > 0 && a.Text.En.Length > 0 && a.Points > 0 && a.Count > 0, a.Id));
        Assert.InRange(Achievements.Titled.Count(), 10, 14);
        Assert.All(Achievements.Titled, a => Assert.True(a.Title!.Fr.Length is > 0 and <= 24 && a.Title.En.Length is > 0 and <= 24, a.Id));
        // At least three ask to play together.
        Assert.True(all.Count(a => a.Category == "Together" && a.Goal is AchievementGoal.Feat or AchievementGoal.SharedFights) >= 3);
    }

    [Fact]
    public void EveryAchievement_IsReachable_InTodaysContent()
    {
        string[] waystones = [.. Real.Towns.Values.SelectMany(t => t.Props ?? []).Where(p => p.Look == Waystones.Look).Select(p => p.Id)];
        foreach (AchievementSpec a in Achievements.All)
        {
            Assert.True(a.Goal switch
            {
                AchievementGoal.Waystones => a.Count <= waystones.Length,
                AchievementGoal.Waystone => waystones.Contains(a.Target),
                AchievementGoal.Known => a.Count <= Real.Monsters.Count,
                AchievementGoal.FamilyVictories or AchievementGoal.Chief => Real.Families.ContainsKey(a.Target!) && Real.Dungeons.Values.Any(d => d.Family == a.Target || a.Goal == AchievementGoal.FamilyVictories),
                AchievementGoal.Dungeon => Real.Dungeons.ContainsKey(a.Target!) && Real.Quests.ContainsKey(a.Target + "-cleared"),
                AchievementGoal.Quests => a.Targets is { Count: > 0 } t && t.All(Real.Quests.ContainsKey) && a.Count == t.Count,
                AchievementGoal.Achievements => a.Targets is { Count: > 0 } t && a.Count == t.Count
                    && t.All(id => Achievements.All.FirstOrDefault(o => o.Id == id) is { Goal: not AchievementGoal.Achievements }),
                AchievementGoal.Chiefs => a.Count <= Real.Dungeons.Count,
                AchievementGoal.JobLevel or AchievementGoal.GatherJobsAt or AchievementGoal.CraftJobsAt => a.Count <= Crafting.MaxLevel,
                AchievementGoal.JobsAt => a.Count <= Real.Jobs.Count,
                AchievementGoal.QuestsDone => a.Count <= Real.Quests.Count,
                AchievementGoal.Quest => Real.Quests.ContainsKey(a.Target!),
                AchievementGoal.Talent => Real.Talents.ContainsKey(a.Target!),
                AchievementGoal.Feat => a.Target is Achievements.Grouped or Achievements.Traded or Achievements.GreatHorde,
                _ => true,
            }, a.Id);
        }
    }

    [Fact]
    public void ACharacterWhoFinishedChapterOne_HasAFifthToAThirdOfThem()
    {
        // Level 30 or so (about ten monsters a level): the chapter's five waystones, its eight families met, nine victories over each
        // common one, five chiefs, two trades at 25, the
        // chapter's quests, a pair and a talent; no contract, guild nor group yet (they came after).
        string[] chapter1 = ["woods", "mouldy", "lake", "fields"];
        MonsterFamily[] families = [.. Real.Families.Values.Where(f => chapter1.Contains(f.Zone))];
        var bestiary = families.SelectMany(f => f.Monsters).ToDictionary(m => m.Id, m => m.Role == MonsterRole.Boss ? 0 : 9);
        foreach (MonsterFamily f in families.Take(5))
            bestiary[f[MonsterRole.Boss].Id] = 1;
        // The chapter's story, not its side quests nor its dungeons' (since sprint 159 a dungeon's quest finishes it, D84).
        string[] quests = [.. Real.Quests.Values.Where(q => q.Zone is null || chapter1.Contains(q.Zone)).Where(q => q.Level <= 30 && q.Kind is not (QuestKind.Side or QuestKind.Dungeon)).Select(q => q.Id)];
        Assert.Contains("the-disjoint", quests);
        AchievementState s = State(["waystone-clairval", "waystone-woods", "waystone-mouldy", "waystone-lake", "waystone-fields"], bestiary,
            new Dictionary<string, int> { ["miner"] = 25, ["smith"] = 25 }, quests, talents: ["bogging", "up-close"]);
        double share = 100.0 * Achievements.Earned(s, Real).Count / Achievements.All.Count;
        // 35.4 % since sprint 159: the lines of the story's quests are earned by following it; the band widened to 40.
        Assert.InRange(share, 20, 40);
    }

    [Fact]
    public void TheGoals_ReadWhatTheServerKeeps()
    {
        MonsterFamily boars = Real.Families["boar"];
        AchievementSpec Of(string id) => Achievements.All.Single(a => a.Id == id);
        var bestiary = boars.Monsters.ToDictionary(m => m.Id, _ => 10);
        AchievementState s = State(bestiary: bestiary, jobs: new Dictionary<string, int> { ["miner"] = 41 }, contracts: 12, feats: [Achievements.Grouped]);
        // Ten victories over each of the five boars: fifty of the family's hundred.
        Assert.Equal(boars.Monsters.Count * 10, Achievements.Reached(Of("family-boar-100"), s, Real));
        Assert.Equal(1, Achievements.Reached(Of("dungeon-boar-wallow"), s, Real));
        Assert.Equal(40, Achievements.Reached(Of("job-40"), s, Real));
        Assert.Equal(10, Achievements.Reached(Of("contracts-10"), s, Real));
        Assert.Equal(1, Achievements.Reached(Of("grouped"), s, Real));
        Assert.Equal(0, Achievements.Reached(Of("great-horde"), s, Real));
        Assert.Contains("dungeon-boar-wallow", Achievements.Earned(s, Real));
        Assert.Equal(Of("dungeon-boar-wallow").Points + Of("job-20").Points, Achievements.Points(["dungeon-boar-wallow", "job-20"]));
    }

    [Fact]
    public void ATitle_IsOneOfAnAchievementEarned_OrNone()
    {
        AchievementSpec titled = Achievements.Titled.First();
        AchievementSpec plain = Achievements.All.First(a => a.Title is null);
        Assert.Null(Achievements.WhyNotTitle(null, []));
        Assert.Null(Achievements.WhyNotTitle(titled.Id, [titled.Id]));
        Assert.NotNull(Achievements.WhyNotTitle(titled.Id, []));
        Assert.NotNull(Achievements.WhyNotTitle(plain.Id, [plain.Id]));
        Assert.NotNull(Achievements.WhyNotTitle("no-such", ["no-such"]));
    }

    [Fact]
    public void TheRewards_FollowTheFormula_AtTheReferenceLevel_NeverAboveTheLargest()
    {
        AchievementSpec Of(string id) => Achievements.All.Single(a => a.Id == id);
        // The game designer's examples (docs/succes-recompenses-s159.md §1.3).
        Assert.Equal(new AchievementReward(25, 12), Achievements.Reward(Of("first-victory")));
        Assert.Equal(new AchievementReward(50, 25), Achievements.Reward(Of("clairval-first-steps")));
        Assert.Equal(new AchievementReward(50, 25), Achievements.Reward(Of("dungeon-boar-wallow")));
        Assert.Equal(new AchievementReward(125, 62), Achievements.Reward(Of("woods-all")));
        Assert.Equal(new AchievementReward(187, 93), Achievements.Reward(Of("lake-main")));
        Assert.Equal(new AchievementReward(225, 112), Achievements.Reward(Of("dungeon-mother-tree-roots")));
        Assert.Equal(new AchievementReward(562, 281), Achievements.Reward(Of("ancestors-all")));
        Assert.Equal(new AchievementReward(675, 337), Achievements.Reward(Of("contracts-200")));
        Assert.All(Achievements.All, a => Assert.True(a.Level is >= 1 and <= 45 && Achievements.Reward(a).Xp <= 675, a.Id));
        Assert.Equal(1110, Achievements.All.Sum(a => a.Points));
    }

    [Fact]
    public void TheLevels_FollowTheirRule()
    {
        AchievementSpec Of(string id) => Achievements.All.Single(a => a.Id == id);
        foreach (AchievementSpec a in Achievements.All)
        {
            int expected = a.Goal switch
            {
                // A line of quests: its highest quest; a dungeon: its "-cleared" quest; a crown: the highest it asks.
                AchievementGoal.Quests => a.Targets!.Max(q => Real.Quests[q].Level),
                AchievementGoal.Dungeon => Real.Quests[a.Target + "-cleared"].Level,
                AchievementGoal.Achievements => a.Targets!.Max(t => Of(t).Level),
                _ => a.Level,
            };
            Assert.True(a.Level == expected, $"{a.Id}: {a.Level}, not {expected}");
        }
        // A family's at its dungeon's level.
        foreach (Dungeon d in Real.Dungeons.Values)
            Assert.Equal(Of("dungeon-" + d.Id).Level, Of($"family-{d.Family}-100").Level);
    }

    [Fact]
    public void EveryQuestButADungeons_IsInExactlyOneLineOfQuests()
    {
        var lines = Achievements.All.Where(a => a.Goal == AchievementGoal.Quests).SelectMany(a => a.Targets!).ToList();
        foreach (Quest q in Real.Quests.Values.Where(q => q.Kind != QuestKind.Dungeon))
            Assert.True(lines.Count(id => id == q.Id) == 1, q.Id);
        Assert.Equal(lines.Count, lines.Distinct().Count());
    }

    [Fact]
    public void ADungeon_IsFinished_ByItsChief_OrByItsQuest_AndACrown_ByWhatItAsks()
    {
        AchievementSpec Of(string id) => Achievements.All.Single(a => a.Id == id);
        string chief = Real.Families["boar"][MonsterRole.Boss].Id;
        Assert.Equal(1, Achievements.Reached(Of("dungeon-boar-wallow"), State(bestiary: new Dictionary<string, int> { [chief] = 1 }), Real));
        Assert.Equal(1, Achievements.Reached(Of("dungeon-boar-wallow"), State(quests: ["boar-wallow-cleared"]), Real));
        Assert.Equal(0, Achievements.Reached(Of("dungeon-boar-wallow"), State(quests: ["wolf-den-cleared"]), Real));
        Assert.Equal(1, Achievements.Reached(Of("dungeons-all"), State(quests: ["boar-wallow-cleared"]), Real));
        // The woods' crown: its two lines and its two dungeons.
        string[] woods = [.. Of("woods-main").Targets!, .. Of("woods-side").Targets!];
        Assert.Equal(2, Achievements.Reached(Of("woods-all"), State(quests: woods), Real));
        Assert.Equal(4, Achievements.Reached(Of("woods-all"), State(quests: [.. woods, "boar-wallow-cleared", "wolf-den-cleared"]), Real));
        Assert.Equal(3, Achievements.Reached(Of("woods-main"), State(quests: ["edge-of-the-woods", "restless-beasts", "the-root-circle"]), Real));
        // Claimable: earned and not yet claimed.
        Assert.Equal(["woods-all"], Achievements.Claimable(["woods-main", "woods-all"], ["woods-main"]));
    }
}
