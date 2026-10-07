using Rogue.Core;

namespace Rogue.Cli;

/// <summary>Plays one run: draws the screen, reads a key, applies it, until the run ends or the player quits.</summary>
internal sealed class Session(Game game, Texts texts, IInput input, TextWriter output, bool interactive)
{
    public Game Game => game;

    public void Play()
    {
        var messages = new List<string>();
        while (game.State == GameState.Playing)
        {
            Show(messages);
            messages.Clear();
            Command? command = input.Read();
            if (command is null || command.Value.Quit)
                break;
            if (command.Value.Action is not GameAction action)
                continue;
            if (!game.CanApply(action))
            {
                messages.Add(texts.Impossible(action));
                continue;
            }
            messages.AddRange(game.Apply(action).Select(texts.Describe));
        }
        Show(messages);
        output.WriteLine(texts.End(game));
    }

    /// <summary>Lets the autopilot play the whole run; only the last screen is shown.</summary>
    public void Watch()
    {
        IReadOnlyList<GameEvent> last = [];
        while (game.State == GameState.Playing)
            last = game.Apply(Autopilot.Choose(game));
        Show(last.Select(texts.Describe).ToList());
        output.WriteLine(texts.End(game));
    }

    private void Show(List<string> messages)
    {
        if (interactive)
            Console.Clear();
        output.WriteLine($"{texts.Status(game)}   {texts.Seed(game.Seed)}");
        (string[] rows, bool[,] remembered) = Screen.Draw(game);
        for (int y = 0; y < rows.Length; y++)
            WriteRow(rows[y], remembered, y);
        foreach (string message in messages)
            output.WriteLine(message);
        if (game.State == GameState.Playing)
        {
            output.WriteLine(texts.Legend);
            output.WriteLine(texts.Keys);
        }
    }

    /// <summary>In a terminal, the tiles out of sight are drawn in dark grey.</summary>
    private void WriteRow(string row, bool[,] remembered, int y)
    {
        if (!interactive)
        {
            output.WriteLine(row.TrimEnd());
            return;
        }
        for (int x = 0; x < row.Length; x++)
        {
            Console.ForegroundColor = remembered[y, x] ? ConsoleColor.DarkGray : ConsoleColor.Gray;
            output.Write(row[x]);
        }
        Console.ResetColor();
        output.WriteLine();
    }
}
