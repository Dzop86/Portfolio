"""The rules. A position is a string of nine cells, read row by row: "X", "O" or "." for empty.

Strings are immutable and hashable, so positions can be cached by the AI and used as keys of the
move book read by the web page.
"""

from collections.abc import Iterator
from typing import Literal

Player = Literal["X", "O"]
EMPTY_CELL = "."
START = EMPTY_CELL * 9
LINES: tuple[tuple[int, int, int], ...] = (
    (0, 1, 2), (3, 4, 5), (6, 7, 8),  # rows
    (0, 3, 6), (1, 4, 7), (2, 5, 8),  # columns
    (0, 4, 8), (2, 4, 6),  # diagonals
)


def winner(pos: str) -> Player | None:
    """The player with three in a row, if any."""
    for a, b, c in LINES:
        if pos[a] != EMPTY_CELL and pos[a] == pos[b] == pos[c]:
            return "X" if pos[a] == "X" else "O"
    return None


def is_over(pos: str) -> bool:
    return winner(pos) is not None or EMPTY_CELL not in pos


def to_move(pos: str) -> Player:
    """X always starts, so X moves whenever both have played as often."""
    return "X" if pos.count("X") == pos.count("O") else "O"


def legal_moves(pos: str) -> list[int]:
    """Empty cells, in order; none once the game is over."""
    if is_over(pos):
        return []
    return [i for i, c in enumerate(pos) if c == EMPTY_CELL]


def play(pos: str, cell: int) -> str:
    """The position after the player to move takes `cell`."""
    if cell not in legal_moves(pos):
        raise ValueError(f"illegal move {cell} in {pos}")
    return pos[:cell] + to_move(pos) + pos[cell + 1:]


def is_reachable(pos: str) -> bool:
    """Whether `pos` can arise in a game from the empty board."""
    if len(pos) != 9 or set(pos) - {"X", "O", EMPTY_CELL}:
        return False
    x, o = pos.count("X"), pos.count("O")
    if x - o not in (0, 1):
        return False
    w = winner(pos)
    # The winner made the last move: X wins on its move (x = o + 1), O wins on its move (x = o).
    if w == "X" and x != o + 1:
        return False
    if w == "O" and x != o:
        return False
    if w is not None:
        # The game stopped at the winning move: taking it back must leave no winner.
        last = "X" if w == "X" else "O"
        return any(winner(pos[:i] + EMPTY_CELL + pos[i + 1:]) is None for i, c in enumerate(pos) if c == last)
    return True


def reachable_positions() -> Iterator[str]:
    """Every position of every game, each once, from the empty board (breadth first)."""
    seen = {START}
    frontier = [START]
    while frontier:
        yield from frontier
        nxt: list[str] = []
        for pos in frontier:
            for cell in legal_moves(pos):
                child = play(pos, cell)
                if child not in seen:
                    seen.add(child)
                    nxt.append(child)
        frontier = nxt


def render(pos: str) -> str:
    """Three rows; empty cells show their number (1 to 9) so a person can pick one."""
    cells = [c if c != EMPTY_CELL else str(i + 1) for i, c in enumerate(pos)]
    rows = [" " + " | ".join(cells[r * 3:r * 3 + 3]) for r in range(3)]
    return "\n---+---+---\n".join(rows)
