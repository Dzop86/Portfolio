import json
from pathlib import Path

from morpion.ai import best_moves, score
from morpion.board import is_over, reachable_positions, winner
from morpion.book import build, to_json

BOOK = Path(__file__).resolve().parent.parent / "data" / "book.json"


def test_book_covers_every_position() -> None:
    positions = build()["positions"]
    assert set(positions) == set(reachable_positions())
    for pos, e in positions.items():
        if is_over(pos):
            assert e == (winner(pos) or "=")
        else:
            assert isinstance(e, list)
            assert e == [score(pos), *best_moves(pos)]


def test_json_is_valid_and_round_trips() -> None:
    assert json.loads(to_json()) == build()


def test_committed_book_is_up_to_date() -> None:
    """data/book.json is what the page reads: rewrite it with `python -m morpion.book > data/book.json`."""
    assert BOOK.read_text(encoding="utf-8") == to_json()
