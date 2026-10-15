using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A fighter's line in the end-of-fight window (sprint 87): its name and level; for the player, what
/// the fight gave (experience, Éclats, loot) and, when the server said it, where its experience bar
/// now stands (0 to 1 of the way to the next level).
/// </summary>
public sealed record SummaryRow(int Fighter, string Name, int Level, bool IsPlayer, long? Xp = null, long? Shards = null,
    IReadOnlyList<ItemCount>? Loot = null, double? XpBar = null, string? XpText = null);

/// <summary>
/// The end-of-fight window without any engine (sprint 87, D68), as in the games of the genre: Victory
/// or Defeat, the winners and the losers, a line for each, the player's rewards on its own line; a tab of
/// statistics (<see cref="FightStats"/>). Signed in, the rewards are the server's
/// (<see cref="FightResult"/>); offline, or until the server answers, those the rules would give.
/// </summary>
public sealed class FightSummary
{
    /// <param name="offline">No server keeps the fight: the window says nothing is kept.</param>
    public FightSummary(Fight fight, int playerTeam, FightResult? result, Texts texts, bool offline = false)
    {
        ArgumentNullException.ThrowIfNull(fight);
        ArgumentNullException.ThrowIfNull(texts);
        Title = fight.WinningTeam is null ? texts["draw"] : fight.WinningTeam == playerTeam ? texts["victory"] : texts["defeat"];
        Won = fight.WinningTeam == playerTeam;
        Fighter? hero = fight.Hero is null ? null : fight.HeroFighter;
        SummaryRow Row(Fighter f)
        {
            if (f != hero)
                return new SummaryRow(f.Id, texts.Name(f), f.Spec.Level, false);
            if (result is null)
                return new SummaryRow(f.Id, texts.Name(f), f.Spec.Level, true, Progression.FightXp(fight), Progression.FightShards(fight), Equipment.Loot(fight));
            long from = Progression.XpToReach(result.Level), to = Progression.XpToReach(result.Level + 1);
            bool max = result.Level >= Hero.MaxLevel;
            return new SummaryRow(f.Id, texts.Name(f), result.Level, true, result.Xp, result.Shards, result.Loot ?? [],
                max ? 1 : Math.Clamp((double)(result.TotalXp - from) / (to - from), 0, 1), texts.XpLine(result.Level, result.TotalXp));
        }
        int winners = fight.WinningTeam ?? playerTeam;
        Fighter[] people = [.. fight.Fighters.Where(f => !f.IsSummon)];
        Winners = [.. people.Where(f => f.Team == winners).Select(Row)];
        Losers = [.. people.Where(f => f.Team != winners).Select(Row)];
        Stats = [.. FightStats.Of(fight).Select(s => (texts.Name(fight.Fighters[s.Fighter]), s))];
        var notes = new List<string>();
        if (result?.DungeonDone == true)
            notes.Add(texts["dungeon.done"]);
        if (result?.Quest is string id && GameData.Embedded.Quests.TryGetValue(id, out Quest? quest))
            notes.Add(texts["result.quest", quest.Name.In(texts.Lang)]);
        if (result is not null && hero is not null && result.Level > fight.Hero!.Level)
            notes.Add(texts["result.level", result.Level]);
        if (offline && hero is not null && fight.Hero!.Class is not null)
            notes.Add(texts["result.offline"]);
        Notes = string.Join(" · ", notes);
    }

    /// <summary>"Victory!", "Defeat…" or "Draw".</summary>
    public string Title { get; }

    public bool Won { get; }

    /// <summary>The winning team's fighters (the player's team on a draw), summons left out.</summary>
    public IReadOnlyList<SummaryRow> Winners { get; }

    public IReadOnlyList<SummaryRow> Losers { get; }

    /// <summary>What each fighter did, with its name.</summary>
    public IReadOnlyList<(string Name, FighterStats Stats)> Stats { get; }

    /// <summary>A dungeon finished, a quest done, a new level, or that offline nothing is kept; empty otherwise.</summary>
    public string Notes { get; }

    /// <summary>The player's line.</summary>
    public SummaryRow? Player => Winners.Concat(Losers).FirstOrDefault(r => r.IsPlayer);
}
