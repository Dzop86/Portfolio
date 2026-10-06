"""Unit tests for the ctypes binding to lib-c."""
import pytest

from meshapi.libmesh import MeshError, detect_format, read_stats


def test_reads_an_obj_cube_and_its_topology(sample):
    s = read_stats(sample("cube.obj"))
    assert (s.format, s.vertices, s.polygons, s.triangles) == ("OBJ", 8, 6, 12)
    assert (s.edges, s.boundary_edges, s.euler_characteristic) == (18, 0, 2)
    assert s.bbox_min == (0.0, 0.0, 0.0) and s.bbox_max == (1.0, 1.0, 1.0)


def test_reads_ply_and_stl(sample):
    assert read_stats(sample("tetrahedron.ply")).euler_characteristic == 2
    stl = read_stats(sample("cube.stl"))
    assert (stl.format, stl.vertices, stl.triangles) == ("STL", 8, 12)


def test_torus_has_euler_characteristic_zero(sample):
    assert read_stats(sample("torus.obj")).euler_characteristic == 0


def test_errors_carry_status_and_line():
    with pytest.raises(MeshError) as err:
        read_stats(b"v 0 0 0\nv 1 0 0\nf 1 2 3\n")
    assert (err.value.status, err.value.line) == ("index out of range", 3)
    with pytest.raises(MeshError) as err:
        read_stats(b"bogus\n")
    assert err.value.status == "syntax error"


def test_empty_input_is_an_empty_mesh():
    s = read_stats(b"")
    assert (s.vertices, s.triangles, s.euler_characteristic) == (0, 0, 0)


def test_detects_formats():
    assert detect_format(b"ply\nformat ascii 1.0\n") == "PLY"
    assert detect_format(b"solid x\n") == "STL"
    assert detect_format(b"v 0 0 0\n") == "OBJ"


def test_repeated_reads_do_not_leak_or_crash(sample):
    data = sample("torus.obj")
    for _ in range(500):
        assert read_stats(data).triangles == 96


def test_rejects_non_bytes():
    with pytest.raises(TypeError):
        read_stats("v 0 0 0")  # type: ignore[arg-type]
