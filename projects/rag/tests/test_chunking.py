from pathlib import Path

import pytest

from docrag.chunking import chunk_document, find_root, load_corpus, split_blocks

DOC = """# Guide

Intro line one
and line two.

## Install

- first item
  goes on here
- second item

| a | b |
|---|---|
| 1 | 2 |

```sh
# not a heading
make
```

### Deep

Last words.
"""


def test_blocks_keep_their_lines_and_the_headings_above_them():
    blocks = split_blocks(DOC.splitlines())
    got = [(h, b.start, b.end, b.text.splitlines()[0]) for h, b in blocks]
    assert got == [
        (["Guide"], 3, 4, "Intro line one"),
        (["Guide", "Install"], 8, 9, "- first item"),
        (["Guide", "Install"], 10, 10, "- second item"),
        (["Guide", "Install"], 12, 14, "| a | b |"),
        (["Guide", "Install"], 16, 19, "```sh"),
        (["Guide", "Install", "Deep"], 23, 23, "Last words."),
    ]


def test_a_hash_inside_a_code_block_is_not_a_heading():
    code = [b for _, b in split_blocks(DOC.splitlines()) if b.text.startswith("```")]
    assert code[0].text.splitlines() == ["```sh", "# not a heading", "make", "```"]


def test_a_skipped_heading_level_leaves_no_empty_name_in_the_title():
    chunks = chunk_document("a.md", "# Top\n\n### Deep\n\ntext\n")
    assert chunks[0].title == "Top > Deep"


def test_passages_group_blocks_under_the_same_headings_up_to_the_size():
    chunks = chunk_document("docs/guide.md", DOC, max_chars=40)
    assert [(c.title, c.start, c.end) for c in chunks] == [
        ("Guide", 3, 4),
        ("Guide > Install", 8, 10),
        ("Guide > Install", 12, 14),
        ("Guide > Install", 16, 19),
        ("Guide > Install > Deep", 23, 23),
    ]
    assert chunks[1].id == "docs/guide.md:8-10"
    assert chunks[1].text.startswith("Guide > Install\n\n- first item")


def test_a_block_longer_than_the_size_is_never_cut():
    long = "word " * 500
    chunks = chunk_document("a.md", f"# T\n\n{long}\n\nshort\n", max_chars=100)
    assert [len(c.blocks) for c in chunks] == [1, 1]
    assert chunks[0].body == long.strip()


def test_the_corpus_reads_the_matching_files_in_a_stable_order(tmp_path: Path):
    (tmp_path / "projects" / "b").mkdir(parents=True)
    (tmp_path / "projects" / "a").mkdir()
    (tmp_path / "README.md").write_text("# Root\n\nhello\n", encoding="utf-8")
    (tmp_path / "projects" / "b" / "README.md").write_text("# B\n\nbee\n", encoding="utf-8")
    (tmp_path / "projects" / "a" / "DECISIONS.md").write_text("# A\n\nay\n", encoding="utf-8")
    (tmp_path / "projects" / "a" / "notes.md").write_text("# not read\n\nx\n", encoding="utf-8")
    (tmp_path / "projects" / "rag").mkdir()
    (tmp_path / "projects" / "rag" / "README.md").write_text("# this assistant's own\n\nx\n", encoding="utf-8")
    sources = [c.source for c in load_corpus(tmp_path)]
    assert sources == ["README.md", "projects/a/DECISIONS.md", "projects/b/README.md"]


def test_the_repository_is_found_from_any_folder_inside_it_and_nowhere_else(tmp_path: Path):
    (tmp_path / "projects" / "rag" / "eval").mkdir(parents=True)
    (tmp_path / "projects" / "rag" / "pyproject.toml").write_text("", encoding="utf-8")
    assert find_root(tmp_path / "projects" / "rag" / "eval") == tmp_path.resolve()
    assert find_root(tmp_path / "projects") == tmp_path.resolve()
    elsewhere = tmp_path / "elsewhere"
    elsewhere.mkdir()
    (tmp_path / "projects" / "rag" / "pyproject.toml").unlink()
    with pytest.raises(FileNotFoundError, match="DOCRAG_CORPUS"):
        find_root(elsewhere)
