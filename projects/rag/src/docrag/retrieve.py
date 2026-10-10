"""Finding the passages that answer a question: keywords (BM25), meaning (vectors in Qdrant), or both fused."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Literal

from qdrant_client import QdrantClient, models

from .bm25 import BM25
from .chunking import Chunk
from .embed import Embedder

Mode = Literal["bm25", "dense", "hybrid"]
MODES: tuple[Mode, ...] = ("bm25", "dense", "hybrid")

# Reciprocal rank fusion: a passage scores 1 / (RRF_K + rank) in each list it appears in.
RRF_K = 60


@dataclass(frozen=True)
class Hit:
    chunk: Chunk
    score: float


class VectorStore:
    """The passages' vectors in a Qdrant collection (cosine), on a server or in memory."""

    def __init__(self, client: QdrantClient, collection: str = "docs") -> None:
        self.client, self.collection = client, collection

    def index(self, vectors: list[list[float]], batch: int = 256) -> None:
        if self.client.collection_exists(self.collection):
            self.client.delete_collection(self.collection)
        size = len(vectors[0]) if vectors else 1
        self.client.create_collection(self.collection, vectors_config=models.VectorParams(size=size, distance=models.Distance.COSINE))
        for i in range(0, len(vectors), batch):
            points = [models.PointStruct(id=j, vector=v) for j, v in enumerate(vectors[i:i + batch], start=i)]
            self.client.upsert(self.collection, points=points)

    def search(self, vector: list[float], k: int) -> list[tuple[int, float]]:
        found = self.client.query_points(self.collection, query=vector, limit=k).points
        return [(int(p.id), float(p.score)) for p in found]


class Retriever:
    def __init__(self, chunks: list[Chunk], embedder: Embedder, store: VectorStore) -> None:
        if not chunks:
            raise ValueError("no passage to search: is the corpus empty?")
        self.chunks, self.embedder, self.store = chunks, embedder, store
        self.bm25 = BM25([c.text for c in chunks])
        store.index(embedder.embed([c.text for c in chunks]))

    def search(self, query: str, k: int = 5, mode: Mode = "hybrid") -> list[Hit]:
        if mode == "bm25":
            ranked = self.bm25.top(query, k)
        elif mode == "dense":
            ranked = self.store.search(self.embedder.embed([query])[0], k)
        elif mode == "hybrid":
            depth = max(20, 4 * k)
            fused: dict[int, float] = {}
            for results in (self.bm25.top(query, depth), self.store.search(self.embedder.embed([query])[0], depth)):
                for rank, (i, _) in enumerate(results, start=1):
                    fused[i] = fused.get(i, 0.0) + 1.0 / (RRF_K + rank)
            ranked = sorted(fused.items(), key=lambda item: (-item[1], item[0]))[:k]
        else:
            raise ValueError(f"unknown search mode {mode!r}")
        return [Hit(self.chunks[i], s) for i, s in ranked]
