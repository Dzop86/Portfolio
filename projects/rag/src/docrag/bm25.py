"""Okapi BM25 over the passages: the keyword half of the hybrid search, without any dependency."""
from __future__ import annotations

import math
import re
import unicodedata
from collections import Counter

# Words too common in French and English to tell passages apart.
STOPWORDS = frozenset(
    """a an and are as at be by de des du est et for from how in is it la le les of on or que qui
    the to un une what which with dans pour par sur au aux ce ces son sa ses il elle on ne pas plus
    quel quelle quels quelles comment pourquoi does do this that""".split()
)


def tokenize(text: str) -> list[str]:
    """Lower case, without accents, words and numbers of two characters or more, common words left out."""
    plain = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode().lower()
    return [w for w in re.findall(r"[a-z0-9]+", plain) if len(w) > 1 and w not in STOPWORDS]


class BM25:
    def __init__(self, documents: list[str], k1: float = 1.5, b: float = 0.75) -> None:
        self.k1, self.b = k1, b
        self.docs = [Counter(tokenize(d)) for d in documents]
        self.lengths = [sum(d.values()) for d in self.docs]
        self.average = sum(self.lengths) / len(self.docs) if self.docs else 0.0
        df: Counter[str] = Counter()
        for d in self.docs:
            df.update(d.keys())
        n = len(self.docs)
        self.idf = {w: math.log((n - f + 0.5) / (f + 0.5) + 1) for w, f in df.items()}

    def scores(self, query: str) -> list[float]:
        words = tokenize(query)
        out = []
        for doc, length in zip(self.docs, self.lengths):
            norm = self.k1 * (1 - self.b + self.b * length / self.average) if self.average else self.k1
            out.append(sum(self.idf[w] * doc[w] * (self.k1 + 1) / (doc[w] + norm) for w in words if w in doc))
        return out

    def top(self, query: str, k: int) -> list[tuple[int, float]]:
        """The ``k`` best documents with a score above zero, best first (ties by position)."""
        ranked = sorted(((s, -i) for i, s in enumerate(self.scores(query)) if s > 0), reverse=True)
        return [(-i, s) for s, i in ranked[:k]]
