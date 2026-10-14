using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A deck as a short code to copy and paste (sprint 82, D66): the class, a hyphen, then one letter per
/// spell in the order of the bar, the letter of its place in the class's list (A for the first): a
/// fire and earth mage might give "MAGE-CDKLQ". The same code gives the same deck on any computer, as
/// long as the class keeps its spells in the same order.
/// </summary>
public static class DeckCode
{
    public static string Encode(string heroClass, IReadOnlyList<string> deck, GameData data)
    {
        ArgumentNullException.ThrowIfNull(heroClass);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(data);
        HeroClass c = data.Class(heroClass) ?? throw new ArgumentException($"Unknown class '{heroClass}'.", nameof(heroClass));
        return c.Id.ToUpperInvariant() + "-" + string.Concat(deck.Select(id => c.Spells.ToList().IndexOf(id)).Where(i => i >= 0 && i < 26).Select(i => (char)('A' + i)));
    }

    /// <summary>The class and the spells of a code (spaces and case aside), or null if it is not one.</summary>
    public static (string Class, IReadOnlyList<string> Spells)? Decode(string? code, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string[] parts = (code ?? "").Trim().Split('-');
        if (parts.Length != 2 || data.Classes.FirstOrDefault(c => c.Id.Equals(parts[0].Trim(), StringComparison.OrdinalIgnoreCase)) is not HeroClass c)
            return null;
        string letters = parts[1].Trim().ToUpperInvariant();
        if (letters.Length > Hero.DeckSize || letters.Any(l => l < 'A' || l - 'A' >= c.Spells.Count) || letters.Distinct().Count() != letters.Length)
            return null;
        return (c.Id, [.. letters.Select(l => c.Spells[l - 'A'])]);
    }
}
