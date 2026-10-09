namespace Rpg.Core;

/// <summary>
/// How a hero progresses (sprint 58): experience from won fights and quests, the levels it reaches,
/// and the points each level gives: 10 characteristic points and 1 spell point. A spell's rank costs
/// more the higher it goes (<see cref="RankCost"/>). The server applies these rules; the client only
/// shows them.
/// </summary>
public static class Progression
{
    public const int CharacteristicPointsPerLevel = 10;
    public const int SpellPointsPerLevel = 1;

    /// <summary>A monster this many levels below the hero, or more, gives nothing.</summary>
    public const int LevelGap = 20;

    /// <summary>Percent more experience for each ally beyond the first (the group bonus).</summary>
    public const int GroupBonus = 10;

    /// <summary>The experience a hero needs in all to reach a level: 0 for level 1, 100 for 2, 495,000 for 100.</summary>
    public static long XpToReach(int level) => 50L * (Math.Clamp(level, 1, Hero.MaxLevel) - 1) * Math.Clamp(level, 1, Hero.MaxLevel);

    /// <summary>The level a hero with this much experience has.</summary>
    public static int LevelFor(long xp)
    {
        int level = 1;
        while (level < Hero.MaxLevel && XpToReach(level + 1) <= xp)
            level++;
        return level;
    }

    public static int CharacteristicPoints(int level) => CharacteristicPointsPerLevel * (level - 1);

    public static int SpellPoints(int level) => SpellPointsPerLevel * (level - 1);

    /// <summary>The spell points a rank costs in all from rank 1: 1 for rank 2, 3 for rank 3, 10 for rank 5.</summary>
    public static int RankCost(int rank) => rank * (rank - 1) / 2;

    /// <summary>What defeating a monster of this level is worth.</summary>
    public static long MonsterXp(int level) => 20 + 10L * level;

    /// <summary>
    /// The experience the hero earns from a fight: nothing unless its team won; for each monster of
    /// the other team (neither a summon nor a rival hero), its worth, unless it is
    /// <see cref="LevelGap"/> levels below the hero or more; then the group bonus for each ally.
    /// </summary>
    public static long FightXp(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        if (fight.Hero is not Hero hero || fight.WinningTeam != 0)
            return 0;
        Fighter? rival = fight.Rival is null ? null : fight.Fighters.First(f => f.Team == 1);
        long xp = fight.Fighters.Where(f => f.Team != 0 && !f.IsSummon && f != rival && f.Spec.Level > hero.Level - LevelGap)
            .Sum(f => MonsterXp(f.Spec.Level));
        int allies = fight.Fighters.Count(f => f.Team == 0 && !f.IsSummon);
        return xp * (100 + GroupBonus * (allies - 1)) / 100;
    }

    /// <summary>
    /// A hero of this class and level with its points spent the simple way, for simulations: half the
    /// characteristic points in Vitality, the rest in the characteristics of its damaging spells'
    /// elements (in proportion to their number); spell points on the strongest unlocked damaging
    /// spells, a rank at a time, the cheapest step first.
    /// </summary>
    public static Hero Built(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        if (hero.Class is null || data.Class(hero.Class) is not HeroClass c)
            return hero;
        Spell[] damaging = [.. c.Spells.Select(id => data.Spells[id]).Where(s => s.Level <= hero.Level && s.DamageMax > 0)];
        int points = CharacteristicPoints(hero.Level);
        int vitality = points / 2;
        Element[] elements = [.. damaging.Select(s => s.Element == Element.Neutral ? Element.Earth : s.Element)];
        int Share(Element e) => elements.Length == 0 ? 0 : (points - vitality) * elements.Count(x => x == e) / elements.Length;
        var stats = new Characteristics(points - Share(Element.Earth) - Share(Element.Fire) - Share(Element.Water) - Share(Element.Air),
            Share(Element.Earth), Share(Element.Fire), Share(Element.Water), Share(Element.Air));
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        int spent = 0;
        while (true)
        {
            // The next step: the cheapest, then the strongest spell (on average), then the first listed.
            Spell? next = damaging.Where(s => ranks.GetValueOrDefault(s.Id, 1) < s.MaxRank)
                .OrderBy(s => ranks.GetValueOrDefault(s.Id, 1)).ThenByDescending(s => s.AverageDamage).FirstOrDefault();
            if (next is null)
                break;
            int rank = ranks.GetValueOrDefault(next.Id, 1) + 1;
            int step = RankCost(rank) - RankCost(rank - 1);
            if (spent + step > SpellPoints(hero.Level))
                break;
            spent += step;
            ranks[next.Id] = rank;
        }
        return hero with { Stats = stats, Ranks = ranks };
    }
}
