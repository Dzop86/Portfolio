import pytest

from morpion.board import START, is_over, is_reachable, legal_moves, play, reachable_positions, render, to_move, winner


def test_start() -> None:
    assert START == "........."
    assert to_move(START) == "X"
    assert legal_moves(START) == list(range(9))
    assert not is_over(START)


def test_turns_alternate() -> None:
    pos = play(START, 4)
    assert pos == "....X...."
    assert to_move(pos) == "O"
    assert play(pos, 0) == "O...X...."


@pytest.mark.parametrize(
    ("pos", "expected"),
    [
        ("XXXOO....", "X"),  # row
        ("OXXOX.O..", "O"),  # column
        ("X.OOX...X", "X"),  # main diagonal
        ("OXX.O.X.O", "O"),  # main diagonal, for O
        ("..XOX.XO.", "X"),  # anti-diagonal
        ("XOXXOOOXX", None),  # full, no line: draw
        ("XO.......", None),
    ],
)
def test_winner(pos: str, expected: str | None) -> None:
    assert winner(pos) == expected


def test_no_move_after_the_end() -> None:
    assert legal_moves("XXXOO....") == []
    assert is_over("XOXXOOOXX")
    with pytest.raises(ValueError):
        play("XXXOO....", 5)


def test_illegal_moves_are_refused() -> None:
    with pytest.raises(ValueError):
        play("X........", 0)  # taken
    with pytest.raises(ValueError):
        play(START, 9)  # off the board


def test_reachable() -> None:
    assert is_reachable(START)
    assert is_reachable("XXXOO....")
    assert not is_reachable("OOO......")  # O never moves first
    assert not is_reachable("XXXOOO...")  # play went on after X won
    assert not is_reachable("XXOO....")  # wrong length
    assert not is_reachable("XXAOO....")


def test_known_counts() -> None:
    """Classic results: 5,478 positions, 958 of them final (626 won by X, 316 by O, 16 draws)."""
    positions = list(reachable_positions())
    assert len(positions) == len(set(positions)) == 5478
    final = [p for p in positions if is_over(p)]
    assert len(final) == 958
    assert sum(winner(p) == "X" for p in final) == 626
    assert sum(winner(p) == "O" for p in final) == 316
    assert all(is_reachable(p) for p in positions)


def test_reachable_matches_brute_force() -> None:
    """The rule-based check and the game tree agree on all 3^9 grids."""
    from itertools import product

    tree = set(reachable_positions())
    for cells in product("XO.", repeat=9):
        pos = "".join(cells)
        assert is_reachable(pos) == (pos in tree), pos


def test_render() -> None:
    assert render("X...O....") == " X | 2 | 3\n---+---+---\n 4 | O | 6\n---+---+---\n 7 | 8 | 9"
