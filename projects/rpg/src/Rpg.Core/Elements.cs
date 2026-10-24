namespace Rpg.Core;

/// <summary>
/// The elements beyond the four (sprint 138, D80, <c>docs/elements.md</c>): a compound is made of its parents, strikes on
/// twice their characteristics' average, is resisted by the average of their resistances, and bears a signature.
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

    /// <summary>The compounds, in the order of the enumeration.</summary>
    public static IEnumerable<Element> Compounds => Enum.GetValues<Element>().Where(IsCompound);
}
