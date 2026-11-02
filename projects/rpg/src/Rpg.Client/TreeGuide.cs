using Rpg.Core;

namespace Rpg.Client;

/// <summary>The tree's layers as the window names them (sprint 149, D82, M1 a, names to be validated by the storyteller then Charles).</summary>
public enum TreeLayer
{
    /// <summary>The trunk: lit by itself.</summary>
    Foundations,

    /// <summary>The six styles (since sprint 182, D93, the pairs, the Hearts and the braid are gone to the Elements page).</summary>
    Styles,
}

/// <summary>
/// What the panel says of a node (sprint 149): its layer, the layer's banner, a line of lore (in italics), its name, what it
/// does, its counterpart, its state, and a line more for some (Helping Hand's figures, a group node).
/// </summary>
public sealed record TreeCard(string Layer, string Banner, string Lore, string Name, string Effect, string Counterpart, string State, string? Extra)
{
    /// <summary>Its lines in the order the panel shows them, the empty ones left out.</summary>
    public IEnumerable<string> Lines => new[] { Name, Effect, Counterpart, Extra, State }.Where(l => !string.IsNullOrEmpty(l))!;
}

/// <summary>
/// What the tree's window explains of a build, without any engine (sprint 149, D82): the layer of a node, the style a build
/// holds most of (N8 a), the best bonus a hit can reach against the ceiling of +35 % (N1 a), and the elements Helping Hand
/// works with (D9 a). Since sprint 182 (D93), the pairs and their advice are the Elements page's.
/// </summary>
public static class TreeGuide
{
    public static TreeLayer LayerOf(TreeNode node, GameData data)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(data);
        return node.IsTrunk ? TreeLayer.Foundations : TreeLayer.Styles;
    }

    /// <summary>The style the build has most nodes of (N8 a), a tie broken by <see cref="TreeLayout.Styles"/>; null without any.</summary>
    public static Branch? Title(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        return taken.Select(id => data.Talents[id]).Where(x => TreeLayout.Styles.Contains(x.Branch)).GroupBy(x => x.Branch)
            .OrderByDescending(g => g.Count()).ThenBy(g => TreeLayout.Styles.ToList().IndexOf(g.Key)).Select(g => (Branch?)g.Key).FirstOrDefault();
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
            + rules.OfType<BonusRule>().Sum(r => Up(r.Percent))
            + rules.OfType<StillRule>().Sum(r => Up(Math.Max(r.Still, r.Moved)))
            + rules.OfType<DanceRule>().Sum(r => Up(r.Max))
            + rules.OfType<ApCostRule>().Sum(r => Up(r.Percent))
            + rules.OfType<FirstCastRule>().Sum(r => Up(Math.Max(r.First, r.Others)))
            + rules.OfType<BackedRule>().Sum(r => Up(Math.Max(r.Damage, r.Otherwise)))
            + rules.OfType<GlyphTargetRule>().Sum(r => Up(Math.Max(r.Inside, r.Outside)))
            + rules.OfType<VirtuosoRule>().Sum(r => Up(r.Percent));
    }

    /// <summary>
    /// The bonus of the talents on a typical hit (sprint 150, M4 b): 2 cells away, without moving, the first damage spell of
    /// the turn, of 3 AP, in the hero's strongest base element; without <paramref name="talent"/>, then with it (what hangs
    /// on a talent given back gone with it).
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
        List<string> with = taken.Contains(talent) ? [.. taken] : [.. taken, talent];
        return (On(without), On(with));
    }

    /// <summary>The base elements Helping Hand helps with (D9 a): all four (since sprint 182, D93, every hero has them all).</summary>
    public static IReadOnlyList<Element> Helps => [Element.Earth, Element.Fire, Element.Water, Element.Air];
}
