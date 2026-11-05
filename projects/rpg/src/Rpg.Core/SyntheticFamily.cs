namespace Rpg.Core;

/// <summary>A family made from the templates at a zone's level, as the fields' are at 26: its groups, its spells, its kits.</summary>
public sealed class SyntheticFamily
{
    private static GameData Real => GameData.Embedded;

    private static readonly LocalizedText Text = new("essai", "test");

    public GameData Data { get; }
    public int Level { get; }
    public IReadOnlyList<(string Scenario, int Size, bool InTheOpen)> Groups { get; }

    public SyntheticFamily(int level)
    {
        Level = level;
        int by = level - Fields;
        var spells = new Dictionary<string, Spell>(StringComparer.Ordinal);
        var monsters = new Dictionary<string, FighterSpec>(StringComparer.Ordinal);
        // Each monster of the fields' families, as many levels higher, given its role's template at its new level: hit
        // points and initiative; its main spell (its first that hurts) the template's roll, its other spells in the same
        // proportion, a support's heal the template's; its resistances the template's, on the same elements; its passives.
        foreach ((MonsterFamily family, MonsterSpec m) in Real.Families.Values.Where(f => f.Zone == "fields").SelectMany(f => f.Monsters.Select(m => (f, m))))
        {
            int at = m.Level + by;
            MonsterTemplate t = Templates.Monster(m.Role, at);
            Spell main = m.Spells.Select(id => Real.Spells[id]).First(s => s.DamageMax > 0);
            double ratio = t.Damage / ((main.DamageMin + main.DamageMax) / 2.0);
            var cast = new List<string>();
            foreach (Spell s in m.Spells.Select(id => Real.Spells[id]))
            {
                IReadOnlyList<SpellEffect>? effects = s.Effects?.Select(e => e switch
                {
                    HealEffect h => h with { Min = t.Heal - t.Heal / 7, Max = t.Heal + t.Heal / 7 },
                    ShieldEffect sh => sh with { Amount = (int)Math.Round(sh.Amount * ratio) },
                    _ => e,
                }).ToList();
                Spell made = s == main
                    ? s with { DamageMin = t.Roll.Min, DamageMax = t.Roll.Max }
                    : s with { DamageMin = (int)Math.Round(s.DamageMin * ratio), DamageMax = (int)Math.Round(s.DamageMax * ratio) };
                made = made with { Id = $"synth-{m.Id}-{s.Id}", Effects = effects };
                spells[made.Id] = made;
                cast.Add(made.Id);
            }
            Dictionary<Element, int> resistances = (m.Resistances ?? new Dictionary<Element, int>()).ToDictionary(r => r.Key, r => r.Value > 0 ? t.Resistance : t.Weakness);
            monsters[m.Id] = m.Fighter(0, family: family.Id) with { Hp = t.Hp, Initiative = t.Initiative, Spells = cast, Resistances = resistances, Level = at };
        }
        // The fields' groups, on the same battle maps.
        FighterSpec hero = new(Text, "female-d", 0, 80, 6, 3, 120, 0, ["strike"]);
        var scenarios = new List<Scenario>();
        var groups = new List<(string, int, bool)>();
        foreach (MonsterGroup g in Real.Groups.Values.Where(g => Real.Towns[g.Map].Zone == "fields" && g.Fight is null).OrderBy(g => g.Id, StringComparer.Ordinal))
        {
            string id = $"synth:{g.Id}";
            scenarios.Add(new Scenario(id, Text, Real.Families[g.Family].BattleOf(g.Id), [hero, .. g.Monsters.Select((m, i) => monsters[m] with { Start = i })]));
            // The Meadow's hordes of clod moles, left out of the "craft" kit's ceiling as in the fields (T64, T77).
            bool open = Real.Families[g.Family].BattleOf(g.Id) == "meadow" && g.Monsters.Count(m => m == "clod-mole") >= 3;
            groups.Add((id, g.Monsters.Count, open));
        }
        Groups = groups;
        Data = new GameData([.. Real.Spells.Values, .. spells.Values], Real.Maps.Values, [.. Real.Scenarios.Values, .. scenarios], Real.Classes,
            summons: Real.Summons.Values, items: [.. Real.Items.Values, .. Pieces(level)], sets: Real.Sets.Values, passives: Real.Passives.Values,
            talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);
    }

    /// <summary>
    /// A solid horde (sprint 162, T129): seven monsters or more, or one with three times the hit points of the weakest. The
    /// others, six small monsters without a chief, fall to any hero of one element well equipped (2.9 of 10 alone); a
    /// content flaw, left to the Mines (a solid monster in every horde), and printed without a ceiling.
    /// </summary>
    public bool Solid(string scenario)
    {
        int[] hp = [.. Data.FindScenario(scenario)!.Fighters.Where(f => f.Team == 1).Select(f => f.Hp)];
        return hp.Length >= 7 || hp.Max() >= 3 * hp.Min();
    }

    /// <summary>The level the fields' kits are measured at (T41), which the kits of the synthetic family are carried from.</summary>
    private const int Fields = 26;

    private static readonly string[] Kits = ["zone", "craft"];

    /// <summary>
    /// The pieces of the kits "zone" and "craft" at the fields' level (T41), carried to this level: the same slots and
    /// elements, each piece as many levels higher, its characteristics brought to the budget of its new level in the same
    /// proportions, its initiative kept.
    /// </summary>
    private static IEnumerable<Item> Pieces(int level)
    {
        int by = level - Fields;
        foreach (string id in Kits.SelectMany(k => Progression.Kit(k, "fields", Fields, Real).Values).Distinct())
        {
            Item real = Real.Items[id];
            int budget = Templates.Budget(real.Level + by, real.Slot!.Value) - (int)Math.Ceiling(0.3 * real.Initiative);
            Characteristics c = real.Stats!;
            double total = c.Vitality + c.Earth + c.Fire + c.Water + c.Air;
            int Part(int v) => (int)Math.Round(v * budget / total);
            int vitality = Part(c.Vitality), earth = Part(c.Earth), fire = Part(c.Fire), water = Part(c.Water);
            int air = c.Air > 0 ? budget - vitality - earth - fire - water : 0;
            yield return real with { Id = "synth-" + id, Level = real.Level + by, Stats = new Characteristics(vitality, earth, fire, water, air) };
        }
    }

    /// <summary>The hero of a class at the level, its points spent the simple way, wearing a kit ("none", "zone" or "craft") carried from the fields'.</summary>
    public Hero Hero(string id, string kit)
    {
        var worn = Progression.Kit(kit, "fields", Fields, Real).ToDictionary(w => w.Key, w => "synth-" + w.Value);
        if (Equipment.Problem(worn, Level, Data) is { } problem)
            throw new InvalidOperationException($"The kit '{kit}' is not wearable at {Level}: {problem}.");
        return Progression.Built(new Hero("Essai", "female-d", id, Level: Level), Data, worn);
    }

    public int Wins(string scenario, Hero hero, int fights)
    {
        int wins = 0;
        for (ulong seed = 0; seed < (ulong)fights; seed++)
        {
            var f = new Fight(Data, scenario, seed, hero);
            Ai.PlayOut(f);
            wins += f.WinningTeam == 0 ? 1 : 0;
        }
        return wins;
    }
}
