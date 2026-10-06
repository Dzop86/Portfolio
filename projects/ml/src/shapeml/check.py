"""DVC stage `check`: fails (exit code 1) if the best model's test accuracy is below the threshold."""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import yaml


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metrics", default="metrics.json")
    parser.add_argument("--params", default="params.yaml")
    parser.add_argument("--min-accuracy", type=float, help="overrides check.min_accuracy in params.yaml")
    parser.add_argument("--export", default="export/pointnet.json",
                        help="metadata of the exported ONNX model, checked too when present")
    args = parser.parse_args(argv)
    threshold = args.min_accuracy
    if threshold is None:
        threshold = yaml.safe_load(pathlib.Path(args.params).read_text())["check"]["min_accuracy"]
    metrics = json.loads(pathlib.Path(args.metrics).read_text())
    for name, m in metrics.items():
        print(f"{name:10s} accuracy {m['accuracy']:.4f}")
    best = max(m["accuracy"] for m in metrics.values())
    if best < threshold:
        print(f"FAIL: best accuracy {best:.4f} is below the threshold {threshold:.4f}")
        return 1
    exported = pathlib.Path(args.export)
    if exported.exists():
        onnx_accuracy = json.loads(exported.read_text())["test_accuracy"]
        print(f"onnx       accuracy {onnx_accuracy:.4f} (the model the API serves)")
        if onnx_accuracy < threshold:
            print(f"FAIL: the exported model's accuracy {onnx_accuracy:.4f} is below the threshold {threshold:.4f}")
            return 1
    print(f"OK: best accuracy {best:.4f} >= {threshold:.4f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
