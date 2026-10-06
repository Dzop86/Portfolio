"""Unit tests for the features, both models and the accuracy gate."""
import json

import numpy as np
import torch  # the `train` extra is required: a skip here would hide the PointNet tests in CI

from shapeml.dataset import build
from shapeml.features import describe
from shapeml.pointnet import PointNet, train_pointnet
from shapeml.shapes import CLASSES


def random_rotation(seed):
    q, r = np.linalg.qr(np.random.default_rng(seed).normal(size=(3, 3)))
    return (q * np.sign(np.diag(r))).astype(np.float32)


def test_features_are_invariant_to_rotation_and_point_order():
    cloud = build(per_class=1, points=256, seed=0)["x"][1]
    base = describe(cloud[None])[0]
    rotated = describe((cloud @ random_rotation(1).T)[None])[0]
    shuffled = describe(cloud[np.random.default_rng(2).permutation(256)][None])[0]
    assert np.allclose(base, rotated, atol=1e-5)
    assert np.allclose(base, shuffled, atol=1e-5)


def test_features_separate_a_sphere_from_a_cylinder():
    d = build(per_class=10, points=256, seed=3)
    f = describe(d["x"])
    sphere, cylinder = f[d["y"] == CLASSES.index("sphere")], f[d["y"] == CLASSES.index("cylinder")]
    assert np.linalg.norm(sphere.mean(0) - cylinder.mean(0)) > 3 * max(sphere.std(0).mean(), 1e-6)


def test_pointnet_is_invariant_to_point_order():
    torch.manual_seed(0)
    net = PointNet(classes=len(CLASSES)).eval()
    cloud = torch.from_numpy(build(per_class=1, points=128, seed=4)["x"][:2])
    perm = torch.randperm(128)
    assert torch.allclose(net(cloud), net(cloud[:, perm]), atol=1e-5)
    assert net(cloud).shape == (2, len(CLASSES))


def test_pointnet_learns_on_a_tiny_set():
    d = build(per_class=8, points=128, seed=5)
    _, history = train_pointnet(d["x"], d["y"], epochs=15, seed=0, batch_size=16, augment=False)
    assert history[-1] < 0.6 * history[0], f"loss went from {history[0]:.3f} to {history[-1]:.3f}"


def test_the_gate_fails_below_the_threshold(tmp_path):
    from shapeml.check import main

    metrics = tmp_path / "metrics.json"
    metrics.write_text(json.dumps({"baseline": {"accuracy": 0.8}, "pointnet": {"accuracy": 0.95}}))
    assert main(["--metrics", str(metrics), "--min-accuracy", "0.9"]) == 0
    assert main(["--metrics", str(metrics), "--min-accuracy", "0.97"]) == 1
