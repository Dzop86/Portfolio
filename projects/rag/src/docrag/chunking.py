"""Markdown documents split into passages that keep their file, their headings and their lines.

A passage (``Chunk``) is a run of blocks under the same headings, at most ``max_chars`` long. A block is
the smallest piece a citation can point to: a paragraph, a list item, a table or a fenced code block,
with its first and last line in the file.
"""
from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

# The documentation of the portfolio: its plan, its decisions and each project's README and decisions.
DEFAULT_PATTERNS = ("README.md", "PLAN.md", "DECISIONS.md", "projects/*/README.md", "projects/*/DECISIONS.md")
# Except this project's own: it talks about the reference questions and would answer them about itself.
DEFAULT_EXCLUDE = ("projects/rag/",)

_HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
_ITEM = re.compile(r"^\s{0,3}(?:[-*+]|\d+[.)])\s+")
_FENCE = re.compile(r"^\s{0,3}(```|~~~)")


@dataclass(frozen=True)
class Block:
    start: int  # first line, from 1
    end: int  # last line, included
    text: str


@dataclass(frozen=True)
class Chunk:
    source: str  # path from the corpus root, with forward slashes
    title: str  # the headings above it, "README > Tests"
    blocks: tuple[Block, ...]

    @property
    def start(self) -> int:
        return self.blocks[0].start

    @property
    def end(self) -> int:
        return self.blocks[-1].end

    @property
    def id(self) -> str:
        return f"{self.source}:{self.start}-{self.end}"

    @property
    def body(self) -> str:
        return "\n\n".join(b.text for b in self.blocks)

    @property
    def text(self) -> str:
        """What is searched: the headings, which say what the passage is about, then the passage."""
        return f"{self.title}\n\n{self.body}" if self.title else self.body


def split_blocks(lines: list[str]) -> list[tuple[list[str], Block]]:
    """The blocks of a Markdown file, each with the headings above it. Headings are not blocks."""
    out: list[tuple[list[str], Block]] = []
    headings: list[str] = []
    current: list[str] = []
    first = 0
    kind = ""  # "para", "item", "table" or "code"

    def flush(last: int) -> None:
        nonlocal current, kind
        if current and any(line.strip() for line in current):
            out.append((list(headings), Block(first, last, "\n".join(current).strip())))
        current, kind = [], ""

    for n, line in enumerate(lines, start=1):
        if kind == "code":
            current.append(line)
            if _FENCE.match(line):
                flush(n)
            continue
        if _FENCE.match(line):
            flush(n - 1)
            current, first, kind = [line], n, "code"
            continue
        heading = _HEADING.match(line)
        if heading:
            flush(n - 1)
            level = len(heading.group(1))
            del headings[level - 1:]
            headings.extend([""] * (level - 1 - len(headings)))
            headings.append(heading.group(2))
            continue
        if not line.strip():
            flush(n - 1)
            continue
        starts = "item" if _ITEM.match(line) else "table" if line.lstrip().startswith("|") else "para"
        # A new list item starts a new block; a table or a paragraph goes on until a blank line.
        if current and (starts == "item" or (starts == "table") != (kind == "table")):
            flush(n - 1)
        if not current:
            first, kind = n, starts
        current.append(line)
    flush(len(lines))
    return out


def chunk_document(source: str, text: str, max_chars: int = 1200) -> list[Chunk]:
    """The passages of one file: consecutive blocks under the same headings, up to ``max_chars``.

    A block longer than ``max_chars`` makes a passage on its own: it is never cut, so a citation always
    points to whole lines.
    """
    chunks: list[Chunk] = []
    run: list[Block] = []
    run_headings: list[str] | None = None

    def close() -> None:
        if run:
            title = " > ".join(h for h in (run_headings or []) if h)
            chunks.append(Chunk(source, title, tuple(run)))

    for headings, block in split_blocks(text.splitlines()):
        size = sum(len(b.text) for b in run) + len(block.text)
        if headings != run_headings or (run and size > max_chars):
            close()
            run = []
            run_headings = headings
        run.append(block)
    close()
    return chunks


def load_corpus(
    root: Path, patterns: tuple[str, ...] = DEFAULT_PATTERNS, max_chars: int = 1200, exclude: tuple[str, ...] = DEFAULT_EXCLUDE
) -> list[Chunk]:
    """Every passage of the files under ``root`` that match ``patterns``, except those under ``exclude``, in a stable order."""
    files = sorted({p for pattern in patterns for p in root.glob(pattern) if p.is_file()})
    chunks: list[Chunk] = []
    for path in files:
        source = path.relative_to(root).as_posix()
        if source.startswith(exclude):
            continue
        chunks.extend(chunk_document(source, path.read_text(encoding="utf-8"), max_chars))
    return chunks
