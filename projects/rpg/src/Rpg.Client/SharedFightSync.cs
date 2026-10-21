using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A fight with the party on the game's side (sprint 121, D74): the same <see cref="Fight"/> as the server's, built from
/// its start; the player's own actions applied at once (and sent), the others' as the server tells them, in their
/// order, each once (one's own come back too, and are passed over). Out of step (an action missed, or refused here),
/// it asks to be built again from what the server says.
/// </summary>
public sealed class SharedFightSync
{
    public SharedFightSync(SharedFightStart start, Guid me, GameData data, IEnumerable<FightAction>? played = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(data);
        Start = start;
        Me = start.Players.ToList().IndexOf(me);
        if (Me < 0)
            throw new ArgumentException("Not a player of this fight.", nameof(me));
        Fight = start.Build(data);
        foreach (FightAction a in played ?? [])
            Fight.Apply(a);
    }

    public SharedFightStart Start { get; }
    public Fight Fight { get; }

    /// <summary>Which hero the player plays: its place in <see cref="Fight.HeroFighters"/>.</summary>
    public int Me { get; }

    /// <summary>The player's hero.</summary>
    public Fighter Hero => Fight.HeroFighters[Me];

    /// <summary>Whether it is the player's hero's turn.</summary>
    public bool MyTurn => !Fight.IsOver && Fight.Current == Hero;

    /// <summary>True when an action came that does not follow: the fight is to be built again from the server's.</summary>
    public bool OutOfStep { get; private set; }

    /// <summary>An action the server tells, numbered: applied if it is the next one, passed over if already applied.</summary>
    public void Receive(int number, FightAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (number < Fight.History.Count)
        {
            if (!Equals(Fight.History[number], action))
                OutOfStep = true;
            return;
        }
        if (number > Fight.History.Count || Fight.Apply(action) != ActionError.None)
            OutOfStep = true;
    }
}
