using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>What an achievement asks (sprint 155, D83): every goal reads what the server already keeps of a character.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AchievementGoal>))]
public enum AchievementGoal
{
    Waystones,
    Waystone,
    Victories,
    Known,
    FamilyVictories,
    Chief,
    Chiefs,
    JobLevel,
    JobsAt,
    GatherJobsAt,
    CraftJobsAt,
    QuestsDone,
    Quest,
    Contracts,
    Talents,
    Pair,
    Heart,
    Key,
    Talent,
    Feat,
    Guild,
    SharedFights,

    /// <summary>Every quest of <see cref="AchievementSpec.Targets"/> handed back (sprint 159, D84: a zone's line of quests).</summary>
    Quests,

    /// <summary>Every achievement of <see cref="AchievementSpec.Targets"/> earned (sprint 159: a zone's crown, never one of these).</summary>
    Achievements,

    /// <summary>The dungeon <see cref="AchievementSpec.Target"/> finished: its chief in the bestiary, or its "-cleared" quest handed back (sprint 159).</summary>
    Dungeon,
}

/// <summary>
/// An achievement of <c>data/achievements.json</c> (sprint 155): its category, its words, its points, its goal and, for some, a title;
/// since sprint 158 (D84), the group of its category it is shown under (none where the category has no groups); since
/// sprint 159, the level its reward is reckoned at and, for a line of quests or a crown, the quests or achievements it asks.
/// </summary>
public sealed record AchievementSpec(string Id, string Category, LocalizedText Name, LocalizedText Text, int Points, AchievementGoal Goal, int Count,
    string? Target = null, LocalizedText? Title = null, string? Group = null, int Level = 0, IReadOnlyList<string>? Targets = null);

/// <summary>What an achievement gives once claimed (sprint 159): experience and Éclats, never any power.</summary>
public sealed record AchievementReward(int Xp, int Shards);

/// <summary>
/// What the server knows of a character that the achievements read (sprint 155): nothing kept for them alone but the feats
/// (a fight in a group, a trade, a great horde at four), the shared fights won and the contracts handed back; the rest is
/// what the game already keeps, so that every achievement is retroactive.
/// </summary>
public sealed record AchievementState(IReadOnlyCollection<string> Waystones, IReadOnlyDictionary<string, int> Bestiary, IReadOnlyDictionary<string, int> JobLevels,
    IReadOnlyCollection<string> QuestsDone, int Contracts, IReadOnlyCollection<string> Talents, bool InGuild, int SharedWins, IReadOnlyCollection<string> Feats);

/// <summary>An achievement as a character stands toward it: how far, of how much.</summary>
public sealed record AchievementProgress(string Id, int Reached, int Count)
{
    [JsonIgnore]
    public bool Earned => Reached >= Count;
}

/// <summary>
/// The rules of the achievements (sprint 155, D83, the game designer's §12.4): about sixty in seven categories, their points
/// and a dozen cosmetic titles, never any power; counted by the server from what it keeps, so none is lost for good and none
/// asks to play on a given day.
/// </summary>
public static class Achievements
{
    /// <summary>
    /// The rewards' formula (sprint 159, the game designer's <c>docs/succes-recompenses-s159.md</c> §1): at its reference level L
    /// (10 at least), an achievement of p points gives p × L / 2 experience (p / 2 % of a level there) and p × L / 4 Éclats;
    /// never at the character's level, so that waiting to claim is no strategy.
    /// </summary>
    public const double XpPerPointLevel = 0.5, ShardsPerPointLevel = 0.25;

    public const int RewardFloor = 10;

    public static AchievementReward Reward(AchievementSpec a)
    {
        ArgumentNullException.ThrowIfNull(a);
        int level = Math.Max(RewardFloor, a.Level);
        return new AchievementReward((int)(a.Points * level * XpPerPointLevel), (int)(a.Points * level * ShardsPerPointLevel));
    }

    /// <summary>The ids of the achievements earned and not yet claimed.</summary>
    public static IReadOnlyList<string> Claimable(IEnumerable<string> earned, IReadOnlyCollection<string> claimed) =>
        [.. (earned ?? throw new ArgumentNullException(nameof(earned))).Where(id => !(claimed ?? throw new ArgumentNullException(nameof(claimed))).Contains(id))];

    /// <summary>The feats the server writes when they happen (the only state kept for the achievements alone).</summary>
    public const string Grouped = "grouped", Traded = "traded", GreatHorde = "horde-9-at-4";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The achievements built into the library (<c>data/achievements.json</c>), in their order.</summary>
    public static IReadOnlyList<AchievementSpec> All { get; } = Load();

    private static List<AchievementSpec> Load()
    {
        using Stream stream = typeof(Achievements).Assembly.GetManifestResourceStream("data/achievements.json")
            ?? throw new InvalidDataException("data/achievements.json is not built in.");
        return JsonSerializer.Deserialize<List<AchievementSpec>>(stream, Json) ?? throw new InvalidDataException("data/achievements.json is empty.");
    }

