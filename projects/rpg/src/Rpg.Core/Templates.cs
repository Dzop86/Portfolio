namespace Rpg.Core;

/// <summary>
/// What a monster of a role has at a level (sprint 128, D79, docs/suite-apres-d69.md §11.6): its hit points, the
/// average damage of its main spell, the heal of a support, its resistance to its own element and its weakness, and
/// its initiative. <see cref="Hp"/>, <see cref="Damage"/> and <see cref="Heal"/> are a family's average: one monster may
/// stray from them, the family as a whole should not.
/// </summary>
public sealed record MonsterTemplate(int Hp, int Damage, int Heal, int Resistance, int Weakness, int Initiative)
{
    /// <summary>The ends of the main spell's roll: the average, less or more a seventh.</summary>
    public (int Min, int Max) Roll => (Damage - Damage / 7, Damage + Damage / 7);
}

/// <summary>
/// The yardsticks of the levels after 30 (sprint 128, D79): the monsters of the new regions follow them, and every
/// piece of equipment keeps to its budget. Drawn from the first chapter's families, from the lake to the fields
/// (levels 16 to 30), carried on by a straight line; checked by simulation at levels 35, 40 and 45 on the fields'
/// families made from them (<c>TemplateTests</c>, T96).
/// </summary>
public static class Templates
{
    /// <summary>The last level of the first chapter: the monsters above it have the initiative of <see cref="Initiative"/>.</summary>
    public const int FirstChapter = 30;

    /// <summary>How far a new region's monster may stray from <see cref="Initiative"/>, in percent.</summary>
    public const int InitiativeLeeway = 10;

    // By role: hit points at level 30 and, in tenths, how many more a level; the main spell's damage likewise, in tenths;
    // a support's heal likewise; resistance to its element, weakness; the initiative's base (§3.5).
    private static readonly Dictionary<MonsterRole, (int Hp, int HpStep, int Damage, int DamageStep, int Heal, int HealStep, int Resistance, int Weakness, int Initiative)> Roles = new()
    {
        [MonsterRole.Melee] = (60, 10, 220, 4, 0, 0, 10, -15, 70),
        [MonsterRole.Ranged] = (50, 10, 180, 4, 0, 0, 10, -15, 85),
        [MonsterRole.Tank] = (70, 14, 210, 4, 0, 0, 25, -15, 45),
        [MonsterRole.Support] = (62, 10, 210, 4, 170, 4, 10, -15, 60),
        [MonsterRole.Boss] = (390, 125, 340, 7, 0, 0, 30, -20, 60),
    };

    /// <summary>
    /// A monster's initiative after level 30 (§3.5): its role's base (melee 70, ranged 85, tank 45, support 60, chief 60)
    /// times 1 + level / 40, so that the new regions' monsters play earlier the deeper they live.
    /// </summary>
    public static int Initiative(MonsterRole role, int level) => Roles[role].Initiative * (40 + level) / 40;

    /// <summary>What a monster of this role has at this level (from level 30; under it, the first chapter's families are as written).</summary>
    public static MonsterTemplate Monster(MonsterRole role, int level)
    {
        var r = Roles[role];
        int above = level - FirstChapter;
        return new MonsterTemplate(r.Hp + r.HpStep * above / 10, (r.Damage + r.DamageStep * above) / 10, (r.Heal + r.HealStep * above) / 10,
            r.Resistance, r.Weakness, Initiative(role, level));
    }

    /// <summary>
    /// The characteristics a piece of equipment of this level may hold (sprint 109, D76): about half of 8 + its level,
    /// twice that for a two-handed weapon.
    /// </summary>
    public static int Budget(int level, ItemSlot slot) => (int)Math.Ceiling((8 + level) / 2.0) * (slot == ItemSlot.TwoHanded ? 2 : 1);

    /// <summary>What a piece spends of its budget: its characteristics, a point of initiative counting 0.3.</summary>
    public static double Spent(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Characteristics c = item.Stats ?? Characteristics.None;
        return c.Vitality + c.Earth + c.Fire + c.Water + c.Air + 0.3 * item.Initiative;
    }

    /// <summary>Whether a piece keeps to its budget, a point over allowed for rounding; the pet and the mount stay out of the rule.</summary>
    public static bool WithinBudget(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind != ItemKind.Equipment || item.Slot is not ItemSlot slot || slot is ItemSlot.Pet or ItemSlot.Mount
            || Spent(item) <= Budget(item.Level, slot) + 1;
    }

    /// <summary>Whether a monster's initiative is its role's at its level, within <see cref="InitiativeLeeway"/>; always so up to level 30.</summary>
    public static bool InitiativeFits(MonsterSpec monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        if (monster.Level <= FirstChapter)
            return true;
        int expected = Initiative(monster.Role, monster.Level);
        return Math.Abs(monster.Initiative - expected) * 100 <= expected * InitiativeLeeway;
    }

    /// <summary>The monsters of its own level a hero defeats to go up one (about 10, as in the first chapter's zones).</summary>
    public static double MonstersPerLevel(int level) =>
        (double)(Progression.XpToReach(level + 1) - Progression.XpToReach(level)) / Progression.MonsterXp(level);
}
