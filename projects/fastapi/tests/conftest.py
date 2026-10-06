"""Finds lib-c's shared library (built by CMake) for the tests, unless MESHLIB_PATH already points to it."""
import os
import pathlib

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]  # projects/
DATA = ROOT / "lib-c" / "tests" / "data"
NAMES = ("libmesh.so", "libmesh.dylib", "mesh.dll")


def _find_library() -> str:
    for build in ROOT.joinpath("lib-c").glob("build*"):
        for name in NAMES:
            found = sorted(build.rglob(name))
            if found:
                return str(found[0])
    raise RuntimeError("lib-c shared library not found: build it with -DBUILD_SHARED_LIBS=ON (see README)")


if "MESHLIB_PATH" not in os.environ:
    os.environ["MESHLIB_PATH"] = _find_library()


@pytest.fixture
def sample():
    return lambda name: (DATA / name).read_bytes()
