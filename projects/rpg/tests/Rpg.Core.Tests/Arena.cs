namespace Rpg.Core.Tests;

/// <summary>Small boards and fighters written in the tests themselves, and the game's real data.</summary>
internal static class Arena
{
    public static readonly LocalizedText Text = new("essai", "test");

    // Fixed damage where a test counts hit points; a range where it checks the roll.
    public static readonly Spell Strike = new("strike", Text, 3, 1, 1, true, false, 10, 10, 2);
    public static readonly Spell Bow = new("bow", Text, 2, 2, 5, true, false, 4, 8, 3);
    public static readonly Spell Spear = new("spear", Text, 2, 1, 3, true, true, 5, 5, 2);
    public static readonly Spell Lob = new("lob", Text, 2, 1, 4, false, false, 1, 1, 9);
    public static readonly Spell Sacrifice = new("sacrifice", Text, 1, 0, 0, false, false, 100, 100, 1);
    public static readonly Spell[] Spells = [Strike, Bow, Spear, Lob, Sacrifice];

    public static FighterSpec Spec(int team, int start = 0, int hp = 50, int ap = 6, int mp = 3, int initiative = 100, params string[] spells) =>
        new(new LocalizedText($"f{team}{start}", $"f{team}{start}"), "male-a", team, hp, ap, mp, initiative, start, spells);

    public static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new(Spells, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]);

    public static Fight Fight(string[] rows, params FighterSpec[] fighters) => new(Data(rows, fighters), "s", 1);

    public static GameData Real => GameData.Embedded;

    /// <summary>Everything observable about a fight, to compare two fights.</summary>
    public static string Fingerprint(Fight f) =>
        string.Join('|', f.Events.Select(e => e is Moved m ? $"Moved {m.Fighter} {string.Join(' ', m.Path)}" : e.ToString()))
        + "|" + string.Join(';', f.Fighters.Select(x => $"{x.Id}:{x.Hp}:{x.Cell}"));
}
