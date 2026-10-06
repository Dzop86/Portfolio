"""Point clouds sampled on synthetic shapes: the dataset of the project, built from a seed."""
from __future__ import annotations

import numpy as np

from .shapes import CLASSES, make_shape


def sample_surface(vertices: np.ndarray, faces: np.ndarray, count: int, rng: np.random.Generator,
                   normalise: bool = True) -> np.ndarray:
    """`count` points drawn uniformly on the surface (area-weighted triangles, uniform barycentric coordinates).
    With `normalise`, the cloud is centred on its mean and scaled into the unit sphere."""
    a, b, c = (vertices[faces[:, k]] for k in range(3))
    areas = np.linalg.norm(np.cross(b - a, c - a), axis=1) / 2
    tri = rng.choice(len(faces), size=count, p=areas / areas.sum())
    r1, r2 = rng.random(count), rng.random(count)
    s = np.sqrt(r1)
    points = (1 - s)[:, None] * a[tri] + (s * (1 - r2))[:, None] * b[tri] + (s * r2)[:, None] * c[tri]
    if normalise:
        points = points - points.mean(axis=0)
        points /= np.linalg.norm(points, axis=1).max()
    return points


def build(per_class: int, points: int, seed: int) -> dict[str, np.ndarray]:
    """`per_class` clouds of `points` points for every class, deterministic for a given seed."""
    rng = np.random.default_rng(seed)
    x = np.empty((per_class * len(CLASSES), points, 3), dtype=np.float32)
    y = np.repeat(np.arange(len(CLASSES)), per_class)
    for i, label in enumerate(y):
        vertices, faces = make_shape(CLASSES[label], rng)
        x[i] = sample_surface(vertices, faces, points, rng)
    return {"x": x, "y": y, "classes": np.array(CLASSES)}
