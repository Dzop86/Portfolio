"""The AI: minimax (in its negamax form) over the whole game, cached by position.

A score is seen from the player to move: a win is worth 1 plus the number of empty cells left when
it happens, so the AI wins as fast as it can and, when lost, holds out as long as it can; a draw is 0.
"""

import random
from functools import cache
from typing import Literal

from .board import EMPTY_CELL, is_over, legal_moves, play, winner

Level = Literal["beginner", "unbeatable"]


@cache
def score(pos: str) -> int:
    """The game's value for the player to move, if both sides play perfectly from here."""
    if winner(pos) is not None:
        # The previous player has just won.
        return -(1 + pos.count(EMPTY_CELL))
    if is_over(pos):
        return 0
    return max(-score(play(pos, cell)) for cell in legal_moves(pos))


def best_moves(pos: str) -> list[int]:
    """Every move that keeps the best score, in cell order."""
    moves = legal_moves(pos)
    if not moves:
        return []
    best = score(pos)
    return [cell for cell in moves if -score(play(pos, cell)) == best]


def choose(pos: str, level: Level, rng: random.Random) -> int:
    """The AI's move: any legal move for a beginner, one of the best moves otherwise."""
    moves = legal_moves(pos) if level == "beginner" else best_moves(pos)
    if not moves:
        raise ValueError(f"no move in {pos}")
    return rng.choice(moves)
