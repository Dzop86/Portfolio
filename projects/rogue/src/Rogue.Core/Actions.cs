namespace Rogue.Core;

public enum GameAction
{
    North,
    South,
    East,
    West,
    Wait,
    Descend,
    Drink,
}

/// <summary>One character per action: a run's action list is a short string.</summary>
public static class ActionCodec
{
    private const string Codes = "nsew.>p";

    public static char ToChar(GameAction action) =>
        (int)action >= 0 && (int)action < Codes.Length ? Codes[(int)action] : throw new ArgumentOutOfRangeException(nameof(action));

    public static bool TryParse(char code, out GameAction action)
    {
        int index = Codes.IndexOf(code, StringComparison.Ordinal);
        action = (GameAction)Math.Max(index, 0);
        return index >= 0;
    }

    public static string Encode(IEnumerable<GameAction> actions) => string.Concat(actions.Select(ToChar));

    public static Direction? DirectionOf(GameAction action) => action switch
    {
        GameAction.North => Direction.North,
        GameAction.South => Direction.South,
        GameAction.East => Direction.East,
        GameAction.West => Direction.West,
        _ => null,
    };

    public static GameAction Move(Direction direction) => direction switch
    {
        Direction.North => GameAction.North,
        Direction.South => GameAction.South,
        Direction.East => GameAction.East,
        Direction.West => GameAction.West,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}
