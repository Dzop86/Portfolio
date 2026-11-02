using Rpg.Core;

namespace Rpg.Client;

/// <summary>What the game says when a character passes a tier of its path (sprint 186, D93, §v3.5): no quest at 50 and 100, a word and the evolution that lights up.</summary>
public static class PathNews
{
    /// <summary>The announcement of a gain of levels, the hero at its new level, or null when it passes no tier.</summary>
    public static string? Of(Hero hero, int before, GameData data, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(texts);
        if (Specs.TierPassed(before, hero.Level) is not int tier)
            return null;
        string said = texts[$"path.tier.{tier.ToString(System.Globalization.CultureInfo.InvariantCulture)}"];
        return Specs.TitleOf(hero, data) is LocalizedText title ? texts["path.evolved", said, title.In(texts.Lang)] : said;
    }
}
