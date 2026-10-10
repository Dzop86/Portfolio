"""Text embeddings: a multilingual model (FastEmbed, ONNX, no PyTorch) or, for the tests, a hashed bag of words."""
from __future__ import annotations

import hashlib
import math
from typing import Protocol, Sequence

from .bm25 import tokenize

# Multilingual (the documentation is in French and English), 384 dimensions, about 220 MB.
DEFAULT_MODEL = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"


class Embedder(Protocol):
    name: str
    dim: int

    def embed(self, texts: Sequence[str]) -> list[list[float]]: ...


class HashEmbedder:
    """A deterministic stand-in for the tests: each word lands in one of ``dim`` buckets.

    It knows nothing of meaning (two synonyms never meet), so it is only good to check the plumbing, on
    every OS, without downloading a model.
    """

    def __init__(self, dim: int = 256) -> None:
        self.name, self.dim = f"hash-{dim}", dim

    def embed(self, texts: Sequence[str]) -> list[list[float]]:
        out = []
        for text in texts:
            v = [0.0] * self.dim
            for word in tokenize(text):
                h = int.from_bytes(hashlib.blake2b(word.encode(), digest_size=8).digest(), "little")
                v[h % self.dim] += 1.0 if (h >> 32) & 1 else -1.0
            norm = math.sqrt(sum(x * x for x in v)) or 1.0
            out.append([x / norm for x in v])
        return out


class FastEmbedder:
    def __init__(self, model: str = DEFAULT_MODEL, cache_dir: str | None = None) -> None:
        from fastembed import TextEmbedding  # optional dependency: pip install ".[embed]"

        self._model = TextEmbedding(model_name=model, cache_dir=cache_dir)
        self.name = model
        self.dim = len(next(iter(self._model.embed(["dimension"]))))

    def embed(self, texts: Sequence[str]) -> list[list[float]]:
        # Small batches: 256 passages of 512 tokens at once (FastEmbed's default) need gigabytes.
        return [v.tolist() for v in self._model.embed(list(texts), batch_size=32)]
