"""Synthetic closed meshes for six shape classes, with random size, proportions, resolution, rotation and noise.

All shapes but the torus are surfaces of revolution closed at both poles (or a sphere mapped onto a box), so
they are topological spheres (Euler characteristic 2); the torus has Euler characteristic 0.
"""
from __future__ import annotations

import numpy as np

CLASSES = ("sphere", "torus", "box", "cylinder", "cone", "capsule")


def _revolution(profile: np.ndarray, segments: int) -> tuple[np.ndarray, np.ndarray]:
    """Revolves a profile [(r, z), ...] around z. The first and last points must have r = 0 (poles)."""
    rings = profile[1:-1]
    angles = np.linspace(0, 2 * np.pi, segments, endpoint=False)
    ring_vertices = np.stack(
        [np.outer(rings[:, 0], np.cos(angles)), np.outer(rings[:, 0], np.sin(angles)),
         np.repeat(rings[:, 1:2], segments, axis=1)], axis=-1).reshape(-1, 3)
    vertices = np.vstack([[0, 0, profile[0, 1]], ring_vertices, [0, 0, profile[-1, 1]]])
    top = len(vertices) - 1
    ring = lambda i, j: 1 + i * segments + j % segments  # noqa: E731
    faces = []
    for j in range(segments):
        faces.append((0, ring(0, j + 1), ring(0, j)))
        faces.append((top, ring(len(rings) - 1, j), ring(len(rings) - 1, j + 1)))
        for i in range(len(rings) - 1):
            a, b, c, d = ring(i, j), ring(i, j + 1), ring(i + 1, j + 1), ring(i + 1, j)
            faces += [(a, b, c), (a, c, d)]
    return vertices, np.array(faces)


def _polyline(points: list[tuple[float, float]], samples: int) -> np.ndarray:
    """Resamples a profile polyline with `samples` points per segment, keeping every corner."""
    out = [points[0]]
    for p, q in zip(points, points[1:]):
        for t in np.linspace(0, 1, samples + 1)[1:]:
            out.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
    return np.array(out)


def _random_rotation(rng: np.random.Generator) -> np.ndarray:
    q, r = np.linalg.qr(rng.normal(size=(3, 3)))
    q *= np.sign(np.diag(r))
    if np.linalg.det(q) < 0:
        q[:, 0] = -q[:, 0]
    return q


def make_shape(name: str, rng: np.random.Generator) -> tuple[np.ndarray, np.ndarray]:
    """Vertices (n, 3) and triangles (m, 3) of a random shape of class `name`."""
    segments = int(rng.integers(12, 33))
    steps = int(rng.integers(3, 9))
    if name == "sphere":
        t = np.linspace(0, np.pi, 2 * steps + 1)
        vertices, faces = _revolution(np.stack([np.sin(t), -np.cos(t)], axis=1), segments)
    elif name == "box":
        t = np.linspace(0, np.pi, 2 * steps + 1)
        vertices, faces = _revolution(np.stack([np.sin(t), -np.cos(t)], axis=1), 4 * (segments // 4))
        vertices = vertices / np.abs(vertices).max(axis=1, keepdims=True)  # project the sphere onto the cube
        vertices *= rng.uniform(0.4, 1.0, size=3)
    elif name == "cylinder":
        r, h = rng.uniform(0.3, 1.0), rng.uniform(0.5, 1.5)
        vertices, faces = _revolution(_polyline([(0, -h), (r, -h), (r, h), (0, h)], steps), segments)
    elif name == "cone":
        r, h = rng.uniform(0.3, 1.0), rng.uniform(0.5, 1.5)
        vertices, faces = _revolution(_polyline([(0, -h), (r, -h), (0, h)], steps), segments)
    elif name == "capsule":
        r, h = rng.uniform(0.3, 0.8), rng.uniform(0.3, 1.2)
        t = np.linspace(0, np.pi / 2, steps + 1)
        bottom = np.stack([r * np.sin(t), -h - r * np.cos(t)], axis=1)
        top = np.stack([r * np.cos(t), h + r * np.sin(t)], axis=1)
        vertices, faces = _revolution(np.vstack([bottom, top]), segments)
    elif name == "torus":
        big, small = rng.uniform(0.6, 1.0), rng.uniform(0.15, 0.45)
        rings = int(rng.integers(8, 17))
        u = np.linspace(0, 2 * np.pi, segments, endpoint=False)[:, None]
        v = np.linspace(0, 2 * np.pi, rings, endpoint=False)[None, :]
        vertices = np.stack([(big + small * np.cos(v)) * np.cos(u), (big + small * np.cos(v)) * np.sin(u),
                             small * np.sin(v) * np.ones_like(u)], axis=-1).reshape(-1, 3)
        idx = lambda i, j: (i % segments) * rings + j % rings  # noqa: E731
        faces = np.array([f for i in range(segments) for j in range(rings)
                          for f in ((idx(i, j), idx(i + 1, j), idx(i + 1, j + 1)), (idx(i, j), idx(i + 1, j + 1), idx(i, j + 1)))])
    else:
        raise ValueError(f"unknown shape {name!r}; expected one of {CLASSES}")

    vertices = vertices * rng.uniform(0.5, 2.0) @ _random_rotation(rng).T
    vertices += rng.normal(scale=rng.uniform(0, 0.02) * np.ptp(vertices), size=vertices.shape)
    return vertices, np.asarray(faces, dtype=np.int64)
