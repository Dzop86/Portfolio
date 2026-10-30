using Rpg.Core;

namespace Rpg.Client;

/// <summary>A monster as the bestiary shows it (sprint 154): its name (a silhouette when unknown), its look, what is known of it.</summary>
public sealed record BestiaryMonster(string Id, string Name, string Look, Knowledge Known, int Victories, IReadOnlyList<string> Lines);

/// <summary>A family in the bestiary, under its zone's name.</summary>
public sealed record BestiaryFamily(string Id, string Zone, string Name, IReadOnlyList<BestiaryMonster> Monsters);

/// <summary>
/// The bestiary as the game shows it (sprint 154, D83), without any engine: the families by zone in the order of the world,
/// each monster in silhouette until fought, then its role, level and zone; studied (a victory) its hit points, resistances
/// and weakness; known (five, two for a chief) its spells and its family's lesson; mastered (twenty-five, five for a chief)
/// the loot it drops, in words (sprint 168's steps, <see cref="Bestiary.Steps"/>; the card by blocks comes in sprint 169).
/// </summary>
public static class BestiaryPage
{
    public static IReadOnlyList<BestiaryFamily> Families(GameData data, IReadOnlyDictionary<string, int> victories, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(victories);
        ArgumentNullException.ThrowIfNull(texts);
        return [.. data.Families.Values.OrderBy(f => f.Monsters.Min(m => m.Level)).ThenBy(f => f.Id, StringComparer.Ordinal)
            .Select(f => new BestiaryFamily(f.Id, texts.ZoneName(f.Zone, data), f.Name.In(texts.Lang),
                [.. f.Monsters.OrderBy(m => m.Role).Select(m => Monster(data, f, m, victories, texts))]))];
    }

    public static BestiaryMonster Monster(GameData data, MonsterFamily family, MonsterSpec m, IReadOnlyDictionary<string, int> victories, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(m);
        ArgumentNullException.ThrowIfNull(victories);
        ArgumentNullException.ThrowIfNull(texts);
        Knowledge known = Bestiary.Of(victories, m);
        (Knowledge Step, int Left)? next = Bestiary.Next(victories, m);
        int wins = victories.GetValueOrDefault(m.Id);
        if (known == Knowledge.Unknown)
            return new BestiaryMonster(m.Id, texts["bestiary.unknown"], m.Look, known, 0, [texts["bestiary.fight-it"]]);
        var lines = new List<string> { texts["bestiary.met", texts["role." + m.Role], m.Level, texts.ZoneName(family.Zone, data)], texts["bestiary.wins", wins] };
        if (known >= Knowledge.Studied)
        {
            lines.Add(texts["bestiary.hp", m.Hp]);
            string resist = m.Resistances is { Count: > 0 } r ? string.Join(", ", r.OrderBy(x => x.Key).Select(x => $"{texts["element." + x.Key]} {x.Value:+0;−0;0} %")) : texts["bestiary.none"];
            lines.Add(texts["bestiary.resistances", resist]);
            lines.Add(Bestiary.Weakness(m) is Element weak ? texts["bestiary.weakness", texts["element." + weak]] : texts["bestiary.no-weakness"]);
        }
        else
            lines.Add(texts["bestiary.next", next!.Value.Left]);
        if (known >= Knowledge.Known)
        {
            lines.Add(texts["bestiary.spells", string.Join(", ", m.Spells.Where(data.Spells.ContainsKey).Select(s => data.Spells[s].Name.In(texts.Lang)))]);
            foreach (string p in (m.Passives ?? []).Where(data.Passives.ContainsKey))
                lines.Add(texts["bestiary.lesson", data.Passives[p].Name.In(texts.Lang), data.Passives[p].Description.In(texts.Lang)]);
        }
        else if (known >= Knowledge.Studied)
            lines.Add(texts["bestiary.next", next!.Value.Left]);
        if (known >= Knowledge.Mastered)
            lines.Add(texts["bestiary.loot", string.Join(", ", (m.Loot ?? []).Where(l => l.Quest is null && data.Items.ContainsKey(l.Item))
                .Select(l => $"{data.Items[l.Item].Name.In(texts.Lang)} ({texts["rarity." + Bestiary.RarityOf(l.Percent)]})"))]);
        else if (known >= Knowledge.Known)
            lines.Add(texts["bestiary.next", next!.Value.Left]);
        return new BestiaryMonster(m.Id, m.Name.In(texts.Lang), m.Look, known, wins, lines);
    }
}