    /// <summary>How far a character has gone toward an achievement, never past its count.</summary>
    public static int Reached(AchievementSpec a, AchievementState s, GameData data)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(data);
        int FamilyWins(string family) => data.Families.TryGetValue(family, out MonsterFamily? f) ? f.Monsters.Sum(m => s.Bestiary.GetValueOrDefault(m.Id)) : 0;
        bool ChiefBeaten(string family) => data.Families.TryGetValue(family, out MonsterFamily? f) && s.Bestiary.GetValueOrDefault(f[MonsterRole.Boss].Id) > 0;
        // A dungeon finished: its chief only stands in the last room; before the bestiary (sprint 154), its quest says so.
        bool Finished(Dungeon d) => ChiefBeaten(d.Family) || s.QuestsDone.Contains($"{d.Id}-cleared");
        IEnumerable<TalentSpec> talents = s.Talents.Where(data.Talents.ContainsKey).Select(t => data.Talents[t]);
        int reached = a.Goal switch
        {
            AchievementGoal.Waystones => s.Waystones.Count,
            AchievementGoal.Waystone => s.Waystones.Contains(a.Target) ? 1 : 0,
            AchievementGoal.Victories => s.Bestiary.Values.Sum(),
            AchievementGoal.Known => s.Bestiary.Count,
            AchievementGoal.FamilyVictories => FamilyWins(a.Target ?? ""),
            AchievementGoal.Chief => ChiefBeaten(a.Target ?? "") ? 1 : 0,
            AchievementGoal.Chiefs => data.Dungeons.Values.Count(Finished),
            AchievementGoal.Dungeon => data.Dungeons.TryGetValue(a.Target ?? "", out Dungeon? d) && Finished(d) ? 1 : 0,
            AchievementGoal.Quests => (a.Targets ?? []).Count(s.QuestsDone.Contains),
            AchievementGoal.Achievements => (a.Targets ?? []).Count(t => All.FirstOrDefault(o => o.Id == t) is AchievementSpec o && Reached(o, s, data) >= o.Count),
            AchievementGoal.JobLevel => s.JobLevels.Values.DefaultIfEmpty(0).Max(),
            AchievementGoal.JobsAt => s.JobLevels.Values.Count(l => l >= 20),
            AchievementGoal.GatherJobsAt => data.Jobs.Values.Where(j => j.Gathers).All(j => s.JobLevels.GetValueOrDefault(j.Id) >= a.Count) ? a.Count
                : data.Jobs.Values.Where(j => j.Gathers).Min(j => s.JobLevels.GetValueOrDefault(j.Id)),
            AchievementGoal.CraftJobsAt => data.Jobs.Values.Where(j => !j.Gathers).All(j => s.JobLevels.GetValueOrDefault(j.Id) >= a.Count) ? a.Count
                : data.Jobs.Values.Where(j => !j.Gathers).Min(j => s.JobLevels.GetValueOrDefault(j.Id)),
            AchievementGoal.QuestsDone => s.QuestsDone.Count(data.Quests.ContainsKey),
            AchievementGoal.Quest => s.QuestsDone.Contains(a.Target) ? 1 : 0,
            AchievementGoal.Contracts => s.Contracts,
            AchievementGoal.Talents => s.Talents.Count,
            AchievementGoal.Pair => talents.Any(t => t.IsPair) ? 1 : 0,
            AchievementGoal.Heart => talents.Any(t => t.Branch == Branch.Pure) ? 1 : 0,
            AchievementGoal.Key => talents.Any(t => t.Tier == Tier.Key) ? 1 : 0,
            AchievementGoal.Talent => s.Talents.Contains(a.Target) ? 1 : 0,
            AchievementGoal.Feat => s.Feats.Contains(a.Target) ? 1 : 0,
            AchievementGoal.Guild => s.InGuild ? 1 : 0,
            AchievementGoal.SharedFights => s.SharedWins,
            _ => 0,
        };
        return Math.Min(a.Count, reached);
    }

    /// <summary>Every achievement as the character stands toward it.</summary>
    public static IReadOnlyList<AchievementProgress> Progress(AchievementState s, GameData data) =>
        [.. All.Select(a => new AchievementProgress(a.Id, Reached(a, s, data), a.Count))];

    /// <summary>The ids of the achievements earned.</summary>
    public static IReadOnlyList<string> Earned(AchievementState s, GameData data) => [.. Progress(s, data).Where(p => p.Earned).Select(p => p.Id)];

    /// <summary>The points of the achievements earned.</summary>
    public static int Points(IEnumerable<string> earned) => (earned ?? throw new ArgumentNullException(nameof(earned))).Sum(id => All.FirstOrDefault(a => a.Id == id)?.Points ?? 0);

    /// <summary>The achievements that give a title.</summary>
    public static IEnumerable<AchievementSpec> Titled => All.Where(a => a.Title is not null);

    /// <summary>Why a character may not wear this title, or null: one of an achievement it earned, or none (null) to wear nothing.</summary>
    public static string? WhyNotTitle(string? achievement, IReadOnlyCollection<string> earned)
    {
        ArgumentNullException.ThrowIfNull(earned);
        return achievement is null ? null
            : All.FirstOrDefault(a => a.Id == achievement) is not AchievementSpec a || a.Title is null ? "No such title."
            : !earned.Contains(achievement) ? "Not earned yet."
            : null;
    }
}
