using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// Plays a whole fight through the player's controls: the AI decides the player's moves too, but
/// each one goes through <see cref="FightController.Hover"/>, a spell choice and a click, as a mouse
/// would. The Godot client runs it with its views (option --selftest), the tests without.
/// </summary>
public static class SelfPlay
{
    /// <summary>
    /// Plays until the end; <paramref name="afterAction"/> runs after every action (the client
    /// updates its views there). Throws if a click does not give the action the AI chose.
    /// </summary>
    public static void Run(FightController controller, Action<FightAction>? afterAction = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        Fight fight = controller.Fight;
        while (!fight.IsOver)
        {
            FightAction? played;
            if (!controller.IsPlayerTurn)
            {
                played = controller.PlayAiStep();
            }
            else
            {
                FightAction wanted = Ai.Decide(fight);
                played = wanted switch
                {
                    MoveAction m => Walk(controller, m.To),
                    CastAction c => Cast(controller, c),
                    _ => controller.EndTurn(),
                };
                if (played != wanted)
                    throw new InvalidOperationException($"The controls played {played?.ToString() ?? "nothing"} instead of {wanted} ({controller.LastError}).");
            }
            afterAction?.Invoke(played!);
        }
    }

    /// <summary>One line for the CI: the outcome, after checking that the record replays to the same fight.</summary>
    public static string Report(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        Fight replayed = FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(GameData.Embedded);
        bool same = replayed.WinningTeam == fight.WinningTeam && replayed.Round == fight.Round
            && replayed.Fighters.Select(f => (f.Hp, f.Cell)).SequenceEqual(fight.Fighters.Select(f => (f.Hp, f.Cell)));
        string outcome = fight.WinningTeam is int t ? $"team {(char)('A' + t)} wins" : "draw";
        return $"SELFTEST {(same ? "OK" : "FAILED")} seed {fight.Seed}: {outcome} in round {fight.Round}, after {fight.History.Count} actions.";
    }

    private static FightAction? Walk(FightController controller, Cell to)
    {
        Preview preview = controller.Hover(to);
        if (preview.Path.Count == 0 || preview.Path[^1] != to)
            throw new InvalidOperationException($"Hovering {to} shows no path to it.");
        return controller.Click(to);
    }

    private static FightAction? Cast(FightController controller, CastAction cast)
    {
        int index = controller.Fight.Current.Spells.ToList().FindIndex(s => s.Id == cast.Spell);
        controller.SelectSpell(index);
        if (controller.Hover(cast.Target).Target != cast.Target)
            throw new InvalidOperationException($"Hovering {cast.Target} with {cast.Spell} does not show it as a target.");
        return controller.Click(cast.Target);
    }
}
