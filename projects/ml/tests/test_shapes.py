"""Unit tests for the synthetic shape generators: valid meshes with the expected topology."""
import numpy as np
import pytest

from shapeml.shapes import CLASSES, make_shape


def euler_characteristic(vertices: np.ndarray, faces: np.ndarray) -> int:
    edges = np.sort(np.concatenate([faces[:, [0, 1]], faces[:, [1, 2]], faces[:, [2, 0]]]), axis=1)
    return len(vertices) - len(np.unique(edges, axis=0)) + len(faces)


@pytest.mark.parametrize("name", CLASSES)
def test_every_class_gives_a_valid_closed_mesh(name):
    rng = np.random.default_rng(0)
    for _ in range(20):
        v, f = make_shape(name, rng)
        assert v.ndim == 2 and v.shape[1] == 3 and np.isfinite(v).all()
        assert f.min() >= 0 and f.max() < len(v)
        assert (f[:, 0] != f[:, 1]).all() and (f[:, 1] != f[:, 2]).all() and (f[:, 2] != f[:, 0]).all()
        assert euler_characteristic(v, f) == (0 if name == "torus" else 2), name


def test_shapes_vary_in_size_resolution_and_orientation():
    rng = np.random.default_rng(1)
    meshes = [make_shape("cylinder", rng) for _ in range(10)]
    assert len({len(v) for v, _ in meshes}) > 3, "resolution varies"
    extents = [np.ptp(v, axis=0) for v, _ in meshes]
    assert np.std([e.max() / e.min() for e in extents]) > 0.05, "proportions and orientation vary"


def test_unknown_class_is_rejected():
    with pytest.raises(ValueError, match="unknown shape"):
        make_shape("klein bottle", np.random.default_rng(0))
