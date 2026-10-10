from qdrant_client import QdrantClient

from docrag.answer import MODEL, NO_ANSWER, Answerer, Citation, search_results
from docrag.chunking import chunk_document
from docrag.embed import HashEmbedder
from docrag.evaluate import answer_metrics
from docrag.retrieve import Retriever, VectorStore
from fakes import FakeClient, cite, text

DOC = """# Journal

## D8. Node 22 minimum

The site needs Node 22 or later.

It uses node:test and the fetch API.

## D9. Deployment

GitHub Actions deploys to Pages after the tests pass.
"""


def retriever() -> Retriever:
    return Retriever(chunk_document("DECISIONS.md", DOC), HashEmbedder(), VectorStore(QdrantClient(":memory:")))


def node_answer(request):
    """Cites the second block of the first passage when it is the Node one."""
    first = request["messages"][0]["content"][0]
    if "D8." not in first["title"]:
        return [text(NO_ANSWER)], "end_turn"
    return [text("Node 22 or later, "), text("for node:test and fetch.", [cite(0, 1, 2, "It uses node:test and the fetch API.")])], "end_turn"


def test_the_passages_go_as_search_results_with_one_block_per_markdown_block_and_citations_on():
    hits = retriever().search("Node version", k=1)
    [result] = search_results(hits)
    assert result["source"] == "DECISIONS.md#L5-L7"
    assert result["title"] == "Journal > D8. Node 22 minimum"
    assert [b["text"] for b in result["content"]] == ["The site needs Node 22 or later.", "It uses node:test and the fetch API."]
    assert result["citations"] == {"enabled": True}


def test_the_request_holds_only_the_passages_and_the_question_with_low_effort_and_fallbacks():
    client = FakeClient(node_answer)
    Answerer(retriever(), client, k=2).ask("Which Node version?")
    [request] = client.requests
    assert request["model"] == MODEL
    assert request["output_config"] == {"effort": "low"}
    assert request["fallbacks"] == "default" and request["betas"] == ["server-side-fallback-2026-07-01"]
    content = request["messages"][0]["content"]
    assert [c["type"] for c in content] == ["search_result", "search_result", "text"]
    assert content[-1]["text"] == "Which Node version?"
    assert NO_ANSWER in request["system"]


def test_a_citation_points_back_to_the_lines_of_the_cited_blocks():
    a = Answerer(retriever(), FakeClient(node_answer), k=2).ask("Which Node version?")
    assert a.found
    assert a.text == "Node 22 or later, for node:test and fetch."
    assert a.citations == [Citation("DECISIONS.md", 7, 7, "Journal > D8. Node 22 minimum", "It uses node:test and the fetch API.")]


def test_no_answer_an_uncited_answer_or_a_refusal_all_count_as_not_found():
    r = retriever()
    assert not Answerer(r, FakeClient(lambda _: ([text(NO_ANSWER)], "end_turn"))).ask("Which database?").found
    uncited = Answerer(r, FakeClient(lambda _: ([text("Probably PostgreSQL.")], "end_turn"))).ask("Which database?")
    assert (uncited.found, uncited.citations) == (False, [])
    refused = Answerer(r, FakeClient(lambda _: ([], "refusal"))).ask("Which database?")
    assert (refused.found, refused.text) == (False, NO_ANSWER)


def test_citations_out_of_range_or_empty_are_dropped_and_repeats_kept_once():
    reply = [text("a", [cite(0, 0, 1), cite(9, 0, 1), cite(0, 5, 6)]), text("b", [cite(0, 0, 1)])]
    a = Answerer(retriever(), FakeClient(lambda _: (reply, "end_turn")), k=1).ask("Node version")
    assert [(c.start, c.end) for c in a.citations] == [(5, 5)]


def test_the_answer_measures_count_right_sources_precision_and_abstentions():
    questions = [
        {"id": "a1", "lang": "en", "question": "Which Node version?", "answerable": True, "expected": [{"source": "DECISIONS.md", "section": "D8."}]},
        {"id": "a2", "lang": "en", "question": "Which Node version again?", "answerable": True, "expected": [{"source": "DECISIONS.md", "section": "D9."}]},
        {"id": "u1", "lang": "en", "question": "Which Kubernetes cluster?", "answerable": False},
    ]
    def reply(request):
        question = request["messages"][0]["content"][-1]["text"]
        if "Kubernetes" in question:
            return [text(NO_ANSWER)], "end_turn"
        d8 = next(i for i, c in enumerate(request["messages"][0]["content"]) if "D8." in c.get("title", ""))
        return [text("Node 22.", [cite(d8, 0, 1)])], "end_turn"

    scores, records = answer_metrics(Answerer(retriever(), FakeClient(reply), k=2), questions)
    # a1 and a2 both get the Node answer, right for a1 only; u1 gets no answer.
    assert scores == {"answered": 1.0, "answered_with_right_source": 0.5, "citation_precision": 0.5, "abstained_when_unanswerable": 1.0}
    assert [(r["id"], r["found"], r["cites_right_place"]) for r in records] == [("a1", True, True), ("a2", True, False), ("u1", False, False)]
    assert records[0]["citations"] == [{"source": "DECISIONS.md", "start": 5, "end": 5, "title": "Journal > D8. Node 22 minimum", "right": True}]
