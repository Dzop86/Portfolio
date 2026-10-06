import random

import pytest

from morpion.ai import best_moves, choose, score
from morpion.board import START, Player, is_over, legal_moves, play, to_move, winner


def test_perfect_play_is_a_draw() -> None:
    assert score(START) == 0


def test_takes_a_win_at_once() -> None:
    # X to move, wins with cell 2 (top row); the AI must not settle for a slower win.
    pos = "XX.OO...."
    assert best_moves(pos) == [2]
    assert score(pos) == 1 + 4


def test_blocks_a_threat() -> None:
    # O to move, X threatens the top row: O must take cell 2.
    pos = "XX..O...."
    assert best_moves(pos) == [2]


def test_answers_a_corner_with_the_centre() -> None:
    # The only reply to an opening corner that does not lose is the centre.
    assert best_moves("X........") == [4]


def test_every_opening_move_draws() -> None:
    assert all(score(play(START, cell)) == 0 for cell in range(9))
    assert best_moves(START) == list(range(9))


def test_beginner_plays_any_legal_move() -> None:
    rng = random.Random(1)
    seen = {choose(START, "beginner", rng) for _ in range(200)}
    assert seen == set(range(9))


def test_no_move_when_over() -> None:
    assert best_moves("XXXOO....") == []
    with pytest.raises(ValueError):
        choose("XXXOO....", "unbeatable", random.Random(0))


def games_against_every_opponent(pos: str, ai: Player) -> tuple[int, int, int]:
    """Plays every game where the opponent tries every move and the AI tries each of its best moves.

    Returns (AI wins, draws, AI losses) over all those games.
    """
    if is_over(pos):
        w = winner(pos)
        return (0, 1, 0) if w is None else (1, 0, 0) if w == ai else (0, 0, 1)
    moves = best_moves(pos) if to_move(pos) == ai else legal_moves(pos)
    totals = [0, 0, 0]
    for cell in moves:
        for i, n in enumerate(games_against_every_opponent(play(pos, cell), ai)):
            totals[i] += n
    return totals[0], totals[1], totals[2]


@pytest.mark.parametrize("ai", ["X", "O"])
def test_unbeatable(ai: Player) -> None:
    """Whatever the opponent does, and whichever best move the AI picks, the AI never loses."""
    wins, draws, losses = games_against_every_opponent(START, ai)
    assert losses == 0
    assert wins > 0 and draws > 0


def count_games(pos: str) -> tuple[int, int, int]:
    if is_over(pos):
        w = winner(pos)
        return (1, 0, 0) if w == "X" else (0, 1, 0) if w == "O" else (0, 0, 1)
    totals = [0, 0, 0]
    for cell in legal_moves(pos):
        for i, n in enumerate(count_games(play(pos, cell))):
            totals[i] += n
    return totals[0], totals[1], totals[2]


def test_known_number_of_games() -> None:
    """Classic result: 255,168 games, 131,184 won by X, 77,904 by O, 46,080 drawn."""
    assert count_games(START) == (131184, 77904, 46080)
