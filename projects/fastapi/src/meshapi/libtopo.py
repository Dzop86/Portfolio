"""ctypes binding to the C API of the C++ topology library (projects/topologie, tools/c_api.cpp).

The library is found through TOPOLIB_PATH. Its C API keeps one mesh at a time, so every call goes through a
lock: FastAPI runs requests in a thread pool.
"""
from __future__ import annotations

import ctypes
import json
import os
import threading
from dataclasses import dataclass
from functools import cache

from .libmesh import MeshError

_INVALID_MESH = 10  # statuses 1..4 are lib-c's
_lock = threading.Lock()


@dataclass(frozen=True)
class Topology:
    components: int
    isolated_vertices: int
    boundary_loops: int
    non_manifold_edges: int
    non_manifold_vertices: int
    manifold: bool
    orientable: bool
    consistently_oriented: bool
    euler_characteristic: int
    genus: int | None
    total_curvature: float


@cache
def _lib() -> ctypes.CDLL:
    path = os.environ.get("TOPOLIB_PATH")
    if not path:
        raise RuntimeError("TOPOLIB_PATH must point to the topology C API (libtopoc.so, libtopoc.dylib or topoc.dll)")
    lib = ctypes.CDLL(path)
    lib.topoc_read.argtypes, lib.topoc_read.restype = [ctypes.c_char_p, ctypes.c_size_t], ctypes.c_int
    lib.topoc_error.argtypes, lib.topoc_error.restype = [], ctypes.c_char_p
    lib.topoc_error_line.argtypes, lib.topoc_error_line.restype = [], ctypes.c_double
    lib.topoc_summary.argtypes, lib.topoc_summary.restype = [], ctypes.c_char_p
    return lib


def loaded() -> bool:
    try:
        _lib()
        return True
    except (OSError, RuntimeError):
        return False


def read_topology(data: bytes) -> Topology:
    """Invariants and total curvature of an OBJ, PLY or STL mesh. Raises MeshError."""
    if not isinstance(data, (bytes, bytearray, memoryview)):
        raise TypeError(f"expected bytes, got {type(data).__name__}")
    data = bytes(data)
    lib = _lib()
    with _lock:  # the C API's state is shared; copy everything out before releasing the lock
        status = lib.topoc_read(data, len(data))
        if status == _INVALID_MESH:
            raise MeshError("invalid mesh", 0, lib.topoc_error().decode())
        if status != 0:
            raise MeshError(lib.topoc_error().decode(), int(lib.topoc_error_line()))
        summary = json.loads(lib.topoc_summary())
    return Topology(
        components=summary["components"],
        isolated_vertices=summary["isolatedVertices"],
        boundary_loops=summary["boundaryLoops"],
        non_manifold_edges=summary["nonManifoldEdges"],
        non_manifold_vertices=summary["nonManifoldVertices"],
        manifold=summary["manifold"],
        orientable=summary["orientable"],
        consistently_oriented=summary["consistentlyOriented"],
        euler_characteristic=summary["euler"],
        genus=summary["genus"],
        total_curvature=summary["totalCurvature"],
    )
