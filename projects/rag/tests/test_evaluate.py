import json

import pytest
from qdrant_client import QdrantClient

from docrag.chunking import chunk_document, load_corpus
from docrag.embed import HashEmbedder
from docrag.evaluate import ROOT, check_expected, load_questions, relevant, retrieval_metrics
from docrag.retrieve import Retriever, VectorStore


def write(tmp_path, rows):
    path = tmp_path / "q.jsonl"
    path.write_text("\n".join(json.dumps(r) for r in rows), encoding="utf-8")
    return path


def test_questions_need_unique_ids_and_a_place_exactly_when_they_are_answerable(tmp_path):
    ok = {"id": "a", "question": "?", "answerable": True, "expected": [{"source": "x.md"}]}
    assert load_questions(write(tmp_path, [ok, {"id": "u", "question": "?", "answerable": False}]))[1]["id"] == "u"
    with pytest.raises(ValueError, match="share an id"):
        load_questions(write(tmp_path, [ok, ok]))
    with pytest.raises(ValueError, match="needs where"):
        load_questions(write(tmp_path, [{"id": "a", "question": "?", "answerable": True}]))
    with pytest.raises(ValueError, match="needs where"):
        load_questions(write(tmp_path, [{**ok, "answerable": False}]))


def test_a_passage_is_relevant_in_the_right_file_under_the_right_headings():
    chunk = chunk_document("d.md", "# Journal\n\n## D4. Privacy scan\n\ntext\n")[0]
    assert relevant(chunk, [{"source": "d.md", "section": "d4."}])
    assert relevant(chunk, [{"source": "d.md"}])
    assert not relevant(chunk, [{"source": "d.md", "section": "D5."}])
    assert not relevant(chunk, [{"source": "e.md", "section": "D4."}])


def test_recall_and_mrr_count_the_rank_of_the_first_right_passage():
    doc = "# Alpha\n\napple apple\n\n# Beta\n\napple banana\n\n# Gamma\n\ncherry\n"
    r = Retriever(chunk_document("f.md", doc), HashEmbedder(), VectorStore(QdrantClient(":memory:")))
    questions = [
        {"id": "1", "question": "apple", "answerable": True, "expected": [{"source": "f.md", "section": "Alpha"}]},  # rank 1
        {"id": "2", "question": "apple", "answerable": True, "expected": [{"source": "f.md", "section": "Beta"}]},  # rank 2
        {"id": "3", "question": "durian", "answerable": True, "expected": [{"source": "f.md", "section": "Gamma"}]},  # not found by words
        {"id": "4", "question": "nothing", "answerable": False},  # not counted
    ]
    bm25 = retrieval_metrics(r, questions)["bm25"]
    assert bm25 == {"recall@1": 0.333, "recall@3": 0.667, "recall@5": 0.667, "mrr": 0.5}


def test_integration_every_reference_question_points_to_a_passage_of_the_real_documentation():
    questions = load_questions()
    chunks = load_corpus(ROOT)
    assert len(chunks) > 300
    assert check_expected(chunks, questions) == []
    assert sum(q["answerable"] for q in questions) >= 30
    assert sum(not q["answerable"] for q in questions) >= 8
    assert {q["lang"] for q in questions} == {"fr", "en"}


def test_integration_hybrid_search_over_the_real_documentation_without_a_model():
    """With the hashed bag of words, the words alone already find most answers in the first five."""
    r = Retriever(load_corpus(ROOT), HashEmbedder(), VectorStore(QdrantClient(":memory:")))
    scores = retrieval_metrics(r, load_questions())
    assert scores["bm25"]["recall@5"] >= 0.6
    hits = r.search("Quelle version minimale de Node faut-il ?", k=3)
    assert any(h.chunk.source == "DECISIONS.md" and "D8." in h.chunk.title for h in hits)
