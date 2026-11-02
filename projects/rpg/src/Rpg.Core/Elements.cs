namespace Rpg.Core;

/// <summary>
/// The elements beyond the four (sprint 138, D80, <c>docs/elements.md</c>): a compound is made of its parents, strikes on
/// the highest of their characteristics, is resisted by the average of their resistances, and bears a signature. Since
/// sprint 182 (D93, <c>docs/elements-specialisations.md</c> §v3), a hero creates it on the Elements page: a pair from
/// level <see cref="Pairs"/>, a trio from level <see cref="Trios"/>, for free and undone as freely.
/// </summary>
public static class Elements
{
    private static readonly Dictionary<Element, Element[]> ParentsOf = new()
    {
        [Element.Lightning] = [Element.Fire, Element.Air],
    };

    /// <summary>Arc (Lightning's signature): this percent of a hit jumps to the nearest enemy within <see cref="ArcCells"/> cells of the target.</summary>
    public const int Arc = 35;

    public const int ArcCells = 2;

    /// <summary>A compound's parents, in the order of its name ("Fire and Air"); none for a base element or the neutral.</summary>
    public static IReadOnlyList<Element> Parents(Element element) => ParentsOf.TryGetValue(element, out Element[]? p) ? p : [];

    public static bool IsCompound(Element element) => ParentsOf.ContainsKey(element);

    /// <summary>The level the pairs open at, and with them the six reactions (the Awakening).</summary>
    public const int Pairs = 30;

    /// <summary>The level the trios open at (the Affirmation).</summary>
    public const int Trios = 50;

    /// <summary>The level a compound may be created from: a pair's, a trio's; 0 for an element that is no compound.</summary>
    public static int CreatedFrom(Element element) => Parents(element).Count switch
    {
        2 => Pairs,
        3 => Trios,
        _ => 0,
    };

    /// <summary>Why these compounds cannot be created at this level, or null: compounds, each once, each open at the level.</summary>
    public static string? Problem(IReadOnlyList<Element> compounds, int level)
    {
        ArgumentNullException.ThrowIfNull(compounds);
        foreach (Element e in compounds)
        {
            if (!IsCompound(e))
                return $"'{e}' is no compound.";
            if (level < CreatedFrom(e))
                return $"'{e}' is created from level {CreatedFrom(e)}.";
        }
        return compounds.Distinct().Count() != compounds.Count ? "A compound is created once." : null;
    }

    /// <summary>The compounds, in the order of the enumeration.</summary>
    public static IEnumerable<Element> Compounds => Enum.GetValues<Element>().Where(IsCompound);
}
