using Rogue.Client;
using Rogue.Core;

namespace Rogue.Cli;

internal interface IInput
{
    /// <summary>The next command, or null when the input is over.</summary>
    Command? Read();
}

internal static class KeyMap
{
    public static Command FromKey(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.UpArrow => new(GameAction.North),
        ConsoleKey.DownArrow => new(GameAction.South),
        ConsoleKey.RightArrow => new(GameAction.East),
        ConsoleKey.LeftArrow => new(GameAction.West),
        ConsoleKey.Escape => Command.Stop,
        _ => Keys.FromChar(key.KeyChar),
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
                return Keys.FromChar((char)c);
        }
    }
}

internal sealed class KeyboardInput : IInput
{
    public Command? Read() => KeyMap.FromKey(Console.ReadKey(intercept: true));
}
