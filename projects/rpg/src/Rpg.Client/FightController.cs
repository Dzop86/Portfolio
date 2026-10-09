using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// What the board shows for the hovered cell. Without a spell: the cells the player can walk to and
/// the path to the hovered one. With a spell: the cells within its range, those it can really hit
/// (line of sight, free of obstacles), and the hovered cell if it is one of them.
/// </summary>
public sealed record Preview(
    IReadOnlySet<Cell> Reachable,
    IReadOnlyList<Cell> Path,
    IReadOnlySet<Cell> InRange,
    IReadOnlySet<Cell> Targetable,
    Cell? Target)
{
    public static readonly Preview None = new(new HashSet<Cell>(), [], new HashSet<Cell>(), new HashSet<Cell>(), null);
}

/// <summary>
/// The player's side of a fight, without any engine: choosing a spell, hovering and clicking cells,
/// ending the turn, letting the AI play the other team. The Godot client only draws what this says,
/// so that everything a click can do is tested here.
/// </summary>
public sealed class FightController(Fight fight, int playerTeam = 0)
{
    public Fight Fight { get; } = fight ?? throw new ArgumentNullException(nameof(fight));
    public int PlayerTeam { get; } = playerTeam;

    /// <summary>The spell chosen for the next click, or null to walk.</summary>
    public Spell? SelectedSpell { get; private set; }

    /// <summary>Why the last click or spell choice did nothing, for the message line; null when it worked.</summary>
    public ActionError? LastError { get; private set; }

    /// <summary>The player plays their team's fighters; summons are left to the AI.</summary>
    public bool IsPlayerTurn => !Fight.IsOver && Fight.Current.Team == PlayerTeam && !Fight.Current.IsSummon;

    /// <summary>Whether the current fighter could cast the spell somewhere this turn (enough AP, casts left).</summary>
    public bool CanUse(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        return Fight.Current.Ap >= spell.ApCost && Fight.Current.CastsLeft(spell) > 0 && Fight.Current.CooldownLeft(spell) == 0;
    }

    /// <summary>
    /// Chooses the spell at <paramref name="index"/> in the current fighter's list; choosing it again
    /// goes back to walking. A spell that cannot be cast this turn is not chosen and says why.
    /// </summary>
    public void SelectSpell(int index)
    {
        if (!IsPlayerTurn || index < 0 || index >= Fight.Current.Spells.Count)
            return;
        Spell spell = Fight.Current.Spells[index];
        if (SelectedSpell == spell)
        {
            SelectedSpell = null;
            LastError = null;
            return;
        }
        LastError = Fight.Current.Ap < spell.ApCost ? ActionError.NotEnoughAp
            : Fight.Current.CastsLeft(spell) <= 0 ? ActionError.CastLimit
            : Fight.Current.CooldownLeft(spell) > 0 ? ActionError.Cooldown
            : null;
        SelectedSpell = LastError is null ? spell : null;
    }

    public void CancelSpell()
    {
        SelectedSpell = null;
        LastError = null;
    }

    /// <summary>What to highlight with the pointer on <paramref name="hovered"/> (null: off the board).</summary>
    public Preview Hover(Cell? hovered)
    {
        if (!IsPlayerTurn)
            return Preview.None;
        Fighter me = Fight.Current;
        if (SelectedSpell is Spell spell)
        {
            var inRange = new HashSet<Cell>();
            var targetable = new HashSet<Cell>();
            foreach (Cell c in Fight.Board.Cells())
            {
                int d = me.Cell.DistanceTo(c);
                if (!Fight.Board.IsFloor(c) || d < spell.MinRange || d > spell.MaxRange || (spell.InLine && !me.Cell.IsInLineWith(c)))
                    continue;
                inRange.Add(c);
                if (Fight.CheckCast(me, spell, me.Cell, c) == ActionError.None)
                    targetable.Add(c);
            }
            Cell? target = hovered is Cell h && targetable.Contains(h) ? h : null;
            return new Preview(new HashSet<Cell>(), [], inRange, targetable, target);
        }
        var reachable = Fight.ReachableCells().Keys.Where(c => c != me.Cell).ToHashSet();
        IReadOnlyList<Cell> path = hovered is Cell to && reachable.Contains(to)
            ? Pathfinding.FindPath(Fight.Board, me.Cell, to, Fight.IsFree) ?? []
            : [];
        return new Preview(reachable, path, new HashSet<Cell>(), new HashSet<Cell>(), null);
    }

    /// <summary>
    /// A click on a cell: casts the chosen spell there, or walks there. Returns the action played,
    /// or null (and <see cref="LastError"/>) when the click does nothing.
    /// </summary>
    public FightAction? Click(Cell cell)
    {
        if (!IsPlayerTurn)
            return null;
        FightAction action = SelectedSpell is Spell spell ? new CastAction(spell.Id, cell) : new MoveAction(cell);
        ActionError error = Fight.Apply(action);
        if (error != ActionError.None)
        {
            LastError = error;
            return null;
        }
        // As in the games of the genre, a spell is chosen for one cast.
        SelectedSpell = null;
        LastError = null;
        return action;
    }

    public FightAction? EndTurn()
    {
        if (!IsPlayerTurn)
            return null;
        SelectedSpell = null;
        LastError = null;
        var action = new EndTurnAction();
        Fight.Apply(action);
        return action;
    }

    /// <summary>One action of the AI, when it is not the player's turn; null otherwise.</summary>
    public FightAction? PlayAiStep()
    {
        if (Fight.IsOver || IsPlayerTurn)
            return null;
        FightAction action = Ai.Decide(Fight);
        ActionError error = Fight.Apply(action);
        return error == ActionError.None ? action : throw new InvalidOperationException($"The AI proposed {action}, refused: {error}.");
    }
}
