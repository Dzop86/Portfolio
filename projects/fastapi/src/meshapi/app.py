"""FastAPI application: mesh statistics computed by lib-c. Run with `uvicorn meshapi.app:app`."""
from __future__ import annotations

from fastapi import FastAPI, HTTPException, Request
from starlette.concurrency import run_in_threadpool
from pydantic import BaseModel, Field

from . import libmesh

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


class Health(BaseModel):
    status: str
    libmesh: str


@app.get("/health", response_model=Health)
def health() -> Health:
    return Health(status="ok", libmesh="loaded" if libmesh.loaded() else "missing")


@app.post(
    "/v1/mesh/stats",
    response_model=MeshStats,
    responses={413: {"description": "Mesh larger than 32 MiB"}, 422: {"description": "Unreadable mesh: lib-c status and line"}},
)
async def mesh_stats(request: Request) -> MeshStats:
    """Send the mesh file as the raw request body, e.g. `curl --data-binary @cube.obj .../v1/mesh/stats`."""
    declared = request.headers.get("content-length")
    if declared and declared.isdigit() and int(declared) > MAX_BYTES:
        raise HTTPException(413, "mesh larger than 32 MiB")
    body = bytearray()
    async for chunk in request.stream():
        body += chunk
        if len(body) > MAX_BYTES:
            raise HTTPException(413, "mesh larger than 32 MiB")
    try:
        # The C call blocks for up to a second on large meshes: run it in the thread pool, not on the event
        # loop (ctypes releases the GIL during the call).
        s = await run_in_threadpool(libmesh.read_stats, bytes(body))
    except libmesh.MeshError as e:
        raise HTTPException(422, {"status": e.status, "line": e.line}) from e
    return MeshStats(
        format=s.format, vertices=s.vertices, polygons=s.polygons, triangles=s.triangles, edges=s.edges,
        boundary_edges=s.boundary_edges, euler_characteristic=s.euler_characteristic,
        bbox=BoundingBox(min=list(s.bbox_min), max=list(s.bbox_max)),
    )
