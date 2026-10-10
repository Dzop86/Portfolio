"""The HTTP API: search the documentation, or ask a question and get an answer that cites its sources.

Configuration, by environment variables:
- ``DOCRAG_CORPUS``: the folder holding the documentation (default: this repository);
- ``QDRANT_URL``: a Qdrant server (default: Qdrant in memory, in the process);
- ``DOCRAG_EMBEDDER``: ``fastembed`` (default, the multilingual model) or ``hash`` (no model, for trials);
- ``ANTHROPIC_API_KEY``: for ``/ask`` only; without it, ``/ask`` answers 503 and ``/search`` still works.
"""
from __future__ import annotations

import os
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Annotated, Any

from fastapi import FastAPI, HTTPException, Query
from pydantic import BaseModel, Field

from .answer import Answerer
from .retrieve import Mode, Retriever

ROOT = Path(__file__).resolve().parents[4]


class Passage(BaseModel):
    source: str
    start: int
    end: int
    title: str
    score: float
    text: str


class Question(BaseModel):
    question: Annotated[str, Field(min_length=3, max_length=500)]


class CitationOut(BaseModel):
    source: str
    start: int
    end: int
    title: str
    quote: str


class AnswerOut(BaseModel):
    answer: str
    found: bool
    citations: list[CitationOut]
    model: str


def build_retriever() -> Retriever:
    from qdrant_client import QdrantClient

    from .chunking import load_corpus
    from .embed import FastEmbedder, HashEmbedder
    from .retrieve import VectorStore

    corpus = Path(os.environ.get("DOCRAG_CORPUS", ROOT))
    embedder = HashEmbedder() if os.environ.get("DOCRAG_EMBEDDER") == "hash" else FastEmbedder()
    client = QdrantClient(url=os.environ["QDRANT_URL"]) if os.environ.get("QDRANT_URL") else QdrantClient(":memory:")
    return Retriever(load_corpus(corpus), embedder, VectorStore(client))


def create_app(retriever: Retriever | None = None, client: Any = None) -> FastAPI:
    """The app; without a retriever, it builds one at start-up from the environment."""
    state: dict[str, Any] = {}

    @asynccontextmanager
    async def lifespan(_: FastAPI):
        state["retriever"] = retriever or build_retriever()
        yield

    app = FastAPI(title="docrag", version="0.1.0", lifespan=lifespan,
                  description="A documentation assistant: hybrid search over Markdown docs, answers that cite file and lines.")

    def answerer() -> Answerer:
        if "answerer" not in state:
            if client is None and not os.environ.get("ANTHROPIC_API_KEY"):
                raise HTTPException(503, "no ANTHROPIC_API_KEY: /search works, /ask needs a key")
            state["answerer"] = Answerer(state["retriever"], client)
        return state["answerer"]

    @app.get("/health")
    def health() -> dict[str, Any]:
        r: Retriever = state["retriever"]
        return {"status": "ok", "passages": len(r.chunks), "embedder": r.embedder.name}

    @app.get("/search", response_model=list[Passage])
    def search(q: Annotated[str, Query(min_length=2, max_length=500)], k: Annotated[int, Query(ge=1, le=20)] = 5, mode: Mode = "hybrid") -> list[Passage]:
        hits = state["retriever"].search(q, k, mode)
        return [Passage(source=h.chunk.source, start=h.chunk.start, end=h.chunk.end, title=h.chunk.title, score=h.score, text=h.chunk.body) for h in hits]

    @app.post("/ask", response_model=AnswerOut)
    def ask(body: Question) -> AnswerOut:
        import anthropic

        try:
            a = answerer().ask(body.question)
        except anthropic.AuthenticationError as e:
            raise HTTPException(503, "the Claude API refused the key") from e
        except anthropic.RateLimitError as e:
            raise HTTPException(429, "the Claude API is rate limiting this key, try again later") from e
        except (anthropic.APIConnectionError, anthropic.InternalServerError) as e:
            raise HTTPException(502, "the Claude API could not be reached") from e
        return AnswerOut(answer=a.text, found=a.found, model=a.model,
                         citations=[CitationOut(**vars(c)) for c in a.citations])

    return app


app = create_app()
