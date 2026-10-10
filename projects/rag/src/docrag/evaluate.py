"""Measures on the reference questions (eval/questions.jsonl).

Each question says where its answer lives (``expected``: a file and, optionally, a few words of the
headings above the answer), or that the documentation does not answer it (``answerable: false``).

Retrieval: recall@k (the share of questions with a right passage in the first k) and MRR (the mean of
1 / rank of the first right passage, 0 past the tenth), for each search mode.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .chunking import Chunk
from .retrieve import MODES, Retriever

ROOT = Path(__file__).resolve().parents[4]  # the repository: projects/rag/src/docrag -> ../../../..
QUESTIONS = Path(__file__).resolve().parents[2] / "eval" / "questions.jsonl"
KS = (1, 3, 5)


def load_questions(path: Path = QUESTIONS) -> list[dict]:
    questions = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    ids = [q["id"] for q in questions]
    if len(set(ids)) != len(ids):
        raise ValueError("two reference questions share an id")
    for q in questions:
        if q["answerable"] == (not q.get("expected")):
            raise ValueError(f"{q['id']}: an answerable question needs where its answer is, and only it")
    return questions


def relevant(chunk: Chunk, expected: list[dict]) -> bool:
    return any(chunk.source == e["source"] and e.get("section", "").lower() in chunk.title.lower() for e in expected)


def check_expected(chunks: list[Chunk], questions: list[dict]) -> list[str]:
    """The expected places that match no passage at all (a renamed file or heading)."""
    return [f"{q['id']}: {e}" for q in questions for e in q.get("expected", []) if not any(relevant(c, [e]) for c in chunks)]


def retrieval_metrics(retriever: Retriever, questions: list[dict], depth: int = 10) -> dict[str, dict[str, float]]:
    answerable = [q for q in questions if q["answerable"]]
    out: dict[str, dict[str, float]] = {}
    for mode in MODES:
        ranks = []
        for q in answerable:
            hits = retriever.search(q["question"], depth, mode)
            ranks.append(next((r for r, h in enumerate(hits, start=1) if relevant(h.chunk, q["expected"])), None))
        n = len(ranks) or 1
        scores = {f"recall@{k}": sum(1 for r in ranks if r is not None and r <= k) / n for k in KS}
        scores["mrr"] = sum(1 / r for r in ranks if r is not None) / n
        out[mode] = {name: round(v, 3) for name, v in scores.items()}
    return out


def build_retriever(embedder_name: str, model: str | None = None, root: Path = ROOT) -> Retriever:
    from qdrant_client import QdrantClient

    from .chunking import load_corpus
    from .embed import DEFAULT_MODEL, FastEmbedder, HashEmbedder
    from .retrieve import VectorStore

    embedder = HashEmbedder() if embedder_name == "hash" else FastEmbedder(model or DEFAULT_MODEL)
    return Retriever(load_corpus(root), embedder, VectorStore(QdrantClient(":memory:")))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="python -m docrag.evaluate", description=__doc__.splitlines()[0])
    parser.add_argument("--embedder", choices=["fastembed", "hash"], default="fastembed")
    parser.add_argument("--model", help="the FastEmbed model (default: docrag.embed.DEFAULT_MODEL)")
    parser.add_argument("--out", type=Path, help="write the results as JSON there")
    args = parser.parse_args(argv)
    questions = load_questions()
    retriever = build_retriever(args.embedder, args.model)
    missing = check_expected(retriever.chunks, questions)
    if missing:
        print("expected places found nowhere:\n  " + "\n  ".join(missing), file=sys.stderr)
        return 1
    results = {
        "embedder": retriever.embedder.name,
        "passages": len(retriever.chunks),
        "questions": len(questions),
        "answerable": sum(q["answerable"] for q in questions),
        "retrieval": retrieval_metrics(retriever, questions),
    }
    text = json.dumps(results, ensure_ascii=False, indent=2)
    print(text)
    if args.out:
        args.out.write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
