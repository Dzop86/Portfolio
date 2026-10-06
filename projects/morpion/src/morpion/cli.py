"""Play in the terminal: `morpion [--as X|O] [--level beginner|unbeatable] [--lang fr|en] [--seed N]`.

`play_game` takes its input and output as functions, so tests can script a whole game.
"""

import argparse
import random
from collections.abc import Callable
from typing import Literal

from .ai import Level, choose
from .board import START, Player, is_over, legal_moves, play, render, to_move, winner

Lang = Literal["fr", "en"]
Outcome = Literal["win", "loss", "draw", "quit"]

TEXT: dict[Lang, dict[str, str]] = {
    "fr": {
        "intro": "Morpion : vous jouez les {human}, l'IA joue les {ai} (niveau {level}). Les X commencent.",
        "beginner": "débutant",
        "unbeatable": "imbattable",
        "prompt": "Votre case (1 à 9, q pour quitter) : ",
        "bad": "Case invalide : choisissez une case libre entre 1 et 9.",
        "ai": "L'IA joue la case {cell}.",
        "win": "Vous gagnez !",
        "loss": "L'IA gagne.",
        "draw": "Partie nulle.",
        "quit": "Partie abandonnée.",
    },
    "en": {
        "intro": "Tic-tac-toe: you play {human}, the AI plays {ai} ({level} level). X starts.",
        "beginner": "beginner",
        "unbeatable": "unbeatable",
        "prompt": "Your square (1 to 9, q to quit): ",
        "bad": "Invalid square: pick a free square from 1 to 9.",
        "ai": "The AI plays square {cell}.",
        "win": "You win!",
        "loss": "The AI wins.",
        "draw": "Draw.",
        "quit": "Game abandoned.",
    },
}


def play_game(
    read: Callable[[str], str],
    write: Callable[[str], None],
    *,
    human: Player = "X",
    level: Level = "unbeatable",
    lang: Lang = "fr",
    rng: random.Random | None = None,
) -> Outcome:
    """One game between a person (through `read`/`write`) and the AI; returns how it ended for the person."""
    t = TEXT[lang]
    rng = rng or random.Random()
    ai: Player = "O" if human == "X" else "X"
    write(t["intro"].format(human=human, ai=ai, level=t[level]))
    pos = START
    while not is_over(pos):
        if to_move(pos) == human:
            write(render(pos))
            answer = read(t["prompt"]).strip().lower()
            if answer in ("q", "quit", "quitter"):
                write(t["quit"])
                return "quit"
            if not (answer.isdigit() and int(answer) - 1 in legal_moves(pos)):
                write(t["bad"])
                continue
            pos = play(pos, int(answer) - 1)
        else:
            cell = choose(pos, level, rng)
            write(t["ai"].format(cell=cell + 1))
            pos = play(pos, cell)
    write(render(pos))
    w = winner(pos)
    outcome: Outcome = "draw" if w is None else "win" if w == human else "loss"
    write(t[outcome])
    return outcome


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="morpion", description="Tic-tac-toe against a minimax AI.")
    parser.add_argument("--as", dest="human", choices=["X", "O"], default="X")
    parser.add_argument("--level", choices=["beginner", "unbeatable"], default="unbeatable")
    parser.add_argument("--lang", choices=["fr", "en"], default="fr")
    parser.add_argument("--seed", type=int, default=None)
    args = parser.parse_args(argv)
    try:
        play_game(input, print, human=args.human, level=args.level, lang=args.lang, rng=random.Random(args.seed))
    except (EOFError, KeyboardInterrupt):
        print()
    return 0
