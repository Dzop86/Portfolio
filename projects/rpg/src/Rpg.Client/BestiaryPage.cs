using Rpg.Core;

namespace Rpg.Client;

/// <summary>A monster's numbers, known once met.</summary>
public sealed record BestiaryNumbers(int Hp, int Ap, int Mp, int Initiative);

/// <summary>An item a monster leaves: its rate (the item's card's own), how many, and the quest it falls for, if any.</summary>
public sealed record BestiaryLoot(string Item, int Percent, int Count, string? Quest);

/// <summary>A step of the card: reached or not, the victories it still asks, and the line said while it is not.</summary>
public sealed record BestiarySection(Knowledge Step, bool Reached, int Left, string Locked);

/// <summary>
/// A monster as the bestiary shows it (sprint 154; by blocks since sprint 171, D87, <c>docs/bestiaire-encyclopedie.md</c>
/// §4.3): who it is, always (a silhouette's role, level and zone until met); then, each with its step, its numbers and
/// the maps it lives on (met), its resistances, weakness and loot (studied), its spells, its family's lesson and how it
/// fights (known). What a step does not open yet is null.
/// </summary>
public sealed record BestiaryMonster(string Id, string Name, string Family, MonsterRole Role, int Level, string Zone, string Look, Knowledge Known, int Victories,
    (Knowledge Step, int Left)? Next, IReadOnlyList<BestiarySection> Sections, BestiaryNumbers? Numbers = null, IReadOnlyList<string>? Maps = null,
    IReadOnlyDictionary<Element, int>? Resistances = null, Element? Weakness = null, IReadOnlyList<BestiaryLoot>? Loot = null,
    IReadOnlyList<string>? Spells = null, IReadOnlyList<string>? Lessons = null, string? Behaviour = null);

/// <summary>A family in the bestiary, under its zone's name: its monsters, how many met and mastered.</summary>
public sealed record BestiaryFamily(string Id, string Zone, string Name, IReadOnlyList<BestiaryMonster> Monsters)
{
    public int Met => Monsters.Count(m => m.Known != Knowledge.Unknown);
    public int Mastered => Monsters.Count(m => m.Known == Knowledge.Mastered);
}

/// <summary>
/// The bestiary as the game shows it (sprint 154, D83), without any engine: the families by zone in the order of the world,
/// each monster's card by blocks opened by the steps of <see cref="Bestiary"/> (unknown, met, studied, known, mastered); an
/// item left for a quest shows only once the quest is under way or done.
/// </summary>
public static class BestiaryPage
{
    /// <summary>The elements a monster's resistances are shown for, in the sheet's order.</summary>
    public static readonly IReadOnlyList<Element> Elements = [Element.Neutral, Element.Earth, Element.Fire, Element.Water, Element.Air, Element.Lightning];

    public static IReadOnlyList<BestiaryFamily> Families(GameData data, IReadOnlyDictionary<string, int> victories, Texts texts, IReadOnlySet<string>? quests = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(victories);
        ArgumentNullException.ThrowIfNull(texts);
        return [.. data.Families.Values.OrderBy(f => f.Monsters.Min(m => m.Level)).ThenBy(f => f.Id, StringComparer.Ordinal)
            .Select(f => new BestiaryFamily(f.Id, texts.ZoneName(f.Zone, data), f.Name.In(texts.Lang),
                [.. f.Monsters.OrderBy(m => m.Role).Select(m => Monster(data, f, m, victories, texts, quests))]))];
    }

    public static BestiaryMonster Monster(GameData data, MonsterFamily family, MonsterSpec m, IReadOnlyDictionary<string, int> victories, Texts texts, IReadOnlySet<string>? quests = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(m);
        ArgumentNullException.ThrowIfNull(victories);
        ArgumentNullException.ThrowIfNull(texts);
        Knowledge known = Bestiary.Of(victories, m);
        int wins = victories.GetValueOrDefault(m.Id);
        (int studied, int knownAt, int mastered) = Bestiary.Steps(m);
        // A step not reached: one line, what it opens and the victories it still asks.
        BestiarySection Step(Knowledge step, int at, string opens) =>
            new(step, known >= step, Math.Max(0, at - wins), texts["bestiary.locked", texts["bestiary.step." + step], at, texts[opens]]);
        BestiarySection[] sections =
        [
            new(Knowledge.Met, known >= Knowledge.Met, 0, texts["bestiary.fight-it"]),
            Step(Knowledge.Studied, studied, "bestiary.opens.studied"),
            Step(Knowledge.Known, knownAt, "bestiary.opens.known"),
            Step(Knowledge.Mastered, mastered, "bestiary.opens.mastered"),
        ];
        string zone = texts.ZoneName(family.Zone, data);
        var monster = new BestiaryMonster(m.Id, known == Knowledge.Unknown ? texts["bestiary.unknown"] : m.Name.In(texts.Lang), family.Name.In(texts.Lang), m.Role, m.Level,
            zone, m.Look, known, wins, Bestiary.Next(victories, m), sections);
        if (known == Knowledge.Unknown)
            return monster;
        monster = monster with
        {
            Numbers = new BestiaryNumbers(m.Hp, m.Ap, m.Mp, m.Initiative),
            Maps = [.. data.Groups.Values.Where(g => g.Monsters.Contains(m.Id) && data.Towns.ContainsKey(g.Map)).Select(g => texts.MapTitle(data.Towns[g.Map])).Distinct().Order(StringComparer.Ordinal)],
        };
        if (known >= Knowledge.Studied)
            monster = monster with
            {
                Resistances = Elements.ToDictionary(e => e, e => m.Resistances?.GetValueOrDefault(e) ?? 0),
                Weakness = Bestiary.Weakness(m),
                Loot = [.. (m.Loot ?? []).Where(l => data.Items.ContainsKey(l.Item) && (l.Quest is null || quests?.Contains(l.Quest) == true))
                    .OrderByDescending(l => l.Percent).Select(l => new BestiaryLoot(l.Item, l.Percent, l.Count, l.Quest))],
            };
        if (known >= Knowledge.Known)
            monster = monster with
            {
                Spells = [.. m.Spells.Where(data.Spells.ContainsKey)],
                Lessons = [.. (m.Passives ?? []).Where(data.Passives.ContainsKey)],
                Behaviour = texts["bestiary.behaviour." + m.Role],
            };
        return monster;
    }
}
