"""Integration tests of POST /v1/mesh/classify: meshes made by the ML project's generator, sent as OBJ
files, read by lib-c and classified by the exported PointNet through ONNX Runtime."""
import numpy as np
import pytest
from fastapi.testclient import TestClient

from meshapi import app as app_module
from meshapi.app import MAX_BYTES, app
from shapeml.shapes import CLASSES, make_shape

client = TestClient(app)


def obj(vertices, faces) -> bytes:
    lines = [f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in vertices]
    lines += [f"f {a + 1} {b + 1} {c + 1}" for a, b, c in faces]
    return ("\n".join(lines) + "\n").encode()


def classify(body: bytes):
    return client.post("/v1/mesh/classify", content=body)


@pytest.mark.parametrize("shape", ["sphere", "torus", "cone", "capsule"])
def test_shapes_the_model_never_confuses_are_recognised(shape):
    # These four classes are recognised at 100 % on the test set; two new meshes each, read by lib-c.
    for seed in (11, 12):
        r = classify(obj(*make_shape(shape, np.random.default_rng(seed))))
        assert r.status_code == 200
        assert r.json()["label"] == shape, (seed, r.json()["probabilities"])


def test_probabilities_cover_the_six_classes_and_sum_to_one():
    body = classify(obj(*make_shape("box", np.random.default_rng(3)))).json()
    assert set(body["probabilities"]) == set(CLASSES)
    assert abs(sum(body["probabilities"].values()) - 1) < 1e-4
    assert body["label"] == max(body["probabilities"], key=body["probabilities"].get)
    assert body["model"]["name"] == "pointnet"
    assert body["model"]["test_accuracy"] > 0.9
    assert len(body["model"]["sha256"]) == 64


def test_the_same_mesh_always_gets_the_same_answer(sample):
    first, second = classify(sample("cube.obj")).json(), classify(sample("cube.obj")).json()
    assert first == second


def test_lib_c_s_cube_is_a_box(sample):
    assert classify(sample("cube.obj")).json()["label"] == "box"
    assert classify(sample("cube.stl")).json()["label"] == "box"


def test_errors_unreadable_no_surface_too_large():
    r = classify(b"v 0 0 0\nv 1 0 0\nf 1 2 3\n")
    assert r.status_code == 422 and r.json()["detail"]["line"] == 3
    flat = classify(b"v 0 0 0\nv 1 0 0\nv 2 0 0\nf 1 2 3\n")
    assert flat.status_code == 422
    assert flat.json()["detail"] == {"status": "cannot classify", "message": "the mesh has no surface area"}
    assert classify(b"#" * (MAX_BYTES + 1)).status_code == 413


def test_without_the_model_the_endpoint_says_503(monkeypatch):
    app_module._classifier.cache_clear()
    monkeypatch.setenv("MODEL_PATH", "/nonexistent/pointnet.onnx")
    try:
        assert classify(b"v 0 0 0\n").status_code == 503
        assert client.get("/health").json()["model"] == "missing"
    finally:
        monkeypatch.undo()
        app_module._classifier.cache_clear()
