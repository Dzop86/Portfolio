"""The move book read by the web page: the AI's answer for every position of every game.

`python -m morpion.book > data/book.json` writes it; CI rewrites it and fails if it changed.
Format: {"positions": {pos: entry}} where an entry is
  "X", "O" or "=" for a finished game (winner, or draw), or
  [score, best move, ...] otherwise, with the score of `ai.score` (seen from the player to move).
"""

import json
import sys

from .ai import best_moves, score
from .board import is_over, reachable_positions, winner

Entry = str | list[int]


def entry(pos: str) -> Entry:
    if is_over(pos):
        return winner(pos) or "="
    return [score(pos), *best_moves(pos)]


def build() -> dict[str, dict[str, Entry]]:
    return {"positions": {pos: entry(pos) for pos in sorted(reachable_positions())}}


def to_json() -> str:
    # One position per line: readable diffs, and small enough (about 100 kB).
    lines = [f"  {json.dumps(pos)}: {json.dumps(e, separators=(',', ':'))}" for pos, e in build()["positions"].items()]
    return '{"positions": {\n' + ",\n".join(lines) + "\n}}\n"


def main() -> None:
    sys.stdout.write(to_json())


if __name__ == "__main__":
    main()
