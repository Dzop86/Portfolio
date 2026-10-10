import pytest
from qdrant_client import QdrantClient

from docrag.bm25 import BM25, tokenize
from docrag.chunking import chunk_document
from docrag.embed import HashEmbedder
from docrag.retrieve import RRF_K, Retriever, VectorStore

DOC = """# Build

The site is built by a small Node script, without any framework.

# Deploy

GitHub Actions deploys to Pages once every test passes.

# Mesh

The C library reads OBJ, PLY and STL files and triangulates polygons as a fan.
"""


def retriever(embedder=None) -> Retriever:
    return Retriever(chunk_document("doc.md", DOC), embedder or HashEmbedder(), VectorStore(QdrantClient(":memory:")))


def test_words_lose_their_case_accents_and_common_words():
    assert tokenize("Où est la Génération du maillage ? The OBJ files, v2") == ["ou", "generation", "maillage", "obj", "files", "v2"]


def test_bm25_ranks_the_passage_with_the_rare_words_first_and_ignores_the_others():
    bm25 = BM25(["the cat sat", "the dog sat", "a cat and a cat"])
    assert [i for i, _ in bm25.top("cat", 5)] == [2, 0]
    assert bm25.top("unicorn", 5) == []


def test_bm25_weighs_a_rare_word_above_a_common_one_said_twice():
    bm25 = BM25(["common common", "rare", "common other", "common other", "common other"])
    assert bm25.top("common rare", 1)[0][0] == 1


@pytest.mark.parametrize("mode", ["bm25", "dense", "hybrid"])
def test_each_mode_finds_the_passage_that_holds_the_words(mode):
    hits = retriever().search("which files does the C library read, OBJ or STL?", k=2, mode=mode)
    assert hits[0].chunk.title == "Mesh"
    assert hits[0].chunk.start == 11


def test_hybrid_fuses_the_two_rankings_by_reciprocal_rank():
    r = retriever()
    query = "deploys Pages"
    bm25 = [i for i, _ in r.bm25.top(query, 20)]
    dense = [i for i, _ in r.store.search(r.embedder.embed([query])[0], 20)]
    expected = {i: sum(1 / (RRF_K + lst.index(i) + 1) for lst in (bm25, dense) if i in lst) for i in set(bm25) | set(dense)}
    hits = r.search(query, k=3, mode="hybrid")
    assert [h.score for h in hits] == sorted(expected.values(), reverse=True)[:3]


def test_hybrid_keeps_a_passage_only_one_search_found():
    class Constant:
        """Every text gets the same vector: only the order of the store decides the dense ranking."""

        name, dim = "constant", 2

        def embed(self, texts):
            return [[1.0, 0.0] for _ in texts]

    hits = retriever(Constant()).search("framework", k=3, mode="hybrid")
    assert {h.chunk.title for h in hits} == {"Build", "Deploy", "Mesh"}
    assert hits[0].chunk.title == "Build"  # first in both lists


def test_an_unknown_mode_or_an_empty_corpus_is_refused():
    with pytest.raises(ValueError, match="unknown search mode"):
        retriever().search("x", mode="magic")
    with pytest.raises(ValueError, match="no passage"):
        Retriever([], HashEmbedder(), VectorStore(QdrantClient(":memory:")))


def test_indexing_again_replaces_the_collection():
    store = VectorStore(QdrantClient(":memory:"))
    store.index([[1.0, 0.0], [0.0, 1.0]])
    store.index([[0.0, 1.0]])
    assert store.search([0.0, 1.0], 5) == [(0, pytest.approx(1.0))]
