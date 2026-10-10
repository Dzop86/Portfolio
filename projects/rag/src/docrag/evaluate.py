"""Measures on the reference questions (eval/questions.jsonl).

Each question says where its answer lives (``expected``: a file and, optionally, a few words of the
headings above the answer), or that the documentation does not answer it (``answerable: false``).

Retrieval: recall@k (the share of questions with a right passage in the first k) and MRR (the mean of
1 / rank of the first right passage, 0 past the tenth), for each search mode.

Answers (``--answers``, needs a Claude API key): how often an answerable question gets a cited answer,
how often that answer cites a right place, the share of citations that point to a right place, and how
often a question the documentation does not answer gets "no answer".
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .chunking import Chunk, find_root
from .retrieve import MODES, Retriever

KS = (1, 3, 5)


def questions_file() -> Path:
    return find_root() / "projects" / "rag" / "eval" / "questions.jsonl"


def load_questions(path: Path | None = None) -> list[dict]:
    path = path or questions_file()
    questions = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
    ids = [q["id"] for q in questions]
    if len(set(ids)) != len(ids):
        raise ValueError("two reference questions share an id")
    for q in questions:
        if q["answerable"] == (not q.get("expected")):
            raise ValueError(f"{q['id']}: an answerable question needs where its answer is, and only it")
    return questions


def relevant(chunk: Chunk, expected: list[dict]) -> bool:
    return relevant_place(chunk.source, chunk.title, expected)


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


def answer_metrics(answerer, questions: list[dict]) -> tuple[dict[str, float], list[dict]]:
    """The four answer measures, and what was answered to each question (for the project page)."""
    records = []
    for q in questions:
        a = answerer.ask(q["question"])
        right = [relevant_place(c.source, c.title, q.get("expected", [])) for c in a.citations]
        records.append({
            "id": q["id"], "lang": q["lang"], "question": q["question"], "answerable": q["answerable"],
            "answer": a.text, "found": a.found, "cites_right_place": any(right),
            "citations": [{"source": c.source, "start": c.start, "end": c.end, "title": c.title, "right": r} for c, r in zip(a.citations, right)],
        })
    answerable = [r for r in records if r["answerable"]]
    unanswerable = [r for r in records if not r["answerable"]]
    cited = [c for r in answerable for c in r["citations"]]

    def share(part: int, whole: int) -> float:
        return round(part / whole, 3) if whole else 0.0

    scores = {
        "answered": share(sum(r["found"] for r in answerable), len(answerable)),
        "answered_with_right_source": share(sum(r["cites_right_place"] for r in answerable), len(answerable)),
        "citation_precision": share(sum(c["right"] for c in cited), len(cited)),
        "abstained_when_unanswerable": share(sum(not r["found"] for r in unanswerable), len(unanswerable)),
    }
    return scores, records


def relevant_place(source: str, title: str, expected: list[dict]) -> bool:
    return any(source == e["source"] and e.get("section", "").lower() in title.lower() for e in expected)


def build_retriever(embedder_name: str, model: str | None = None, root: Path | None = None) -> Retriever:
    from qdrant_client import QdrantClient

    from .chunking import load_corpus
    from .embed import DEFAULT_MODEL, FastEmbedder, HashEmbedder
    from .retrieve import VectorStore

    embedder = HashEmbedder() if embedder_name == "hash" else FastEmbedder(model or DEFAULT_MODEL)
    return Retriever(load_corpus(root or find_root()), embedder, VectorStore(QdrantClient(":memory:")))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="python -m docrag.evaluate", description=__doc__.splitlines()[0])
    parser.add_argument("--embedder", choices=["fastembed", "hash"], default="fastembed")
    parser.add_argument("--model", help="the FastEmbed model (default: docrag.embed.DEFAULT_MODEL)")
    parser.add_argument("--out", type=Path, help="write the results as JSON there")
    parser.add_argument("--answers", type=Path, help="also ask Claude every question (needs a key), write the answers there")
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
    if args.answers:
        from .answer import Answerer

        answerer = Answerer(retriever)
        results["model"] = answerer.model
        results["answers"], records = answer_metrics(answerer, questions)
        args.answers.write_text(json.dumps(records, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    text = json.dumps(results, ensure_ascii=False, indent=2)
    print(text)
    if args.out:
        args.out.write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
