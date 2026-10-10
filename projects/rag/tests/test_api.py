import anthropic
import httpx2
import pytest
from fastapi.testclient import TestClient
from qdrant_client import QdrantClient

from docrag.answer import NO_ANSWER
from docrag.api import create_app
from docrag.chunking import chunk_document, load_corpus
from docrag.embed import HashEmbedder
from docrag.evaluate import ROOT
from docrag.retrieve import Retriever, VectorStore
from fakes import FakeClient, cite, text

DOC = "# Guide\n\n## Install\n\nRun make install as root.\n\n## Usage\n\nRun the viewer with a mesh file.\n"


def app(client=None, doc=DOC):
    r = Retriever(chunk_document("guide.md", doc), HashEmbedder(), VectorStore(QdrantClient(":memory:")))
    return TestClient(create_app(r, client))


def cite_first(_):
    return [text("Run make install.", [cite(0, 0, 1, "Run make install as root.")])], "end_turn"


def test_health_and_search_return_passages_with_their_lines():
    with app() as c:
        assert c.get("/health").json() == {"status": "ok", "passages": 2, "embedder": "hash-256"}
        found = c.get("/search", params={"q": "install make", "k": 1, "mode": "bm25"}).json()
        assert found == [{"source": "guide.md", "start": 5, "end": 5, "title": "Guide > Install", "score": pytest.approx(found[0]["score"]), "text": "Run make install as root."}]


def test_search_refuses_bad_parameters():
    with app() as c:
        assert c.get("/search", params={"q": "x"}).status_code == 422
        assert c.get("/search", params={"q": "install", "k": 0}).status_code == 422
        assert c.get("/search", params={"q": "install", "mode": "magic"}).status_code == 422


def test_ask_answers_with_its_citations():
    with app(FakeClient(cite_first)) as c:
        body = c.post("/ask", json={"question": "How do I install it?"}).json()
    assert body["found"] is True
    assert body["citations"] == [{"source": "guide.md", "start": 5, "end": 5, "title": "Guide > Install", "quote": "Run make install as root."}]


def test_ask_without_a_key_or_with_a_refused_key_says_so(monkeypatch):
    monkeypatch.delenv("ANTHROPIC_API_KEY", raising=False)
    with app() as c:
        r = c.post("/ask", json={"question": "How do I install it?"})
        assert r.status_code == 503 and "ANTHROPIC_API_KEY" in r.json()["detail"]
        assert c.get("/search", params={"q": "install"}).status_code == 200

    def refuse(_):
        raise anthropic.AuthenticationError("bad key", response=httpx2.Response(401, request=httpx2.Request("POST", "https://x")), body=None)

    with app(FakeClient(refuse)) as c:
        assert c.post("/ask", json={"question": "How do I install it?"}).status_code == 503
        assert c.post("/ask", json={"question": "?"}).status_code == 422


def test_integration_the_real_documentation_through_the_api():
    """The repository's documentation, the hashed embedder, a fake model that says what it was given."""
    r = Retriever(load_corpus(ROOT), HashEmbedder(), VectorStore(QdrantClient(":memory:")))
    client = FakeClient(lambda req: ([text(NO_ANSWER)], "end_turn"))
    with TestClient(create_app(r, client)) as c:
        assert c.get("/health").json()["passages"] > 300
        hits = c.get("/search", params={"q": "Quelle version minimale de Node faut-il ?", "k": 3}).json()
        assert any(h["source"] == "DECISIONS.md" and "D8." in h["title"] for h in hits)
        assert c.post("/ask", json={"question": "Which Kubernetes cluster runs the APIs?"}).json()["found"] is False
    assert len(client.requests[0]["messages"][0]["content"]) == 7  # six passages and the question
