"""ctypes binding to lib-c (projects/lib-c), built as a shared library.

The library is found through the MESHLIB_PATH environment variable. Only the reading and topology entry
points are bound; every mesh is freed before returning.
"""
from __future__ import annotations

import ctypes
import os
from dataclasses import dataclass
from functools import cache


class _Vec3(ctypes.Structure):
    _fields_ = [("x", ctypes.c_double), ("y", ctypes.c_double), ("z", ctypes.c_double)]


class _Mesh(ctypes.Structure):  # mirrors `mesh` in mesh/mesh.h
    _fields_ = [
        ("vertices", ctypes.POINTER(_Vec3)),
        ("vertex_count", ctypes.c_size_t),
        ("triangles", ctypes.POINTER(ctypes.c_uint32 * 3)),
        ("triangle_count", ctypes.c_size_t),
        ("polygon_count", ctypes.c_size_t),
    ]


class _Topology(ctypes.Structure):  # mirrors `mesh_topology`
    _fields_ = [
        ("edge_count", ctypes.c_size_t),
        ("boundary_edge_count", ctypes.c_size_t),
        ("euler_characteristic", ctypes.c_int64),
        ("min", _Vec3),
        ("max", _Vec3),
    ]


_FORMATS = ("OBJ", "PLY", "STL")  # mesh_format


class MeshError(ValueError):
    """lib-c could not read the mesh. `line` is 0 when the error has no line (binary data)."""

    def __init__(self, status: str, line: int, detail: str = ""):
        message = f"{status} (line {line})" if line else status
        super().__init__(f"{message}: {detail}" if detail else message)
        self.status = status
        self.line = line
        self.detail = detail


@dataclass(frozen=True)
class MeshStats:
    format: str
    vertices: int
    polygons: int
    triangles: int
    edges: int
    boundary_edges: int
    euler_characteristic: int
    bbox_min: tuple[float, float, float]
    bbox_max: tuple[float, float, float]


@cache
def _lib() -> ctypes.CDLL:
    path = os.environ.get("MESHLIB_PATH")
    if not path:
        raise RuntimeError("MESHLIB_PATH must point to lib-c's shared library (libmesh.so, libmesh.dylib or mesh.dll)")
    lib = ctypes.CDLL(path)
    mesh_p, size_p = ctypes.POINTER(_Mesh), ctypes.POINTER(ctypes.c_size_t)
    lib.mesh_init.argtypes, lib.mesh_init.restype = [mesh_p], None
    lib.mesh_free.argtypes, lib.mesh_free.restype = [mesh_p], None
    lib.mesh_read_buffer.argtypes = [ctypes.c_char_p, ctypes.c_size_t, mesh_p, size_p]
    lib.mesh_read_buffer.restype = ctypes.c_int
    lib.mesh_detect_format.argtypes, lib.mesh_detect_format.restype = [ctypes.c_char_p, ctypes.c_size_t], ctypes.c_int
    lib.mesh_compute_topology.argtypes = [mesh_p, ctypes.POINTER(_Topology)]
    lib.mesh_compute_topology.restype = ctypes.c_int
    lib.mesh_status_string.argtypes, lib.mesh_status_string.restype = [ctypes.c_int], ctypes.c_char_p
    return lib


def loaded() -> bool:
    """True if the library can be loaded."""
    try:
        _lib()
        return True
    except (OSError, RuntimeError):
        return False


def _check_bytes(data: bytes) -> None:
    if not isinstance(data, (bytes, bytearray, memoryview)):
        raise TypeError(f"expected bytes, got {type(data).__name__}")


def detect_format(data: bytes) -> str:
    _check_bytes(data)
    data = bytes(data)
    return _FORMATS[_lib().mesh_detect_format(data, len(data))]


def read_stats(data: bytes) -> MeshStats:
    """Reads an OBJ, PLY or STL mesh from bytes and returns its size and topology. Raises MeshError."""
    _check_bytes(data)
    data = bytes(data)
    lib = _lib()
    mesh = _Mesh()
    lib.mesh_init(ctypes.byref(mesh))
    line = ctypes.c_size_t(0)
    try:
        status = lib.mesh_read_buffer(data, len(data), ctypes.byref(mesh), ctypes.byref(line))
        topo = _Topology()
        if status == 0:
            status = lib.mesh_compute_topology(ctypes.byref(mesh), ctypes.byref(topo))
        if status != 0:
            raise MeshError(lib.mesh_status_string(status).decode(), line.value)
        return MeshStats(
            format=_FORMATS[lib.mesh_detect_format(data, len(data))],
            vertices=mesh.vertex_count,
            polygons=mesh.polygon_count,
            triangles=mesh.triangle_count,
            edges=topo.edge_count,
            boundary_edges=topo.boundary_edge_count,
            euler_characteristic=topo.euler_characteristic,
            bbox_min=(topo.min.x, topo.min.y, topo.min.z),
            bbox_max=(topo.max.x, topo.max.y, topo.max.z),
        )
    finally:
        lib.mesh_free(ctypes.byref(mesh))
