using Rogue.Core;

namespace Rogue.Client;

/// <summary>A key pressed: a game action, quitting, or a key that means nothing here.</summary>
public readonly record struct Command(GameAction? Action, bool Quit = false)
{
    public static Command Ignored => new(null);

    public static Command Stop => new(null, Quit: true);
}

public static class Keys
{
    /// <summary>zqsd (AZERTY), wasd (QWERTY) and hjkl (vi) all move: none of their letters clash.</summary>
    public static Command FromChar(char c) => c switch
    {
        'z' or 'w' or 'k' or 'Z' or 'W' or 'K' => new(GameAction.North),
        's' or 'j' or 'S' or 'J' => new(GameAction.South),
        'd' or 'l' or 'D' or 'L' => new(GameAction.East),
        'q' or 'a' or 'h' or 'Q' or 'A' or 'H' => new(GameAction.West),
        '.' or ' ' => new(GameAction.Wait),
        '>' => new(GameAction.Descend),
        'p' or 'P' => new(GameAction.Drink),
        'x' or 'X' or '\u001b' => Command.Stop,
        _ => Command.Ignored,
    };
}
