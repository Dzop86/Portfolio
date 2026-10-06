"""DVC stage `generate`: builds the train and test sets from params.yaml into data/shapes.npz."""
from __future__ import annotations

import argparse
import pathlib

import numpy as np
import yaml

from .dataset import build


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--params", default="params.yaml")
    parser.add_argument("--out", default="data/shapes.npz")
    args = parser.parse_args(argv)
    p = yaml.safe_load(pathlib.Path(args.params).read_text())["generate"]
    # Different seeds: no test shape is a copy of a training shape.
    train = build(per_class=p["train_per_class"], points=p["points"], seed=p["seed"])
    test = build(per_class=p["test_per_class"], points=p["points"], seed=p["seed"] + 1)
    out = pathlib.Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    np.savez_compressed(out, x_train=train["x"], y_train=train["y"], x_test=test["x"], y_test=test["y"],
                        classes=train["classes"])
    print(f"{out}: {len(train['y'])} training and {len(test['y'])} test clouds of {p['points']} points")


if __name__ == "__main__":
    main()
