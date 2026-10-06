"""ONNX export and inference: parity with PyTorch, deterministic preprocessing shared with the API, and
the committed model (export/pointnet.onnx) checked against a freshly generated test set."""
import hashlib
import json
from pathlib import Path

import numpy as np
import pytest
import torch

from shapeml.dataset import build
from shapeml.export import TOLERANCE, export
from shapeml.infer import OnnxClassifier, prepare, softmax
from shapeml.pointnet import train_pointnet
from shapeml.shapes import CLASSES, make_shape

ROOT = Path(__file__).resolve().parent.parent


@pytest.fixture(scope="module")
def tiny_model(tmp_path_factory):
    """A PointNet trained for two epochs on a small set: enough to compare the two runtimes."""
    d = build(per_class=8, points=128, seed=5)
    net, _ = train_pointnet(d["x"], d["y"], epochs=2, seed=0, batch_size=16)
    folder = tmp_path_factory.mktemp("export")
    torch.save(net.state_dict(), folder / "pointnet.pt")
    out = folder / "pointnet.onnx"
    export(folder / "pointnet.pt", list(CLASSES), 128, out)
    (folder / "pointnet.json").write_text(json.dumps({"classes": list(CLASSES), "points": 128}))
    net.eval()
    return net, OnnxClassifier(out), d


def test_onnx_runtime_gives_the_logits_of_pytorch(tiny_model):
    net, onnx_model, d = tiny_model
    with torch.no_grad():
        expected = net(torch.from_numpy(d["x"])).numpy()
    assert np.abs(expected - onnx_model.logits(d["x"])).max() < TOLERANCE


def test_any_batch_size_goes_through(tiny_model):
    _, onnx_model, d = tiny_model
    assert onnx_model.logits(d["x"][:1]).shape == (1, len(CLASSES))
    assert onnx_model.logits(d["x"][:7]).shape == (7, len(CLASSES))


def test_classify_returns_a_class_and_probabilities_that_sum_to_one(tiny_model):
    _, onnx_model, _ = tiny_model
    vertices, faces = make_shape("torus", np.random.default_rng(1))
    p = onnx_model.classify(vertices, faces)
    assert p.label in CLASSES
    assert abs(sum(p.probabilities.values()) - 1) < 1e-5
    assert p.label == max(p.probabilities, key=p.probabilities.get)


def test_preprocessing_is_deterministic_and_matches_training():
    vertices, faces = make_shape("cone", np.random.default_rng(2))
    a, b = prepare(vertices, faces, 256), prepare(vertices, faces, 256)
    assert a.shape == (1, 256, 3) and a.dtype == np.float32
    assert np.array_equal(a, b)
    assert abs(np.linalg.norm(a[0], axis=1).max() - 1) < 1e-6, "scaled into the unit sphere, as for training"
    assert np.allclose(a[0].mean(axis=0), 0, atol=1e-6)


def test_preprocessing_refuses_meshes_without_surface():
    with pytest.raises(ValueError, match="no triangle"):
        prepare(np.zeros((3, 3)), np.zeros((0, 3), dtype=int), 64)
    flat = np.array([[0, 0, 0], [1, 0, 0], [2, 0, 0]], dtype=float)
    with pytest.raises(ValueError, match="no surface"):
        prepare(flat, np.array([[0, 1, 2]]), 64)


def test_softmax_is_stable_on_large_logits():
    p = softmax(np.array([[1000.0, 0.0, -1000.0]]))
    assert np.isfinite(p).all() and abs(p.sum() - 1) < 1e-12


def test_the_committed_model_is_the_one_described_and_reaches_the_threshold():
    model = ROOT / "export" / "pointnet.onnx"
    meta = json.loads(model.with_suffix(".json").read_text())
    assert meta["onnx_sha256"] == hashlib.sha256(model.read_bytes()).hexdigest()
    assert meta["classes"] == list(CLASSES)
    assert meta["max_logit_gap"] < TOLERANCE
    # 20 fresh clouds per class, from the seed of the test set: the served model still recognises them.
    d = build(per_class=20, points=meta["points"], seed=2027)
    accuracy = (OnnxClassifier(model).logits(d["x"]).argmax(axis=1) == d["y"]).mean()
    assert accuracy >= 0.9, accuracy
