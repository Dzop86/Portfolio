"""PointNet (Qi et al. 2017), small version: a shared per-point MLP, then a max over the points, which makes
the network invariant to point order, then a classifier."""
from __future__ import annotations

import numpy as np
import torch
from torch import nn


class PointNet(nn.Module):
    def __init__(self, classes: int, width: int = 128):
        super().__init__()
        self.points = nn.Sequential(  # applied to every point independently (1x1 convolutions)
            nn.Conv1d(3, 64, 1), nn.BatchNorm1d(64), nn.ReLU(),
            nn.Conv1d(64, width, 1), nn.BatchNorm1d(width), nn.ReLU(),
            nn.Conv1d(width, 2 * width, 1), nn.BatchNorm1d(2 * width), nn.ReLU(),
        )
        self.head = nn.Sequential(
            nn.Linear(2 * width, width), nn.BatchNorm1d(width), nn.ReLU(), nn.Dropout(0.3),
            nn.Linear(width, classes),
        )

    def forward(self, clouds: torch.Tensor) -> torch.Tensor:
        """(batch, points, 3) -> (batch, classes) logits."""
        features = self.points(clouds.transpose(1, 2))
        return self.head(features.max(dim=2).values)


def _random_rotations(n: int, generator: torch.Generator) -> torch.Tensor:
    q, r = torch.linalg.qr(torch.randn(n, 3, 3, generator=generator))
    return q * torch.sign(torch.diagonal(r, dim1=1, dim2=2)).unsqueeze(1)


def train_pointnet(x: np.ndarray, y: np.ndarray, *, epochs: int, seed: int, batch_size: int = 32,
                   lr: float = 1e-3, augment: bool = True) -> tuple[PointNet, list[float]]:
    """Trains on CPU and returns the model (in eval mode) and the mean loss of every epoch. With `augment`,
    every batch is randomly rotated and jittered, since test shapes come in any orientation."""
    torch.manual_seed(seed)
    generator = torch.Generator().manual_seed(seed)
    model = PointNet(classes=int(y.max()) + 1)
    optimiser = torch.optim.Adam(model.parameters(), lr=lr)
    schedule = torch.optim.lr_scheduler.CosineAnnealingLR(optimiser, T_max=epochs)
    xs, ys = torch.from_numpy(x), torch.from_numpy(y).long()
    history = []
    for _ in range(epochs):
        model.train()
        order = torch.randperm(len(xs), generator=generator)
        total = 0.0
        for start in range(0, len(xs), batch_size):
            idx = order[start:start + batch_size]
            if len(idx) < 2:  # batch norm needs two samples
                continue
            batch = xs[idx]
            if augment:
                batch = batch @ _random_rotations(len(idx), generator).transpose(1, 2)
                batch = batch + 0.01 * torch.randn(batch.shape, generator=generator)
            loss = nn.functional.cross_entropy(model(batch), ys[idx])
            optimiser.zero_grad()
            loss.backward()
            optimiser.step()
            total += loss.item() * len(idx)
        schedule.step()
        history.append(total / len(xs))
    return model.eval(), history


@torch.no_grad()
def predict(model: PointNet, x: np.ndarray, batch_size: int = 256) -> np.ndarray:
    model.eval()
    return np.concatenate([model(torch.from_numpy(x[i:i + batch_size])).argmax(1).numpy()
                           for i in range(0, len(x), batch_size)])
