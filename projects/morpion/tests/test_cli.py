import random
from collections.abc import Callable
from typing import Any

import pytest

from morpion.ai import best_moves
from morpion.board import START, legal_moves, play
from morpion.cli import Outcome, main, play_game


def run(answers: list[str], **kwargs: Any) -> tuple[Outcome, list[str], str]:
    """A game where the person gives fixed answers."""
    it = iter(answers)
    prompts: list[str] = []
    out: list[str] = []

    def read(prompt: str) -> str:
        prompts.append(prompt)
        return next(it)

    outcome = play_game(read, out.append, rng=random.Random(0), **kwargs)
    return outcome, prompts, "\n".join(out)


def against(strategy: Callable[[str], int], seed: int, **kwargs: Any) -> Outcome:
    """A game where the person picks a square with `strategy`, following the game from what is printed."""
    pos = START

    def read(_prompt: str) -> str:
        nonlocal pos
        cell = strategy(pos)
        pos = play(pos, cell)
        return str(cell + 1)

    def write(line: str) -> None:
        nonlocal pos
        if line.startswith(("L'IA joue la case", "The AI plays square")):
            pos = play(pos, int(line.rstrip(".").rsplit(" ", 1)[1]) - 1)

    return play_game(read, write, rng=random.Random(seed), **kwargs)


def first_free(pos: str) -> int:
    return legal_moves(pos)[0]


def perfect(pos: str) -> int:
    return best_moves(pos)[0]


def test_unbeatable_ai_beats_a_careless_player() -> None:
    assert against(first_free, 0) == "loss"
    assert against(first_free, 0, human="O", lang="en") == "loss"


def test_perfect_players_draw() -> None:
    assert all(against(perfect, seed) == "draw" for seed in range(10))


def test_a_beginner_can_be_beaten() -> None:
    assert any(against(perfect, seed, level="beginner") == "win" for seed in range(50))


def test_invalid_answers_are_asked_again() -> None:
    outcome, prompts, text = run(["0", "abc", "5", "5", "q"])
    assert outcome == "quit"
    assert text.count("Case invalide") == 3  # 0, abc, then 5 already taken
    assert "Partie abandonnée." in text
    assert len(prompts) == 5


def test_english_and_playing_second() -> None:
    outcome, _, text = run(["q"], human="O", lang="en")
    assert outcome == "quit"
    assert text.startswith("Tic-tac-toe: you play O, the AI plays X (unbeatable level).")
    assert "The AI plays square" in text


def test_main_parses_options(monkeypatch: pytest.MonkeyPatch, capsys: pytest.CaptureFixture[str]) -> None:
    answers = iter(["q"])
    monkeypatch.setattr("builtins.input", lambda _prompt: next(answers))
    assert main(["--as", "O", "--level", "beginner", "--lang", "en", "--seed", "3"]) == 0
    assert "(beginner level)" in capsys.readouterr().out


def test_main_ends_quietly_at_end_of_input(monkeypatch: pytest.MonkeyPatch) -> None:
    def eof(_prompt: str) -> str:
        raise EOFError

    monkeypatch.setattr("builtins.input", eof)
    assert main([]) == 0


def test_main_rejects_unknown_options() -> None:
    with pytest.raises(SystemExit):
        main(["--level", "expert"])
