using Rpg.Core;

namespace Rpg.Client;

/// <summary>The tree's layers as the window names them (sprint 149, D82, M1 a, names to be validated by the storyteller then Charles).</summary>
public enum TreeLayer
{
    /// <summary>The trunk: lit by itself.</summary>
    Foundations,

    /// <summary>The fork's six pairs.</summary>
    Affinities,

    /// <summary>The Hearts.</summary>
    Pure,

    /// <summary>A pair's braid (the Lightning one today).</summary>
    Fusion,

    /// <summary>The six styles shared by every pair.</summary>
    Styles,
}

/// <summary>
/// What the panel says of a node (sprint 149): its layer, the layer's banner, a line of lore (in italics), its name, a pair's
/// equation, what it does, its counterpart, its state, and a line more for some (Helping Hand's figures, a group node).
/// </summary>
public sealed record TreeCard(string Layer, string Banner, string Lore, string Name, string? Equation, string Effect, string Counterpart, string State, string? Extra)
{
    /// <summary>Its lines in the order the panel shows them, the empty ones left out.</summary>
    public IEnumerable<string> Lines => new[] { Name, Equation, Effect, Counterpart, Extra, State }.Where(l => !string.IsNullOrEmpty(l))!;
}

/// <summary>
/// What the tree's window explains of a build, without any engine (sprint 149, D82): the layer of a node, a title for the
/// build (N8 a), its summary (N1 a: every counterpart, and the best bonus a hit can reach against the ceiling of +35 %), the
/// pair the hero's two highest characteristics point to (C5 b), and the elements Helping Hand works with (D9 a).
/// </summary>
public static class TreeGuide
{
    public static TreeLayer LayerOf(TreeNode node, GameData data)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(data);
        if (node.IsTrunk)
            return TreeLayer.Foundations;
        Branch? branch = node.IsTalent ? data.Talents[node.Id].Branch : node.Branch;
        return branch switch
        {
            Branch.Fork => TreeLayer.Affinities,
            Branch.Pure => TreeLayer.Pure,
            Branch.Braids => TreeLayer.Fusion,
            null when node.IsComing => TreeLayer.Fusion,
            _ => TreeLayer.Styles,
        };
    }

    /// <summary>The compound a pair stands for, by its reaction (Electrifying: Lightning, with Fire and Air).</summary>
    public static ReactionSpec? PairOf(TalentSpec pair, GameData data)
    {
        ArgumentNullException.ThrowIfNull(pair);
        ArgumentNullException.ThrowIfNull(data);
        return pair.Rules.OfType<ReactionRule>().FirstOrDefault() is ReactionRule r ? data.Reactions[r.Reaction] : null;
    }

    /// <summary>
    /// The build's title (N8 a): its door (the pairs taken, or its Heart's element) then the style it has most nodes of;
    /// the Conversion said with its door. Keys of <see cref="Texts"/>: the window says them.
    /// </summary>
    public static (IReadOnlyList<string> Doors, Element? Heart, bool Converted, Branch? Style) Title(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        TalentSpec[] t = [.. taken.Select(id => data.Talents[id])];
        string[] doors = [.. t.Where(x => x.IsPair).Select(x => PairOf(x, data)!.Id)];
        Element? heart = t.Where(x => x.Branch == Branch.Pure).SelectMany(x => x.Rules.OfType<ElementRule>()).Select(r => (Element?)r.Element).FirstOrDefault();
        Branch? style = t.Where(x => TreeLayout.Styles.Contains(x.Branch)).GroupBy(x => x.Branch)
            .OrderByDescending(g => g.Count()).ThenBy(g => TreeLayout.Styles.ToList().IndexOf(g.Key)).Select(g => (Branch?)g.Key).FirstOrDefault();
        return (doors, heart, t.Any(x => x.IsSwitch), style);
    }

    /// <summary>
    /// The best bonus the build's talents can give one hit, unbounded (N1 a): every rule at its best at once. Over
    /// <see cref="Talents.Ceiling"/>, the window warns that a part is lost.
    /// </summary>
    public static int Best(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        TalentRule[] rules = [.. taken.SelectMany(id => data.Talents[id].Rules)];
        static int Up(int v) => Math.Max(0, v);
        return rules.OfType<DistanceRule>().Sum(r => Up(r.Percent))
            + rules.OfType<ElementRule>().Sum(r => Up(r.Percent))
            + rules.OfType<CompoundRule>().Sum(r => Up(r.Percent))
            + rules.OfType<BonusRule>().Sum(r => Up(r.Percent))
            + rules.OfType<StillRule>().Sum(r => Up(Math.Max(r.Still, r.Moved)))
            + rules.OfType<DanceRule>().Sum(r => Up(r.Max))
            + rules.OfType<ApCostRule>().Sum(r => Up(r.Percent))
            + rules.OfType<FirstCastRule>().Sum(r => Up(Math.Max(r.First, r.Others)))
            + rules.OfType<BackedRule>().Sum(r => Up(Math.Max(r.Damage, r.Otherwise)))
            + rules.OfType<GlyphTargetRule>().Sum(r => Up(Math.Max(r.Inside, r.Outside)))
            + rules.OfType<VirtuosoRule>().Sum(r => Up(r.Percent));
    }

    /// <summary>The pair the hero's two highest characteristics of element point to (C5 b); null before any point.</summary>
    public static string? Advice(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        Characteristics s = hero.Stats ?? Characteristics.None;
        Element[] two = [.. new[] { (Element.Earth, s.Earth), (Element.Fire, s.Fire), (Element.Water, s.Water), (Element.Air, s.Air) }
            .Where(x => x.Item2 > 0).OrderByDescending(x => x.Item2).ThenBy(x => x.Item1).Take(2).Select(x => x.Item1)];
        if (two.Length < 2)
            return null;
        return data.Talents.Values.Where(t => t.IsPair).FirstOrDefault(t => PairOf(t, data)!.Joins(two[0], two[1]))?.Id;
    }

    /// <summary>
    /// The bonus of the talents on a typical hit (sprint 150, M4 b): 2 cells away, without moving, the first damage spell of
    /// the turn, of 3 AP, in the hero's strongest base element; without <paramref name="talent"/>, then with it (a Conversion
    /// in its Transmutation's place, what hangs on a talent given back gone with it).
    /// </summary>
    public static (int Before, int After) BeforeAfter(IReadOnlyList<string> taken, string talent, Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        Characteristics s = hero.Stats ?? Characteristics.None;
        Element element = new[] { (Element.Earth, s.Earth), (Element.Fire, s.Fire), (Element.Water, s.Water), (Element.Air, s.Air) }.OrderByDescending(x => x.Item2).ThenBy(x => x.Item1).First().Item1;
        int On(IEnumerable<string> ids) => Talents.Percent([.. ids.Select(id => data.Talents[id])], new Cell(0, 0), new Cell(2, 0), element, new HitContext(ApCost: 3));
        List<string> without = taken.Contains(talent) ? Talents.Without(taken, talent, data) : [.. taken];
        List<string> with = taken.Contains(talent) ? [.. taken] : Talents.With(taken, talent, data);
        return (On(without), On(with));
    }

    /// <summary>The base elements Helping Hand helps with (D9 a): those of the pairs taken, else all four.</summary>
    public static IReadOnlyList<Element> Helps(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        Element[] mine = [.. taken.Select(id => data.Talents[id]).Where(t => t.IsPair).Select(t => PairOf(t, data)!).SelectMany(r => new[] { r.First, r.Second }).Distinct().Order()];
        return mine.Length > 0 ? mine : [Element.Earth, Element.Fire, Element.Water, Element.Air];
    }
}
