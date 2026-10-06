"""Exports the trained PointNet to ONNX and checks the export: ONNX Runtime must give the logits of
PyTorch on the whole test set, and the accuracy it measures is written next to the model.

    python -m shapeml.export --params params.yaml --data data/shapes.npz --out export/pointnet.onnx
"""
from __future__ import annotations

import argparse
import hashlib
import json
import warnings
from pathlib import Path

import numpy as np
import torch
import yaml

from .infer import OnnxClassifier
from .pointnet import PointNet

#: Largest difference accepted between PyTorch and ONNX Runtime logits.
TOLERANCE = 1e-4


def export(state_dict_path: str | Path, classes: list[str], points: int, out: str | Path) -> PointNet:
    """Writes the ONNX model (batch size free, `points` x 3 inputs named "points")."""
    net = PointNet(len(classes))
    net.load_state_dict(torch.load(state_dict_path, map_location="cpu", weights_only=True))
    net.eval()
    # The TorchScript exporter is the legacy one since PyTorch 2.9 but still supported; the new one
    # (dynamo) needs onnxscript, one more dependency, for a model this simple (see ML DECISIONS).
    with warnings.catch_warnings():
        # Only around this call: the legacy exporter's own deprecation notices, nothing else.
        warnings.simplefilter("ignore", DeprecationWarning)
        torch.onnx.export(
            net, torch.zeros(1, points, 3), str(out), input_names=["points"], output_names=["logits"],
            dynamic_axes={"points": {0: "batch"}, "logits": {0: "batch"}}, opset_version=17, dynamo=False,
        )
    return net


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--params", default="params.yaml")
    parser.add_argument("--data", default="data/shapes.npz")
    parser.add_argument("--model", default="models/pointnet.pt")
    parser.add_argument("--out", default="export/pointnet.onnx")
    args = parser.parse_args(argv)
    params = yaml.safe_load(Path(args.params).read_text(encoding="utf-8"))
    data = np.load(args.data)
    classes = [str(c) for c in data["classes"]]
    x_test, y_test = data["x_test"], data["y_test"]
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    net = export(args.model, classes, int(params["generate"]["points"]), out)
    out.with_suffix(".json").write_text(json.dumps({"classes": classes, "points": int(x_test.shape[1])}), encoding="utf-8")

    onnx_model = OnnxClassifier(out)
    with torch.no_grad():
        expected = net(torch.from_numpy(x_test)).numpy()
    got = onnx_model.logits(x_test)
    gap = float(np.abs(expected - got).max())
    if gap > TOLERANCE:
        raise SystemExit(f"ONNX export differs from PyTorch: max |logit difference| = {gap:.2e} > {TOLERANCE}")
    accuracy = float((got.argmax(axis=1) == y_test).mean())
    meta = {
        "model": "pointnet",
        "classes": classes,
        "points": int(x_test.shape[1]),
        "test_accuracy": accuracy,
        "max_logit_gap": gap,
        "onnx_sha256": hashlib.sha256(out.read_bytes()).hexdigest(),
    }
    out.with_suffix(".json").write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")
    print(f"exported {out}: accuracy {accuracy:.4f} with ONNX Runtime, max logit gap {gap:.1e}")


if __name__ == "__main__":
    main()
