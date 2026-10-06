"""Inference on a mesh with the exported ONNX model: the same preprocessing as for training (surface
sampling, centring, unit sphere), shared with the API so the two cannot drift apart. Needs numpy and,
for `OnnxClassifier`, onnxruntime; not PyTorch."""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np

from .dataset import sample_surface

#: Seed of the sampling at inference time: the same mesh always gives the same answer.
INFERENCE_SEED = 0


def prepare(vertices: np.ndarray, faces: np.ndarray, points: int, seed: int = INFERENCE_SEED) -> np.ndarray:
    """(1, points, 3) float32 cloud for the model. Raises ValueError on a mesh with no surface."""
    vertices = np.asarray(vertices, dtype=np.float64).reshape(-1, 3)
    faces = np.asarray(faces, dtype=np.int64).reshape(-1, 3)
    if len(faces) == 0:
        raise ValueError("the mesh has no triangle")
    a, b, c = (vertices[faces[:, k]] for k in range(3))
    if not np.isfinite(vertices).all() or np.linalg.norm(np.cross(b - a, c - a), axis=1).sum() <= 0:
        raise ValueError("the mesh has no surface area")
    cloud = sample_surface(vertices, faces, points, np.random.default_rng(seed))
    return cloud.astype(np.float32)[None]


def softmax(logits: np.ndarray) -> np.ndarray:
    z = logits - logits.max(axis=-1, keepdims=True)
    e = np.exp(z)
    return e / e.sum(axis=-1, keepdims=True)


@dataclass(frozen=True)
class Prediction:
    label: str
    probabilities: dict[str, float]


class OnnxClassifier:
    """The exported PointNet and its metadata (classes, points per cloud, test accuracy)."""

    def __init__(self, model_path: str | Path):
        import onnxruntime as ort  # imported here: training does not need it

        model_path = Path(model_path)
        self.meta = json.loads(model_path.with_suffix(".json").read_text(encoding="utf-8"))
        self.classes: list[str] = self.meta["classes"]
        self.points: int = self.meta["points"]
        options = ort.SessionOptions()
        options.intra_op_num_threads = 1
        self.session = ort.InferenceSession(str(model_path), options, providers=["CPUExecutionProvider"])

    def logits(self, clouds: np.ndarray) -> np.ndarray:
        return self.session.run(None, {"points": clouds.astype(np.float32)})[0]

    def classify(self, vertices: np.ndarray, faces: np.ndarray) -> Prediction:
        p = softmax(self.logits(prepare(vertices, faces, self.points)))[0]
        return Prediction(self.classes[int(p.argmax())], {c: float(v) for c, v in zip(self.classes, p)})
