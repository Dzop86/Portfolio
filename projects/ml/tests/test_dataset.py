"""Unit tests for surface sampling and the dataset build."""
import numpy as np

from shapeml.dataset import build, sample_surface
from shapeml.shapes import CLASSES


def test_samples_lie_on_the_surface_and_are_normalised():
    # Octahedron |x| + |y| + |z| = 1: every sampled point satisfies it before normalisation.
    v = np.array([[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]], float)
    f = np.array([[0, 2, 4], [2, 1, 4], [1, 3, 4], [3, 0, 4], [2, 0, 5], [1, 2, 5], [3, 1, 5], [0, 3, 5]])
    raw = sample_surface(v, f, 2000, np.random.default_rng(0), normalise=False)
    assert np.allclose(np.abs(raw).sum(axis=1), 1.0)
    pts = sample_surface(v, f, 2000, np.random.default_rng(0))
    assert np.isclose(np.linalg.norm(pts, axis=1).max(), 1.0)
    assert np.allclose(pts.mean(axis=0), 0.0, atol=0.05)


def test_sampling_is_area_weighted():
    # Two triangles, one with 9 times the area of the other.
    v = np.array([[0, 0, 0], [1, 0, 0], [0, 1, 0], [10, 0, 0], [13, 0, 0], [10, 3, 0]], float)
    f = np.array([[0, 1, 2], [3, 4, 5]])
    pts = sample_surface(v, f, 10000, np.random.default_rng(0), normalise=False)
    share_big = (pts[:, 0] >= 10).mean()
    assert 0.88 < share_big < 0.92


def test_build_is_deterministic_balanced_and_shaped():
    a = build(per_class=5, points=64, seed=7)
    b = build(per_class=5, points=64, seed=7)
    c = build(per_class=5, points=64, seed=8)
    assert a["x"].shape == (5 * len(CLASSES), 64, 3) and a["x"].dtype == np.float32
    assert np.array_equal(a["x"], b["x"]) and np.array_equal(a["y"], b["y"])
    assert not np.array_equal(a["x"], c["x"])
    assert np.bincount(a["y"]).tolist() == [5] * len(CLASSES)
    assert list(a["classes"]) == list(CLASSES)


def test_generate_writes_disjoint_train_and_test_sets(tmp_path):
    from shapeml.generate import main

    (tmp_path / "params.yaml").write_text("generate: {seed: 1, train_per_class: 3, test_per_class: 2, points: 32}\n")
    main(["--params", str(tmp_path / "params.yaml"), "--out", str(tmp_path / "d" / "shapes.npz")])
    d = np.load(tmp_path / "d" / "shapes.npz")
    assert d["x_train"].shape == (3 * len(CLASSES), 32, 3) and d["x_test"].shape == (2 * len(CLASSES), 32, 3)
    flat_train = {row.tobytes() for row in d["x_train"]}
    assert not any(row.tobytes() in flat_train for row in d["x_test"])
