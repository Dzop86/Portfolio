"""Hand-made shape descriptors for the baseline: invariant to rotation, scale (clouds are normalised) and
point order. Distances between all pairs of points (D2 shape distribution, Osada et al. 2002), distances to
the centre, and the normalised eigenvalues of the covariance."""
from __future__ import annotations

import numpy as np

D2_BINS = 24
RADIAL_BINS = 16


def describe(clouds: np.ndarray) -> np.ndarray:
    """(n, points, 3) normalised clouds -> (n, features) descriptors."""
    out = []
    for p in clouds.astype(np.float64):
        diff = p[:, None, :] - p[None, :, :]
        d2 = np.sqrt((diff ** 2).sum(-1))[np.triu_indices(len(p), k=1)]
        radial = np.linalg.norm(p, axis=1)
        eig = np.sort(np.linalg.eigvalsh(np.cov(p.T)))[::-1]
        out.append(np.concatenate([
            np.histogram(d2, bins=D2_BINS, range=(0, 2))[0] / len(d2),
            np.histogram(radial, bins=RADIAL_BINS, range=(0, 1))[0] / len(radial),
            [radial.mean(), radial.std(), d2.mean(), d2.std()],
            eig / eig.sum(),
        ]))
    return np.array(out, dtype=np.float32)
