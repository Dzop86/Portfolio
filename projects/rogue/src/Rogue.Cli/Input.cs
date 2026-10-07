using Rogue.Core;

namespace Rogue.Cli;

/// <summary>A key pressed: a game action, quitting, or a key that means nothing here.</summary>
internal readonly record struct Command(GameAction? Action, bool Quit = false)
{
    public static Command Ignored => new(null);

    public static Command Stop => new(null, Quit: true);
}

internal interface IInput
{
    /// <summary>The next command, or null when the input is over.</summary>
    Command? Read();
}

internal static class KeyMap
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

    public static Command FromKey(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.UpArrow => new(GameAction.North),
        ConsoleKey.DownArrow => new(GameAction.South),
        ConsoleKey.RightArrow => new(GameAction.East),
        ConsoleKey.LeftArrow => new(GameAction.West),
        ConsoleKey.Escape => Command.Stop,
        _ => FromChar(key.KeyChar),
    };
}

/// <summary>Keys from a text stream (piped input, tests): one character each, line breaks skipped.</summary>
internal sealed class TextInput(TextReader reader) : IInput
{
    public Command? Read()
    {
        while (true)
        {
            int c = reader.Read();
            if (c < 0)
                return null;
            if (c is not ('\n' or '\r'))
                return KeyMap.FromChar((char)c);
        }
    }
}

internal sealed class KeyboardInput : IInput
{
    public Command? Read() => KeyMap.FromKey(Console.ReadKey(intercept: true));
}
