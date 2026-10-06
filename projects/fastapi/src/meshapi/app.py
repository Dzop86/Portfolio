"""FastAPI application: mesh statistics computed by lib-c. Run with `uvicorn meshapi.app:app`."""
from __future__ import annotations

import math
import os
from functools import cache

from fastapi import FastAPI, HTTPException, Request
from starlette.concurrency import run_in_threadpool
from pydantic import BaseModel, Field

from . import libmesh, libtopo

MAX_BYTES = 32 * 1024 * 1024  # same limit as the browser demos

app = FastAPI(
    title="meshapi",
    version="0.1.0",
    description="Statistics and topology of OBJ, PLY and STL meshes, computed by lib-c (C11) through ctypes.",
)


class BoundingBox(BaseModel):
    min: list[float] = Field(min_length=3, max_length=3)
    max: list[float] = Field(min_length=3, max_length=3)


class MeshStats(BaseModel):
    format: str = Field(description="OBJ, PLY or STL, detected from the content")
    vertices: int
    polygons: int = Field(description="faces in the file (STL: facets kept after welding)")
    triangles: int
    edges: int
    boundary_edges: int
    euler_characteristic: int = Field(description="V - E + F")
    bbox: BoundingBox


class Topology(BaseModel):
    components: int = Field(description="connected components, isolated vertices included")
    isolated_vertices: int
    boundary_loops: int
    non_manifold_edges: int
    non_manifold_vertices: int
    manifold: bool
    orientable: bool
    consistently_oriented: bool
    euler_characteristic: int
    genus: int | None = Field(description="total genus of an orientable manifold, null otherwise")
    total_curvature: float = Field(description="sum of the angle defects, 2 pi chi by Gauss-Bonnet")
    total_curvature_turns: float = Field(description="total_curvature / 2 pi, rounded to 6 decimals")


class Health(BaseModel):
    status: str
    libmesh: str
    libtopo: str
    model: str = Field(description="the ONNX shape classifier: loaded or missing")


class ModelInfo(BaseModel):
    name: str
    test_accuracy: float = Field(description="accuracy on the 600 test clouds, measured with ONNX Runtime")
    sha256: str


class Classification(BaseModel):
    label: str = Field(description="sphere, torus, box, cylinder, cone or capsule")
    probabilities: dict[str, float]
    model: ModelInfo


@cache
def _classifier():
    """The exported PointNet (projects/ml), from MODEL_PATH. Needs the shapeml package with onnxruntime."""
    path = os.environ.get("MODEL_PATH")
    if not path:
        raise RuntimeError("MODEL_PATH must point to the exported model (projects/ml/export/pointnet.onnx)")
    from shapeml.infer import OnnxClassifier

    return OnnxClassifier(path)


def _model_loaded() -> bool:
    try:
        _classifier()
        return True
    except (ImportError, OSError, RuntimeError, ValueError):
        return False


@app.get("/health", response_model=Health)
def health() -> Health:
    state = lambda ok: "loaded" if ok else "missing"  # noqa: E731
    return Health(status="ok", libmesh=state(libmesh.loaded()), libtopo=state(libtopo.loaded()),
                  model=state(_model_loaded()))


async def _read_body(request: Request) -> bytes:
    """The raw request body, refused with 413 beyond MAX_BYTES (checked while streaming)."""
    declared = request.headers.get("content-length")
    if declared and declared.isdigit() and int(declared) > MAX_BYTES:
        raise HTTPException(413, "mesh larger than 32 MiB")
    body = bytearray()
    async for chunk in request.stream():
        body += chunk
        if len(body) > MAX_BYTES:
            raise HTTPException(413, "mesh larger than 32 MiB")
    return bytes(body)


async def _call(function, data: bytes):
    """Runs a C call in the thread pool (it may take a second on large meshes); lib-c errors become 422."""
    try:
        return await run_in_threadpool(function, data)
    except libmesh.MeshError as e:
        detail = {"status": e.status, "line": e.line}
        if e.detail:
            detail["message"] = e.detail
        raise HTTPException(422, detail) from e


ERRORS = {413: {"description": "Mesh larger than 32 MiB"}, 422: {"description": "Unreadable or invalid mesh"}}


@app.post("/v1/mesh/stats", response_model=MeshStats, responses=ERRORS)
async def mesh_stats(request: Request) -> MeshStats:
    """Send the mesh file as the raw request body, e.g. `curl --data-binary @cube.obj .../v1/mesh/stats`."""
    s = await _call(libmesh.read_stats, await _read_body(request))
    return MeshStats(
        format=s.format, vertices=s.vertices, polygons=s.polygons, triangles=s.triangles, edges=s.edges,
        boundary_edges=s.boundary_edges, euler_characteristic=s.euler_characteristic,
        bbox=BoundingBox(min=list(s.bbox_min), max=list(s.bbox_max)),
    )


@app.post("/v1/mesh/topology", response_model=Topology, responses=ERRORS)
async def mesh_topology(request: Request) -> Topology:
    """Topological invariants and total Gaussian curvature, computed by the C++ topology library."""
    t = await _call(libtopo.read_topology, await _read_body(request))
    return Topology(
        **{k: getattr(t, k) for k in libtopo.Topology.__dataclass_fields__},
        total_curvature_turns=round(t.total_curvature / (2 * math.pi), 6) + 0.0,
    )


@app.post("/v1/mesh/classify", response_model=Classification,
          responses=ERRORS | {503: {"description": "The classifier is not installed"}})
async def mesh_classify(request: Request) -> Classification:
    """Shape of the mesh among six classes, by the PointNet of the ML project exported to ONNX: 512 points
    sampled on the surface (fixed seed, so the same mesh always gets the same answer), then ONNX Runtime."""
    data = await _read_body(request)
    if not _model_loaded():
        raise HTTPException(503, "the shape classifier is not installed")
    vertices, triangles = await _call(libmesh.read_arrays, data)
    classifier = _classifier()
    try:
        p = await run_in_threadpool(classifier.classify, vertices, triangles)
    except ValueError as e:  # no triangle, or no surface area
        raise HTTPException(422, {"status": "cannot classify", "message": str(e)}) from e
    meta = classifier.meta
    return Classification(
        label=p.label, probabilities={k: round(v, 6) for k, v in p.probabilities.items()},
        model=ModelInfo(name=meta["model"], test_accuracy=meta["test_accuracy"], sha256=meta["onnx_sha256"]),
    )
