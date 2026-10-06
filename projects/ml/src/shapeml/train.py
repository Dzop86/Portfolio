"""DVC stage `train`: trains the baseline and PointNet, evaluates both on the test set, logs everything to
MLflow and writes metrics.json, confusion.json and models/."""
from __future__ import annotations

import argparse
import json
import pathlib
import time

import joblib
import mlflow
import numpy as np
import torch
import yaml
from sklearn.ensemble import HistGradientBoostingClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score

from .features import describe
from .pointnet import predict, train_pointnet


def _scores(y_true: np.ndarray, y_pred: np.ndarray) -> dict:
    return {"accuracy": float(accuracy_score(y_true, y_pred)), "f1_macro": float(f1_score(y_true, y_pred, average="macro"))}


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--params", default="params.yaml")
    parser.add_argument("--data", default="data/shapes.npz")
    args = parser.parse_args(argv)
    params = yaml.safe_load(pathlib.Path(args.params).read_text())
    p = params["train"]
    d = np.load(args.data)
    classes = [str(c) for c in d["classes"]]
    pathlib.Path("models").mkdir(exist_ok=True)
    torch.set_num_threads(p.get("threads", 4))

    mlflow.set_tracking_uri(params["mlflow"]["tracking_uri"])
    mlflow.set_experiment(params["mlflow"]["experiment"])
    metrics, confusion = {}, {}

    with mlflow.start_run(run_name="baseline"):
        start = time.perf_counter()
        model = HistGradientBoostingClassifier(random_state=p["seed"], max_iter=p["baseline_iterations"])
        model.fit(describe(d["x_train"]), d["y_train"])
        y_pred = model.predict(describe(d["x_test"]))
        metrics["baseline"] = _scores(d["y_test"], y_pred) | {"seconds": time.perf_counter() - start}
        confusion["baseline"] = confusion_matrix(d["y_test"], y_pred).tolist()
        mlflow.log_params({"model": "HistGradientBoosting on D2/radial/eigen features", "iterations": p["baseline_iterations"]})
        mlflow.log_metrics(metrics["baseline"])
        joblib.dump(model, "models/baseline.joblib")

    with mlflow.start_run(run_name="pointnet"):
        start = time.perf_counter()
        net, history = train_pointnet(d["x_train"], d["y_train"], epochs=p["epochs"], seed=p["seed"],
                                      batch_size=p["batch_size"], lr=p["lr"])
        y_pred = predict(net, d["x_test"])
        metrics["pointnet"] = _scores(d["y_test"], y_pred) | {"seconds": time.perf_counter() - start}
        confusion["pointnet"] = confusion_matrix(d["y_test"], y_pred).tolist()
        mlflow.log_params({k: p[k] for k in ("epochs", "batch_size", "lr", "seed")} | {"model": "PointNet"})
        for epoch, loss in enumerate(history):
            mlflow.log_metric("train_loss", loss, step=epoch)
        mlflow.log_metrics(metrics["pointnet"])
        torch.save(net.state_dict(), "models/pointnet.pt")

    pathlib.Path("metrics.json").write_text(json.dumps(metrics, indent=2) + "\n")
    pathlib.Path("confusion.json").write_text(json.dumps({"classes": classes, **confusion}, indent=2) + "\n")
    print(json.dumps(metrics, indent=2))


if __name__ == "__main__":
    main()
