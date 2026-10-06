"""Unit tests for the ctypes binding to the C++ topology library."""
import math
from concurrent.futures import ThreadPoolExecutor

import pytest

from meshapi.libmesh import MeshError
from meshapi.libtopo import read_topology


def test_torus_has_genus_one(topo_sample):
    t = read_topology(topo_sample("torus.obj"))
    assert (t.components, t.boundary_loops, t.euler_characteristic, t.genus) == (1, 0, 0, 1)
    assert t.orientable and t.manifold and t.consistently_oriented
    assert abs(t.total_curvature) < 1e-9


def test_moebius_strip_is_not_orientable_and_has_no_genus(topo_sample):
    t = read_topology(topo_sample("mobius.obj"))
    assert not t.orientable
    assert t.boundary_loops == 1
    assert t.genus is None


def test_gauss_bonnet_on_lib_c_samples(sample):
    for name in ("cube.obj", "cube.stl", "tetrahedron.ply"):
        t = read_topology(sample(name))
        assert math.isclose(t.total_curvature, 2 * math.pi * t.euler_characteristic, abs_tol=1e-9), name


def test_reader_errors_keep_lib_c_status_and_line():
    with pytest.raises(MeshError) as err:
        read_topology(b"v 0 0 0\nv 1 0 0\nf 1 2 3\n")
    assert (err.value.status, err.value.line) == ("index out of range", 3)


def test_invalid_meshes_are_reported():
    with pytest.raises(MeshError) as err:
        read_topology(b"v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 2\n")
    assert err.value.status == "invalid mesh"
    assert "degenerate" in str(err.value)


def test_concurrent_calls_do_not_mix_results(topo_sample, sample):
    # The C API keeps one mesh at a time: the binding must serialise calls.
    inputs = [(topo_sample("torus.obj"), 1), (topo_sample("sphere.obj"), 0), (sample("cube.obj"), 0)] * 30
    with ThreadPoolExecutor(max_workers=8) as pool:
        genera = list(pool.map(lambda case: read_topology(case[0]).genus, inputs))
    assert genera == [g for _, g in inputs]
