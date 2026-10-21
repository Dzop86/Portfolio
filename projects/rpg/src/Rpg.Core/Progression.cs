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
    /// <param name="who">Since sprint 120, the hero of the party it is for (its level counts); the fight's hero when none.</param>
    public static long FightXp(Fight fight, Hero? who = null)
    {
        ArgumentNullException.ThrowIfNull(fight);
        if ((who ?? fight.Hero) is not Hero hero || fight.WinningTeam != 0)
            return 0;
        Fighter? rival = fight.Rival is null ? null : fight.Fighters.First(f => f.Team == 1);
        long xp = fight.Fighters.Where(f => f.Team != 0 && !f.IsSummon && f != rival && f.Spec.Level > hero.Level - LevelGap)
            .Sum(f => MonsterXp(f.Spec.Level));
        int allies = fight.Fighters.Count(f => f.Team == 0 && !f.IsSummon);
        return xp * (100 + GroupBonus * (allies - 1)) / 100;
    }

    /// <summary>The Éclats, the money (sprint 87, D68), a monster of this level leaves.</summary>
    public static long MonsterShards(int level) => 2 + level;

    /// <summary>
    /// The Éclats the hero earns from a fight: as for the experience, nothing unless its team won, and
    /// for each monster of the other team (neither a summon nor a rival hero) its worth, whatever its
    /// level; shared by nobody (each hero of a group would earn as much).
    /// </summary>
    public static long FightShards(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        if (fight.Hero is null || fight.WinningTeam != 0)
            return 0;
        Fighter? rival = fight.Rival is null ? null : fight.Fighters.First(f => f.Team == 1);
        return fight.Fighters.Where(f => f.Team != 0 && !f.IsSummon && f != rival).Sum(f => MonsterShards(f.Spec.Level));
    }

    /// <summary>
    /// A hero of this class and level with its points spent the simple way, for simulations: half the
    /// characteristic points in Vitality, the rest in the characteristics of its damaging spells'
    /// elements (in proportion to their number); spell points on the strongest unlocked damaging
    /// spells, a rank at a time, the cheapest step first; wearing <paramref name="worn"/> if given (sprint 95).
    /// </summary>
    public static Hero Built(Hero hero, GameData data, IReadOnlyDictionary<Slot, string>? worn = null)
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
        return hero with { Stats = stats, Ranks = ranks, Worn = worn ?? hero.Worn };
    }

    /// <summary>The kits a simulation can wear (sprint 95, D71): nothing, two quick pieces of the zone, all the quick ones; since sprint 109 (D76), the craftsmen's.</summary>
    public static readonly IReadOnlyList<string> Kits = ["none", "zone", "craft", "fast"];

    /// <summary>
    /// What a hero of this level wears in a simulation, from the sets of the zone's families: "none" nothing (how
    /// the floors of T41 are measured); "fast" in each slot the piece with the most initiative, of the quickest set,
    /// then the most characteristics, and since sprint 109 a craftsman's piece in the slots left empty (the best case);
    /// "zone" the two pieces of that kit with the most initiative only; "craft" (sprint 109, D76) those two, and in
    /// every other slot the best craftsman's piece of the hero's level or under, of any zone: what a player alone is
    /// sure to have. "zone" and "craft" are how the ceilings are measured.
    /// </summary>
    public static IReadOnlyDictionary<Slot, string> Kit(string kit, string zone, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var worn = new Dictionary<Slot, string>();
        if (kit == "none")
            return worn;
        if (!Kits.Contains(kit))
            throw new ArgumentException($"A kit is one of: {string.Join(", ", Kits)}.", nameof(kit));
        HashSet<string> sets = [.. data.Families.Values.Where(f => f.Zone == zone).Select(f => f.Id)];
        Item[] pieces = [.. data.Items.Values.Where(i => i.Set is string s && sets.Contains(s) && i.Level <= level)
            .OrderByDescending(i => i.Initiative).ThenByDescending(i => SetInitiative(i.Set!, data)).ThenByDescending(i => Sum(i.Stats)).ThenBy(i => i.Id, StringComparer.Ordinal)];
        foreach (Item piece in pieces.Where(p => !worn.ContainsValue(p.Id)))
        {
            foreach (Slot slot in piece.Slots.Where(s => !worn.ContainsKey(s)))
            {
                if (Equipment.Problem(new Dictionary<Slot, string>(worn) { [slot] = piece.Id }, level, data) is null)
                {
                    worn[slot] = piece.Id;
                    break;
                }
            }
        }
        if (kit != "fast")
            worn = worn.Where(w => data.Items[w.Value].Initiative > 0).OrderByDescending(w => data.Items[w.Value].Initiative).ThenBy(w => w.Key).Take(2).ToDictionary();
        return kit == "zone" ? worn : WithMadePieces(worn, level, data);
    }

    /// <summary>
    /// What is worn, the empty slots filled with the best craftsman's pieces of the level or under: the most characteristics
    /// first; both hands with whichever holds more, a two-handed weapon or a one-handed weapon and a shield.
    /// </summary>
    private static Dictionary<Slot, string> WithMadePieces(Dictionary<Slot, string> worn, int level, GameData data)
    {
        Item[] made = [.. data.MadePieces.Where(i => i.Level <= level).OrderByDescending(i => Sum(i.Stats)).ThenBy(i => i.Id, StringComparer.Ordinal)];
        Item? Best(ItemSlot slot) => made.FirstOrDefault(i => i.Slot == slot);
        var filled = new Dictionary<Slot, string>(worn);
        bool handsFree = !worn.ContainsKey(Slot.OneHanded) && !worn.ContainsKey(Slot.TwoHanded) && !worn.ContainsKey(Slot.Shield);
        if (handsFree)
        {
            Item? two = Best(ItemSlot.TwoHanded), one = Best(ItemSlot.OneHanded), shield = Best(ItemSlot.Shield);
            if (two is not null && Sum(two.Stats) > Sum(one?.Stats) + Sum(shield?.Stats))
                filled[Slot.TwoHanded] = two.Id;
            else
            {
                if (one is not null)
                    filled[Slot.OneHanded] = one.Id;
                if (shield is not null)
                    filled[Slot.Shield] = shield.Id;
            }
        }
        foreach (Item piece in made.Where(p => p.Slot is not (ItemSlot.OneHanded or ItemSlot.TwoHanded or ItemSlot.Shield)))
        {
            Slot? free = piece.Slots.Cast<Slot?>().FirstOrDefault(s => !filled.ContainsKey(s!.Value));
            if (free is Slot slot && !filled.ContainsValue(piece.Id))
                filled[slot] = piece.Id;
        }
        return filled;
    }

    /// <summary>The initiative of a whole set: its pieces' and its best bonus (the quickest set's pieces come first on a tie).</summary>
    private static int SetInitiative(string set, GameData data) =>
        data.Items.Values.Where(i => i.Set == set).Sum(i => i.Initiative) + (data.Sets.TryGetValue(set, out ItemSet? s) ? s.Bonuses.Max(b => b.Initiative) : 0);

    private static int Sum(Characteristics? c) => c is null ? 0 : c.Vitality + c.Earth + c.Fire + c.Water + c.Air;
}
