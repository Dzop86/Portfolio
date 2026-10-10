"""Answers written by Claude from the retrieved passages only, each claim cited down to the file and lines.

The passages go to the Messages API as ``search_result`` blocks, one text block per Markdown block, so
that Claude's citations (``search_result_location``: which passage, which blocks) map back to lines of a
file. An answer without any citation is not trusted: it counts as no answer.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from .retrieve import Hit, Mode, Retriever

MODEL = "claude-opus-5-5"
NO_ANSWER = "NO_ANSWER"
SYSTEM = f"""You answer a development team's questions about its own technical documentation.
Use only the search results given with the question: each one is a passage of a documentation file.
Answer in the language of the question, in a few sentences, and cite the passages you rely on.
If the search results do not hold the answer, reply with exactly {NO_ANSWER} and nothing else: never
answer from general knowledge, and never guess what the documentation might say."""


@dataclass(frozen=True)
class Citation:
    source: str
    start: int  # first line cited, from 1
    end: int  # last line cited, included
    title: str
    quote: str


@dataclass
class Answer:
    question: str
    text: str
    found: bool
    citations: list[Citation] = field(default_factory=list)
    passages: list[Hit] = field(default_factory=list)
    model: str = ""


def search_results(hits: list[Hit]) -> list[dict[str, Any]]:
    """The passages as ``search_result`` content blocks, citations on, one text block per Markdown block."""
    return [
        {
            "type": "search_result",
            "source": f"{h.chunk.source}#L{h.chunk.start}-L{h.chunk.end}",
            "title": h.chunk.title or h.chunk.source,
            "content": [{"type": "text", "text": b.text} for b in h.chunk.blocks],
            "citations": {"enabled": True},
        }
        for h in hits
    ]


def citations_of(response: Any, hits: list[Hit]) -> list[Citation]:
    """Claude's citations, mapped from (passage, blocks) back to (file, lines), each once, in order."""
    out: list[Citation] = []
    for block in response.content:
        if block.type != "text":
            continue
        for c in getattr(block, "citations", None) or []:
            if c.type != "search_result_location" or not 0 <= c.search_result_index < len(hits):
                continue
            chunk = hits[c.search_result_index].chunk
            cited = chunk.blocks[c.start_block_index:c.end_block_index]
            if not cited:
                continue
            citation = Citation(chunk.source, cited[0].start, cited[-1].end, chunk.title, c.cited_text)
            if citation not in out:
                out.append(citation)
    return out


class Answerer:
    def __init__(self, retriever: Retriever, client: Any = None, model: str = MODEL, k: int = 6, mode: Mode = "hybrid", effort: str = "low") -> None:
        if client is None:
            import anthropic

            client = anthropic.Anthropic()
        self.retriever, self.client, self.model, self.k, self.mode, self.effort = retriever, client, model, k, mode, effort

    def ask(self, question: str) -> Answer:
        hits = self.retriever.search(question, self.k, self.mode)
        if not hits:
            return Answer(question, NO_ANSWER, False, model=self.model)
        response = self.client.beta.messages.create(
            model=self.model,
            max_tokens=4096,
            system=SYSTEM,
            # Short answers from a few passages: low effort is enough, and costs less.
            output_config={"effort": self.effort},
            # If a safety classifier declines, the API reruns the request on a model chosen for that case.
            betas=["server-side-fallback-2026-07-01"],
            fallbacks="default",
            messages=[{"role": "user", "content": [*search_results(hits), {"type": "text", "text": question}]}],
        )
        if response.stop_reason == "refusal":
            return Answer(question, NO_ANSWER, False, passages=hits, model=response.model)
        text = "".join(b.text for b in response.content if b.type == "text").strip()
        citations = citations_of(response, hits)
        found = text != NO_ANSWER and bool(citations)
        return Answer(question, text, found, citations if found else [], hits, response.model)
